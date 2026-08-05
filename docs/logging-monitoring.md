# Logging & Monitoring

## Logging

- **Library**: Serilog (see [tech-stack.md](tech-stack.md)), same as this workspace's other product — structured (JSON), never plain string interpolation into a log message (`Log.Information("Task {TaskId} created by {UserId}", taskId, userId)`, not `$"Task {taskId} created"`).
- **Sinks**:
  - Dev: Console + rolling file (`Logs/log-{Date}.txt`).
  - Staging/Prod: Console (captured by the host) + Seq or Application Insights (see [third-party-integrations.md](third-party-integrations.md)) for queryable structured search and alerting.
- **Levels**: `Debug` for dev-only diagnostic detail (off by default in Staging/Prod), `Information` for every business-significant event (created/updated/deleted, login, permission denied), `Warning` for recoverable anomalies (a webhook delivery failure, a cache miss on something that shouldn't miss), `Error` for anything that reached the global exception middleware ([error-handling.md](error-handling.md)), `Fatal` for anything that took the process down.
- **Correlation ID**: every request gets a `TraceId` (ASP.NET Core's built-in `Activity.Current.Id`, or a custom `X-Correlation-Id` header propagated from `TaskPlatform.Web` through to `TaskPlatform.Api`) attached to every log line for that request — NFR-9. Lets a single user-reported issue be traced end-to-end across both processes.
- **What's never logged**: raw passwords, full JWTs/refresh tokens, MFA secrets, file contents. Emails/names are logged (needed for support debugging) but flagged if this product ever needs GDPR-style "right to be forgotten" log scrubbing — tracked as an open question in [faq.md](faq.md), not solved speculatively now.

## Audit log vs. application log

**`AuditLogEntry`** ([database-schema.md](database-schema.md), Module 3) is a *business-facing*, permanent, queryable-by-an-Admin record of sensitive actions (role changes, billing changes, member removal, ownership transfer) — it is a domain table, not a Serilog sink, and it is never subject to log-retention deletion (below). **Application logs** (Serilog) are an *operational* diagnostic tool for engineers — different audience, different retention, never conflated into one system.

## What's monitored

| Signal | Tool | Alert threshold (starting point, tune after real traffic) |
|---|---|---|
| API error rate (5xx) | Seq/App Insights query or dashboard | > 1% of requests over 5 min |
| API P95 latency | Same | > 500ms sustained over 5 min (NFR-3) |
| Failed login rate (possible credential-stuffing) | Serilog `Warning`-level login-failure events, aggregated | > N failures/min from a single IP (specific N tuned once there's real baseline traffic) |
| Webhook delivery failure rate | `WebhookDeliveryLog` (see [webhooks.md](webhooks.md)) | Any subscription reaching `Unhealthy` |
| Hangfire job failures (recurring task generation, AI nightly precompute) | Hangfire dashboard + a Serilog `Error` on job failure | Any failure — these are silent-by-default background jobs, a failure here has no other visible symptom until a user notices missing data days later |
| SignalR connection count / Redis backplane health | App Insights / Redis metrics | Connection count dropping to zero unexpectedly, or Redis latency spike |
| SQL Server DTU/CPU (if Azure SQL) | Azure Monitor | > 80% sustained |

## Retention

- Application logs: 30 days hot (queryable), 90 days cold/archived, then deleted.
- `AuditLogEntry`/`ActivityLogEntry` (domain tables, see [database-schema.md](database-schema.md)): retained indefinitely in v1 — these are product data, not operational logs, and `ActivityLogEntry`'s own indexing strategy (§"Indexing notes" in database-schema.md) already anticipates this table growing large. Revisit an archival strategy once real volume data exists, not speculatively now.

## Health checks

`GET /health` on both `TaskPlatform.Api` and `TaskPlatform.Web`, checking DB connectivity, Redis connectivity, and (Api only) the configured `IAIProvider`'s reachability (a lightweight ping, not a real generation call) — used by the deployment pipeline's smoke-check ([deployment.md](deployment.md)) and by any external uptime monitor.
