# TaskPlatform

An organization-wide project and task management platform with a built-in AI assistant layer — the internal working name for the product described in `Project_Functional_Specification.docx` (v1.0, August 2026). Think Jira/Asana's core hierarchy (Organization → Workspaces → Teams → Projects → Tasks) plus an AI layer that can generate a project plan from a one-line prompt, auto-schedule work, extract tasks from meeting notes, and answer natural-language questions about project state.

This `docs/` folder is the planning and reference set for the build. Start here, then follow the links below in roughly the order you'll need them.

## Quick start (local dev)

Prerequisites: .NET 8 SDK, SQL Server (LocalDB is fine), Redis (via Docker: `docker run -p 6379:6379 redis`), Node not required (no build step — JS libraries are vendored under `wwwroot/assets`).

```bash
git clone <repo-url> TaskPlatform
cd TaskPlatform
dotnet restore
dotnet ef database update --project Modules/UserManagement --startup-project TaskPlatform.Api   # repeat per module, see migrations.md
dotnet run --project TaskPlatform.Api      # https://localhost:7401
dotnet run --project TaskPlatform.Web      # https://localhost:7400
```

Full step-by-step, including seed data and secrets setup: [environment-setup.md](environment-setup.md).

## What this system is

- **Hierarchy:** Organization → Workspaces / Teams / Users / Projects (Members, Milestones, Tasks) / AI Assistant. See [domain-model.md](domain-model.md).
- **30 modules across 8 delivery phases**, 10 weeks. See [plan.md](plan.md) and [scope.md](scope.md).
- **One shared web application**, not one deployable per role — Owner/Admin/PM/Team Lead/Developer/Tester/Viewer/Guest all collaborate on the same boards in real time. See [architecture.md](architecture.md) and [user-roles.md](user-roles.md).

## Map of this docs/ folder

**Planning & Scope**
- [plan.md](plan.md) — roadmap, milestones, timeline
- [scope.md](scope.md) — in/out of scope
- [requirements.md](requirements.md) — functional & non-functional requirements

**Architecture & Design**
- [architecture.md](architecture.md) — system architecture, pattern
- [tech-stack.md](tech-stack.md) — technologies and why
- [folder-structure.md](folder-structure.md) — solution layout
- [design-decisions.md](design-decisions.md) — ADRs

**Database**
- [database-schema.md](database-schema.md) · [migrations.md](migrations.md) · [sql.md](sql.md)

**API**
- [api-conventions.md](api-conventions.md) · [api-endpoints.md](api-endpoints.md)

**Development Standards**
- [coding-standards.md](coding-standards.md) · [git-workflow.md](git-workflow.md) · [environment-setup.md](environment-setup.md)

**Security & Auth**
- [auth.md](auth.md)

**Testing & Deployment**
- [testing-strategy.md](testing-strategy.md) · [deployment.md](deployment.md)

**Tracking**
- [changelog.md](changelog.md)

**Domain & Business Logic**
- [domain-model.md](domain-model.md) · [business-rules.md](business-rules.md) · [user-roles.md](user-roles.md)

**Integration & External Systems**
- [third-party-integrations.md](third-party-integrations.md) · [webhooks.md](webhooks.md)

**Performance & Reliability**
- [caching-strategy.md](caching-strategy.md) · [logging-monitoring.md](logging-monitoring.md) · [error-handling.md](error-handling.md)

**Configuration**
- [configuration.md](configuration.md) · [feature-flags.md](feature-flags.md)

**Frontend/UI**
- [ui-guidelines.md](ui-guidelines.md) · [frontend-state-management.md](frontend-state-management.md)

**Team & Process**
- [onboarding.md](onboarding.md) · [known-issues.md](known-issues.md) · [faq.md](faq.md)

**AI-Specific**
- [ai-prompts.md](ai-prompts.md) · [ai-usage-guidelines.md](ai-usage-guidelines.md)

## Status

Planning stage — this docs set was drafted from the functional specification before any code was written. Treat every doc here as the **intended** design; as sprints execute, update the relevant doc in the same PR as the code (see [git-workflow.md](git-workflow.md)) rather than letting docs drift, and log what actually shipped vs. what was planned in [changelog.md](changelog.md) and [known-issues.md](known-issues.md).
