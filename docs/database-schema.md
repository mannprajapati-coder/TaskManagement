# Database Schema

One SQL Server database (`TaskPlatformDb`), one `DbContext` per module (ADR-006). This doc lists tables per module, key relationships, and the tenancy mechanism. Column-level DDL lives in migrations (see [migrations.md](migrations.md)), not duplicated here — this is the ER-notes level, kept in sync by hand at each phase boundary (see [plan.md](plan.md)).

## Tenancy

Every table below marked **(tenant)** carries an `OrganizationId` FK and has a global EF Core query filter applied in its owning `DbContext`:

```csharp
modelBuilder.Entity<Project>().HasQueryFilter(p => p.OrganizationId == _tenantProvider.OrganizationId);
```

`_tenantProvider` resolves `OrganizationId` from the current request's JWT/cookie claim (see [auth.md](auth.md)) — never from a client-supplied parameter. `User` and `Organization` itself are the only tables in the schema **not** tenant-filtered (User is global; Organization *is* the tenant).

## Tables by module

**Module 1 — Authentication** (`AuthenticationDbContext`)
- `RefreshToken` (UserId FK, TokenHash, ExpiresAt, Family/RotationId, RevokedAt nullable)
- `EmailVerificationToken` (UserId FK, TokenHash, ExpiresAt)
- `PasswordResetToken` (UserId FK, TokenHash, ExpiresAt, UsedAt nullable)
- `MfaSecret` (UserId FK, EncryptedSecret, IsEnabled)

**Module 2 — User Management** (`UserManagementDbContext`)
- `User` — global, not tenant-scoped. Email (unique), PasswordHash, FullName, ProfilePictureUrl, IsEmailVerified, GoogleSubjectId (nullable, unique when set), CreatedAt.
- `UserPreference` (UserId FK 1:1) — TimeZone, Language, NotificationChannelPrefs (JSON, per BR-19-02).
- `ActiveSession` (UserId FK, RefreshTokenId FK, DeviceInfo, IpAddress, LastSeenAt).

**Module 3 — Organization** (`OrganizationsDbContext`)
- `Organization` — the tenant root. Name, LogoUrl, OwnerUserId FK.
- `OrganizationMembership` (tenant) — OrganizationId FK, UserId FK, RoleId FK. **This is where global Role is attached to a User** (see [domain-model.md](domain-model.md) "User is global" note).
- `Subscription` (tenant, 1:1 with Organization) — Tier, Limits (JSON or discrete columns: MaxWorkspaces, MaxProjects, MaxUsers), RenewsAt.
- `AuditLogEntry` (tenant, append-only) — ActorUserId, Action, TargetType, TargetId, Metadata (JSON), CreatedAt.

**Module 4 — Workspace** (`WorkspacesDbContext`)
- `Workspace` (tenant) — OrganizationId FK, Name, IsArchived.
- `WorkspaceInvite` (tenant) — WorkspaceId FK, TokenHash, ExpiresAt, MaxUses, UseCount.

**Module 5 — Team Management** (`TeamsDbContext`)
- `Team` (tenant) — WorkspaceId FK, Name, LeadUserId FK nullable.
- `TeamMember` (tenant) — TeamId FK, UserId FK, PermissionOverrides (JSON, sparse).

**Module 6 — Role & Permission** (`RolesPermissionsDbContext`)
- `Role` — the 8 system roles (seeded, not user-creatable in v1). Name, IsSystemRole.
- `Permission` — the 7 permission keys (seeded). Key, Description.
- `RolePermission` — RoleId FK, PermissionId FK, IsGranted (the matrix in [user-roles.md](user-roles.md), as data).

**Module 7 — Project** (`ProjectsDbContext`)
- `Project` (tenant) — WorkspaceId FK, Name, Status, StartDate, EndDate nullable, Budget nullable, Client nullable, ProjectManagerUserId FK, IsArchived, IsFavoritedBy (see note below — actually a join table, not a column).
- `ProjectFavorite` (tenant) — ProjectId FK, UserId FK (per-user favorite, many-to-many; corrects the column implied above).

**Module 8 — Project Members** (part of `ProjectsDbContext`)
- `ProjectMember` (tenant) — ProjectId FK, UserId FK, ProjectScopedRoleId FK nullable (override, see BR in business-rules.md), PermissionOverrides (JSON, sparse).
- `ProjectJoinRequest` (tenant) — ProjectId FK, RequestingUserId FK, Status (Pending/Approved/Rejected), ResolvedByUserId nullable.

**Module 9 — Milestones** (`MilestonesDbContext`)
- `Milestone` (tenant) — ProjectId FK, Name, Deadline, *(Completion% is computed, not a column — see BR-09-01)*.
- `MilestoneDependency` (tenant) — MilestoneId FK, DependsOnMilestoneId FK.

**Module 10 — Task** (`TasksDbContext`) — the central table
- `Task` (tenant) — ProjectId FK, MilestoneId FK nullable, Title, Description, Status, Priority, DueDate nullable, StartDate nullable, EstimatedHours nullable, ActualHours (computed/cached, see BR-10-01), PrimaryAssigneeId FK nullable, RecurrenceRuleId FK nullable, ParentTaskId FK nullable *(self-referencing — used by Subtasks, Module 11, rather than a separate table, since a Subtask is structurally a Task with a parent)*.
- `Label` (tenant) / `TaskLabel` (join) — free-form labels per Project.

**Module 11 — Subtasks** — modeled as `Task.ParentTaskId` self-reference (see above), no separate table. Kept as its own *module* (own `IServices`, own controller) per ADR-005 even though it shares `Task`'s table — the module boundary is about ownership of behavior, not a 1:1 requirement with a dedicated table.

**Module 12 — Task Assignment** (part of `TasksDbContext`)
- `TaskAssignee` (tenant) — TaskId FK, UserId FK nullable, TeamId FK nullable *(exactly one of the two set)*.
- `TaskWatcher` (tenant) — TaskId FK, UserId FK.

**Module 13 — Dependencies** (part of `TasksDbContext`)
- `TaskDependency` (tenant) — TaskId FK (successor), DependsOnTaskId FK (predecessor). Unique on the pair; cycle-checked at write time (BR-13-02), not at the DB level (SQL Server can't express a graph-acyclic constraint declaratively).

**Module 14 — Checklist** (part of `TasksDbContext`)
- `ChecklistItem` (tenant) — TaskId FK, Text, IsComplete, SortOrder.

**Module 15 — Recurring Tasks** (`RecurringTasksDbContext`)
- `RecurrenceRule` (tenant) — TemplateTaskId FK (the Task fields to copy), Pattern (JSON: frequency, interval, days-of-week, end condition), IsActive.
- Generated occurrences are ordinary `Task` rows carrying `RecurrenceRuleId` + `OccurrenceSequence` (unique together, BR-15-01).

**Module 16 — Comments** (`CommentsDbContext`)
- `Comment` (tenant) — TaskId FK (or ProjectId FK — polymorphic via `EntityType`/`EntityId` pair, kept simple rather than a nullable-FK-per-target-type design), AuthorUserId FK, ParentCommentId FK nullable (threading), Body, IsEdited, IsDeleted (soft).
- `CommentReaction` (tenant) — CommentId FK, UserId FK, Emoji.

**Module 17 — Attachments** (`AttachmentsDbContext`)
- `Attachment` (tenant) — LogicalAttachmentId (groups versions), EntityType/EntityId (same polymorphic pattern as Comment), FileName, StorageKey, ContentType, SizeBytes, UploadedByUserId FK, Version.

**Module 18 — Activity Timeline** (`ActivityTimelineDbContext`)
- `ActivityLogEntry` (tenant, append-only, no update/delete ever) — EntityType/EntityId, ActorUserId FK, ActionType, Metadata (JSON: before/after where relevant), CreatedAt. Heavily indexed on `(EntityType, EntityId, CreatedAt)` and `(OrganizationId, ActorUserId, CreatedAt)` — this table grows fastest, see [caching-strategy.md](caching-strategy.md) and [logging-monitoring.md](logging-monitoring.md) for retention notes.

**Module 19 — Notifications** (`NotificationsDbContext`)
- `Notification` (tenant) — RecipientUserId FK, Category, EntityType/EntityId, Body, CreatedAt.
- `NotificationDelivery` (tenant) — NotificationId FK, Channel (InApp/Email/RealTime), Status, DeliveredAt nullable.

**Module 20 — Kanban Board** (`KanbanDbContext`)
- `Board` (tenant) — ProjectId FK (1:1 in v1 — one board per Project).
- `BoardColumn` (tenant) — BoardId FK, Name, SortOrder, WipLimit nullable, StatusMapping (which `Task.Status` values land in this column).

**Module 21 — Calendar / Module 22 — Gantt Chart** — no owned tables; pure read models composed from Task/Milestone/TaskDependency via their own `IServices` calls (see [domain-model.md](domain-model.md) cross-module read table).

**Module 23 — Time Tracking** (`TimeTrackingDbContext`)
- `TimeLog` (tenant) — TaskId FK, UserId FK, StartedAt, EndedAt nullable (null = currently running, enforces BR-23-01's one-active-timer rule via a partial unique index on `(UserId) WHERE EndedAt IS NULL`), DurationMinutes (computed on stop).

**Module 24 — Dashboard** — no owned tables; aggregates via the read-model calls in [domain-model.md](domain-model.md).

**Module 25 — Reports** (`ReportsDbContext`)
- `ReportDefinition` (tenant) — Type (Daily/Weekly/Sprint/EmployeePerformance/ProjectHealth), Filters (JSON), CreatedByUserId FK.
- `ReportExport` (tenant) — ReportDefinitionId FK, Format (PDF/Excel/CSV), StorageKey, GeneratedAt.

**Modules 26–30 — AI** (`AiDbContext`, one context for all 5 AI modules — they're small and share no meaningful bounded-context conflict)
- `AiConversation` / `AiMessage` (Module 26)
- `AiGeneratedPlan` / `AiGeneratedTask` (Module 27, draft-only per BR-27-01)
- `AiScheduleSuggestion` (Module 28)
- `AiMeetingExtraction` / `AiExtractedItem` (Module 29, draft-only per BR-29-01)
- `AiPrediction` (Module 30, cached — has an `ExpiresAt` so stale predictions don't linger, see [caching-strategy.md](caching-strategy.md))

All AI tables are **(tenant)** and additionally carry a `RequestedByUserId` — an AI Assistant conversation, for instance, only ever answers from data the requesting user can already see (BR-25-01's "respects effective permissions" principle applied to AI too).

## Soft delete

Every non-append-only table above has an `IsDeleted`/`DeletedAt` pair rather than a hard `DELETE`, per NFR-6, filtered out by the same query-filter mechanism as tenancy (composed, not a second separate filter). Hard delete exists only as an explicit Admin/Owner action, itself logged to `AuditLogEntry` before the row is actually removed.

## Indexing notes

- Every FK gets a non-clustered index by default (EF Core convention); the exceptions worth calling out explicitly are `ActivityLogEntry`'s compound indexes above and `TimeLog`'s partial unique index enforcing BR-23-01.
- `Task(ProjectId, Status)` — the single most common query shape (board rendering) — gets a dedicated covering index once real load-testing (see [testing-strategy.md](testing-strategy.md)) shows it's warranted; not pre-optimized before there's data to measure against.
