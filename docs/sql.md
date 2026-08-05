# Raw SQL & Stored Procedures Reference

EF Core LINQ is the default for everything (see [migrations.md](migrations.md) for schema, [database-schema.md](database-schema.md) for tables). This doc is the exception list: where raw SQL or a stored procedure is deliberately used instead, and why — so nobody has to reverse-engineer the reason from the code later.

## When raw SQL/a stored procedure is justified

1. A reporting/aggregation query spans enough tables and computed columns that the LINQ-generated SQL is materially worse than a hand-tuned query (confirmed by comparing actual execution plans, not assumed up front).
2. A query needs to run inside a background job (Hangfire) at a frequency where connection/round-trip overhead matters (e.g. the nightly AI Analytics precompute).
3. A bulk operation (e.g. generating the next occurrence for hundreds of active `RecurrenceRule`s at once) is meaningfully faster as a set-based `UPDATE`/`INSERT ... SELECT` than N individual EF Core `SaveChanges()` round-trips.

Anything not matching one of these three stays as ordinary LINQ against the module's `DbContext`. This list should stay short — if it starts growing, that's a signal the underlying EF model or indexing needs attention (see [database-schema.md](database-schema.md) indexing notes) before reaching for more raw SQL as the fix.

## Candidate stored procedures (to be added as each module reaches build, per [plan.md](plan.md))

| Name | Module | Purpose | Called from |
|---|---|---|---|
| `sp_GenerateRecurringTaskOccurrences` | Recurring Tasks (15) | Set-based generation of the next occurrence for every active `RecurrenceRule` whose current occurrence is complete/overdue (BR-15-01) | Hangfire nightly job |
| `sp_ComputeMilestoneCompletion` | Milestones (9) | Batch-recompute completion % for all Milestones in an Organization (used to warm the cache in [caching-strategy.md](caching-strategy.md) rather than compute on every read under heavy load) | Hangfire job, on-demand cache-miss fallback |
| `sp_ProjectHealthReport` | Reports (25) | The "Project Health" report's underlying aggregation (task counts by status, overdue count, milestone slippage) across one Project | `ReportsService.GenerateAsync` |
| `sp_TeamWorkloadSummary` | Dashboard (24) | Per-user open-task and overdue-task counts for a Team, feeding the "Team Workload" widget | `DashboardService.GetWorkloadWidgetAsync` |
| `sp_AiAnalyticsNightlyPrecompute` | AI Analytics (30) | Precomputes `AiPrediction` rows (risk/delay/completion-date estimates) for every active Project, so the Dashboard/Reports read path is always a cache/table read, never a live model call (NFR-10) | Hangfire nightly job |

Each stored procedure, once actually written, is checked into the owning module's migration (`Modules/<Name>/Infrastructure/Migrations/`) via `migrationBuilder.Sql(...)`, exactly like any other schema object — never a loose `.sql` file outside the migration history (same ADR-004 reasoning: versioned, reviewable, CI-verified).

## Raw SQL via `FromSqlRaw`/`FromSqlInterpolated`

Where a stored procedure is overkill but LINQ still isn't the right tool (a one-off complex read), use `FromSqlInterpolated` (parameterized, safe from SQL injection by construction) — never string-concatenated SQL. Example shape:

```csharp
var overdueByTeam = await _db.Database
    .SqlQuery<TeamOverdueCount>($"""
        SELECT t.TeamId, COUNT(*) AS OverdueCount
        FROM Task task
        JOIN TaskAssignee ta ON ta.TaskId = task.Id AND ta.TeamId IS NOT NULL
        JOIN Team t ON t.Id = ta.TeamId
        WHERE task.DueDate < {DateTime.UtcNow} AND task.Status != 'Done' AND task.OrganizationId = {organizationId}
        GROUP BY t.TeamId
        """)
    .ToListAsync();
```

Note the explicit `OrganizationId` filter — raw SQL bypasses EF Core's automatic tenant query filter (ADR-006), so **every raw query must filter by `OrganizationId` by hand**, and that line is exactly what a code reviewer checks for before approving any PR that adds one. This is the one sharp edge raw SQL introduces that LINQ doesn't have, and it's called out here so it's never silently missed.
