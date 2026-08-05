# API Conventions

Applies to every controller in `TaskPlatform.Api` across all 30 modules. Consistency here is what lets `TaskPlatform.Web`'s `ApiService` (see [folder-structure.md](folder-structure.md)) stay a thin, generic wrapper instead of special-casing each module.

## Routing & naming

- Base route: `api/{Module}/{Action}` — e.g. `api/Tasks/Create`, `api/Kanban/MoveCard`. Matches Sawi's existing `ApiEndPoint.cs`-constant convention (kept in `TaskPlatform.Shared/Constants/ApiEndPoint.cs`) rather than pure REST nouns-only routing, for consistency with how this workspace already names things.
- Controller name = module name, singular where the module name already reads naturally singular (`TaskController`... **no** — module is literally named "Task" but the entity is plural-friendly; follow the module name from [domain-model.md](domain-model.md) exactly: `TasksController`, `ProjectsController`, `MilestonesController`, `KanbanController`, `CalendarController`, `GanttChartController`, etc. — one controller per module, never split or merged across module boundaries.
- Action verbs are explicit, not HTTP-verb-implied nouns: `Create`, `Update`, `Delete`, `GetById`, `GetAll`, `GetMy*` (self-scoped, see below), plus domain verbs where a generic CRUD name would lose meaning: `MoveCard`, `ApproveJoinRequest`, `StartTimer`, `AcceptGeneratedPlan`.

## HTTP verbs

| Verb | Used for |
|---|---|
| `GET` | Reads. Never mutates. Query params for filters/paging, never in the body. |
| `POST` | Create, and any mutating action that isn't a pure update-by-id (`StartTimer`, `MoveCard`, `AcceptGeneratedPlan`). |
| `PUT` | Full update-by-id (`Update`). |
| `PATCH` | Partial update where the ViewModel is explicitly a partial shape (rare — most updates are `PUT` with the full ViewModel, matching Sawi convention). |
| `DELETE` | Soft-delete (see [database-schema.md](database-schema.md) — this always sets `IsDeleted`, never a hard delete except the explicit Admin/Owner hard-delete action, which is its own named `POST` action, not overloading `DELETE`). |

## Request/response shape

- Every request/response DTO lives in `TaskPlatform.Shared/ViewModels/`, named `<Action><Module>RequestViewModel` / `<Action><Module>ResponseViewModel` — e.g. `CreateTaskRequestViewModel`, `GetTaskResponseViewModel`. Shared between `Api` and `Web` (never redefined per-project) — this is the whole point of `TaskPlatform.Shared` existing.
- Every list endpoint returns a paged envelope, never a bare array:

```json
{ "items": [...], "page": 1, "pageSize": 25, "totalCount": 142 }
```

- Every single-item response wraps in a consistent envelope so error/success shape is uniform (matches [error-handling.md](error-handling.md)):

```json
{ "success": true, "data": { ... }, "errors": null }
```
```json
{ "success": false, "data": null, "errors": [{ "field": "DueDate", "message": "DueDate cannot be before StartDate" }] }
```

## Validation

- DataAnnotations on the request ViewModel for shape/format (`[Required]`, `[MaxLength]`, `[Range]`); FluentValidation for cross-field/business rules that map to a specific `BR-xx-yy` in [business-rules.md](business-rules.md) — every FluentValidation rule's failure message should be traceable back to the BR ID it enforces, in a code comment if not in the message itself.
- `[ApiController]`'s automatic `ModelState` → 400 behavior is relied on for DataAnnotations failures — **never `[JsonIgnore]` a validation-attributed property that's also bound `[FromBody]`** (a real bug class this exact mistake caused elsewhere in this workspace: the property silently never reaches the server to be validated, or the server's independent copy of the validation attribute rejects a payload the client never actually sent that field in).

## Pagination, filtering, sorting

- `page` (1-based), `pageSize` (default 25, max 100), `sortBy`, `sortDir` (`asc`/`desc`) as query params on every `GetAll`-style action.
- Filters are explicit named query params per endpoint (`status`, `assigneeId`, `projectId`, ...), never a generic free-form filter-expression query param — keeps every endpoint's contract self-documenting in [api-endpoints.md](api-endpoints.md) without needing a query-language spec of its own.

## Authorization

- Every action (except `[AllowAnonymous]` auth endpoints — register/login/forgot-password/verify) requires `[Authorize]` plus a permission check against the matrix in [user-roles.md](user-roles.md), enforced by a `RoleClaimAuthorizeFilter`-equivalent `IAsyncActionFilter` (mirrors the existing pattern already used in this workspace) — never only a UI-side hide/show.
- Every action that takes a resource id must resolve the caller's *own* scope from their claim before trusting any id in the request — see [auth.md](auth.md) "Ownership checks" and the IDOR-class bugs repeatedly found and fixed elsewhere in this workspace when this check was skipped.

## Versioning

- URL segment versioning from day one even though v1 has no v2 yet: `api/v1/Tasks/Create`. Cheap to add now, expensive to retrofit once a mobile client or third-party integration (see [webhooks.md](webhooks.md)) depends on the unversioned shape.

## Idempotency

- Any action that could plausibly be double-submitted from a flaky network (`Create`, `StartTimer`, `AcceptGeneratedPlan`) accepts an optional `Idempotency-Key` header; the server stores the key+response for 24h and replays the stored response on a repeat, rather than creating a duplicate.

## Real-time vs. REST

- REST is the source of truth for every write. SignalR (`ActivityHub`, see [architecture.md](architecture.md) §6) is a **notification-only** channel telling connected clients "something changed, re-fetch" or carrying a small payload for optimistic UI update — a client that misses a SignalR event must still end up correct on its next REST read. Never make SignalR the only path a piece of state can change through.
