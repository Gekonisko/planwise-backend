# PlanWise Backend — Implemented API Reference

This documents what is actually implemented in the backend today, as opposed to `PlanWise API.pdf` at the repo root, which is the full target spec (10 sections). Sections 1–5 exist (section 5's Gantt/milestones half included), plus section 6 (Cost estimation), section 9 (Schedule optimisation), and the `GET /jobs/{id}` half of section 10; sections 7–8 (risk prediction, backlog prioritisation) and the rest of section 10 (notifications, search, preferences, SignalR hub) have no backend code yet.

**Section 6 needs a real secret to run**: `POST /projects/{id}/cost-estimates/run` calls the Anthropic API and requires `CostEstimation:Anthropic:ApiKey` set via `dotnet user-secrets set "CostEstimation:Anthropic:ApiKey" "sk-ant-..." --project src/API/PlanWise.API`, then a container restart (`docker compose up -d --build planwise.api`) to pick it up. Without it, runs fail cleanly (`GET /jobs/{id}` shows `Failed` with the real Anthropic error) rather than crashing the app.

Live, interactive docs (Scalar/OpenAPI) are also available at `/scalar/v1` when the API runs in the `Development` environment.

## Conventions

- Base path: `/api/v1`
- Auth: JWT bearer access token in the `Authorization: Bearer <token>` header, except `POST /auth/register`, `POST /auth/login`, `POST /auth/refresh`, `POST /auth/password/forgot`, `POST /auth/password/reset`, which are anonymous. Every other endpoint below requires the header.
- Refresh token: returned via an `HttpOnly`, `Secure`, `SameSite=Strict` cookie named `planwise_refresh_token`, scoped to path `/api/v1/auth`. It is never present in a JSON response body. **Because it's `Secure`, the browser (and any spec-compliant HTTP client, including .NET's `CookieContainer`) will not store or send it over plain HTTP** — `POST /auth/refresh` and `POST /auth/logout` only work against the HTTPS port (`5001` in the local Docker Compose setup, not `5000`). Testing over HTTP silently fails with `400 Auth.InvalidRefreshToken` — the cookie was simply never sent, which is easy to mistake for the token itself being wrong.
- Errors: RFC 7807 problem details (`title`, `detail`, `type`, `status`), produced by `PlanWise.Common.Presentation.Results.ApiResults.Problem`. Validation failures additionally carry an `errors` extension (field → messages). Status codes: `400` (validation/business-rule problem), `401` (unauthorized), `404` (not found — also used for authorization failures on scoped resources, so as not to reveal existence to non-members), `409` (conflict). A request body that fails to deserialize at all (wrong JSON shape, an empty string where a `DateOnly`/`Guid`/enum is required, etc.) never reaches a handler or FluentValidation. **In Production only**, this is caught by a global exception handler (`Program.cs` → `UseProblemDetailsExceptionHandler`, gated on `app.Environment.IsProduction()`) that returns the same problem-details shape with `title: "Request.InvalidBody"` and `status: 400`, without leaking the underlying exception/stack trace. In Development (including the local `docker-compose.override.yml` setup, which sets `ASPNETCORE_ENVIRONMENT=Development`), the handler is skipped on purpose so the full exception detail — parameter name, JSON path, converter — comes through instead, since that detail is useful when debugging locally and the safety concern doesn't apply outside Production.
- IDs are GUIDs. Enum fields (`status`, `priority`, `state`, `type`) currently serialize as their **integer** ordinal (no `JsonStringEnumConverter` is registered) — see the per-field enum tables below for the mapping.
- Success bodies are plain JSON (not wrapped); mutations that don't return a resource respond `204 No Content`.

---

## 1. Authentication

Module: `IdentityAccess`. Not wired to any external email sender — `password/forgot`/`password/reset` are functional against the database (reset tokens are created and consumed) but no email is actually sent.

### `POST /auth/register`
Anonymous. Creates a user (seeded with the `User` role) and immediately logs them in.

Request:
```json
{ "email": "a@b.com", "firstName": "Ada", "lastName": "Lovelace", "password": "Passw0rd!" }
```
Response `200`: same shape as login (below).

### `POST /auth/login`
Anonymous.

Request:
```json
{ "email": "a@b.com", "password": "Passw0rd!", "rememberMe": false }
```
`rememberMe` controls the refresh token cookie's lifetime (longer-lived when `true`).

Response `200`:
```json
{
  "userId": "guid", "email": "a@b.com", "firstName": "Ada", "lastName": "Lovelace",
  "roles": ["User"], "accessToken": "eyJ...", "accessTokenExpiresAtUtc": "2026-08-22T12:00:00Z"
}
```
Sets the `planwise_refresh_token` cookie. Failure: `401` on bad credentials.

### `POST /auth/refresh`
Anonymous, reads the refresh cookie (not a body param). Rotates the refresh token and issues a new access token. Response shape identical to login. `401` if the cookie is missing/expired/already-rotated.

### `POST /auth/logout`
Reads and revokes the refresh cookie, then deletes it client-side. Always `204`, even if no cookie was present.

### `POST /auth/password/forgot`
Anonymous.
```json
{ "email": "a@b.com" }
```
Always `202 Accepted`, regardless of whether the email exists — prevents account enumeration.

### `POST /auth/password/reset`
Anonymous.
```json
{ "token": "the-reset-token", "password": "NewPassw0rd!" }
```
`204` on success, `400` if the token is invalid/expired.

### `GET /auth/me`
Auth required.
```json
{ "id": "guid", "email": "a@b.com", "firstName": "Ada", "lastName": "Lovelace", "roles": ["User"], "permissions": ["..."] }
```

---

## 2. Projects and workspace

Module: `WorkspaceManagement` (Postgres schema `workspace_management`).

Membership model: a `ProjectMember` can exist with `userId = null`, keyed only by email (a pending invite). Such a member gets read access immediately (matched by email in every access check) and is automatically linked to the real `userId` the next time that email's owner calls `GET /projects` — see `Project.ClaimMembership`, invoked as a side effect of `GetProjectsQueryHandler`.

### `GET /projects`
Returns every project the caller owns or is a member of (including pending-by-email).
```json
[{ "id": "guid", "name": "Apollo", "keyPrefix": "APL", "process": "scrum", "clientName": null,
   "status": "Active", "ownerId": "guid", "memberCount": 3, "labelCount": 0 }]
```
`status` is a string here (`Active`/`OnHold`/`Completed`/`Archived`) — WorkspaceManagement, unlike Delivery, converts its enums to strings before returning them.

### `POST /projects`
Creates a project; the caller becomes `Owner`.
```json
{ "name": "Apollo", "keyPrefix": "APL", "process": "scrum", "clientName": null }
```
`keyPrefix` must match `^[A-Z][A-Z0-9]{1,9}$` and be globally unique (`409` otherwise). `process` must be `scrum` or `kanban`. Response `200`: `ProjectResponse` (shape above).

### `GET /projects/{id}`
`ProjectResponse`. `404` if the caller has no access.

### `PATCH /projects/{id}`
```json
{ "name": null, "process": null, "clientName": null, "status": "OnHold" }
```
Only non-null fields are applied — **a field already set can't be cleared back to null this way** (a known simplification shared by every PATCH endpoint in this codebase, not just this one).

### `DELETE /projects/{id}`
Archives (does not hard-delete) the project — sets `status = Archived`. `204`.

### `GET /projects/{id}/members`
```json
[{ "id": "guid", "userId": "guid|null", "email": "a@b.com", "role": "Owner", "capacity": 1.0, "hourlyRate": 0 }]
```

### `POST /projects/{id}/members`
```json
{ "userId": null, "email": "new@b.com", "role": "Developer", "capacity": 1.0, "hourlyRate": 50 }
```
`userId` is optional — omit it to invite by email (pending member, claimed on that person's first `GET /projects` after registering/logging in). `409` if already a member or already invited.

### `PATCH /projects/{id}/members/{memberId}`
```json
{ "role": "Lead", "capacity": 0.5, "hourlyRate": 60 }
```
All three fields are required (this one is a full replace, not a partial patch, unlike the project/task PATCH endpoints).

### `DELETE /projects/{id}/members/{memberId}`
`204`.

### `GET /projects/{id}/labels`
```json
[{ "id": "guid", "name": "bug", "color": "#ff0000" }]
```
**Known gap**: there is no `POST` to create a label. `Project.AddLabel()` exists in the domain but nothing calls it — this list is currently always empty for every project.

---

## 3. Sprints

Module: `Delivery` (Postgres schema `delivery`). Lifecycle: `Planned(0) → Active(1) → Completed(2)`, one active sprint per project at a time.

### `GET /projects/{projectId}/sprints`
```json
[{ "id": "guid", "projectId": "guid", "name": "Sprint 1", "goal": null,
   "startDate": "2026-08-24", "endDate": "2026-09-07", "state": 0,
   "committedPoints": 0, "completedPoints": 0 }]
```
`committedPoints`/`completedPoints` are **always 0** — not wired to task data yet.

`state`: `0` = Planned, `1` = Active, `2` = Completed.

### `POST /projects/{projectId}/sprints`
```json
{ "name": "Sprint 1", "goal": "Ship auth", "startDate": "2026-08-24", "endDate": "2026-09-07" }
```
Created in `Planned` state.

### `GET /sprints/{id}` / `PATCH /sprints/{id}`
Same `SprintResponse` shape. PATCH body: `{ "name": null, "goal": null, "startDate": null, "endDate": null }`, partial-update semantics (non-null wins).

### `POST /sprints/{id}/start`
`409` if the project already has an active sprint; `400` if this sprint isn't `Planned`.

### `POST /sprints/{id}/complete`
`400` if this sprint isn't `Active`.

**Not implemented**: `GET /sprints/{id}/burndown`, `GET /projects/{id}/velocity` (need real point aggregation across tasks).

---

## 4. Tasks, board and backlog

Module: `Delivery`. The domain entity is called `ProjectTask` internally (not `Task`, to avoid colliding with `System.Threading.Tasks.Task`) — this is purely an internal naming detail and doesn't affect the API surface.

`status`: `0` Backlog, `1` Todo, `2` InProgress, `3` Done. `priority` (request body only, string): `Low`/`Medium`/`High`/`Urgent`, case-insensitive. Task keys look like `APL-1`, `APL-2`, ... generated per-project.

### `GET /projects/{projectId}/tasks?sprintId=&status=&assigneeId=&label=&q=`
All filters optional and combinable. `status` is the string name (`"Todo"` etc, parsed case-insensitively) even though the response serializes it as an int. `label` is a label GUID. `q` does a case-insensitive substring match against title/description (`EF.Functions.ILike`).
```json
[{ "id": "guid", "projectId": "guid", "sprintId": null, "key": "APL-1", "title": "...", "description": null,
   "status": 0, "priority": 1, "points": 5, "assigneeId": null, "dueDate": null, "rank": 1024.0,
   "businessValue": null, "labelIds": [], "subtasks": [], "links": [] }]
```
Note comments are **not** included here or on `GET /tasks/{id}` — fetch them separately.

### `GET /projects/{projectId}/board`
Convenience Kanban shape — three fixed columns (Todo/InProgress/Done; Backlog is excluded, reachable via the filter above instead).
```json
{ "columns": [
  { "status": 1, "wipLimit": null, "pointTotal": 5, "tasks": [ /* TaskResponse[] */ ] },
  { "status": 2, "wipLimit": null, "pointTotal": 0, "tasks": [] },
  { "status": 3, "wipLimit": null, "pointTotal": 0, "tasks": [] }
]}
```
`wipLimit` is always `null` — not configurable yet.

### `POST /projects/{projectId}/tasks`
```json
{ "title": "Design schema", "description": null, "priority": "High", "points": 5,
  "assigneeId": null, "dueDate": null, "businessValue": null, "labelIds": [] }
```
Created into `Backlog` status, ranked after every other backlog item.

### `GET /tasks/{id}`
Full `TaskResponse` including `subtasks` and `links` (still no `comments`).

### `PATCH /tasks/{id}`
```json
{ "title": null, "description": null, "priority": null, "points": null,
  "assigneeId": null, "dueDate": null, "sprintId": null, "labelIds": null }
```
Partial-update (non-null wins), same "can't clear back to null" caveat as `PATCH /projects/{id}` — **except** `labelIds`, where an explicit `[]` does clear all labels (a list can distinguish "untouched" from "emptied" where a scalar can't). `sprintId` here is how a backlog item gets scheduled into a sprint — there's no separate endpoint for it.

### `DELETE /tasks/{id}`
Hard delete (unlike projects, which archive). `204`.

### `POST /tasks/{id}/move`
```json
{ "status": "InProgress", "index": 0 }
```
Drag-and-drop: sets both status and rank atomically. `index` is the desired 0-based position within the target column; rank is computed as the midpoint between the tasks that end up adjacent (gap of 1024 at the ends), so a single move never touches any other row.

### `POST /projects/{projectId}/tasks/reorder`
```json
{ "taskIds": ["guid1", "guid2", "guid3"] }
```
Bulk-reassigns sequential ranks (`1024, 2048, 3072, ...`) in the given order, across any statuses. `400` if any ID doesn't belong to the project or the set is incomplete relative to what was passed.

### `PUT /tasks/{id}/business-value`
```json
{ "businessValue": 80 }
```
0–100 inclusive.

### `POST /tasks/{id}/subtasks`
```json
{ "title": "Draft ERD" }
```
Response: `{ "id": "guid", "title": "Draft ERD", "isDone": false }`.

### `PATCH /tasks/{id}/subtasks/{subId}`
```json
{ "title": null, "isDone": true }
```

### `DELETE /tasks/{id}/subtasks/{subId}`
`204`.

### `GET /tasks/{id}/comments`
```json
[{ "id": "guid", "taskId": "guid", "authorUserId": "guid", "body": "...", "createdAtUtc": "2026-08-22T10:00:00Z" }]
```
Ordered oldest-first.

### `POST /tasks/{id}/comments`
```json
{ "body": "Looks good to me" }
```
`authorUserId` is taken from the JWT, not the body.

### `POST /tasks/{id}/links`
```json
{ "linkedTaskId": "guid", "type": "Blocks" }
```
`type`: `Blocks` / `BlockedBy` / `RelatesTo`. **Stored one-directionally** — creating `A blocks B` does not also create `B blockedBy A`; the caller is responsible for creating the inverse link if the UI needs to show it from both sides. `linkedTaskId` must be a task in the same project.

### `DELETE /tasks/{id}/links/{linkId}`
`204`.

---

## 5. Overview, timeline and schedule

Dashboard/activity/workload/calendar are hosted on `Delivery` (they're pure reads over Delivery's own tables). Gantt/milestones are a separate module, `Scheduling` (Postgres schema `scheduling`), which reads task data from `Delivery` through an in-process cross-module contract (`IProjectTasksService`) rather than referencing it directly — see "Cross-module contracts" below.

### `GET /projects/{id}/overview`
```json
{ "taskCounts": { "backlog": 2, "todo": 1, "inProgress": 0, "done": 3 },
  "totalPoints": 20, "completedPoints": 8,
  "needsAttention": [ /* TaskResponse[] — overdue and incomplete */ ],
  "activeSprint": null }
```

### `GET /projects/{id}/activity?limit=&offset=`
Paginated, newest first. Defaults: `limit=20`, `offset=0`.
```json
[{ "id": "guid", "projectId": "guid", "description": "Sprint 1 started", "occurredAtUtc": "2026-08-23T07:00:00Z" }]
```
Backed by real domain events (`SprintStarted`/`SprintCompleted`/`ProjectTaskCreated`/`ProjectTaskMoved`/`ProjectTaskCommentAdded`), captured via an outbox table and drained in-process by a background poller (no message bus) into `delivery.activity_log_entries`. Only these five events exist today — other actions (e.g. editing a task's title) don't appear here.

### `GET /projects/{id}/workload?sprintId=`
```json
{ "members": [{ "userId": "guid", "email": "a@b.com", "capacity": 1.0, "assignedPoints": 8 }] }
```
`sprintId` optional — omit for all-time assigned points, or scope to one sprint. Only members with a linked `userId` (not pending-by-email invites) appear.

### `GET /projects/{id}/calendar?from=&to=`
```json
{ "tasks": [{ "id": "guid", "key": "APL-1", "title": "...", "dueDate": "2026-08-25", "status": 1 }],
  "sprints": [{ "id": "guid", "name": "Sprint 1", "startDate": "2026-08-24", "endDate": "2026-09-07", "state": 0 }] }
```
Tasks with a due date in range, plus sprints overlapping the range at all (not just fully contained).

### `GET /projects/{id}/schedule`
Module: `Scheduling`. Gantt rows for every task in the project, plus milestones, plus the computed critical path.
```json
{ "tasks": [{ "taskId": "guid", "key": "APL-1", "title": "...", "isDone": false,
              "startDate": "2026-08-23", "endDate": "2026-08-26",
              "slackDays": 0, "isCritical": true, "isManuallyScheduled": false,
              "predecessorTaskIds": [] }],
  "milestones": [{ "milestoneId": "guid", "name": "Beta", "dueDate": "2026-09-10", "status": "Upcoming" }],
  "criticalPathTaskIds": ["guid", "..."] }
```
Dates are computed with a real critical-path (CPM) algorithm — forward pass from today (or from a task's predecessors' finish dates), backward pass from the project end date, `slackDays = latestStart - earliestStart`, `isCritical = slackDays <= 0`. A task that has never been manually rescheduled has no persisted row anywhere; its dates are computed fresh on every request. **Duration model**: since `ProjectTask` has no dedicated estimate field, duration is derived from story points — 1 point ≈ 1 day, minimum 1 day for unpointed tasks. Predecessors are read from `blocks`/`blockedBy` task links (both directions reconciled, since links aren't auto-mirrored). **Known gap**: "epics" (mentioned in the spec's row types) don't exist as an entity anywhere in the codebase — the Gantt only has tasks and milestones.

### `PATCH /schedule/items/{taskId}`
Persists a manually dragged bar. `{taskId}` is the task's own id (there's no separate "schedule item" id to look up first — a task that's never been scheduled yet still has an addressable id).
```json
{ "startDate": "2026-08-26", "endDate": "2026-08-31" }
```
Rejects the move with `400 Schedule.DependencyViolation` if the new start date is before any predecessor's (computed or manually-set) finish date — the server does **not** silently allow a task to start before its dependencies are done. Only the predecessor-side constraint is enforced (not the reverse — moving a task later doesn't currently check whether that invalidates an already-fixed successor). On success, returns the affected task's row with freshly recomputed slack/critical-path flags.

### `POST /projects/{id}/schedule/validate`
Dry run: check a batch of proposed moves against the dependency graph without saving anything.
```json
{ "moves": [{ "taskId": "guid", "startDate": "2026-08-23", "endDate": "2026-08-24" }] }
```
Response: `{ "violations": [{ "taskId": "guid", "reason": "Task APL-2 cannot start on 2026-08-23 before predecessor APL-1 finishes on 2026-08-26" }] }` — empty array means the whole batch is valid. Moves are validated against each other as a set (so moving two dependent tasks together in one call doesn't false-flag).

### `GET /projects/{id}/milestones`
```json
[{ "id": "guid", "projectId": "guid", "name": "Kickoff", "dueDate": "2026-08-01", "status": "Achieved" }]
```
Ordered by due date. `status` is derived, not stored: `"Achieved"` if `dueDate` is in the past, `"Upcoming"` otherwise — there's no explicit completion flag.

### `POST /projects/{id}/milestones`
```json
{ "name": "Beta Release", "dueDate": "2026-09-10" }
```
**Deviation from the spec**: the spec's table only lists `GET /projects/{id}/milestones`, with no creation endpoint. Without one, milestones could never exist, so this was added as the minimal completion of the feature rather than a speculative addition. There's no `PATCH`/`DELETE` yet.

**Not implemented**: the rest of section 9 (Schedule optimisation — `POST /projects/{id}/schedule/optimise`, proposals, `apply`/`apply-partial`, `explanation`) and its supporting reads (`GET /projects/{id}/availability`, `GET /members/{id}/skills`).

### Cross-module contracts (new in this pass)
`Scheduling` needs richer data from `Delivery` than a yes/no access check (the full task graph: points, due dates, dependencies), so it consumes a second Common abstraction, `IProjectTasksService` (`PlanWise.Common.Application.Abstractions`), implemented by `Delivery.Infrastructure`. Same pattern as `IProjectAccessService`/`IProjectMembersService` from section 5's Delivery-hosted half — a narrow, DTO-only interface, currently in-process DI, deliberately kept serialization-friendly so it can become a real network call if `Scheduling` is ever split into its own service.

---

## 6. Cost estimation

Module: `CostEstimation` (Postgres schema `cost_estimation`). The only section that calls a real external model — everything else "intelligence"-shaped in this codebase (the Scheduling optimiser, below) is a deterministic heuristic. Uses the same shared `common.async_jobs` job contract as section 9: `POST .../run` → `202` + job id, poll `GET /jobs/{id}`.

**Setup**: needs `CostEstimation:Anthropic:ApiKey` — see the note at the top of this document. Model defaults to `claude-sonnet-5` (`CostEstimation:Anthropic:Model`), calling the real Anthropic Messages API directly over `HttpClient` (no SDK dependency) with a forced tool-use call so the response is reliably structured JSON rather than parsed out of prose. One retry is built in for the rare case where the model's output doesn't strictly match the declared schema on the first attempt (observed live during development — not hypothetical); a second miss surfaces as a real job failure.

### `POST /projects/{id}/cost-estimates/run`
`202 Accepted`: `{ "jobId": "guid" }`. **Caches on a hash of backlog + rate card** (per the spec's implementation note) — if neither changed since the last run, the job resolves near-instantly to the *existing* run's location instead of calling the LLM again; no duplicate row is created. Change any task's title/description/priority/points (or the rate card) and the next run genuinely re-estimates.

### `GET /projects/{id}/cost-estimates/latest`
```json
{ "id": "guid", "projectId": "guid", "jobId": "guid", "modelName": "claude-sonnet-5", "currency": "USD",
  "result": { "scenarios": [{ "name": "Expected Case", "percentile": 80, "total": 19700, "confidence": "Medium: ..." }],
              "labourLines": [{ "role": "Developer", "hours": 160, "hourlyRate": 75, "cost": 12000 }],
              "nonLabourLines": [{ "description": "Contingency buffer (15%)", "amount": 3400 }],
              "priorityBreakdown": [{ "priority": "Medium", "total": 22640 }],
              "assumptions": ["No historical actuals are available for this project; ...", "..."],
              "reasoning": "Free-text methodology explanation" },
  "createdAtUtc": "2026-08-23T15:12:25Z" }
```
`404 CostEstimate.NoRun` if nothing has ever been run for the project. **`priorityBreakdown` stands in for the spec's "epic breakdown"** — there's no epic/grouping concept above individual tasks anywhere in the codebase (same gap as the Scheduling Gantt), so priority is the closest existing dimension.

### `GET /cost-estimates/{id}`
Same `CostEstimateResponse` shape, by run id directly (not project-scoped in the URL — access is checked via the run's own `projectId`).

### `GET /projects/{id}/cost-estimates`
Full run history, newest first — every run is persisted (cache hits are *not* re-persisted, only genuinely new estimates), so this is what shows estimate drift over time.

### `GET /cost-estimates/{id}/explanation`
```json
{ "id": "guid", "modelName": "claude-sonnet-5", "assumptions": ["..."], "reasoning": "...", "generatedAtUtc": "2026-08-23T15:12:25Z" }
```

### `GET /projects/{id}/budget` / `PUT /projects/{id}/budget`
```json
{ "projectId": "guid", "amount": 50000, "currency": "USD", "updatedAtUtc": "2026-08-23T14:52:41Z" }
```
`PUT` body: `{ "amount": 50000, "currency": "USD" }`. `GET` on a project with no budget set returns a zero default (`amount: 0`, `updatedAtUtc: null`) rather than `404`. Owned by `CostEstimation`, not `WorkspaceManagement` — reverses an earlier roadmap note; the endpoint lives with the screen it serves (same reasoning as Workload living in Delivery), not with the `Project` entity.

### `GET /reference/rates`
```json
[{ "role": "Developer", "hourlyRate": 75, "currency": "USD" }]
```
A fixed, hardcoded role→rate table (`DefaultRateCardProvider`) — no `PUT` exists in the spec for it, and none is built. Five roles seeded: Developer, Lead Developer, Designer, QA Engineer, Project Manager.

**Not implemented / gaps, stated plainly**:
- No genuine "historical actuals" (real spend/time-tracking data) exist anywhere in the system — the prompt tells the model this explicitly rather than inventing figures, and the model's own `assumptions` reflect it.
- `GET /reference/rates` has no write endpoint (matches the literal spec, which also has none).
- Rate card is global, not per-project or per-org configurable.

---

## 9. Schedule optimisation

Module: `Scheduling`. Follows the spec's shared "intelligence endpoint" shape: `POST .../optimise` returns `202` with a job id, poll `GET /jobs/{id}` (below) until `Succeeded`, then read the result at `resultLocation`. Unlike a real ML/LLM model, today's optimiser is a fast deterministic heuristic, so in practice a job is usually already `Succeeded` by the time a client's first poll lands — the async contract is honoured regardless.

**v1 scope, stated plainly**: the optimiser only proposes an **assignee** for currently-unassigned, not-done tasks, balancing load by remaining member capacity. It never touches dates, never reassigns already-assigned work, and does not do competency/skill matching (no task carries a required-skill signal yet) — every member with spare capacity is treated as eligible for every task. Both relaxations are reported honestly in every proposal's `constraintsRelaxed`, not silently assumed away.

### `POST /projects/{id}/schedule/optimise`
Anonymous body (none required). `202 Accepted`:
```json
{ "jobId": "guid" }
```

### `GET /projects/{id}/schedule/proposals/latest`
```json
{ "id": "guid", "projectId": "guid", "jobId": "guid", "status": "Pending",
  "assignments": [{ "id": "guid", "taskId": "guid", "taskKey": "APL-4", "currentAssigneeId": null,
                     "proposedAssigneeId": "guid", "proposedAssigneeEmail": "a@b.com", "isApplied": false }],
  "createdAtUtc": "2026-08-23T12:36:53Z" }
```
`status`: `Pending` / `Applied` / `PartiallyApplied`. `404` if no optimisation has ever been run for the project.

### `POST /schedule/proposals/{id}/apply`
Commits every not-yet-applied assignment in the proposal — pushes each `proposedAssigneeId` to the real task in `Delivery` via a cross-module write (`IProjectTasksService.AssignTaskAsync`) — then returns the updated `GET /projects/{id}/schedule` payload (`ScheduleResponse`, section 5). Calling `apply` again on an already-applied proposal is a safe no-op.

### `POST /schedule/proposals/{id}/apply-partial`
```json
{ "assignmentIds": ["guid1", "guid2"] }
```
Same as `apply`, but only for the listed assignment ids (from the proposal's `assignments[].id`, not task ids). `400 Schedule.InvalidAssignmentSet` if any id doesn't belong to the proposal. Also returns the updated schedule. Proposal `status` becomes `PartiallyApplied` unless every assignment ends up applied.

### `GET /schedule/proposals/{id}/explanation`
```json
{ "id": "guid", "modelName": "GreedyCapacityBalancer v1",
  "objective": "Balance workload for unassigned backlog tasks across project members by remaining capacity",
  "constraintsHonoured": ["Existing assignments on already-assigned tasks were not changed", "..."],
  "constraintsRelaxed": ["Competency/skill matching not yet implemented — ...", "..."],
  "expectedGain": "Reduces max/min assigned-points imbalance across members from 4 to 0",
  "generatedAtUtc": "2026-08-23T12:36:53Z" }
```

### `GET /projects/{id}/availability?from=&to=`
```json
[{ "userId": "guid", "email": "a@b.com", "capacity": 1.0, "availableDates": ["2026-08-24", "2026-08-25", "..."] }]
```
Derived, not stored: every business day (Mon–Fri) in range is reported as available at the member's flat `Capacity` — there's no per-day calendar (holidays/leave) yet. Pending-by-email members (no `userId`) are excluded, same as Workload.

**Not implemented**: `GET /members/{id}/skills` — no skills/competency data model exists anywhere in the codebase yet (would need a new field on `ProjectMember` in `WorkspaceManagement`), and v1's optimiser doesn't consume skills anyway, so it was deferred rather than built unused.

---

## 10. Cross-cutting (partial)

### `GET /jobs/{id}`
Hosted centrally in `Common` (schema `common`), not owned by any one module — every module that runs an async "intelligence" job (today, only Scheduling's optimiser) writes into the same `async_jobs` table via a shared `IAsyncJobService`/`IAsyncJobHandler` contract, so this one endpoint works regardless of which module started the job.
```json
{ "id": "guid", "projectId": "guid", "jobType": "ScheduleOptimisation", "status": 2,
  "resultLocation": "/api/v1/schedule/proposals/guid", "error": null,
  "createdAtUtc": "2026-08-23T12:36:52Z", "completedAtUtc": "2026-08-23T12:36:53Z" }
```
`status`: `0` Queued, `1` Running, `2` Succeeded, `3` Failed. `404` if the job doesn't exist or the caller isn't a member of the project it belongs to.

**Not implemented**: `GET /notifications`, `POST /notifications/read`, `GET /search`, `GET/PUT /me/preferences`, `WS /hubs/project/{id}`.

---

## What's not implemented at all

Sections 7 (Risk prediction) and 8 (Backlog prioritisation) — the two remaining ML modules; `common.async_jobs`/`IAsyncJobHandler` currently has two consumers (Scheduling's optimiser, CostEstimation's LLM run) and either would register the same way. The rest of section 10 (Notifications, Search, Preferences, SignalR hub) and `GET /members/{id}/skills` from section 9. See `PlanWise API.pdf` for the target shape of each.
