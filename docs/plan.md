# Implementation Plan — Customized Roadmap

Roadmap for building TaskPlatform tailored to the user's custom selection of 16 core modules across 10 structured sprints.

## Milestone Summary

- **Phase 1 — Foundation & Core Account** (Sprints 1–2): Auth, Identity, User Management, Workspaces.
- **Phase 2 — Project & Team Structure** (Sprint 3): Projects, Project Members, Project Roles.
- **Phase 3 — Task Engine** (Sprints 4–6): Core Tasks, Subtasks, Task Assignment, Checklists, Recurring Tasks.
- **Phase 4 — Collaboration & Communication** (Sprints 7–8): Task Comments, Mentions, Attachments, Activity Log, Real-time Notifications.
- **Phase 5 — Planning & Dashboard** (Sprints 9–10): Calendar View, Central Metrics Dashboard, Hardening Pass.

## Sprint Breakdown

1. **Sprint 01 — Authentication** *(Completed)*: Identity setup, JWT RS256, Refresh token rotation with reuse detection, Registration, Password reset, Google login, MFA, MVC Auth views.
2. **Sprint 02 — User Management & Workspace**: Profile editing, User Preferences, Active session revocation, Workspace CRUD, Invites & Joining.
3. **Sprint 03 — Project & Project Members**: Project creation, status/dates/budget, Project Members, role assignments, join requests.
4. **Sprint 04 — Task (Core)**: Task entity, priority/status management, due dates, estimated/actual hours, task list & detail views.
5. **Sprint 05 — Subtasks & Task Assignment**: Parent-child subtask hierarchy, status constraints, multi-assignees, task watchers.
6. **Sprint 06 — Checklist & Recurring Tasks**: Checklist items per task, Hangfire background jobs for recurring task auto-generation.
7. **Sprint 07 — Comments & Attachments**: Task discussion comments, @mentions, file storage service, attachment uploads/previews.
8. **Sprint 08 — Activity Timeline & Notifications**: Event audit log, SignalR real-time activity hub, in-app notification center & email alerts.
9. **Sprint 09 — Calendar View**: FullCalendar drag-and-drop integration for tasks, start/due dates, month/week/day views.
10. **Sprint 10 — Default Dashboard**: Central dashboard widgets, workload summary, project status metrics, final UI polish.
