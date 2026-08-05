# Error Handling

## Global exception middleware

`TaskPlatform.Api/Middleware/ExceptionHandlingMiddleware.cs` (mirrors this workspace's other product's own middleware pattern) is the single place an unhandled exception becomes an HTTP response — controllers should rarely `try/catch` themselves (see [coding-standards.md](coding-standards.md)).

## Exception → HTTP status mapping

| Exception type (`TaskPlatform.Shared/Exceptions`) | HTTP status | When thrown |
|---|---|---|
| `ValidationException` (FluentValidation) | 400 | A business-rule validation failure not expressible as a DataAnnotation (see [business-rules.md](business-rules.md)) |
| *(DataAnnotations / `ModelState` invalid)* | 400 | `[ApiController]`'s automatic behavior, before the action body runs at all |
| `DomainException` | 400 | A domain invariant violation with a specific, user-facing message (e.g. BR-13-02's cycle detection) |
| `PermissionDeniedException` | 403 | Authorization layer 2/3 failure (see [auth.md](auth.md)) — the caller is authenticated but not allowed to do *this* |
| `NotFoundException` | 404 | Resource id doesn't resolve to a row the caller can see (deliberately the same response whether the row doesn't exist at all or exists in another tenant — never leak "it exists, just not for you" via a different status code, see security note below) |
| `ConflictException` | 409 | Optimistic concurrency conflict (e.g. two users editing the same Task's status simultaneously) or a uniqueness violation surfaced as a friendly message |
| *(unhandled/unexpected)* | 500 | Logged at `Error` with full stack trace server-side; response body never includes the stack trace or exception message, only a generic message + a `TraceId` the user can quote to support |

## Response envelope

Matches [api-conventions.md](api-conventions.md):

```json
{ "success": false, "data": null, "errors": [{ "field": "DueDate", "message": "..." }], "traceId": "00-abc123..." }
```

`field` is `null` for errors that aren't tied to one specific input (permission denied, not found, conflict) — the frontend distinguishes field-level errors (render inline on the form) from whole-request errors (render as a banner/toast) by whether `field` is present.

## Security note — information disclosure

- 403 vs 404 is deliberately collapsed to 404 for any resource-existence question that would otherwise let a caller enumerate what exists in another Organization ("is Task #4821 real" should never be answerable differently depending on whether it's real-but-forbidden vs. truly nonexistent).
- 500 responses never include exception details, connection strings, or stack traces in the body — full detail goes to the log sink only (see [logging-monitoring.md](logging-monitoring.md)), correlated by `TraceId`.
- Validation error messages never echo back anything that could leak whether an email/username is already registered in a way that aids account enumeration (`Auth/Register`'s "email already in use" message is deliberately identical in timing/wording regardless of whether the email exists, per standard practice — flagged here as a specific thing to get right on that one endpoint rather than assumed obvious).

## Retry / transient fault handling

- Outbound calls to Redis, the AI provider, and email/SMTP use Polly-based retry with exponential backoff + circuit breaker (see [tech-stack.md](tech-stack.md) implied resilience layer) — a transient Redis blip degrades gracefully (cache-aside pattern in [caching-strategy.md](caching-strategy.md) already tolerates a cache-unavailable state by falling through to the DB) rather than surfacing as a 500 to the end user.
- Webhook delivery has its own specific retry policy — see [webhooks.md](webhooks.md), not duplicated here.

## Client-side (TaskPlatform.Web) handling

- `ApiService` (see [folder-structure.md](folder-structure.md)) centralizes reading the envelope above — every controller in `Web` gets a parsed, typed error rather than each one re-parsing JSON. This is the exact fix this workspace's other product had to apply after discovering its own error handler only ever looked for a `"message"` property and silently swallowed `[ApiController]`'s real `ValidationProblemDetails`/`errors`-dictionary shape — `TaskPlatform.Shared`'s `ApiService` is built against the envelope in [api-conventions.md](api-conventions.md) from day one specifically so that class of bug has nothing to hide behind here.
- User-facing error display: inline field errors on forms, a toast/banner for whole-request errors, and a distinct friendly full-page error for "Api is unreachable at all" (see [environment-setup.md](environment-setup.md) troubleshooting table) rather than a raw connection-refused stack trace.

## Rate limiting responses

429, with a `Retry-After` header, backed by the Redis counters in [caching-strategy.md](caching-strategy.md) — applied most strictly to `Auth/Login`/`Auth/ForgotPassword` (credential-stuffing/enumeration protection) and to AI endpoints (cost protection, see [ai-usage-guidelines.md](ai-usage-guidelines.md)).
