# Webhooks

## v1 stance: outgoing only, internal-consumer-oriented

The functional spec does not name a specific third-party webhook requirement; this doc defines a conservative, genuinely useful v1 shape rather than inventing an unbounded integration platform (see [scope.md](scope.md) — public developer API/incoming webhooks are explicitly a v2 candidate).

## Outgoing webhooks

An Organization (Admin+ only, `ManageUsers`-tier permission) can register one or more webhook subscriptions:

```
POST api/v1/Webhooks/Subscribe
{
  "url": "https://example.com/hooks/taskplatform",
  "events": ["task.created", "task.status_changed", "milestone.completed"],
  "secret": "<shared secret the receiver uses to verify the signature>"
}
```

### Supported events (v1)

| Event | Fired when | Payload highlights |
|---|---|---|
| `task.created` | A Task is created (Module 10) | TaskId, ProjectId, Title, PrimaryAssigneeId |
| `task.status_changed` | BR-13-01/BR-20-02-validated status transition succeeds | TaskId, OldStatus, NewStatus, ActorUserId |
| `task.assigned` | Module 12 assignment change | TaskId, AssigneeId (user or team) |
| `milestone.completed` | Computed Completion% (BR-09-01) reaches 100% | MilestoneId, ProjectId |
| `project.created` | Module 7 | ProjectId, Name |
| `comment.created` | Module 16 | CommentId, EntityType, EntityId, AuthorUserId |

### Delivery mechanics

- Fire-and-forget from a Hangfire background job (never inline in the request that caused the event) — a slow/dead receiver endpoint must never add latency to the actual user-facing action.
- HMAC-SHA256 signature of the raw payload using the subscription's `secret`, sent as an `X-TaskPlatform-Signature` header — receiver verifies before trusting the payload.
- Retry: 3 attempts with exponential backoff (1m, 5m, 15m); after 3 failures the subscription is marked `Unhealthy` and surfaced in its own settings screen — not silently retried forever, and not silently dropped either.
- Every delivery attempt (success or failure, with response status/latency) is logged to a `WebhookDeliveryLog` table for the Organization's own visibility/debugging.

## Incoming webhooks

None in v1. The only "incoming" integration is Google OAuth's callback ([third-party-integrations.md](third-party-integrations.md)), which is an auth flow, not a webhook in this sense.

## Relationship to the AI Meeting Notes module

Module 29 (AI Meeting Notes) accepts an **uploaded file** (transcript/PDF/DOCX), not a webhook push from a meeting tool (Zoom/Teams/etc.) — explicitly narrower than "integrate with a meeting platform," matching exactly what the spec asks for (FR-29-1) and nothing more. A future "auto-pull transcripts from Zoom" integration would be a genuinely new, larger piece of work — track as a v2 candidate in [scope.md](scope.md) if requested, don't build it opportunistically alongside the file-upload version.

## Security notes

- Webhook URLs are validated against a private-IP/localhost blocklist at registration time (basic SSRF mitigation) — a webhook subscription cannot point at `TaskPlatform.Api`'s own internal network.
- Secrets are stored encrypted at rest, same as any other credential (see [configuration.md](configuration.md)).
