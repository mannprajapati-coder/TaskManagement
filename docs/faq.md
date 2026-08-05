# FAQ

Questions that come up repeatedly while reading the spec or planning the build — answered where there's a real answer, flagged as genuinely open where there isn't. If a question here gets asked again after being answered, that's a signal the answer needs to move somewhere more prominent (linked doc's own top section), not just live here.

## "Why is this one shared app instead of one deployable per role, like this workspace's other product?"
Because TaskPlatform's roles all collaborate on the same boards/projects in real time; a per-role split works when actors never share a screen, which isn't true here. Full reasoning: ADR-001 in [design-decisions.md](design-decisions.md).

## "Why does Web still call Api over HTTP if there's only one frontend?"
Consistency with this workspace's established pattern, plus it forces `TaskPlatform.Api` to be a real, independently-correct public contract from day one (useful for a future mobile client or webhooks). ADR-002.

## "Why 30 separate class libraries instead of grouping by phase?"
The spec itself names 30 modules; keeping that as the code boundary means there's never a lookup table needed to answer "which project is Feature X in." ADR-005.

## "Why real EF Core Migrations instead of the `.sql`-script approach used elsewhere in this workspace?"
Because that approach's exact failure mode (scripts sitting unapplied for weeks, surfacing as live `Invalid object name` errors) has already happened repeatedly elsewhere in this workspace. ADR-004, [migrations.md](migrations.md).

## "How does billing actually work?"
**Open.** The spec names "Billing" as an Organization setting but gives no payment provider, plan tiers, or invoicing detail. v1 models `Subscription.Tier` as a plain field with no live payment processing — see [scope.md](scope.md). Needs a real product decision (which payment gateway, what tiers cost, proration rules) before it can move from "field" to "feature."

## "What's the actual difference between Developer and Tester?"
**Resolved as: none, permission-wise, in v1.** The spec lists them as separate roles but never gives a differing right — see [user-roles.md](user-roles.md)'s note. They're kept as separate role labels for reporting (Module 25's "Employee Performance") only. Split their permissions for real only if a genuine need shows up during build.

## "Can a Guest ever create anything?"
Only comments (see [user-roles.md](user-roles.md)) — chosen so Guest has a coherent, distinct meaning from Viewer (pure read-only) rather than being redundant with it. This is a judgment call made for planning purposes (see [scope.md](scope.md) assumption #4), not something explicitly stated in the spec — worth confirming with whoever owns product decisions once that person exists.

## "Does the AI layer ever act autonomously?"
No, by design — every AI module's output is a draft/suggestion a human explicitly accepts before it becomes real data (BR-27-01, BR-29-01, and AI Smart Scheduler's suggestion-only stance). See [ai-usage-guidelines.md](ai-usage-guidelines.md).

## "Is there a mobile app?"
Not in v1, not named in the spec. `TaskPlatform.Api` is built REST-first specifically so a mobile client is possible later without a backend rewrite — see [scope.md](scope.md), ADR-002's reasoning.

## "What happens if the AI provider is down or rate-limits us?"
AI calls run through the background-job pattern (NFR-10) with retry/circuit-breaker handling (see [error-handling.md](error-handling.md)) — a user sees "still generating" / a graceful failure message, never a hung request or a 500 from an unrelated feature. Non-AI features have zero runtime dependency on AI provider availability (module boundary, [architecture.md](architecture.md) §2).

## "Why SQL Server and not a NoSQL store, given all the JSON-ish fields (Filters, Metadata, Pattern)?"
The domain is relentlessly relational at its core (the Organization→Workspace→Team→Project→Task hierarchy, plus Dependencies' graph structure) — a handful of genuinely schema-flexible fields (recurrence pattern, activity metadata) are modeled as JSON columns *within* the relational schema, which SQL Server supports natively, rather than justifying a second database technology for the whole product. See [tech-stack.md](tech-stack.md) "Explicitly not chosen."

## "Who decides when scope.md's open questions get resolved?"
**Open** — this docs set is planning-stage, authored before a product owner/stakeholder process existed to formally answer things like the Billing question above. Update this FAQ (and [scope.md](scope.md)) with the real answer and who gave it, once that process exists, rather than leaving a stale "TBD."
