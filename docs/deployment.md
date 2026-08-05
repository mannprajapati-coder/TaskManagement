# Deployment

## Environments

| Environment | Purpose | Trigger |
|---|---|---|
| **Local** | Dev machine | manual (`dotnet run`) — see [environment-setup.md](environment-setup.md) |
| **Staging** | Pre-release verification, matches prod config shape | auto-deploy on merge to `main` |
| **Production** | Live | manual approval gate after Staging smoke-check passes |

## CI/CD pipeline (GitHub Actions)

```yaml
# .github/workflows/ci.yml — shape, not literal final file
on: [pull_request, push]
jobs:
  build-and-test:
    steps:
      - checkout
      - setup-dotnet (8.0.x)
      - dotnet restore TaskPlatform.sln
      - dotnet format --verify-no-changes            # coding-standards.md
      - dotnet build TaskPlatform.sln --no-restore
      - dotnet test TaskPlatform.Tests                # testing-strategy.md
      - migrations-apply-check:                        # migrations.md — the gate that closes the Sawi gap
          for-each: Modules/*/
          run: dotnet ef database update --project $module --startup-project TaskPlatform.Api
  deploy-staging:
    needs: build-and-test
    if: github.ref == 'refs/heads/main'
    steps:
      - dotnet publish TaskPlatform.Api -c Release
      - dotnet publish TaskPlatform.Web -c Release
      - deploy to Staging App Service(s)
      - run migrations against Staging DB (same per-module command, Staging connection string)
      - smoke-check: hit /health on both Api and Web
  deploy-production:
    needs: deploy-staging
    environment: production   # requires manual approval in GitHub Environments
    steps:
      - same publish/deploy/migrate shape, against Production
```

**The migration-apply-check step is the single most important line in this whole pipeline** — it is the concrete enforcement of ADR-004/[migrations.md](migrations.md)'s "never a hand-run `.sql` script" rule. A PR cannot merge if its migrations don't apply cleanly to a fresh database, which is exactly the check that was missing when this workspace's other product's DB scripts sat unapplied for weeks.

## Hosting

- `TaskPlatform.Api` and `TaskPlatform.Web` as two separate Azure App Services (or two IIS sites / two containers on one VM for the smallest deployment) — separate so each can scale independently once there's real traffic data to justify it, per ADR-002/ADR-003's "keep the seam available" reasoning.
- SQL Server: Azure SQL Database (or a managed VM instance, matching whatever this workspace's other product already uses operationally, for one fewer thing to learn).
- Redis: Azure Cache for Redis (or a self-hosted instance) — required in every environment including Staging, not just Production, since SignalR's backplane and caching are load-bearing from Phase 5 onward (see [caching-strategy.md](caching-strategy.md)).

## Configuration per environment

Handled entirely through [configuration.md](configuration.md)'s layered `appsettings.{Environment}.json` + Key Vault approach — this file only states *when* each environment's config is applied (on deploy), not what the values are.

## Release process

1. Merge to `main` → auto-deploys to Staging (including migrations).
2. Manual smoke-check on Staging: the specific screens/flows for whatever [plan.md](plan.md) milestone just closed (matches this workspace's other product's "verified live, not just build-clean" standard).
3. Tag the commit (`vX.Y-<milestone-name>`, per [git-workflow.md](git-workflow.md)).
4. Approve the Production deploy gate in GitHub Actions.
5. Post-deploy: watch [logging-monitoring.md](logging-monitoring.md)'s dashboards for an elevated error rate for the first hour; rollback plan below if needed.

## Rollback

- **Code**: redeploy the previous tag's build artifact (App Service deployment slots make this a slot-swap, not a rebuild, if configured — recommended once Production traffic exists).
- **Database**: only roll back a migration if its `Down()` was verified in CI (see [migrations.md](migrations.md)) — a migration without a safe rollback is called out in its own PR and in [known-issues.md](known-issues.md), and the response to a bad deploy involving one is a forward-fix migration, not a rollback attempt.

## Background jobs

Hangfire's dashboard (`/hangfire`, restricted to Admin+ via the same RBAC filter as everything else — see [auth.md](auth.md)) is deployed alongside `TaskPlatform.Api` in every environment, so Recurring Tasks generation, AI Analytics nightly precompute, and reminder dispatch (see [sql.md](sql.md) candidate stored procedures) are visible/debuggable in Staging before they ever run unsupervised in Production.
