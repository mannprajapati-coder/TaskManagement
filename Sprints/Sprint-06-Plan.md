# Sprint 06 — Checklist & Recurring Tasks (Modules 14 & 15)

**Status:** Not Started
**Started:** —          **Completed:** —
**Actual days spent:** —   (Est. 4 days)

## Objective

Build `Checklist` (Module 14) item management within tasks and automated `Recurring Tasks` (Module 15) rules with Hangfire background jobs.

## Included features / Requirements covered

- **Module 14**: Task Checklist CRUD (`Tasks/{id}/Checklists`, `Checklists/{id}/Items`, toggle item).
- **Module 15**: Recurring Task Rule setup (`Tasks/{id}/RecurringRule`) & Hangfire auto-generator job.

## Task breakdown

1. **Checklist Entities** — Create `Checklist` and `ChecklistItem` entities.
2. **Recurring Task Rule Entity & Job** — Create `RecurrenceRule` entity (Daily, Weekly, Monthly, Custom cron). Configure Hangfire job `RecurringTaskGenerator` to automatically generate next task instance on schedule.
3. **API & Web UI** — Interactive checklist UI on task modal and recurrence schedule picker.
4. **Tests** — Unit tests for checklist completion percentage and recurring task next-date calculation.

## Dependencies

- Sprint 04 — Task Engine

## Deliverables

- Migration `AddChecklistsAndRecurringTasksSchema`.
- Working task checklists and automated background recurring task generator.

## Acceptance criteria

- [ ] Users can add checklists and toggle items on tasks.
- [ ] Tasks configured with recurrence rules automatically spawn next instances via Hangfire background job.
