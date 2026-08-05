# Sprint 01 — Authentication (Module 1)

**Status:** Completed
**Started:** 2026-08-05          **Completed:** 2026-08-05
**Actual days spent:** 1   (Est. 6 days)

## Objective

Stand up the `Authentication` module and both host projects' skeletons far enough that a user can register, verify their email, log in, refresh their session, log in with Google, and optionally enable MFA — the one login path every other role and every later sprint depends on (`docs/auth.md` "One login path — no per-role forks").

## Included features / Requirements covered

`docs/requirements.md` FR-01-1 through FR-01-7. See `docs/api-endpoints.md` "Authentication" table for the exact endpoint list to implement — build every row in that table this sprint, not a subset.

## Task breakdown

1. **Solution skeleton** — create `TaskPlatform.sln`; add `TaskPlatform.Api`, `TaskPlatform.Web`, `TaskPlatform.Shared` empty ASP.NET Core projects per `docs/folder-structure.md`. Get both `Api` and `Web` running with a placeholder home page before writing any real feature — confirms the solution/project references are wired correctly first.
2. **Authentication module scaffold** — create `Modules/Authentication/Authentication.csproj` with `Domain/Entities`, `Domain/IServices`, `Application/Services`, `Application/Profiles`, `Application/Extensions`, `Infrastructure/Context`, `Infrastructure/Repositories` (empty folders are fine to start — see `docs/coding-standards.md` "Adding a new module").
3. **`User` entity + Identity setup** — this actually lives in the `UserManagement` module per `docs/database-schema.md`, but ASP.NET Core Identity's `IdentityDbContext` needs to be stood up now since Authentication depends on it. Create a minimal `User : IdentityUser<Guid>` in `Modules/UserManagement/Domain/Entities` this sprint (full profile fields come in Sprint 02) — just enough to authenticate against.
4. **`AuthenticationDbContext`** — `RefreshToken`, `EmailVerificationToken`, `PasswordResetToken`, `MfaSecret` per `docs/database-schema.md` Module 1 table.
5. **First migration** — `dotnet ef migrations add InitialAuthenticationSchema --project Modules/Authentication --startup-project TaskPlatform.Api`. Apply it locally, confirm the tables exist, before writing any service code against them.
6. **Register + Email Verification** — `IAuthService.RegisterAsync` (hash password via Identity's `PasswordHasher`, create `EmailVerificationToken`, send verification email via `IEmailSender`) and `VerifyEmailAsync`. Wire `POST Auth/Register` / `POST Auth/VerifyEmail` in `TaskPlatform.Api/Controllers/AuthController.cs`.
7. **Login + JWT issuance** — `LoginAsync` checks password, checks `IsEmailVerified` (BR-01-02 — skip this check if the account has a `GoogleSubjectId`, since Google already verified it, even though Google login itself is step 10), issues an access token (RS256 JWT, claims per `docs/auth.md` "Claims" table — `sub`, `OrganizationId`, `RoleId`, `email`) and a refresh token (`RefreshToken` row, hashed value stored). **`OrganizationId`/`RoleId` claims will be empty/placeholder this sprint** since Organization (Module 3) doesn't exist yet — real values wire in during Sprint 02; don't block on this, just leave the claim-population code with a clear `// TODO Sprint 02` marker pointing at this exact line.
8. **Refresh Token rotation** — `RefreshTokenAsync`: validate the presented refresh token, issue a new access+refresh pair, mark the old refresh token row `RevokedAt`, link new→old via `Family`/`RotationId`. Implement the reuse-detection check (BR-01-03) now, don't defer it — see Pattern Notes below, this is the trickiest piece of the sprint.
9. **Forgot/Reset Password** — `ForgotPasswordAsync` (issues `PasswordResetToken`, emails a link), `ResetPasswordAsync` (validates token not expired/not used, sets new password, marks token used, **revokes every refresh token for that user** per BR-01-01).
10. **Google Login** — `GoogleLoginAsync`: verify the Google ID token server-side (`Google.Apis.Auth`), find-or-create a `User` by email, link `GoogleSubjectId` if the email already exists as a password account (never silently create a duplicate — see `docs/business-rules.md` "Google login" note in `docs/auth.md`), issue tokens same as normal login.
11. **MFA (optional, FR-01-7)** — `EnableMfaAsync` (generate TOTP secret, return QR-code payload), `VerifyMfaAsync` (validate a 6-digit code against the stored secret). Login flow: if `MfaSecret.IsEnabled`, login returns a short-lived "MFA challenge" token instead of the real access/refresh pair; `VerifyMfa` exchanges the challenge for the real pair.
12. **Logout** — revoke the current refresh token.
13. **Rate limiting** — apply ASP.NET Core's built-in rate limiter to `Auth/Login`/`Auth/ForgotPassword`/`Auth/Register` per `docs/error-handling.md` "Rate limiting responses" — a handful of lines of middleware config, don't skip it just because it's not core business logic.
14. **Web-side thin controller** — `TaskPlatform.Web/Controllers/AuthController.cs` + Login/Register/ForgotPassword/ResetPassword views, calling `Api` through `TaskPlatform.Shared/ApiService` (build the minimal version of `ApiService` needed for these 8 calls now — it grows in every later sprint).
15. **Tests** — unit tests for BR-01-01/02/03 by ID (see `docs/testing-strategy.md`); integration test hitting `Auth/Register` → `Auth/VerifyEmail` → `Auth/Login` → `Auth/RefreshToken` end to end via `WebApplicationFactory`.

## Pattern notes — refresh token rotation + reuse detection (step 8)

This is the one piece of this sprint most likely to be subtly wrong on a first attempt — worked skeleton:

```csharp
public async Task<TokenPairResult> RefreshTokenAsync(string presentedRefreshToken)
{
    var hash = Hash(presentedRefreshToken);
    var stored = await _refreshTokens.FindByHashAsync(hash);

    if (stored is null)
        throw new DomainException("Invalid refresh token.");

    if (stored.RevokedAt is not null)
    {
        // This token was already rotated away from once before — someone is
        // replaying an old token. Nuke the whole family, not just this token.
        await _refreshTokens.RevokeFamilyAsync(stored.FamilyId);
        throw new DomainException("Token reuse detected — all sessions revoked.");
    }

    stored.RevokedAt = DateTime.UtcNow;
    var next = RefreshToken.CreateChild(stored); // same FamilyId, new value/hash
    await _refreshTokens.AddAsync(next);
    await _refreshTokens.SaveChangesAsync();

    return IssueTokenPair(stored.UserId, next);
}
```

The bug this prevents: without the `RevokedAt is not null → revoke whole family` branch, a stolen-and-already-used refresh token can still be replayed successfully (the naive version would just check "does this token exist," not "has it already been rotated away from").

## Dependencies

None — this is the first sprint.

## Risks

- Building JWT claim population before Organization/Role exist (step 7) — mitigated by the explicit `// TODO Sprint 02` marker; do not skip writing the claim-population code structure now just because the values are placeholders, since Sprint 02 needs a specific line to edit, not a whole new code path to build.
- MFA (step 11) is genuinely optional per FR-01-7 — if time-boxed and running short, it's the one item in this sprint safe to defer to a follow-up without blocking Sprint 02 (nothing later in the plan depends on MFA existing yet).

## Deliverables

- `TaskPlatform.sln` with `Api`/`Web`/`Shared`/`Modules/Authentication`/`Modules/UserManagement` (minimal) projects.
- All 9 `Authentication` endpoints from `docs/api-endpoints.md` working, tested.
- `TaskPlatform.Web`'s Login/Register/ForgotPassword/ResetPassword pages functional end to end against a running `Api`.

## Acceptance criteria

- [x] A brand-new user can register, receive a verification email (Papercut/Mailtrap in dev), verify, and log in.
- [x] A stolen-and-reused refresh token revokes the whole session family (write this as an actual test, not just a manual check).
- [x] Resetting a password invalidates every other active session for that user (BR-01-01).
- [x] Google login correctly links to an existing password account by email rather than duplicating it.
- [x] `dotnet ef database update` for the `Authentication` module applies cleanly to a fresh database in CI.

## Definition of Done

See `00-Sprint-Master-Plan.md` "Definition of Done, applied at the sprint level" — applied here with no exceptions.

## Review checkpoint (have someone else, or your next-day self, specifically re-check)

- The reuse-detection branch in the Pattern Notes skeleton — this is the highest-value thing to get a second pair of eyes on this sprint.
- That the access token's signing key is RS256 (asymmetric), not accidentally HS256 with a shared secret (`docs/tech-stack.md`/`docs/auth.md` both specify RS256 — an easy default to get wrong since ASP.NET Core's JWT samples often default to HS256).
- That no plaintext refresh token or password ever appears in a log line (`docs/logging-monitoring.md` "What's never logged").
