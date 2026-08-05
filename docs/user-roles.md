# User Roles & Permission Matrix

## Roles (Module 6, spec-defined, org-wide default)

| Role | Typical holder | One-line intent |
|---|---|---|
| **Owner** | Organization creator | Everything Admin can do, plus billing, subscription, and org deletion/transfer — exactly one Owner per Organization at a time. |
| **Admin** | Org/IT administrator | Full operational control of the Organization short of billing/ownership transfer. |
| **Project Manager** | Runs one or more projects | Full control within their own Projects (create/edit/archive, milestones, members) — not org-wide user/billing management. |
| **Team Lead** | Leads a functional Team | Manages their Team's membership and assignments; project-level rights only where added as a Project Member. |
| **Developer** | Does the work | Create/edit/comment/log time on Tasks assigned to or visible within their Projects; cannot delete Projects or manage users. |
| **Tester** | QA | Same base rights as Developer; distinguished in reporting (Module 25's "Employee Performance") and typically the role that moves a Task from "Testing" → "Done" on the Kanban board — not a different permission set from Developer in v1 (see note below). |
| **Viewer** | Stakeholder who needs visibility, not editing | Read-only across everything they're a member of: can view boards, reports, comments — cannot create/edit/comment. |
| **Guest** | External collaborator, client | Read-only, same as Viewer, **plus** commenting — the one write action a Guest has, so they can participate in discussion without touching data (see [scope.md](scope.md) assumption #4). |

**Note on Developer vs. Tester:** the spec lists them as separate roles but gives no differing permission, only differing reporting semantics. They share one `PermissionSet` in [database-schema.md](database-schema.md); the distinction is carried as a `Role` label for reporting/filtering (Module 25), not a second permission table. If a real behavioral difference emerges during build, split them then — don't invent one now.

## Permissions (Module 6, spec-defined)

| Permission key | Meaning |
|---|---|
| `CreateProject` | Create a new Project within a Workspace. |
| `DeleteProject` | Archive/delete a Project (soft-delete, see [database-schema.md](database-schema.md)). |
| `AssignTask` | Assign/reassign a Task to a user or team. |
| `ManageUsers` | Invite/remove Organization or Workspace members, change their Role. |
| `ExportReports` | Export a Report to PDF/Excel/CSV (Module 25). |
| `ManageBilling` | View/change Subscription tier, billing details. |
| `ViewReports` | View (not export) Reports and Dashboard widgets. |

## Role → Permission matrix

| Permission | Owner | Admin | Project Manager | Team Lead | Developer | Tester | Viewer | Guest |
|---|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|
| CreateProject | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| DeleteProject | ✅ | ✅ | ✅ *(own projects only)* | ❌ | ❌ | ❌ | ❌ | ❌ |
| AssignTask | ✅ | ✅ | ✅ | ✅ *(own team)* | ❌ | ❌ | ❌ | ❌ |
| ManageUsers | ✅ | ✅ | ❌ | ❌ *(own team members only — see below)* | ❌ | ❌ | ❌ | ❌ |
| ExportReports | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| ManageBilling | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| ViewReports | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |

Everything not in this table (create/edit Task, Comment, Attachment, log time, etc.) is implicitly allowed for Owner/Admin/PM/Team Lead/Developer/Tester wherever they are a *member* of the relevant Project, and denied for Viewer/Guest. This is enforced by `RoleClaimAuthorizeFilter`-equivalent middleware server-side on every `TaskPlatform.Api` action (see [auth.md](auth.md)) — the matrix above is the source of truth the filter is generated/checked against, not the UI's own judgment call.

**Team Lead's `ManageUsers` is scoped, not the base permission** — a Team Lead can add/remove members of *their own* Team (Module 5's own "Add/Remove Members") without holding the org-wide `ManageUsers` permission. This is modeled as a resource-scoped check (`IsTeamLeadOf(teamId)`) layered on top of the base RBAC table, exactly the same shape as Project Members' (Module 8) project-scoped role overrides — see [business-rules.md](business-rules.md) BR-06-01.

## Project-level and Team-level overrides (Modules 5 & 8)

A user's effective permission on a given Project is: **base Role permission** (table above) **overridden by** any Project-scoped role assignment (Module 8's "Assign Role" on a `ProjectMember`) **overridden by** any Team-scoped permission override (Module 5's "Team Permissions") that applies to that Project via Team membership. Most-specific-wins. This is one permission system with two layers of scoped override, not three independent systems — see [scope.md](scope.md) assumption #2 and ADR reasoning in [design-decisions.md](design-decisions.md).

## Special cases

- **Owner is singular and non-deletable while holding the role.** Transfer Ownership (Module 4/3) is the only way to change who holds it; there is no "delete the Owner" action.
- **A Guest is always Project-scoped**, never invited at the Workspace or Organization level — there is no such thing as an org-wide Guest.
- **A user can hold different roles on different Projects simultaneously** (e.g., Project Manager on Project A, Developer on Project B) via Module 8's per-project role assignment — the Organization-level Role (Module 6) is the *default* a new Project Membership starts from, not a ceiling or floor on what a project-scoped assignment can grant, except that no override can grant `ManageBilling`/org-level `ManageUsers` to anyone but Owner/Admin — those two stay org-level-only regardless of project overrides.
