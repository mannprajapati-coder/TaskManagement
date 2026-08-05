# Sprint 05 — Subtasks & Task Assignment (Modules 11 & 12)

**Status:** Completed
**Started:** 2026-08-05          **Completed:** 2026-08-05
**Actual days spent:** 1   (Est. 5 days)

## Objective

Extend tasks with parent-child `Subtasks` (Module 11) tree structures and multi-user `Task Assignment` & Watchers (Module 12).

## Included features / Requirements covered

- **Module 11**: Subtask tree CRUD (`Tasks/{id}/Subtasks`, `Tasks/{id}/DeleteWithSubtasks`).
- **Module 12**: Multi-assignee assignment (`Tasks/{id}/Assignees`), Primary assignee auto-sync (BR-12-01), Task Watchers (`Tasks/{id}/Watchers`).

## Task breakdown

1. **Subtask Self-Reference & Validation** — Wire `ParentTaskId` self-referencing navigation on `Task` entity. Implement parent completion constraint (BR-11-01: parent cannot be marked Done with incomplete subtasks) and deletion cascade rule (BR-10-02).
2. **Task Assignee & Watcher Join Entities** — Create `TaskAssignee` and `TaskWatcher` join tables. Implement primary assignee auto-addition (BR-12-01).
3. **API & Web UI Updates** — Add subtask inline tree UI and multi-assignee selection dropdowns in task detail view.
4. **Tests** — Unit tests for BR-11-01 (parent done block) and BR-12-01 (primary assignee auto-sync).

## Dependencies

- Sprint 04 — Task Engine

## Deliverables

- Migration `AddSubtasksAndAssignmentsSchema`.
- Subtask hierarchy and multi-assignee/watcher management working end-to-end.

## Acceptance criteria

- [x] Parent tasks cannot be completed while incomplete subtasks remain (BR-11-01).
- [x] Primary assignee is automatically added to the co-assignee list if not already present (BR-12-01).
