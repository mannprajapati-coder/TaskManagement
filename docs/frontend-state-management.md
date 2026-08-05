# Frontend State Management

`TaskPlatform.Web` is server-rendered Razor MVC (ADR-007), not a SPA, so "state management" here means something narrower than Redux/Context — how server state, page-local JS state, and real-time push updates stay consistent without a client-side store.

## Sources of truth, in priority order

1. **The database, via `TaskPlatform.Api`** — always the ultimate source of truth. Every screen's initial render comes from a real API call server-side (`TaskPlatform.Web` controller calls `ApiService`, passes the result to the view) — never client-only mock/placeholder data.
2. **SignalR push (`ActivityHub`)** — a live hint that something changed; triggers a targeted re-fetch of just the affected piece of the page, never trusted as the update itself (see [api-conventions.md](api-conventions.md) "Real-time vs. REST").
3. **Page-local JS state** — the current in-memory shape of, e.g., the Kanban board's DOM order while a drag is in progress, before the `MoveCard` call confirms it. Optimistic: the card moves immediately in the DOM, then rolls back if the API call fails (with a toast explaining why — matches BR-20-02's rejection-with-reason requirement).

## Per-screen patterns

### Kanban Board (Module 20)
- Initial render: full board (columns + cards) from `Kanban/GetBoard` server-side.
- Drag-drop: SortableJS fires a client event → optimistic DOM move → `PUT Kanban/MoveCard` → on success, no-op (DOM already correct); on failure, snap back + toast (BR-20-02).
- Real-time: another user's move arrives via `ActivityHub` → client re-fetches just that one card's current column/position (a small `GET Tasks/GetById/{id}`-style call), not a full board reload — keeps the board responsive under concurrent editing.

### Comments (Module 16)
- New comment posted → optimistic append to the thread DOM → confirmed/replaced with the server's real `CommentId`/timestamp on response.
- Incoming comment from another user (via `ActivityHub`) → appended to the thread live, matching the spec's collaboration intent (Module 16) — this is the one screen where real-time feel matters most, since it's literally a discussion.

### Calendar (Module 21) / Gantt (Module 22)
- Initial render: full range from `Calendar/GetView`/`GanttChart/GetForProject`.
- Calendar drag-reschedule: same optimistic-then-confirm pattern as Kanban, calling `Calendar/RescheduleViaDragDrop`.
- Gantt: read-only in v1 (see [scope.md](scope.md)) — no local mutable state beyond zoom/scroll position, which is UI-only and never persisted server-side.

### Notifications (Module 19)
- Unread badge count: pushed live via `ActivityHub` on every new `Notification`, decremented client-side on `MarkRead`/`MarkAllRead` optimistically, corrected on the next full page load if it ever drifts (never a source of truth in itself — just a fast-feeling counter).

### AI Assistant (Module 26) chat panel
- Streamed response: SignalR (or Server-Sent Events, whichever proves simpler against the chosen `IAIProvider`'s own streaming API — a build-time choice, not fixed here) pushes tokens as they arrive; the full message is persisted to `AiMessage` only once complete, so a page refresh mid-stream shows the last *completed* message, never a truncated one.

## What's explicitly avoided

- **No client-side global store** (no Redux-equivalent, no shared `window.appState` object mutated from multiple scripts) — each page's JS is scoped to that page's own concerns, matching the "no SPA" decision's actual payoff (simplicity) rather than half-adopting SPA-style state management without a SPA's tooling to manage it well.
- **No polling loops** for anything `ActivityHub` already covers — a `setInterval` re-fetch is only used as a last-resort fallback if a SignalR connection genuinely can't be established (logged as a `Warning`, see [logging-monitoring.md](logging-monitoring.md)), not as the primary update mechanism anywhere.

## Form state

Standard server-rendered forms + `jquery.validate` client-side pre-check (matches [tech-stack.md](tech-stack.md)) backed by the same DataAnnotations the server re-validates (see [api-conventions.md](api-conventions.md)) — no separate client-only validation ruleset that could drift from the server's.
