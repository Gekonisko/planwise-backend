"""Tables for the allocation study: four methods, identical instances, one scoring function.

Reads one or more report files (later files win on duplicate scenario/arm/attempt, so a resumed
series merges with the run it continues) and prints what the thesis quotes.

The gap column is deliberately blank for incomplete allocations. An unowned task occupies nobody's
timeline, so an arm that allocates nothing finishes "soonest"; completeness is reported as its own
column instead, and a mean gap is taken only over complete runs.
"""
import json
import statistics
import sys
import glob
from collections import defaultdict

ARMS = ["cpsat", "greedy", "llm-single-shot", "llm-agent"]
LABEL = {
    "cpsat": "CP-SAT",
    "greedy": "zachlanny",
    "llm-single-shot": "LLM 1 strzal",
    "llm-agent": "LLM agent",
}


def load(paths):
    runs = {}
    for path in paths:
        with open(path, encoding="utf-8") as handle:
            report = json.load(handle)
        for run in report.get("AllocationRuns") or []:
            runs[(run["ScenarioId"], run["Arm"], run["Attempt"])] = run
    return list(runs.values())


def summarise(runs):
    grouped = defaultdict(list)
    for run in runs:
        grouped[(run["ScenarioId"], run["Arm"])].append(run)
    return grouped


def stat(values):
    if not values:
        return None, None, None
    return min(values), statistics.median(values), max(values)


def main():
    paths = sys.argv[1:] or sorted(glob.glob("runs/llm-evaluation-*.json"))
    runs = load(paths)
    if not runs:
        print("Brak przebiegow alokacji w podanych plikach.")
        return

    grouped = summarise(runs)
    scenarios = sorted({key[0] for key in grouped})

    descriptions = {run["ScenarioId"]: run["Description"] for run in runs}

    header = (f"{'scen':6} {'ramie':14} {'n':>2} {'opt':>4} {'dni min/med/max':>17} "
              f"{'luka sr':>8} {'kompl':>6} {'niepopr':>7} {'wywol':>6} {'tok.wy':>8} {'sek':>6}")
    print(header)
    print("-" * len(header))

    for scenario in scenarios:
        print(f"# {scenario}: {descriptions[scenario]}")
        for arm in ARMS:
            batch = grouped.get((scenario, arm))
            if not batch:
                continue

            # A run whose model name says "fallback" is the greedy balancer's answer wearing an LLM
            # label: the API call failed and the optimiser did the honest thing. Counting it as an
            # LLM result would credit the model with a deterministic algorithm's work, so it is
            # excluded from every statistic and reported on its own line.
            fallback_runs = [run for run in batch
                             if run["Metrics"] and run["Metrics"]["FellBackToGreedy"]]
            ok = [run for run in batch if run["Metrics"] and not run["Metrics"]["FellBackToGreedy"]]
            metrics = [run["Metrics"] for run in ok]
            if not metrics:
                errors = [run for run in batch if run["Error"]]
                note = f"{len(fallback_runs)} zejscie do zachlannego" if fallback_runs else f"{len(errors)} blad(ow)"
                print(f"{'':6} {LABEL[arm]:14} {len(batch):>2} {'':>4} {'BRAK DANYCH LLM':>17} {note}")
                continue
            makespans = [m["MakespanDays"] for m in metrics if m["MakespanDays"] >= 0]
            gaps = [m["GapPercent"] for m in metrics if m["GapPercent"] is not None]
            complete = sum(1 for m in metrics if m["Complete"])
            n_real = len(ok)
            invalid = sum(m["InvalidAssignments"] for m in metrics)
            reference = metrics[0]["ReferenceMakespanDays"]

            low, mid, high = stat(makespans)
            gap_text = f"{statistics.mean(gaps):+7.1f}%" if gaps else "      --"
            calls = statistics.mean(run["HttpCalls"] for run in ok) if ok else 0
            tokens = statistics.mean(run["Usage"]["OutputTokens"] for run in ok) if ok else 0
            seconds = statistics.mean(run["ElapsedSeconds"] for run in ok) if ok else 0

            span = f"{low}/{mid:.0f}/{high}" if makespans else "--"
            print(f"{'':6} {LABEL[arm]:14} {n_real:>2} {reference:>4} {span:>17} "
                  f"{gap_text:>8} {complete:>3}/{n_real:<2} {invalid:>7} "
                  f"{calls:>6.1f} {tokens:>8.0f} {seconds:>6.1f}")

            errors = [run for run in batch if run["Error"]]
            if errors:
                print(f"{'':21} BLEDY: {len(errors)} — {errors[0]['Error'][:70]}")
            if fallback_runs:
                print(f"{'':21} ODRZUCONO {len(fallback_runs)} przebieg(ow): wywolanie API padlo, odpowiedzial zachlanny")
        print()

    print("=== zbiorczo po ramionach (srednia luka nad optimum, tylko kompletne alokacje) ===")
    for arm in ARMS:
        gaps, tokens, calls = [], [], []
        complete = total = 0
        for scenario in scenarios:
            for run in grouped.get((scenario, arm), []):
                metrics = run["Metrics"]
                if not metrics or metrics["FellBackToGreedy"]:
                    continue
                total += 1
                if metrics["Complete"]:
                    complete += 1
                if metrics["GapPercent"] is not None:
                    gaps.append(metrics["GapPercent"])
                tokens.append(run["Usage"]["OutputTokens"])
                calls.append(run["HttpCalls"])

        if not total:
            continue
        gap_text = f"{statistics.mean(gaps):+6.1f}%" if gaps else "    --"
        spread = f" (rozrzut {min(gaps):+.1f}..{max(gaps):+.1f})" if len(gaps) > 1 else ""
        print(f"{LABEL[arm]:14} luka {gap_text}{spread}; kompletne {complete}/{total}; "
              f"srednio {statistics.mean(calls):.1f} wywolan, {statistics.mean(tokens):.0f} tokenow wyjscia")

    # Repeatability of the two LLM arms: the same question the cost-estimation series asked, applied
    # to a quantity that has a correct answer. A deterministic arm has nothing to report here.
    print()
    print("=== powtarzalnosc ramion LLM (rozrzut dlugosci projektu przy identycznym wejsciu) ===")
    for scenario in scenarios:
        for arm in ("llm-single-shot", "llm-agent"):
            batch = [run["Metrics"] for run in grouped.get((scenario, arm), [])
                     if run["Metrics"] and not run["Metrics"]["FellBackToGreedy"]]
            values = [m["MakespanDays"] for m in batch if m["MakespanDays"] >= 0]
            if len(values) < 2:
                continue
            mean = statistics.mean(values)
            cv = statistics.pstdev(values) / mean if mean else 0
            unique = sorted(set(values))
            print(f"{scenario:6} {LABEL[arm]:14} n={len(values):>2} CV={cv:5.3f} "
                  f"wartosci={unique}")


if __name__ == "__main__":
    main()
