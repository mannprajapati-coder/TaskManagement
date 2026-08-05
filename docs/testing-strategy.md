# Testing Strategy

## Levels

| Level | Tool | Scope | Runs |
|---|---|---|---|
| Unit | xUnit + Moq + FluentAssertions | One `Application/Services` class in isolation, all dependencies mocked via `IServices` interfaces | Every PR, every module changed |
| Integration (module) | xUnit + EF Core against a real SQL Server (Testcontainers) | A module's service + real `DbContext` + real migration applied — specifically to catch query-filter (tenancy) mistakes InMemory would silently miss | Every PR touching that module's `Infrastructure`/`Domain` |
| Integration (API) | `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`) | A full HTTP round-trip through `TaskPlatform.Api`, auth included | Every PR touching a controller |
| End-to-end | Manual for v1 (see note below) | A real click-through in `TaskPlatform.Web` against a running `Api` | Once per phase milestone (see [plan.md](plan.md)), before marking a milestone Done |

**Why Testcontainers over EF Core InMemory for anything touching tenancy:** InMemory doesn't enforce real SQL Server constraints (unique indexes, FK cascade behavior) and — critically — global query filters can pass against InMemory while a real SQL Server query plan behaves differently. NFR-1 (zero cross-Organization leakage) is exactly the kind of guarantee that needs the real engine under test, not a stand-in.

**Why manual E2E for v1, not Selenium/Playwright:** this workspace's other product's own established practice is "verified live" (run the app, click through the actual flow) rather than a maintained browser-automation suite, given the team size and timeline. Revisit adding Playwright coverage for the highest-traffic flows (login, task CRUD, Kanban drag-drop) once the product has enough surface area that manual click-through per phase stops being feasible — track that decision point in [known-issues.md](known-issues.md), don't silently let E2E stay manual forever without revisiting.

## What must be tested per PR (see also [coding-standards.md](coding-standards.md) PR checklist)

1. **Every `BR-xx-yy`** in [business-rules.md](business-rules.md) that the PR implements or touches gets a unit test named after that ID, e.g. `BR_13_01_TaskCannotStartWhilePredecessorIncomplete`.
2. **Every endpoint** in [api-endpoints.md](api-endpoints.md) that the PR adds/changes gets at least one `WebApplicationFactory` integration test covering the happy path and the one most likely failure (permission denied, validation failure, or not-found).
3. **Every new migration** is exercised by the CI "apply to fresh DB" gate ([migrations.md](migrations.md)) — this is infrastructure, not a per-PR test to write by hand.
4. **Any ownership-check code path** (see [auth.md](auth.md)) gets an explicit "user without access gets 403, not the data" test — this exact class of bug (an endpoint trusting a client-supplied id) has been the single most common real bug found late in this workspace's other product, so it gets a standing, non-optional test requirement here rather than being left to reviewer memory.

## Multi-tenancy test suite (standing, not per-PR)

A dedicated `Tests/Integration/TenancyIsolationTests.cs` suite that, for every module with tenant-scoped tables, creates two Organizations with overlapping data shapes and asserts that Organization A's calls never return Organization B's rows — run on every CI build, not just when tenancy-related code changes, since a regression here could be introduced by an unrelated change to a query filter's inheritance chain.

## Test data

Seeded via a shared `TestDataBuilder` (one per module, composable) rather than hand-written setup duplicated across test files — mirrors the "reduce and reuse" principle already established for `TaskPlatform.Shared` at the production-code level, applied to test code too.

## Performance testing

Once Phase 4 (Task Management) is Done (see [plan.md](plan.md)), a baseline load test (k6 or similar) against `Tasks/GetByProject` and `Kanban/GetBoard` establishes the numbers NFR-3 claims — not asserted from first principles before there's a real schema/index to measure.

## AI module testing

AI modules (26–30) are tested against a **fake `IAIProvider`** (deterministic canned responses) for all business-logic tests — never a live LLM call in CI, both for cost and for determinism. A small, separate, manually-triggered "smoke test against the real provider" exists to catch prompt/schema drift, run before a release, not on every PR (see [ai-usage-guidelines.md](ai-usage-guidelines.md)).

## Coverage target

No hard numeric gate in v1 (a coverage percentage is a weak proxy for the things that actually matter — see items 1–4 above). Revisit if/when a specific module's bug rate suggests targeted coverage improvement is the right lever, tracked via [known-issues.md](known-issues.md).
