# Business Rules

Validation logic, edge cases, and calculations that aren't obvious just from reading the code — organized by module. Rule IDs are `BR-<ModuleNo>-<seq>`. Every rule here should have a corresponding test named after its ID (see [testing-strategy.md](testing-strategy.md)).

### Module 1 — Authentication
- **BR-01-01**: A password reset token is single-use and expires 60 minutes after issue; using it invalidates all of that user's existing refresh tokens (forces re-login everywhere, standard "reset password logs out other sessions" behavior).
- **BR-01-02**: Email verification is required before login **unless** the account was created via Google login (Google already verified the email).
- **BR-01-03**: Refresh tokens rotate on use (old one invalidated, new one issued) — reusing an already-rotated refresh token is treated as a compromise signal and invalidates the entire token family for that user.

### Module 3 — Organization
- **BR-03-01**: Exactly one `Owner` per Organization at all times. "Transfer Ownership" is atomic: the target user becomes Owner and the previous Owner is demoted to Admin in the same transaction — there is never a moment with zero or two Owners.
- **BR-03-02**: Deleting/archiving an Organization is only available to the Owner and requires re-entering the account password (destructive, hard-to-reverse action — extra confirmation friction is deliberate).
- **BR-03-03**: Subscription tier gates *limits*, not features in v1 (see [scope.md](scope.md) — no real billing yet): max Workspaces, max Projects, max Users per tier, enforced at the point of creation with a clear "upgrade to add more" error, not a silent cap.

### Module 4 — Workspace
- **BR-04-01**: An invite link has an expiry (default 7 days) and an optional max-use count; joining via an expired/exhausted link fails with a specific error, not a generic 404.
- **BR-04-02**: Archiving a Workspace does not delete its Projects/Tasks — it hides the Workspace from the default nav and blocks *new* Project creation inside it. Unarchiving is always possible (soft, reversible per NFR-6).

### Module 5/6 — Teams, Roles & Permission
- **BR-06-01**: A Team Lead's `ManageUsers` scope is computed as "is this user the `LeadUserId` of the Team the target member belongs to" — checked server-side on every add/remove-member call, not inferred from the UI hiding the button (see [user-roles.md](user-roles.md)).
- **BR-06-02**: Removing the last member of a Team does not delete the Team (Teams can be empty); removing a Team Lead without designating a replacement leaves the Team lead-less, which is allowed but surfaced as a dashboard warning to Admin/Owner, not silently ignored.

### Module 7/8 — Projects & Members
- **BR-07-01**: `EndDate` must be on or after `StartDate` if both are set; either may be null (open-ended projects are valid).
- **BR-08-01**: A Join Request notifies every user on the Project who holds `ManageUsers` at the project-effective-permission level (see [user-roles.md](user-roles.md) override rules) — approving creates a `ProjectMember` row with a default role of the lowest role that has any create rights (currently Developer), not Owner/Admin, to avoid an accidental privilege escalation via join request.

### Module 9 — Milestones
- **BR-09-01**: Completion % is `completedTaskCount / totalTaskCount` among Tasks whose `MilestoneId` matches, `0` when there are zero linked Tasks (never divide-by-zero, never null) — see [domain-model.md](domain-model.md)'s "computed, not stored" note.
- **BR-09-02**: A Milestone with an incomplete predecessor `MilestoneDependency` is flagged "blocked" in the UI but is **not** hard-blocked from having its own Tasks worked on — unlike Task-level dependencies (BR-13-01), Milestone dependency is advisory/visual only in v1, since the spec lists "Dependencies" for Milestones without the Task-level "Block Start Until Dependency Complete" wording it uses for Module 13.

### Module 10/11 — Task & Subtasks
- **BR-10-01**: `ActualHours` is never user-typed directly in v1 for tasks with any `TimeLog` entries — it's the sum of that Task's TimeLogs, displayed read-only; a Task with zero TimeLogs allows a manual estimate-style entry (covers quick "log 2h retroactively" use without requiring the full timer flow).
- **BR-10-02**: Deleting a parent Task requires either zero Subtasks or explicit confirmation that all Subtasks will be deleted too (cascade is never silent).
- **BR-11-01**: A Subtask's own Status is independent of its parent's, but a parent Task cannot be marked "Done" while it has an incomplete Subtask — surfaced as a validation error naming the incomplete Subtask(s), not a silently-ignored status change.

### Module 12 — Task Assignment
- **BR-12-01**: `PrimaryAssigneeId` must always be a member of the `TaskAssignee` set — assigning a new primary assignee who isn't already a co-assignee adds them to `TaskAssignee` in the same operation.
- **BR-12-02**: A Watcher is not notified of *every* activity (that's what following the full Activity Timeline is for) — only of Status changes, new Comments, and due-date changes, to keep Module 19 notification volume sane.
- **BR-12-03**: Editing, completing, or deleting a Task (or Subtask) — and adding/toggling/deleting its Checklist items — is restricted to that Task's assignee(s), the owning Project's Owner, or the Workspace Owner. Anyone else is rejected with a permission error (HTTP 403), even if they're authenticated. Creating a new Task/Subtask is unrestricted (the creator has no assignee to check against yet); comments, attachments, watching, and recurring rules are unaffected by this rule.

### Module 13 — Dependencies
- **BR-13-01**: A Task with an incomplete predecessor cannot transition to any "in progress"-family status (checked against the Board's own column semantics, Module 20) — attempting to do so (via API, drag-drop, or bulk action) returns a specific "blocked by predecessor" error naming the blocking Task, never a generic validation failure.
- **BR-13-02**: Circular dependencies (A→B→C→A) are rejected at creation time with a cycle-detection check before the new `TaskDependency` row is persisted — never allowed to exist even transiently.

### Module 14 — Checklist
- **BR-14-01**: Checklist completion contributes to a Task's own "progress" display (e.g. "3/5 items"). A Task with at least one checklist item **cannot** transition to "Completed" while any item remains unchecked — attempting to do so returns a validation error rather than silently completing with unfinished items. A Task with zero checklist items is unaffected by this rule.

### Module 15 — Recurring Tasks
- **BR-15-01**: The next occurrence is generated when the current occurrence's due date passes **or** it's marked complete, whichever comes first — never both (idempotent generation, guarded by a unique constraint on `(RecurrenceRuleId, OccurrenceSequence)`).
- **BR-15-02**: "Cancel Series" stops future generation but never deletes already-generated past/current occurrences; "Edit Series" only applies to occurrences not yet generated — an already-generated Task is a normal, independently-editable Task from that point on.

### Module 16 — Comments
- **BR-16-01**: A comment can only be edited/deleted by its author or a user holding `ManageUsers`-or-above on that Project — edit leaves an "edited" marker (no silent rewrite of history), delete is soft (see NFR-6) so Activity Timeline entries referencing the comment remain coherent.
- **BR-16-02**: An @mention only notifies the mentioned user if they have read access to that Task's Project — mentioning someone without access is accepted (no error, avoids leaking "does this person have access" info to the mentioner) but silently produces no notification.

### Module 17 — Attachments
- **BR-17-01**: Re-uploading a file with the same `LogicalAttachmentId` creates a new `AttachmentVersion` rather than overwriting — the version list is the whole history, current version is just the latest by `CreatedAt`.
- **BR-17-02**: File type/size validated server-side before storage write, not just client-side — matches the hardening precedent already established in this workspace (Sawi's own recent "Enhance file upload validation and error handling" work). Default max 25MB/file, configurable per Organization tier (see [configuration.md](configuration.md)).

### Module 19 — Notifications
- **BR-19-01**: A user's own action never generates a notification to themselves (e.g., completing your own assigned Task doesn't notify you that you completed it) — filtered at the point of dispatch by comparing actor to recipient.
- **BR-19-02**: Notification channel preference (Module 2) is per-category, not global — a user can get real-time SignalR pushes for mentions but only daily-digest email for status changes; the default for every category is "In-App + Email," Real-Time is additive on top when connected.

### Module 20 — Kanban Board
- **BR-20-01**: A WIP limit is a soft warning (visually flags the column, does not block the drop) — spec says "WIP Limits" without "block," and hard-blocking a drag-drop mid-gesture is a worse UX than a warning band.
- **BR-20-02**: Dragging a card to a column that maps to a terminal-family status (e.g. "Done") re-checks BR-13-01 (blocked-by-predecessor) and BR-11-01 (incomplete subtasks) — a drag that would violate either is rejected with the card snapping back and a toast explaining why, not a silent no-op.

### Module 22 — Gantt Chart
- **BR-22-01**: Progress overlay is the same computed value as the Task's own checklist/subtask progress (BR-14-01's "3/5 items" concept generalized) — Gantt does not maintain a second, independent progress number.

### Module 23 — Time Tracking
- **BR-23-01**: Only one active (running) timer per user at a time across the whole system — starting a new timer while one is already running auto-stops the previous one and logs its elapsed time, rather than silently allowing two timers or blocking the new start.

### Module 25 — Reports
- **BR-25-01**: Export respects the exporting user's own effective permissions at generation time — a report can never contain a row from a Project the exporting user can't view, even if the report's *filter* would otherwise include it.

### Module 27 — AI Task Generator / Module 29 — AI Meeting Notes
- **BR-27-01 / BR-29-01**: AI-generated output is never committed directly into Tasks/Milestones/Projects tables — it lands in the AI module's own draft tables (`AiGeneratedTask`, `AiExtractedItem`) and only becomes real domain data when a human explicitly accepts it, which then goes through the normal `ITasksService`/etc. call path (and therefore through every rule above) exactly like a manually-created Task would. See [ai-usage-guidelines.md](ai-usage-guidelines.md).

### Module 28 — AI Smart Scheduler
- **BR-28-01**: A schedule suggestion that would violate BR-13-01 (dependency ordering) is never generated in the first place — the scheduler's own solver is constrained by the same dependency graph the Kanban board enforces, so its suggestions are always legal to accept as-is.
