# Feature 036: Demo Environment E2E Tests

## Status: In Progress 🔄

## Overview

Configure the existing Playwright E2E test suite to run against the demo environment at `budgetdemo.becauseimclever.com`. This enables continuous validation of the production-like demo deployment without affecting the local development workflow. Tests authenticate using dedicated test credentials (`test/demo123`) and verify critical user journeys on the live demo server.

## Problem Statement

### Current State

- Playwright E2E tests exist in `tests/BudgetExperiment.E2E.Tests` but are configured to run against localhost:5099
- No automated validation of the demo environment exists
- Demo environment issues may go undetected until manual verification
- The existing `AuthenticationHelper` already uses `test/demo123` credentials which work on the demo environment

### Target State

- E2E tests can run against `budgetdemo.becauseimclever.com` via environment variable
- CI/CD can optionally run E2E tests against demo after deployments (future integration)
- Demo environment health is continuously validated with existing test coverage
- Clear documentation for running tests against demo vs. local

---

## User Stories

### Configuration

#### US-036-001: Environment Variable for Demo URL
**As a** developer  
**I want to** run E2E tests against the demo environment using an environment variable  
**So that** I can validate the demo deployment without code changes

**Acceptance Criteria:**
- [x] `BUDGET_APP_URL` environment variable already supported in `PlaywrightFixture`
- [ ] Set `BUDGET_APP_URL=https://budgetdemo.becauseimclever.com` to target demo
- [ ] Tests connect successfully and authenticate with `test/demo123`
- [ ] Documentation updated with demo test commands

#### US-036-002: Skip Server Startup for Remote Targets
**As a** developer  
**I want** the test fixture to skip local server startup when targeting remote URLs  
**So that** tests run immediately against the demo without startup delays

**Acceptance Criteria:**
- [ ] `PlaywrightFixture` detects remote URLs (not localhost)
- [ ] Skips `StartServerAsync` for remote targets
- [ ] Still validates server is reachable before running tests

### Test Execution

#### US-036-003: Run Core Test Suites Against Demo
**As a** developer  
**I want** to run existing navigation, smoke, accounts, and budget tests against demo  
**So that** I can verify demo environment functionality

**Acceptance Criteria:**
- [ ] `SmokeTests` pass against demo
- [ ] `NavigationTests` pass against demo
- [ ] `AccountsTests` pass against demo (read operations)
- [ ] `BudgetTests` pass against demo

#### US-036-004: Demo-Safe Test Execution
**As a** developer  
**I want** tests to be safe for demo environment (no destructive operations on shared data)  
**So that** demo data integrity is preserved

**Acceptance Criteria:**
- [ ] Document which tests are safe for demo (read-only operations)
- [ ] Consider test category/trait for "demo-safe" tests
- [ ] CRUD tests that create/modify data should use unique identifiers

---

## Technical Design

### Configuration Changes

The existing `PlaywrightFixture` already supports `BUDGET_APP_URL` override. Minor enhancement needed to skip local server startup for remote URLs:

```csharp
// In PlaywrightFixture.EnsureServerIsRunningAsync()
private async Task EnsureServerIsRunningAsync()
{
    // Skip server startup for remote URLs
    if (!BaseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase) &&
        !BaseUrl.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase))
    {
        // Just verify remote server is reachable
        if (!await IsServerRunningAsync())
        {
            throw new InvalidOperationException(
                $"Remote server at {BaseUrl} is not reachable. Verify the URL and network connectivity.");
        }
        return;
    }

    // Existing local server startup logic...
}
```

### Test Credentials

Already configured in `AuthenticationHelper`:
- Username: `test`
- Password: `demo123`

These credentials work for both local (Authentik) and demo environment authentication.

### Environment Variables

| Variable | Purpose | Default |
|----------|---------|---------|
| `BUDGET_APP_URL` | Target application URL | `http://localhost:5099` |
| `HEADED` | Run with visible browser | `false` |

---

## Implementation Plan

### Phase 1: Update PlaywrightFixture for Remote URLs

**Objective:** Skip local server startup when targeting remote URLs

**Tasks:**
- [ ] Modify `EnsureServerIsRunningAsync` to detect remote URLs
- [ ] Skip `StartServerAsync` for non-localhost targets
- [ ] Add clear error message if remote server is unreachable
- [ ] Test against demo environment manually

**Commit:**
```bash
git add .
git commit -m "test(e2e): support remote URLs in PlaywrightFixture

- Skip local server startup for non-localhost URLs
- Add validation that remote server is reachable
- Enables running tests against budgetdemo.becauseimclever.com

Refs: #036"
```

### Phase 2: Add Demo Test Traits/Categories

**Objective:** Mark tests that are safe to run against demo (optional)

**Tasks:**
- [ ] Create `[Trait("Category", "DemoSafe")]` for read-only tests
- [ ] Document which tests modify data and should be skipped on demo
- [ ] Update README with demo test filtering examples

**Commit:**
```bash
git add .
git commit -m "test(e2e): add DemoSafe trait for demo-compatible tests

- Mark read-only tests as DemoSafe
- Document test categories for demo vs. local execution

Refs: #036"
```

### Phase 3: Documentation

**Objective:** Update documentation for demo environment testing

**Tasks:**
- [ ] Update E2E test project README
- [ ] Add quick-start commands for demo testing
- [ ] Document demo credentials and URL

---

## Running Tests Against Demo

### Prerequisites

1. Ensure `budgetdemo.becauseimclever.com` is accessible
2. Playwright browsers installed: `pwsh tests/BudgetExperiment.E2E.Tests/bin/Debug/net10.0/playwright.ps1 install`

### Commands

```powershell
# Run all E2E tests against demo environment
$env:BUDGET_APP_URL = "https://budgetdemo.becauseimclever.com"
dotnet test c:\ws\BudgetExperiment\tests\BudgetExperiment.E2E.Tests\BudgetExperiment.E2E.Tests.csproj

# Run with visible browser for debugging
$env:BUDGET_APP_URL = "https://budgetdemo.becauseimclever.com"
$env:HEADED = "true"
dotnet test c:\ws\BudgetExperiment\tests\BudgetExperiment.E2E.Tests\BudgetExperiment.E2E.Tests.csproj

# Run only smoke tests
$env:BUDGET_APP_URL = "https://budgetdemo.becauseimclever.com"
dotnet test c:\ws\BudgetExperiment\tests\BudgetExperiment.E2E.Tests\BudgetExperiment.E2E.Tests.csproj --filter "FullyQualifiedName~SmokeTests"

# Run demo-safe tests only (after Phase 2)
$env:BUDGET_APP_URL = "https://budgetdemo.becauseimclever.com"
dotnet test c:\ws\BudgetExperiment\tests\BudgetExperiment.E2E.Tests\BudgetExperiment.E2E.Tests.csproj --filter "Category=DemoSafe"

# Single command (PowerShell)
$env:BUDGET_APP_URL = "https://budgetdemo.becauseimclever.com"; dotnet test c:\ws\BudgetExperiment\tests\BudgetExperiment.E2E.Tests\BudgetExperiment.E2E.Tests.csproj
```

### Linux/macOS/CI

```bash
# Run all tests against demo
BUDGET_APP_URL=https://budgetdemo.becauseimclever.com dotnet test tests/BudgetExperiment.E2E.Tests

# With headed mode
BUDGET_APP_URL=https://budgetdemo.becauseimclever.com HEADED=true dotnet test tests/BudgetExperiment.E2E.Tests
```

---

## Demo Environment Details

| Property | Value |
|----------|-------|
| URL | `https://budgetdemo.becauseimclever.com` |
| Test Username | `test` |
| Test Password | `demo123` |
| Auth Provider | Authentik (same as production) |

---

## Future Enhancements

- **CI/CD Integration:** Add GitHub Actions workflow step to run E2E tests against demo after deployment
- **Scheduled Tests:** Nightly E2E run against demo to catch environment issues
- **Test Reports:** Publish Playwright test reports to GitHub Pages or artifact storage
- **Parallel Execution:** Configure test parallelization for faster demo validation

---

## Notes

- Demo environment uses the same Authentik instance for authentication
- The `test` user is specifically created for automated testing
- Avoid running data-destructive tests against demo without unique identifiers
- Consider test data isolation strategies for future CRUD testing on demo

---

## References

- [docs/archive/024.4-playwright-e2e-tests.md](archive/024.4-playwright-e2e-tests.md) - Original Playwright setup
- [Playwright for .NET Documentation](https://playwright.dev/dotnet/)
- [tests/BudgetExperiment.E2E.Tests](../tests/BudgetExperiment.E2E.Tests) - E2E test project
