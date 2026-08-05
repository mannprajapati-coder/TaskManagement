# Feature Flags

## Approach

Simple config-driven boolean/tier flags (`FeatureFlags:*` in [configuration.md](configuration.md)), read via `IOptions<FeatureFlagsOptions>` — no third-party feature-flag SaaS (LaunchDarkly, etc.) in v1. The spec's scope (see [scope.md](scope.md)) is bounded enough that a full flagging platform would be more infrastructure than the actual need justifies; revisit if flag count/rollout complexity grows materially past v1.

## Flags (planning-stage list — add to this table in the same PR that introduces a real flag)

| Flag | Default | Purpose | Removal condition |
|---|---|---|---|
| `FeatureFlags:MfaEnabled` | `true` | Gates whether `Auth/EnableMfa` (FR-01-7) is exposed at all — spec marks MFA "(Optional)", flag lets it be disabled org-wide without a code change if it's causing support load early on. | Remove once MFA has shipped stably for a full milestone with no rollback need. |
| `FeatureFlags:AiSmartSchedulerAutoApply` | `false`, and hardcoded false regardless of this flag per ADR in [ai-usage-guidelines.md](ai-usage-guidelines.md) | Reserved for a possible future "let the scheduler apply suggestions automatically" mode — **not wired to actually bypass the human-confirmation requirement in v1**; exists as a placeholder so the eventual decision has a named flag rather than a scattered code change. | N/A until the underlying policy decision (see [scope.md](scope.md) "AI Smart Scheduler auto-apply") changes. |
| `FeatureFlags:GanttEditable` | `false` | Gates drag-to-reschedule on the Gantt chart (deferred per [scope.md](scope.md)) — flipping this on is the intended activation switch once that work is actually built, not a rollout mechanism for something half-built. | Remove once Gantt editing ships and has been stable for a milestone. |
| `FeatureFlags:PushNotifications` | `false` | Module 19's spec-marked "(Future)" channel — placeholder so `NotificationDelivery.Channel` can include `Push` in the enum from day one (cheap to reserve) without the code path being reachable. | Remove once a mobile app exists to receive push and the channel is actually implemented. |
| `FeatureFlags:PublicWebhooksApi` | `false` | Gates any future incoming-webhook/public-API-key surface beyond the outgoing-only v1 shape in [webhooks.md](webhooks.md). | Remove once (if) that v2 candidate is scoped and built. |

## Rules

- A flag defaults to the *safe/off* state for anything not yet fully built — a flag is never used to hide a half-finished feature that's reachable by a determined user; if it's flagged off, the code path is genuinely gated server-side (see [auth.md](auth.md)'s layered-check discipline — a feature flag check is an additional layer, not a replacement for the permission checks).
- Every flag has a stated **removal condition** in the table above — a flag with no plan to ever become permanent-on-or-deleted is a smell, not a feature.
- Flags are read server-side only (`TaskPlatform.Api`); `TaskPlatform.Web` learns a flag's state from an API response (e.g. a "capabilities" field on the user's session response), never from its own independent config — one source of truth, matching the rest of this design's "Web never re-implements a decision Api already made" principle (see ADR-002).
- Per-Organization overrides (e.g. one Organization opts into `GanttEditable` early) are an explicit v2 extension of this table's shape, not built speculatively now — v1 flags are global/environment-level only.
