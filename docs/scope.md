# Project Scope (Customized Scope)

This document defines the in-scope and out-of-scope modules for the customized TaskPlatform build.

## In-Scope Modules (16 Core Modules)

1. **Module 1 — Authentication**: Register, Login, JWT RS256, Refresh token rotation, Password reset, Google Login, MFA.
2. **Module 2 — User Management**: Profile, Preferences, Session Management.
3. **Module 4 — Workspace**: Workspace CRUD, Invites & Joining.
4. **Module 7 — Project**: Project creation, status, dates, client, favorites.
5. **Module 8 — Project Members**: Project member assignment, project roles & overrides.
6. **Module 10 — Task**: Core task entity, priority, status, dates, hours.
7. **Module 11 — Subtasks**: Parent-child task tree hierarchy.
8. **Module 12 — Task Assignment**: Multi-assignees, primary assignee, watchers.
9. **Module 14 — Checklists**: Task checklists and items.
10. **Module 15 — Recurring Tasks**: Recurring task rules and automated background generation.
11. **Module 16 — Comments**: Task comments and @mentions.
12. **Module 17 — Attachments**: File storage & attachment management.
13. **Module 18 — Activity Timeline**: Audit and activity logging.
14. **Module 19 — Notifications**: SignalR real-time & email notifications.
15. **Module 21 — Calendar**: FullCalendar month/week/day task scheduling view.
16. **Module 24 — Default Dashboard**: Central overview dashboard metrics and widgets.

## Out-of-Scope Modules

- Module 3 — Organization (handled via minimal tenant container)
- Module 5 — Team Management
- Module 6 — Role & Permission matrix
- Module 9 — Milestones
- Module 13 — Task Dependencies
- Module 20 — Kanban Board
- Module 22 — Gantt Chart
- Module 23 — Time Tracking
- Module 25 — Custom Reports
- Modules 26–30 — All AI Modules (AI Assistant, AI Generator, Smart Scheduler, Meeting Notes, Analytics)
