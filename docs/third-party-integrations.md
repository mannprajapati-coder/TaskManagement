# Third-Party Integrations

## Google OAuth (Module 1 — FR-01-6)

- **Purpose**: Google login/signup.
- **Config needed**: `GoogleAuth:ClientId`, `GoogleAuth:ClientSecret` (Google Cloud Console → OAuth consent screen + credentials), redirect URI registered per environment (`https://localhost:7400/signin-google` dev, real domain in Staging/Prod).
- **Where used**: `Authentication` module, `Auth/GoogleLogin` endpoint ([api-endpoints.md](api-endpoints.md)).
- **Data shared**: email, display name, profile picture URL — nothing else requested from the consent scope.

## Email delivery (Modules 1, 19)

- **Dev**: Papercut SMTP (local, no real send) or Mailtrap sandbox.
- **Prod/Staging**: SendGrid (or equivalent transactional email provider).
- **Config needed**: `Email:ApiKey`, `Email:FromAddress`, `Email:FromName`.
- **Where used**: email verification, password reset ([auth.md](auth.md)), email-channel notifications (BR-19-02), report delivery (optional, if a report is emailed rather than only downloaded).
- **Library**: MailKit (see [tech-stack.md](tech-stack.md)) — abstracted behind `IEmailSender` so swapping provider is a config + adapter change, not a rewrite.

## File storage (Module 17 — Attachments, plus profile pictures Module 2)

- **Dev**: local disk under `TaskPlatform.Api/wwwroot/Uploads` (matches this workspace's other product's existing convention).
- **Prod/Staging**: Azure Blob Storage.
- **Config needed**: `Storage:Provider` (`Local`/`AzureBlob`), `Storage:AzureBlob:ConnectionString`, `Storage:AzureBlob:ContainerName`.
- **Where used**: behind `IFileStorageService` (ADR — see [design-decisions.md](design-decisions.md) philosophy applied here) so `AttachmentsController`/`UsersController` never know which backend is active.
- **Validation**: server-side type/size checks per BR-17-02, regardless of backend.

## AI provider (Modules 26–30)

- **Primary**: Anthropic Claude (via the Claude API — Messages API for Q&A/generation; pick the current model tier at build time based on latency/cost needs per AI module).
- **Fallback/alternative**: any OpenAI-compatible provider, swappable because every AI module depends only on `IAIProvider` (ADR-008), never a vendor SDK type.
- **Config needed**: `AiProvider:Name`, `AiProvider:ApiKey`, `AiProvider:Model`, `AiProvider:MaxTokens`, `AiProvider:TimeoutSeconds`.
- **Where used**: `AIAssistant`, `AITaskGenerator`, `AISmartScheduler`, `AIMeetingNotes`, `AIAnalytics` — each calls `IAIProvider.GenerateAsync`/`StreamAsync`, never the vendor client directly.
- **Cost/latency handling**: see [ai-usage-guidelines.md](ai-usage-guidelines.md) and NFR-10 ([requirements.md](requirements.md)) — background-job pattern, not a blocking synchronous call on the main request thread.

## Redis (infrastructure, not a "third party" business integration, but external to the app process)

- **Purpose**: SignalR backplane, distributed cache, rate-limit counters — see [caching-strategy.md](caching-strategy.md).
- **Config needed**: `Redis:ConnectionString`.

## Logging/monitoring sink (Staging/Prod only)

- **Choice**: Seq (self-hosted) or Azure Application Insights — either is compatible with the Serilog sink already chosen in [tech-stack.md](tech-stack.md); pick whichever this workspace's infra already has an account/subscription for, to avoid onboarding a second monitoring vendor for one product.
- **Config needed**: `Serilog:WriteTo:Seq:ServerUrl` or `ApplicationInsights:ConnectionString`.

## Export libraries (Module 25 — Reports)

- **PDF**: QuestPDF (or similar open-source .NET PDF generator).
- **Excel**: ClosedXML.
- **CSV**: `CsvHelper`, or plain `System.Text` composition given CSV's simplicity — no heavyweight dependency needed for this one.
- These are libraries, not services with API keys/accounts — listed here for completeness of "what does Reports depend on," not because they need [configuration.md](configuration.md) secrets.

## Explicitly not integrated in v1

- Payment/billing gateway (Stripe, etc.) — see [scope.md](scope.md) "Billing" row; `Subscription.Tier` is a plain field with no live payment processing behind it yet.
- SSO/SAML — see [scope.md](scope.md).
- Any incoming third-party webhook consumer, or outgoing public webhook delivery beyond the internal-use shape — see [webhooks.md](webhooks.md).
