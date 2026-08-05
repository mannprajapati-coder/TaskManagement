# Tech Stack

Every choice below either mirrors an existing, working decision from Sawi (this workspace's other ASP.NET Core product) or explicitly deviates with a reason and a link to the ADR in [design-decisions.md](design-decisions.md).

## Backend

| Concern | Choice | Version | Why |
|---|---|---|---|
| Runtime / framework | ASP.NET Core | .NET 8 (LTS) | Same as Sawi; LTS support through Nov 2026, comfortably covers the 10-week build + first year of operation. |
| Language | C# | 12 | Ships with .NET 8. |
| API style | ASP.NET Core Web API, `[ApiController]` | — | Same as `Sawi.API`. |
| ORM | Entity Framework Core | 8.x | Same as Sawi, but with real Migrations from day one — ADR-004. |
| Database | SQL Server | 2022 (or Azure SQL) | Same engine as Sawi; team already operates it. |
| Auth | ASP.NET Core Identity + JWT (access + refresh token) | — | Richer than Sawi's plain-JWT login (spec calls for email verification, refresh tokens, Google login, optional MFA) — Identity gives password hashing, lockout, token providers for free. See [auth.md](auth.md). |
| Real-time | SignalR | 8.x | Spec explicitly calls for real-time notifications (Module 19) and underlies Kanban/board live updates. Redis backplane for scale-out. |
| Background jobs | Hangfire | 1.8.x | Recurring Tasks (Module 15) auto-generation, scheduled reminders, AI Smart Scheduler batch runs, report generation. SQL Server storage — no extra infra beyond the DB already required. |
| Caching / distributed state | Redis | 7.x | Session/output caching, SignalR backplane, rate-limit counters. See [caching-strategy.md](caching-strategy.md). |
| Logging | Serilog | 3.x | Same as Sawi. Sinks: Console + rolling file in dev, + Seq or Application Insights in staging/prod. See [logging-monitoring.md](logging-monitoring.md). |
| Object mapping | AutoMapper | 13.x | Same as Sawi's `Application/Profiles` convention. |
| Validation | FluentValidation + DataAnnotations | 11.x | DataAnnotations on ViewModels (matches Sawi); FluentValidation for the more compound business rules in [business-rules.md](business-rules.md) that don't fit an attribute. |
| Background/email | MailKit + SMTP (dev: Papercut/Mailtrap; prod: SendGrid) | — | See [third-party-integrations.md](third-party-integrations.md). |
| File storage | `IFileStorageService` abstraction — local disk (dev) / Azure Blob Storage (prod) | — | Matches Sawi's recent file-upload-validation hardening; kept behind an interface so the prod backend is a config choice. |
| AI provider | `IAIProvider` abstraction — Claude (Anthropic) primary, OpenAI-compatible fallback | — | ADR-008. See [ai-usage-guidelines.md](ai-usage-guidelines.md). |

## Frontend

| Concern | Choice | Why |
|---|---|---|
| Web framework | ASP.NET Core Razor MVC | Matches Sawi's whole frontend fleet — one stack, one dev environment, one auth/session model. ADR-007. |
| CSS | Bootstrap 5 + a small custom design-token layer | See [ui-guidelines.md](ui-guidelines.md). |
| JS — general | jQuery (form submission, AJAX, validation glue) | Matches Sawi convention (`jquery.validate`, `_ValidationScriptsPartial`). |
| JS — Kanban | SortableJS | Lightweight, no framework dependency, drag-drop-only — exactly what Module 20 needs. |
| JS — Gantt | Frappe Gantt (fallback: DHTMLX Gantt CE) | Open-source, renders from a plain JSON task list — fits a server-rendered page. |
| JS — Calendar | FullCalendar | De facto standard for drag-and-drop calendar views; has day/week/month views out of the box (Module 21's exact requirement). |
| Real-time client | `@microsoft/signalr` JS client | Talks to `ActivityHub` on `TaskPlatform.Api`. |
| Charts (Dashboard/Reports/AI Analytics) | Chart.js | Lightweight, sufficient for completion-rate/workload/burndown-style widgets (Modules 24, 25, 30). |

## Infrastructure / DevOps

| Concern | Choice | Why |
|---|---|---|
| Source control | Git | See [git-workflow.md](git-workflow.md). |
| CI/CD | GitHub Actions | See [deployment.md](deployment.md). |
| Hosting | Azure App Service (Api + Web as two App Services) or a single VM behind IIS/Nginx for the smallest deployment | Environment-dependent; see [deployment.md](deployment.md). |
| Secrets | Azure Key Vault (prod), User Secrets (dev) | See [configuration.md](configuration.md). |
| Containerization | Docker (optional, for local Redis/SQL Server via `docker-compose`, and as a future deployment path) | Not required to ship v1; documented so it's not a foreclosed option. |

## Testing

| Concern | Choice | Why |
|---|---|---|
| Unit / integration | xUnit + FluentAssertions + Moq | See [testing-strategy.md](testing-strategy.md). |
| Integration test DB | EF Core InMemory for pure logic, Testcontainers (SQL Server image) for anything touching real query filters/migrations | Global query filters (ADR-006) are exactly the kind of thing InMemory can silently get wrong. |
| API contract tests | `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`) | Exercises `TaskPlatform.Api` the same way `TaskPlatform.Web` and any future third-party client do. |

## Explicitly not chosen (and why)

- **Microservices** — ADR-003.
- **A SPA framework (React/Angular/Vue) for the whole app** — ADR-007; revisit only per-screen if a specific interaction genuinely outgrows the JS-library-on-Razor approach.
- **NoSQL / document store** — the domain is relentlessly relational (Organization → Workspace → Team → Project → Task, with dependencies and hierarchies); SQL Server's existing operational familiarity wins.
- **GraphQL** — one first-party frontend consumes the API; REST's simplicity (and Sawi team familiarity) outweighs GraphQL's over-fetching benefits at this scale.
