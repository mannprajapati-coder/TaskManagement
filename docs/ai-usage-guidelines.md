# AI Usage Guidelines

Governs how Modules 26–30 (the product's own AI features) are built and how AI-assisted coding is used to build TaskPlatform itself — two different things, both covered here since both matter to this project.

## Part 1 — AI features *within* TaskPlatform (Modules 26–30)

### The one non-negotiable rule: human confirmation before data changes

No AI module writes directly into core domain tables (Task, Project, Milestone, ...). Every AI output lands in that module's own draft table first (`AiGeneratedTask`, `AiExtractedItem`, `AiScheduleSuggestion`) and becomes real data only when a human explicitly accepts it — at which point it goes through the exact same `ITasksService`/etc. call path (and therefore the exact same business rules) as a manually-created row would (BR-27-01, BR-29-01; see [business-rules.md](business-rules.md)). This is true even for Module 28 (Smart Scheduler), whose spec wording ("automatically optimizes") sounds more autonomous than the implementation actually is — see [scope.md](scope.md)'s explicit deferral of auto-apply.

### Data the AI can see

An AI Assistant (Module 26) query, or any other AI module's context, is scoped to exactly what the requesting user could already see through the normal permission system ([user-roles.md](user-roles.md)) — an AI feature is never a way to bypass RBAC by asking a question instead of calling an endpoint. Enforced by having the AI modules call the same `IServices` interfaces (with the same caller-identity context) as every other consumer, never a privileged "AI service account" with broader access.

### Provider abstraction

Every AI module depends on `IAIProvider`, never a vendor SDK type (ADR-008) — see [tech-stack.md](tech-stack.md), [third-party-integrations.md](third-party-integrations.md). This keeps prompt-engineering and business logic separable: the `IAIProvider` implementation owns "how do I talk to Claude/OpenAI," the module's `Application/Services` owns "what do I ask for and what do I do with the answer."

### Prompt/context construction

- Never send more of the caller's data into a prompt than the specific feature needs (Module 27 generating a plan from a one-line brief doesn't need the caller's entire task history; Module 26 answering "what should I work on" does need their open tasks, but not their whole organization's).
- Every prompt template lives in [ai-prompts.md](ai-prompts.md) — not inlined ad hoc in service code — so prompt changes are reviewable as a doc diff and reusable across the module and its tests.

### Cost & latency (NFR-10)

- Nothing in Modules 26–30 blocks a request thread waiting on a multi-second LLM call. Long operations (Task Generator, Meeting Notes extraction) run as Hangfire background jobs; the client polls or receives a SignalR push when the result is ready (see [frontend-state-management.md](frontend-state-management.md)).
- Rate limiting (see [error-handling.md](error-handling.md), [caching-strategy.md](caching-strategy.md)) applies per-user and per-Organization on every AI endpoint specifically because AI calls carry real marginal cost, unlike a normal CRUD endpoint.

### Testing

Business logic in AI modules is tested against a fake `IAIProvider` returning deterministic canned responses — never a live model call in CI (cost + determinism). See [testing-strategy.md](testing-strategy.md) "AI module testing."

### Transparency to the end user

Every AI-generated draft is visibly labeled as AI-generated in the UI (not merged indistinguishably with human-created data) until accepted — after acceptance, the resulting Task/etc. is a normal record with normal provenance (who accepted it, when — logged to `ActivityLogEntry` like any other creation), not permanently tagged "AI-made" forever.

## Part 2 — Using AI coding assistance to build TaskPlatform itself

### What AI-assisted coding can touch

Any code in this repo, subject to the same PR/review process as human-written code — there is no separate, lighter-weight review path for AI-generated code. The [coding-standards.md](coding-standards.md) PR checklist applies identically.

### What needs extra human scrutiny before merging AI-authored (or AI-assisted) changes

- **Anything in `auth.md`'s three named bug classes** — ownership checks, RoleId-claim shape, account-creation login-gating — since these are exactly the kind of subtle, easy-to-miss issues that have already recurred multiple times in this workspace's other product, including in AI-assisted sessions. A human should explicitly verify these three, not assume an AI assistant checked them.
- **New migrations** — verify a migration was actually generated (not hand-written to merely look like one) and that it applies cleanly per [migrations.md](migrations.md)'s CI gate — never trust a description of "I added a migration" without the CI check having actually run green.
- **Module boundary violations** — an AI assistant working across multiple files in one session is exactly the situation most likely to accidentally reach into another module's `Infrastructure` for convenience; the [coding-standards.md](coding-standards.md) review checklist item exists partly for this reason.
- **Docs drift** — if this docs/ set (architecture, api-endpoints, business-rules, etc.) isn't updated in the same PR as a code change that contradicts it, treat that as a review blocker, not a follow-up task — a docs set that silently diverges from the code stops being trustworthy for the next reader (human or AI).

### What AI-assisted coding should not do unsupervised

- Apply a migration directly to a Staging/Production database (always through the CI/CD pipeline, [deployment.md](deployment.md)) — an interactive session running `dotnet ef database update` against anything but a local dev DB is out of bounds.
- Rotate/regenerate a real secret (JWT signing key, API keys) — that's a [configuration.md](configuration.md) operational action with real consequences (invalidates every existing token/session), done deliberately by a human, not as a side effect of a coding session.
- Force-push, delete a branch, or bypass a CI check to "unblock" a merge — see [git-workflow.md](git-workflow.md)'s "Never" list, which applies regardless of who/what is driving the commit.
