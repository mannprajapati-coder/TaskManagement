# Configuration

## Layering

Standard ASP.NET Core configuration layering, lowest to highest precedence:

1. `appsettings.json` — safe defaults, no secrets, committed to source control.
2. `appsettings.{Environment}.json` (`Development`/`Staging`/`Production`) — environment-specific non-secret values (log level, feature flag defaults), committed.
3. **User Secrets** (dev only, `dotnet user-secrets`) — never committed, holds local connection strings/API keys (see [environment-setup.md](environment-setup.md)).
4. **Azure Key Vault** (Staging/Production) — holds every actual secret; the app reads Key Vault references at startup via `AddAzureKeyVault(...)`, so no secret value ever exists in an `appsettings*.json` file or an environment variable dump.
5. Environment variables — used only for the handful of values that must be set by the hosting platform itself (e.g. `ASPNETCORE_ENVIRONMENT`), not as a secrets mechanism.

## Configuration keys (non-exhaustive, grows per module)

```
ConnectionStrings:Default                    SQL Server connection string
Redis:ConnectionString
Jwt:SigningKey / Jwt:AccessTokenMinutes / Jwt:RefreshTokenDays
GoogleAuth:ClientId / GoogleAuth:ClientSecret
Email:Provider / Email:ApiKey / Email:FromAddress / Email:FromName
Storage:Provider / Storage:AzureBlob:ConnectionString / Storage:AzureBlob:ContainerName
AiProvider:Name / AiProvider:ApiKey / AiProvider:Model / AiProvider:MaxTokens / AiProvider:TimeoutSeconds
Serilog:MinimumLevel / Serilog:WriteTo:Seq:ServerUrl / ApplicationInsights:ConnectionString
Hangfire:ConnectionString
Cors:AllowedOrigins
FeatureFlags:*                                see feature-flags.md
```

## Per-module configuration ownership

Each module's `Application/Extensions/ServiceCollectionExtensions.cs` (`AddXModule(...)`, see [folder-structure.md](folder-structure.md)) binds and validates its own configuration section via `IOptions<T>` with a startup-time validation check (`ValidateDataAnnotations()` / a custom `IValidateOptions<T>`) — a missing/malformed config value for a module fails fast at app startup with a clear message, never as a mysterious runtime `NullReferenceException` deep in a request three weeks after deploy.

## Secrets rotation

- JWT signing key: rotatable via a key-id-aware scheme (support verifying tokens signed by the previous key for a grace period after rotation) — not a hardcoded single key with no rotation story.
- AI provider key, email provider key, Google client secret: rotated directly in Key Vault; the app picks up the new value on next restart (or immediately if Key Vault reference caching is configured with a short refresh interval) — no code change needed to rotate a secret.

## Multi-environment values that legitimately differ (not secrets, still worth tracking explicitly)

| Key | Dev | Staging | Prod |
|---|---|---|---|
| `Jwt:AccessTokenMinutes` | 60 (convenience while developing) | 15 | 15 |
| `Serilog:MinimumLevel` | Debug | Information | Information |
| `Cors:AllowedOrigins` | `https://localhost:7400` | Staging Web URL | Prod Web URL |
| `Storage:Provider` | `Local` | `AzureBlob` | `AzureBlob` |
| Rate-limit thresholds ([error-handling.md](error-handling.md)) | relaxed/off | prod-equivalent (so Staging catches a misconfigured limit before Prod does) | strict |

## What never goes in `appsettings.json`

Connection strings with credentials, any API key, the JWT signing key, OAuth client secrets — enforced by a pre-commit check (or CI secret-scanning step, see [deployment.md](deployment.md)) that fails a PR if a plausible secret pattern is detected in a committed `appsettings*.json`.
