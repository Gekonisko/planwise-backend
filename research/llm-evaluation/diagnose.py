"""Why did some runs report a zero middle-scenario total? Prints the scenario block of every run."""
import json, sys, glob

paths = sys.argv[1:] or [sorted(glob.glob("runs/llm-evaluation-*.json"))[-1]]
runs = []
for p in paths:
    runs += json.load(open(p, encoding="utf-8"))["CostRuns"]

for r in runs:
    if r["Error"]:
        print(f"{r['ScenarioId']}/{r['Attempt']:2}  BLAD {r['Error'][:90]}")
        continue
    m, res = r["Metrics"], r["Result"]
    scen = res.get("Scenarios") or []
    flag = "  <== ZERO" if float(m["RealisticTotal"]) == 0 else ""
    print(f"{r['ScenarioId']}/{r['Attempt']:2}  n_scen={m['ScenarioCount']} "
          f"real={float(m['RealisticTotal']):>10,.0f} skladniki={float(m['ComponentsSum']):>10,.0f}{flag}")
    for s in scen:
        print(f"        - {str(s.get('Name'))[:58]:58} p={s.get('Percentile')} total={s.get('Total')}")
    if not scen:
        print("        (brak scenariuszy w odpowiedzi)")
