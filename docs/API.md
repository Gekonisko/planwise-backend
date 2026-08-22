# PlanWise Backend — Implemented API Reference

This documents what is actually implemented in the backend today, as opposed to `PlanWise API.pdf` at the repo root, which is the full target spec (10 sections). Only sections 1–4 exist; sections 5–10 (overview/schedule, cost estimation, risk prediction, backlog prioritisation, schedule optimisation, cross-cutting) have no backend code yet.

Live, interactive docs (Scalar/OpenAPI) are also available at `/scalar/v1` when the API runs in the `Development` environment.

## Conventions

- Base path: `/api/v1`
- Auth: JWT bearer access token in the `Authorization: Bearer <token>` header, except `POST /auth/register`, `POST /auth/login`, `POST /auth/refresh`, `POST /auth/password/forgot`, `POST /auth/password/reset`, which are anonymous. Every other endpoint below requires the header.
- Refresh token: returned via an `HttpOnly`, `Secure`, `SameSite=Strict` cookie named `planwise_refresh_token`, scoped to path `/api/v1/auth`. It is never present in a JSON response body.
- Errors: RFC 7807 problem details (`title`, `detail`, `type`, `status`), produced by `PlanWise.Common.Presentation.Results.ApiResults.Problem`. Validation failures additionally carry an `errors` extension (field → messages). Status codes: `400` (validation/business-rule problem), `401` (unauthorized), `404` (not found — also used for authorization failures on scoped resources, so as not to reveal existence to non-members), `409` (conflict).
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

**Not implemented**: `GET /projects/{id}/overview`, `/activity`, `/workload`, `/calendar`, `/schedule` (spec section 5) — no backend code for any of these yet.

---

## What's not implemented at all

Sections 5 (Gantt/schedule — the overview/activity/workload/calendar reads exist nowhere yet either), 6 (Cost estimation), 7 (Risk prediction), 8 (Backlog prioritisation), 9 (Schedule optimisation), 10 (Jobs, Notifications, Search, Preferences, SignalR hub). See `PlanWise API.pdf` for the target shape of each.
