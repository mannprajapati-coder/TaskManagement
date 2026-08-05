# Coding Standards

## Naming

- PascalCase for classes, methods, public properties; camelCase for locals/parameters; `_camelCase` for private fields.
- Interfaces prefixed `I` (`ITasksService`, `IAiProvider`).
- Async methods suffixed `Async` (`GetByIdAsync`), and always actually async — no `Async`-suffixed method that just wraps sync code in `Task.FromResult` unless there's genuinely no async work to do yet and a real implementation is coming.
- ViewModels named `<Action><Module>RequestViewModel` / `...ResponseViewModel` (see [api-conventions.md](api-conventions.md)) — never bare `TaskViewModel` reused across five different endpoints with slightly different optional fields; each endpoint gets its own explicit shape.
- Permission keys, role names, and status/priority enum members match the exact wording in [user-roles.md](user-roles.md)/[business-rules.md](business-rules.md) verbatim — no ad hoc renaming for "cleaner code" that then requires a mapping table to relate back to the spec.

## Adding a new module

Every module (see [folder-structure.md](folder-structure.md)) is created from the same shape — never invent a variant:

1. `Modules/<Name>/<Name>.csproj` (class library, targets `net8.0`).
2. `Domain/Entities/`, `Domain/IServices/` — no framework/EF dependency in `Domain`.
3. `Application/Services/`, `Application/Profiles/`, `Application/Extensions/ServiceCollectionExtensions.cs` exposing `AddXModule(this IServiceCollection services, IConfiguration config)`.
4. `Infrastructure/Context/<Name>DbContext.cs`, `Infrastructure/Repositories/`.
5. Register in `TaskPlatform.Api/Program.cs` via the module's `AddXModule(...)` call — one line, no other file in `Api` should need to change to wire up a new module's DI.
6. First migration (see [migrations.md](migrations.md)) before the first real feature PR, not after.

## Module boundaries (enforced in review, not just convention)

- A module's `Application`/`Infrastructure` may only be referenced by that module's own `.csproj` and by `TaskPlatform.Api`. **Never** by another module or by `TaskPlatform.Web` directly.
- Cross-module logic goes through the other module's `IServices` interface, injected via DI — never a direct `DbContext`-to-another-module's-table reference, and never a project reference to another module's `.csproj` for anything beyond its `IServices`/`Domain.Entities` where a genuine shared value type is needed (rare — most shared vocabulary, like `Roles`, lives in `TaskPlatform.Shared/Enums`).
- PR review checklist item: does this PR add a `using` that reaches into another module's `Application`/`Infrastructure` namespace? If yes, it needs a specific justification in the PR description or it gets rejected — this is the exact discipline that keeps [architecture.md](architecture.md) §2's "modular monolith" claim true rather than aspirational.

## PR review checklist (beyond normal correctness)

- [ ] New/changed entity → migration included ([migrations.md](migrations.md)).
- [ ] New/changed endpoint → [api-endpoints.md](api-endpoints.md) updated in the same PR.
- [ ] New business rule → corresponding `BR-xx-yy` added to [business-rules.md](business-rules.md), and a test named after that ID.
- [ ] Any endpoint taking a resource id → ownership check present ([auth.md](auth.md) "Ownership checks").
- [ ] Any new account-creation path → does the created account actually end up able to log in ([auth.md](auth.md) "Account-creation must actually enable login").
- [ ] Any raw SQL → explicit `OrganizationId` filter present by hand ([sql.md](sql.md)).
- [ ] Any `[JsonIgnore]` added to a ViewModel property → confirm that property has no validation attribute also relied on server-side ([api-conventions.md](api-conventions.md)).

## Comments

Default to none. Add a comment only for a non-obvious *why* — a business rule reference (`// BR-13-01`), a workaround for a specific constraint, a genuinely surprising invariant. Never a comment restating what the code already says via good naming.

## Error handling

Application-layer services throw a typed exception (`DomainException`, `PermissionDeniedException` — see [error-handling.md](error-handling.md)) for expected failure modes; `TaskPlatform.Api`'s global exception middleware maps these to the response envelope in [api-conventions.md](api-conventions.md). Controllers themselves should rarely contain a `try/catch` — that's the middleware's job.

## Testing expectations

See [testing-strategy.md](testing-strategy.md) for the full strategy; the standing rule for any PR is: a new business rule needs a test named after its `BR-xx-yy` ID, and a new endpoint needs at least one integration test exercising it through `WebApplicationFactory`, not only a unit test against the service in isolation.

## Formatting / tooling

`.editorconfig` at the repo root enforces brace style, `var` usage rules, and using-directive ordering; `dotnet format` runs in CI (see [deployment.md](deployment.md)) and fails the build on a diff — never a manual "please format your code" review comment.
