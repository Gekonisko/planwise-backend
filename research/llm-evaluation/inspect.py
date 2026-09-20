"""Quick look at a harness report: failures first, then the headline metrics per run."""
import json, sys, glob, os

path = sys.argv[1] if len(sys.argv) > 1 else sorted(glob.glob("runs/llm-evaluation-*.json"))[-1]
report = json.load(open(path, encoding="utf-8"))

print(f"plik: {os.path.basename(path)}")
print(f"model kosztowy: {report['CostModelName']}   model priorytetow: {report['PrioritisationModelName']}")
print()

print("=== KOSZTY ===")
for run in report["CostRuns"]:
    head = f"{run['ScenarioId']}/{run['Attempt']}"
    if run["Error"]:
        print(f"{head:10} BLAD: {run['Error'][:160]}")
        continue
    m = run["Metrics"]
    print(
        f"{head:10} linie={m['LabourLineCount']:2} spozacennika={m['RolesNotInRateCard']} "
        f"stawki_ok={m['RateExactShare']:.2f} pokrycie={m['RateCardCoverage']:.2f} "
        f"scen={m['ScenarioCount']} nazwy={int(m['ScenarioRolesRecognisable'])}{int(m['ScenarioNamesLiteral'])} mono={int(m['ScenarioTotalsMonotonic'])} "
        f"arytm={m['LineArithmeticShare']:.2f} "
        f"suma_odch={('%.4f' % m['SumRelativeDeviation']) if m['SumRelativeDeviation'] is not None else 'n/d'} "
        f"real={m['RealisticTotal']:.0f} godz={m['TotalHours']:.0f} "
        f"t={run['ElapsedSeconds']:.0f}s tok={run['Usage']['OutputTokens']}"
    )

print()
print("=== PRIORYTETY ===")
for run in report["PrioritisationRuns"]:
    head = f"{run['ScenarioId']}/{run['Attempt']}"
    if run["Error"]:
        print(f"{head:10} BLAD: {run['Error'][:160]}")
        continue
    m = run["Metrics"]
    dep = m["DependencyRespectShare"]
    corr = m["ValueScoreRankCorrelation"]
    print(
        f"{head:10} wej={m['InputCount']:2} surowo={m['RawReturnedCount']:2} "
        f"obce={m['RawUnknownKeys']} dupl={m['RawDuplicateKeys']} brak={m['RawMissingKeys']} "
        f"poza01={m['RawScoresOutOfRange']} wynik={m['ResultCount']:2} nieuszer={m['UnrankedCount']} "
        f"zalezn={('%.2f' % dep) if dep is not None else 'n/d'}({m['DependencyEdges']}) "
        f"uzas_roznych={m['ReasonDistinctShare']:.2f} "
        f"korel={('%.2f' % corr) if corr is not None else 'n/d'} "
        f"t={run['ElapsedSeconds']:.0f}s tok={run['Usage']['OutputTokens']}"
    )
