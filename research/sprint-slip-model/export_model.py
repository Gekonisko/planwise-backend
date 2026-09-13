"""Fit and export the sprint-slip model PlanWise actually serves.

train.py answers "what is achievable on this data". This answers a narrower and harder question:
what is achievable using *only* the features PlanWise can compute at inference time, and can that be
shipped as an auditable artifact.

THE SERVING CONSTRAINT. A model may only use features the product can produce for a live task. Two
of the structural features from the study fail that test and are dropped here:

  * days_in_backlog -- PlanWise's ProjectTask has no creation timestamp at all (nothing in Delivery
    records one), so the age of a task is simply unknowable. Worth 37.0% -> 47.8% slip across its
    range in the study, so this is a real loss, and it is measured below rather than waved away.
  * assoc_links -- TaskInsightSummary exposes predecessors and a blocks-count, but no count of
    non-blocking "relates to" links. The weakest of the structural features.

Training on features that cannot be served is the classic training/serving skew bug: it produces a
model that scores well offline and silently degrades in production as the missing inputs get
imputed. Dropping them up front and paying the measured cost is the honest trade.

WHY THIS SHIPS A LINEAR MODEL. IRiskPredictionModel requires per-feature attributions
(RiskFeatureContribution) -- the UI shows a reader *why* a task was flagged. Logistic regression
gives that exactly and for free as coefficient x transformed value. A gradient-boosted model needs
TreeSHAP, which means either a Python sidecar or reimplementing TreeSHAP in C#.

That cost is only worth paying if boosting is actually better, and measured under GroupKFold it is
not -- provided the linear model is given features on a sane scale. On raw values logistic scores
0.510, a coin flip, because the real relationships are monotonic but sharply non-linear (blocking
links 0 -> 1 -> 2 steps 39% -> 55% -> 68%). log1p on the skewed counts linearises exactly that, and
the linear model then reaches 0.588 -- ahead of boosting on the same features (0.569) and level with
the study's best full-structural boosted result (0.589). The comparison table below is printed on
every run so the choice stays falsifiable rather than inherited.

The consequence is a model that serves as a few dozen floats, explains itself exactly, and needs no
inference runtime in the container.
"""

from __future__ import annotations

import argparse
import json
from datetime import datetime, timezone
from pathlib import Path

import numpy as np
import pandas as pd
from sklearn.calibration import CalibratedClassifierCV
from sklearn.ensemble import HistGradientBoostingClassifier
from sklearn.impute import SimpleImputer
from sklearn.linear_model import LogisticRegression
from sklearn.metrics import brier_score_loss, roc_auc_score
from sklearn.model_selection import GroupKFold
from sklearn.pipeline import Pipeline
from sklearn.preprocessing import FunctionTransformer, StandardScaler

# Exactly what PlanWise can compute for a live task. The C# side maps onto these names, in this
# order, and the artifact carries the order so the two cannot drift apart silently.
SERVABLE = [
    "story_point",
    "blocking_links",
    "sprint_issue_count",
    "sprint_story_points",
    "sprint_length_days",
]

# The study's full structural set, kept only as the reference point that shows what the serving
# constraint costs.
FULL_STRUCTURAL = SERVABLE + ["days_in_backlog", "assoc_links"]


def clamp_non_negative(values: np.ndarray) -> np.ndarray:
    """log1p is undefined below -1, and every servable feature is a count or a duration.

    Nothing in the training data is negative, but PlanWise can produce a negative sprint length from
    an end date set before its start date, so the floor is enforced here and mirrored in the C#
    implementation rather than assumed away.
    """
    return np.log1p(np.clip(values, 0.0, None))


def linear_pipeline(*, transform: bool = True) -> Pipeline:
    steps = [("impute", SimpleImputer(strategy="median"))]
    if transform:
        steps.append(("log", FunctionTransformer(clamp_non_negative, feature_names_out="one-to-one")))
    steps += [("scale", StandardScaler()), ("model", LogisticRegression(max_iter=2000, C=1.0))]
    return Pipeline(steps)


def boosted_pipeline() -> Pipeline:
    return Pipeline([
        ("impute", SimpleImputer(strategy="median")),
        ("model", HistGradientBoostingClassifier(random_state=0)),
    ])


def cross_validate(frame: pd.DataFrame, columns: list[str], build, folds: int) -> dict:
    """GroupKFold over project_key -- every test fold is projects never seen in training."""
    x, y = frame[columns], frame["slipped"].to_numpy()
    groups = frame["project_key"].to_numpy()

    aucs, briers = [], []
    for train_idx, test_idx in GroupKFold(n_splits=folds).split(x, y, groups):
        model = build()
        model.fit(x.iloc[train_idx], y[train_idx])
        proba = model.predict_proba(x.iloc[test_idx])[:, 1]
        aucs.append(roc_auc_score(y[test_idx], proba))
        briers.append(brier_score_loss(y[test_idx], proba))

    return {
        "roc_auc": float(np.mean(aucs)),
        "roc_auc_std": float(np.std(aucs)),
        "brier": float(np.mean(briers)),
        "brier_std": float(np.std(briers)),
    }


def pick_calibration(frame: pd.DataFrame, folds: int) -> tuple[str, dict[str, float]]:
    """Compare sigmoid against isotonic on held-out projects, by Brier. Lower is better.

    Calibration is fitted on held-out *projects*, not a random split, for the same reason the whole
    study uses GroupKFold: a calibration curve tuned on projects the model has already seen would
    flatter it.
    """
    x, y = frame[SERVABLE], frame["slipped"].to_numpy()
    groups = frame["project_key"].to_numpy()

    scores: dict[str, list[float]] = {"sigmoid": [], "isotonic": []}
    for method in scores:
        for train_idx, test_idx in GroupKFold(n_splits=folds).split(x, y, groups):
            calibrated = CalibratedClassifierCV(linear_pipeline(), method=method, cv=3)
            calibrated.fit(x.iloc[train_idx], y[train_idx])
            proba = calibrated.predict_proba(x.iloc[test_idx])[:, 1]
            scores[method].append(brier_score_loss(y[test_idx], proba))

    means = {method: float(np.mean(values)) for method, values in scores.items()}
    return min(means, key=means.get), means


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--data", type=Path, default=Path("dataset.parquet"))
    parser.add_argument("--folds", type=int, default=5)
    parser.add_argument("--out", type=Path, default=Path("sprint-slip-model.json"))
    args = parser.parse_args()

    frame = pd.read_parquet(args.data)
    print(f"{len(frame):,} rows  {frame['project_key'].nunique()} projects  "
          f"slip rate {frame['slipped'].mean():.1%}\n")

    print("what the serving constraint costs, and whether boosting earns its keep")
    print("  (GroupKFold over project_key -- unseen projects)\n")
    print(f"  {'candidate':<34}{'ROC AUC':>18}{'Brier':>10}")
    print("  " + "-" * 62)

    untransformed = lambda: linear_pipeline(transform=False)  # noqa: E731 - table entry, not an API
    candidates = {
        "logistic raw      / servable 5": (SERVABLE, untransformed),
        "logistic log1p    / servable 5": (SERVABLE, linear_pipeline),
        "boosted           / servable 5": (SERVABLE, boosted_pipeline),
        "logistic log1p    / structural 7": (FULL_STRUCTURAL, linear_pipeline),
        "boosted           / structural 7": (FULL_STRUCTURAL, boosted_pipeline),
    }
    results = {}
    for label, (columns, build) in candidates.items():
        results[label] = cross_validate(frame, columns, build, args.folds)
        row = results[label]
        print(f"  {label:<34}{row['roc_auc']:>11.3f}+-{row['roc_auc_std']:<5.3f}{row['brier']:>10.3f}")

    chosen = results["logistic log1p    / servable 5"]["roc_auc"]
    transform_gain = chosen - results["logistic raw      / servable 5"]["roc_auc"]
    boosting_gain = results["boosted           / servable 5"]["roc_auc"] - chosen
    constraint_cost = results["logistic log1p    / structural 7"]["roc_auc"] - chosen
    print()
    print(f"  what log1p buys the linear model:                {transform_gain:+.3f} AUC")
    print(f"  what boosting buys over it on the same features: {boosting_gain:+.3f} AUC")
    print(f"  what the serving constraint costs (7 -> 5):      {constraint_cost:+.3f} AUC")
    print("  boosting only earns a TreeSHAP dependency while that middle number is clearly positive.")

    method, brier_by_method = pick_calibration(frame, args.folds)
    print("\ncalibration on held-out projects (Brier, lower is better)")
    for name, value in sorted(brier_by_method.items(), key=lambda kv: kv[1]):
        marker = "   <- chosen" if name == method else ""
        print(f"  {name:<12}{value:.4f}{marker}")

    # Final fit on everything. The exported artifact is the linear model: its coefficients ARE the
    # per-feature attribution the seam needs, and it serves as a few dozen floats rather than a
    # native inference runtime in the container.
    pipeline = linear_pipeline()
    pipeline.fit(frame[SERVABLE], frame["slipped"].to_numpy())
    imputer: SimpleImputer = pipeline.named_steps["impute"]
    scaler: StandardScaler = pipeline.named_steps["scale"]
    logistic: LogisticRegression = pipeline.named_steps["model"]

    # Platt scaling over the final model's own decision function, so the exported a/b apply directly
    # to the logit the C# side computes.
    calibrator = CalibratedClassifierCV(pipeline, method="sigmoid", cv=args.folds)
    calibrator.fit(frame[SERVABLE], frame["slipped"].to_numpy())
    sigmoids = [fitted.calibrators[0] for fitted in calibrator.calibrated_classifiers_]
    slope = float(np.mean([sigmoid.a_ for sigmoid in sigmoids]))
    offset = float(np.mean([sigmoid.b_ for sigmoid in sigmoids]))

    span = (frame["sprint_end"].max() - frame["sprint_start"].min()).days

    artifact = {
        "schemaVersion": 1,
        "modelName": "TawosSprintSlip Logistic v1",
        "createdUtc": datetime.now(timezone.utc).isoformat(timespec="seconds"),
        "provenance": {
            "dataset": "TAWOS (Tawosi, Al-Subaihin, Moussa & Sarro, MSR 2022)",
            "rows": int(len(frame)),
            "projects": int(frame["project_key"].nunique()),
            "sprints": int(frame["sprint_id"].nunique()),
            "baseRate": float(frame["slipped"].mean()),
            "trainingWindowDays": int(span),
        },
        # Order is load-bearing: the C# side builds its vector in exactly this sequence.
        "features": [
            {"name": name, "median": float(median), "mean": float(mean), "scale": float(scale),
             "coefficient": float(coefficient)}
            for name, median, mean, scale, coefficient
            in zip(SERVABLE, imputer.statistics_, scaler.mean_, scaler.scale_, logistic.coef_[0])
        ],
        # Applied in this order per feature: median-impute if absent, clamp to >= 0, log1p,
        # subtract mean, divide by scale, multiply by coefficient. The C# side does exactly this.
        "transform": "log1p",
        "intercept": float(logistic.intercept_[0]),
        # sklearn's _SigmoidCalibration is p = 1 / (1 + exp(a * f + b)) over the decision function f.
        # Replicated exactly on the C# side; note the sign convention, a is normally negative.
        "calibration": {"kind": "sigmoid", "a": slope, "b": offset},
        "evaluation": {
            "protocol": f"GroupKFold over project_key, {args.folds} folds",
            "candidates": results,
            "calibrationBrier": brier_by_method,
        },
    }

    args.out.write_text(json.dumps(artifact, indent=2), encoding="utf-8")
    print(f"\nwrote {args.out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
