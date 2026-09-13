"""Sprint-slip prediction on TAWOS.

Target: will an issue committed to a sprint fail to be resolved by that sprint's end date?
The base rate is 41.2%, so unlike the Sourcetree study this is a balanced problem and the usual
threshold metrics mean something.

Two evaluation protocols, because they answer different questions and a model can pass one and fail
the other:

  * BY PROJECT (GroupKFold over project_key) -- can it score a project it has never seen? This is
    the question PlanWise actually faces, since every new customer is an unseen project. It is also
    the check the Sourcetree study could not run at all, having only one project.
  * BY TIME (earlier sprints train, later sprints test) -- does it hold up going forward, rather
    than only across a random slice of history?

`project_key` is deliberately *not* a feature. It is the grouping variable, and including it would
let the model memorise each project's own slip rate -- which would look like skill on a random split
and collapse on any project it had not seen.

Excluded as leakage, all of them outcome columns rather than inputs: Resolution_Date,
Resolution_Time_Minutes, Total_Effort_Minutes, In_Progress_Minutes, Timespent, Status, Resolution.
build_dataset.py never emits them; this file never sees them.
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import numpy as np
import pandas as pd
from scipy import sparse
from sklearn.compose import ColumnTransformer
from sklearn.dummy import DummyClassifier
from sklearn.ensemble import HistGradientBoostingClassifier
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.impute import SimpleImputer
from sklearn.linear_model import LogisticRegression
from sklearn.metrics import (
    accuracy_score,
    average_precision_score,
    brier_score_loss,
    f1_score,
    precision_score,
    recall_score,
    roc_auc_score,
)
from sklearn.model_selection import GroupKFold
from sklearn.pipeline import Pipeline
from sklearn.preprocessing import OneHotEncoder, StandardScaler

CATEGORICAL = ["issue_type", "priority"]
# Structural features carry no project-specific vocabulary: a blocking link means the same thing in
# every tracker, whereas "Major" exists in some projects' priority schemes and not others. Split out
# as its own feature set to test whether that is what limits cross-project transfer.
STRUCTURAL = [
    "story_point", "sprint_length_days", "days_in_backlog", "sprint_issue_count",
    "sprint_story_points", "blocking_links", "assoc_links",
]
NUMERIC = [
    "story_point", "sprint_length_days", "days_in_backlog", "sprint_issue_count",
    "sprint_story_points", "blocking_links", "assoc_links", "title_len",
    "description_len", "is_assigned",
]
TFIDF_MAX_FEATURES = 30_000


def load(path: Path) -> pd.DataFrame:
    frame = pd.read_parquet(path)
    frame["text"] = (frame["title"].fillna("") + "\n" + frame["description"].fillna("")).str.strip()
    return frame


def build_features(kind: str) -> ColumnTransformer:
    blocks = []
    if kind == "structural":
        return ColumnTransformer([("num", Pipeline([
            ("impute", SimpleImputer(strategy="median", add_indicator=True)),
            ("scale", StandardScaler()),
        ]), STRUCTURAL)], remainder="drop", sparse_threshold=1.0)
    if kind in {"metadata", "combined"}:
        blocks.append(("cat", OneHotEncoder(handle_unknown="ignore", min_frequency=10), CATEGORICAL))
        # story_point is missing for some rows even in the filtered set; median-impute rather than
        # drop, and let the flag column tell the model when it was imputed.
        blocks.append(("num", Pipeline([
            ("impute", SimpleImputer(strategy="median", add_indicator=True)),
            ("scale", StandardScaler()),
        ]), NUMERIC))
    if kind in {"text", "combined"}:
        blocks.append(("txt", TfidfVectorizer(
            max_features=TFIDF_MAX_FEATURES, ngram_range=(1, 2), min_df=3,
            strip_accents="unicode", sublinear_tf=True), "text"))
    return ColumnTransformer(blocks, remainder="drop", sparse_threshold=1.0)


def metrics(y_true: np.ndarray, proba: np.ndarray) -> dict:
    pred = (proba >= 0.5).astype(int)
    return {
        "roc_auc": roc_auc_score(y_true, proba),
        "pr_auc": average_precision_score(y_true, proba),
        # Brier judges whether the probabilities are honest, not just well ordered -- the thing that
        # matters if a number like this is ever shown to someone as "68% likely to slip".
        "brier": brier_score_loss(y_true, proba),
        "accuracy": accuracy_score(y_true, pred),
        "precision": precision_score(y_true, pred, zero_division=0),
        "recall": recall_score(y_true, pred, zero_division=0),
        "f1": f1_score(y_true, pred, zero_division=0),
    }


def fit_predict(kind: str, train: pd.DataFrame, test: pd.DataFrame, y_train: np.ndarray) -> np.ndarray:
    if kind == "baseline":
        return DummyClassifier(strategy="prior").fit(train, y_train).predict_proba(test)[:, 1]

    features = build_features(kind)
    x_train = features.fit_transform(train)
    x_test = features.transform(test)

    # Trees on the dense numeric/categorical blocks, a linear model on the sparse text matrix.
    if kind in {"metadata", "structural"}:
        x_train = x_train.toarray() if sparse.issparse(x_train) else x_train
        x_test = x_test.toarray() if sparse.issparse(x_test) else x_test
        model = HistGradientBoostingClassifier(random_state=0)
    else:
        model = LogisticRegression(max_iter=3000, C=1.0)

    model.fit(x_train, y_train)
    return model.predict_proba(x_test)[:, 1]


def evaluate_by_project(frame: pd.DataFrame, folds: int) -> dict[str, dict]:
    """GroupKFold over project_key: every test fold is made of projects never seen in training."""
    y = frame["slipped"].to_numpy()
    groups = frame["project_key"].to_numpy()
    splitter = GroupKFold(n_splits=folds)

    per_kind: dict[str, list[dict]] = {}
    for train_idx, test_idx in splitter.split(frame, y, groups):
        train, test = frame.iloc[train_idx], frame.iloc[test_idx]
        for kind in KINDS:
            proba = fit_predict(kind, train, test, y[train_idx])
            per_kind.setdefault(kind, []).append(metrics(y[test_idx], proba))

    summary = {}
    for kind, fold_metrics in per_kind.items():
        summary[kind] = {
            key: {"mean": float(np.mean([m[key] for m in fold_metrics])),
                  "std": float(np.std([m[key] for m in fold_metrics]))}
            for key in fold_metrics[0]
        }
    return summary


def evaluate_by_time(frame: pd.DataFrame, test_fraction: float) -> dict[str, dict]:
    ordered = frame.sort_values("sprint_start").reset_index(drop=True)
    cut = int(len(ordered) * (1 - test_fraction))
    train, test = ordered.iloc[:cut], ordered.iloc[cut:]
    y_train = train["slipped"].to_numpy()
    y_test = test["slipped"].to_numpy()

    print(f"  train {len(train):,} issues to {train['sprint_start'].max():%Y-%m-%d}  "
          f"(slip {y_train.mean():.1%})")
    print(f"  test  {len(test):,} issues from {test['sprint_start'].min():%Y-%m-%d}  "
          f"(slip {y_test.mean():.1%})")

    return {kind: metrics(y_test, fit_predict(kind, train, test, y_train)) for kind in KINDS}


KINDS = ("baseline", "structural", "metadata", "text", "combined")

COLUMNS = [("ROC AUC", "roc_auc"), ("PR AUC", "pr_auc"), ("Brier", "brier"),
           ("accuracy", "accuracy"), ("precision", "precision"), ("recall", "recall"), ("F1", "f1")]


def print_grouped(title: str, summary: dict[str, dict]) -> None:
    print(f"\n{title}")
    header = f"  {'features':<10}" + "".join(f"{label:>17}" for label, _ in COLUMNS)
    print(header)
    print("  " + "-" * (len(header) - 2))
    for kind in KINDS:
        row = summary[kind]
        print(f"  {kind:<10}" + "".join(
            f"{row[key]['mean']:>11.3f}±{row[key]['std']:<5.3f}" for _, key in COLUMNS))


def print_flat(title: str, results: dict[str, dict]) -> None:
    print(f"\n{title}")
    header = f"  {'features':<10}" + "".join(f"{label:>12}" for label, _ in COLUMNS)
    print(header)
    print("  " + "-" * (len(header) - 2))
    for kind in KINDS:
        print(f"  {kind:<10}" + "".join(f"{results[kind][key]:>12.3f}" for _, key in COLUMNS))


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--data", type=Path, default=Path("dataset.parquet"))
    parser.add_argument("--folds", type=int, default=5)
    parser.add_argument("--test-fraction", type=float, default=0.2)
    parser.add_argument("--out", type=Path, default=Path("results.json"))
    args = parser.parse_args()

    frame = load(args.data)
    print(f"{len(frame):,} issues  {frame['project_key'].nunique()} projects  "
          f"{frame['sprint_id'].nunique():,} sprints   slip rate {frame['slipped'].mean():.1%}")

    print(f"\nprotocol 1: unseen projects (GroupKFold, {args.folds} folds over project_key)")
    by_project = evaluate_by_project(frame, args.folds)

    print("\nprotocol 2: forward in time")
    by_time = evaluate_by_time(frame, args.test_fraction)

    print_grouped(f"UNSEEN PROJECTS - mean ± std over {args.folds} folds", by_project)
    print_flat("FORWARD IN TIME - held-out later sprints", by_time)

    args.out.write_text(json.dumps(
        {"rows": len(frame), "projects": int(frame["project_key"].nunique()),
         "slip_rate": float(frame["slipped"].mean()),
         "by_project": by_project, "by_time": by_time}, indent=2), encoding="utf-8")
    print(f"\nwrote {args.out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
