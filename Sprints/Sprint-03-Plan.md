# Sprint 03 — Project & Project Members (Modules 7 & 8)

**Status:** Completed
**Started:** 2026-08-05          **Completed:** 2026-08-05
**Actual days spent:** 1   (Est. 5 days)

## Objective

Build `Project` (Module 7) creation/management and `Project Members` (Module 8) membership, role assignment, and join request features inside Workspaces.

## Included features / Requirements covered

- **Module 7**: Project CRUD (`Projects/Create`, `Projects/GetById`, `Projects/Update`, `Projects/Archive`, `Projects/ToggleFavorite`).
- **Module 8**: Project Member management (`Projects/AddMembers`, `Projects/RemoveMembers`, `Projects/UpdateMemberRole`), Join Requests (`Projects/SubmitJoinRequest`, `Projects/ResolveJoinRequest`).

## Task breakdown

1. **Entities & DbContext** — Create `Project`, `ProjectFavorite`, `ProjectMember`, `ProjectJoinRequest` entities in `Modules/Projects`. Configure `ProjectsDbContext` with tenant global query filters (`OrganizationId`).
2. **`ProjectsService` & API** — Implement `IProjectService` handling project creation, date validation (BR-07-01), favoriting, member role assignment, and join request approvals (BR-08-01).
3. **`ProjectsController` API** — Expose REST endpoints under `api/v1/Projects`.
4. **Web MVC Controllers & Views** — `ProjectController.cs` in `TaskPlatform.Web` + `Index.cshtml`, `Detail.cshtml`, `Create.cshtml`, `Members.cshtml`.
5. **Tests** — Unit tests for BR-07-01 (`EndDate >= StartDate`) and BR-08-01 (Join request approval defaults to lowest create role).

## Dependencies

- Sprint 02 — User Management & Workspace

## Deliverables

- `ProjectsDbContext` migration `AddProjectsAndMembersSchema`.
- Functional API & Web UI for project creation, favoriting, and project member management.

## Acceptance criteria

- [x] Projects can be created within a Workspace with valid date range checks (BR-07-01).
- [x] Users can favorite projects for quick navigation.
- [x] Project members can be added with specific roles, and join requests can be submitted and resolved (BR-08-01).
