# Environment Setup

## Prerequisites

| Tool | Version | Notes |
|---|---|---|
| .NET SDK | 8.0.x | `dotnet --version` to confirm. |
| SQL Server | 2022, or LocalDB for the lightest local setup | Matches [tech-stack.md](tech-stack.md). |
| Redis | 7.x | Required even in dev — SignalR backplane and caching are wired in from the start (see [caching-strategy.md](caching-strategy.md)), not dev-optional. Easiest via Docker: `docker run -d -p 6379:6379 redis:7`. |
| IDE | Visual Studio 2022 (17.8+) or JetBrains Rider | Either works; this workspace's other product is developed in Visual Studio, so VS is the more tested path for solution-wide operations (build, EF Core Package Manager Console). |
| Node.js | Not required | No frontend build step — JS libraries are vendored under `wwwroot/assets` per [tech-stack.md](tech-stack.md)/[ui-guidelines.md](ui-guidelines.md), same as this workspace's other product. |
| Docker (optional) | — | Convenience only, for Redis/SQL Server containers; not required if you already have both installed natively. |

## First-time setup

```bash
git clone <repo-url> TaskPlatform
cd TaskPlatform
dotnet restore TaskPlatform.sln
```

### Secrets (never committed)

Use `dotnet user-secrets` per host project — never put connection strings or API keys in `appsettings.json` (see [configuration.md](configuration.md)):

```bash
dotnet user-secrets init --project TaskPlatform.Api
dotnet user-secrets set "ConnectionStrings:Default" "Server=(localdb)\\mssqllocaldb;Database=TaskPlatformDb;Trusted_Connection=True;" --project TaskPlatform.Api
dotnet user-secrets set "Redis:ConnectionString" "localhost:6379" --project TaskPlatform.Api
dotnet user-secrets set "Jwt:SigningKey" "<generate a local dev RSA key, do not reuse across environments>" --project TaskPlatform.Api
dotnet user-secrets set "GoogleAuth:ClientId" "<from Google Cloud Console dev project>" --project TaskPlatform.Api
dotnet user-secrets set "GoogleAuth:ClientSecret" "<...>" --project TaskPlatform.Api
dotnet user-secrets set "AiProvider:ApiKey" "<Claude/OpenAI dev key>" --project TaskPlatform.Api
```

`TaskPlatform.Web` needs its own `user-secrets init` only for `ApiBaseUrl` (points at `TaskPlatform.Api`'s local URL) — it holds no direct DB/Redis/AI secrets, since it never talks to any of those except through `TaskPlatform.Api` (ADR-002).

### Database

```bash
# Run once per module, in phase order (Phase 1 modules first — later modules' FKs depend on earlier ones existing)
for module in Authentication UserManagement Organizations Workspaces Teams RolesPermissions \
              Projects Milestones Tasks RecurringTasks Comments Attachments ActivityTimeline \
              Notifications Kanban TimeTracking Reports AI; do
  dotnet ef database update --project "Modules/$module" --startup-project TaskPlatform.Api
done
```

See [migrations.md](migrations.md) for the per-module command shape and why it's per-module rather than one combined migration.

### Seed data

The 8 Roles, 7 Permissions, and default Role-Permission matrix ([user-roles.md](user-roles.md)) are seeded automatically by the `RolesPermissions` module's own migration (`HasData(...)`) — no separate manual seed script/step required, unlike this workspace's other product's hand-run `.sql` seed files.

### Running locally

```bash
dotnet run --project TaskPlatform.Api      # https://localhost:7401 — Swagger at /swagger in Development
dotnet run --project TaskPlatform.Web      # https://localhost:7400
```

Both should be running simultaneously for `Web` to function — it has no fallback for `Api` being down beyond a friendly error page (see [error-handling.md](error-handling.md)).

### First login

Register via `TaskPlatform.Web`'s `/Auth/Register` — the first registered user of a brand-new Organization becomes its Owner automatically (BR-03-01). There is no pre-seeded admin account; this matches the spec's own "Create Organization" flow rather than inventing a bootstrap account that wouldn't exist in production either.

## Common issues

| Symptom | Likely cause | Fix |
|---|---|---|
| `Web` shows a generic connection error on every page | `Api` isn't running, or `ApiBaseUrl` user-secret is wrong/missing | Confirm `Api` is running and reachable at the configured URL. |
| `Invalid object name '<Table>'` | A module's migration hasn't been applied yet | Re-run the per-module `dotnet ef database update` for that specific module — see [migrations.md](migrations.md); this is the exact failure mode ADR-004 exists to prevent going forward, but a fresh clone still needs the one-time apply step. |
| SignalR client never connects / real-time updates don't appear | Redis isn't running, or CORS origin mismatch between `Web`'s dev URL and `Api`'s configured allowed origins | Confirm Redis container is up; check `Api`'s CORS policy in `Program.cs` matches `Web`'s actual `https://localhost:7400`. |
| `dotnet build` fails with `MSB3027`/`MSB3021` but no `error CS` lines | A file is locked by a running debug session (IIS Express/Kestrel process still attached from a previous run) | Stop the running process for that project (Visual Studio: Debug → Stop, or end the specific `dotnet`/`iisexpress` process) and rebuild — this is not a real code error. |
