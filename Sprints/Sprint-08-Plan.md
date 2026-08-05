# Sprint 08 — Activity Timeline & Notifications (Modules 18 & 19)

**Status:** Not Started
**Started:** —          **Completed:** —
**Actual days spent:** —   (Est. 5 days)

## Objective

Build `Activity Timeline` (Module 18) audit logging and SignalR real-time & email `Notifications` (Module 19).

## Included features / Requirements covered

- **Module 18**: Activity Audit Log (`Tasks/{id}/Activity`, `Projects/{id}/Activity`).
- **Module 19**: Notification Center (`Notifications/GetMyNotifications`, `Notifications/MarkAsRead`), SignalR `ActivityHub` live alerts, Email notifications for assignees/watchers (BR-12-02).

## Task breakdown

1. **Activity Log Entity & Interceptor** — Create `ActivityLogEntry` entity and automatic EF Core audit logger for task/project mutations.
2. **Notification Entity & SignalR Hub** — Create `Notification` entity, `ActivityHub` SignalR hub in `TaskPlatform.Api`, and notification dispatcher.
3. **API & Web UI** — Real-time notification bell dropdown in header, toast alerts, and task activity history tab.
4. **Tests** — Unit tests for notification event dispatching and watcher filter (BR-12-02).

## Dependencies

- Sprint 07 — Comments & Attachments

## Deliverables

- Migration `AddTimelineAndNotificationsSchema`.
- Real-time SignalR notifications & task activity audit trail.

## Acceptance criteria

- [ ] Task mutations produce structured activity audit logs.
- [ ] Task assignees and watchers receive real-time SignalR alerts and email notifications on key events (BR-12-02).
