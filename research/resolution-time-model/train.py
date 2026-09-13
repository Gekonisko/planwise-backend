"""Resolution-time study on the public Sourcetree-for-Windows JIRA export.

Question: given only what is known when an issue is filed -- its text, type, priority, component and
a few shape features -- can you predict how long it will take to resolve?

Censoring is the whole methodological problem here, and the first version of this script got it
wrong. The export is a snapshot taken on a fixed date, so a recently filed issue can only *appear*
resolved if it resolved quickly. Training on "resolved issues, split by date" therefore compared a
training set that was 20% fast against a test set that was 70% fast, and every metric was measuring
that artefact rather than the model. Two rules fix it:

  1. Observation window. Only issues created at least `--horizon-days` before the export date are
     eligible, so every issue in the study has had the same amount of time to resolve.
  2. Unresolved issues are label 0, not dropped. An issue that never resolved is the strongest
     possible example of "did not resolve within 30 days"; discarding it is survivorship bias.

Together these make the target fully observed: for every eligible issue, "resolved within N days"
is a known fact rather than a guess, for any N up to the horizon.

The target is reported at several thresholds rather than as a regression on days. Regression would
have to either drop the never-resolved issues (bias again) or invent a duration for them, whereas
a threshold question is answerable for every row. It is also the more useful output: P(resolves
within the time available) is the shape a delivery-risk estimate actually needs.

Four feature sets run against each threshold, because the interesting result is not one accuracy
number but *which* information carries the signal:
  baseline -- none (predicts the training base rate)
  metadata -- type, priority, component, votes, text lengths, month, weekday
  text     -- TF-IDF over summary + description
  combined -- both
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
from sklearn.linear_model import LogisticRegression
from sklearn.metrics import average_precision_score, brier_score_loss, roc_auc_score
from sklearn.preprocessing import OneHotEncoder, StandardScaler

CATEGORICAL = ["issue_type", "priority", "component"]
NUMERIC = ["votes", "summary_len", "description_len", "created_month", "created_dow"]
THRESHOLDS = (7, 30, 90, 180)

# Large enough that the vocabulary is not the bottleneck, small enough that the study runs in about
# a minute; raising it moves the headline numbers by less than run-to-run noise.
TFIDF_MAX_FEATURES = 30_000


def load_eligible(path: Path, horizon_days: int) -> pd.DataFrame:
    """Issues old enough that their outcome within `horizon_days` is fully observed."""
    frame = pd.read_parquet(path).sort_values("created").reset_index(drop=True)
    frame["text"] = (frame["summary"].fillna("") + "\n" + frame["description"].fillna("")).str.strip()

    export_date = frame["created"].max()
    cutoff = export_date - pd.Timedelta(days=horizon_days)
    eligible = frame[frame["created"] <= cutoff].copy().reset_index(drop=True)

    print(f"export snapshot   : {export_date:%Y-%m-%d}")
    print(f"observation window: {horizon_days} days  ->  eligible if created <= {cutoff:%Y-%m-%d}")
    print(f"eligible issues   : {len(eligible):,} of {len(frame):,} "
          f"({len(frame) - len(eligible):,} too recent to judge)")
    return eligible


def label_for(frame: pd.DataFrame, threshold_days: int) -> np.ndarray:
    """1 if resolved within the threshold; 0 if it took longer *or never resolved at all*."""
    days = frame["days_to_resolve"]
    return ((days.notna()) & (days <= threshold_days)).astype(int).to_numpy()


def build_features(kind: str) -> ColumnTransformer:
    blocks = []
    if kind in {"metadata", "combined"}:
        blocks.append(("cat", OneHotEncoder(handle_unknown="ignore", min_frequency=5), CATEGORICAL))
        blocks.append(("num", StandardScaler(), NUMERIC))
    if kind in {"text", "combined"}:
        blocks.append((
            "txt",
            TfidfVectorizer(
                max_features=TFIDF_MAX_FEATURES,
                ngram_range=(1, 2),
                min_df=3,
                strip_accents="unicode",
                sublinear_tf=True,
            ),
            "text",
        ))
    return ColumnTransformer(blocks, remainder="drop", sparse_threshold=1.0)


def metrics(y_true: np.ndarray, proba: np.ndarray, top_fraction: float = 0.10) -> dict:
    """
    Reported at the top decile rather than at a fixed 0.5 cut. Only ~5% of issues resolve quickly,
    so almost nothing ever crosses 0.5 and precision/recall there read as 0.000 for every model --
    a property of the threshold, not of the model. "Of the 10% the model ranks most promising, how
    many actually resolved in time" is both measurable and the way a ranking like this gets used.
    """
    base_rate = float(y_true.mean())
    k = max(1, int(len(y_true) * top_fraction))
    # Ties are broken at random rather than by row order. The baseline model assigns every issue the
    # same probability, so without this its "top decile" would be whatever happened to be first in
    # the file, and the row would read as a signal it does not have.
    jitter = np.random.default_rng(0).random(len(proba)) * 1e-12
    top_k = np.argsort(proba + jitter)[::-1][:k]
    precision_at_k = float(y_true[top_k].mean())

    return {
        # AUC and PR-AUC judge the ranking; Brier judges whether the probabilities are honest,
        # which is what matters if a number like this is ever shown to someone as a percentage.
        "roc_auc": roc_auc_score(y_true, proba),
        "pr_auc": average_precision_score(y_true, proba),
        "brier": brier_score_loss(y_true, proba),
        "base_rate": base_rate,
        "precision_at_10pct": precision_at_k,
        # How much better than picking at random. 1.0 means the ranking is worthless.
        "lift_at_10pct": precision_at_k / base_rate if base_rate > 0 else float("nan"),
    }


def fit_and_score(kind: str, train: pd.DataFrame, test: pd.DataFrame,
                  y_train: np.ndarray, y_test: np.ndarray) -> dict:
    features = build_features(kind)
    x_train = features.fit_transform(train)
    x_test = features.transform(test)

    # Trees on dense metadata, a linear model on the sparse text matrix: the usual pairing.
    # Boosting over a 30k-column TF-IDF is both far slower and worse.
    if kind == "metadata":
        x_train = x_train.toarray() if sparse.issparse(x_train) else x_train
        x_test = x_test.toarray() if sparse.issparse(x_test) else x_test
        model = HistGradientBoostingClassifier(random_state=0)
    else:
        model = LogisticRegression(max_iter=2000, C=1.0)

    model.fit(x_train, y_train)
    proba = model.predict_proba(x_test)[:, 1]
    return {"features": kind, "model": type(model).__name__, **metrics(y_test, proba)}


def run_threshold(train: pd.DataFrame, test: pd.DataFrame, threshold: int) -> list[dict]:
    y_train, y_test = label_for(train, threshold), label_for(test, threshold)
    rows = [{
        "features": "baseline",
        "model": "base rate",
        **metrics(y_test, DummyClassifier(strategy="prior")
                  .fit(train, y_train).predict_proba(test)[:, 1]),
    }]
    rows += [fit_and_score(kind, train, test, y_train, y_test) for kind in ("metadata", "text", "combined")]
    return rows


def print_table(title: str, rows: list[dict]) -> None:
    columns = [("ROC AUC", "roc_auc"), ("PR AUC", "pr_auc"), ("Brier", "brier"),
               ("prec@10%", "precision_at_10pct"), ("lift@10%", "lift_at_10pct")]
    print(f"\n{title}")
    header = f"  {'features':<10} {'model':<32}" + "".join(f"{label:>12}" for label, _ in columns)
    print(header)
    print("  " + "-" * (len(header) - 2))
    for row in rows:
        print(f"  {row['features']:<10} {row['model']:<32}"
              + "".join(f"{row[key]:>12.3f}" for _, key in columns))


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--data", type=Path, default=Path("dataset.parquet"))
    parser.add_argument("--horizon-days", type=int, default=365,
                        help="how long every issue must have been observable for")
    parser.add_argument("--test-fraction", type=float, default=0.2)
    parser.add_argument("--out", type=Path, default=Path("results.json"))
    args = parser.parse_args()

    eligible = load_eligible(args.data, args.horizon_days)

    # Split by creation date, not at random: a random split would let the model learn from issues
    # filed after the ones it is tested on, which is both leakage and not how it would be used.
    cut = int(len(eligible) * (1 - args.test_fraction))
    train, test = eligible.iloc[:cut].copy(), eligible.iloc[cut:].copy()
    print(f"train             : {len(train):,}  {train['created'].min():%Y-%m-%d} .. {train['created'].max():%Y-%m-%d}")
    print(f"test              : {len(test):,}  {test['created'].min():%Y-%m-%d} .. {test['created'].max():%Y-%m-%d}")

    print("\nbase rates (train / test) -- these should now be close; a large gap means residual censoring:")
    for threshold in THRESHOLDS:
        print(f"  resolved within {threshold:>3}d : "
              f"{label_for(train, threshold).mean():.1%} / {label_for(test, threshold).mean():.1%}")

    results = {}
    for threshold in THRESHOLDS:
        rows = run_threshold(train, test, threshold)
        results[f"within_{threshold}d"] = rows
        print_table(f"RESOLVED WITHIN {threshold} DAYS", rows)

    args.out.write_text(json.dumps(
        {"horizon_days": args.horizon_days, "train_rows": len(train), "test_rows": len(test),
         "results": results}, indent=2), encoding="utf-8")
    print(f"\nwrote {args.out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
