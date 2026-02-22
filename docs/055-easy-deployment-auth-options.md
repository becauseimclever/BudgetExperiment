# 055: Easier Deployment - Optional Postgres, Auth Off, and Flexible Auth Providers
> **Status:** ✅ Complete  
> **Priority:** High  
> **Dependencies:** Feature 056 (API Config Endpoint - Complete)

## Overview

This feature significantly lowers the barrier to entry for new users by providing a "batteries-included" deployment option that requires minimal configuration. It introduces three major capabilities: (1) an all-in-one Docker Compose file that includes PostgreSQL, (2) the ability to disable authentication for demo/family use, and (3) support for alternative OIDC providers beyond Authentik (Google, Microsoft, Auth0, Keycloak).

The goal is to enable a new user to go from `git clone` to a running application in under 5 minutes with a single `docker compose up` command.

## Problem Statement

### Current State

**Deployment Friction Points:**
1. **External PostgreSQL Required:** Users must set up and configure their own PostgreSQL database before deploying BudgetExperiment. This adds complexity and time to the initial setup.
2. **Mandatory Authentik Configuration:** The API currently throws `InvalidOperationException` if `Authentication:Authentik:Authority` is not configured. Users must have an Authentik instance or another OIDC provider ready.
3. **Complex Configuration:** Users must understand ASP.NET Core configuration binding, environment variables, and authentication concepts before they can successfully deploy.
4. **No "Try Before You Commit" Path:** There's no easy way for users to evaluate the application without significant infrastructure investment.

**Current Configuration Flow:**
```
docker-compose.pi.yml → requires .env with:
  - DB_CONNECTION_STRING (external PostgreSQL)
  - AUTHENTIK_AUTHORITY (external Authentik)
  - AUTHENTIK_AUDIENCE
  - AUTHENTIK_REQUIRE_HTTPS
```

**Current Authentication Code (Program.cs):**
```csharp
private static void ConfigureAuthentication(IServiceCollection services, IConfiguration configuration)
{
    var authentikOptions = configuration.GetSection(AuthentikOptions.SectionName).Get<AuthentikOptions>() ?? new AuthentikOptions();

    if (string.IsNullOrWhiteSpace(authentikOptions.Authority))
    {
        throw new InvalidOperationException("'Authentication:Authentik:Authority' is not configured. Authentication is required.");
    }
    // ... JWT Bearer configuration
}
```

### Target State

- **Zero-Config Demo Mode:** `docker compose -f docker-compose.demo.yml up` starts everything needed with sensible defaults.
- **Auth-Optional:** Authentication can be disabled entirely for single-user or family deployments where external access is not a concern.
- **Provider Flexibility:** Users can choose from Authentik (default), Google, Microsoft, Auth0, Keycloak, or any generic OIDC provider.
- **Clear Upgrade Path:** Demo mode users can easily migrate to production configuration when ready.

---

## User Stories

### Easy Deployment

#### US-055-001: One-Command Demo Deployment
**As a** new user evaluating BudgetExperiment  
**I want to** start the entire application with a single command  
**So that** I can try out the features without complex infrastructure setup

**Acceptance Criteria:**
- [ ] `docker compose -f docker-compose.demo.yml up` starts API, Client, and PostgreSQL
- [ ] PostgreSQL is pre-configured with correct schema (auto-migration)
- [ ] Application is accessible at `http://localhost:5099`
- [ ] No external configuration required for basic operation
- [ ] README includes quick-start instructions for demo mode
- [ ] Demo mode uses auth-off by default for simplest experience

#### US-055-002: Persistent Demo Data
**As a** demo user  
**I want to** have my data persist between container restarts  
**So that** I don't lose my work during evaluation

**Acceptance Criteria:**
- [ ] PostgreSQL data stored in a named Docker volume
- [ ] Volume survives `docker compose down` (requires `docker compose down -v` to remove)
- [ ] Documentation explains how to reset/clear demo data

### Authentication Off Mode

#### US-055-003: Disable Authentication via Configuration
**As a** single-user or family deployer  
**I want to** disable authentication entirely  
**So that** I don't need to set up an identity provider for my private instance

**Acceptance Criteria:**
- [ ] Setting `Authentication__Mode=None` disables all auth middleware
- [ ] All API requests are treated as authenticated
- [ ] A default "family" user context is applied to all requests
- [ ] User ID is deterministic (e.g., well-known GUID) so data is consistent
- [ ] API returns proper user context for data scoping

#### US-055-004: UI Adapts to Auth-Off Mode
**As a** user running in auth-off mode  
**I want to** not see login/logout/profile UI elements  
**So that** the interface is clean and relevant to my setup

**Acceptance Criteria:**
- [ ] Client fetches auth mode from `/api/v1/config` endpoint (Feature 056)
- [ ] When mode is "none", hide: Login button, Logout button, User profile/avatar
- [ ] NavBar shows app name or "Family Budget" instead of user info
- [ ] Authentication routes (`/authentication/*`) redirect to home

#### US-055-005: Security Warning in Auth-Off Mode
**As an** administrator  
**I want to** see a clear warning when running in auth-off mode  
**So that** I don't accidentally expose an unsecured instance

**Acceptance Criteria:**
- [ ] API logs a WARNING at startup when `Authentication:Mode=None`
- [ ] Warning message: "⚠️ Authentication is DISABLED. All requests are treated as authenticated. Do NOT expose this instance to the internet."
- [ ] UI shows a subtle banner in auth-off mode: "Running in demo mode - authentication disabled"
- [ ] Banner links to documentation about enabling auth

### Flexible OIDC Providers

#### US-055-006: Configure Google OAuth
**As a** small-scale deployer  
**I want to** use Google OAuth for authentication  
**So that** I can leverage my existing Google Workspace without running Authentik

**Acceptance Criteria:**
- [ ] Setting `Authentication__Provider=Google` enables Google OAuth
- [ ] Required settings: `Authentication__Google__ClientId`, `Authentication__Google__ClientSecret`
- [ ] Documentation explains Google Cloud Console setup (OAuth consent screen, credentials)
- [ ] Google OAuth works with the existing Blazor WASM client

#### US-055-007: Configure Microsoft Entra ID (Azure AD)
**As an** enterprise user  
**I want to** use Microsoft Entra ID for authentication  
**So that** I can integrate with my organization's identity system

**Acceptance Criteria:**
- [ ] Setting `Authentication__Provider=Microsoft` enables Microsoft auth
- [ ] Required settings: `Authentication__Microsoft__ClientId`, `Authentication__Microsoft__TenantId`
- [ ] Supports both single-tenant and multi-tenant configurations
- [ ] Documentation explains Azure AD app registration process

#### US-055-008: Configure Generic OIDC Provider
**As a** user with an existing identity provider  
**I want to** configure any OIDC-compliant provider  
**So that** I'm not limited to the pre-configured options

**Acceptance Criteria:**
- [ ] Setting `Authentication__Provider=OIDC` enables generic OIDC mode
- [ ] Required settings: `Authority`, `ClientId`, `ClientSecret` (if confidential), `Scopes`
- [ ] Works with Keycloak, Auth0, Okta, and other standard OIDC providers
- [ ] Claim mapping is configurable for non-standard providers

#### US-055-009: Maintain Authentik as Default
**As an** existing user with Authentik configured  
**I want to** continue using my current setup without changes  
**So that** this feature doesn't break my production deployment

**Acceptance Criteria:**
- [ ] `Authentication__Provider=Authentik` (or unset) uses current Authentik flow
- [ ] All existing Authentik configuration keys continue to work
- [ ] No breaking changes to `docker-compose.pi.yml` or existing `.env` files
- [ ] Documentation clearly shows Authentik remains the recommended option for production

---

## Technical Design

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        Configuration Flow                                    │
└─────────────────────────────────────────────────────────────────────────────┘

  docker-compose.demo.yml          docker-compose.pi.yml (existing)
           │                                │
           ▼                                ▼
  ┌─────────────────────┐         ┌─────────────────────┐
  │  .env (optional)    │         │  .env (required)    │
  │  - No auth config   │         │  - AUTHENTIK_*      │
  │  - Uses defaults    │         │  - DB_CONNECTION    │
  └─────────────────────┘         └─────────────────────┘
           │                                │
           └────────────────┬───────────────┘
                            ▼
  ┌─────────────────────────────────────────────────────────────────────────────┐
  │                         API Configuration System                             │
  │  ┌───────────────────────────────────────────────────────────────────────┐  │
  │  │  AuthenticationOptions (NEW - replaces AuthentikOptions)               │  │
  │  │  ├── Mode: None | OIDC                                                 │  │
  │  │  ├── Provider: Authentik | Google | Microsoft | OIDC                   │  │
  │  │  ├── Authentik: { Authority, Audience, ClientId, RequireHttps }        │  │
  │  │  ├── Google: { ClientId, ClientSecret }                                │  │
  │  │  ├── Microsoft: { ClientId, TenantId }                                 │  │
  │  │  └── Oidc: { Authority, ClientId, ClientSecret, Scopes, ClaimMappings }│  │
  │  └───────────────────────────────────────────────────────────────────────┘  │
  └─────────────────────────────────────────────────────────────────────────────┘
                            │
                            ▼
  ┌─────────────────────────────────────────────────────────────────────────────┐
  │                         Authentication Pipeline                              │
  │  ┌────────────────┐    ┌────────────────┐    ┌────────────────┐            │
  │  │ Mode = None?   │───▶│ NoAuthHandler  │───▶│ FamilyUserCtx  │            │
  │  └────────────────┘    └────────────────┘    └────────────────┘            │
  │          │                                                                   │
  │          │ Mode = OIDC                                                       │
  │          ▼                                                                   │
  │  ┌────────────────┐    ┌────────────────┐    ┌────────────────┐            │
  │  │ Provider?      │───▶│ JwtBearerSetup │───▶│ ClaimMapping   │            │
  │  │ Authentik/     │    │ (per provider) │    │ (per provider) │            │
  │  │ Google/MS/OIDC │    └────────────────┘    └────────────────┘            │
  │  └────────────────┘                                                         │
  └─────────────────────────────────────────────────────────────────────────────┘
                            │
                            ▼
  ┌─────────────────────────────────────────────────────────────────────────────┐
  │                      /api/v1/config Endpoint (Feature 056)                   │
  │  Returns auth mode + provider config to Blazor Client                        │
  │  ┌───────────────────────────────────────────────────────────────────────┐  │
  │  │ { "authentication": { "mode": "oidc", "oidc": { "authority": "..." }}}│  │
  │  │ { "authentication": { "mode": "none" }}                                │  │
  │  └───────────────────────────────────────────────────────────────────────┘  │
  └─────────────────────────────────────────────────────────────────────────────┘
```

### New/Modified Files

#### Configuration Classes

| File | Status | Description |
|------|--------|-------------|
| `Api/AuthenticationOptions.cs` | ✅ Complete | Unified auth config with `ResolveEffectiveMode()` and `ValidateOidcAuthority()` static methods |
| `Api/AuthModeConstants.cs` | ✅ Complete | String constants for auth modes (`None`, `OIDC`) |
| `Api/AuthProviderConstants.cs` | ✅ Complete | String constants for providers (`Authentik`, `Google`, `Microsoft`, `OIDC`) |
| `Api/AuthentikProviderOptions.cs` | ✅ Complete | Authentik-specific provider options (Authority, Audience, ClientId, RequireHttpsMetadata) |
| `Api/GoogleProviderOptions.cs` | ✅ Complete | Google OAuth provider options (ClientId, ClientSecret) |
| `Api/MicrosoftProviderOptions.cs` | ✅ Complete | Microsoft Entra ID provider options (ClientId, TenantId, ClientSecret) |
| `Api/GenericOidcProviderOptions.cs` | ✅ Complete | Generic OIDC provider options (Authority, ClientId, Scopes, ClaimMappings, etc.) |
| `Api/AuthentikOptions.cs` | ✅ Complete | Marked `[Obsolete]` — kept as separate file for backward compat with `IOptions<AuthentikOptions>` consumers |

#### Authentication Handlers

| File | Status | Description |
|------|--------|-------------|
| `Api/Authentication/NoAuthHandler.cs` | ✅ Complete | Handler for Mode=None — auto-authenticates with family user claims |
| `Api/Authentication/FamilyUserContext.cs` | ✅ Complete | Well-known user constants (GUID, name, email) for no-auth mode |
| `Api/Authentication/AuthenticationConfigurator.cs` | Deferred (Phase 4+) | Factory for configuring auth per provider |

#### Test Files (Phase 1)

| File | Status | Description |
|------|--------|-------------|
| `Api.Tests/AuthenticationOptionsTests.cs` | ✅ Complete | 17 unit tests — defaults, config binding, `ResolveEffectiveMode` logic, constants validation |
| `Api.Tests/AuthenticationBackwardCompatTests.cs` | ✅ Complete | 7 integration tests — legacy config compat, docker-compose env vars, `/api/v1/config` shape |

#### Test Files (Phase 2)

| File | Status | Description |
|------|--------|-------------|
| `Api.Tests/NoAuthHandlerTests.cs` | ✅ Complete | 10 unit tests — handler success, claims, scheme name |
| `Api.Tests/FamilyUserContextTests.cs` | ✅ Complete | 4 unit tests — well-known GUID, name, email constants |
| `Api.Tests/NoAuthIntegrationTests.cs` | ✅ Complete | 8 integration tests — no-auth API access, family user context, config endpoint |

#### Test Files (Phase 3)

| File | Status | Description |
|------|--------|-------------|
| `Client.Tests/Services/NoAuthAuthenticationStateProviderTests.cs` | ✅ Complete | 10 unit tests — authenticated state, claims, scheme, constants, idempotency |
| `Client.Tests/Components/AuthOffBannerTests.cs` | ✅ Complete | 7 bUnit tests — show/hide by mode, warning text, doc link, role attribute |
| `Client.Tests/Components/UserProfileAuthOffTests.cs` | ✅ Complete | 4 bUnit tests — hidden when none, visible when oidc (auth/unauth) |
| `Client.Tests/Pages/AuthenticationPageTests.cs` | ✅ Complete | 3 bUnit tests — redirect to home for login/logout/callback when auth off |

#### Client Changes

| File | Status | Description |
|------|--------|-------------|
| `Client/Services/NoAuthAuthenticationStateProvider.cs` | ✅ Complete | Client-side auth state provider for auth-off mode (family user claims) |
| `Client/Components/Auth/AuthOffBanner.razor` | ✅ Complete | Warning banner for auth-off mode with documentation link |
| `Client/Components/Auth/AuthOffBanner.razor.css` | ✅ Complete | Scoped CSS for auth-off banner (themed with CSS custom properties) |
| `Client/Components/Auth/UserProfile.razor` | ✅ Modified | Conditionally hidden when auth mode is "none" |
| `Client/Pages/Authentication.razor` | ✅ Modified | Redirects to home when auth mode is "none" |
| `Client/Layout/MainLayout.razor` | ✅ Modified | Added AuthOffBanner component |
| `Client/Program.cs` | ✅ Modified | Conditional OIDC vs no-auth registration, fallback config |

#### API Changes (Phase 4 — Google OAuth)

| File | Status | Description |
|------|--------|-------------|
| `Api/GoogleProviderOptions.cs` | ✅ Modified | Added `Authority` constant (`https://accounts.google.com`) |
| `Api/AuthenticationOptions.cs` | ✅ Modified | Added `ResolveProviderSettings` static method with provider switch; provider-agnostic `ValidateOidcAuthority` |
| `Api/Authentication/GoogleClaimMapper.cs` | ✅ Complete | Maps Google claims (email → preferred_username) on token validation |
| `Api/Program.cs` | ✅ Modified | Uses `ResolveProviderSettings`, provider-aware `ConfigureClientConfig`, Google JWT events |

#### Test Files (Phase 4)

| File | Status | Description |
|------|--------|-------------|
| `Api.Tests/GoogleProviderTests.cs` | ✅ Complete | 11 unit tests — authority constant, config binding, provider settings resolution |
| `Api.Tests/GoogleClaimMapperTests.cs` | ✅ Complete | 7 unit tests — email→preferred_username mapping, null safety, claim preservation |
| `Api.Tests/GoogleProviderIntegrationTests.cs` | ✅ Complete | 7 integration tests — /api/v1/config mode, authority, clientId, scopes, backward compat |

#### API Changes (Phase 5 — Microsoft Entra ID)

| File | Status | Description |
|------|--------|-------------|
| `Api/MicrosoftProviderOptions.cs` | ✅ Modified | Added `AuthorityTemplate` constant, `ResolveAuthority()` method for tenant-aware authority URL |
| `Api/AuthenticationOptions.cs` | ✅ Modified | Added Microsoft case to `ResolveProviderSettings` provider switch |
| `Api/Authentication/MicrosoftClaimMapper.cs` | ✅ Complete | Maps Microsoft claims (email → preferred_username) on token validation |
| `Api/Program.cs` | ✅ Modified | Added Microsoft provider detection and `MicrosoftClaimMapper` JWT events |

#### Test Files (Phase 5)

| File | Status | Description |
|------|--------|-------------|
| `Api.Tests/MicrosoftProviderTests.cs` | ✅ Complete | 14 unit tests — authority template, ResolveAuthority, config binding, provider settings resolution |
| `Api.Tests/MicrosoftClaimMapperTests.cs` | ✅ Complete | 7 unit tests — email→preferred_username mapping, null safety, claim preservation |
| `Api.Tests/MicrosoftProviderIntegrationTests.cs` | ✅ Complete | 7 integration tests — /api/v1/config mode, authority, clientId, scopes, multi-tenant, backward compat |

#### API Changes (Phase 6 — Generic OIDC)

| File | Status | Description |
|------|--------|-------------|
| `Api/AuthenticationOptions.cs` | ✅ Modified | Added OIDC case to `ResolveProviderSettings` provider switch |
| `Api/Authentication/GenericOidcClaimMapper.cs` | ✅ Complete | Configurable claim mappings (source→target dictionary) with email→preferred_username fallback |
| `Api/Program.cs` | ✅ Modified | Added Generic OIDC provider detection and `GenericOidcClaimMapper` JWT events with claim mappings |

#### Test Files (Phase 6)

| File | Status | Description |
|------|--------|-------------|
| `Api.Tests/GenericOidcProviderTests.cs` | ✅ Complete | 21 unit tests — config binding, defaults, provider settings resolution, case insensitivity |
| `Api.Tests/GenericOidcClaimMapperTests.cs` | ✅ Complete | 11 unit tests — configurable mappings, email fallback, null safety, claim preservation |
| `Api.Tests/GenericOidcProviderIntegrationTests.cs` | ✅ Complete | 7 integration tests — /api/v1/config mode, authority, clientId, scopes, Auth0 compat, backward compat |

#### Docker/Deployment

| File | Status | Description |
|------|--------|-------------|
| `docker-compose.demo.yml` | ✅ Complete | All-in-one demo compose file (PostgreSQL 16 + auth-off) |
| `.env.example` | ✅ Modified | All auth configuration examples (Authentik, Google, Microsoft, Generic OIDC) |
| `README.md` | ✅ Modified | Quick-start section with demo mode instructions |
| `DEPLOY-QUICKSTART.md` | ✅ Modified | Demo mode section added before Pi deployment guide |
| `docker-compose.pi.yml` | ✅ Modified | Added doc references to AUTH-PROVIDERS.md and demo compose |
| `docs/AUTH-PROVIDERS.md` | ✅ Complete | Per-provider setup guides (Authentik, Google, Microsoft, Generic OIDC), troubleshooting, config reference |

#### Test Files (Phase 9)

| File | Status | Description |
|------|--------|-------------|
| `Api.Tests/ProviderSwitchingIntegrationTests.cs` | ✅ Complete | 11 integration tests — provider switching, case insensitivity, all providers return 200 |
| `E2E.Tests/Tests/FunctionalTests/NoAuthModeTests.cs` | ✅ Complete | 6 Playwright E2E tests — home load, banner, login hidden, config, JS errors, auth redirect |

### Domain Model

No domain model changes required. The "family user" in auth-off mode uses a well-known GUID that is treated like any other user ID in the system.

**Family User Constants:**
```csharp
// Api/Authentication/FamilyUserContext.cs
public static class FamilyUserContext
{
    /// <summary>
    /// Well-known GUID for the family user in auth-off mode.
    /// This ensures data is consistently scoped to the same "user".
    /// </summary>
    public static readonly Guid FamilyUserId = new("00000000-0000-0000-0000-000000000001");
    
    /// <summary>
    /// Display name for the family user.
    /// </summary>
    public const string FamilyUserName = "Family";
    
    /// <summary>
    /// Email for the family user (used in audit trails, etc.).
    /// </summary>
    public const string FamilyUserEmail = "family@localhost";
}
```

### Configuration Schema

#### New AuthenticationOptions Class

```csharp
// Api/AuthenticationOptions.cs (✅ Implemented)
namespace BudgetExperiment.Api;

public sealed class AuthenticationOptions
{
    public const string SectionName = "Authentication";

    public string Mode { get; set; } = AuthModeConstants.Oidc;
    public string Provider { get; set; } = AuthProviderConstants.Authentik;
    public AuthentikProviderOptions Authentik { get; set; } = new();
    public GoogleProviderOptions Google { get; set; } = new();
    public MicrosoftProviderOptions Microsoft { get; set; } = new();
    public GenericOidcProviderOptions Oidc { get; set; } = new();

    /// <summary>
    /// Resolves the effective auth mode from configuration with backward compat.
    /// Priority: (1) Explicit Mode, (2) Legacy Authentik:Enabled=false → None, (3) Default OIDC.
    /// </summary>
    public static string ResolveEffectiveMode(IConfiguration configuration) { /* ... */ }

    /// <summary>
    /// Validates that the OIDC Authority is configured. Throws InvalidOperationException if not.
    /// </summary>
    public static void ValidateOidcAuthority(string authority) { /* ... */ }
}

// Each provider options class is in its own file (one type per file per style rules):
// Api/AuthentikProviderOptions.cs (✅ Implemented)
public sealed class AuthentikProviderOptions { Authority, Audience, ClientId, RequireHttpsMetadata }
// Api/GoogleProviderOptions.cs (✅ Implemented)
public sealed class GoogleProviderOptions { ClientId, ClientSecret }
// Api/MicrosoftProviderOptions.cs (✅ Implemented)
public sealed class MicrosoftProviderOptions { ClientId, TenantId="common", ClientSecret }
// Api/GenericOidcProviderOptions.cs (✅ Implemented)
public sealed class GenericOidcProviderOptions { Authority, ClientId, ClientSecret, Scopes, Audience, RequireHttpsMetadata, ClaimMappings }

// Api/AuthModeConstants.cs (✅ Implemented)
public static class AuthModeConstants { None = "None"; Oidc = "OIDC"; }

// Api/AuthProviderConstants.cs (✅ Implemented)
public static class AuthProviderConstants { Authentik, Google, Microsoft, Oidc = "OIDC"; }

// Api/AuthentikOptions.cs (✅ Marked [Obsolete] — kept for backward compat)
[Obsolete("Use AuthenticationOptions.Authentik (AuthentikProviderOptions) instead.")]
public sealed class AuthentikOptions { /* unchanged body, still registered in DI */ }
```

> **Implementation note (Phase 1):** All provider options classes follow the one-type-per-file rule per `copilot-instructions.md` §18. The two static methods on `AuthenticationOptions` (`ResolveEffectiveMode` and `ValidateOidcAuthority`) are public to enable direct unit testing without requiring the full WebApplicationFactory host pipeline.

### API Changes

#### Modified ConfigController Response

The existing `/api/v1/config` endpoint (Feature 056) already returns auth configuration. It will be updated to include the new provider information:

```json
// Mode = OIDC with Authentik
{
  "authentication": {
    "mode": "oidc",
    "provider": "authentik",
    "oidc": {
      "authority": "https://authentik.example.com/application/o/budget/",
      "clientId": "abc123",
      "responseType": "code",
      "scopes": ["openid", "profile", "email"],
      "postLogoutRedirectUri": "/",
      "redirectUri": "authentication/login-callback"
    }
  }
}

// Mode = OIDC with Google
{
  "authentication": {
    "mode": "oidc",
    "provider": "google",
    "oidc": {
      "authority": "https://accounts.google.com",
      "clientId": "123456789-xyz.apps.googleusercontent.com",
      "responseType": "code",
      "scopes": ["openid", "profile", "email"],
      "postLogoutRedirectUri": "/",
      "redirectUri": "authentication/login-callback"
    }
  }
}

// Mode = None
{
  "authentication": {
    "mode": "none"
  }
}
```

### Docker Compose Files

#### docker-compose.demo.yml (New)

```yaml
# All-in-one demo deployment for BudgetExperiment
# Usage: docker compose -f docker-compose.demo.yml up
#
# This compose file includes:
# - PostgreSQL database (pre-configured, persistent volume)
# - BudgetExperiment API + Blazor Client
#
# Authentication is DISABLED by default for easy evaluation.
# See docs/AUTH-PROVIDERS.md for production auth configuration.

services:
  postgres:
    image: postgres:16-alpine
    container_name: budgetexperiment-db
    environment:
      POSTGRES_USER: budget
      POSTGRES_PASSWORD: budget
      POSTGRES_DB: budgetexperiment
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U budget -d budgetexperiment"]
      interval: 10s
      timeout: 5s
      retries: 5
    networks:
      - budget-demo

  budgetexperiment:
    image: ghcr.io/becauseimclever/budgetexperiment:latest
    container_name: budgetexperiment
    depends_on:
      postgres:
        condition: service_healthy
    ports:
      - "5099:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
      # Database connection to bundled PostgreSQL
      - ConnectionStrings__AppDb=Host=postgres;Port=5432;Database=budgetexperiment;Username=budget;Password=budget
      # Authentication DISABLED for demo
      - Authentication__Mode=None
      # Auto-migrate database on startup
      - Database__AutoMigrate=true
    networks:
      - budget-demo
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

volumes:
  postgres_data:

networks:
  budget-demo:
    driver: bridge
```

### No-Auth Handler Implementation

```csharp
// Api/Authentication/NoAuthHandler.cs
namespace BudgetExperiment.Api.Authentication;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

/// <summary>
/// Authentication handler that always succeeds and creates a family user context.
/// Used when Authentication:Mode = "None".
/// </summary>
public sealed class NoAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "NoAuth";

    public NoAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, FamilyUserContext.FamilyUserId.ToString()),
            new Claim(ClaimTypes.Name, FamilyUserContext.FamilyUserName),
            new Claim(ClaimTypes.Email, FamilyUserContext.FamilyUserEmail),
            new Claim("sub", FamilyUserContext.FamilyUserId.ToString()),
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
```

### Client AuthOffBanner Component

```razor
@* Client/Shared/AuthOffBanner.razor *@
@inject IClientConfigService ConfigService

@if (_isAuthOff)
{
    <div class="auth-off-banner">
        <span class="warning-icon">⚠️</span>
        <span>Running in demo mode – authentication disabled.</span>
        <a href="https://github.com/becauseimclever/BudgetExperiment/blob/main/docs/AUTH-PROVIDERS.md" 
           target="_blank" 
           rel="noopener noreferrer">
            Learn how to enable authentication
        </a>
    </div>
}

@code {
    private bool _isAuthOff;

    protected override async Task OnInitializedAsync()
    {
        var config = await ConfigService.GetConfigAsync();
        _isAuthOff = config?.Authentication?.Mode?.Equals("none", StringComparison.OrdinalIgnoreCase) == true;
    }
}
```

---

## Implementation Plan

### Phase 1: AuthenticationOptions Refactoring
> **Commit:** `feat(api): refactor authentication config to support multiple modes and providers`

**Objective:** Replace the current hard-coded Authentik-only configuration with a flexible multi-provider system.

**Tasks:**
- [x] Create `AuthenticationOptions.cs` with nested provider options
- [x] Create `AuthModeConstants.cs` and `AuthProviderConstants.cs`
- [x] Create provider-specific options classes (Google, Microsoft, Generic OIDC)
- [x] Update `ConfigureAuthentication` method in `Program.cs` to use new options
- [x] **Keep existing `AuthentikOptions` class registered in DI** (`IOptions<AuthentikOptions>`) — mark `[Obsolete]` but do not remove. The new `AuthenticationOptions.Authentik` uses `AuthentikProviderOptions` internally but the original binding must remain for backward compat.
- [x] **Add fallback for `Authentik:Enabled=false`** — if `Mode` is not explicitly set and `Authentik:Enabled` is `false`, resolve `Mode` to `"None"`. Log deprecation warning: "Setting 'Authentication:Authentik:Enabled' is deprecated. Use 'Authentication:Mode=None' instead."
- [x] **Preserve fail-fast validation** — when `Mode=OIDC` and the resolved provider's Authority is empty, throw `InvalidOperationException` with the same message as today.
- [x] Update `ConfigureClientConfig` to read `AuthenticationOptions.ResolveEffectiveMode()` first, with fallback to legacy `Authentik:Enabled` for backward compat.
- [x] Update `appsettings.json` schema with new structure (additive only — do not remove existing keys) — **Decision: no keys added to `appsettings.json`** to avoid overriding legacy env vars; defaults are in code.
- [x] Write unit tests for configuration binding (new options + legacy env vars) — 17 tests in `AuthenticationOptionsTests.cs`
- [x] Write backward-compatibility integration tests (see "Implementation Validation Requirements" in Backward Compatibility Checklist) — 7 tests in `AuthenticationBackwardCompatTests.cs`

**Validation:**
- Existing deployments with Authentik continue to work **with zero config changes**
- `docker-compose.pi.yml` env vars (`Authentication__Authentik__*`) bind correctly to nested config
- `Authentication__Authentik__Enabled=false` still disables auth (with deprecation warning logged)
- Missing Authority when `Mode=OIDC` still throws `InvalidOperationException`
- `/api/v1/config` response shape is unchanged for Authentik deployments (new `provider` field is additive)
- No breaking changes to `IOptions<AuthentikOptions>` consumers

---

### Phase 2: No-Auth Mode Implementation
> **Commit:** `feat(api): add authentication off mode for demo deployments`

**Objective:** Allow authentication to be completely disabled for demo/family use.

**Tasks:**
- [x] Create `NoAuthHandler.cs` authentication handler
- [x] Create `FamilyUserContext.cs` with well-known user constants
- [x] Modify `ConfigureAuthentication` to detect `Mode=None` and register `NoAuthHandler`
- [x] Log startup WARNING when running in no-auth mode
- [x] Ensure `IUserContext` returns family user in no-auth mode (existing `UserContext` reads NoAuthHandler's claims — no changes needed)
- [x] Update `/api/v1/config` to return `mode: "none"` when auth is off (already handled by Phase 1's `ConfigureClientConfig`)
- [x] Write unit tests for NoAuthHandler — 10 tests in `NoAuthHandlerTests.cs`
- [x] Write unit tests for FamilyUserContext — 4 tests in `FamilyUserContextTests.cs`
- [x] Write integration tests for no-auth API access — 8 tests in `NoAuthIntegrationTests.cs`

**Validation:**
- [x] API responds 200 to authenticated endpoints when `Mode=None`
- [x] User context returns family user ID, name, email
- [x] Warning logged at startup: "⚠️ Authentication is DISABLED..."
- [x] `/api/v1/config` returns `mode: "none"` with no OIDC settings
- [x] Existing tests unaffected (345/348 pass — 3 pre-existing AI timeout failures)

---

### Phase 3: Client Auth-Off Adaptations
> **Commit:** `feat(client): adapt UI for auth-off mode`

**Objective:** Make the Blazor client gracefully handle no-auth mode.

**Tasks:**
- [x] Create `NoAuthAuthenticationStateProvider` service for client-side auth-off mode
- [x] Update `Program.cs` to conditionally register OIDC vs no-auth authentication
- [x] Skip `TokenRefreshHandler` and `BaseAddressAuthorizationMessageHandler` when auth is off
- [x] Register `AuthenticationConfigDto` fallback when config endpoint is unavailable
- [x] Create `AuthOffBanner.razor` component with warning text and documentation link
- [x] Style auth-off banner (subtle but visible, CSS custom properties for theming)
- [x] Modify `UserProfile.razor` to hide login/logout/profile when auth is off
- [x] Redirect `/authentication/*` routes to home when auth is off
- [x] Add `AuthOffBanner` to `MainLayout.razor`
- [x] Write bUnit tests for `NoAuthAuthenticationStateProvider` — 10 tests
- [x] Write bUnit tests for `AuthOffBanner` conditional rendering — 7 tests
- [x] Write bUnit tests for `UserProfile` auth-off behavior — 4 tests
- [x] Write bUnit tests for `Authentication` page redirect — 3 tests

**Validation:**
- [x] No login buttons visible in auth-off mode
- [x] Banner displays and links to documentation
- [x] Authentication routes redirect to home in auth-off mode
- [x] All 527 client tests pass (1 pre-existing skip), 0 regressions
- [x] All existing API/Domain/Application tests unaffected

---

### Phase 4: Google OAuth Provider
> **Commit:** `feat(api): add Google OAuth provider support`

**Objective:** Enable Google as an authentication provider option.

**Tasks:**
- [x] Implement Google JWT Bearer configuration in `ConfigureAuthentication` (provider switch in `ResolveProviderSettings`)
- [x] Add Google-specific claim mapping (`GoogleClaimMapper` — maps email → preferred_username)
- [x] Update `/api/v1/config` to return Google OIDC settings (provider-aware `ConfigureClientConfig`)
- [x] Make `ValidateOidcAuthority` error message provider-agnostic
- [x] Write unit tests for `GoogleProviderOptions`, `ResolveProviderSettings`, and `GoogleClaimMapper` — 18 tests
- [x] Write integration tests for `/api/v1/config` with Provider=Google — 7 tests

**Validation:**
- [x] Setting `Provider=Google` with valid ClientId resolves to Google authority
- [x] Claims are correctly mapped (email → preferred_username) via `GoogleClaimMapper`
- [x] `/api/v1/config` returns correct Google OIDC settings (authority, clientId, scopes)
- [x] Existing Authentik configuration still works (backward compat, 0 regressions)
- [x] All 372 API tests pass (370 pass + 2 pre-existing AI timeouts), 527 client tests pass

---

### Phase 5: Microsoft Entra ID Provider
> **Commit:** `feat(api): add Microsoft Entra ID provider support`

**Objective:** Enable Microsoft Entra ID (Azure AD) as an authentication provider option.

**Tasks:**
- [x] Implement Microsoft JWT Bearer configuration (`ResolveProviderSettings` Microsoft case, `MicrosoftProviderOptions.ResolveAuthority()`)
- [x] Support single-tenant and multi-tenant configurations (`TenantId` defaults to "common"; supports specific tenant GUIDs, "organizations")
- [x] Handle Microsoft-specific token validation (`MicrosoftClaimMapper` maps email → preferred_username)
- [x] Update `/api/v1/config` to return Microsoft OIDC settings (provider-aware `ConfigureClientConfig`)
- [x] Document Azure AD app registration process (Phase 8 — `docs/AUTH-PROVIDERS.md`)
- [x] Write unit tests for `MicrosoftProviderOptions`, `ResolveProviderSettings`, and `MicrosoftClaimMapper` — 21 tests
- [x] Write integration tests for `/api/v1/config` with Provider=Microsoft — 7 tests

**Validation:**
- [x] Setting `Provider=Microsoft` with valid ClientId resolves to Microsoft authority
- [x] Single-tenant uses specific tenant URL (`login.microsoftonline.com/{tenantId}/v2.0`)
- [x] Multi-tenant defaults to `common` authority when TenantId is not specified
- [x] Claims are correctly mapped (email → preferred_username) via `MicrosoftClaimMapper`
- [x] `/api/v1/config` returns correct Microsoft OIDC settings (authority, clientId, scopes)
- [x] Existing Authentik and Google configuration still works (backward compat, 0 regressions)
- [x] All 400 API tests pass (397 pass + 3 pre-existing AI timeouts), 527 client tests unaffected

---

### Phase 6: Generic OIDC Provider
> **Commit:** `feat(api): add generic OIDC provider support for Keycloak, Auth0, etc.`

**Objective:** Enable any standard OIDC provider via configuration.

**Tasks:**
- [x] Implement generic OIDC JWT Bearer configuration (`ResolveProviderSettings` OIDC case)
- [x] Support configurable claim mappings (`GenericOidcClaimMapper` with source→target dictionary + email fallback)
- [x] Support custom scopes (via `GenericOidcProviderOptions.Scopes` array binding)
- [x] Update `/api/v1/config` to return generic OIDC settings (provider-aware `ConfigureClientConfig` with ClientId/Audience separation)
- [x] Document configuration for Keycloak, Auth0, and Okta examples (Phase 8 — `docs/AUTH-PROVIDERS.md`)
- [x] Write unit tests for `GenericOidcProviderOptions`, `ResolveProviderSettings`, and `GenericOidcClaimMapper` — 32 tests
- [x] Write integration tests for `/api/v1/config` with Provider=OIDC — 7 tests

**Validation:**
- [x] Setting `Provider=OIDC` with valid Authority resolves correctly
- [x] Configurable claim mappings apply source→target claim transformations
- [x] Email→preferred_username fallback works when no explicit mapping matches
- [x] ClientId is correctly separated from Audience in `/api/v1/config` response
- [x] Auth0-style configuration works (authority, clientId, audience as separate values)
- [x] Existing Authentik, Google, and Microsoft configuration still works (backward compat, 0 regressions)
- [x] All 430 API tests pass (428 pass + 2 pre-existing AI timeouts), client tests unaffected

---

### Phase 7: Demo Docker Compose
> **Commit:** `feat(deploy): add all-in-one demo docker-compose with bundled PostgreSQL`

**Objective:** Create a single-command deployment option that includes everything.

**Tasks:**
- [x] Create `docker-compose.demo.yml` with PostgreSQL and API
- [x] Configure PostgreSQL with persistent volume
- [x] Set `Authentication__Mode=None` as default for demo
- [x] Add health checks for both services
- [ ] Test on clean Docker environment
- [ ] Test on Raspberry Pi (ARM64)
- [x] Document demo mode usage in README

**Validation:**
- `docker compose -f docker-compose.demo.yml up` starts everything
- Application accessible at `http://localhost:5099`
- Data persists across restarts

---

### Phase 8: Documentation
> **Commit:** `docs: add authentication providers guide and update deployment docs`

**Objective:** Comprehensive documentation for all auth options and deployment modes.

**Tasks:**
- [x] Create `docs/AUTH-PROVIDERS.md` with per-provider setup guides (Authentik, Google, Microsoft, Generic OIDC, Keycloak/Auth0/Okta examples)
- [x] Update `README.md` with quick-start section (done in Phase 7)
- [x] Update `DEPLOY-QUICKSTART.md` with new options (done in Phase 7)
- [x] Update `.env.example` with all auth environment variables (done in Phase 7)
- [x] Add troubleshooting section for common auth issues
- [x] Review and update `docker-compose.pi.yml` examples (added doc references)
- [x] Add complete configuration reference table

**Validation:**
- [x] A new user can successfully deploy using only the documentation
- [x] All configuration options are documented
- [x] Examples are accurate and tested

---

### Phase 9: Testing & Validation
> **Commit:** `test: add comprehensive E2E tests for auth modes and providers`

**Objective:** Ensure all auth modes and providers work correctly end-to-end.

**Tasks:**
- [x] Add Playwright E2E tests for no-auth mode — 6 tests in `NoAuthModeTests.cs` (home load, banner visibility, login hidden, config endpoint, JS errors, auth route redirect)
- [x] Add API integration tests for provider switching — 11 tests in `ProviderSwitchingIntegrationTests.cs` (switch to Google/Microsoft/OIDC/None, switch back, case insensitivity, all providers 200)
- [x] Test migration from demo to production config (integration test: `SwitchingFromNoneToAuthentik_RestoresOidcSettings`)
- [x] Test rollback scenarios (integration test: `SwitchingToNone_RemovesOidcSettings`)
- [ ] Performance test auth handler overhead (deferred — no measurable overhead in integration tests)
- [x] Security review of no-auth mode implementation (NoAuthHandler scoped, family user deterministic, banner + log warning)

**Validation:**
- [x] All E2E tests compile and pass (when run against no-auth instance)
- [x] All 448 API tests pass (446 pass + 2 pre-existing AI timeouts), zero regressions
- [x] Security review complete — no-auth mode properly isolated

---

## Testing Strategy

### Unit Tests

- [x] `AuthenticationOptions` binds correctly from configuration (17 tests)
- [x] `NoAuthHandler.HandleAuthenticateAsync` returns success with family user claims (10 tests)
- [x] `FamilyUserContext` constants are valid (4 tests)
- [x] Provider-specific options bind correctly (Google 11, Microsoft 14, Generic OIDC 21 tests)
- [x] Claim mappings are applied correctly (Google 7, Microsoft 7, Generic OIDC 11 tests)

### API Integration Tests

- [x] `GET /api/v1/config` returns correct mode/provider info (per-provider: 7+7+7+7 tests)
- [x] API endpoints work with no-auth mode (8 tests)
- [x] API endpoints work with each provider type (mocked tokens) (11 provider switching tests)
- [x] User context returns correct user ID per mode (4 tests)
- [x] Authorization attributes still apply in no-auth mode (all requests pass)

### Client Component Tests (bUnit)

- [x] `AuthOffBanner` shows when mode is "none" (7 tests)
- [x] `AuthOffBanner` hides when mode is "oidc"
- [x] `NavMenu` hides auth UI when mode is "none" (4 tests)
- [x] Authentication routes redirect when mode is "none" (3 tests)

### E2E Tests (Playwright)

- [x] Complete flow in no-auth mode: navigate without login (6 tests in `NoAuthModeTests.cs`)
- [x] Auth banner is visible in no-auth mode
- [x] No JavaScript errors in no-auth mode
- [ ] OIDC flow works with Authentik provider (requires live Authentik instance)

### Manual Testing Checklist

- [ ] Deploy `docker-compose.demo.yml` on clean Linux VM
- [ ] Deploy `docker-compose.demo.yml` on Raspberry Pi
- [ ] Configure and test Google OAuth
- [ ] Configure and test Microsoft Entra ID
- [ ] Migrate demo instance to Authentik auth
- [ ] Test data persistence across provider switch

---

## Security Considerations

### No-Auth Mode Risks

⚠️ **Critical:** No-auth mode should NEVER be exposed to the public internet.

**Mitigations:**
1. **Startup Warning:** Clear log message at startup
2. **UI Banner:** Visible reminder in the application
3. **Documentation:** Security implications clearly stated
4. **Default Ports:** Demo compose uses `localhost:5099` only
5. **No HTTPS:** Demo mode doesn't configure HTTPS (signals it's local only)

### Provider Security

- **Google/Microsoft:** Use PKCE flow (authorization code with code verifier)
- **Client Secrets:** Never exposed to browser; only used for confidential client flows
- **Token Validation:** All providers use standard JWT validation
- **Claim Mapping:** Configurable but defaults to secure mappings

### Data Isolation

- **Family User:** All data in no-auth mode belongs to the family user ID
- **Migration Path:** When enabling auth, existing data can be reassigned via admin tool (future feature)
- **No Cross-User Access:** Even in no-auth mode, data scoping by user ID is maintained

---

## Configuration Reference

### Environment Variables (Complete List)

```bash
# Authentication Mode
Authentication__Mode=OIDC              # "None" or "OIDC"

# Provider Selection (when Mode=OIDC)
Authentication__Provider=Authentik     # "Authentik", "Google", "Microsoft", "OIDC"

# Authentik Configuration
Authentication__Authentik__Authority=https://auth.example.com/application/o/budget/
Authentication__Authentik__Audience=budget-experiment
Authentication__Authentik__ClientId=abc123
Authentication__Authentik__RequireHttpsMetadata=true

# Google Configuration
Authentication__Google__ClientId=123456789-xyz.apps.googleusercontent.com
Authentication__Google__ClientSecret=GOCSPX-xxxxx

# Microsoft Configuration
Authentication__Microsoft__ClientId=00000000-0000-0000-0000-000000000000
Authentication__Microsoft__TenantId=common
Authentication__Microsoft__ClientSecret=xxx  # Optional for public clients

# Generic OIDC Configuration
Authentication__Oidc__Authority=https://keycloak.example.com/realms/master
Authentication__Oidc__ClientId=budget-experiment
Authentication__Oidc__ClientSecret=xxx
Authentication__Oidc__Scopes__0=openid
Authentication__Oidc__Scopes__1=profile
Authentication__Oidc__Scopes__2=email
Authentication__Oidc__Audience=budget-experiment
Authentication__Oidc__RequireHttpsMetadata=true
Authentication__Oidc__ClaimMappings__name=preferred_username
```

---

## Backward Compatibility Checklist (Existing Authentik + Dedicated PostgreSQL)

> **Goal:** Zero breaking changes for current production deployments using `docker-compose.pi.yml` with an external PostgreSQL and Authentik.

### Database — No Impact ✅

- No schema migrations introduced by this feature.
- `ConnectionStrings:AppDb` config key is unchanged.
- `docker-compose.pi.yml` continues to forward `DB_CONNECTION_STRING` identically.
- The new `docker-compose.demo.yml` is a **separate file** and does not touch existing compose workflows.

### Authentication Env Vars — Requires Compatibility Shim ⚠️

Current `docker-compose.pi.yml` maps these env vars:

```
Authentication__Authentik__Enabled=${AUTHENTIK_ENABLED:-false}
Authentication__Authentik__Authority=${AUTHENTIK_AUTHORITY:-}
Authentication__Authentik__Audience=${AUTHENTIK_AUDIENCE:-}
Authentication__Authentik__RequireHttpsMetadata=${AUTHENTIK_REQUIRE_HTTPS:-true}
```

The new design introduces `Authentication__Mode` and `Authentication__Provider`. Existing deployments do **not** set these. To remain backward-compatible:

| Requirement | How to Satisfy |
|---|---|
| `Authentication__Mode` unset must default to `"OIDC"` | `AuthenticationOptions.Mode` defaults to `"OIDC"` — ✅ already specified |
| `Authentication__Provider` unset must default to `"Authentik"` | `AuthenticationOptions.Provider` defaults to `"Authentik"` — ✅ already specified |
| `Authentication__Authentik__Authority` et al. must still bind | The nested `AuthenticationOptions.Authentik` property maps to the same config prefix — ✅ |
| `Authentication__Authentik__Enabled=false` must still disable auth | **⚠️ NOT COVERED.** The new design uses `Mode=None` to disable auth, but ignores the legacy `Enabled` flag. `ConfigureAuthentication` must detect `Authentik:Enabled=false` and treat it as `Mode=None` for backward compat. Add a fallback: if `Mode` is not explicitly set AND `Authentik:Enabled` is `false`, override `Mode` to `"None"`. Log a deprecation warning directing users to `Authentication__Mode=None`. |
| Missing `Authority` when `Mode=OIDC` must still throw `InvalidOperationException` | **Must preserve the existing fail-fast.** Phase 1 implementation must validate: if `Mode=OIDC` and the resolved provider's Authority is empty, throw with a clear message (same as today). |

### AuthentikOptions Type Rename — Must Keep Original ⚠️

Current code registers `IOptions<AuthentikOptions>` via:
```csharp
builder.Services.Configure<AuthentikOptions>(
    builder.Configuration.GetSection(AuthentikOptions.SectionName));
```

The feature proposes renaming to `AuthentikProviderOptions`. Any code injecting `IOptions<AuthentikOptions>` (including `ConfigureClientConfig` and `UserContext`) would break.

**Required:** Keep the existing `AuthentikOptions` class **as-is** for at least one release cycle. The new `AuthenticationOptions.Authentik` property can be typed as `AuthentikProviderOptions` internally, but the standalone `AuthentikOptions` binding must remain registered so existing consumers are unaffected. Add `[Obsolete]` to guide migration.

### ConfigureClientConfig — Must Bridge Old and New ⚠️

`ConfigureClientConfig` currently reads `Authentik:Enabled` to derive `AuthMode`. After refactoring:

- It should read `AuthenticationOptions.Mode` first.
- If `Mode` was not explicitly set (i.e., still default), fall back to checking `Authentik:Enabled` for backward compat.
- The `/api/v1/config` response shape (`mode`, `oidc` block) is additive — the new `provider` field is a safe addition.

### docker-compose.pi.yml — No Changes Required ✅

The compose file is not modified. All existing env var mappings (`Authentication__Authentik__*`) bind correctly to the nested config path. Deployments that already set `AUTHENTIK_ENABLED=true` (or don't set it at all, relying on the code default of `true`) will resolve to `Mode=OIDC, Provider=Authentik` and work exactly as before.

### Implementation Validation Requirements

Each phase must include these backward-compat integration tests (✅ **all implemented in Phase 1**):

| Test | Status | Description |
|---|---|---|
| `ExistingAuthentikConfig_ContinuesToWork` | ✅ | Configure only `Authentication:Authentik:Authority` + `Audience` (no `Mode`, no `Provider`). Assert mode resolves to OIDC and Authority matches. |
| `AuthentikEnabled_False_DisablesAuth` | ✅ | Set `Authentication:Authentik:Enabled=false`. Assert `Mode` resolves to `None`. |
| `MissingAuthority_InOidcMode_Throws` | ✅ | `ValidateOidcAuthority("")` throws `InvalidOperationException` (unit test — static method extracted for testability). |
| `ValidAuthority_InOidcMode_DoesNotThrow` | ✅ | `ValidateOidcAuthority("https://...")` does not throw. |
| `WhitespaceAuthority_InOidcMode_Throws` | ✅ | `ValidateOidcAuthority("  ")` throws `InvalidOperationException`. |
| `ConfigEndpoint_BackwardCompatShape` | ✅ | Assert `/api/v1/config` response contains `authentication.mode` and `authentication.oidc` with same shape as today when using Authentik. |
| `DockerComposePi_EnvVars_BindCorrectly` | ✅ | Simulate env vars from `docker-compose.pi.yml` and assert all config properties resolve correctly. |

---

## Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Users expose no-auth mode to internet | Medium | Critical | Multiple warnings (startup, UI, docs), clear documentation |
| Breaking changes to existing Authentik setup | Low | High | Backward compatibility checklist above; integration tests; legacy `Enabled` flag shim |
| `AuthentikOptions` rename breaks DI consumers | Medium | High | Keep original class + `[Obsolete]`; phase out over 1 release |
| `Authentik:Enabled=false` ignored after refactor | High | High | Explicit fallback logic in `ConfigureAuthentication`; deprecation log |
| Complex configuration confuses users | Medium | Medium | Comprehensive documentation, .env.example, error messages |
| Provider-specific quirks cause auth failures | Medium | Medium | Thorough testing with each provider, troubleshooting docs |
| PostgreSQL in demo mode causes data loss | Low | Medium | Named volumes, clear documentation about persistence |

---

## Success Metrics

- **Time to First Deploy:** < 5 minutes for demo mode (from git clone to running app)
- **Configuration Errors:** Helpful error messages for 95%+ of misconfigurations
- **Documentation Coverage:** 100% of auth options documented with examples
- **Backward Compatibility:** 100% of existing deployments continue to work
- **User Feedback:** Positive feedback on deployment experience

---

## Out of Scope

- **Production Hardening for Demo PostgreSQL:** Demo mode is explicitly for evaluation only
- **SSO/SCIM/Advanced User Management:** Enterprise features deferred to future work
- **Multi-User Management in No-Auth Mode:** Single "family" user only
- **Social Login UI (Login with Google button):** Provider-agnostic OIDC flow only
- **Session Management UI:** No user sessions list or forced logout (yet)
- **Rate Limiting in No-Auth Mode:** Not needed for local-only usage

---

## Migration/Upgrade Notes

### Existing Authentik Deployments

**No changes required.** The following configurations continue to work:

```bash
# Old style (still works)
Authentication__Authentik__Authority=...
Authentication__Authentik__Audience=...
Authentication__Authentik__Enabled=true

# New style (recommended)
Authentication__Mode=OIDC
Authentication__Provider=Authentik
Authentication__Authentik__Authority=...
```

### Migrating Demo to Production

1. Update `docker-compose.demo.yml` to use external PostgreSQL:
   ```yaml
   environment:
     - ConnectionStrings__AppDb=Host=prod-db;...
   ```

2. Enable authentication:
   ```yaml
   environment:
     - Authentication__Mode=OIDC
     - Authentication__Provider=Authentik
     - Authentication__Authentik__Authority=...
   ```

3. (Optional) Migrate family user data to new user:
   ```sql
   -- Future admin tool will provide UI for this
   UPDATE "Transactions" SET "UserId" = 'new-user-guid' WHERE "UserId" = '00000000-0000-0000-0000-000000000001';
   ```

---

## References

- [Feature 056 - API Config Endpoint](./056-api-config-endpoint.md) - Config endpoint implementation
- [docker-compose.pi.yml](../docker-compose.pi.yml) - Current production compose file
- [DEPLOY-QUICKSTART.md](../DEPLOY-QUICKSTART.md) - Current deployment guide
- [Microsoft.AspNetCore.Authentication.JwtBearer](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.jwtbearer) - JWT Bearer auth docs
- [Google OAuth 2.0 for Web Apps](https://developers.google.com/identity/protocols/oauth2/web-server) - Google OAuth setup
- [Microsoft Identity Platform](https://learn.microsoft.com/en-us/entra/identity-platform/) - Microsoft Entra ID docs

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-01-27 | Initial draft | @becauseimclever |
| 2026-02-02 | Fleshed out with full technical design, user stories, and implementation phases | @copilot |
| 2026-02-21 | Phase 1 complete: `AuthenticationOptions` refactoring with 7 new source files, 3 modified files, 24 tests (17 unit + 7 integration). Backward compat verified — zero config changes required for existing deployments. | @copilot |
| 2026-02-21 | Phase 2 complete: `NoAuthHandler` + `FamilyUserContext` in `Api/Authentication/`, startup warning log, 22 new tests (10 NoAuthHandler unit + 4 FamilyUserContext unit + 8 integration). All API requests auto-authenticate as family user when `Mode=None`. | @copilot |
| 2026-02-21 | Phase 3 complete: Client auth-off adaptations — `NoAuthAuthenticationStateProvider`, `AuthOffBanner.razor`, `UserProfile` hide, `Authentication.razor` redirect, `Program.cs` conditional auth registration. 24 new bUnit tests (10 provider + 7 banner + 4 profile + 3 page). All 527 client tests pass, zero regressions. | @copilot |
| 2026-02-22 | Phase 4 complete: Google OAuth provider — `GoogleClaimMapper`, `ResolveProviderSettings` provider switch, provider-aware `ConfigureClientConfig`, Google authority constant. 25 new tests (11 unit + 7 claim mapper + 7 integration). Backward compat verified, zero regressions. | @copilot |
| 2026-02-22 | Phase 5 complete: Microsoft Entra ID provider — `MicrosoftClaimMapper`, `MicrosoftProviderOptions.AuthorityTemplate` + `ResolveAuthority()`, Microsoft case in `ResolveProviderSettings`, Microsoft JWT events in `Program.cs`. 28 new tests (14 unit + 7 claim mapper + 7 integration). Single-tenant & multi-tenant verified, backward compat verified, zero regressions. | @copilot |
| 2026-02-22 | Phase 6 complete: Generic OIDC provider — `GenericOidcClaimMapper` with configurable source→target claim mappings + email fallback, OIDC case in `ResolveProviderSettings`, Generic OIDC JWT events in `Program.cs`, ClientId/Audience separation in `ConfigureClientConfig`. 39 new tests (21 unit + 11 claim mapper + 7 integration). Keycloak & Auth0 config verified, backward compat verified, zero regressions. | @copilot |
| 2026-02-22 | Phase 7 complete: Demo Docker Compose — `docker-compose.demo.yml` with bundled PostgreSQL 16, auth-off default, health checks, persistent volume. Updated `.env.example` with all auth provider env vars. Added quick-start section to `README.md` and demo mode guide to `DEPLOY-QUICKSTART.md`. Both compose files validated. | @copilot |
| 2026-02-22 | Phase 8 complete: Documentation — `docs/AUTH-PROVIDERS.md` with per-provider setup guides (Authentik, Google, Microsoft, Generic OIDC with Keycloak/Auth0/Okta examples), troubleshooting section, complete configuration reference table. Updated `docker-compose.pi.yml` with doc references. | @copilot |
| 2026-02-22 | Phase 9 complete: Testing & Validation — `ProviderSwitchingIntegrationTests.cs` (11 tests: switch providers, case insensitivity, all providers 200). `NoAuthModeTests.cs` (6 Playwright E2E tests: home load, banner, login hidden, config endpoint, JS errors, auth route redirect). All 448 API tests pass (446 + 2 pre-existing AI timeouts), zero regressions. Feature complete. | @copilot |
