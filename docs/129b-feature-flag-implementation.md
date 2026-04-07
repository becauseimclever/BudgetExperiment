# Feature Flag Implementation Proposal

**Author:** Lucius  
**Created:** 2026-04-05  
**Updated:** 2026-04-09 (Runtime toggleability — Database + Cache)  
**Status:** Approved (Alfred — `.squad/decisions/inbox/alfred-runtime-feature-flags.md`)  
**Related:** Feature 128 (Alfred's architecture), Feature 129 (Kakeibo alignment audit)

---

## Executive Summary

**Recommended Approach:** Database-backed feature flags with in-memory cache (`IMemoryCache`). Flags are stored in a `FeatureFlags` DB table, loaded into cache at startup, and toggled at runtime via `PUT /api/v1/features/{flagName}` admin endpoint. Client fetches flags via `GET /api/v1/features` (public, cached for 60 seconds). No external dependencies.

**Why database instead of file-based config?** The user requires runtime toggleability without restarts or performance impact. File-based flags (`IOptions<T>`) require app restart. `IOptionsMonitor<T>` with file hot-reload doesn't work in Docker (env vars don't hot-reload, modifying `appsettings.json` in containers is ephemeral/unsafe). Database-backed flags deliver zero per-request overhead (cache hit) while enabling true runtime toggles across all deployment contexts (local dev, Docker, Raspberry Pi). The cost is one DB table and a cache invalidation pattern — both standard, low-complexity infrastructure.

**Why not `Microsoft.FeatureManagement`?** It's excellent for complex scenarios (targeting, A/B testing, percentage rollouts, external providers), but we don't need that complexity yet. Our flags control gradual rollout of features and user simplification (hiding unused features). Simple on/off switches are sufficient. The hand-rolled DB + cache approach keeps the codebase leaner, reduces third-party abstractions, and aligns with the "no magic" principle (§3, §8 copilot-instructions.md).

---

## 1. Storage & Runtime Toggleability

### Database Schema

**Table:** `FeatureFlags`

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `Name` | TEXT | PRIMARY KEY | Flag name (e.g., `Calendar:SpendingHeatmap`) |
| `IsEnabled` | BOOLEAN | NOT NULL | Current flag state |
| `UpdatedAtUtc` | TIMESTAMP | NOT NULL | Last toggle timestamp (UTC) |

**Rationale:** Database is the source of truth for flag state. Allows runtime toggles without app restarts or file edits. Supports all deployment contexts (local dev, Docker, Pi) uniformly.

### Seed Data Strategy

**Initial provisioning:** Seed 17 flags from Feature 129 audit (`docs/129-feature-audit-kakeibo-alignment.md`) via application startup INSERT-if-missing logic (never `EF Core HasData()`).

> ⚠️ **PROHIBITED:** Do NOT use `builder.HasData(...)` in entity configuration for feature flags. `HasData()` generates migration SQL that overwrites existing rows during `dotnet ef database update` and Docker container startup — this would destroy user customisations every time the app updates. Use the startup seeder below instead.

**INSERT-if-missing contract:** At application startup (after `MigrateAsync()`), the `FeatureFlagSeeder` writes each default flag using `ON CONFLICT (Name) DO NOTHING`. New flags added in future releases are seeded with their defaults; flags the user has already toggled are never touched.

**Defaults applied at seed time:** Default-off for experimental/unfinished features (`Calendar:KakeiboOverlay`, `Reports:CustomReportBuilder`, `Kaizen:Dashboard`). Default-on for completed/shipped features (`AI:ChatAssistant`, `AI:RecurringChargeDetection`, `Reports:LocationReport`, `Paycheck:PaycheckPlanner`).

**Optional file-based seeding:** For dev convenience, an `appsettings.json` `FeatureFlags` section can be used to hydrate the DB on first run (via startup seed logic). Example:

```json
{
  "FeatureFlags": {
    "Calendar:SpendingHeatmap": true,
    "Calendar:KakeiboOverlay": false,
    "Kakeibo:MonthlyReflectionPrompts": true,
    "Kakeibo:CalendarOverlay": false,
    "Kaizen:MicroGoals": true,
    "Kaizen:Dashboard": false,
    "AI:ChatAssistant": true,
    "AI:RuleSuggestions": true,
    "AI:RecurringChargeDetection": true,
    "Reports:CustomReportBuilder": false,
    "Reports:LocationReport": true,
    "Charts:AdvancedCharts": false,
    "Paycheck:PaycheckPlanner": true,
    "Reconciliation:StatementReconciliation": true,
    "DataHealth:Dashboard": true,
    "Location:Geocoding": false
  }
}
```

**Important:** After initial seed, `appsettings.json` is NOT the source of truth. Runtime toggles via API write to DB only. File changes do not propagate unless DB is re-seeded (destructive operation).

---

## 2. Layer Placement

```
┌─────────────────────────────────────────────────────────────────┐
│ Client (Blazor WASM)                                           │
│   └─ IFeatureFlagClientService (interface)                    │
│       └─ FeatureFlagClientService (impl, calls /api/v1/features)│
│           └─ FeatureFlags (cached POCO)                       │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ HTTP GET /api/v1/features (public, 60s cache)
                              │ HTTP PUT /api/v1/features/{name} (admin, invalidates cache)
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ API (ASP.NET Core)                                             │
│   └─ FeaturesController                                        │
│       └─ IMemoryCache (key: "feature:all", TTL: 5min)        │
│           └─ IFeatureFlagService (Application layer)          │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ DI resolution
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Application (BudgetExperiment.Application)                     │
│   └─ IFeatureFlagService (interface)                          │
│       └─ FeatureFlagService (impl, uses repository + cache)   │
│           └─ IFeatureFlagRepository (Infrastructure)          │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ Repository abstraction
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Infrastructure (BudgetExperiment.Infrastructure)               │
│   └─ FeatureFlagRepository (EF Core)                          │
│       └─ AppDbContext.FeatureFlags (DbSet<FeatureFlag>)       │
│           └─ PostgreSQL: FeatureFlags table                   │
└─────────────────────────────────────────────────────────────────┘
```

**Rationale:**

- **Database (Infrastructure):** Holds runtime-toggleable flag state. Migration seeds defaults from Feature 129 audit.
- **Application Service:** `IFeatureFlagService.IsEnabled(string flagName)` checks `IMemoryCache` first (cache hit = zero DB overhead), falls back to repository on cache miss. `SetFlagAsync(string flagName, bool enabled)` writes to DB, invalidates cache.
- **API Controller:** Read endpoint `GET /api/v1/features` returns all flags (public, `ResponseCache` 60 seconds). Write endpoint `PUT /api/v1/features/{flagName}` requires `[Authorize]` (admin users only).
- **Client Service:** Fetches flags at startup, caches for session, exposes `IsEnabled(string flagName)` for Razor components. `RefreshAsync()` method re-fetches from API (for use after admin toggle, if admin panel is in Blazor client).

---

## 3. Code Sketches

### 3.1 `FeatureFlag.cs` (BudgetExperiment.Domain/Entities)

```csharp
// <copyright file="FeatureFlag.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Entities;

/// <summary>
/// Feature flag entity. Controls gradual rollout of features to clients.
/// </summary>
public sealed class FeatureFlag
{
    /// <summary>
    /// Gets or sets the flag name (e.g., "Calendar:SpendingHeatmap").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the feature is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp (UTC).
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
```

---

### 3.2 `FeatureFlagConfiguration.cs` (BudgetExperiment.Infrastructure/Data/Config)

```csharp
// <copyright file="FeatureFlagConfiguration.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetExperiment.Infrastructure.Data.Config;

/// <summary>
/// EF Core configuration for <see cref="FeatureFlag"/>.
/// </summary>
public sealed class FeatureFlagConfiguration : IEntityTypeConfiguration<FeatureFlag>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<FeatureFlag> builder)
    {
        builder.ToTable("FeatureFlags");

        builder.HasKey(f => f.Name);

        builder.Property(f => f.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(f => f.IsEnabled)
            .IsRequired();

        builder.Property(f => f.UpdatedAtUtc)
            .IsRequired();

        // ⚠️ DO NOT add HasData() here — see Seed Data Strategy in section 2.3.
        // Default flags are seeded via FeatureFlagSeeder (INSERT-if-missing at startup).
    }
}
```

---

### 3.2b `FeatureFlagSeeder.cs` (BudgetExperiment.Infrastructure/Persistence/Seeders)

Called from `Program.cs` after `MigrateAsync()`. Uses raw SQL `ON CONFLICT (Name) DO NOTHING` so
existing user-toggled values are never overwritten by updates.

```csharp
// <copyright file="FeatureFlagSeeder.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure.Persistence.Seeders;

/// <summary>
/// Seeds default feature flags at application startup.
/// Uses INSERT-if-missing (ON CONFLICT DO NOTHING) so user-toggled values are never overwritten.
/// </summary>
public static class FeatureFlagSeeder
{
    private static readonly (string Name, bool IsEnabled)[] Defaults =
    [
        ("Calendar:SpendingHeatmap",              true),
        ("Calendar:KakeiboOverlay",               false),
        ("Kakeibo:MonthlyReflectionPrompts",      true),
        ("Kakeibo:CalendarOverlay",               false),
        ("Kaizen:MicroGoals",                     true),
        ("Kaizen:Dashboard",                      false),
        ("AI:ChatAssistant",                      true),
        ("AI:RuleSuggestions",                    true),
        ("AI:RecurringChargeDetection",           true),
        ("Reports:CustomReportBuilder",           false),
        ("Reports:LocationReport",                true),
        ("Charts:AdvancedCharts",                 false),
        ("Charts:CandlestickChart",               false),
        ("Paycheck:PaycheckPlanner",              true),
        ("Reconciliation:StatementReconciliation",true),
        ("DataHealth:Dashboard",                  true),
        ("Location:Geocoding",                    false),
    ];

    /// <summary>
    /// Seeds any missing feature flags with their default values. Never overwrites existing rows.
    /// </summary>
    public static async Task SeedAsync(BudgetDbContext context)
    {
        var now = DateTime.UtcNow;
        foreach (var (name, isEnabled) in Defaults)
        {
            await context.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO "FeatureFlags" ("Name", "IsEnabled", "UpdatedAtUtc")
                VALUES ({0}, {1}, {2})
                ON CONFLICT ("Name") DO NOTHING
                """,
                name, isEnabled, now);
        }
    }
}
```

**`Program.cs` call site (after `MigrateAsync()`):**

```csharp
await using var scope = app.Services.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<BudgetDbContext>();
await db.Database.MigrateAsync();
await FeatureFlagSeeder.SeedAsync(db);
```

---

### 3.3 `IFeatureFlagRepository.cs` (BudgetExperiment.Application/Common/Interfaces/Repositories)

```csharp
// <copyright file="IFeatureFlagRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain.Entities;

namespace BudgetExperiment.Application.Common.Interfaces.Repositories;

/// <summary>
/// Repository for feature flag persistence.
/// </summary>
public interface IFeatureFlagRepository
{
    /// <summary>
    /// Gets all feature flags.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All feature flags.</returns>
    Task<IReadOnlyList<FeatureFlag>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single feature flag by name.
    /// </summary>
    /// <param name="name">Flag name (case-sensitive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The flag, or null if not found.</returns>
    Task<FeatureFlag?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a feature flag's enabled state.
    /// </summary>
    /// <param name="name">Flag name (case-sensitive).</param>
    /// <param name="isEnabled">New enabled state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(string name, bool isEnabled, CancellationToken cancellationToken = default);
}
```

---

### 3.4 `FeatureFlagRepository.cs` (BudgetExperiment.Infrastructure/Data/Repositories)

```csharp
// <copyright file="FeatureFlagRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Common.Interfaces.Repositories;
using BudgetExperiment.Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for feature flags.
/// </summary>
public sealed class FeatureFlagRepository : IFeatureFlagRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureFlagRepository"/> class.
    /// </summary>
    /// <param name="context">Database context.</param>
    public FeatureFlagRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<FeatureFlag>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FeatureFlags
            .AsNoTracking()
            .OrderBy(f => f.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<FeatureFlag?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.FeatureFlags
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Name == name, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(string name, bool isEnabled, CancellationToken cancellationToken = default)
    {
        var flag = await _context.FeatureFlags
            .FirstOrDefaultAsync(f => f.Name == name, cancellationToken);

        if (flag == null)
        {
            return;
        }

        flag.IsEnabled = isEnabled;
        flag.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

---

### 3.5 `IFeatureFlagService.cs` (BudgetExperiment.Application/Common/Interfaces/Services)

```csharp
// <copyright file="IFeatureFlagService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Common.Interfaces.Services;

/// <summary>
/// Service for accessing and managing feature flags.
/// </summary>
public interface IFeatureFlagService
{
    /// <summary>
    /// Checks if a feature flag is enabled.
    /// </summary>
    /// <param name="flagName">Flag name (e.g., "Calendar:SpendingHeatmap").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if enabled; otherwise, <c>false</c>.</returns>
    Task<bool> IsEnabledAsync(string flagName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all feature flags.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary of flag name → enabled state.</returns>
    Task<Dictionary<string, bool>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a feature flag's enabled state (admin operation).
    /// </summary>
    /// <param name="flagName">Flag name (case-sensitive).</param>
    /// <param name="isEnabled">New enabled state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetFlagAsync(string flagName, bool isEnabled, CancellationToken cancellationToken = default);
}
```

---

### 3.6 `FeatureFlagService.cs` (BudgetExperiment.Application/Services)

```csharp
// <copyright file="FeatureFlagService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Common.Interfaces.Repositories;
using BudgetExperiment.Application.Common.Interfaces.Services;

using Microsoft.Extensions.Caching.Memory;

namespace BudgetExperiment.Application.Services;

/// <summary>
/// Service for accessing and managing feature flags with in-memory cache.
/// </summary>
public sealed class FeatureFlagService : IFeatureFlagService
{
    private const string CacheKey = "feature:all";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly IFeatureFlagRepository _repository;
    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureFlagService"/> class.
    /// </summary>
    /// <param name="repository">Feature flag repository.</param>
    /// <param name="cache">Memory cache.</param>
    public FeatureFlagService(IFeatureFlagRepository repository, IMemoryCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    /// <inheritdoc/>
    public async Task<bool> IsEnabledAsync(string flagName, CancellationToken cancellationToken = default)
    {
        var flags = await GetAllAsync(cancellationToken);
        return flags.TryGetValue(flagName, out var isEnabled) && isEnabled;
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, bool>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue<Dictionary<string, bool>>(CacheKey, out var cached))
        {
            return cached!;
        }

        var flags = await _repository.GetAllAsync(cancellationToken);
        var dict = flags.ToDictionary(f => f.Name, f => f.IsEnabled);

        _cache.Set(CacheKey, dict, CacheDuration);

        return dict;
    }

    /// <inheritdoc/>
    public async Task SetFlagAsync(string flagName, bool isEnabled, CancellationToken cancellationToken = default)
    {
        await _repository.UpdateAsync(flagName, isEnabled, cancellationToken);
        _cache.Remove(CacheKey); // Invalidate cache
    }
}
```

---

### 3.7 `FeaturesController.cs` (BudgetExperiment.Api/Controllers)

```csharp
// <copyright file="FeaturesController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Common.Interfaces.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for feature flags.
/// Provides feature flag state for the Blazor WebAssembly client.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public sealed class FeaturesController : ControllerBase
{
    private readonly IFeatureFlagService _featureFlagService;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeaturesController"/> class.
    /// </summary>
    /// <param name="featureFlagService">Feature flag service.</param>
    public FeaturesController(IFeatureFlagService featureFlagService)
    {
        _featureFlagService = featureFlagService;
    }

    /// <summary>
    /// Gets the current feature flag state for all flags.
    /// </summary>
    /// <remarks>
    /// This endpoint is public (no authentication required) because the client
    /// needs feature flags before authentication to determine whether to show
    /// auth-gated features in the UI. Cached for 60 seconds to reduce API load.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary of flag name → enabled state.</returns>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<Dictionary<string, bool>>(StatusCodes.Status200OK)]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<Dictionary<string, bool>>> GetFeatures(CancellationToken cancellationToken)
    {
        var flags = await _featureFlagService.GetAllAsync(cancellationToken);
        return Ok(flags);
    }

    /// <summary>
    /// Updates a feature flag's enabled state (admin operation).
    /// </summary>
    /// <param name="flagName">Flag name (e.g., "Calendar:SpendingHeatmap").</param>
    /// <param name="request">Update request containing new enabled state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with updated flag state, or 404 if flag not found.</returns>
    [HttpPut("{flagName}")]
    [Authorize]
    [ProducesResponseType<UpdateFeatureFlagResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UpdateFeatureFlagResponse>> UpdateFeatureFlag(
        string flagName,
        [FromBody] UpdateFeatureFlagRequest request,
        CancellationToken cancellationToken)
    {
        await _featureFlagService.SetFlagAsync(flagName, request.Enabled, cancellationToken);

        var isEnabled = await _featureFlagService.IsEnabledAsync(flagName, cancellationToken);

        return Ok(new UpdateFeatureFlagResponse
        {
            Name = flagName,
            Enabled = isEnabled,
            UpdatedAtUtc = DateTime.UtcNow,
        });
    }
}

/// <summary>
/// Request DTO for updating a feature flag.
/// </summary>
public sealed record UpdateFeatureFlagRequest
{
    /// <summary>
    /// Gets or sets a value indicating whether the flag should be enabled.
    /// </summary>
    public bool Enabled { get; set; }
}

/// <summary>
/// Response DTO for updating a feature flag.
/// </summary>
public sealed record UpdateFeatureFlagResponse
{
    /// <summary>
    /// Gets or sets the flag name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the flag is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the update timestamp (UTC).
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; }
}
```

**Pattern Match:** Matches existing `ConfigController` (`[AllowAnonymous]` on GET, `ResponseCache` for client efficiency). Write endpoint requires `[Authorize]` (admin users only). Cache duration reduced from 3600 to 60 seconds to allow faster propagation of runtime toggles (1-hour eventual consistency on client side is acceptable per Alfred's decision).

---

### 3.8 DI Registration (API `Program.cs` + Application + Infrastructure)

**Infrastructure `DependencyInjection.cs` (add repository registration):**

```csharp
// After existing repository registrations (around line 45)
services.AddScoped<IFeatureFlagRepository, FeatureFlagRepository>();
```

**Application `DependencyInjection.cs` (add service registration + memory cache):**

```csharp
// Add IMemoryCache if not already registered (check existing registrations first)
services.AddMemoryCache();

// Add feature flag service (around line 60, after other scoped services)
services.AddScoped<IFeatureFlagService, FeatureFlagService>();
```

**API `Program.cs` (no changes required — repository/service registered in layer DI extensions).**

**Existing Pattern:** Matches `IBudgetCategoryRepository` + `BudgetCategoryRepository` registration, `IAccountService` + `AccountService` registration. Zero custom wiring — layered DI extension methods handle everything.

---

### 3.9 Client: `IFeatureFlagClientService.cs` (BudgetExperiment.Client/Services)

```csharp
// <copyright file="IFeatureFlagClientService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Service for accessing feature flags in the Blazor client.
/// </summary>
public interface IFeatureFlagClientService
{
    /// <summary>
    /// Gets the current feature flag state as a dictionary.
    /// </summary>
    /// <remarks>
    /// Cached after first load. If flags could not be loaded, returns empty dictionary.
    /// </remarks>
    Dictionary<string, bool> Flags { get; }

    /// <summary>
    /// Checks if a specific feature is enabled.
    /// </summary>
    /// <param name="flagName">The name of the feature flag (case-sensitive, e.g., "Calendar:SpendingHeatmap").</param>
    /// <returns><c>true</c> if the feature is enabled; otherwise, <c>false</c>.</returns>
    bool IsEnabled(string flagName);

    /// <summary>
    /// Loads feature flags from the API. Called once at app startup.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task LoadFlagsAsync();

    /// <summary>
    /// Refreshes feature flags from the API (for use after admin toggle).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task RefreshAsync();
}
```

---

### 3.10 Client: `FeatureFlagClientService.cs` (BudgetExperiment.Client/Services)

```csharp
// <copyright file="FeatureFlagClientService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net.Http.Json;

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Service for accessing feature flags in the Blazor client.
/// Fetches flags from the API at startup and caches them for the session.
/// </summary>
public sealed class FeatureFlagClientService : IFeatureFlagClientService
{
    private readonly HttpClient _httpClient;
    private Dictionary<string, bool> _flags = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureFlagClientService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client for API calls.</param>
    public FeatureFlagClientService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc/>
    public Dictionary<string, bool> Flags => _flags;

    /// <inheritdoc/>
    public bool IsEnabled(string flagName)
    {
        return _flags.TryGetValue(flagName, out var isEnabled) && isEnabled;
    }

    /// <inheritdoc/>
    public async Task LoadFlagsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<Dictionary<string, bool>>("api/v1/features");
            _flags = response ?? new Dictionary<string, bool>();
        }
        catch (HttpRequestException)
        {
            // API unavailable or network failure — default to empty (all flags off, safe fallback)
            _flags = new Dictionary<string, bool>();
        }
    }

    /// <inheritdoc/>
    public async Task RefreshAsync()
    {
        await LoadFlagsAsync();
    }
}
```

**Graceful Degradation:** If the API is unreachable (dev environment, network partition), the client defaults to empty dictionary (all flags off). Completed features that need to remain visible should have alternative checks (e.g., check if nav menu item should be visible based on authentication state, not just feature flags).

---

### 3.11 Client DI Registration (`Client/Program.cs`)

**Add after existing service registrations (around line 141), before localization:**

```csharp
// Feature flags
builder.Services.AddScoped<IFeatureFlagClientService, FeatureFlagClientService>();

// Load feature flags before running the app
var host = builder.Build();
var featureFlagService = host.Services.GetRequiredService<IFeatureFlagClientService>();
await featureFlagService.LoadFlagsAsync();
await host.RunAsync();
```

**Why Scoped?** Flags are session-stable (change requires API toggle + client refresh). Scoped matches the lifetime of `IBudgetApiService`, `IChatApiService`, and other data services. Singleton would prevent future per-user flag overrides (if we ever add them).

**Why Load Before `RunAsync`?** Prevents race conditions where components render before flags are ready. Existing pattern: `ClientConfigDto` is fetched before DI registration in some scenarios (verify current implementation; adjust if needed).

---

## 4. Flag Inventory (17 Flags from Feature 129 Audit)

| Flag Name | Default | Type | Description |
|-----------|---------|------|-------------|
| `Calendar:SpendingHeatmap` | `true` | [user-simplification] | Heatmap overlay on calendar cells showing spending intensity |
| `Calendar:KakeiboOverlay` | `false` | [experimental] | Kakeibo category badges on calendar (Feature 128 rollout) |
| `Kakeibo:MonthlyReflectionPrompts` | `true` | [user-simplification] | Month-end reflection prompts (Kakeibo philosophy) |
| `Kakeibo:CalendarOverlay` | `false` | [experimental] | Duplicate of `Calendar:KakeiboOverlay` — consider consolidating |
| `Kaizen:MicroGoals` | `true` | [user-simplification] | Weekly micro-goal tracking |
| `Kaizen:Dashboard` | `false` | [experimental] | 12-week Kaizen dashboard report (Feature 128 rollout) |
| `AI:ChatAssistant` | `true` | [user-simplification] + [experimental] | Ollama-powered chat assistant |
| `AI:RuleSuggestions` | `true` | [user-simplification] | Ollama-powered categorization rule suggestions |
| `AI:RecurringChargeDetection` | `true` | [user-simplification] | AI-detected recurring transaction patterns |
| `Reports:CustomReportBuilder` | `false` | [experimental] | User-defined custom report builder (power-user feature) |
| `Reports:LocationReport` | `true` | [user-simplification] | Geographic spending report |
| `Charts:AdvancedCharts` | `false` | [experimental] | Candlestick, BoxPlot, SparkLine (showcase only, not in active reports) |
| `Charts:CandlestickChart` | `false` | [experimental] | Individual chart type (consider folding into `Charts:AdvancedCharts`) |
| `Paycheck:PaycheckPlanner` | `true` | [user-simplification] | Paycheck allocation calculator |
| `Reconciliation:StatementReconciliation` | `true` | [user-simplification] | PDF statement reconciliation |
| `DataHealth:Dashboard` | `true` | [user-simplification] | Data hygiene dashboard |
| `Location:Geocoding` | `false` | [experimental] | Geocoding service (stub implementation) |

**Note:** Some flags are redundant (e.g., `Calendar:KakeiboOverlay` vs `Kakeibo:CalendarOverlay`). Alfred may consolidate these during implementation.

---

## 5. Admin UI Sketch (Optional — Phase 2)

**Endpoint:** `PUT /api/v1/features/{flagName}` (implemented in FeaturesController)

**Admin Page:** `/admin/features` or `/settings` (new section)

**UI Mockup:**

```
Feature Flags (Admin)
───────────────────────────────────────────────────
Calendar
  [x] Spending Heatmap               (Calendar:SpendingHeatmap)
  [ ] Kakeibo Overlay                (Calendar:KakeiboOverlay)

Kakeibo
  [x] Monthly Reflection Prompts     (Kakeibo:MonthlyReflectionPrompts)
  [ ] Calendar Overlay               (Kakeibo:CalendarOverlay)

Kaizen
  [x] Micro-Goals                    (Kaizen:MicroGoals)
  [ ] Dashboard                      (Kaizen:Dashboard)

AI
  [x] Chat Assistant                 (AI:ChatAssistant)
  [x] Rule Suggestions               (AI:RuleSuggestions)
  [x] Recurring Charge Detection     (AI:RecurringChargeDetection)

Reports
  [ ] Custom Report Builder          (Reports:CustomReportBuilder)
  [x] Location Report                (Reports:LocationReport)

Charts
  [ ] Advanced Charts                (Charts:AdvancedCharts)
  [ ] Candlestick Chart              (Charts:CandlestickChart)

...
```

**Blazor Component:**

```razor
@page "/admin/features"
@inject IFeatureFlagClientService FeatureFlagService
@inject IBudgetApiService ApiService

<h3>Feature Flags (Admin)</h3>

@foreach (var (name, enabled) in FeatureFlagService.Flags.OrderBy(f => f.Key))
{
    <div class="flag-toggle">
        <input type="checkbox" 
               checked="@enabled" 
               @onchange="@(e => ToggleFlag(name, (bool)e.Value!))" />
        <label>@name</label>
    </div>
}

@code {
    private async Task ToggleFlag(string name, bool enabled)
    {
        // Call PUT /api/v1/features/{name}
        await ApiService.UpdateFeatureFlagAsync(name, enabled);
        
        // Refresh client cache
        await FeatureFlagService.RefreshAsync();
    }
}
```

**Note:** Admin UI is optional for MVP. CLI/curl is sufficient for initial implementation:

```bash
curl -X PUT https://pi.local:5099/api/v1/features/Calendar:KakeiboOverlay \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <admin-token>" \
  -d '{"enabled":true}'
```

---

## 6. Testing Strategy (Barbara's Responsibility)

### Unit Tests

**Domain Layer:**
- `FeatureFlagTests` — verify entity properties (Name, IsEnabled, UpdatedAtUtc).

**Application Layer:**
- `FeatureFlagServiceTests` — verify cache hit/miss logic, `SetFlagAsync` invalidates cache, `IsEnabledAsync` returns correct value.
- Mock `IMemoryCache` and `IFeatureFlagRepository`.

**Infrastructure Layer:**
- `FeatureFlagRepositoryTests` — verify `GetAllAsync`, `GetByNameAsync`, `UpdateAsync` (use in-memory SQLite or Testcontainers PostgreSQL).

**API Layer:**
- `FeaturesControllerTests` — verify `GetFeatures()` returns flags from service, `UpdateFeatureFlag()` requires auth, returns 200 on success, 404 on unknown flag.

**Client Layer:**
- `FeatureFlagClientServiceTests` — verify `LoadFlagsAsync` caches flags, `IsEnabled` logic correct, graceful degradation on HTTP failure (returns empty dictionary).

### Integration Tests

**API:**
- `FeaturesEndpointTests` (using `WebApplicationFactory`) — verify `/api/v1/features` returns JSON from DB, verify `ResponseCache` headers (max-age=60), verify PUT endpoint toggles flag and invalidates cache.

**Client:**
- `FeatureFlagIntegrationTests` (using bUnit + mocked `HttpClient`) — verify service fetches flags from `/api/v1/features`, caches them, and components can query via `IsEnabled`.

### Manual Testing

1. **Seed verification:** Deploy with migration → verify 17 flags seeded in DB with correct defaults.
2. **Toggle via API:** `PUT /api/v1/features/Calendar:KakeiboOverlay` with `{"enabled":true}` → verify DB updated, cache invalidated.
3. **Client refresh:** Blazor client loads → verify flags fetched, wait 60 seconds → verify client-side cache expires → re-fetch picks up new state within ~1 minute.
4. **Graceful degradation:** Stop API, run client standalone → verify empty dictionary fallback (all flags off).

### Performance Tests

**Zero per-request overhead (cache hit path):** Feature flag checks hit `IMemoryCache` only (no DB access after initial load). Add a micro-benchmark if needed:

```csharp
[Benchmark]
public async Task<bool> FeatureFlagService_IsEnabledAsync_CacheHit()
{
    return await _service.IsEnabledAsync("Calendar:SpendingHeatmap");
}
```

Expected: < 1 µs (in-memory dictionary lookup + cache read).
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

## 7. Migration Path: Retrofitting Existing Features

### Problem

We already have production features (AI Chat Assistant, Recurring Charge Detection, Reports). If we add flags and set them to `false` by default, we break existing deployments. If we set them to `true`, the flags are meaningful only for new/unfinished features.

### Solution: Default-On for Completed/Shipped Features

1. **Completed features** (e.g., `AI:ChatAssistant`, `AI:RecurringChargeDetection`, `Reports:LocationReport`, `Paycheck:PaycheckPlanner`) default to `true` in DB seed.
2. **Experimental/unfinished features** (e.g., `Calendar:KakeiboOverlay`, `Kaizen:Dashboard`, `Reports:CustomReportBuilder`) default to `false` in DB seed.
3. Components check `IsEnabled("AI:ChatAssistant")` — flag is `true` by default, so existing behavior unchanged.
4. New features (e.g., Kakeibo overlay) are hidden by default until toggled on via admin API.

### Rollout Steps

1. **Backend:** Create migration for `FeatureFlags` table → seed 17 flags → implement repository + service + controller.
2. **Client:** Add `IFeatureFlagClientService`, load flags at startup.
3. **Existing features:** Wrap navigation links in `@if (FeatureFlagService.IsEnabled("AI:ChatAssistant"))` — no behavior change (flag is `true`).
4. **New features:** Add components behind `@if (FeatureFlagService.IsEnabled("Calendar:KakeiboOverlay"))` — hidden by default.
5. **Activation:** `PUT /api/v1/features/Calendar:KakeiboOverlay` with `{"enabled":true}` → feature appears after client refresh.

**No Breaking Changes:** Defaults align with current production state. Existing features remain visible unless explicitly toggled off.

---

## 8. Future Extensions (NOT IN SCOPE)

### Per-User Feature Flags

**Current:** Flags are instance-level (all users on a deployment see same state).

**If Needed Later:**
- Add `UserFeatureFlagOverrides` table (userId, flagName, enabled).
- Merge global flags + user overrides in `FeaturesController` or service layer.
- Requires authentication context (user ID).

**Cost:** Database queries on every flag check, caching complexity, user-specific invalidation logic. Defer until user-specific rollout is required.

**Note:** User preferences (e.g., "Hide Paycheck Planner in my nav menu") are handled via existing `UserSettings` entity, not feature flags. Feature flags control what's deployed/available; user settings control what's visible per user.

### Microsoft.FeatureManagement Migration

**When to Consider:**
- Targeting (enable for 10% of users, specific roles, specific environments).
- Time windows (enable flag between 2026-04-10 and 2026-04-15).
- External configuration (Azure App Configuration, LaunchDarkly).
- Percentage rollouts (gradual ramp from 0% → 100%).

**Migration Path:**
1. Keep `FeatureFlag` entity and repository.
2. Introduce `Microsoft.FeatureManagement` NuGet package.
3. Implement custom `IFeatureDefinitionProvider` backed by `IFeatureFlagRepository`.
4. Replace `IFeatureFlagService` injection with `IFeatureManager` in controllers.

**Effort:** ~4 hours. Not justified unless we need advanced features (targeting, time windows, external providers).

---

## 9. Decision Rationale Summary

| Question | Decision | Why |
|----------|----------|-----|
| **Library or hand-rolled?** | Hand-rolled DB + cache | Simple on/off switches; no targeting/A/B testing needed; aligns with "no magic" principle; Docker/Pi deployment requires DB-backed runtime toggles. |
| **Storage layer?** | PostgreSQL `FeatureFlags` table | Runtime toggleability without app restarts; file-based config (env vars) doesn't hot-reload in Docker; DB is source of truth across all deployments. |
| **Cache strategy?** | `IMemoryCache` (5-min TTL) | Zero per-request overhead (cache hit = no DB access); invalidate on admin toggle; graceful degradation (cache miss → DB fallback). |
| **Client delivery pattern?** | `/api/v1/features` endpoint fetched at startup | Matches existing `/api/v1/config` pattern (ConfigController); graceful degradation on API failure (empty dictionary = all flags off). |
| **Client cache duration?** | 60 seconds (`ResponseCache`) | Eventual consistency acceptable (1-hour client-side cache is fine per Alfred's decision); reduced from 3600 to allow faster propagation of runtime toggles. |
| **Default-on or default-off?** | Default-off for experimental, default-on for shipped | Prevents breaking existing deployments; allows gradual feature activation; completed features remain visible unless explicitly disabled. |
| **Authentication required?** | GET: No (`[AllowAnonymous]`), PUT: Yes (`[Authorize]`) | Client needs flags before authentication to decide UI state. No sensitive data exposed (flags are deployment config, not user secrets). Admin toggle requires auth. |
| **Flag naming convention?** | Hierarchical colon-separated (e.g., `Calendar:SpendingHeatmap`) | Matches Feature 129 audit inventory; groups related flags; extensible to nested categories. |

---

## 10. Open Questions for Alfred (ANSWERED)

**All questions deferred to Alfred's decision in `.squad/decisions/inbox/alfred-runtime-feature-flags.md` (approved Option B).**

1. **Runtime toggleability?** ✅ YES — DB-backed with cache.
2. **Flag naming?** ✅ Hierarchical colon-separated (`Calendar:SpendingHeatmap`).
3. **Blazor component pattern?** ✅ Inline `@if (FeatureFlagService.IsEnabled("..."))` (matches existing patterns).
4. **Backend gating?** ✅ Client-only for MVP (API endpoints always functional; simplifies backend logic).

---

## 11. Implementation Checklist (For Lucius)

- [ ] Create `FeatureFlag.cs` entity in Domain
- [ ] Create `FeatureFlagConfiguration.cs` in Infrastructure with seed data (17 flags from Feature 129 audit)
- [ ] Add `DbSet<FeatureFlag>` to `AppDbContext`
- [ ] Generate and apply migration for `FeatureFlags` table
- [ ] Create `IFeatureFlagRepository` interface in Application
- [ ] Implement `FeatureFlagRepository` in Infrastructure
- [ ] Create `IFeatureFlagService` interface in Application
- [ ] Implement `FeatureFlagService` with `IMemoryCache` in Application
- [ ] Register `IMemoryCache` in Application `DependencyInjection.cs` (if not already registered)
- [ ] Register `IFeatureFlagRepository` + `FeatureFlagRepository` in Infrastructure `DependencyInjection.cs`
- [ ] Register `IFeatureFlagService` + `FeatureFlagService` in Application `DependencyInjection.cs`
- [ ] Create `FeaturesController.cs` with GET and PUT endpoints in API
- [ ] Add `UpdateFeatureFlagRequest` and `UpdateFeatureFlagResponse` DTOs in Controller file (or Contracts if shared)
- [ ] Create `IFeatureFlagClientService` interface in Client
- [ ] Implement `FeatureFlagClientService` in Client
- [ ] Register `IFeatureFlagClientService` in Client `Program.cs` (scoped)
- [ ] Load flags at startup in Client `Program.cs` (before `RunAsync()`)
- [ ] Barbara: Write unit tests for `FeatureFlagService` (cache behavior)
- [ ] Barbara: Write unit tests for `FeatureFlagRepository` (CRUD operations)
- [ ] Barbara: Write unit tests for `FeaturesController` (GET/PUT endpoints)
- [ ] Barbara: Write unit tests for `FeatureFlagClientService` (graceful degradation)
- [ ] Barbara: Write integration test for `/api/v1/features` endpoint (DB → API → response)
- [ ] Barbara: Write integration test for PUT endpoint (toggle → cache invalidation → verify)
- [ ] Update `.env.example` (Docker deployment) with flag toggle curl example (optional)
- [ ] Update `CHANGELOG.md` with Feature 129b (Runtime Feature Flags)

---

## 12. Estimated Effort

- **Lucius (Implementation):** 6 hours (entity, migration, repository, service, controller, client service, DI wiring)
- **Barbara (Tests):** 6 hours (unit tests for all layers + integration tests for API + Client)
- **Alfred (Review & Integration):** 1 hour (ensure flag inventory matches Feature 129 audit, verify cache strategy)
- **Total:** ~13 hours

---

## Conclusion

This database-backed + cache approach delivers runtime-toggleable feature flags with zero per-request performance overhead, zero external dependencies, and full compatibility with all deployment contexts (local dev, Docker, Raspberry Pi). It's trivial to extend (add a row to `FeatureFlags` table via migration), trivial to toggle (admin API call), and trivial to test (mock `IMemoryCache` + `IFeatureFlagRepository`). If we need advanced features (targeting, percentage rollouts, external providers), we can migrate to `Microsoft.FeatureManagement` in ~4 hours by implementing a custom `IFeatureDefinitionProvider` backed by our existing repository. Until then, YAGNI.

**Next Step:** Lucius implements per checklist (§11). Barbara writes tests. Alfred reviews flag inventory alignment with Feature 129 audit.
