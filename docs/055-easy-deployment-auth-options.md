# 055: Easier Deployment - Optional Postgres, Auth Off, and Flexible Auth Providers
> **Status:** 🗒️ Planning  
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
| `Api/AuthenticationOptions.cs` | New | Unified auth config (replaces direct Authentik binding) |
| `Api/AuthModeConstants.cs` | New | String constants for auth modes |
| `Api/AuthProviderConstants.cs` | New | String constants for providers |
| `Api/AuthentikOptions.cs` | Modified | Becomes a nested class within AuthenticationOptions |

#### Authentication Handlers

| File | Status | Description |
|------|--------|-------------|
| `Api/Authentication/NoAuthHandler.cs` | New | Handler for Mode=None |
| `Api/Authentication/FamilyUserContext.cs` | New | Default user context for no-auth mode |
| `Api/Authentication/AuthenticationConfigurator.cs` | New | Factory for configuring auth per provider |

#### Client Changes

| File | Status | Description |
|------|--------|-------------|
| `Client/Services/AuthStateService.cs` | Modified | Handle auth mode from /api/v1/config |
| `Client/Shared/NavMenu.razor` | Modified | Conditionally show/hide auth UI |
| `Client/Shared/AuthOffBanner.razor` | New | Warning banner for auth-off mode |

#### Docker/Deployment

| File | Status | Description |
|------|--------|-------------|
| `docker-compose.demo.yml` | New | All-in-one demo compose file |
| `.env.example` | Modified | Add all auth configuration examples |
| `README.md` | Modified | Add quick-start section |
| `docs/AUTH-PROVIDERS.md` | New | Per-provider setup documentation |

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
// Api/AuthenticationOptions.cs
namespace BudgetExperiment.Api;

/// <summary>
/// Root authentication configuration options.
/// </summary>
public sealed class AuthenticationOptions
{
    public const string SectionName = "Authentication";

    /// <summary>
    /// Authentication mode: "None" or "OIDC".
    /// When "None", all authentication is disabled and the family user context is used.
    /// Default: "OIDC" (authentication required).
    /// </summary>
    public string Mode { get; set; } = AuthModeConstants.Oidc;

    /// <summary>
    /// OIDC provider to use when Mode = "OIDC".
    /// Options: "Authentik", "Google", "Microsoft", "OIDC" (generic).
    /// Default: "Authentik".
    /// </summary>
    public string Provider { get; set; } = AuthProviderConstants.Authentik;

    /// <summary>
    /// Authentik-specific configuration.
    /// </summary>
    public AuthentikProviderOptions Authentik { get; set; } = new();

    /// <summary>
    /// Google OAuth configuration.
    /// </summary>
    public GoogleProviderOptions Google { get; set; } = new();

    /// <summary>
    /// Microsoft Entra ID configuration.
    /// </summary>
    public MicrosoftProviderOptions Microsoft { get; set; } = new();

    /// <summary>
    /// Generic OIDC provider configuration.
    /// </summary>
    public GenericOidcProviderOptions Oidc { get; set; } = new();
}

/// <summary>
/// Authentik-specific provider options.
/// </summary>
public sealed class AuthentikProviderOptions
{
    public string Authority { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public bool RequireHttpsMetadata { get; set; } = true;
}

/// <summary>
/// Google OAuth provider options.
/// </summary>
public sealed class GoogleProviderOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}

/// <summary>
/// Microsoft Entra ID provider options.
/// </summary>
public sealed class MicrosoftProviderOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string TenantId { get; set; } = "common"; // "common", "organizations", or specific tenant
    public string ClientSecret { get; set; } = string.Empty; // Optional for public clients
}

/// <summary>
/// Generic OIDC provider options for Keycloak, Auth0, Okta, etc.
/// </summary>
public sealed class GenericOidcProviderOptions
{
    public string Authority { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string[] Scopes { get; set; } = ["openid", "profile", "email"];
    public string Audience { get; set; } = string.Empty;
    public bool RequireHttpsMetadata { get; set; } = true;
    
    /// <summary>
    /// Custom claim mappings for non-standard providers.
    /// Key: Standard claim name (e.g., "sub"), Value: Provider's claim name.
    /// </summary>
    public Dictionary<string, string> ClaimMappings { get; set; } = new();
}

/// <summary>
/// Authentication mode constants.
/// </summary>
public static class AuthModeConstants
{
    public const string None = "None";
    public const string Oidc = "OIDC";
}

/// <summary>
/// Authentication provider constants.
/// </summary>
public static class AuthProviderConstants
{
    public const string Authentik = "Authentik";
    public const string Google = "Google";
    public const string Microsoft = "Microsoft";
    public const string Oidc = "OIDC";
}
```

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
- [ ] Create `AuthenticationOptions.cs` with nested provider options
- [ ] Create `AuthModeConstants.cs` and `AuthProviderConstants.cs`
- [ ] Create provider-specific options classes (Google, Microsoft, Generic OIDC)
- [ ] Update `ConfigureAuthentication` method in `Program.cs` to use new options
- [ ] Maintain backward compatibility with existing Authentik configuration
- [ ] Update `appsettings.json` schema with new structure
- [ ] Write unit tests for configuration binding
- [ ] Write integration tests ensuring existing Authentik flow works

**Validation:**
- Existing deployments with Authentik continue to work
- Configuration binds correctly from environment variables
- No breaking changes

---

### Phase 2: No-Auth Mode Implementation
> **Commit:** `feat(api): add authentication off mode for demo deployments`

**Objective:** Allow authentication to be completely disabled for demo/family use.

**Tasks:**
- [ ] Create `NoAuthHandler.cs` authentication handler
- [ ] Create `FamilyUserContext.cs` with well-known user constants
- [ ] Modify `ConfigureAuthentication` to detect `Mode=None` and register `NoAuthHandler`
- [ ] Log startup WARNING when running in no-auth mode
- [ ] Ensure `IUserContext` returns family user in no-auth mode
- [ ] Update `/api/v1/config` to return `mode: "none"` when auth is off
- [ ] Write unit tests for NoAuthHandler
- [ ] Write integration tests for no-auth API access

**Validation:**
- API responds 200 to authenticated endpoints when `Mode=None`
- User context returns family user ID
- Warning logged at startup

---

### Phase 3: Client Auth-Off Adaptations
> **Commit:** `feat(client): adapt UI for auth-off mode`

**Objective:** Make the Blazor client gracefully handle no-auth mode.

**Tasks:**
- [ ] Update `AuthStateService` to handle `mode: "none"` from config
- [ ] Create `AuthOffBanner.razor` component
- [ ] Modify `NavMenu.razor` to hide login/logout/profile when auth is off
- [ ] Redirect `/authentication/*` routes to home when auth is off
- [ ] Style auth-off banner (subtle but visible)
- [ ] Write bUnit tests for conditional rendering
- [ ] Test navigation flow in auth-off mode

**Validation:**
- No login buttons visible in auth-off mode
- Banner displays and links to documentation
- No JavaScript errors or auth redirects

---

### Phase 4: Google OAuth Provider
> **Commit:** `feat(api): add Google OAuth provider support`

**Objective:** Enable Google as an authentication provider option.

**Tasks:**
- [ ] Implement Google JWT Bearer configuration in `ConfigureAuthentication`
- [ ] Add Google-specific claim mapping (Google uses different claim names)
- [ ] Update `/api/v1/config` to return Google OIDC settings
- [ ] Document Google Cloud Console setup (OAuth consent screen, credentials)
- [ ] Write integration tests with mocked Google tokens
- [ ] Test with real Google OAuth (manual testing)

**Validation:**
- Setting `Provider=Google` with valid ClientId/Secret enables Google auth
- Claims are correctly mapped to application user context
- Client can complete Google OAuth flow

---

### Phase 5: Microsoft Entra ID Provider
> **Commit:** `feat(api): add Microsoft Entra ID provider support`

**Objective:** Enable Microsoft Entra ID (Azure AD) as an authentication provider option.

**Tasks:**
- [ ] Implement Microsoft JWT Bearer configuration
- [ ] Support single-tenant and multi-tenant configurations
- [ ] Handle Microsoft-specific token validation
- [ ] Update `/api/v1/config` to return Microsoft OIDC settings
- [ ] Document Azure AD app registration process
- [ ] Write integration tests with mocked Microsoft tokens

**Validation:**
- Setting `Provider=Microsoft` with valid config enables Microsoft auth
- Works with both personal and organizational accounts (based on TenantId)

---

### Phase 6: Generic OIDC Provider
> **Commit:** `feat(api): add generic OIDC provider support for Keycloak, Auth0, etc.`

**Objective:** Enable any standard OIDC provider via configuration.

**Tasks:**
- [ ] Implement generic OIDC JWT Bearer configuration
- [ ] Support configurable claim mappings
- [ ] Support custom scopes
- [ ] Update `/api/v1/config` to return generic OIDC settings
- [ ] Document configuration for Keycloak, Auth0, and Okta examples
- [ ] Write integration tests for generic flow

**Validation:**
- Any OIDC-compliant provider can be configured
- Claim mappings work correctly
- Documentation covers common providers

---

### Phase 7: Demo Docker Compose
> **Commit:** `feat(deploy): add all-in-one demo docker-compose with bundled PostgreSQL`

**Objective:** Create a single-command deployment option that includes everything.

**Tasks:**
- [ ] Create `docker-compose.demo.yml` with PostgreSQL and API
- [ ] Configure PostgreSQL with persistent volume
- [ ] Set `Authentication__Mode=None` as default for demo
- [ ] Add health checks for both services
- [ ] Test on clean Docker environment
- [ ] Test on Raspberry Pi (ARM64)
- [ ] Document demo mode usage in README

**Validation:**
- `docker compose -f docker-compose.demo.yml up` starts everything
- Application accessible at `http://localhost:5099`
- Data persists across restarts

---

### Phase 8: Documentation
> **Commit:** `docs: add authentication providers guide and update deployment docs`

**Objective:** Comprehensive documentation for all auth options and deployment modes.

**Tasks:**
- [ ] Create `docs/AUTH-PROVIDERS.md` with per-provider setup guides
- [ ] Update `README.md` with quick-start section
- [ ] Update `DEPLOY-QUICKSTART.md` with new options
- [ ] Update `.env.example` with all auth environment variables
- [ ] Add troubleshooting section for common auth issues
- [ ] Review and update `docker-compose.pi.yml` examples

**Validation:**
- A new user can successfully deploy using only the documentation
- All configuration options are documented
- Examples are accurate and tested

---

### Phase 9: Testing & Validation
> **Commit:** `test: add comprehensive E2E tests for auth modes and providers`

**Objective:** Ensure all auth modes and providers work correctly end-to-end.

**Tasks:**
- [ ] Add Playwright E2E tests for no-auth mode
- [ ] Add API integration tests for provider switching
- [ ] Test migration from demo to production config
- [ ] Test rollback scenarios
- [ ] Performance test auth handler overhead
- [ ] Security review of no-auth mode implementation

**Validation:**
- All E2E tests pass
- No regressions in existing functionality
- Security review complete

---

## Testing Strategy

### Unit Tests

- [ ] `AuthenticationOptions` binds correctly from configuration
- [ ] `NoAuthHandler.HandleAuthenticateAsync` returns success with family user claims
- [ ] `FamilyUserContext` constants are valid
- [ ] Provider-specific options bind correctly
- [ ] Claim mappings are applied correctly

### API Integration Tests

- [ ] `GET /api/v1/config` returns correct mode/provider info
- [ ] API endpoints work with no-auth mode
- [ ] API endpoints work with each provider type (mocked tokens)
- [ ] User context returns correct user ID per mode
- [ ] Authorization attributes still apply in no-auth mode (all requests pass)

### Client Component Tests (bUnit)

- [ ] `AuthOffBanner` shows when mode is "none"
- [ ] `AuthOffBanner` hides when mode is "oidc"
- [ ] `NavMenu` hides auth UI when mode is "none"
- [ ] Authentication routes redirect when mode is "none"

### E2E Tests (Playwright)

- [ ] Complete flow in no-auth mode: navigate, create transaction, view reports
- [ ] Auth banner is visible in no-auth mode
- [ ] No JavaScript errors in no-auth mode
- [ ] OIDC flow works with Authentik provider

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

## Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Users expose no-auth mode to internet | Medium | Critical | Multiple warnings (startup, UI, docs), clear documentation |
| Breaking changes to existing Authentik setup | Low | High | Extensive backward compatibility testing, maintain old config keys |
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
