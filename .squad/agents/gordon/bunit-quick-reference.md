# bUnit Quick Reference

**Quick patterns for Phase 3 Tier 3 client testing.**

## Setup Template

```csharp
using System.Globalization;
using Bunit;
using BudgetExperiment.Client.TestHelpers;
using BudgetExperiment.Client.Services;
using Xunit;
using Shouldly;

namespace BudgetExperiment.Client.Tests.YourNamespace;

public class YourComponentTests : BunitContext, IAsyncLifetime
{
    private StubBudgetApiService _fakeApiService = null!;
    private StubFeatureFlagClientService _fakeFeatureFlags = null!;

    public YourComponentTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        
        _fakeApiService = new StubBudgetApiService();
        _fakeFeatureFlags = new StubFeatureFlagClientService();
        
        this.Services.AddSingleton(_fakeApiService);
        this.Services.AddSingleton<IBudgetApiService>(_fakeApiService);
        this.Services.AddSingleton(_fakeFeatureFlags);
        this.Services.AddSingleton<IFeatureFlagClientService>(_fakeFeatureFlags);
        this.Services.AddSingleton<ThemeService>();
        this.Services.AddSingleton<CultureService>();
    }

    public Task InitializeAsync()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public void Test_SomeBehavior()
    {
        // Arrange

        // Act

        // Assert
    }
}
```

## Feature Flags

```csharp
// Enable
_fakeFeatureFlags.Flags["Kaizen:Dashboard"] = true;

// Disable
_fakeFeatureFlags.Flags["Kaizen:Dashboard"] = false;

// In test
public async Task FeatureFlagDisabled_HidesComponent()
{
    _fakeFeatureFlags.Flags["Kaizen:Dashboard"] = false;
    var cut = this.Render<KaizenDashboardView>();
    cut.FindAll(".kaizen-dashboard").ShouldBeEmpty();
}
```

## Rendering with Parameters

```csharp
var cut = this.Render<YourComponent>(parameters => parameters
    .Add(p => p.PropertyName, value)
    .Add(p => p.AnotherProperty, value2));

// Common assertions
cut.FindAll(".selector").ShouldNotBeEmpty();
Assert.NotNull(cut.Find(".required-element"));
```

## Async Control (TaskCompletionSource)

```csharp
public async Task LoadingState_DisplaysWhileDataFetching()
{
    var taskSource = new TaskCompletionSource<DataDto?>();
    _fakeApiService.KaizenDashboardTaskSource = taskSource;
    
    var cut = this.Render<KaizenDashboardView>();
    
    // Loading spinner visible immediately
    cut.FindAll(".spinner").Count.ShouldBe(1);
    
    // Complete async operation
    taskSource.SetResult(new DataDto 
    { 
        Weeks = new[] { /* data */ }
    });
    
    // Wait for re-render
    cut.WaitForState(() => cut.FindAll(".spinner").Count == 0, TimeSpan.FromSeconds(2));
    
    // Data now visible
    cut.FindAll("[data-testid='data-table']").ShouldNotBeEmpty();
}
```

## Error Handling

```csharp
public async Task ApiError_DisplaysErrorMessage()
{
    var taskSource = new TaskCompletionSource<DataDto?>();
    _fakeApiService.KaizenDashboardTaskSource = taskSource;
    
    var cut = this.Render<KaizenDashboardView>();
    
    taskSource.SetException(new InvalidOperationException("API failed"));
    
    cut.WaitForState(() => cut.FindAll(".error-message").Any(), TimeSpan.FromSeconds(1));
    
    cut.FindAll(".error-message").Count.ShouldBe(1);
}
```

## Form Input & Submission

```csharp
public async Task GoalSubmission_WithValidAmount_SubmitsSuccessfully()
{
    var cut = this.Render<MonthIntentionPrompt>(parameters => parameters
        .Add(p => p.Year, 2026)
        .Add(p => p.Month, 1));
    
    // Set goal amount
    var goalInput = cut.Find("input[type='number']");
    goalInput.Change("150.50");
    
    // Set intention text
    var intentionInput = cut.Find("textarea");
    intentionInput.Change("Save for emergency fund");
    
    // Submit
    var submitBtn = cut.Find("button:contains('Set Goal')");
    submitBtn.Click();
    
    // Verify API was called with correct values
    // (Check stub's received data or verify success state)
}
```

## Character Counter & Validation

```csharp
public void IntentionInput_WithMaxLength_EnforcesLimit()
{
    var cut = this.Render<MonthIntentionPrompt>();
    var textarea = cut.Find("textarea");
    
    var longText = new string('a', 300);
    textarea.Change(longText);
    
    // Component should truncate or block at 255
    textarea.GetAttribute("value")!.Length.ShouldBeLessThanOrEqualTo(255);
}

public void IntentionInput_DisplaysCharacterCount()
{
    var cut = this.Render<MonthIntentionPrompt>();
    var textarea = cut.Find("textarea");
    
    textarea.Change("Hello");
    
    var counter = cut.Find(".char-count");
    counter.TextContent.ShouldContain("5");
}
```

## Whitespace Normalization

```csharp
public async Task Intention_WithOnlyWhitespace_ConvertsToNull()
{
    var mockTaskSource = new TaskCompletionSource<MonthlyReflectionDto?>();
    _fakeApiService.CreateOrUpdateReflectionTaskSource = mockTaskSource;
    
    var cut = this.Render<MonthIntentionPrompt>();
    
    var goalInput = cut.Find("input[type='number']");
    goalInput.Change("150.50");
    
    var intentionInput = cut.Find("textarea");
    intentionInput.Change("   \n   ");  // Only whitespace
    
    var submitBtn = cut.Find("button:contains('Set Goal')");
    submitBtn.Click();
    
    mockTaskSource.SetResult(new MonthlyReflectionDto { /* ... */ });
    
    // Verify intention was nullified (not submitted as whitespace)
}
```

## Waiting for Async Completion

```csharp
// Wait for condition (up to 2 seconds)
cut.WaitForState(() => cut.FindAll(".spinner").Count == 0, TimeSpan.FromSeconds(2));

// Alternative: check render and wait by component
var cut = Render<Component>();
// ... trigger async operation
cut.WaitForState(() => !cut.Instance.IsLoading);
```

## Testing Disabled States

```csharp
public async Task SubmitButton_DisabledDuringSubmission()
{
    var taskSource = new TaskCompletionSource<MonthlyReflectionDto?>();
    _fakeApiService.CreateOrUpdateReflectionTaskSource = taskSource;
    
    var cut = this.Render<MonthIntentionPrompt>();
    
    // Fill and submit
    var goalInput = cut.Find("input[type='number']");
    goalInput.Change("150.50");
    var submitBtn = cut.Find("button");
    submitBtn.Click();
    
    // Button should be disabled and show "Saving…"
    cut.FindAll("button[disabled]").Count.ShouldBeGreaterThan(0);
    cut.Find(".submit-button").TextContent.ShouldContain("Saving…");
    
    // Complete submission
    taskSource.SetResult(new MonthlyReflectionDto { /* ... */ });
    cut.WaitForState(() => cut.FindAll("button[disabled]").Count == 0);
}
```

## Finding Elements

```csharp
// By CSS selector
cut.Find(".class-name");
cut.Find("#element-id");
cut.Find("button[type='submit']");
cut.Find("input[type='number']");

// Multiple elements
cut.FindAll(".item");
cut.FindAll("button");

// Contains text (Razor syntax varies, may need manual Find + filter)
var btn = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Save"));
```

## Assertions (Shouldly)

```csharp
// Existence
element.ShouldNotBeNull();
collection.ShouldNotBeEmpty();

// Equality
value.ShouldBe(expected);
text.ShouldContain("substring");
list.Count.ShouldBe(5);

// Collections
collection.ShouldBeEmpty();
collection.ShouldContain(item);

// Conditionals
cut.FindAll(".selector").Count.ShouldBeGreaterThan(0);
```

---

**See also:** `KaizenDashboardViewTests.cs`, `MonthIntentionPromptTests.cs`, `CalendarBudgetIntegrationTests.cs` for full examples.
