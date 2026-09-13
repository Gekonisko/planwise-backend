"""Import a slice of the JIRA export into PlanWise as a real project.

Purpose is demo and pipeline-exercise data: a backlog of genuine issue text, so the cost estimate,
backlog prioritisation and risk forecast run against something that reads like real work instead of
"test1 / tewst2 / fgsdfsd".

What is real and what is not:

  REAL       title (Summary), description (Description), priority (mapped from JIRA's), and the
             relative age/ordering of the issues.
  SYNTHETIC  due dates, story points, and sprint assignment. The export has 49 due dates and 49
             story-point values across 49,000 issues, so there is nothing to import; these are
             generated so the app's scheduling and risk features have something to work with.

The synthetic fields are deterministic given --seed, and the created project's description says so,
so imported data can never be mistaken for measured data.
"""

from __future__ import annotations

import argparse
import datetime as dt
import json
import random
import sys
import urllib.error
import urllib.request
from pathlib import Path

import pandas as pd

# JIRA priority -> PlanWise TaskPriority. PlanWise has no "Lowest".
PRIORITY_MAP = {
    "Highest": "Urgent",
    "High": "High",
    "Medium": "Medium",
    "Low": "Low",
    "Lowest": "Low",
}

# Story points are not in the export. Rather than inventing a number per task, issues are bucketed
# onto a Fibonacci scale by description length, which at least correlates with how much was written
# about them. Stated as synthetic in the project description.
POINT_BUCKETS = [(200, 1), (600, 2), (1200, 3), (2500, 5), (6000, 8)]


class ApiError(RuntimeError):
    def __init__(self, status: int, message: str):
        super().__init__(message)
        self.status = status


class PlanWise:
    def __init__(self, base_url: str):
        self.base = base_url.rstrip("/")
        self.token: str | None = None

    def call(self, method: str, path: str, body: dict | None = None) -> dict | list | None:
        data = json.dumps(body).encode() if body is not None else None
        request = urllib.request.Request(f"{self.base}{path}", data=data, method=method)
        request.add_header("Content-Type", "application/json")
        if self.token:
            request.add_header("Authorization", f"Bearer {self.token}")
        try:
            with urllib.request.urlopen(request) as response:
                payload = response.read()
                return json.loads(payload) if payload else None
        except urllib.error.HTTPError as error:
            detail = error.read().decode("utf-8", errors="replace")
            raise ApiError(error.code, f"{method} {path} -> {error.code}: {detail}") from error


def points_for(description_length: int) -> int:
    for limit, points in POINT_BUCKETS:
        if description_length <= limit:
            return points
    return 13


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--data", type=Path, default=Path("dataset.parquet"))
    parser.add_argument("--api", default="http://localhost:5000/api/v1")
    parser.add_argument("--email", required=True)
    parser.add_argument("--password", default="Passw0rd!x")
    parser.add_argument("--register", action="store_true", help="create the account instead of logging in")
    parser.add_argument("--count", type=int, default=60, help="issues to import")
    parser.add_argument("--project-name", default="Sourcetree (imported sample)")
    parser.add_argument("--seed", type=int, default=0)
    args = parser.parse_args()

    rng = random.Random(args.seed)
    api = PlanWise(args.api)

    if args.register:
        auth = api.call("POST", "/auth/register", {
            "email": args.email, "password": args.password,
            "firstName": "Imported", "lastName": "Sample"})
    else:
        auth = api.call("POST", "/auth/login", {"email": args.email, "password": args.password})
    api.token = auth["accessToken"]

    frame = pd.read_parquet(args.data)
    # Only issues with enough text to be worth reading in the UI.
    frame = frame[frame["description"].str.len() > 80].copy()
    # Public trackers are full of the same bug filed over and over -- the newest 60 issues in this
    # export are almost all "Authentication failed when attempting Fetch command". Without this the
    # imported backlog is 60 identical cards, which is useless as demo data and tells the
    # prioritisation and cost models nothing.
    frame["_summary_key"] = frame["summary"].str.strip().str.lower()
    frame = frame.drop_duplicates(subset="_summary_key", keep="first")
    sample = frame.sort_values("created", ascending=False).head(args.count)
    print(f"candidates after dedupe: {len(frame):,}   importing {len(sample)}")

    # The key prefix must be unique across the whole workspace and is not freed when a project is
    # archived, so it is drawn from a system-seeded RNG (not --seed, which controls the synthetic
    # data and must stay reproducible) and retried on collision.
    prefix_rng = random.SystemRandom()
    project = None
    for _ in range(10):
        try:
            project = api.call("POST", "/projects", {
                "name": args.project_name,
                "keyPrefix": f"ST{prefix_rng.randint(100, 999)}",
                "process": "scrum",
                "clientName": "Atlassian (public data)",
            })
            break
        except ApiError as error:
            if error.status != 409:
                raise
    if project is None:
        raise SystemExit("could not find a free project key prefix after 10 attempts")
    project_id = project["id"]
    print(f"project {project_id}  {args.project_name}")

    sprint = api.call("POST", f"/projects/{project_id}/sprints", {
        "name": "Sprint 1",
        "goal": "Imported sample from the public Sourcetree tracker",
        "startDate": str(dt.date.today()),
        "endDate": str(dt.date.today() + dt.timedelta(days=14)),
    })
    sprint_id = sprint["id"]

    today = dt.date.today()
    created = 0
    for position, (_, issue) in enumerate(sample.iterrows()):
        # Due dates are spread across the next six weeks so the risk forecast and Gantt have a range
        # to work with, and roughly a fifth land in the past so the outcome-labelling path is
        # exercised rather than leaving every snapshot Pending.
        offset = rng.randint(-10, 32)
        body = {
            "title": str(issue["summary"])[:300],
            "description": str(issue["description"])[:5000],
            "priority": PRIORITY_MAP.get(str(issue["priority"]), "Medium"),
            "points": points_for(int(issue["description_len"])),
            "dueDate": str(today + dt.timedelta(days=offset)),
        }
        task = api.call("POST", f"/projects/{project_id}/tasks", body)

        # First third goes into the sprint; assigning a sprint also promotes the task onto the board.
        if position < args.count // 3:
            api.call("PATCH", f"/tasks/{task['id']}", {"sprintId": sprint_id})
        created += 1

    api.call("POST", f"/sprints/{sprint_id}/start", {})

    api.call("PATCH", f"/projects/{project_id}", {
        "name": args.project_name,
        "clientName": "Atlassian (public data) - due dates, points and sprint are SYNTHETIC",
    })

    print(f"imported {created} issues; {args.count // 3} placed in Sprint 1 (started)")
    print(f"open: http://localhost:4200/projects/{project_id}/backlog")
    return 0


if __name__ == "__main__":
    sys.exit(main())
