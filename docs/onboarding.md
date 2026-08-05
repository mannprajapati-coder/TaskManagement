# Onboarding — Steps for a New Developer Joining This Project

## Day 1

1. Read, in this order: [README.md](README.md) → [architecture.md](architecture.md) → [domain-model.md](domain-model.md) → [user-roles.md](user-roles.md) → [plan.md](plan.md). This gives the shape of the system and where the current phase (see [plan.md](plan.md)) sits before touching any code.
2. Follow [environment-setup.md](environment-setup.md) top to bottom — clone, restore, secrets, DB, run both `Api` and `Web` locally, register a first account.
3. Skim [scope.md](scope.md) — specifically the "out of scope" and "ambiguous items resolved" tables, so early questions like "should Gantt be editable" or "how does billing actually work" are answered before they're asked in chat/standup.

## First week

4. Read [design-decisions.md](design-decisions.md) fully — every ADR explains a "why," and several exist specifically because a naive-but-reasonable alternative was already considered and rejected (e.g. "why not a SPA," "why not microservices," "why HTTP hop with only one frontend"). Re-proposing one of these without reading the ADR first is the single most avoidable source of repeated discussion.
5. Read [coding-standards.md](coding-standards.md) and [git-workflow.md](git-workflow.md) before opening a first PR — the module-boundary rule and the PR checklist are enforced in review, not just suggested.
6. Pick a small, well-scoped task from the current phase in [plan.md](plan.md) — ideally something inside one module, to exercise the create-a-module/add-an-endpoint/write-a-migration loop end to end once (see [folder-structure.md](folder-structure.md) "Adding a new module").

## Standing references, come back to these as needed

- [business-rules.md](business-rules.md) — before implementing any feature, check whether a `BR-xx-yy` already defines the exact edge-case behavior expected.
- [api-conventions.md](api-conventions.md) + [api-endpoints.md](api-endpoints.md) — before adding any endpoint.
- [testing-strategy.md](testing-strategy.md) — what test coverage a PR is expected to include.
- [auth.md](auth.md) — specifically the "Ownership checks" and "Account-creation must actually enable login" sections; these are the two bug classes most likely to slip through review if not actively checked for.
- [known-issues.md](known-issues.md) and [faq.md](faq.md) — before spending time re-investigating something someone else already ran into.

## Who to ask

Solo/small-team project in its planning stage — see [plan.md](plan.md) for current phase status and [changelog.md](changelog.md) for what's actually shipped vs. still planned. Update this section with real names/contacts once the team is staffed; left generic here since this docs set was authored before implementation began.

## Common first-week mistakes (pre-empted, not hypothetical — see the reasoning in the linked docs)

- Reaching into another module's `Infrastructure`/`Domain.Entities` directly instead of through its `IServices` — see [coding-standards.md](coding-standards.md) "Module boundaries."
- Hand-editing the database instead of writing a migration — see [migrations.md](migrations.md).
- Trusting a client-supplied id without an ownership check — see [auth.md](auth.md).
- Building a feature Web-side that calls a module directly instead of going through `TaskPlatform.Api` — see ADR-002.
