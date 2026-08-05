# UI Guidelines

## No Figma file exists yet for TaskPlatform

Unlike this workspace's other product (which had a real Figma export to port screen-for-screen), `Project_Functional_Specification.docx` names no design file — this doc defines a starting design-token set to unblock building Phase 1 screens, **not** a final visual design. Treat every value below as a placeholder to be swapped for a real design system the moment one exists (see [faq.md](faq.md) open question), the same way this repo's own Figma-sourced theme superseded an earlier placeholder theme for its other product. Do not invest in pixel-perfect polish against these placeholder tokens.

## Design tokens (placeholder, until a real design system exists)

```css
:root {
  /* brand */
  --color-primary: #2F6FED;        /* placeholder blue, swap when real branding exists */
  --color-primary-hover: #2557C2;
  --color-success: #1FA971;        /* Done / completed */
  --color-warning: #E0A100;        /* WIP-limit warning (BR-20-01), overdue */
  --color-danger: #D64545;         /* blocked (BR-13-01), destructive actions */
  --color-info: #3B8FD6;

  /* neutrals */
  --color-bg: #F7F8FA;
  --color-surface: #FFFFFF;
  --color-border: #E2E5EA;
  --color-text: #1B1F27;
  --color-text-muted: #6B7280;

  /* spacing (4px base scale) */
  --space-1: 4px;  --space-2: 8px;  --space-3: 12px;
  --space-4: 16px; --space-6: 24px; --space-8: 32px;

  /* typography */
  --font-family: 'Inter', -apple-system, Segoe UI, sans-serif;
  --font-size-sm: 13px; --font-size-base: 14px; --font-size-lg: 16px; --font-size-xl: 20px;

  /* radius */
  --radius-sm: 4px; --radius-md: 8px; --radius-lg: 12px;
}
```

## Component mapping (spec feature → component)

| Feature | Component pattern | Notes |
|---|---|---|
| Kanban Board (Module 20) | `.board` / `.board-column` / `.board-card` | SortableJS-driven drag-drop, see [tech-stack.md](tech-stack.md)/ADR-007; column header shows a WIP-limit badge that turns `--color-warning` when exceeded (BR-20-01). |
| Calendar (Module 21) | FullCalendar themed via its CSS var overrides to match the tokens above | Day/Week/Month toggle as a segmented control. |
| Gantt Chart (Module 22) | Frappe Gantt themed the same way | Read-only in v1 — bars are not draggable; dependency lines rendered as the library's native arrows. |
| Status badges (Task/Milestone/Subscription/Board) | `.status-badge` + a modifier class per status (`.status-badge-done`, `.status-badge-blocked`, ...) | One consistent badge component reused everywhere a status appears — dashboard, board, task detail, reports — never a bespoke one-off per screen. |
| Task/Project card (list views) | `.entity-card` | The one reusable "structured record" card pattern for anything that's a titled thing with a few key-value facts (matches the reuse principle already proven useful elsewhere in this workspace — one card pattern, many entity types). |
| Comment thread | `.comment` / `.comment-reply` (nested) | @mentions rendered as `.mention` inline chips; reactions as a small emoji-count row. |
| Modals (confirm delete, accept AI plan, etc.) | `.modal` built on Bootstrap 5's real `bootstrap.Modal` JS API | Vanilla-JS trigger, not a jQuery `.modal()` call — avoids the exact BS5/jQuery mismatch bug already found and fixed once in this workspace's other product. |
| Stat tiles (Dashboard widgets) | `.stat-tile` grid | Consistent number+label+trend-indicator shape across every widget, not a different layout per widget. |

## Accessibility (NFR-7)

- All interactive elements keyboard-reachable, including Kanban drag-drop (provide a keyboard-accessible "Move to..." menu alternative on each card — drag-and-drop alone never satisfies WCAG 2.1 AA).
- Color is never the only signal for status (BR-13-01 "blocked," WIP-limit warning) — always paired with an icon or text label, not color alone, for colorblind users.
- Contrast ratios for the placeholder token set above should be verified against WCAG AA (4.5:1 body text) before this file's tokens are treated as final — flagged here as a to-do specifically because these are placeholder values, not yet audited.

## Internationalization (NFR-8)

UI strings resource-based (`.resx` or a JSON resource file per locale) from the first screen built, even though only English ships in v1 — matches Module 2's own `Language` preference (FR-02-3) actually being meaningful once a second locale exists, rather than hardcoding English strings that would all need retrofitting later.

## Responsive behavior

Kanban/Gantt/Calendar are desktop-first (their information density doesn't meaningfully compress to a phone screen) — the spec doesn't call out mobile web as a requirement, so these three screens explicitly target desktop/tablet widths; simpler screens (task detail, comments, dashboard list widgets) are responsive down to mobile width using Bootstrap's grid, since there's no reason to exclude them.
