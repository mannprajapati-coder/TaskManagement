# Sprint 09 — Calendar View (Module 21)

**Status:** Completed
**Started:** 2026-08-05          **Completed:** 2026-08-05
**Actual days spent:** 1   (Est. 4 days)

## Objective

Build `Calendar` view (Module 21) integrating FullCalendar JS library for visual task scheduling across month, week, and day views with drag-and-drop due date updates.

## Included features / Requirements covered

- **Module 21**: Calendar Task Feed (`Calendar/GetEvents`), Drag-and-drop due date rescheduling (`Calendar/UpdateEventDate`).

## Task breakdown

1. **Calendar API Endpoint** — Endpoint returning task events formatted for FullCalendar (Title, Start, End/DueDate, Status, Priority, Project).
2. **FullCalendar Integration** — Integrate FullCalendar JS client in `TaskPlatform.Web` with view filters (by Workspace, Project, Assignee).
3. **Interactive Rescheduling** — Handle drag-and-drop event move to update task start/due dates via AJAX API call.
4. **Tests** — Unit tests for calendar event payload mapping and date range query filters.

## Dependencies

- Sprint 04 — Task Engine

## Deliverables

- Interactive Calendar page (`Calendar/Index.cshtml`) with FullCalendar drag-and-drop task scheduling.

## Acceptance criteria

- [ ] Users can view tasks on Month, Week, and Day calendar layouts.
- [ ] Dragging a task to a new date updates its due date seamlessly.
