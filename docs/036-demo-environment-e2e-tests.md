# Feature 036: Demo Environment E2E Tests

## Status: Planning 📋

## Overview

Create a Playwright-based E2E test suite that validates the Budget Experiment application against the demo environment at `budgetdemo.becauseimclever.com`. This enables continuous validation of the production-like demo deployment. Tests will authenticate using dedicated test credentials (`test/demo123`) and verify critical user journeys.

## Problem Statement

### Current State

- No E2E test project exists (previous implementation was removed)
- No automated validation of the demo environment
- Demo environment issues may go undetected until manual verification
- Need a clean, well-structured E2E testing foundation

### Target State

- New Playwright E2E test project with proper architecture
- Tests run against `budgetdemo.becauseimclever.com` by default
- Option to run against localhost for local development testing
- Demo environment health continuously validated
- Clear test organization with categories for different test types

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
- [ ] Test Dashboard page loads
- [ ] Test Transactions page loads
- [ ] Test Accounts page loads
- [ ] Test Budget pages load
- [ ] Test Settings page loads

#### US-036-006: Demo-Safe Data Tests
**As a** developer  
**I want** read-only data verification tests  
**So that** I can validate data displays correctly without modifying demo data

**Acceptance Criteria:**
- [ ] Tests read and verify existing data (accounts, transactions)
- [ ] No tests create, update, or delete shared demo data
- [ ] Tests marked with `[Trait("Category", "DemoSafe")]`

---

## Technical Design

### Project Structure

```
tests/
└── BudgetExperiment.E2E.Tests/
    ├── BudgetExperiment.E2E.Tests.csproj
    ├── GlobalUsings.cs
    ├── Fixtures/
    │   └── PlaywrightFixture.cs
    ├── Helpers/
    │   └── AuthenticationHelper.cs
    ├── Tests/
    │   ├── SmokeTests.cs
    │   ├── NavigationTests.cs
    │   └── AccountsTests.cs
    └── README.md
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
- [ ] Test each main navigation item
- [ ] Verify page content loads

**Commit:**
```
test(e2e): add navigation tests for all main pages

- Dashboard, Transactions, Accounts pages
- Budget, Reports, Settings pages
- Verify each page loads successfully

Refs: #036
```

### Phase 5: Documentation

**Objective:** Document how to run E2E tests

**Tasks:**
- [ ] Create project README
- [ ] Add quick-start commands
- [ ] Document environment variables
- [ ] Add troubleshooting section

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
```

### Linux/macOS/CI

```bash
# Run tests against demo
dotnet test tests/BudgetExperiment.E2E.Tests

# With headed mode
HEADED=true dotnet test tests/BudgetExperiment.E2E.Tests

# Against local
BUDGET_APP_URL=http://localhost:5099 dotnet test tests/BudgetExperiment.E2E.Tests
```

---

## Demo Environment Details

| Property | Value |
|----------|-------|
| URL | `https://budgetdemo.becauseimclever.com` |
| Test Username | `test` |
| Test Password | `demo123` |
| Auth Provider | Authentik |

---

## Future Enhancements

- **CI/CD Integration:** Add GitHub Actions workflow to run E2E tests after deployment
- **Scheduled Tests:** Nightly E2E run against demo to catch environment issues
- **Test Reports:** Publish Playwright test reports as artifacts
- **Visual Regression:** Add screenshot comparison tests
- **CRUD Tests:** Add data tests with unique identifiers for test isolation

---

## Notes

- Demo environment uses Authentik for authentication
- The `test` user is specifically created for automated testing
- All tests should be demo-safe (read-only on shared data)
- Consider test data isolation strategies for future CRUD testing

---

## References

- [Playwright for .NET Documentation](https://playwright.dev/dotnet/)
- [xUnit Documentation](https://xunit.net/)
