# Feature 036: Demo Environment E2E Tests
> **Status:** 🗒️ Planning

## Quick Reference

| Item | Value |
|------|-------|
| **Project Location** | `tests/BudgetExperiment.E2E.Tests/` |
| **Target URL** | `https://budgetdemo.becauseimclever.com` |
| **Test Credentials** | `test` / `demo123` |
| **Run Tests** | `dotnet test tests/BudgetExperiment.E2E.Tests` |
| **Headed Mode** | `$env:HEADED="true"` |
| **Test Framework** | Playwright for .NET + xUnit |

## Overview

Create a Playwright-based E2E test suite that validates the Budget Experiment application against the demo environment at `budgetdemo.becauseimclever.com`. This enables continuous validation of the production-like demo deployment. Tests will authenticate using dedicated test credentials (`test/demo123`) and verify critical user journeys.

The E2E test suite will serve as a safety net for the demo environment, catching regressions in authentication flows, page accessibility, navigation, and core user journeys before they impact users exploring the application.

## Problem Statement

### Current State

- No E2E test project exists (previous implementation was removed)
- No automated validation of the demo environment
- Demo environment issues may go undetected until manual verification
- Need a clean, well-structured E2E testing foundation
- Currently relying on manual testing to verify deployment success
- No visibility into demo environment health between deployments

### Target State

- New Playwright E2E test project with proper architecture
- Tests run against `budgetdemo.becauseimclever.com` by default
- Option to run against localhost for local development testing
- Demo environment health continuously validated via scheduled CI runs
- Clear test organization with categories for different test types
- Test results published as artifacts for debugging failures
- Foundation for future visual regression and data validation tests

---

## User Stories

### Project Setup

#### US-036-001: Create E2E Test Project
**As a** developer  
**I want** a new Playwright E2E test project  
**So that** I can write and run browser-based integration tests

**Acceptance Criteria:**
- [ ] New `BudgetExperiment.E2E.Tests` project created under `tests/`
- [ ] Uses Playwright for .NET with xUnit
- [ ] Follows project conventions (StyleCop, nullable reference types)
- [ ] References necessary packages (Microsoft.Playwright, xUnit)

#### US-036-002: Test Fixture with Environment Configuration
**As a** developer  
**I want** a configurable test fixture  
**So that** tests can run against demo or local environments

**Acceptance Criteria:**
- [ ] `BUDGET_APP_URL` environment variable configures target (default: demo URL)
- [ ] `HEADED` environment variable enables visible browser mode
- [ ] Fixture handles browser lifecycle (create/dispose)
- [ ] Server reachability validated before test execution

### Authentication

#### US-036-003: Authentication Helper
**As a** developer  
**I want** an authentication helper that logs into the application  
**So that** tests can access protected pages

**Acceptance Criteria:**
- [ ] Helper logs in with `test/demo123` credentials
- [ ] Handles Authentik OAuth flow
- [ ] Waits for successful redirect to application
- [ ] Reusable across all test classes

### Test Suites

#### US-036-004: Smoke Tests
**As a** developer  
**I want** basic smoke tests  
**So that** I can quickly verify the application is functioning

**Acceptance Criteria:**
- [ ] Test that home page loads
- [ ] Test that login succeeds
- [ ] Test that main navigation renders
- [ ] Tests are fast and reliable

#### US-036-005: Navigation Tests
**As a** developer  
**I want** navigation tests  
**So that** I can verify all main pages are accessible

**Acceptance Criteria:**
- [ ] Test Calendar (home) page loads
- [ ] Test Recurring Bills page loads
- [ ] Test Auto-Transfers page loads
- [ ] Test Reconciliation page loads
- [ ] Test Transfers page loads
- [ ] Test Paycheck Planner page loads
- [ ] Test Categories page loads
- [ ] Test Auto-Categorize (Rules) page loads
- [ ] Test Budget page loads
- [ ] Test Reports Overview page loads
- [ ] Test Category Spending Report page loads
- [ ] Test Accounts page loads
- [ ] Test Import page loads
- [ ] Test Smart Insights (AI Suggestions) page loads
- [ ] Test Category Suggestions page loads
- [ ] Test Settings page loads

#### US-036-006: Demo-Safe Data Tests
**As a** developer  
**I want** read-only data verification tests  
**So that** I can validate data displays correctly without modifying demo data

**Acceptance Criteria:**
- [ ] Tests read and verify existing data (accounts, transactions)
- [ ] No tests create, update, or delete shared demo data
- [ ] Tests marked with `[Trait("Category", "DemoSafe")]`
- [ ] Verify account list displays on Accounts page
- [ ] Verify calendar shows transaction data
- [ ] Verify categories are displayed on Categories page

#### US-036-007: Critical User Journey Tests
**As a** developer  
**I want** tests that verify critical user journeys  
**So that** I can ensure the most important workflows are functioning

**Acceptance Criteria:**
- [ ] Test: Navigate to account → view transactions
- [ ] Test: Open calendar → click on a day → view transactions for that day
- [ ] Test: Navigate to Reports → verify charts render
- [ ] All tests marked with `[Trait("Category", "UserJourney")]`

#### US-036-008: CI/CD Integration
**As a** developer  
**I want** E2E tests to run automatically in CI/CD  
**So that** demo environment health is continuously validated

**Acceptance Criteria:**
- [ ] GitHub Actions workflow for E2E tests
- [ ] Tests run on schedule (e.g., nightly or after deployment)
- [ ] Test results and traces published as artifacts
- [ ] Failure notifications (optional: Slack/email integration)

---

## Technical Design

### Project Structure

```
tests/
└── BudgetExperiment.E2E.Tests/
    ├── BudgetExperiment.E2E.Tests.csproj
    ├── GlobalUsings.cs
    ├── playwright.ps1               # Browser installation script (generated)
    ├── Fixtures/
    │   └── PlaywrightFixture.cs
    ├── Helpers/
    │   ├── AuthenticationHelper.cs
    │   └── NavigationHelper.cs
    ├── Tests/
    │   ├── SmokeTests.cs
    │   ├── NavigationTests.cs
    │   ├── AccountsTests.cs
    │   ├── CalendarTests.cs
    │   └── ReportsTests.cs
    └── README.md
```

### Package Dependencies

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Playwright" Version="1.49.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.0.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector" Version="6.0.4">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
    <Using Include="Microsoft.Playwright" />
  </ItemGroup>

</Project>
```

### GlobalUsings.cs

```csharp
global using Xunit;
global using Microsoft.Playwright;
global using static Microsoft.Playwright.Assertions;
```

### PlaywrightFixture Design

```csharp
public class PlaywrightFixture : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    
    public IBrowserContext Context { get; private set; } = null!;
    public IPage Page { get; private set; } = null!;
    
    public string BaseUrl { get; } = Environment.GetEnvironmentVariable("BUDGET_APP_URL") 
        ?? "https://budgetdemo.becauseimclever.com";
    
    public bool Headed { get; } = Environment.GetEnvironmentVariable("HEADED")
        ?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = !Headed });
        Context = await _browser.NewContextAsync();
        Page = await Context.NewPageAsync();
        
        // Verify server is reachable
        await ValidateServerAsync();
    }

    public async Task DisposeAsync()
    {
        await Context.DisposeAsync();
        await _browser!.DisposeAsync();
        _playwright!.Dispose();
    }
    
    private async Task ValidateServerAsync()
    {
        var response = await Page.GotoAsync(BaseUrl);
        if (response?.Status >= 400)
        {
            throw new InvalidOperationException($"Server at {BaseUrl} returned {response.Status}");
        }
    }
}
```

### AuthenticationHelper Design

```csharp
public static class AuthenticationHelper
{
    private const string TestUsername = "test";
    private const string TestPassword = "demo123";

    public static async Task LoginAsync(IPage page, string baseUrl)
    {
        await page.GotoAsync(baseUrl);
        
        // Click login button if present
        var loginButton = page.GetByRole(AriaRole.Link, new() { Name = "Login" });
        if (await loginButton.IsVisibleAsync())
        {
            await loginButton.ClickAsync();
        }
        
        // Fill Authentik login form
        await page.GetByLabel("Username or Email").FillAsync(TestUsername);
        await page.GetByLabel("Password").FillAsync(TestPassword);
        await page.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();
        
        // Wait for redirect back to app
        await page.WaitForURLAsync(url => url.StartsWith(baseUrl) && !url.Contains("authentik"));
    }
}
```

### Environment Variables

| Variable | Purpose | Default |
|----------|---------|---------|
| `BUDGET_APP_URL` | Target application URL | `https://budgetdemo.becauseimclever.com` |
| `HEADED` | Run with visible browser | `false` |
| `SLOWMO` | Slow down operations (ms) for debugging | `0` |
| `PLAYWRIGHT_TRACES` | Enable trace capture for debugging | `false` |

### Application Pages & Routes

The following pages will be tested (derived from NavMenu.razor):

| Route | Page Name | Category |
|-------|-----------|----------|
| `/` | Calendar (Home) | Primary |
| `/recurring` | Recurring Bills | Primary |
| `/recurring-transfers` | Auto-Transfers | Primary |
| `/reconciliation` | Reconciliation | Primary |
| `/transfers` | Transfers | Primary |
| `/paycheck-planner` | Paycheck Planner | Primary |
| `/categories` | Budget Categories | Primary |
| `/rules` | Auto-Categorize | Primary |
| `/budget` | Budget Overview | Primary |
| `/reports` | Reports Overview | Secondary |
| `/reports/categories` | Category Spending | Secondary |
| `/accounts` | All Accounts | Secondary |
| `/accounts/{id}/transactions` | Account Transactions | Dynamic |
| `/import` | Import Transactions | Utility |
| `/ai/suggestions` | Smart Insights | AI Features |
| `/category-suggestions` | Category Suggestions | AI Features |
| `/settings` | Settings | Utility |

### Navigation Helper Design

```csharp
public static class NavigationHelper
{
    /// <summary>
    /// Primary navigation routes that appear in the main nav menu.
    /// </summary>
    public static readonly IReadOnlyList<(string Route, string Name)> PrimaryRoutes = new[]
    {
        ("", "Calendar"),
        ("recurring", "Recurring Bills"),
        ("recurring-transfers", "Auto-Transfers"),
        ("reconciliation", "Reconciliation"),
        ("transfers", "Transfers"),
        ("paycheck-planner", "Paycheck Planner"),
        ("categories", "Budget Categories"),
        ("rules", "Auto-Categorize"),
        ("budget", "Budget Overview"),
    };

    /// <summary>
    /// Secondary routes that may require expanding sections.
    /// </summary>
    public static readonly IReadOnlyList<(string Route, string Name)> SecondaryRoutes = new[]
    {
        ("reports", "Reports Overview"),
        ("reports/categories", "Category Spending"),
        ("accounts", "All Accounts"),
        ("import", "Import Transactions"),
        ("ai/suggestions", "Smart Insights"),
        ("category-suggestions", "Category Suggestions"),
        ("settings", "Settings"),
    };
}

---

## Implementation Plan

### Phase 1: Project Setup

**Objective:** Create the E2E test project with proper structure

**Tasks:**
- [ ] Create new xUnit test project
- [ ] Add Playwright NuGet packages
- [ ] Configure project settings (StyleCop, nullable)
- [ ] Create `GlobalUsings.cs`
- [ ] Create folder structure

**Commit:**
```
feat(e2e): create Playwright E2E test project

- New BudgetExperiment.E2E.Tests project
- Playwright for .NET integration
- Project structure and configuration

Refs: #036
```

### Phase 2: Test Infrastructure

**Objective:** Create fixture and authentication helper

**Tasks:**
- [ ] Implement `PlaywrightFixture`
- [ ] Implement `AuthenticationHelper`
- [ ] Add Playwright browser installation script
- [ ] Verify fixture works against demo

**Commit:**
```
feat(e2e): add PlaywrightFixture and AuthenticationHelper

- Configurable fixture with environment variable support
- Authentication helper for Authentik login flow
- Browser lifecycle management

Refs: #036
```

### Phase 3: Smoke Tests

**Objective:** Create initial smoke test suite

**Tasks:**
- [ ] Create `SmokeTests.cs`
- [ ] Test home page loads
- [ ] Test login succeeds
- [ ] Test navigation renders

**Commit:**
```
test(e2e): add smoke tests for basic app validation

- Verify home page accessibility
- Verify login flow works
- Verify main navigation renders

Refs: #036
```

### Phase 4: Navigation Tests

**Objective:** Verify all main pages are accessible

**Tasks:**
- [ ] Create `NavigationTests.cs`
- [ ] Create `NavigationHelper.cs` with route constants
- [ ] Test each primary navigation route
- [ ] Test secondary/expandable section routes
- [ ] Verify page content loads (no error states)
- [ ] Add appropriate test traits for filtering

**Commit:**
```
test(e2e): add navigation tests for all main pages

- Test all 17 application routes
- NavigationHelper with route constants
- Verify each page loads successfully

Refs: #036
```

### Phase 5: User Journey Tests

**Objective:** Verify critical user workflows

**Tasks:**
- [ ] Create `CalendarTests.cs` for calendar interactions
- [ ] Create `AccountsTests.cs` for account navigation
- [ ] Create `ReportsTests.cs` for report rendering
- [ ] Test account → transactions flow
- [ ] Test calendar day selection
- [ ] Test reports with chart verification

**Commit:**
```
test(e2e): add critical user journey tests

- Calendar interaction tests
- Account navigation tests
- Report rendering verification

Refs: #036
```

### Phase 6: CI/CD Integration

**Objective:** Automate E2E tests in GitHub Actions

**Tasks:**
- [ ] Create `.github/workflows/e2e-tests.yml`
- [ ] Configure Playwright browser installation in CI
- [ ] Set up test result artifact publishing
- [ ] Configure scheduled nightly runs
- [ ] Add manual trigger option for on-demand testing

**Commit:**
```
ci(e2e): add GitHub Actions workflow for E2E tests

- Nightly scheduled runs against demo environment
- Playwright trace artifacts on failure
- Manual workflow dispatch option

Refs: #036
```

### Phase 7: Documentation

**Objective:** Document how to run E2E tests

**Tasks:**
- [ ] Create project README.md
- [ ] Add quick-start commands
- [ ] Document environment variables
- [ ] Add troubleshooting section
- [ ] Document CI/CD integration

**Commit:**
```
docs(e2e): add E2E test documentation

- Project README with quick-start guide
- Environment variable documentation
- Troubleshooting section

Refs: #036
```

---

## GitHub Actions Workflow

Create `.github/workflows/e2e-tests.yml`:

```yaml
name: E2E Tests

on:
  # Run nightly at 2 AM UTC
  schedule:
    - cron: '0 2 * * *'
  
  # Run on manual trigger
  workflow_dispatch:
    inputs:
      target_url:
        description: 'Target URL to test'
        required: false
        default: 'https://budgetdemo.becauseimclever.com'
  
  # Optional: Run after successful deployment
  # workflow_run:
  #   workflows: ["Docker Build & Publish"]
  #   types: [completed]
  #   branches: [main]

env:
  BUDGET_APP_URL: ${{ github.event.inputs.target_url || 'https://budgetdemo.becauseimclever.com' }}

jobs:
  e2e-tests:
    runs-on: ubuntu-latest
    timeout-minutes: 30
    
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      
      - name: Restore dependencies
        run: dotnet restore tests/BudgetExperiment.E2E.Tests
      
      - name: Build
        run: dotnet build tests/BudgetExperiment.E2E.Tests --no-restore
      
      - name: Install Playwright browsers
        run: pwsh tests/BudgetExperiment.E2E.Tests/bin/Debug/net10.0/playwright.ps1 install --with-deps
      
      - name: Run E2E tests
        run: dotnet test tests/BudgetExperiment.E2E.Tests --no-build --logger "trx;LogFileName=test-results.trx"
        env:
          BUDGET_APP_URL: ${{ env.BUDGET_APP_URL }}
          PLAYWRIGHT_TRACES: true
      
      - name: Upload test results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: e2e-test-results
          path: |
            tests/BudgetExperiment.E2E.Tests/TestResults/
            tests/BudgetExperiment.E2E.Tests/playwright-traces/
          retention-days: 14
      
      - name: Publish test results
        uses: dorny/test-reporter@v1
        if: always()
        with:
          name: E2E Test Results
          path: tests/BudgetExperiment.E2E.Tests/TestResults/*.trx
          reporter: dotnet-trx
```

---

## Running Tests

### Prerequisites

1. Ensure `budgetdemo.becauseimclever.com` is accessible
2. Install Playwright browsers (first time only):
   ```powershell
   pwsh tests/BudgetExperiment.E2E.Tests/bin/Debug/net10.0/playwright.ps1 install
   ```

### Commands

```powershell
# Run all E2E tests against demo (default)
dotnet test c:\ws\BudgetExperiment\tests\BudgetExperiment.E2E.Tests

# Run with visible browser for debugging
$env:HEADED = "true"
dotnet test c:\ws\BudgetExperiment\tests\BudgetExperiment.E2E.Tests

# Run against local development server
$env:BUDGET_APP_URL = "http://localhost:5099"
dotnet test c:\ws\BudgetExperiment\tests\BudgetExperiment.E2E.Tests

# Run only smoke tests
dotnet test c:\ws\BudgetExperiment\tests\BudgetExperiment.E2E.Tests --filter "FullyQualifiedName~SmokeTests"

# Run by category trait
dotnet test c:\ws\BudgetExperiment\tests\BudgetExperiment.E2E.Tests --filter "Category=Smoke"
dotnet test c:\ws\BudgetExperiment\tests\BudgetExperiment.E2E.Tests --filter "Category=Navigation"
dotnet test c:\ws\BudgetExperiment\tests\BudgetExperiment.E2E.Tests --filter "Category=UserJourney"

# Run with slow motion for debugging (1 second delay between actions)
$env:SLOWMO = "1000"
$env:HEADED = "true"
dotnet test c:\ws\BudgetExperiment\tests\BudgetExperiment.E2E.Tests --filter "Category=Smoke"

# Enable trace capture
$env:PLAYWRIGHT_TRACES = "true"
dotnet test c:\ws\BudgetExperiment\tests\BudgetExperiment.E2E.Tests
```

### Linux/macOS/CI

```bash
# Run tests against demo
dotnet test tests/BudgetExperiment.E2E.Tests

# With headed mode
HEADED=true dotnet test tests/BudgetExperiment.E2E.Tests

# Against local
BUDGET_APP_URL=http://localhost:5099 dotnet test tests/BudgetExperiment.E2E.Tests

# Run by category
dotnet test tests/BudgetExperiment.E2E.Tests --filter "Category=Smoke"

# Combine environment variables
HEADED=true SLOWMO=500 dotnet test tests/BudgetExperiment.E2E.Tests --filter "Category=Smoke"
```

---

## Demo Environment Details

| Property | Value |
|----------|-------|
| URL | `https://budgetdemo.becauseimclever.com` |
| Test Username | `test` |
| Test Password | `demo123` |
| Auth Provider | Authentik |
| Test User Purpose | Dedicated account for automated E2E testing |

---

## Testing Strategy

### Test Categories

Tests should be organized using xUnit traits for filtering:

| Trait | Value | Purpose |
|-------|-------|---------|
| `Category` | `Smoke` | Quick validation that app is running |
| `Category` | `Navigation` | Page accessibility tests |
| `Category` | `UserJourney` | Critical workflow tests |
| `Category` | `DemoSafe` | Read-only tests safe for shared demo |
| `Category` | `DataTest` | Tests that verify data display |

### Test Execution Order

1. **Smoke Tests** - Run first, fast-fail if basic functionality broken
2. **Navigation Tests** - Verify all pages accessible
3. **User Journey Tests** - Validate critical workflows
4. **Data Tests** - Verify data displays correctly

### Example Test Class Structure

```csharp
[Collection("Playwright")]
public class SmokeTests : IClassFixture<PlaywrightFixture>
{
    private readonly PlaywrightFixture _fixture;

    public SmokeTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task HomePage_ShouldLoad_WhenServerIsRunning()
    {
        // Arrange
        var page = _fixture.Page;

        // Act
        var response = await page.GotoAsync(_fixture.BaseUrl);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Ok);
        await Expect(page).ToHaveTitleAsync(new Regex("Budget"));
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task Login_ShouldSucceed_WithTestCredentials()
    {
        // Arrange
        var page = _fixture.Page;

        // Act
        await AuthenticationHelper.LoginAsync(page, _fixture.BaseUrl);

        // Assert
        await Expect(page.GetByRole(AriaRole.Navigation)).ToBeVisibleAsync();
    }
}
```

### Manual Testing Checklist

For pre-release verification:

- [ ] Run full E2E suite against demo: `dotnet test tests/BudgetExperiment.E2E.Tests`
- [ ] Verify all tests pass
- [ ] Spot-check with headed mode: `$env:HEADED="true"; dotnet test ...`
- [ ] Review any flaky test patterns

---

## Security Considerations

- **Test Credentials:** The `test/demo123` credentials are intentionally simple for a demo environment. This is a dedicated test account with limited permissions.
- **No Secrets in Code:** Test credentials for demo environment only. Production environments would require secure credential management (GitHub Secrets, etc.).
- **Demo Data Safety:** All tests must be read-only to prevent data corruption on shared demo environment.
- **Authentication Flow:** Tests interact with Authentik OAuth flow - ensure the authentication helper handles rate limiting gracefully.

---

## Performance Considerations

- **Parallel Execution:** Consider running navigation tests in parallel using xUnit's parallelization features to reduce total test time.
- **Browser Reuse:** The PlaywrightFixture creates a single browser instance per test class to minimize overhead.
- **Timeout Configuration:** Default timeouts should be generous for CI environments but configurable for faster local development.
- **Trace Capture:** Only enable traces when debugging failures to avoid storage/performance overhead.

---

## Troubleshooting

### Common Issues

| Issue | Cause | Solution |
|-------|-------|----------|
| Browser not found | Playwright browsers not installed | Run `pwsh playwright.ps1 install` |
| Authentication timeout | Authentik slow or unreachable | Increase timeout, check network |
| Element not found | Page structure changed | Update selectors, check for loading states |
| Flaky tests | Race conditions | Add proper waits, use `WaitForURLAsync` |
| SSL certificate errors | Self-signed cert in local dev | Use `--ignore-https-errors` option |

### Debugging Tips

1. **Headed Mode:** Run with `HEADED=true` to see browser actions
2. **Slow Motion:** Set `SLOWMO=1000` to slow down actions (ms)
3. **Traces:** Enable `PLAYWRIGHT_TRACES=true` and view with `npx playwright show-trace trace.zip`
4. **Screenshots:** Use `await page.ScreenshotAsync(new() { Path = "debug.png" })` to capture state
5. **Console Logs:** Listen to console events with `page.Console += (_, e) => Console.WriteLine(e.Text);`

---

## Future Enhancements

- **Visual Regression:** Add screenshot comparison tests using Playwright's built-in visual comparison
- **CRUD Tests:** Add data tests with unique identifiers (e.g., timestamped names) for test isolation
- **Performance Metrics:** Capture Core Web Vitals during E2E runs
- **Mobile Viewport Tests:** Add responsive design tests with different viewport sizes
- **Accessibility Tests:** Integrate axe-core for automated WCAG compliance checks
- **Cross-Browser Testing:** Extend to Firefox and WebKit for broader coverage
- **API Mocking:** Add tests with mocked API responses for edge cases

---

## Notes

- Demo environment uses Authentik for authentication
- The `test` user is specifically created for automated testing
- All tests should be demo-safe (read-only on shared data)
- Consider test data isolation strategies for future CRUD testing
- Tests assume stable demo environment - flakiness may indicate environment issues

---

## References

- [Playwright for .NET Documentation](https://playwright.dev/dotnet/)
- [xUnit Documentation](https://xunit.net/)
- [Playwright Test Best Practices](https://playwright.dev/docs/best-practices)
- [GitHub Actions - Playwright](https://playwright.dev/docs/ci-intro)

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-01-30 | Initial draft with full implementation plan | @copilot |
