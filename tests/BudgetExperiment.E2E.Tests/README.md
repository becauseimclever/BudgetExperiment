# BudgetExperiment E2E Tests

End-to-end tests using Playwright for .NET to validate the Budget Experiment application.

## Quick Start

### Prerequisites

1. .NET 10 SDK
2. Playwright browsers (installed once)

### Install Playwright Browsers

After building the project, run:

```powershell
# Windows
pwsh tests/BudgetExperiment.E2E.Tests/bin/Debug/net10.0/playwright.ps1 install

# Linux/macOS
pwsh tests/BudgetExperiment.E2E.Tests/bin/Debug/net10.0/playwright.ps1 install
```

### Run Tests

```powershell
# Run all tests against demo environment (default)
dotnet test c:\ws\BudgetExperiment\tests\BudgetExperiment.E2E.Tests

# Run only smoke tests
dotnet test c:\ws\BudgetExperiment\tests\BudgetExperiment.E2E.Tests --filter "Category=Smoke"

# Run navigation tests
dotnet test c:\ws\BudgetExperiment\tests\BudgetExperiment.E2E.Tests --filter "Category=Navigation"
```

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `BUDGET_APP_URL` | Target application URL | `https://budgetdemo.becauseimclever.com` |
| `HEADED` | Show browser window | `false` |
| `SLOWMO` | Slow down actions (ms) | `0` |
| `PLAYWRIGHT_TRACES` | Capture traces | `false` |

### Examples

```powershell
# Run against local development
$env:BUDGET_APP_URL = "http://localhost:5099"
dotnet test tests/BudgetExperiment.E2E.Tests

# Debug with visible browser and slow motion
$env:HEADED = "true"
$env:SLOWMO = "1000"
dotnet test tests/BudgetExperiment.E2E.Tests --filter "Category=Smoke"

# Capture traces for debugging failures
$env:PLAYWRIGHT_TRACES = "true"
dotnet test tests/BudgetExperiment.E2E.Tests
```

## Test Categories

Use `--filter "Category=X"` to run specific test categories:

| Category | Description |
|----------|-------------|
| `Smoke` | Quick validation that app is running |
| `Navigation` | All pages load successfully |
| `DemoSafe` | Read-only tests safe for shared demo |
| `UserJourney` | Critical workflow tests |
| `Performance` | Core Web Vitals and performance validation |
| `ZeroFlash` | Verify no auth flash messages appear |
| `CoreWebVitals` | FCP, LCP, TTI, CLS threshold tests |
| `FCP` | First Contentful Paint specific tests |
| `CLS` | Cumulative Layout Shift tests |

### Running Performance Tests

```powershell
# Run all performance tests
dotnet test tests/BudgetExperiment.E2E.Tests --filter "Category=Performance"

# Run only Core Web Vitals tests
dotnet test tests/BudgetExperiment.E2E.Tests --filter "Category=CoreWebVitals"

# Run zero-flash authentication tests
dotnet test tests/BudgetExperiment.E2E.Tests --filter "Category=ZeroFlash"
```

## Demo Environment

| Property | Value |
|----------|-------|
| URL | `https://budgetdemo.becauseimclever.com` |
| Test Username | `test` |
| Test Password | `demo123` |
| Auth Provider | Authentik |

> **Note:** These credentials are for the shared demo environment only. Do not use production credentials in tests.

## Troubleshooting

### Browser not found

Run the Playwright install script:
```powershell
pwsh tests/BudgetExperiment.E2E.Tests/bin/Debug/net10.0/playwright.ps1 install
```

### Authentication timeout

- Check if `budgetdemo.becauseimclever.com` is accessible
- The Authentik server may be slow; tests have generous timeouts

### Element not found

- UI may have changed; update selectors
- Try running with `HEADED=true` to see what's happening

### Viewing traces

If you enabled `PLAYWRIGHT_TRACES=true`, traces are saved to `playwright-traces/`:

```powershell
npx playwright show-trace playwright-traces/trace-*.zip
```

## Project Structure

```
BudgetExperiment.E2E.Tests/
├── Fixtures/
│   └── PlaywrightFixture.cs      # Browser lifecycle management
├── Helpers/
│   ├── AuthenticationHelper.cs    # Authentik login flow
│   ├── NavigationHelper.cs        # Route constants
│   ├── PerformanceHelper.cs       # Core Web Vitals capture utilities
│   ├── PerformanceMetrics.cs      # Performance metrics record type
│   └── PerformanceThresholds.cs   # Performance threshold constants
├── Tests/
│   ├── SmokeTests.cs              # Basic app validation
│   ├── NavigationTests.cs         # Page accessibility tests
│   ├── PerformanceTests.cs        # Core Web Vitals tests
│   └── ZeroFlashAuthTests.cs      # Auth flash verification
├── GlobalUsings.cs
├── PlaywrightCollection.cs        # xUnit collection fixture
└── README.md
```
