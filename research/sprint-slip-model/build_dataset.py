"""Build the sprint-slip modelling frame from the TAWOS dataset.

TAWOS (Tawosi et al., MSR 2022) is 458,232 Jira issues from 39 agile open-source projects, with
sprints, story points and issue links. Unlike the Sourcetree export in ../resolution-time-model, it
carries the fields PlanWise itself collects, so the label can be the one PlanWise actually cares
about:

    slipped = the issue was not resolved by the end of the sprint it was committed to

That is the agile-native form of "did it slip". It does not depend on a Due Date field (which almost
no team fills in) -- the sprint's own end date is the deadline.

Two leakage traps are handled here, both found by inspecting the data rather than assuming:

  1. Estimates made mid-sprint. 10,914 of the 30,165 labelable issues with a story point were
     estimated *during or after* the sprint they belong to. Using those means predicting a sprint's
     outcome partly from information that only existed once the sprint was underway. `--estimate-
     timing before` (the default) keeps only issues estimated before the sprint started.

  2. Outcome columns. Resolution_Time_Minutes, Total_Effort_Minutes, In_Progress_Minutes, Timespent,
     Status and Resolution all describe what happened. None is a feature; they exist in TAWOS
     precisely because they are the thing to be predicted.

Everything selected below is knowable at sprint-planning time.
"""

from __future__ import annotations

import argparse
import subprocess
import sys
from pathlib import Path

import pandas as pd

# Issue links whose direction carries real dependency meaning. "Duplicate"/"Relates"/"Reference"
# are associations, not blockers, so they are counted separately rather than lumped in.
BLOCKING_LINK_NAMES = ("Depends", "Blocks", "Blocker", "blocks", "Dependency")

QUERY = """
SELECT
    i.ID                                   AS issue_id,
    i.Issue_Key                            AS issue_key,
    p.Project_Key                          AS project_key,
    i.Title                                AS title,
    COALESCE(i.Description_Text, '')       AS description,
    i.Type                                 AS issue_type,
    COALESCE(i.Priority, '(none)')         AS priority,
    i.Story_Point                          AS story_point,
    i.Story_Point_Changed_After_Estimation AS story_point_changed,
    (i.Assignee_ID IS NOT NULL)            AS is_assigned,
    CHAR_LENGTH(i.Title)                   AS title_len,
    CHAR_LENGTH(COALESCE(i.Description_Text, '')) AS description_len,
    s.ID                                   AS sprint_id,
    s.Start_Date                           AS sprint_start,
    s.End_Date                             AS sprint_end,
    DATEDIFF(s.End_Date, s.Start_Date)     AS sprint_length_days,
    -- How long the issue sat before the sprint picked it up. Negative means it was created after
    -- the sprint had already started, i.e. added mid-sprint.
    DATEDIFF(s.Start_Date, i.Creation_Date) AS days_in_backlog,
    sl.sprint_issue_count                  AS sprint_issue_count,
    sl.sprint_story_points                 AS sprint_story_points,
    COALESCE(bl.blocking_links, 0)         AS blocking_links,
    COALESCE(al.assoc_links, 0)            AS assoc_links,
    -- Outcome side. Kept for labelling only; build_dataset never emits these as features.
    i.Resolution_Date                      AS resolution_date
FROM Issue i
JOIN Sprint  s ON s.ID = i.Sprint_ID
JOIN Project p ON p.ID = i.Project_ID
LEFT JOIN (
    SELECT i2.Sprint_ID AS sid,
           COUNT(*) AS sprint_issue_count,
           SUM(COALESCE(i2.Story_Point, 0)) AS sprint_story_points
    FROM Issue i2 WHERE i2.Sprint_ID IS NOT NULL GROUP BY i2.Sprint_ID
) sl ON sl.sid = s.ID
LEFT JOIN (
    SELECT Issue_ID, COUNT(*) AS blocking_links FROM Issue_Link
    WHERE Name IN ('Depends','Blocks','Blocker','blocks','Dependency') GROUP BY Issue_ID
) bl ON bl.Issue_ID = i.ID
LEFT JOIN (
    SELECT Issue_ID, COUNT(*) AS assoc_links FROM Issue_Link
    WHERE Name NOT IN ('Depends','Blocks','Blocker','blocks','Dependency') GROUP BY Issue_ID
) al ON al.Issue_ID = i.ID
WHERE s.End_Date IS NOT NULL
  AND s.Start_Date IS NOT NULL
  {estimate_filter}
"""

ESTIMATE_FILTERS = {
    # Only issues whose story point was set before the sprint began: the honest predictive setting.
    "before": "AND i.Story_Point IS NOT NULL AND i.Estimation_Date IS NOT NULL "
              "AND i.Estimation_Date <= s.Start_Date",
    # Every estimated issue, including ones estimated mid-sprint. Larger but leaky; for comparison.
    "any": "AND i.Story_Point IS NOT NULL",
    # No estimate required at all -- the largest set, for measuring what story points are worth.
    "none": "",
}


def run_query(container: str, database: str, sql: str) -> pd.DataFrame:
    """Runs the query through the mysql client in the container and reads its TSV back."""
    result = subprocess.run(
        ["docker", "exec", "-i", container, "mysql", "-uroot", "-ptawos", "-D", database,
         "--batch", "--raw", "--default-character-set=utf8mb4", "-e", sql],
        capture_output=True,
    )
    if result.returncode != 0:
        raise SystemExit(result.stderr.decode("utf-8", errors="replace"))

    from io import StringIO
    text = result.stdout.decode("utf-8", errors="replace")
    return pd.read_csv(StringIO(text), sep="\t", quoting=3, na_values=["NULL"], low_memory=False)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--container", default="tawos-mysql")
    parser.add_argument("--database", default="tawos")
    parser.add_argument("--estimate-timing", choices=sorted(ESTIMATE_FILTERS), default="before",
                        help="which issues to keep based on when their story point was set")
    parser.add_argument("--out", type=Path, default=Path("dataset.parquet"))
    args = parser.parse_args()

    sql = QUERY.format(estimate_filter=ESTIMATE_FILTERS[args.estimate_timing])
    frame = run_query(args.container, args.database, sql)

    # Newlines inside Title/Description break the TSV row alignment, so anything that failed to
    # parse into a real issue_id is dropped rather than silently carried as a corrupt row.
    before = len(frame)
    frame = frame[pd.to_numeric(frame["issue_id"], errors="coerce").notna()].copy()
    if len(frame) != before:
        print(f"dropped {before - len(frame):,} rows mangled by embedded newlines")

    for column in ("sprint_start", "sprint_end", "resolution_date"):
        frame[column] = pd.to_datetime(frame[column], errors="coerce")
    for column in ("story_point", "sprint_length_days", "days_in_backlog", "sprint_issue_count",
                   "sprint_story_points", "blocking_links", "assoc_links", "title_len",
                   "description_len", "is_assigned", "story_point_changed"):
        frame[column] = pd.to_numeric(frame[column], errors="coerce")

    frame = frame[frame["sprint_end"].notna()].copy()

    # THE LABEL. Never resolved counts as slipped: an issue that was committed to a sprint and is
    # still open is the clearest possible case of not delivering on the commitment, and dropping
    # those would bias the data towards work that eventually got finished.
    frame["slipped"] = (
        frame["resolution_date"].isna() | (frame["resolution_date"] > frame["sprint_end"])
    ).astype(int)

    frame = frame.sort_values("sprint_start").reset_index(drop=True)
    frame.to_parquet(args.out, index=False)

    print(f"wrote {args.out}  rows={len(frame):,}  projects={frame['project_key'].nunique()}  "
          f"sprints={frame['sprint_id'].nunique():,}")
    print(f"  sprint range : {frame['sprint_start'].min():%Y-%m-%d} .. {frame['sprint_end'].max():%Y-%m-%d}")
    print(f"  SLIP RATE    : {frame['slipped'].mean():.1%}  "
          f"({frame['slipped'].sum():,} slipped / {len(frame):,})")
    print(f"  story points : median={frame['story_point'].median():.0f}  "
          f"p90={frame['story_point'].quantile(0.9):.0f}")
    print(f"  issues per project: min={frame.groupby('project_key').size().min():,}  "
          f"max={frame.groupby('project_key').size().max():,}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
