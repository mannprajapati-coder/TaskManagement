# Authentication & Authorization

## Identity provider

ASP.NET Core Identity (`User` in [database-schema.md](database-schema.md) is Identity's user table, extended with `ProfilePictureUrl`, `GoogleSubjectId`, etc.) — chosen over a hand-rolled login (Sawi's simpler approach) because the spec explicitly asks for email verification, password reset, refresh tokens, Google login, and optional MFA (FR-01-1 through FR-01-7), which is exactly what Identity's token providers give for free instead of hand-building each one.

## Token model

- **Access token**: JWT, 15-minute expiry, signed with an asymmetric key (RS256) so `TaskPlatform.Web` and any future third-party consumer of `TaskPlatform.Api` can verify it without sharing a symmetric secret.
- **Refresh token**: opaque random value, stored hashed (never plaintext) in `RefreshToken`, 30-day sliding expiry, **rotates on every use** (BR-01-03) — reuse of an already-rotated token invalidates the whole token family (every token descended from the same original login), the standard mitigation for a stolen refresh token being replayed after the legitimate client already rotated past it.
- `TaskPlatform.Web` holds the refresh token in an `HttpOnly`, `Secure`, `SameSite=Strict` cookie — never in `localStorage`/JS-accessible storage. The access token is kept server-side (in the auth cookie's encrypted ticket, mirroring how this workspace's existing `WebAuthHelper`-equivalent pattern already keeps the JWT out of the browser's JS reach) and attached as a Bearer header only on the server-to-server `Web → Api` call.

## Claims

Every access token carries:

| Claim | Source | Used for |
|---|---|---|
| `sub` (UserId) | `User.Id` | identity |
| `OrganizationId` | the Organization the user is currently acting in (a user can belong to multiple — see [domain-model.md](domain-model.md); switching Organization issues a fresh token) | every EF Core global query filter (ADR-006) |
| `RoleId` | `OrganizationMembership.RoleId` for the current `OrganizationId` | base RBAC check ([user-roles.md](user-roles.md)) |
| `email` | `User.Email` | display, audit log actor |

**Numeric `RoleId`, never the role name as a string.** This exact bug (a role-specific login path putting the role *name* into the claim instead of its numeric id, silently breaking every downstream `claim.RoleId` check) has bitten this workspace's other product more than once — `AuthController.Login` is the **only** code path that issues a token, precisely so there's no second, divergent login path that could get this wrong. See "One login path" below.

## One login path — no per-role forks

Every role (Owner through Guest) authenticates through the exact same `Auth/Login` action. There is deliberately no role-specific login controller/action, because this workspace has already independently discovered, three separate times (once per new actor type added to its other product), that a second login path inevitably drifts from the first — either in the RoleId-claim shape, in how pending/inactive accounts are distinguished from a wrong password, or in whether it even remembers to set the account's login-gating flag at all. TaskPlatform avoids the whole bug class by construction: one path, used by all 8 roles, tested once.

## Ownership checks (IDOR prevention)

Any endpoint that takes an id identifying "which team/project/task am I allowed to touch" from the request body or query string must resolve the caller's *own* scope from their JWT claim first and compare — never trust a client-supplied id as sufficient proof of access. Concretely:

```csharp
// wrong — trusts the client
var task = await _tasksService.GetByIdAsync(request.TaskId);

// right — resolves caller's access first
var task = await _tasksService.GetByIdAsync(request.TaskId);
if (!await _projectMembersService.HasAccessAsync(task.ProjectId, currentUser.Id))
    return Forbid();
```

This is a standing item on the PR review checklist (see [coding-standards.md](coding-standards.md)) for exactly the reason above — it's the single most common real bug class found late, after a feature already "works" for the happy path, in this workspace's other product.

## Account-creation must actually enable login

Every code path that creates a `User`+`OrganizationMembership` (registration, invite-accept, Google-login-first-time) must explicitly set the account into a state where login actually succeeds once its preconditions are met (email verified, or admin-approved where that applies) — checked explicitly in the code, not assumed from "the entity has an IsActive-equivalent flag so it must default correctly." This workspace's other product hit the identical bug (a flag silently left in its default "can't log in" state) independently in three different onboarding flows; treat "does this new account-creation path actually leave the account able to log in" as a mandatory manual check on every PR that adds one, not an afterthought.

## Authorization layers

1. **`[Authorize]`** — is there a valid token at all.
2. **`RoleClaimAuthorizeFilter`-equivalent `IAsyncActionFilter`** — does the caller's Role (+ any Team/Project-scoped override, per [user-roles.md](user-roles.md)'s override rules) grant the permission this action requires. Declared per-action via an attribute naming the required `PermissionKey`.
3. **Ownership check** (above) — does the caller actually have access to *this specific* resource id, not just the permission in the abstract.
4. **Tenant query filter** (ADR-006) — the last line of defense; even if 1–3 were somehow bypassed, no query can return a row from another Organization.

All four layers are independent and all four must pass — this is deliberate defense in depth, not redundancy to be "simplified" later.

## Multi-Factor Authentication

TOTP-based (Google Authenticator-compatible), user-opt-in via `Auth/EnableMfa` (FR-01-7). Not mandatory for v1 for any role, including Owner — revisit as a per-Organization *policy* (Admin can require it for all members) in a later phase once there's a real security-policy screen to attach it to (see [feature-flags.md](feature-flags.md)).

## Google login

OAuth 2.0 / OpenID Connect via `Auth/GoogleLogin`. A Google-authenticated email is treated as pre-verified (BR-01-02) — no separate email-verification step for that path. If the email matches an existing password-based account, the accounts are linked (one `User` row gains a `GoogleSubjectId`), never silently creating a duplicate account for the same email.

## Session / cookie boundary

`TaskPlatform.Web`'s own session is a standard ASP.NET Core cookie-auth ticket; the JWT/refresh token pair lives inside that ticket's encrypted payload, not as a separate visible cookie. Logging out of `Web` calls `Auth/Logout` (revokes the refresh token server-side) before clearing the cookie — a cleared cookie alone does not revoke the underlying token, which matters for the "view/revoke active sessions" feature (FR-02-4) working correctly across devices.

## Password storage

ASP.NET Core Identity's default `PasswordHasher` (PBKDF2, per-user salt, configurable iteration count) — no custom hashing scheme.

## Transport & headers

HTTPS everywhere, including local dev (matches Sawi's existing `launchSettings.json` convention of an `https` profile by default). `TaskPlatform.Api` sets `Strict-Transport-Security`, and CORS is locked to `TaskPlatform.Web`'s own origin(s) only — no wildcard, since the API is not yet a public third-party surface (see [scope.md](scope.md)).
