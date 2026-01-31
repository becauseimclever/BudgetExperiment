# 056: API Config Endpoint for Client
> **Status:** ✅ Complete  
> **Created:** 2026-01-30  
> **Completed:** 2026-01-30

## Problem Statement

Currently, the Blazor WebAssembly Client uses its own static `wwwroot/appsettings.json` file for configuration (e.g., OIDC settings). This creates several issues:

1. **Configuration Drift:** Client and API auth settings can diverge, leading to authentication failures.
2. **Docker Deployment Friction:** The client's static `appsettings.json` is baked into the Docker image at build time. Changing settings (like the Authentik authority URL) requires rebuilding the image or volume-mounting files.
3. **No Single Source of Truth:** Operators must configure the same values in multiple places (`docker-compose.pi.yml` environment vars for API, but manually edit client config or create custom builds).
4. **Environment Variable Gap:** Docker environment variables flow into the API via ASP.NET Core's configuration system, but the Blazor WASM client cannot access server-side environment variables.

## Goals

- **Single Source of Truth:** All client-relevant configuration originates from the API, which reads from environment variables/appsettings.
- **Zero Static Config in Client:** Remove the need for `wwwroot/appsettings.json` in the Blazor Client.
- **Docker-Friendly:** Operators configure everything via `.env` / `docker-compose` environment variables. No custom builds needed.
- **Secure by Design:** Only expose safe, non-secret settings to the client (no connection strings, API keys, or server secrets).
- **Versioned & Documented:** Endpoint is versioned (`/api/v1/config`) and included in OpenAPI spec with examples.
- **Extensible:** Easy to add new client config values in the future (feature flags, UI settings, etc.).

## Current State Analysis

### Client Configuration (Today)
**File:** `src/BudgetExperiment.Client/wwwroot/appsettings.json`
```json
{
  "Authentication": {
    "Authentik": {
      "Authority": "https://authentik.becauseimclever.com/application/o/budget-experiment/",
      "ClientId": "kt22z8MtUCs7d7MBIZQlQfXvV9DjHd98ahp3iT3H",
      "ResponseType": "code",
      "PostLogoutRedirectUri": "/",
      "RedirectUri": "authentication/login-callback"
    }
  }
}
```

**File:** `src/BudgetExperiment.Client/Program.cs`
```csharp
builder.Services.AddOidcAuthentication(options =>
{
    builder.Configuration.Bind("Authentication:Authentik", options.ProviderOptions);
    // ...
});
```

### API Configuration (Today)
**File:** `src/BudgetExperiment.Api/appsettings.json` + environment variables
```json
{
  "Authentication": {
    "Authentik": {
      "Authority": "...",
      "Audience": "...",
      "RequireHttpsMetadata": true
    }
  }
}
```

**Docker Compose Environment (example):**
```yaml
- Authentication__Authentik__Enabled=${AUTHENTIK_ENABLED:-false}
- Authentication__Authentik__Authority=${AUTHENTIK_AUTHORITY:-}
- Authentication__Authentik__Audience=${AUTHENTIK_AUDIENCE:-}
```

### The Gap
The API can read `AUTHENTIK_AUTHORITY` from the environment, but the Blazor WASM client cannot—it only sees its static `appsettings.json` baked into the published `wwwroot`.

## Proposed Solution

### 1. Create `/api/v1/config` Endpoint (Unauthenticated)

The API exposes a public (no auth required) endpoint that returns client-relevant configuration:

```
GET /api/v1/config
```

**Response (200 OK):**
```json
{
  "authentication": {
    "mode": "oidc",
    "oidc": {
      "authority": "https://authentik.becauseimclever.com/application/o/budget-experiment/",
      "clientId": "kt22z8MtUCs7d7MBIZQlQfXvV9DjHd98ahp3iT3H",
      "responseType": "code",
      "scopes": ["openid", "profile", "email"],
      "postLogoutRedirectUri": "/",
      "redirectUri": "authentication/login-callback"
    }
  }
}
```

**Key Design Decisions:**
- **Unauthenticated:** The endpoint must be public because the client needs config *before* it can authenticate.
- **Cache-Friendly:** Response can include `Cache-Control` headers (e.g., `max-age=3600`) since config rarely changes.
- **No Secrets:** Never include connection strings, API keys, JWT signing keys, or any server-side secrets.

### 2. Define DTO in `BudgetExperiment.Contracts`

**File:** `src/BudgetExperiment.Contracts/Dtos/ClientConfigDto.cs`

```csharp
/// <summary>
/// Configuration settings exposed to the Blazor WebAssembly client.
/// This DTO contains ONLY non-secret, client-appropriate settings.
/// </summary>
public sealed class ClientConfigDto
{
    /// <summary>
    /// Authentication configuration.
    /// </summary>
    public required AuthenticationConfigDto Authentication { get; init; }
}

/// <summary>
/// Authentication configuration for the client.
/// </summary>
public sealed class AuthenticationConfigDto
{
    /// <summary>
    /// Authentication mode: "none", "oidc".
    /// When "none", auth is disabled and all users get default scope.
    /// </summary>
    public required string Mode { get; init; }

    /// <summary>
    /// OIDC provider settings (populated when Mode = "oidc").
    /// </summary>
    public OidcConfigDto? Oidc { get; init; }
}

/// <summary>
/// OIDC provider configuration.
/// </summary>
public sealed class OidcConfigDto
{
    /// <summary>
    /// The OIDC authority URL (issuer).
    /// </summary>
    public required string Authority { get; init; }

    /// <summary>
    /// The OAuth2 client ID (public identifier, NOT a secret for PKCE flows).
    /// </summary>
    public required string ClientId { get; init; }

    /// <summary>
    /// OAuth2 response type (typically "code" for PKCE).
    /// </summary>
    public string ResponseType { get; init; } = "code";

    /// <summary>
    /// Scopes to request during authentication.
    /// </summary>
    public IReadOnlyList<string> Scopes { get; init; } = ["openid", "profile", "email"];

    /// <summary>
    /// Redirect URI after logout.
    /// </summary>
    public string PostLogoutRedirectUri { get; init; } = "/";

    /// <summary>
    /// Redirect URI after login callback.
    /// </summary>
    public string RedirectUri { get; init; } = "authentication/login-callback";
}
```

### 3. Add API Controller

**File:** `src/BudgetExperiment.Api/Controllers/ConfigController.cs`

```csharp
/// <summary>
/// Provides client configuration settings.
/// </summary>
[ApiController]
[Route("api/v1/config")]
[ApiVersion("1.0")]
[AllowAnonymous] // Must be public - client needs config before auth
public class ConfigController : ControllerBase
{
    private readonly IOptions<ClientConfigOptions> _clientConfigOptions;

    public ConfigController(IOptions<ClientConfigOptions> clientConfigOptions)
    {
        _clientConfigOptions = clientConfigOptions;
    }

    /// <summary>
    /// Gets client configuration settings.
    /// </summary>
    /// <returns>Client configuration DTO.</returns>
    [HttpGet]
    [ProducesResponseType<ClientConfigDto>(StatusCodes.Status200OK)]
    [ResponseCache(Duration = 3600)] // Cache for 1 hour
    public ActionResult<ClientConfigDto> GetConfig()
    {
        var options = _clientConfigOptions.Value;
        return Ok(options.ToDto());
    }
}
```

### 4. Create API Options Class

**File:** `src/BudgetExperiment.Api/ClientConfigOptions.cs`

```csharp
/// <summary>
/// Strongly-typed options for client configuration.
/// Maps from IConfiguration and exposes settings safe for client consumption.
/// </summary>
public sealed class ClientConfigOptions
{
    public const string SectionName = "ClientConfig";

    // Authentication
    public string AuthMode { get; set; } = "oidc";
    public string OidcAuthority { get; set; } = string.Empty;
    public string OidcClientId { get; set; } = string.Empty;
    public string OidcResponseType { get; set; } = "code";
    public List<string> OidcScopes { get; set; } = ["openid", "profile", "email"];
    public string OidcPostLogoutRedirectUri { get; set; } = "/";
    public string OidcRedirectUri { get; set; } = "authentication/login-callback";

    public ClientConfigDto ToDto() => new()
    {
        Authentication = new AuthenticationConfigDto
        {
            Mode = AuthMode,
            Oidc = AuthMode == "oidc" ? new OidcConfigDto
            {
                Authority = OidcAuthority,
                ClientId = OidcClientId,
                ResponseType = OidcResponseType,
                Scopes = OidcScopes,
                PostLogoutRedirectUri = OidcPostLogoutRedirectUri,
                RedirectUri = OidcRedirectUri,
            } : null,
        },
    };
}
```

### 5. Wire Up Configuration in API

**File:** `src/BudgetExperiment.Api/Program.cs` (add to DI setup)

```csharp
// Build ClientConfigOptions from existing configuration sources
builder.Services.Configure<ClientConfigOptions>(options =>
{
    var authentikSection = builder.Configuration.GetSection("Authentication:Authentik");
    
    // Auth mode (check if Authentik is enabled)
    var enabled = authentikSection.GetValue<bool?>("Enabled") ?? true;
    options.AuthMode = enabled ? "oidc" : "none";
    
    // OIDC settings (from Authentik config)
    options.OidcAuthority = authentikSection.GetValue<string>("Authority") ?? string.Empty;
    options.OidcClientId = authentikSection.GetValue<string>("ClientId") 
        ?? authentikSection.GetValue<string>("Audience") 
        ?? string.Empty;
    
    // Client-specific overrides (if provided in ClientConfig section)
    var clientSection = builder.Configuration.GetSection("ClientConfig");
    if (clientSection.Exists())
    {
        clientSection.Bind(options);
    }
});
```

### 6. Update Blazor Client to Fetch Config at Startup

**File:** `src/BudgetExperiment.Client/Program.cs`

```csharp
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Fetch configuration from API before configuring services
using var httpClient = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
var configResponse = await httpClient.GetFromJsonAsync<ClientConfigDto>("api/v1/config");

if (configResponse is null)
{
    throw new InvalidOperationException("Failed to load client configuration from API.");
}

// Register config as singleton for injection (if needed by components)
builder.Services.AddSingleton(configResponse);
builder.Services.AddSingleton(configResponse.Authentication);

// Configure OIDC authentication based on fetched config
if (configResponse.Authentication.Mode == "oidc" && configResponse.Authentication.Oidc is not null)
{
    var oidc = configResponse.Authentication.Oidc;
    builder.Services.AddOidcAuthentication(options =>
    {
        options.ProviderOptions.Authority = oidc.Authority;
        options.ProviderOptions.ClientId = oidc.ClientId;
        options.ProviderOptions.ResponseType = oidc.ResponseType;
        options.ProviderOptions.PostLogoutRedirectUri = oidc.PostLogoutRedirectUri;
        options.ProviderOptions.RedirectUri = oidc.RedirectUri;
        
        foreach (var scope in oidc.Scopes)
        {
            options.ProviderOptions.DefaultScopes.Add(scope);
        }
    });
}
else
{
    // No-op auth for "none" mode - use a custom auth state provider
    builder.Services.AddSingleton<AuthenticationStateProvider, AnonymousAuthStateProvider>();
}

// ... rest of service registration
```

### 7. Update Docker Compose Environment Variables

No new environment variables are required. The existing `Authentication__Authentik__*` variables already provide the auth configuration. The API will read these and expose them via `/api/v1/config`.

**Existing variables (already in `docker-compose.pi.yml`):**
```yaml
environment:
  # Authentication (Authentik OIDC) - these flow to /api/v1/config
  - Authentication__Authentik__Enabled=${AUTHENTIK_ENABLED:-false}
  - Authentication__Authentik__Authority=${AUTHENTIK_AUTHORITY:-}
  - Authentication__Authentik__Audience=${AUTHENTIK_AUDIENCE:-}
  - Authentication__Authentik__ClientId=${AUTHENTIK_CLIENT_ID:-}  # Add if different from Audience
  - Authentication__Authentik__RequireHttpsMetadata=${AUTHENTIK_REQUIRE_HTTPS:-true}
```

**Note:** The `ClientId` for the client OIDC flow may differ from `Audience` (used for API token validation). If they're the same, the API will use `Audience` as a fallback. If different, add `Authentication__Authentik__ClientId` to your environment.

### 8. Remove Static Client Config

Delete or empty `src/BudgetExperiment.Client/wwwroot/appsettings.json` (keep file for potential local dev fallback, but empty the auth settings).

## Configuration Flow Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         Docker Environment                              │
│  .env file / docker-compose.yml environment variables                  │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ AUTHENTIK_AUTHORITY=https://auth.example.com/...                │   │
│  │ AUTHENTIK_CLIENT_ID=my-client-id                                │   │
│  │ FEATURES_AI_ENABLED=true                                        │   │
│  └─────────────────────────────────────────────────────────────────┘   │
└───────────────────────────────┬─────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                    ASP.NET Core API (BudgetExperiment.Api)              │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ IConfiguration                                                   │   │
│  │  ├─ Environment Variables (highest priority)                    │   │
│  │  ├─ appsettings.Production.json                                 │   │
│  │  └─ appsettings.json (base defaults)                            │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                │                                        │
│                                ▼                                        │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ ClientConfigOptions (strongly-typed, maps from IConfiguration)  │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                │                                        │
│                                ▼                                        │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ GET /api/v1/config → ClientConfigDto (JSON)                     │   │
│  │ (public endpoint, cached, no secrets)                           │   │
│  └─────────────────────────────────────────────────────────────────┘   │
└───────────────────────────────┬─────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────────────┐
│              Blazor WebAssembly Client (BudgetExperiment.Client)        │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ Program.cs: Fetch /api/v1/config at startup                     │   │
│  │ Configure OIDC from response                                    │   │
│  │ Register ClientConfigDto as singleton                           │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                │                                        │
│                                ▼                                        │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ Components inject ClientConfigDto to access feature flags, etc. │   │
│  └─────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────┘
```

## Acceptance Criteria

- [x] API exposes `GET /api/v1/config` endpoint returning `ClientConfigDto`.
- [x] Endpoint requires no authentication (`[AllowAnonymous]`).
- [x] Endpoint is documented in OpenAPI spec with examples.
- [x] Only non-secret, client-appropriate settings are included (audit checklist below).
- [x] Client fetches config from API at startup before configuring OIDC.
- [x] Client's `wwwroot/appsettings.json` is removed or emptied of auth settings.
- [x] `AuthenticationConfigDto` is injectable in Blazor components if needed.
- [x] Auth configuration flows from Docker environment variables through to client.
- [x] Unit tests for `ClientConfigOptions.ToDto()` mapping.
- [x] Integration tests for `/api/v1/config` endpoint.
- [x] Client startup handles config fetch failure gracefully (error UI or retry).
- [ ] Documentation updated:
  - [ ] `docker-compose.pi.yml` example with all new env vars.
  - [ ] `DEPLOY-QUICKSTART.md` updated with config endpoint info.
  - [ ] `copilot-instructions.md` section 17 updated if needed.

## Security Audit Checklist

Before merging, verify these are **NOT** exposed in `/api/v1/config`:

| Setting | Exposed? | Reason |
|---------|----------|--------|
| ConnectionStrings:AppDb | ❌ NO | Database credentials |
| Authentication:Authentik:Audience (server-side) | ❌ NO | Used for API token validation only |
| JWT signing keys | ❌ NO | Server secret |
| API keys (OpenAI, etc.) | ❌ NO | Server secret |
| RequireHttpsMetadata | ❌ NO | Server-side validation setting |
| OIDC Authority URL | ✅ YES | Public endpoint, required by client |
| OIDC ClientId | ✅ YES | Public identifier (PKCE flow, not a secret) |
| OIDC Scopes | ✅ YES | Standard OIDC scopes |
| Redirect URIs | ✅ YES | Client routing paths |

## Out of Scope

- Per-user or per-session configuration (this is global/static config only).
- Server-side secrets or sensitive configuration.
- Real-time config updates (client must refresh/restart to pick up changes).
- Client-side caching of config (rely on HTTP caching headers).

## Implementation Order (TDD)

1. **Domain/Contracts:** Create `ClientConfigDto` and related DTOs in `BudgetExperiment.Contracts`.
2. **API Options:** Create `ClientConfigOptions` in `BudgetExperiment.Api`.
3. **Unit Tests:** Write tests for `ClientConfigOptions.ToDto()` mapping (RED → GREEN).
4. **API Controller:** Create `ConfigController` with `GetConfig()` endpoint.
5. **Integration Tests:** Write tests for `/api/v1/config` endpoint (happy path, caching headers).
6. **API Wiring:** Wire up `ClientConfigOptions` from `IConfiguration` in `Program.cs`.
7. **Client Refactor:** Update `Program.cs` to fetch config at startup.
8. **Client Tests:** Verify client startup with mocked config endpoint (bUnit if applicable).
9. **Remove Static Config:** Delete/empty `wwwroot/appsettings.json`.
10. **Docker Compose:** Update `docker-compose.pi.yml` with new env vars.
11. **Documentation:** Update deployment docs.

## Example: Full Docker Compose Configuration

```yaml
# docker-compose.pi.yml (no changes needed - existing auth vars are sufficient)
services:
  budgetexperiment:
    image: ghcr.io/becauseimclever/budgetexperiment:latest
    environment:
      # Database
      - ConnectionStrings__AppDb=${DB_CONNECTION_STRING}
      
      # Authentication (used by both API validation AND exposed to client via /api/v1/config)
      - Authentication__Authentik__Enabled=${AUTHENTIK_ENABLED:-true}
      - Authentication__Authentik__Authority=${AUTHENTIK_AUTHORITY}
      - Authentication__Authentik__Audience=${AUTHENTIK_AUDIENCE}
      - Authentication__Authentik__ClientId=${AUTHENTIK_CLIENT_ID:-}  # Optional: if different from Audience
      - Authentication__Authentik__RequireHttpsMetadata=${AUTHENTIK_REQUIRE_HTTPS:-true}
```

```env
# .env file example
DB_CONNECTION_STRING=Host=postgres;Database=budget;Username=budget;Password=secret
AUTHENTIK_ENABLED=true
AUTHENTIK_AUTHORITY=https://auth.example.com/application/o/budget/
AUTHENTIK_AUDIENCE=budget-api-audience
AUTHENTIK_CLIENT_ID=budget-client-id
AUTHENTIK_REQUIRE_HTTPS=true
```

**Note:** `AUTHENTIK_AUDIENCE` is used by the API for JWT token validation. `AUTHENTIK_CLIENT_ID` is used by the client for the OIDC login flow. In many setups these are the same value, but they can differ.

## Future Enhancements

- **Feature Flags:** Add feature flags to the config DTO when needed (e.g., `aiEnabled`, `demoMode`). These would be configured in `appsettings.json` rather than environment variables.
- **UI Customization:** Add UI settings (app name, default theme) to the config DTO if branding customization is needed.
- **Config Refresh:** Add a mechanism for the client to periodically refresh config without full page reload.
- **Config Versioning:** Include a config schema version for client compatibility checks.
- **Additional Auth Providers:** Extend `AuthenticationConfigDto` to support Google, Microsoft, or other OIDC providers.

## References

- [copilot-instructions.md](../.github/copilot-instructions.md) - Sections 9, 10, 17 (API, OpenAPI, Config guidance)
- [Feature 055: Easy Deployment Auth Options](055-easy-deployment-auth-options.md)
- [Feature 022: Authentik Integration](archive/022-authentik-integration.md)
- [ASP.NET Core Configuration Documentation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Blazor WASM Authentication](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/webassembly/)
