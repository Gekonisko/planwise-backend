# Sprint-slip prediction — TAWOS

> Will an issue committed to a sprint fail to be resolved by that sprint's end date?

This is the model PlanWise's `IRiskPredictionModel` seam was built for. Unlike the Sourcetree study
in [`../resolution-time-model`](../resolution-time-model), the data here carries the fields PlanWise
itself collects, so the label is the one the product actually needs.

## The data

[TAWOS](https://github.com/SOLAR-group/TAWOS) (Tawosi, Al-Subaihin, Moussa & Sarro, *A Versatile
Dataset of Agile Open Source Software Projects*, MSR 2022) — 458,232 Jira issues from 39 agile
open-source projects, with sprints, story points, issue links and change history.

Why the label works here and not in the Sourcetree export: **the deadline is the sprint's end date,
not a `Due Date` field**. Due dates are almost never filled in (49 of 49,000 in the Sourcetree data),
but every issue committed to a sprint inherits that sprint's end date as its commitment.

| | Sourcetree export | TAWOS |
|---|---|---|
| labelable issues | 49 | **19,251** |
| projects | 1 | **36** |
| story points present | 49 | 30,165 |
| dependency links | 98 | 246,587 |
| sprints | none | 4,594 |
| positive-class rate | 4.9% | **41.2%** |

## Loading it

The dataset is **not in this repository**. Download it from the TAWOS release above (figshare
item `21308124`); the distribution is a 4.3 GB MySQL dump, of which `Change_Log` (44%) and
`Comment` (31%) are not needed for this study. Filtering them out first cuts the import to 1.06 GB:

```bash
# run in the unpacked TAWOS.sql/ directory — writes TAWOS_core.sql
python - <<'EOF'
import re
SKIP = {'Change_Log', 'Comment'}
pat = re.compile(rb'^INSERT INTO `([^`]+)`')
inside = False
with open('TAWOS.sql','rb') as src, open('TAWOS_core.sql','wb') as dst:
    for line in src:
        if inside:
            if line.rstrip().endswith(b';'): inside = False
            continue
        m = pat.match(line[:80])
        if m and m.group(1).decode() in SKIP:
            if not line.rstrip().endswith(b';'): inside = True
            continue
        dst.write(line)
EOF

docker run -d --name tawos-mysql -e MYSQL_ROOT_PASSWORD=tawos -e MYSQL_DATABASE=tawos \
  -p 3307:3306 mysql:8
docker exec -i tawos-mysql sh -c 'exec mysql -uroot -ptawos --force tawos' < TAWOS_core.sql
```

Note `information_schema.table_rows` is only an estimate for InnoDB and under-reports badly here —
use `COUNT(*)`.

## Method

### The label

`slipped = resolution_date IS NULL OR resolution_date > sprint.end_date`

Never-resolved issues count as slipped. An issue committed to a sprint and still open is the
clearest case of a missed commitment, and excluding them would bias the data towards work that
eventually got finished.

### Two leakage traps, both found by inspecting the data

1. **Estimates made mid-sprint.** 10,914 of the 30,165 labelable estimated issues had their story
   point set *during or after* the sprint they belonged to. Using those predicts a sprint's outcome
   partly from information that only existed once it was underway. The default
   `--estimate-timing before` keeps only the 19,251 estimated before the sprint started.
2. **Outcome columns.** `Resolution_Time_Minutes`, `Total_Effort_Minutes`, `In_Progress_Minutes`,
   `Timespent`, `Status` and `Resolution` are all populated and all describe what happened. None is
   a feature; `build_dataset.py` never emits them.

`project_key` is also excluded from the features — it is the grouping variable, and including it
would let the model memorise each project's own slip rate.

### Two evaluation protocols

- **Unseen projects** (`GroupKFold` over `project_key`) — can it score a project it has never seen?
  This is the question PlanWise actually faces, since every new customer is an unseen project. The
  Sourcetree study could not run this check at all, having only one project.
- **Forward in time** — train on earlier sprints, test on later ones.

## Results

19,251 issues · 36 projects · 2,686 sprints · slip rate 41.2%

**Unseen projects** (mean ± std over 5 folds):

| features | ROC AUC | PR AUC | Brier | F1 |
|---|---|---|---|---|
| baseline | 0.500 ± 0.000 | 0.411 ± 0.087 | 0.246 | 0.000 |
| **structural** | **0.589 ± 0.020** | 0.485 ± 0.073 | 0.251 | 0.394 |
| metadata | 0.576 ± 0.026 | 0.475 ± 0.101 | 0.267 | 0.328 |
| text | 0.577 ± 0.027 | 0.481 ± 0.093 | 0.243 | 0.314 |
| combined | 0.580 ± 0.028 | 0.486 ± 0.103 | 0.261 | 0.286 |

**Forward in time** (held-out later sprints):

| features | ROC AUC | PR AUC | Brier | F1 |
|---|---|---|---|---|
| baseline | 0.500 | 0.533 | 0.272 | 0.000 |
| structural | 0.594 | 0.630 | 0.265 | 0.456 |
| **metadata** | **0.606** | 0.638 | 0.259 | 0.525 |
| text | 0.601 | 0.618 | 0.252 | 0.499 |
| combined | 0.590 | 0.621 | 0.253 | 0.560 |

### What this says

**The honest headline: the signal is real but weak.** ROC AUC ~0.59 on unseen projects against a
0.500 baseline. Useful for ranking attention, not for telling someone a task will slip.

**Structural features transfer across projects; vocabulary-bound ones do not.** Structural features
are best *and* most stable on unseen projects (0.589 ± 0.020, the tightest spread of any set), while
metadata — which adds `priority` and `issue_type` — is best forward-in-time within the same project
population (0.606) but drops to 0.576 across unseen projects. The cause is visible in the data:
priority is not one vocabulary but several. Some projects use Blocker/Critical/High, others
Major/Minor/Trivial, others just Medium. A model trained on one scheme meets unseen categories in a
held-out project.

**Individually the features are strongly and monotonically predictive**, which is why the modest AUC
is a statement about *combining* them, not about whether they matter:

| feature | slip rate |
|---|---|
| blocking links: 0 → 1 → 2 | 39.3% → 55.3% → **67.9%** |
| story points: ≤1 → 3 → 8 → 13+ | 30.6% → 42.2% → 46.1% → **51.9%** |
| days in backlog: 0–7 → 30–90 → 365+ | 37.0% → 43.7% → **47.8%** |
| sprint commitment: <20 pts → 100–200 pts | 48.6% → **37.5%** |
| assigned vs unassigned | 41.2% vs 41.4% — *no effect* |

Dependencies are the single strongest signal: an issue with two blocking links slips 68% of the
time against a 41% base rate. That is exactly the `open_predecessor_count` feature PlanWise's
`task_feature_snapshots` already captures.

**Calibration is poor across the board** (Brier 0.243–0.267 against a 0.246 baseline). These
probabilities are not honest enough to display as percentages without a calibration step.

### Limitations

- Open-source projects, which may slip differently from commercial teams under delivery pressure.
- The forward-in-time test period has a 53.3% slip rate against 38.2% in training — real drift, not
  an artefact, but it depresses the measured numbers.
- Sprint-level context is coarse: sprint load is included, but not team velocity history, which is
  probably where the missing signal lives.
- `is_assigned` is useless here because 94.5% of issues are assigned. It may still matter in
  PlanWise, where unassigned work is common.

## From study to shipped model

`export_model.py` turns the above into the artifact PlanWise actually serves. It asks a narrower
question than `train.py`: not *what is achievable on this data*, but *what is achievable using only
the features the product can compute for a live task*.

### The serving constraint

Two structural features cannot be produced at inference time and are dropped:

| feature | why not |
|---|---|
| `days_in_backlog` | `ProjectTask` records no creation timestamp anywhere in Delivery, so a task's age is unknowable |
| `assoc_links` | `TaskInsightSummary` exposes predecessors and a blocks-count, but no non-blocking link count |

Training on features that cannot be served is the classic training/serving skew bug — the model
scores well offline, then quietly degrades as the missing inputs get imputed. Dropping them up front
costs a measured **0.011 ROC AUC**.

### The model is linear, and that was measured rather than assumed

`IRiskPredictionModel` requires per-feature attributions, which a linear model gives exactly and a
boosted one only gives through TreeSHAP. That is worth paying for only if boosting actually wins:

| candidate | ROC AUC (unseen projects) | Brier |
|---|---|---|
| logistic, raw values / servable 5 | 0.510 ± 0.073 | 0.246 |
| **logistic, log1p / servable 5** | **0.588 ± 0.031** | **0.240** |
| boosted / servable 5 | 0.569 ± 0.024 | 0.259 |
| logistic, log1p / structural 7 | 0.599 ± 0.035 | 0.239 |
| boosted / structural 7 | 0.587 ± 0.018 | 0.251 |

On raw values logistic regression is a coin flip and boosting wins comfortably. That is not a fact
about model families — it is the non-linearity showing through. The relationships here are monotonic
but sharply stepped, and `log1p` on the skewed counts linearises exactly that: **+0.077 AUC**, enough
to overtake boosting on the same features (**−0.019** in boosting's favour, i.e. against it) and to
match the study's best full-structural result.

So the shipped model is a logistic regression over five log-transformed features. It serves as a few
dozen floats with no inference runtime in the container, and its coefficients *are* the explanation
the UI shows. Platt scaling beat isotonic on held-out projects (Brier 0.2400 against 0.2413) and is
applied to the decision function.

The fitted coefficients match the study's per-feature findings, including the counter-intuitive one:

| feature | coefficient | reading |
|---|---|---|
| `story_point` | +0.347 | bigger tasks slip more |
| `blocking_links` | +0.252 | dependencies dominate |
| `sprint_issue_count` | +0.145 | busier sprints slip more |
| `sprint_story_points` | **−0.409** | *larger* commitments slip **less** |
| `sprint_length_days` | +0.024 | negligible |

### What ships, and what does not

`sprint-slip-model.json` is embedded in `PlanWise.Modules.RiskPrediction.Application` and read by
`TrainedSlipRiskModel`. Three deliberate limits travel with it:

1. **Only task slip probability is learned.** Sprint completion forecasts and day-impact figures
   still come from `VelocityEstimator` and `SprintForecaster`, because TAWOS carries no label for
   *how late* a slipped issue was. The assumptions returned with every run say so.
2. **The scorecard stays the default.** `RiskPrediction:UseTrainedModel` opts in. At 0.59 the
   trained model is no landslide over a tuned heuristic, and its calibration comes from other
   organisations' data — so the switch should be earned against this system's own captured outcomes,
   which `task_feature_snapshots` is accumulating.
3. **The probabilities are not yet honest as percentages.** Calibrated on TAWOS, not on PlanWise.
   Read them as an ordering of attention.

### Keeping the two implementations honest

The artifact is a specification — impute median, clamp at zero, log1p, standardise, weight, sum,
Platt — and it now has two implementations, Python here and C# in `TrainedSlipScorer`. Drift between
them would not throw; it would silently serve different numbers than were evaluated. `golden_cases.py`
prints the expected side of an xUnit theory that pins them together to eight decimal places;
regenerate it whenever the artifact changes.

## Running it

```bash
python build_dataset.py --estimate-timing before --out dataset.parquet
python train.py --folds 5 --test-fraction 0.2      # the study
python export_model.py --folds 5                   # the servable artifact
python golden_cases.py                             # C# parity fixtures
```

`--estimate-timing any` includes the mid-sprint estimates (30,165 rows, leaky — for comparison);
`none` drops the story-point requirement entirely, for measuring what estimates are worth.

Requires a running `tawos-mysql` container, plus `pandas`, `scikit-learn`, `pyarrow`.
