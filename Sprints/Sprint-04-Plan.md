# Sprint 04 — Task Engine (Module 10)

**Status:** Completed
**Started:** 2026-08-05          **Completed:** 2026-08-05
**Actual days spent:** 1   (Est. 5 days)

## Objective

Build the central `Task` core engine (Module 10) supporting task creation, priority and status workflows, due dates, estimated vs actual hours, and task filtering.

## Included features / Requirements covered

- **Module 10**: Task CRUD (`Tasks/Create`, `Tasks/GetById`, `Tasks/Update`, `Tasks/UpdateStatus`, `Tasks/Delete`, `Tasks/GetByProject`).

## Task breakdown

1. **Task entity & `TasksDbContext`** — Create `TaskEntity` (Title, Description, Status, Priority, StartDate, DueDate, EstimatedHours, ActualHours, PrimaryAssigneeId, ProjectId) in `Modules/Tasks`.
2. **`TasksService` & API** — Implement core task operations, status state transitions, and actual hours computation (BR-10-01).
3. **`TasksController` API** — REST endpoints under `api/v1/Tasks`.
4. **Web MVC Views** — Task list, task creation form, and task detail view in `TaskPlatform.Web`.
5. **Tests** — Unit tests for task status transitions and hour calculation logic (BR-10-01).

## Dependencies

- Sprint 03 — Project & Project Members

## Deliverables

- `TasksDbContext` migration `AddCoreTasksSchema`.
- Full CRUD API & MVC UI for core tasks.

## Acceptance criteria

- [x] Users can create, update, filter, and delete tasks within a project.
- [x] Status updates transition correctly and update task timestamps.
