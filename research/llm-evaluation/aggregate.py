"""
Aggregates a harness report into the three axes of the research question.

Per-run metrics are computed in the C# harness; this script adds everything that only exists
*between* runs — repeatability — and prints the tables used in the thesis.

No ground truth about real spend is used anywhere: conformance is measured against the input the
model was given, consistency against the answer's own internal arithmetic, and repeatability
against other runs of an identical input.
"""
import json
import sys
import glob
import os
import statistics as stats
from collections import defaultdict


def load(paths=None):
    """Merges one or more reports. An interrupted series can therefore be resumed into a new file
    and analysed together with what had already been collected."""
    paths = paths or [sorted(glob.glob("runs/llm-evaluation-*.json"))[-1]]
    merged = None
    for path in paths:
        with open(path, encoding="utf-8") as handle:
            report = json.load(handle)
        if merged is None:
            merged = report
        else:
            merged["CostRuns"] = _replace(merged["CostRuns"], report["CostRuns"])
            merged["PrioritisationRuns"] = _replace(merged["PrioritisationRuns"], report["PrioritisationRuns"])
    return paths, merged


def _replace(existing, incoming):
    """Later files win on a repeated (scenario, arm, attempt). Concatenating instead would keep both
    copies of a scenario that was re-run after a fix and silently double its weight in every average."""
    by_key = {(r["ScenarioId"], r.get("Arm"), r["Attempt"]): r for r in existing}
    by_key.update({(r["ScenarioId"], r.get("Arm"), r["Attempt"]): r for r in incoming})
    return list(by_key.values())


def fmt(value, places=2, dash="n/d"):
    return dash if value is None else f"{value:.{places}f}"


def get(metric, key, fallback=None, default=0):
    """Tolerates reports written before a metric was renamed."""
    if key in metric:
        return metric[key]
    if fallback and fallback in metric:
        return metric[fallback]
    return default


def cv(values):
    """Coefficient of variation; undefined for a zero mean or a single observation."""
    if len(values) < 2:
        return None
    mean = stats.fmean(values)
    return None if mean == 0 else stats.stdev(values) / mean


def jaccard(a, b):
    a, b = set(a), set(b)
    return len(a & b) / len(a | b) if a | b else None


def components_sum(result):
    """Sum of the model's own line items. Independent of how it chose to name its scenarios."""
    labour = sum(float(x["Cost"]) for x in (result.get("LabourLines") or []))
    non_labour = sum(float(x["Amount"]) for x in (result.get("NonLabourLines") or []))
    return labour + non_labour


def best_scenario_gap(result):
    """
    Smallest relative gap between any scenario total and the sum of the line items.

    Measured this way on purpose. A scenario total above the line items is not automatically an
    error: the model routinely publishes a base variant plus uplifted ones ("P70 — with 12%
    contingency"). What would be an inconsistency is if *no* variant reconciles with the components
    it was built from. This reports the closest one.
    """
    scenarios = result.get("Scenarios") or []
    base = components_sum(result)
    if not scenarios or base <= 0:
        return None
    return min(abs(float(s["Total"]) - base) / base for s in scenarios)


ARM_LABEL = {"llm-single-shot": "1s", "llm-agent": "ag"}


def cost_tables(runs):
    # Keyed by scenario *and* arm: the two arms answer the same scenario, so merging them would
    # average a single-shot answer with a checked one and describe neither. Runs from before the
    # arms existed carry no Arm field and are labelled as the single-shot behaviour they were.
    by_scenario = defaultdict(list)
    for run in runs:
        arm = ARM_LABEL.get(run.get("Arm") or "llm-single-shot", run.get("Arm") or "?")
        by_scenario[f'{run["ScenarioId"]}/{arm}'].append(run)

    print("=" * 112)
    print("ESTYMACJA KOSZTOW — zgodnosc z danymi projektu")
    print("=" * 112)
    print(f"{'scen':8} {'n':>3} {'blad':>5} {'role_obce':>9} {'stawki':>7} {'pokrycie':>8} {'tryb_awarii':>40}")
    for scenario, group in sorted(by_scenario.items()):
        ok = [r for r in group if r["Error"] is None]
        failed = [r for r in group if r["Error"]]
        kinds = sorted({r["Error"].split(":")[0] for r in failed})
        if not ok:
            print(f"{scenario:8} {len(group):3} {len(failed):5} {'—':>9} {'—':>7} {'—':>8} {', '.join(kinds):>40}")
            continue
        m = [r["Metrics"] for r in ok]
        print(
            f"{scenario:8} {len(group):3} {len(failed):5} "
            f"{sum(x['RolesNotInRateCard'] for x in m):9} "
            f"{fmt(stats.fmean(x['RateExactShare'] for x in m)):>7} "
            f"{fmt(stats.fmean(x['RateCardCoverage'] for x in m)):>8} "
            f"{(', '.join(kinds) if kinds else '—'):>40}"
        )
    print()
    print("  role_obce   = pozycje pracy z rola spoza cennika (suma po przebiegach)")
    print("  stawki      = udzial pozycji ze stawka dokladnie ta podana w cenniku")
    print("  pokrycie    = udzial rol z cennika wykorzystanych w kosztorysie")
    print()

    print("=" * 112)
    print("ESTYMACJA KOSZTOW — spojnosc wewnetrzna odpowiedzi")
    print("=" * 112)
    print(f"{'scen':8} {'n':>3} {'bez_scen':>9} {'3_scen':>7} {'nazwy':>7} {'mono':>6} "
          f"{'arytm':>6} {'najl_odch':>10} {'uzgodn':>7}")
    for scenario, group in sorted(by_scenario.items()):
        ok = [r for r in group if r["Error"] is None]
        if not ok:
            continue
        m = [r["Metrics"] for r in ok]
        empty = sum(1 for x in m if x["ScenarioCount"] == 0)
        gaps = [g for g in (best_scenario_gap(r["Result"]) for r in ok) if g is not None]
        reconciled = sum(1 for g in gaps if g <= 0.005)
        print(
            f"{scenario:8} {len(ok):3} {empty:9} "
            f"{sum(1 for x in m if x['ScenarioCount'] == 3):>4}/{len(m):<2} "
            f"{sum(1 for x in m if get(x, 'ScenarioRolesRecognisable', 'ScenarioNamesCanonical')):>4}/{len(m):<2} "
            f"{sum(1 for x in m if x['ScenarioTotalsMonotonic']):>3}/{len(m):<2} "
            f"{fmt(stats.fmean(x['LineArithmeticShare'] for x in m)):>6} "
            f"{fmt(stats.fmean(gaps), 4) if gaps else 'n/d':>10} "
            f"{reconciled:>3}/{len(gaps):<3}"
        )
    print()
    print("  bez_scen    = przebiegi, w ktorych odpowiedz nie zawierala zadnego wariantu kosztowego")
    print("  nazwy       = przebiegi z rozpoznawalnymi nazwami trzech wariantow")
    print("  arytm       = udzial pozycji spelniajacych cost = hours x hourlyRate")
    print("  najl_odch   = najmniejsze wzgledne odchylenie sumy wariantu od sumy pozycji")
    print("  uzgodn      = przebiegi, w ktorych jakikolwiek wariant uzgadnia sie z pozycjami (tol. 0,5%)")
    print()

    print("=" * 112)
    print("ESTYMACJA KOSZTOW — powtarzalnosc przy identycznym wejsciu")
    print("=" * 112)
    print(f"{'scen':8} {'n':>3} {'suma_sr':>10} {'suma_cv':>8} {'min':>10} {'max':>10} {'rozstep%':>9} "
          f"{'godz_sr':>8} {'godz_cv':>8} {'role_jacc':>10}")
    for scenario, group in sorted(by_scenario.items()):
        ok = [r for r in group if r["Error"] is None]
        # Anchored on the line items, not on a scenario total: the middle variant is labelled
        # inconsistently across runs (sometimes the base, sometimes base plus contingency), so
        # comparing it between runs would mix two different quantities.
        sums = [components_sum(r["Result"]) for r in ok]
        sums = [x for x in sums if x > 0]
        if len(sums) < 2:
            continue
        m = [r["Metrics"] for r in ok]
        hours = [float(x["TotalHours"]) for x in m if float(x["TotalHours"]) > 0]
        role_sets = [set(x["HoursByRole"].keys()) for x in m]
        pairs = [jaccard(a, b) for i, a in enumerate(role_sets) for b in role_sets[i + 1:]]
        spread = (max(sums) - min(sums)) / stats.fmean(sums)
        print(
            f"{scenario:8} {len(sums):3} {stats.fmean(sums):10,.0f} {fmt(cv(sums), 3):>8} "
            f"{min(sums):10,.0f} {max(sums):10,.0f} {spread * 100:9.0f} "
            f"{stats.fmean(hours):8,.0f} {fmt(cv(hours), 3):>8} "
            f"{fmt(stats.fmean(p for p in pairs if p is not None)):>10}"
        )
    print()
    print("  suma_*      = suma wszystkich pozycji kosztowych (praca + pozaplacowe); kotwica")
    print("                niezalezna od nazewnictwa wariantow")
    print("  rozstep%    = (max - min) / srednia, w procentach")
    print("  role_jacc   = srednie podobienstwo Jaccarda zbioru uzytych rol miedzy parami przebiegow")
    print()


def prioritisation_tables(runs):
    by_scenario = defaultdict(list)
    for run in runs:
        by_scenario[run["ScenarioId"]].append(run)

    print("=" * 108)
    print("PRIORYTETYZACJA — zgodnosc (warstwa surowa modelu) i spojnosc")
    print("=" * 108)
    print(f"{'scen':8} {'n':>3} {'blad':>5} {'zadan':>6} {'obce':>5} {'dupl':>5} {'brak':>5} "
          f"{'poza01':>7} {'nieuszer':>9} {'zaleznosci':>11} {'uzas_roznych':>13} {'korel':>6}")
    for scenario, group in sorted(by_scenario.items()):
        ok = [r for r in group if r["Error"] is None]
        failed = len(group) - len(ok)
        if not ok:
            print(f"{scenario:8} {len(group):3} {failed:5}   — wszystkie proby nieudane")
            continue
        m = [r["Metrics"] for r in ok]
        dep = [x["DependencyRespectShare"] for x in m if x["DependencyRespectShare"] is not None]
        corr = [x["ValueScoreRankCorrelation"] for x in m if x["ValueScoreRankCorrelation"] is not None]
        print(
            f"{scenario:8} {len(group):3} {failed:5} {m[0]['InputCount']:6} "
            f"{sum(x['RawUnknownKeys'] for x in m):5} {sum(x['RawDuplicateKeys'] for x in m):5} "
            f"{sum(x['RawMissingKeys'] for x in m):5} {sum(x['RawScoresOutOfRange'] for x in m):7} "
            f"{sum(x['UnrankedCount'] for x in m):9} "
            f"{fmt(stats.fmean(dep)) if dep else 'n/d':>11} "
            f"{fmt(stats.fmean(x['ReasonDistinctShare'] for x in m)):>13} "
            f"{fmt(stats.fmean(corr)) if corr else 'n/d':>6}"
        )
    print()
    print("  obce/dupl/brak = klucze zadan wymyslone / powtorzone / pominiete przez model (sumy)")
    print("  nieuszer       = pozycje, ktore warstwa aplikacji musiala dopisac sama")
    print("  zaleznosci     = udzial par (poprzednik, nastepnik) uszeregowanych poprawnie")
    print("  korel          = korelacja rangowa wlasnej oceny wartosci modelu z nadana pozycja")
    print()

    print("=" * 108)
    print("PRIORYTETYZACJA — powtarzalnosc przy identycznym wejsciu")
    print("=" * 108)
    print(f"{'scen':8} {'n':>3} {'par':>4} {'D_rank_sr':>10} {'D_rank_max':>11} "
          f"{'rho_sr':>7} {'rho_min':>8} {'top5_jacc':>10} {'identyczne':>11}")
    for scenario, group in sorted(by_scenario.items()):
        ok = [r for r in group if r["Error"] is None]
        if len(ok) < 2:
            continue
        orders = [r["Metrics"]["Order"] for r in ok]
        displacements, rhos, tops, identical = [], [], [], 0
        pairs = 0
        for i, first in enumerate(orders):
            for second in orders[i + 1:]:
                pairs += 1
                positions = {key: index for index, key in enumerate(second)}
                shared = [(index, positions[key]) for index, key in enumerate(first) if key in positions]
                if shared:
                    displacements.append(stats.fmean(abs(a - b) for a, b in shared))
                if len(shared) >= 3:
                    xs = [a for a, _ in shared]
                    ys = [b for _, b in shared]
                    try:
                        rhos.append(stats.correlation(xs, ys))
                    except stats.StatisticsError:
                        pass
                tops.append(jaccard(first[:5], second[:5]))
                identical += first == second
        print(
            f"{scenario:8} {len(ok):3} {pairs:4} {fmt(stats.fmean(displacements) if displacements else None):>10} "
            f"{fmt(max(displacements) if displacements else None):>11} "
            f"{fmt(stats.fmean(rhos) if rhos else None, 3):>7} {fmt(min(rhos) if rhos else None, 3):>8} "
            f"{fmt(stats.fmean(t for t in tops if t is not None)):>10} {identical:>6}/{pairs:<4}"
        )
    print()
    print("  D_rank     = srednia bezwzgledna zmiana pozycji zadania miedzy dwoma przebiegami")
    print("  rho        = korelacja rangowa Spearmana miedzy kolejnosciami")
    print("  identyczne = pary przebiegow o dokladnie tej samej kolejnosci")
    print()


def budget(report):
    runs = report["CostRuns"] + report["PrioritisationRuns"]
    calls = sum(r["Usage"]["Calls"] for r in runs)
    inp = sum(r["Usage"]["InputTokens"] for r in runs)
    out = sum(r["Usage"]["OutputTokens"] for r in runs)
    seconds = sum(r["ElapsedSeconds"] for r in runs)
    print("=" * 108)
    print("KOSZT EKSPERYMENTU")
    print("=" * 108)
    print(f"przebiegi: {len(runs)}   wywolania HTTP: {calls}   "
          f"tokeny wejscia: {inp:,}   tokeny wyjscia: {out:,}   czas: {seconds / 60:.1f} min")
    print(f"model kosztowy: {report['CostModelName']}   model priorytetow: {report['PrioritisationModelName']}")
    print()


if __name__ == "__main__":
    paths, report = load(sys.argv[1:] or None)
    print(f"raporty: {', '.join(os.path.basename(p) for p in paths)}")
    print(f"rozpoczeto: {report['StartedUtc']}")
    print()
    budget(report)
    cost_tables(report["CostRuns"])
    prioritisation_tables(report["PrioritisationRuns"])
