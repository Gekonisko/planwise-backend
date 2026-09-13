"""Turn the raw JIRA export into a modelling frame for the resolution-time study.

Reads the CSV with the stdlib csv module rather than pandas on purpose: the export has 491 columns
of which 231 are duplicate names (85 `Comment` columns alone), so name-based selection is ambiguous
and a full read costs ~1 GB of RAM for the ~10 columns that are actually used.

Everything written here is observable at issue-creation time. Fields that only exist *because* an
issue was resolved -- Status, Resolution, Resolved, Updated -- are deliberately excluded from the
features and used only to build the target, since including them would leak the answer.
"""

from __future__ import annotations

import argparse
import csv
import datetime as dt
import sys
import zipfile
from pathlib import Path

import pandas as pd

csv.field_size_limit(10**9)

# The export writes every timestamp like "17/Mar/2023 9:20 PM".
_DATE_FORMATS = ("%d/%b/%Y %I:%M %p", "%d/%b/%Y")


def parse_timestamp(value: str) -> dt.datetime | None:
    value = value.strip()
    if not value:
        return None
    for fmt in _DATE_FORMATS:
        try:
            return dt.datetime.strptime(value, fmt)
        except ValueError:
            continue
    return None


def column_indexes(header: list[str], name: str) -> list[int]:
    return [i for i, h in enumerate(header) if h == name]


def first_non_empty(row: list[str], indexes: list[int]) -> str:
    for i in indexes:
        if i < len(row) and row[i].strip():
            return row[i].strip()
    return ""


def open_csv(path: Path):
    """Accepts either the .zip or an already-extracted .csv."""
    if path.suffix.lower() == ".zip":
        archive = zipfile.ZipFile(path)
        name = next(n for n in archive.namelist() if n.lower().endswith(".csv"))
        return archive.open(name, "r"), archive
    return path.open("rb"), None


def build(source: Path) -> pd.DataFrame:
    handle, archive = open_csv(source)
    try:
        text = (line.decode("utf-8", errors="replace") for line in handle)
        reader = csv.reader(text)
        header = next(reader)

        idx = {
            "summary": column_indexes(header, "Summary"),
            "key": column_indexes(header, "Issue key"),
            "issue_type": column_indexes(header, "Issue Type"),
            "priority": column_indexes(header, "Priority"),
            "component": column_indexes(header, "Component/s"),
            "votes": column_indexes(header, "Votes"),
            "description": column_indexes(header, "Description"),
            "created": column_indexes(header, "Created"),
            "resolved": column_indexes(header, "Resolved"),
        }
        missing = [k for k, v in idx.items() if not v]
        if missing:
            raise SystemExit(f"export is missing expected columns: {missing}")

        rows = []
        for row in reader:
            created = parse_timestamp(first_non_empty(row, idx["created"]))
            resolved = parse_timestamp(first_non_empty(row, idx["resolved"]))
            # Unresolved issues are kept, with a null duration. Dropping them here would be a
            # survivorship bias -- the issues that never got resolved are systematically the slow
            # ones, and throwing them away makes every project look faster than it is. train.py
            # turns "unresolved" into a real label using an observation window.
            if created is None:
                continue
            days = None
            if resolved is not None:
                days = (resolved - created).total_seconds() / 86400.0
                if days < 0:
                    days = None

            summary = first_non_empty(row, idx["summary"])
            description = first_non_empty(row, idx["description"])
            votes_raw = first_non_empty(row, idx["votes"])

            rows.append(
                {
                    "key": first_non_empty(row, idx["key"]),
                    "created": created,
                    "summary": summary,
                    "description": description,
                    "issue_type": first_non_empty(row, idx["issue_type"]) or "(none)",
                    "priority": first_non_empty(row, idx["priority"]) or "(none)",
                    "component": first_non_empty(row, idx["component"]) or "(none)",
                    "votes": int(votes_raw) if votes_raw.isdigit() else 0,
                    "summary_len": len(summary),
                    "description_len": len(description),
                    "created_month": created.month,
                    "created_dow": created.weekday(),
                    "resolved": resolved,
                    "days_to_resolve": days,
                }
            )
    finally:
        handle.close()
        if archive is not None:
            archive.close()

    return pd.DataFrame(rows).sort_values("created").reset_index(drop=True)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path, help="GFG_FINAL.csv or GFG_FINAL.csv.zip")
    parser.add_argument("--out", type=Path, default=Path("dataset.parquet"))
    args = parser.parse_args()

    frame = build(args.source)
    frame.to_parquet(args.out, index=False)

    print(f"wrote {args.out}  rows={len(frame):,}  columns={len(frame.columns)}")
    print(f"  created range : {frame['created'].min():%Y-%m-%d} .. {frame['created'].max():%Y-%m-%d}")
    resolved = frame["days_to_resolve"].notna()
    print(f"  resolved: {resolved.sum():,} ({resolved.mean():.1%})   unresolved kept: {(~resolved).sum():,}")
    print(f"  days_to_resolve (resolved only) median={frame.loc[resolved, 'days_to_resolve'].median():.1f} "
          f"p90={frame.loc[resolved, 'days_to_resolve'].quantile(0.9):.0f}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
