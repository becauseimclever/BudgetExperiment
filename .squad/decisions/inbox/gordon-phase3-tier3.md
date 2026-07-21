# Gordon's Phase 3 Tier 3 Decision Record

**Date:** 2026-01-09  
**Agent:** Gordon (Backend Dev)  
**Scope:** bUnit test implementation for analytics + form components

## Decision: bUnit Test Patterns for Blazor Components

### Context

Phase 3 Tier 3 required 14-21 new bUnit tests for KaizenDashboardView, MonthIntentionPrompt, and cross-component workflows. Barbara's phase3-test-scenarios.md provided detailed test scenarios but no bUnit patterns were established yet.

### Decision Made

**Implemented 35 tests** (exceeds target 14-21) using the following established patterns:

#### 1. BunitContext Base Setup

```csharp
public class ComponentTests : BunitContext, IAsyncLifetime
{
    public ComponentTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<ThemeService>();
        this.Services.AddSingleton<CultureService>();
        // ... other services
    }
    
    public Task InitializeAsync()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
        return Task.CompletedTask;
    }
}
```

**Rationale:**
- `JSRuntimeMode.Loose` prevents JS interop exceptions in unit tests
- Culture setup (en-US) ensures deterministic string/currency formatting
- IAsyncLifetime enables async setup/teardown

#### 2. Feature Flag Testing

```csharp
_fakeFeatureFlags.Flags["Kaizen:Dashboard"] = true;  // Enable
_fakeFeatureFlags.Flags["Kaizen:Dashboard"] = false; // Disable
```

**NOT:**
```csharp
_fakeFeatureFlags.EnableFeature("Kaizen:Dashboard");  // Method doesn't exist
```

**Rationale:**
- StubFeatureFlagClientService exposes a Dictionary<string, bool> Flags property
- Direct dictionary access is the intended API (no helper methods)

#### 3. TaskCompletionSource for Async API Stubs

```csharp
_fakeApiService.KaizenDashboardTaskSource = new TaskCompletionSource<KaizenDashboardDto?>();
_fakeApiService.KaizenDashboardTaskSource.SetResult(data);  // Success
_fakeApiService.KaizenDashboardTaskSource.SetException(ex);  // Error
```

**Rationale:**
- Enables tests to control async completion timing
- Allows testing loading states (`WaitForState()` blocking until result arrives)
- Supports error scenario testing

#### 4. Component Rendering & Interaction

```csharp
var cut = Render<MonthIntentionPrompt>(parameters => parameters
    .Add(p => p.Year, 2026)
    .Add(p => p.Month, 1)
    .Add(p => p.PreviousMonthGoal, 150.50m));

var input = cut.Find("input[type='number']");
input.Change("250.50");

var buttons = cut.FindAll("button");
Assert.Contains(buttons, b => b.TextContent.Contains("Set Goal"));
```

**Rationale:**
- `Render<T>(parameters => ...)` is the bUnit pattern for component instantiation
- `.Find()` and `.FindAll()` use CSS selectors
- `.Change()` simulates user input
- Assertions use standard xUnit + Shouldly patterns

#### 5. Async State Testing

```csharp
cut.WaitForState(() => !cut.FindAll(".intention-prompt").Any(), TimeSpan.FromSeconds(2));
Assert.Empty(cut.FindAll(".intention-prompt"));
```

**Rationale:**
- `WaitForState()` polls until condition is true or timeout
- Allows testing of components that hide after async operations complete
- 2-second timeout balances test speed vs. reliability

### Component Test Coverage

| Component | Tests | Areas Covered |
|-----------|-------|---------------|
| KaizenDashboardView | 12 | Loading, feature flags, empty state, errors, data display, goal badges |
| MonthIntentionPrompt | 15 | Rendering, validation, form submission, async states, error recovery |
| Calendar Integration | 8 | Cross-component workflows, DI context, multi-instance rendering |

### Helper Modifications

Added to `StubBudgetApiService`:

```csharp
public TaskCompletionSource<KaizenDashboardDto?>? KaizenDashboardTaskSource { get; set; }
public TaskCompletionSource<MonthlyReflectionDto?>? CreateOrUpdateReflectionTaskSource { get; set; }
```

Methods now check for TaskSource before returning defaults:

```csharp
public virtual Task<KaizenDashboardDto?> GetKaizenDashboardAsync(...)
    => this.KaizenDashboardTaskSource?.Task ?? Task.FromResult<KaizenDashboardDto?>(null);
```

**Rationale:**
- Maintains backward compatibility (falls back to null when TaskSource not set)
- Enables tests to control async timing without modifying test infrastructure

### Out of Scope / Deferred

1. **FormStateService integration tests** — JSRuntime instantiation not available in unit context; FormState lifecycle best tested via integration/e2e.
2. **TransactionTable bulk operations** — Complex parameter binding for table rendering; recommend separate focused PR.
3. **EventCallback-based tests** — Simplified CalendarBudgetIntegration to use basic component properties only.

### Future Recommendations

1. **bUnit Patterns Codification** — Document these patterns in `/docs/testing-guide-bunit.md` for future contributors.
2. **Async Test Utilities** — Consider a `WaitForCondition()` helper to reduce timeout boilerplate.
3. **FormStateService Testing** — Create integration tests using Playwright or similar that can instantiate real JSRuntime.

---

**Approved by:** Gordon  
**Status:** Complete  
**Artifacts:** KaizenDashboardViewTests.cs, MonthIntentionPromptTests.cs, CalendarBudgetIntegrationTests.cs
