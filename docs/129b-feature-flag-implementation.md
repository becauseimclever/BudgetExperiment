# Feature Flag Implementation Proposal

**Author:** Lucius  
**Created:** 2026-04-05  
**Status:** Proposal  
**Related:** Feature 128 (Alfred's architecture), Future phased rollouts

---

## Executive Summary

**Recommended Approach:** Hand-rolled `FeatureFlagOptions` POCO with `IOptions<T>` injection, explicit configuration in `appsettings.json`, and a dedicated `/api/v1/features` endpoint for Blazor client delivery. No external dependencies.

**Why not `Microsoft.FeatureManagement`?** It's excellent for complex scenarios (targeting, A/B testing, runtime toggles, external providers like Azure App Configuration), but we don't need that complexity yet. Our flags control gradual rollout of backend-completed features to the client UI. Simple on/off switches are sufficient. If we need runtime toggles without restarts later, `IOptionsMonitor<T>` provides file-watch reloading without adding a NuGet dependency. The hand-rolled approach keeps the codebase leaner, reduces third-party abstractions, and aligns with the "no magic" principle (§3, §8 copilot-instructions.md).

---

## 1. Configuration Shape

### `appsettings.json` Structure

```json
{
  "FeatureFlags": {
    "Kakeibo": false,
    "Kaizen": false,
    "MonthlyReflection": false,
    "AdvancedCharts": true,
    "RecurringChargeSuggestions": true
  }
}
```

### Design Principles

- **Default-off for unfinished features:** New capabilities start `false` until ready for production.
- **Default-on for completed features:** Existing production features (e.g., `AdvancedCharts`, `RecurringChargeSuggestions`) default to `true`. This allows gradual retirement of flags via configuration overrides only — code stays clean.
- **Override hierarchy:** `appsettings.Development.json` → user secrets → environment variables → Docker `.env` file (Pi deployment).
- **No database storage:** Flags are deployment-time config, not runtime user preferences. Database storage would introduce concurrency/caching complexity and violate single-source-of-truth for environment config.

---

## 2. Layer Placement

```
┌─────────────────────────────────────────────────────────────────┐
│ Client (Blazor WASM)                                           │
│   └─ IFeatureFlagService (interface)                          │
│       └─ FeatureFlagService (impl, calls /api/v1/features)    │
│           └─ FeatureFlags (cached POCO)                       │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ HTTP GET /api/v1/features
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ API (ASP.NET Core)                                             │
│   └─ FeaturesController                                        │
│       └─ IOptions<FeatureFlagOptions>                         │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ DI resolution
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Shared (BudgetExperiment.Shared)                               │
│   └─ FeatureFlagOptions.cs (POCO with const SectionName)      │
└─────────────────────────────────────────────────────────────────┘
```

**Rationale:**

- **Shared:** Holds `FeatureFlagOptions` POCO — both API and Client need the same shape, no DTOs needed.
- **API:** Binds configuration, injects `IOptions<FeatureFlagOptions>`, exposes REST endpoint.
- **Client:** Fetches flags at startup, caches them in a scoped service, exposes via `IFeatureFlagService` for components.

**Why not Contracts?** Contracts are typically for request/response DTOs crossing the API boundary. Feature flags are simple config projection, not domain data or commands. `Shared` already holds enums used by both sides (`BudgetScope`, `CategorySource`); flags fit the same pattern.

---

## 3. Code Sketches

### 3.1 `FeatureFlagOptions.cs` (BudgetExperiment.Shared)

```csharp
// <copyright file="FeatureFlagOptions.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Shared;

/// <summary>
/// Feature flag configuration. Controls gradual rollout of features to clients.
/// </summary>
public sealed class FeatureFlagOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "FeatureFlags";

    /// <summary>
    /// Gets or sets a value indicating whether Kakeibo categorization is enabled.
    /// Default: false (feature in development).
    /// </summary>
    public bool Kakeibo { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether Kaizen goal tracking is enabled.
    /// Default: false (feature in development).
    /// </summary>
    public bool Kaizen { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether Monthly Reflection journal is enabled.
    /// Default: false (feature in development).
    /// </summary>
    public bool MonthlyReflection { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether advanced charts (Heatmap, Waterfall, Radar, etc.) are enabled.
    /// Default: true (Feature 127 is complete and shipped).
    /// </summary>
    public bool AdvancedCharts { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether recurring charge suggestion analysis is enabled.
    /// Default: true (existing production feature).
    /// </summary>
    public bool RecurringChargeSuggestions { get; set; } = true;
}
```

**Pattern Match:** Identical to existing `DatabaseOptions`, `AuthenticationOptions`, `ClientConfigOptions` — `const string SectionName`, XML docs, default values via property initializers.

---

### 3.2 `FeaturesController.cs` (BudgetExperiment.Api/Controllers)

```csharp
// <copyright file="FeaturesController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Shared;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for feature flags.
/// Provides feature flag state for the Blazor WebAssembly client.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[AllowAnonymous]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public sealed class FeaturesController : ControllerBase
{
    private readonly IOptions<FeatureFlagOptions> _featureFlagOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeaturesController"/> class.
    /// </summary>
    /// <param name="featureFlagOptions">The feature flag configuration options.</param>
    public FeaturesController(IOptions<FeatureFlagOptions> featureFlagOptions)
    {
        _featureFlagOptions = featureFlagOptions;
    }

    /// <summary>
    /// Gets the current feature flag state.
    /// </summary>
    /// <remarks>
    /// This endpoint is public (no authentication required) because the client
    /// needs feature flags before authentication to determine whether to show
    /// auth-gated features in the UI.
    /// </remarks>
    /// <returns>The feature flag configuration.</returns>
    [HttpGet]
    [ProducesResponseType<FeatureFlagOptions>(StatusCodes.Status200OK)]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public ActionResult<FeatureFlagOptions> GetFeatures()
    {
        return Ok(_featureFlagOptions.Value);
    }
}
```

**Pattern Match:** Identical to `ConfigController` — `IOptions<T>` injection, `[AllowAnonymous]`, `ResponseCache(Duration = 3600)` to minimize round-trips during session. The 1-hour cache is acceptable because flag changes require deployment (appsettings.json change or Docker restart).

---

### 3.3 DI Registration (API `Program.cs`)

**Add after line 96 (`AddInfrastructure`), before localization:**

```csharp
// Feature flags
builder.Services.Configure<FeatureFlagOptions>(
    builder.Configuration.GetSection(FeatureFlagOptions.SectionName));
```

**Existing Pattern:** Matches `DatabaseOptions` registration at line 246 (`IOptions<DatabaseOptions>`), `AuthenticationOptions` at line 70, `ClientConfigOptions` at line 440. Zero custom wiring — ASP.NET Core Options pattern handles everything.

---

### 3.4 Client: `IFeatureFlagService.cs` (BudgetExperiment.Client/Services)

```csharp
// <copyright file="IFeatureFlagService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Shared;

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Service for accessing feature flags in the Blazor client.
/// </summary>
public interface IFeatureFlagService
{
    /// <summary>
    /// Gets the current feature flag state.
    /// </summary>
    /// <remarks>
    /// Cached after first load. If flags could not be loaded, returns all-false defaults.
    /// </remarks>
    FeatureFlagOptions Flags { get; }

    /// <summary>
    /// Checks if a specific feature is enabled.
    /// </summary>
    /// <param name="flagName">The name of the feature flag (case-insensitive).</param>
    /// <returns><c>true</c> if the feature is enabled; otherwise, <c>false</c>.</returns>
    bool IsEnabled(string flagName);

    /// <summary>
    /// Loads feature flags from the API. Called once at app startup.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task LoadFlagsAsync();
}
```

---

### 3.5 Client: `FeatureFlagService.cs` (BudgetExperiment.Client/Services)

```csharp
// <copyright file="FeatureFlagService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net.Http.Json;

using BudgetExperiment.Shared;

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Service for accessing feature flags in the Blazor client.
/// Fetches flags from the API at startup and caches them for the session.
/// </summary>
public sealed class FeatureFlagService : IFeatureFlagService
{
    private readonly HttpClient _httpClient;
    private FeatureFlagOptions _flags = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureFlagService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client for API calls.</param>
    public FeatureFlagService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc/>
    public FeatureFlagOptions Flags => _flags;

    /// <inheritdoc/>
    public bool IsEnabled(string flagName)
    {
        return flagName switch
        {
            nameof(FeatureFlagOptions.Kakeibo) => _flags.Kakeibo,
            nameof(FeatureFlagOptions.Kaizen) => _flags.Kaizen,
            nameof(FeatureFlagOptions.MonthlyReflection) => _flags.MonthlyReflection,
            nameof(FeatureFlagOptions.AdvancedCharts) => _flags.AdvancedCharts,
            nameof(FeatureFlagOptions.RecurringChargeSuggestions) => _flags.RecurringChargeSuggestions,
            _ => false,
        };
    }

    /// <inheritdoc/>
    public async Task LoadFlagsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<FeatureFlagOptions>("api/v1/features");
            _flags = response ?? new FeatureFlagOptions();
        }
        catch (HttpRequestException)
        {
            // API unavailable or network failure — default to all-false (safe fallback)
            _flags = new FeatureFlagOptions();
        }
    }
}
```

**Graceful Degradation:** If the API is unreachable (dev environment, network partition), the client defaults to all-false flags. Completed features (`AdvancedCharts`, `RecurringChargeSuggestions`) use property initializers to default to `true`, so they'll still work even if the API fails.

---

### 3.6 Client DI Registration (`Client/Program.cs`)

**Add after line 141 (`AddScoped<IFormStateService, FormStateService>`), before localization:**

```csharp
// Feature flags
builder.Services.AddScoped<IFeatureFlagService, FeatureFlagService>();

// Load feature flags before running the app
var host = builder.Build();
var featureFlagService = host.Services.GetRequiredService<IFeatureFlagService>();
await featureFlagService.LoadFlagsAsync();
await host.RunAsync();
```

**Why Scoped?** Flags are session-stable (change requires deployment). Scoped matches the lifetime of `IBudgetApiService`, `IChatApiService`, and other data services. Singleton would prevent future per-user flag overrides (if we ever add them).

**Why Load Before `RunAsync`?** Prevents race conditions where components render before flags are ready. Existing pattern: `ClientConfigDto` is fetched at line 25 before DI registration.

---

## 4. Migration Path: Retrofitting Existing Features

### Problem

We already have production features (advanced charts, recurring charge suggestions). If we add flags and set them to `false` by default, we break existing deployments. If we set them to `true`, the flags are meaningless until we add new unfinished features.

### Solution: Default-On for Completed Features

1. **`AdvancedCharts` and `RecurringChargeSuggestions`** default to `true` via property initializers.
2. Components that use these features check `IsEnabled("AdvancedCharts")` but the flag is always `true` in production.
3. Flags can be disabled via `appsettings.json` override (e.g., for A/B testing or gradual rollout to Pi vs cloud).
4. Future features (`Kakeibo`, `Kaizen`, `MonthlyReflection`) default to `false` until implemented.

### Rollout Steps

1. **Backend:** Merge this proposal → create `FeatureFlagOptions`, `FeaturesController`, DI wiring.
2. **Client:** Add `IFeatureFlagService`, load flags at startup.
3. **Existing features:** Wrap navigation links in `@if (FeatureFlagService.IsEnabled("AdvancedCharts"))` — no behavior change (always true).
4. **New features:** Add components behind `@if (FeatureFlagService.IsEnabled("Kakeibo"))` — hidden by default.
5. **Activation:** Change `appsettings.json` `"Kakeibo": true` → redeploy → feature appears.

**No Breaking Changes:** Setting `AdvancedCharts = true` explicitly in `appsettings.json` is optional. The property initializer ensures backward compatibility.

---

## 5. Testing Strategy (Barbara's Responsibility)

### Unit Tests

**API Layer:**
- `FeaturesControllerTests` — verify `GetFeatures()` returns bound options.
- `FeatureFlagOptionsTests` — verify default values (`AdvancedCharts = true`, `Kakeibo = false`).

**Client Layer:**
- `FeatureFlagServiceTests` — verify `LoadFlagsAsync` caches flags, `IsEnabled` logic correct, graceful degradation on HTTP failure.

### Integration Tests

**API:**
- `FeaturesEndpointTests` (using `WebApplicationFactory`) — verify `/api/v1/features` returns JSON matching `appsettings.json`, verify `ResponseCache` headers.

**Client:**
- `FeatureFlagIntegrationTests` (using bUnit + mocked `HttpClient`) — verify service fetches flags from `/api/v1/features`, caches them, and components can query via `IsEnabled`.

### Manual Testing

1. Deploy with `Kakeibo: false` → verify Kakeibo nav link hidden.
2. Deploy with `Kakeibo: true` → verify Kakeibo nav link visible, page renders.
3. Simulate API failure (stop API, run client standalone) → verify client defaults to safe fallback (all-false, except `AdvancedCharts = true` via initializer).

### Performance Tests

No performance impact. Flag checks are in-memory property access (`_flags.Kakeibo`), not database queries. `/api/v1/features` endpoint is cached for 1 hour, fetched once at startup.

---

## 6. Future Extensions (NOT IN SCOPE)

### Runtime Toggles Without Restart

**Current:** Flags require `appsettings.json` change + app restart (or Docker redeploy).

**If Needed Later:**
- Replace `IOptions<T>` with `IOptionsMonitor<T>` in `FeaturesController`.
- `IOptionsMonitor` watches file changes and reloads automatically.
- Client polls `/api/v1/features` every 5 minutes (or WebSocket push).

**Cost:** Adds complexity. Not worth it unless we need runtime A/B testing.

### Per-User Feature Flags

**Current:** Flags are global (all users see same state).

**If Needed Later:**
- Add `UserFeatureFlagOverrides` table (userId, flagName, enabled).
- Merge global flags + user overrides in `FeaturesController`.
- Requires authentication context (user ID).

**Cost:** Database queries on every `/api/v1/features` call, caching complexity. Defer until user-specific rollout is required.

### Microsoft.FeatureManagement Migration

**When to Consider:**
- Targeting (enable for 10% of users, specific roles, specific environments).
- Time windows (enable flag between 2026-04-10 and 2026-04-15).
- External configuration (Azure App Configuration, LaunchDarkly).
- Percentage rollouts (gradual ramp from 0% → 100%).

**Migration Path:**
1. Keep `FeatureFlagOptions` as-is.
2. Wrap `IOptions<FeatureFlagOptions>` in a `IFeatureManagerAdapter`.
3. Introduce `Microsoft.FeatureManagement` NuGet package.
4. Replace `IOptions` injection with `IFeatureManager` in controllers.

**Effort:** ~2 hours. Not justified unless we need advanced features.

---

## 7. Decision Rationale Summary

| Question | Decision | Why |
|----------|----------|-----|
| **Library or hand-rolled?** | Hand-rolled `IOptions<T>` | Simple on/off switches; no targeting/A/B testing needed; aligns with "no magic" principle; zero external dependencies. |
| **Where to place POCO?** | `BudgetExperiment.Shared` | Both API and Client need same shape; avoids DTO mapping; matches existing `BudgetScope`, `CategorySource` enum pattern. |
| **Client delivery pattern?** | `/api/v1/features` endpoint fetched at startup | Matches existing `/api/v1/config` pattern (ConfigController); avoids embedding in HTML (simpler deployment); graceful degradation on API failure. |
| **`IOptions<T>` or `IOptionsMonitor<T>`?** | `IOptions<T>` | Flags change at deployment time (appsettings.json edit), not runtime. `IOptionsMonitor` adds file-watch overhead for zero benefit. If runtime toggles are needed later, trivial to upgrade. |
| **Default-on or default-off?** | Default-off for new features, default-on for shipped features | Prevents breaking existing deployments; allows gradual feature activation; completed features remain visible unless explicitly disabled. |
| **Authentication required?** | No (`[AllowAnonymous]`) | Client needs flags before authentication to decide whether to show login UI or auth-gated features. No sensitive data exposed (flags are deployment config, not user secrets). |
| **Cache duration?** | 1 hour (`ResponseCache(Duration = 3600)`) | Flags change at deployment time (not per request); reduces API load; matches `ConfigController` pattern. |

---

## 8. Open Questions for Alfred

1. **Flag Naming Convention:** Should flags be feature-based (`Kakeibo`, `MonthlyReflection`) or page-based (`KakeiboCategorizationPage`, `ReflectionJournalPage`)? Feature-based is more flexible (one flag can gate multiple pages).

2. **Inventory Management:** Where should we maintain the canonical list of all flags? Options:
   - A. In `FeatureFlagOptions` class itself (self-documenting, enforced by compiler).
   - B. Separate `docs/feature-flag-inventory.md` (easier to audit, but can drift from code).
   - Recommendation: (A) — code is the source of truth, docs are commentary.

3. **Blazor Component Pattern:** Should components check flags in `OnInitializedAsync` or inline in markup?
   ```razor
   @* Option A: Inline *@
   @if (FeatureFlagService.IsEnabled("Kakeibo"))
   {
       <NavLink href="/kakeibo">Kakeibo</NavLink>
   }

   @* Option B: Code-behind property *@
   @code {
       private bool ShowKakeibo => FeatureFlagService.IsEnabled("Kakeibo");
   }
   @if (ShowKakeibo)
   {
       <NavLink href="/kakeibo">Kakeibo</NavLink>
   }
   ```
   Recommendation: **Option A** (inline) — fewer lines, no cognitive overhead, matches existing `@if (clientConfig?.IsAuthEnabled == true)` pattern in `NavMenu.razor`.

4. **Backend Flag Usage:** Should API controllers check flags before executing logic, or only client hides UI?
   - Example: If `Kakeibo: false`, should `/api/v1/kakeibo` return 404 or 403?
   - Recommendation: **Client-only gating** for now. Backend endpoints are always functional. If a user manually calls the API (curl, Postman), they get data. This simplifies backend logic and avoids drift between flag state and endpoint availability. Add backend gating only if security/compliance requires it.

---

## 9. Implementation Checklist (For Alfred → Lucius Handoff)

- [ ] Create `FeatureFlagOptions.cs` in `BudgetExperiment.Shared`
- [ ] Add `Configure<FeatureFlagOptions>` in API `Program.cs`
- [ ] Create `FeaturesController.cs` with `/api/v1/features` endpoint
- [ ] Add `IFeatureFlagService` and `FeatureFlagService` in Client
- [ ] Register `IFeatureFlagService` in Client `Program.cs`
- [ ] Update `appsettings.json` with `FeatureFlags` section
- [ ] Update `appsettings.Development.json` (if needed)
- [ ] Barbara: Write unit tests for `FeaturesController`, `FeatureFlagService`
- [ ] Barbara: Write integration test for `/api/v1/features` endpoint
- [ ] Document flag inventory in `FeatureFlagOptions` XML comments
- [ ] Update `.env.example` (Docker deployment) with `FEATUREFLAGS__*` env var examples

---

## 10. Estimated Effort

- **Lucius (Implementation):** 2 hours (POCO, controller, service, DI wiring)
- **Barbara (Tests):** 3 hours (unit + integration tests for API + Client)
- **Alfred (Review & Integration):** 1 hour (ensure flag inventory matches architecture doc)
- **Total:** ~6 hours

---

## Conclusion

This hand-rolled approach delivers feature flag capability with zero external dependencies, minimal abstraction, and full compatibility with the existing Options pattern (`DatabaseOptions`, `AuthenticationOptions`, `ClientConfigOptions`). It's trivial to extend (add a property to `FeatureFlagOptions`), trivial to query (`IsEnabled("FlagName")`), and trivial to test (mock `IOptions<T>` or `HttpClient`). If we need advanced features (targeting, runtime toggles, external providers), we can migrate to `Microsoft.FeatureManagement` in ~2 hours. Until then, YAGNI.

**Next Step:** Alfred reviews this proposal, answers open questions (§8), and delegates implementation to Lucius + Barbara.
