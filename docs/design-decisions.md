# Design Decisions (ADRs)

Lightweight Architecture Decision Records — why X over Y. Newest at the bottom of each status; never delete a rejected ADR, mark it Superseded and link forward.

---

### ADR-001: One shared multi-role web app, not one deployable per role

**Status:** Accepted

**Context:** Sawi's established convention is one separate Razor MVC deployable per actor type (`Sawi.Admin`, `Sawi.Clinic`, `Sawi.Doctor`, `Sawi.Patient`, ...) because those actors never share a screen or collaborate in real time. TaskPlatform's spec describes Owner/Admin/PM/Team Lead/Developer/Tester/Viewer/Guest all working inside the *same* Kanban board, Gantt chart, and comment thread simultaneously.

**Decision:** Build **one** `TaskPlatform.Web` application. Role/permission checks (see [user-roles.md](user-roles.md)) gate which menu items, buttons, and API actions are available per request — they do not gate which deployable a user lands on.

**Consequences:** Simpler real-time collaboration (one SignalR hub, one set of views to keep in sync); more authorization logic concentrated in one place, so `RoleClaimAuthorizeFilter`-equivalent checks must be applied consistently on every controller action (see [auth.md](auth.md)). Confirmed with the project owner before drafting the rest of these docs.

---

### ADR-002: Web still calls Api over HTTP, even with only one frontend

**Status:** Accepted

**Context:** With only one frontend, `TaskPlatform.Web` *could* reference the module `Application` layers in-process and skip the HTTP hop entirely — that's the more common Clean Architecture MVC shape when there's a single UI.

**Decision:** Keep the Sawi shape anyway: `TaskPlatform.Web` calls `TaskPlatform.Api` over HTTP via `TaskPlatform.Shared`'s `ApiService`, never referencing a module project directly.

**Why:** (1) consistency with the rest of this workspace's tooling, debugging habits, and `Sawi.Helper`-style reuse pattern the developer already knows; (2) `TaskPlatform.Api` becomes a real, independently-testable public surface from day one — valuable given [webhooks.md](webhooks.md) and a plausible future mobile client both need the exact same contract Web already exercises; (3) it forces API request/response DTOs to stay honest (no accidental leakage of EF entities into a view), because there is no other path to the data.

**Trade-off accepted:** one extra network hop per request, in-process on the same box in dev/most likely prod (see [deployment.md](deployment.md)) so the latency cost is small.

---

### ADR-003: Modular monolith, not microservices

**Status:** Accepted

**Context:** 30 modules across 8 phases could map to 30 independently-deployed services.

**Decision:** Single deployable monolith (`TaskPlatform.Api` + `TaskPlatform.Web`), modular only at the code level (see [architecture.md](architecture.md) §2).

**Why:** the delivery roadmap is 10 weeks, largely solo (see [plan.md](plan.md)); microservices' operational cost (30 CI/CD pipelines, service discovery, distributed tracing, cross-service transactions) has no payoff at this team size or this stage of the product's life. Each module's `IServices` boundary is deliberately kept clean so a specific module (most likely one of the AI modules, given its distinct scaling/latency profile) can be extracted later without a rewrite.

---

### ADR-004: Real EF Core Migrations from day one, not hand-run SQL scripts

**Status:** Accepted

**Context:** Sawi's Phase 2 modules ship hand-authored `.sql` scripts under `db/<Module>/` with an instruction to "run them" — in practice, most were never applied to the live environment for weeks, and every module that skipped this step surfaced as a live `Invalid object name` / `Invalid column name` error the first time someone actually exercised the feature.

**Decision:** Every module's `DbContext` uses real EF Core Code-First Migrations (`dotnet ef migrations add`, checked into `Modules/<Name>/Infrastructure/Migrations/`), applied via `dotnet ef database update` as an explicit CI/CD deployment step (see [migrations.md](migrations.md), [deployment.md](deployment.md)) — never a manually-run `.sql` file.

**Why:** migrations are versioned, reviewable in a PR diff, and — critically — CI can verify "does the migration for this branch actually apply cleanly to a fresh database" before merge, closing the exact gap that repeatedly bit Sawi.

---

### ADR-005: One-to-one module-to-class-library mapping (30 projects)

**Status:** Accepted

**Context:** 30 class libraries is more than Sawi's ~23 today; grouping the spec's 30 modules into ~8 coarser libraries (one per phase) would cut project-file overhead.

**Decision:** Keep the spec's 1:1 module boundary as the code boundary. Every module named in the functional spec gets exactly one class library.

**Why:** the spec itself is the sprint/requirements source of truth (see [plan.md](plan.md)); a 1:1 mapping means "which project does Feature X live in" never requires a lookup table, and a module can be hollowed out and extracted to its own service later (ADR-003) without first having to split it out of a coarser sibling. The `.csproj` overhead is mechanical (a template, not a design cost) and Sawi's own module count already trends this direction.

---

### ADR-006: Single database, per-module DbContext, tenant-scoped by global query filter

**Status:** Accepted

**Context:** Multi-tenancy (Organization) needs a hard boundary; 30 separate databases is unnecessary operational weight for this scale.

**Decision:** One SQL Server database (`TaskPlatformDb`). Each module owns its own `DbContext` mapping only its own tables (bounded-context style, matching Sawi's `Infrastructure/Context` convention per module). Every tenant-scoped entity gets a global EF Core query filter on `OrganizationId`, sourced from the current user's claims (see [database-schema.md](database-schema.md) §"Tenancy").

**Why:** row-level filtering by default means a missing `WHERE OrganizationId = ...` can't leak data across tenants by omission — the filter is applied unless a query explicitly opts out (`IgnoreQueryFilters()`), which is deliberately rare and reviewable in a PR.

---

### ADR-007: Kanban/Gantt/Calendar rendered with vanilla JS libraries on Razor views, not a SPA

**Status:** Accepted

**Context:** Drag-and-drop boards, Gantt bars, and a calendar are the kind of UI that's often reached for React/Angular. Sawi's whole frontend stack is Razor MVC + Bootstrap + jQuery.

**Decision:** Stay on Razor MVC. Layer focused, single-purpose JS libraries onto server-rendered views: SortableJS (Kanban drag-drop), Frappe Gantt or DHTMLX Gantt (Gantt chart), FullCalendar (calendar). Same pattern Sawi already uses for DataTables-driven grids.

**Why:** avoids running two frontend stacks (Razor for 90% of the app, a SPA for three screens); keeps one deployment story, one auth/session model, one dev environment. Revisit only if a specific screen's interaction complexity genuinely outgrows what a JS library + partial-view AJAX refresh can do — see [frontend-state-management.md](frontend-state-management.md) for the concrete state-sync approach these three screens use.

---

### ADR-008: AI provider is an interface, not a hard dependency

**Status:** Accepted

**Decision:** All 5 AI modules depend on an `IAIProvider` abstraction (`GenerateAsync`, `StreamAsync`, `EmbedAsync`) defined in `TaskPlatform.Shared`, never on a vendor SDK type directly.

**Why:** keeps the AI vendor a configuration/appsettings choice (see [configuration.md](configuration.md), [third-party-integrations.md](third-party-integrations.md)), makes the AI modules unit-testable against a fake provider, and avoids a repeat of a vendor-specific type leaking into business logic the way a raw HTTP client easily can.
