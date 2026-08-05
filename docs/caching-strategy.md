# Caching Strategy

Backing store: Redis (see [tech-stack.md](tech-stack.md)), required in every environment, not dev-optional — SignalR's backplane depends on it as soon as more than one `TaskPlatform.Api` instance exists, which is true from Staging onward.

## What's cached

| Data | Cache key shape | TTL | Invalidation |
|---|---|---|---|
| Milestone Completion % (BR-09-01) | `milestone:{id}:completion` | 5 min | On any Task status change within that Milestone (explicit invalidation, not just TTL expiry) — a stakeholder looking at a Milestone right after a Task completes shouldn't see stale progress. |
| Dashboard widgets (Module 24) | `dashboard:{orgId}:{userId}:{widget}` | 2 min | TTL-only; these are inherently "as of a moment," not required to be instant. |
| Permission matrix (Module 6) | `permissions:matrix` | until app restart / explicit bust | Busted on any `RolePermission` change (rare — the matrix is mostly static, seeded data). |
| Kanban board layout (Module 20) | `board:{projectId}` | 30 sec | Busted on `MoveCard`/`AddColumn`/`UpdateColumn` — short TTL as a fallback only, real-time updates come from SignalR, not from waiting out the cache. |
| AI Analytics predictions (Module 30) | `ai:prediction:{projectId}:{type}` | until next nightly precompute (see [sql.md](sql.md) `sp_AiAnalyticsNightlyPrecompute`) | Explicit `ExpiresAt` column backing this, not a Redis TTL alone — the DB row is the source of truth, Redis is a read-through cache in front of it. |
| Rate-limit counters (all modules, esp. Auth/AI endpoints) | `ratelimit:{userId or ip}:{endpoint}` | sliding window, per-endpoint | Natural expiry. |
| JWT signing key / revocation list | `auth:revoked-token-families` | until refresh-token-family expiry | Added to on BR-01-03's reuse-detected revocation. |

## What's deliberately NOT cached

- **Task/Project/Comment reads** — these change too frequently relative to their read cost to be worth cache invalidation complexity in v1; EF Core + proper indexing (see [database-schema.md](database-schema.md)) is the first lever, caching is a later optimization only if profiling shows it's warranted.
- **Anything tenant-scoped without the `OrganizationId` in the cache key** — every cache key above that isn't purely global (permission matrix) includes the org/project/user id specifically so a cache hit can never leak across tenants, mirroring the EF Core query-filter discipline in [database-schema.md](database-schema.md) at the caching layer too.

## SignalR backplane

`services.AddSignalR().AddStackExchangeRedis(...)` — required as soon as `TaskPlatform.Api` runs as more than one instance (Staging/Production, see [deployment.md](deployment.md)), so a message published from instance A's hub reaches a client connected to instance B.

## Cache-aside pattern

Standard read-through: check Redis → miss → compute/query DB → write to Redis with TTL → return. No write-through caching in v1 (nothing writes to Redis as its primary store — Redis is always a derived/rebuildable cache, never the source of truth for anything, so a full Redis flush is always safe, just temporarily slower).

## Monitoring

Cache hit/miss ratio per key-prefix is logged (see [logging-monitoring.md](logging-monitoring.md)) — if a cached value's hit rate is low enough that the cache overhead isn't paying for itself, that's a signal to either fix the TTL/invalidation or remove the cache entry entirely, not a permanent unexamined cost.
