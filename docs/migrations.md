# Migrations — EF Core Strategy

**This is the single biggest deliberate deviation from how Sawi handles the database.** Sawi's Phase 2 modules ship hand-authored `.sql` scripts under `db/<Module>/` with an instruction to "run them manually" — in practice, most sat unapplied for weeks and every module that skipped this step eventually surfaced as a live `Invalid object name`/`Invalid column name` error the first time someone actually exercised the feature (see this workspace's own project memory for the repeated pattern). ADR-004 in [design-decisions.md](design-decisions.md) exists specifically to not repeat that.

## Rule

**Every schema change is a real EF Core Migration, committed to source control, applied by an explicit CI/CD step (see [deployment.md](deployment.md)). There is no hand-run `.sql` script for anything a migration can express.**

## Per-module migrations, not one giant migration

Each module's `DbContext` (see [database-schema.md](database-schema.md)) owns its own migration history, stored under that module's own folder:

```
Modules/Tasks/Infrastructure/Migrations/
    20260810120000_InitialTasksSchema.cs
    20260817090000_AddTaskDependencyCycleIndex.cs
```

Commands are always scoped to one module + one startup project:

```bash
dotnet ef migrations add InitialTasksSchema \
  --project Modules/Tasks \
  --startup-project TaskPlatform.Api \
  --context TasksDbContext \
  --output-dir Infrastructure/Migrations

dotnet ef database update \
  --project Modules/Tasks \
  --startup-project TaskPlatform.Api \
  --context TasksDbContext
```

`--startup-project TaskPlatform.Api` is required because the module library itself has no `Program.cs`/config — it borrows `Api`'s connection string and DI container purely to run the CLI tool. This does **not** mean `Api` owns the migration; the migration file lives under the module's own folder and ships in the module's own PR.

## Why per-module, not one shared `TaskPlatformDbContext`

- Matches the module boundary everywhere else in this design (ADR-005, ADR-006) — a module PR is self-contained, including its own schema change.
- A module can be extracted to its own database/service later (see ADR-003's "seam" reasoning) without first having to untangle a shared migration history.
- Blast radius of a bad migration is one module's tables, not the whole schema.

**Trade-off accepted:** 30 independent migration histories against one physical database means ordering across modules during a fresh `database update` matters only where one module's migration references another module's table (rare — cross-module references are through `IServices`, not FKs, per [architecture.md](architecture.md) §2; the few genuine cross-module FKs, e.g. `Task.MilestoneId`, are seeded in dependency order in [deployment.md](deployment.md)'s CI step).

## CI gate (closes the exact gap that bit Sawi)

Every PR that touches a module's `Domain/Entities` or `Infrastructure/Context` must include a migration, and CI verifies it applies cleanly:

```yaml
# excerpt — see deployment.md for the full pipeline
- name: Verify migrations apply to a fresh database
  run: |
    for module in Modules/*/; do
      dotnet ef database update --project "$module" --startup-project TaskPlatform.Api
    done
```

A PR that changes an entity without a matching migration fails this step — `dotnet ef migrations has-pending-model-changes` (or the equivalent check) runs as a separate, faster pre-check so this isn't only caught at the slow "apply to a real DB" stage.

## Local dev

`environment-setup.md` has the exact one-time commands; day-to-day, `dotnet ef database update` per module after `git pull` is the only manual step — never open SSMS and run a `.sql` file by hand for a schema change.

## Seed data

Reference/lookup data that isn't a schema change (the 8 `Role` rows, 7 `Permission` rows, default `RolePermission` matrix from [user-roles.md](user-roles.md)) is seeded via `modelBuilder.Entity<Role>().HasData(...)` inside the migration itself — versioned and applied the same way as schema, not a separate manually-run seed script.

## Rollback

Every migration should have a working `Down()` — verified in CI by an `update → rollback → update` round-trip on the module that changed, not assumed. If a migration is genuinely irreversible (e.g. a destructive data transform), that's called out explicitly in the PR description and in [known-issues.md](known-issues.md) until it's been live in production long enough to remove the note.
