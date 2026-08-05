# Requirements

Source: `Project_Functional_Specification.docx` v1.0 (August 2026). This doc restates the spec as testable requirements, grouped functional-by-module and non-functional by category, so each requirement can be traced to a task in [plan.md](plan.md) and a test in [testing-strategy.md](testing-strategy.md).

## Functional Requirements

Each module below is one row in [domain-model.md](domain-model.md)'s module table and one class library per [folder-structure.md](folder-structure.md). Requirement IDs are `FR-<ModuleNo>-<n>`.

### Phase 1 — Foundation

**Module 1: Authentication**
- FR-01-1 Register with email + password; verify email before first login (except where Google login is used).
- FR-01-2 Login with email + password, returns access token + refresh token.
- FR-01-3 Forgot Password → emailed reset link → Reset Password.
- FR-01-4 Refresh Token endpoint issues a new access token without re-prompting credentials.
- FR-01-5 JWT-based authentication on every protected endpoint.
- FR-01-6 Google OAuth login, linked to an existing account by email or creating a new one.
- FR-01-7 Optional Multi-Factor Authentication (TOTP) — user-enabled, not mandatory for v1.

**Module 2: User Management**
- FR-02-1 View/edit profile (name, title, profile picture).
- FR-02-2 Change password (requires current password).
- FR-02-3 User preferences: notification settings, time zone, language.
- FR-02-4 View and revoke active sessions (per refresh token).

**Module 3: Organization**
- FR-03-1 Create an Organization; creator becomes Owner.
- FR-03-2 Organization Settings: name, logo, billing details.
- FR-03-3 Subscription plan tracked per Organization (tier gates limits — see [business-rules.md](business-rules.md)).
- FR-03-4 Organization-level Audit Log of sensitive actions (see [logging-monitoring.md](logging-monitoring.md)).
- FR-03-5 Full data isolation between Organizations — no query may return rows from another Organization (see ADR-006).

### Phase 2 — Workspace

**Module 4: Workspace**
- FR-04-1 Create Workspace within an Organization.
- FR-04-2 Invite members by email or shareable invite link.
- FR-04-3 Join via invite link (with expiry).
- FR-04-4 Workspace Settings; Archive Workspace (soft, reversible); Transfer Ownership.

**Module 5: Team Management**
- FR-05-1 Create Team within a Workspace; assign a Team Lead.
- FR-05-2 Add/remove members; per-team permission overrides.
- FR-05-3 Team Activity feed (subset of Activity Timeline, Module 18, filtered by team).

**Module 6: Role & Permission**
- FR-06-1 8 system roles: Owner, Admin, Project Manager, Team Lead, Developer, Tester, Viewer, Guest (see [user-roles.md](user-roles.md)).
- FR-06-2 7 permission keys: Create Project, Delete Project, Assign Task, Manage Users, Export Reports, Manage Billing, View Reports.
- FR-06-3 Permission checks enforced server-side on every `TaskPlatform.Api` action, never only in the UI (see [auth.md](auth.md)).

### Phase 3 — Projects

**Module 7: Project**
- FR-07-1 Create Project (name, status, start/end date, budget, client, assigned Project Manager).
- FR-07-2 Archive / Favorite a project.

**Module 8: Project Members**
- FR-08-1 Per-project member list, independent of Workspace/Team membership.
- FR-08-2 Add/remove member, assign a project-scoped role, project-scoped permission overrides.
- FR-08-3 Join Request flow for a user requesting access to a project they can see but aren't a member of.

**Module 9: Milestones**
- FR-09-1 Create Milestone with deadline; track Progress / Completion %.
- FR-09-2 Milestone Dependencies (a milestone can require another milestone complete first).

### Phase 4 — Task Management

**Module 10: Task**
- FR-10-1 CRUD on Task: status, priority, labels, due date, start date, estimated hours, actual hours.

**Module 11: Subtasks**
- FR-11-1 Create Subtask linked to a parent Task; independent status and assignment from the parent.

**Module 12: Task Assignment**
- FR-12-1 Assign a Task to one or more users and/or a Team; support Watchers/Followers who get notified but aren't assignees.

**Module 13: Dependencies**
- FR-13-1 Define predecessor/successor between Tasks (A → B → C); a task cannot move to "In Progress" while an incomplete blocking predecessor exists (see BR-13-01 in [business-rules.md](business-rules.md)).
- FR-13-2 Dependency visualization (feeds the Gantt chart, Module 22).

**Module 14: Checklist**
- FR-14-1 Add/reorder/complete checklist items inside a Task.

**Module 15: Recurring Tasks**
- FR-15-1 Define a recurrence pattern (daily/weekly/monthly/custom cron-like); auto-generate the next occurrence when the current one is completed or its due date passes; edit or cancel the whole series vs. a single occurrence.

### Phase 5 — Collaboration

**Module 16: Comments** — FR-16-1 Threaded replies, @mentions, emoji reactions, edit/delete own comments.

**Module 17: Attachments** — FR-17-1 Upload files/images/PDFs to a Task or Project; keep version history on re-upload of the same logical attachment.

**Module 18: Activity Timeline** — FR-18-1 Automatic, immutable event log per Task and per Project; filterable by user or action type.

**Module 19: Notifications** — FR-19-1 In-app, email, and real-time (SignalR) channels; push is explicitly out of v1 scope (see [scope.md](scope.md)).

### Phase 6 — Planning

**Module 20: Kanban Board** — FR-20-1 Drag-and-drop between customizable columns; optional WIP limit per column with a soft warning when exceeded.

**Module 21: Calendar** — FR-21-1 Day/Week/Month views of tasks + milestones; drag-and-drop reschedule updates the underlying Task's due/start date.

**Module 22: Gantt Chart** — FR-22-1 Timeline bar per task, dependency lines, progress overlay — read-only in v1 (see [scope.md](scope.md) for the drag-to-reschedule deferral).

**Module 23: Time Tracking** — FR-23-1 Start/Pause/Resume/Stop timer per task per user; timesheet view aggregating logged time.

### Phase 7 — Reporting

**Module 24: Dashboard** — FR-24-1 Widgets: pending tasks, overdue tasks, team workload, completion rate, productivity — scoped to the viewer's permitted projects.

**Module 25: Reports** — FR-25-1 Daily/Weekly/Sprint/Employee-Performance/Project-Health reports; export to PDF, Excel, CSV.

### Phase 8 — AI

**Module 26: AI Assistant** — FR-26-1 Natural-language Q&A over the caller's own accessible data ("what should I work on today," "which tasks are overdue"); roadmap generation; work summarization.

**Module 27: AI Task Generator** — FR-27-1 Given a high-level prompt, generate a structured draft plan (modules → tasks/subtasks → timeline → priorities → dependencies) that a human reviews and commits — never auto-commits without review (see [ai-usage-guidelines.md](ai-usage-guidelines.md)).

**Module 28: AI Smart Scheduler** — FR-28-1 Suggests a daily work plan, reschedules overdue tasks, flags scheduling conflicts, and surfaces workload-imbalance suggestions — suggestion-only in v1, applied only on explicit user confirmation.

**Module 29: AI Meeting Notes** — FR-29-1 Upload a transcript/PDF/DOCX; extract candidate tasks, action items, deadlines, and owners as a reviewable draft list.

**Module 30: AI Analytics** — FR-30-1 Project risk & delay prediction, team performance insights, estimated completion date, productivity recommendations — presented as read-only insights, not automated actions.

## Non-Functional Requirements

| ID | Category | Requirement |
|---|---|---|
| NFR-1 | Multi-tenancy | Zero cross-Organization data leakage under any query path (verified by a dedicated integration test suite — see [testing-strategy.md](testing-strategy.md)). |
| NFR-2 | Availability | 99.5% uptime target post-launch (see [deployment.md](deployment.md)). |
| NFR-3 | Performance | P95 API response time < 500ms for CRUD endpoints; Kanban board initial load < 2s for a board with ≤500 visible cards. |
| NFR-4 | Scalability | Horizontally scalable `TaskPlatform.Api`/`.Web` behind a load balancer; SignalR via Redis backplane (see [caching-strategy.md](caching-strategy.md)) — no in-memory session/sticky-session dependency. |
| NFR-5 | Security | OWASP Top 10 mitigations addressed explicitly (see [auth.md](auth.md), [error-handling.md](error-handling.md)); secrets never in source control (see [configuration.md](configuration.md)). |
| NFR-6 | Data retention | Soft-delete for Organization/Workspace/Project/Task; hard-delete only via an explicit, audited admin action. |
| NFR-7 | Accessibility | WCAG 2.1 AA for core flows (login, task CRUD, Kanban keyboard-alternative) — see [ui-guidelines.md](ui-guidelines.md). |
| NFR-8 | Internationalization | UI strings resource-based from day one (even if only English ships in v1) — language preference already required by FR-02-3. |
| NFR-9 | Observability | Every request correlated by a trace/request ID from `TaskPlatform.Web` through `TaskPlatform.Api` to logs (see [logging-monitoring.md](logging-monitoring.md)). |
| NFR-10 | AI cost/latency | AI module calls must not block the primary request thread for non-AI features; long-running AI operations (Task Generator, Meeting Notes extraction) run as background jobs with a polling/SignalR-pushed result, never a synchronous multi-second HTTP call. |

## Traceability

Every `FR-xx-y` should map to at least one Sprint deliverable in [plan.md](plan.md) and at least one test case referenced from [testing-strategy.md](testing-strategy.md). If a requirement is deferred past v1, it must appear in [scope.md](scope.md)'s "Out of Scope" table, not silently disappear from here.
