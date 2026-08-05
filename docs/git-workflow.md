# Git Workflow

## Branching

- `main` — always deployable; every merge to `main` triggers the staging deploy pipeline (see [deployment.md](deployment.md)).
- Feature branches named `<short-description>` in imperative form, matching the convention already in use on this workspace's other repo (e.g. `ProdLoginExecptionWork`, not `feature/prod-login-exception-work`) — short, descriptive, no mandatory ticket-number prefix.
- One branch per module-slice of a phase (see [plan.md](plan.md)) rather than one branch per whole phase — keeps PRs reviewable and matches the module-per-`.csproj` boundary in [folder-structure.md](folder-structure.md).
- Merge `main` into a long-lived feature branch periodically rather than letting it drift for a whole phase — the existing repo's own history (`Merge branch 'main' into ProdLoginExecptionWork`) is the pattern to keep following.

## Commit messages

Plain, imperative, descriptive — matching this workspace's actual existing convention (`Enhance file upload validation and error handling`, `Refactor API handling and enhance null checks`, `Exclude owner doctor from branch doctor list`), not Conventional Commits (`feat:`/`fix:` prefixes). One line stating what changed and, where it's not obvious from the diff, why. Reference a `FR-xx-y` or `BR-xx-yy` id in the body when the commit implements/fixes one, so `git log --grep` can find every commit behind a given requirement.

## PRs

- Every PR description states: which module(s) it touches, which `FR-xx-y`/`BR-xx-yy` it implements, and whether it includes a migration ([migrations.md](migrations.md)).
- Review checklist: [coding-standards.md](coding-standards.md)'s PR checklist section — module-boundary check, migration-included check, ownership-check-present check, docs-updated check.
- Squash-merge to `main` by default, so `main`'s history is one commit per shipped PR — keeps `git log` on `main` readable at the phase/module granularity that matters for [changelog.md](changelog.md).

## Never

- Never commit directly to `main` — even a one-line docs fix goes through a PR, so CI (build + migration-apply check, see [deployment.md](deployment.md)) runs on it.
- Never force-push a shared branch once anyone else has pulled it.
- Never merge a PR with a red CI check, including the "migrations apply cleanly to a fresh database" gate ([migrations.md](migrations.md)) — that gate exists specifically to prevent the exact "DB script silently never applied" failure mode seen repeatedly in this workspace's other product.

## Tags / releases

Tag `main` at each milestone boundary from [plan.md](plan.md) (`v0.1-foundation`, `v0.2-workspace`, ... `v1.0`) — gives [changelog.md](changelog.md) and [deployment.md](deployment.md) a concrete reference point per phase rather than only ever pointing at a moving `main`.
