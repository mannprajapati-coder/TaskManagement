# Domain Model

Plain-language description of every entity, its aggregate, and its business rules — for the exact validation/edge-case logic behind each rule, see [business-rules.md](business-rules.md). For table/column/FK detail, see [database-schema.md](database-schema.md).

## Module index (30 modules → 8 phases → aggregate roots)

| # | Module | Phase | Aggregate root | Owns / contains |
|---|---|---|---|---|
| 1 | Authentication | 1 | *(spans User)* | RefreshToken, EmailVerificationToken, PasswordResetToken, MfaSecret |
| 2 | User Management | 1 | **User** | UserPreferences, ActiveSession |
| 3 | Organization | 1 | **Organization** | OrganizationSettings, Subscription, AuditLogEntry |
| 4 | Workspace | 2 | **Workspace** | WorkspaceInvite |
| 5 | Team Management | 2 | **Team** | TeamMember |
| 6 | Role & Permission | 2 | **Role** | Permission, RolePermission |
| 7 | Project | 3 | **Project** | — |
| 8 | Project Members | 3 | *(part of Project)* | ProjectMember, ProjectJoinRequest |
| 9 | Milestones | 3 | **Milestone** | MilestoneDependency |
| 10 | Task | 4 | **Task** | — |
| 11 | Subtasks | 4 | *(part of Task)* | Subtask |
| 12 | Task Assignment | 4 | *(part of Task)* | TaskAssignee, TaskWatcher |
| 13 | Dependencies | 4 | *(cross-Task)* | TaskDependency |
| 14 | Checklist | 4 | *(part of Task)* | ChecklistItem |
| 15 | Recurring Tasks | 4 | **RecurrenceRule** | (generates Task instances) |
| 16 | Comments | 5 | **Comment** | CommentReaction |
| 17 | Attachments | 5 | **Attachment** | AttachmentVersion |
| 18 | Activity Timeline | 5 | **ActivityLogEntry** | *(append-only, no children)* |
| 19 | Notifications | 5 | **Notification** | NotificationDelivery |
| 20 | Kanban Board | 6 | **Board** | BoardColumn |
| 21 | Calendar | 6 | *(read model over Task/Milestone)* | — |
| 22 | Gantt Chart | 6 | *(read model over Task/Dependency)* | — |
| 23 | Time Tracking | 6 | **TimeLog** | — |
| 24 | Dashboard | 7 | *(read model, no owned tables)* | — |
| 25 | Reports | 7 | **ReportDefinition** | ReportExport |
| 26 | AI Assistant | 8 | **AiConversation** | AiMessage |
| 27 | AI Task Generator | 8 | **AiGeneratedPlan** | AiGeneratedTask (draft, pre-commit) |
| 28 | AI Smart Scheduler | 8 | **AiScheduleSuggestion** | — |
| 29 | AI Meeting Notes | 8 | **AiMeetingExtraction** | AiExtractedItem (draft, pre-commit) |
| 30 | AI Analytics | 8 | *(read model + cached predictions)* | AiPrediction |

## Hierarchy (plain language)

```
Organization                              tenant boundary — everything below is scoped to one Organization
├── Members (Users, via OrganizationMembership + Role)
├── Subscription (tier, limits)
├── AuditLog
├── Workspace (e.g. "Development", "Marketing")
│   ├── Invite links
│   └── Team (e.g. "Backend Team")
│       └── TeamMember (User + team-level permission overrides)
└── Project (e.g. "E-Commerce Rebuild")
    ├── ProjectMember (User + project-scoped role/permission overrides, independent of Team)
    ├── Milestone (e.g. "Payment Integration", with a deadline and % complete)
    │   └── MilestoneDependency → another Milestone
    ├── Board (Kanban) → BoardColumn (ordered, WIP-limited)
    └── Task (the unit of work)
        ├── Subtask (own status/assignment, linked to parent Task)
        ├── TaskAssignee / TaskWatcher (Users and/or Teams)
        ├── TaskDependency → another Task (predecessor/successor)
        ├── ChecklistItem (ordered, completable)
        ├── RecurrenceRule (if this Task is a template for repeating occurrences)
        ├── Comment (threaded, @mentions, reactions)
        ├── Attachment (versioned)
        ├── TimeLog (start/pause/resume/stop, per User)
        └── ActivityLogEntry (immutable, auto-generated on every mutation above)
```

`AiConversation`, `AiGeneratedPlan`, `AiMeetingExtraction`, `AiScheduleSuggestion`, and `AiPrediction` all reference back into this hierarchy (a conversation is scoped to an Organization/Project; a generated plan targets a Project; an extraction's items become draft Tasks) but are not themselves part of the core hierarchy — they are the AI layer's own aggregates that *produce* core-hierarchy entities once a human accepts their output (see [ai-usage-guidelines.md](ai-usage-guidelines.md)).

## Entity notes worth calling out explicitly

- **User is global, Organization membership is not.** A `User` row is not itself tenant-scoped (one person can belong to multiple Organizations, e.g. a contractor); `OrganizationMembership` is the join entity carrying the `Role`. Every *other* entity in the hierarchy above is directly or transitively tenant-scoped by `OrganizationId` (see ADR-006).
- **Task's `AssigneeId` vs `TaskAssignee`**: Task keeps one denormalized `PrimaryAssigneeId` (drives "my tasks" widgets cheaply) in addition to the `TaskAssignee` join table for the full multi-assignee list — see scope.md assumption #1.
- **RecurrenceRule generates real Task rows**, it does not itself appear on a board/calendar. Each generated Task carries a `RecurrenceRuleId` back-pointer so "edit/cancel series" can find every occurrence (including already-generated future ones) vs. "edit this occurrence only."
- **Milestone Completion % is computed, not stored** — derived from the completion state of Tasks whose `MilestoneId` matches, recalculated on read (cached briefly, see [caching-strategy.md](caching-strategy.md)), never written directly by a user action.
- **ActivityLogEntry is the one truly append-only, immutable table** in the schema — no update, no delete, ever (even soft-delete). It is the audit trail Module 18 promises.
- **AiGeneratedTask / AiExtractedItem are drafts, not Tasks.** They live in the AI modules' own tables until a human explicitly "accepts" them, at which point a real `Task` row is created by calling `ITasksService` like any other caller — the AI modules never write directly into the Tasks module's tables (respects the module boundary in [architecture.md](architecture.md) §2).

## Cross-module read dependencies (who reads whom)

```
Dashboard        reads  Task, Milestone, Project, TimeLog
Reports          reads  Task, Milestone, Project, TimeLog, ActivityLogEntry
Calendar         reads  Task, Milestone
GanttChart       reads  Task, TaskDependency, Milestone
AIAssistant      reads  everything the calling user can see (permission-filtered)
AITaskGenerator  writes AiGeneratedPlan/AiGeneratedTask only; on accept, calls Tasks/Milestones/Projects services
AISmartScheduler reads  Task, TaskDependency, TimeLog, TeamMember   writes AiScheduleSuggestion only
AIMeetingNotes   writes AiMeetingExtraction/AiExtractedItem only;   on accept, calls Tasks service
AIAnalytics      reads  Task, Milestone, TimeLog, ActivityLogEntry  writes AiPrediction (cached)
```

No module in this table writes to another module's owned tables directly — every write crosses a module boundary only through that module's public `IServices` interface, per [architecture.md](architecture.md) §2 and [folder-structure.md](folder-structure.md)'s closing note.
