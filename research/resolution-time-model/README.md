# Resolution-time study — public JIRA data

A standalone feasibility study, **not** part of the PlanWise application. It asks one question:

> Given only what is known when an issue is filed — its text, type, priority, component and a few
> shape features — can you predict how long it will take to resolve?

## Why this is separate from PlanWise's risk model

The original plan was to train PlanWise's `IRiskPredictionModel` on this export. That is not
possible, for two independent reasons:

| | |
|---|---|
| **No label** | 49 of 49,000 issues have a due date. "Will this slip past its due date" cannot be learned from 49 examples. |
| **No features** | Story points: 49. Original estimate: 0. Time spent: 0. Assignee: 1,078 (2.2%). Blocker links: 98 (0.2%). Business value: 0. Of the eighteen features PlanWise captures per task, this export can populate about three. |

There is also a domain mismatch: the data is a public bug tracker for a desktop application, where
the median time to resolution is 128 days and the p90 is 386. That is triage-queue behaviour, not
sprint delivery, so even a well-fitted model would be learning the wrong thing.

What the export *does* support is resolution time, which is what this study measures.

## The data

Atlassian's public **Sourcetree for Windows** (`SRCTREEWIN`) issue tracker — a single project,
49,000 issues created 2013–2023, 84% bugs and 16% suggestions. Snapshot taken 2023‑03‑17.

Usable fields: `Summary` and `Description` (both 100% filled, median description 382 chars),
`Component/s` (99.7%, 18 distinct values), `Priority` (85%, though 77% of those are "Low"),
`Issue Type`, `Votes`, `Created`, `Resolved`.

## Method

**Censoring is the whole methodological problem, and the first version of this study got it wrong.**
The export is a snapshot taken on a fixed date, so a recently filed issue can only *appear* resolved
if it resolved quickly. Training on "resolved issues, split by date" produced a training set that
was 20% fast against a test set that was 70% fast, and every metric measured that artefact rather
than the model — ROC AUC came out at 0.385, worse than chance.

Two rules fix it:

1. **Observation window.** Only issues created at least `--horizon-days` (default 365) before the
   snapshot are eligible, so every issue in the study has had the same time to resolve. This drops
   9,163 issues as too recent to judge, leaving 39,837.
2. **Unresolved issues are label 0, not dropped.** An issue that never resolved is the strongest
   possible example of "did not resolve within 30 days"; discarding it is survivorship bias, and it
   would have discarded 33,761 of 49,000 rows.

Together these make the target fully observed: for every eligible issue, "resolved within N days"
is a known fact rather than a guess. The base-rate check in the output confirms it — train and test
rates now sit within about a point of each other, where before they differed by 50.

Other choices:

- **Split by creation date, not at random.** A random split would let the model learn from issues
  filed after the ones it is tested on — leakage, and not how the model would be used.
- **Thresholds, not regression.** Regression on days would have to either drop the never-resolved
  issues (bias again) or invent a duration for them. A threshold question is answerable for every
  row, and `P(resolves within the time available)` is the shape a delivery-risk estimate needs.
- **Leakage columns excluded**: `Status`, `Resolution`, `Resolved`, `Updated` are used only to build
  the target, never as features. `Status` in particular is near-perfectly predictive (`Closed`
  implies resolved) and would have produced a meaningless 0.99 AUC.
- **Top-decile metrics, not precision at 0.5.** Only ~5% of issues resolve quickly, so almost
  nothing crosses a 0.5 probability and precision/recall there read as 0.000 for every model — a
  property of the threshold, not the model.

## Results

Test set: 7,968 issues created 2021‑05‑24 → 2022‑03‑17.

**Resolved within 30 days** (base rate 4.9%):

| features | model | ROC AUC | PR AUC | Brier | prec@10% | lift@10% |
|---|---|---|---|---|---|---|
| baseline | base rate | 0.500 | 0.049 | 0.047 | 0.044 | 0.89 |
| metadata | HistGradientBoosting | 0.741 | 0.103 | 0.057 | 0.062 | 1.25 |
| text | LogisticRegression | 0.647 | 0.149 | 0.047 | 0.185 | 3.75 |
| **combined** | **LogisticRegression** | **0.795** | **0.175** | **0.047** | **0.200** | **4.06** |

Across all four thresholds:

| threshold | base rate | best ROC AUC | best lift@10% |
|---|---|---|---|
| 7 days | 4.9% | 0.697 (combined) | 2.50 |
| 30 days | 4.9% | 0.795 (combined) | 4.06 |
| 90 days | 8.6% | 0.686 (combined) | 2.86 |
| 180 days | 11.1% | 0.674 (text) | 3.89 |

### What this says

- **There is real signal.** At the 30-day threshold the ranking reaches ROC AUC 0.795, and the top
  decile contains 20.0% true positives against a 4.9% base rate — a 4× lift over choosing at random.
- **Text and metadata are complementary.** Neither alone reaches the combined score at 30 days
  (0.647 and 0.741 against 0.795), so what an issue *says* and what it *is* carry different
  information.
- **Text matters more the longer the horizon.** At 180 days text alone is the best model; metadata
  alone collapses to 0.508, essentially chance.
- **Ranking is usable, calibration is not.** The boosted metadata model ranks well (0.741) but has a
  *worse* Brier score than the constant baseline (0.057 vs 0.047), meaning its probabilities are
  poorly calibrated even where its ordering is good. Any use of these numbers as displayed
  percentages would need a calibration step (Platt scaling or isotonic regression) first.

### Limitations, stated plainly

- One project, one product, one team's working practices. Nothing here establishes that the result
  generalises.
- 77% of issues are priority "Low", so priority carries little information in this data.
- The strongest available features in a real delivery system — story points, estimates, dependency
  structure, assignee load — are absent, so this is a weaker feature set than PlanWise itself
  collects.
- Resolution time in a public tracker includes months of sitting untriaged. It is not "time spent
  working", and should not be read as an effort estimate.

## How this connects back to PlanWise

If expected resolution time can be predicted, then slip risk follows:
`P(slip) = P(resolution_time > days_until_due)`. The threshold models above estimate exactly that
quantity at fixed horizons.

The catch is calibration. Durations learned from a 128-day-median triage queue would be badly wrong
for sprint tasks. Bridging the two needs PlanWise's own labelled data — which the
`task_feature_snapshots` capture pipeline in the RiskPrediction module is now accumulating.

## Running it

The export is **not in this repository** — it is Atlassian's public Sourcetree for Windows tracker,
distributed as `GFG_FINAL.csv.zip`. Point `build_dataset.py` at wherever you keep it:

```bash
python build_dataset.py /path/to/GFG_FINAL.csv.zip --out dataset.parquet
python train.py --horizon-days 365 --test-fraction 0.2
```

`build_dataset.py` accepts the `.zip` directly. Outputs `dataset.parquet` (all 49,000 rows, with
unresolved issues kept and a null duration) and `results.json`.

Requires `pandas`, `numpy`, `scikit-learn`, `scipy`, `pyarrow`.
