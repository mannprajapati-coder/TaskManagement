# Known Issues & Limitations

This docs set was authored from the functional specification before any code exists — there are no *bugs* yet. What follows are the **known, deliberate limitations** baked into the v1 design, so nobody rediscovers them mid-build and assumes they're accidental. Once real code ships, add real bugs/limitations here too, each with the phase/PR that introduced it and (once fixed) the PR that closed it — don't let this file silently go stale.

## Deliberate v1 limitations (all cross-referenced to where the decision lives)

| Limitation | Where decided | Why it's acceptable for v1 |
|---|---|---|
| Gantt chart is read-only, no drag-to-reschedule | [scope.md](scope.md) | Spec's own Module 22 wording doesn't ask for editing; real work to reconcile with Dependencies (BR-13-01/13-02) if added. |
| AI Smart Scheduler never auto-applies a suggestion | [ai-usage-guidelines.md](ai-usage-guidelines.md), [feature-flags.md](feature-flags.md) | Human-in-the-loop is a deliberate safety choice, not a missing feature. |
| No push notifications | [scope.md](scope.md) | No mobile app exists yet to receive them; spec itself marks this "(Future)." |
| No live billing/payment gateway | [scope.md](scope.md), [third-party-integrations.md](third-party-integrations.md) | Spec gives no payment provider detail to build against; `Subscription.Tier` is a plain field today. |
| Developer and Tester roles share one permission set | [user-roles.md](user-roles.md) | Spec names them separately but gives no differing permission — split later only if a real behavioral difference emerges. |
| No SSO/SAML | [scope.md](scope.md) | Not named in spec; only Google login is. |
| Manual E2E only, no automated browser test suite | [testing-strategy.md](testing-strategy.md) | Matches team size/timeline; revisit once surface area outgrows manual click-through per milestone. |

## Open architectural risks to watch during build (not yet problems, just named so they don't surprise anyone)

- **Account-creation flows that gate login on a status flag** (email verification, invite-accept, Google-first-login) — this workspace's other product independently hit the same bug three times across different onboarding flows: the flag that gates "can this account log in" was left in its default off state by the creation code, even though the login code itself was correct. Explicitly re-check every new onboarding path in Phases 1–2 against [auth.md](auth.md)'s "Account-creation must actually enable login" section rather than assuming it's obviously fine.
- **`ActivityLogEntry` growth rate** — append-only, every mutation writes one; no archival strategy defined yet (see [logging-monitoring.md](logging-monitoring.md) retention note). Watch table size/query latency once Phase 5 is live with real usage.
- **30 separate migration histories against one database** ([migrations.md](migrations.md)) — cross-module FK ordering during a fresh `database update` is currently handled by convention (seed/apply in phase order); if a genuine circular cross-module FK need ever emerges, that's a real design problem to escalate, not something to solve with migration ordering tricks.
- **AI provider cost at scale** — NFR-10's background-job pattern avoids blocking requests, but the actual per-request cost of Modules 26–30 hasn't been load-tested against real usage; watch spend once Phase 8 is live (see [ai-usage-guidelines.md](ai-usage-guidelines.md)).

## Template for future entries (once real bugs exist)

```
### [Module] Short title
**Found:** date, during which phase/PR
**Impact:** who's affected, how
**Workaround:** if any, until fixed
**Status:** Open / Fixed in PR #123 / Won't fix (link to the scope.md decision if deliberate)
```
