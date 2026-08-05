# TaskPlatform — Sprint Master Plan (Customized Scope)

Index and tracking board for building TaskPlatform, customized strictly to the 16 core requested modules (Authentication, User Management, Workspace, Project, Project Members, Task, Subtasks, Task Assignment, Checklist, Recurring Tasks, Comments, Attachments, Activity Timeline, Notifications, Calendar, and Default Dashboard).

## Streamlined 10-Sprint Table

| # | Sprint File | Phase / Focus | Modules Included | Status | Est. Days |
|---|---|---|---|---|---|
| 01 | `Sprint-01-Plan.md` | Foundation | 1 — Authentication | **Completed** | 6 (Act. 1) |
| 02 | `Sprint-02-Plan.md` | Foundation & Workspaces | 2 — User Management, 4 — Workspace | **Completed** | 5 (Act. 1) |
| 03 | `Sprint-03-Plan.md` | Projects & Members | 7 — Project, 8 — Project Members | **Completed** | 5 (Act. 1) |
| 04 | `Sprint-04-Plan.md` | Task Engine | 10 — Task (Core) | **Completed** | 5 (Act. 1) |
| 05 | `Sprint-05-Plan.md` | Task Structure & Assignment | 11 — Subtasks, 12 — Task Assignment | **Completed** | 5 (Act. 1) |
| 06 | `Sprint-06-Plan.md` | Checklists & Recurring | 14 — Checklist, 15 — Recurring Tasks | **Completed** | 4 (Act. 1) |
| 07 | `Sprint-07-Plan.md` | Collaboration & Attachments | 16 — Comments, 17 — Attachments | **Completed** | 5 (Act. 1) |
| 08 | `Sprint-08-Plan.md` | Timeline & Notifications | 18 — Activity Timeline, 19 — Notifications | **Completed** | 5 (Act. 1) |
| 09 | `Sprint-09-Plan.md` | Planning View | 21 — Calendar View | **Completed** | 4 (Act. 1) |
| 10 | `Sprint-10-Plan.md` | Dashboard & Polish | 24 — Default Dashboard + Hardening Pass | **Completed** | 5 (Act. 1) |

**Total: 49 working days ≈ 10 calendar weeks** for solo developer execution.

## Module Dependency & Load-Bearing Chain

```
Sprint 01: Authentication
    └── Sprint 02: User Management & Workspace
            └── Sprint 03: Project & Project Members
                    └── Sprint 04: Task (Core)
                            ├── Sprint 05: Subtasks & Task Assignment
                            ├── Sprint 06: Checklist & Recurring Tasks
                            ├── Sprint 07: Comments & Attachments
                            ├── Sprint 08: Activity Timeline & Notifications
                            ├── Sprint 09: Calendar View
                            └── Sprint 10: Default Dashboard & Final Polish
```

## Tracking

Each `Sprint-0X-Plan.md` opens with a status banner:

```
**Status:** Not Started | In Progress | Completed
**Started:** —          **Completed:** —
**Actual days spent:** —   (vs. Est. days above)
```
