# Changelog

Format: [Keep a Changelog](https://keepachangelog.com/)-style, grouped by the [plan.md](plan.md) milestone it belongs to. One entry per shipped PR (see [git-workflow.md](git-workflow.md)) — added the same PR that makes the change, not batched later from memory.

Every entry should be traceable to an `FR-xx-y`/`BR-xx-yy` ([requirements.md](requirements.md), [business-rules.md](business-rules.md)) where one exists, and to a [scope.md](scope.md) entry if it's a deliberate deviation from spec.

## [v1.0-sprint10] — 2026-08-05
### Added
- FR-24-1..4: Default Analytics Dashboard module (Workspace overview metrics, task completion rate calculation, status distribution breakdown, and upcoming deadlines widget).
- Comprehensive end-to-end security & multi-tenancy audit pass.
- Unit tests for BR-24-01 (`BR2401_DashboardMetricsAggregationTests`).

## [v0.9-sprint09] — 2026-08-05
### Added
- FR-21-1..4: Calendar View module (Task due date event mapping, priority color coding, and drag-and-drop rescheduling endpoint).
- Unit tests for BR-21-01 (`BR2101_CalendarEventFormattingTests`).

## [v0.8-sprint08] — 2026-08-05
### Added
- FR-18-1..4: Activity Audit Timeline module (System audit log recorder for workspace/project/task mutations).
- FR-19-1..5: Notifications module (In-app notifications center, mark as read, and unread filter).
- EF Core migration `AddActivityAndNotificationsSchema`.
- Unit tests for BR-18-01 and BR-19-01.

## [v0.7-sprint07] — 2026-08-05
### Added
- FR-16-1..4: Task Comments & Mentions module (Discussion threads, comment creation/deletion, and @mentions parser).
- FR-17-1..4: File Attachments module (Metadata storage, file size & type tracking, upload/download endpoints).
- EF Core migration `AddCommentsAndAttachmentsSchema`.
- Unit tests for BR-16-01 and BR-17-01.

## [v0.6-sprint06] — 2026-08-05
### Added
- FR-14-1..4: Task Checklists module (Interactive task checklist items, sort order, toggle completion).
- FR-15-1..4: Recurring Tasks module (Recurrence rule configuration: Daily, Weekly, Monthly, and automated processing background runner).
- EF Core migration `AddChecklistsAndRecurringSchema`.
- Unit tests for BR-14-01 and BR-15-01.

## [v0.5-sprint05] — 2026-08-05
### Added
- FR-11-1..4: Subtasks module (Parent-child subtask tree hierarchy, subtask creation, inline subtasks display, and cascade deletion).
- FR-12-1..4: Task Assignment module (Multi-assignees per task, task watchers, and watcher toggle).
- BR-11-01 (Parent completion constraint: parent task cannot be marked Completed while incomplete subtasks remain).
- BR-12-01 (Primary assignee auto-sync into task assignees list).
- EF Core migration `AddSubtasksAndAssignmentsSchema`.
- Unit tests for BR-11-01 and BR-12-01.

## [v0.4-sprint04] — 2026-08-05
### Added
- FR-10-1..6: Core Task Engine module (Task creation, priority levels, status state transitions, due/start dates, estimated vs actual hours tracking, and project task filtering).
- BR-10-01 (Non-negative actual hours, automatic `CompletedAt` timestamp management, and date validation).
- EF Core migration `AddCoreTasksSchema`.
- Unit tests for BR-10-01 (`CompletedAt` auto-set & reset, non-negative hours, date constraints).

## [v0.3-sprint03] — 2026-08-05
### Added
- FR-07-1..5: Project Management module (Project creation, start/end dates, budget, client, status tracking, favoriting, and archiving).
- FR-08-1..4: Project Members module (Member assignment, role permissions, member removal, join request submission & approval).
- BR-07-01 (Project date validation `EndDate >= StartDate`).
- BR-08-01 (Join request approval defaults role to Developer).
- EF Core migration `AddProjectsAndMembersSchema`.
- Unit tests for BR-07-01 and BR-08-01.

## [v0.2-sprint02] — 2026-08-05
### Added
- FR-02-1..4: User Management module (User profile, job title, bio, password change, preferences, active security session tracking and remote revocation).
- FR-04-1..5: Workspace module (Workspace creation, listing, details, settings, soft archiving/unarchiving, token-based invite links, and joining via invite).
- BR-04-01 (Invite token expiry and max uses validation).
- BR-04-02 (Soft workspace archiving).
- EF Core migrations `AddUserProfileSchema` and `InitialWorkspacesSchema`.
- Unit tests for BR-04-01 and BR-04-02.

## [v0.1-sprint01] — 2026-08-05
### Added
- FR-01-1..7: Authentication module (backend API + ASP.NET Core Razor MVC frontend).
- Identity User entity and EF Core DbContexts (`UserManagementDbContext` and `AuthenticationDbContext`).
- BR-01-01 (Password reset token invalidates all user refresh tokens).
- BR-01-02 (Email verification check for login).
- BR-01-03 (Refresh token rotation with reuse detection family invalidation).
- RS256 JWT access token generator and rate-limited API endpoints.
- EF Core migrations `InitialUserSchema` and `InitialAuthenticationSchema`.
- Unit tests for BR-01-01, BR-01-02, and BR-01-03.

---

## Template for future entries

```
## [v0.1-foundation] — YYYY-MM-DD
### Added
- FR-01-1..7: Authentication module — register/login/verify/reset/refresh/Google/MFA.
- FR-02-1..4: User Management module.
- FR-03-1..5: Organization module, multi-tenancy query filters (ADR-006).

### Changed
- (spec deviations, with a link to the scope.md entry that authorized them)

### Fixed
- (bugs found during this milestone's "verified live" pass — see plan.md's Definition of Done)

### Known issues carried forward
- (link to known-issues.md entries opened this milestone)
```

Keep milestone tags aligned with [git-workflow.md](git-workflow.md)'s tagging convention (`v0.1-foundation`, `v0.2-workspace`, ... `v1.0`) so a tag and a changelog section always correspond 1:1.
