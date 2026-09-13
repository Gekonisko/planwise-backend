# Research

Offline studies that establish what PlanWise's predictive features can and cannot do, kept in
this repository because they are the **provenance of models the backend ships**. A trained model
is only as trustworthy as the evaluation behind it, and that evaluation should be reviewable from
the same commit as the code that serves it.

Python, not .NET. Nothing here is in `PlanWise.sln` and `dotnet build` never sees it.

## What lives where

| | |
|---|---|
| these studies | `backend/research/` — offline, run by hand, never at request time |
| a **trained artifact** | inside the module that serves it, e.g. `Modules/RiskPrediction/…Infrastructure/` |
| the raw datasets | **not in this repository** — each study's README says how to obtain and load its own |

The split matters: a study is research that happens to sit near the backend, but an artifact is a
runtime dependency of it. When a trained model is ready it goes next to `WeightedScorecardRiskModel`
as an implementation of `IRiskPredictionModel`, not in this folder.

Derived datasets (`dataset.parquet`) are gitignored. They are regenerable by each study's
`build_dataset.py` and are binary blobs rewritten on every run.

## The studies

### [`sprint-slip-model/`](sprint-slip-model) — TAWOS

*Will an issue committed to a sprint fail to be resolved by that sprint's end date?* This is the
question `IRiskPredictionModel` exists to answer. 19,251 labelled issues across 36 projects.

Headline: **ROC AUC 0.589 on projects the model has never seen**, against a 0.500 baseline. Real
signal, weak — good for ranking attention, not for asserting a task will slip. The useful finding
is that *structural* features (story points, blocking links, backlog age, sprint load) transfer
across projects while vocabulary-bound ones (`priority`, `issue_type`) do not, because priority is
several different Jira schemes rather than one. Dependencies dominate: an issue with two blocking
links slips 68% of the time against a 41% base rate — exactly the `open_predecessor_count` feature
`task_feature_snapshots` already captures.

### [`resolution-time-model/`](resolution-time-model) — Sourcetree export

*How long until an issue is resolved?* Written first, and it is the reason the TAWOS study exists:
the Sourcetree export has 49 usable labels in 49,000 rows because `Due Date` is almost never filled
in. Read the two together — this one establishes why sprint end dates, not due dates, have to be
the deadline.

It stands on its own as a resolution-time study (combined text + metadata reaches ROC AUC 0.795 at
a 30-day horizon) and it documents a censoring trap worth not rediscovering: the first version
scored **0.385, worse than chance**, because a snapshot export can only show a recent issue as
resolved if it resolved quickly.

It also carries [`import_to_planwise.py`](resolution-time-model/import_to_planwise.py), which is not
a study at all — it imports a slice of real issues into a running PlanWise instance as demo data, so
backlogs contain plausible work rather than `test1`.

## Read both before trusting either

Two conclusions survive across them and should govern anything built on this work:

1. **Ranking is usable; calibration is not.** Both studies produce models whose Brier score is no
   better than a constant baseline. No probability from either may reach the UI as a percentage
   without Platt or isotonic calibration on a held-out split.
2. **The heuristic is the baseline to beat, not the thing to replace.** At AUC 0.59 a trained model
   is not an obvious win over a tuned scorecard. `IRiskPredictionModel` keeps
   `WeightedScorecardRiskModel` runnable precisely so that comparison can be measured on PlanWise's
   own captured data rather than assumed.

## Running them

Each study's README has its own data-loading and run instructions. Both need `pandas`,
`scikit-learn` and `pyarrow`; on Windows run them with `PYTHONIOENCODING=utf-8`, as both datasets
contain characters that crash the default cp1252 stdout.
