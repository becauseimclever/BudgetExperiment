# Per-Module Coverage Validation

## Overview

This directory contains the PowerShell script and configuration for validating per-module code coverage thresholds in the BudgetExperiment solution. The validation enforces minimum coverage requirements for each module and prevents coverage regression.

## Files

- **`validate-module-coverage.ps1`** — Main validation script that parses Cobertura.xml and enforces per-module thresholds
- **`module-coverage-config.json`** — Per-module coverage threshold configuration

## Per-Module Thresholds (Vic's Recommendations)

| Module | Threshold | Rationale |
|--------|-----------|-----------|
| **Domain** | 90% | Financial invariants, MoneyValue arithmetic — immutable primitives demand exhaustive testing |
| **Application** | 85% | Core business logic services — budget calculations, recurring charge detection, suggestions |
| **Api** | 80% | REST endpoints — validation, error handling, DTOs (achieved, maintain) |
| **Client** | 75% | Blazor WASM UI — markup-heavy, inherently difficult to test |
| **Infrastructure** | 70% | EF Core repositories, migrations — Testcontainer-heavy, integration-focused |
| **Contracts** | 60% | DTOs, request/response types — minimal logic, low risk |

## Features

### 1. Per-Module Threshold Enforcement

The script validates that each module meets its minimum coverage threshold. Failures block CI builds.

```powershell
# Run validation (console output)
.\.github\scripts\validate-module-coverage.ps1

# Generate markdown for PR comments
.\.github\scripts\validate-module-coverage.ps1 -OutputFormat github-markdown
```

### 2. Coverage Regression Detection

The script tracks previous coverage state in `coverage-state.json` (gitignored) and fails if any module's coverage drops below its previous level.

```powershell
# Disable regression checking
.\.github\scripts\validate-module-coverage.ps1 -FailOnRegression $false
```

### 3. Trend Tracking

The script shows coverage trends (±X%) compared to the previous run, helping identify improvements or regressions.

## Usage

### Local Development

Run the script after running tests with coverage:

```powershell
# Run tests with coverage
dotnet test BudgetExperiment.sln `
  --configuration Release `
  --filter "Category!=Performance" `
  --collect:"XPlat Code Coverage" `
  --settings coverlet.runsettings `
  --results-directory ./TestResults

# Merge coverage reports
reportgenerator `
  -reports:./TestResults/**/coverage.cobertura.xml `
  -targetdir:./CoverageReport `
  -reporttypes:Cobertura

# Validate per-module coverage
.\.github\scripts\validate-module-coverage.ps1
```

### CI Integration

Add the script to `.github/workflows/ci.yml` after the existing coverage threshold step:

```yaml
- name: Validate per-module coverage
  run: |
    pwsh .github/scripts/validate-module-coverage.ps1 -OutputFormat github-markdown > module-coverage-results.md
    
- name: Add module coverage PR comment
  uses: marocchino/sticky-pull-request-comment@v3
  if: github.event_name == 'pull_request'
  with:
    recreate: true
    path: module-coverage-results.md
    header: module-coverage
```

## Cobertura XML Format

The script expects a merged Cobertura.xml report with the following structure:

```xml
<coverage line-rate="0.7841" branch-rate="0.6892" ...>
  <packages>
    <package name="BudgetExperiment.Domain" line-rate="0.9277" ...>
      <classes>...</classes>
    </package>
    <package name="BudgetExperiment.Application" line-rate="0.9029" ...>
      <classes>...</classes>
    </package>
    <!-- ... more packages ... -->
  </packages>
</coverage>
```

### Module Identification Logic

Modules are identified by the `<package name="...">` attribute in Cobertura XML. Only modules defined in `module-coverage-config.json` are validated. Other packages (e.g., `BudgetExperiment.Shared`) are ignored.

## Configuration

### Customizing Thresholds

Edit `.github/scripts/module-coverage-config.json` to adjust thresholds:

```json
{
  "modules": {
    "BudgetExperiment.Domain": {
      "threshold": 90,
      "rationale": "Financial invariants..."
    }
  }
}
```

### State File Location

The default state file is `coverage-state.json` in the repository root (gitignored). To use a different location:

```powershell
.\.github\scripts\validate-module-coverage.ps1 -StatePath "./custom/path.json"
```

## Output Formats

### Text (Console)

```
==============================================
  Per-Module Coverage Validation Report
  2026-04-25 00:24:36 UTC
==============================================

[✓] BudgetExperiment.Domain: 92.77% (Target: 90%) (+0.52%)
[✗] BudgetExperiment.Api: 77.2% (Target: 80%) (Gap: -2.8%) (-0.15%)

----------------------------------------------
Summary: 4 of 6 modules passed

Coverage Regressions:
  - BudgetExperiment.Api: 77.35% → 77.2% (-0.15%)
----------------------------------------------
VALIDATION FAILED: 2 module(s) below threshold or regressed
==============================================
```

### GitHub Markdown (PR Comments)

```markdown
## 📊 Per-Module Coverage Report

_Generated: 2026-04-25 00:24:36 UTC_

### ❌ Coverage validation failed

**Summary:** 4 of 6 modules passed, **2 failed**.

| Module | Coverage % | Target % | Status | Trend |
|--------|------------|----------|--------|-------|
| BudgetExperiment.Domain | 92.77% | 90% | ✅ Pass | +0.52% |
| BudgetExperiment.Api | 77.2% | 80% | ❌ Fail | -0.15% |

**⚠️ Coverage Regressions Detected:**

- **BudgetExperiment.Api**: Dropped from 77.35% to 77.2% (-0.15%)
```

## Exit Codes

- **0** — All modules passed thresholds and no regressions
- **1** — One or more modules failed threshold or regressed

## Troubleshooting

### Missing Modules

If a module is missing from the coverage report:

1. Verify the module has tests in the corresponding `tests/` project
2. Check that tests are running (not filtered out)
3. Ensure coverlet is collecting coverage for the module
4. Check the merged Cobertura.xml for the package name

Infrastructure module is currently missing from coverage (no tests are running for it).

### False Regression Alerts

To reset the coverage state and start fresh:

```powershell
Remove-Item ./coverage-state.json
.\.github\scripts\validate-module-coverage.ps1
```

### Configuration Not Loading

If the script falls back to hardcoded thresholds:

1. Verify `module-coverage-config.json` exists
2. Check JSON syntax (use a validator)
3. Run with `-Verbose` to see loading messages

## References

- **Feature Doc:** `docs/127-code-coverage-beyond-80-percent.md`
- **Squad Decision:** `.squad/decisions.md` (Vic's mandatory guardrails)
- **CI Workflow:** `.github/workflows/ci.yml`
