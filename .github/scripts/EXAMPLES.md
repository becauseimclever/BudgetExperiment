# Module Coverage Validation — Usage Examples

## Example 1: Console Validation (Local Development)

After running tests with coverage, validate per-module thresholds:

```powershell
PS C:\ws\BudgetExperiment> .\.github\scripts\validate-module-coverage.ps1

==============================================
  Per-Module Coverage Validation Report
  2026-04-25 00:24:36 UTC
==============================================

[✗] BudgetExperiment.Api: 77.2% (Target: 80%) (Gap: -2.8%)
[✓] BudgetExperiment.Application: 90.29% (Target: 85%)
[✗] BudgetExperiment.Client: 68.07% (Target: 75%) (Gap: -6.93%)
[✓] BudgetExperiment.Contracts: 95.01% (Target: 60%)
[✓] BudgetExperiment.Domain: 92.77% (Target: 90%)

----------------------------------------------
Summary: 3 of 5 modules passed
----------------------------------------------
VALIDATION FAILED: 2 module(s) below threshold or regressed
==============================================

PS C:\ws\BudgetExperiment> echo $LASTEXITCODE
1
```

**Result:** Api and Client modules are below threshold. Need to add tests.

---

## Example 2: Markdown Output (PR Comments)

Generate markdown-formatted report for PR comment:

```powershell
PS C:\ws\BudgetExperiment> .\.github\scripts\validate-module-coverage.ps1 -OutputFormat github-markdown > coverage-report.md
PS C:\ws\BudgetExperiment> Get-Content coverage-report.md
```

**Output:**

## 📊 Per-Module Coverage Report

_Generated: 2026-04-25 00:24:36 UTC_

### ❌ Coverage validation failed

**Summary:** 3 of 5 modules passed, **2 failed**.

| Module | Coverage % | Target % | Status | Trend |
|--------|------------|----------|--------|-------|
| BudgetExperiment.Api | 77.2% | 80% | ❌ Fail | ±0.00% |
| BudgetExperiment.Application | 90.29% | 85% | ✅ Pass | ±0.00% |
| BudgetExperiment.Client | 68.07% | 75% | ❌ Fail | ±0.00% |
| BudgetExperiment.Contracts | 95.01% | 60% | ✅ Pass | ±0.00% |
| BudgetExperiment.Domain | 92.77% | 90% | ✅ Pass | ±0.00% |

**Action Required:** Increase test coverage for failing modules before merge.

<details>
<summary>📖 Module Coverage Targets (Vic's Recommendations)</summary>

- **Domain (90%):** Financial invariants, arithmetic, core entities — must be exhaustive
- **Application (85%):** Business logic orchestration — critical paths coverage
- **Api (80%):** Controller orchestration, error paths, concurrency conflicts
- **Client (75%):** UI components — high-traffic pages prioritized
- **Infrastructure (70%):** Data access, integration-heavy, Testcontainer-backed
- **Contracts (60%):** DTOs with minimal logic, low risk

</details>

---

## Example 3: Regression Detection

After improving Api coverage from 77.2% to 78.5%, then later dropping back:

```powershell
PS C:\ws\BudgetExperiment> # First run: 78.5% coverage
PS C:\ws\BudgetExperiment> .\.github\scripts\validate-module-coverage.ps1

[✓] BudgetExperiment.Api: 78.5% (Target: 80%) (Gap: -1.5%)

# State saved: Api = 78.5%

PS C:\ws\BudgetExperiment> # Second run: dropped to 77.2%
PS C:\ws\BudgetExperiment> .\.github\scripts\validate-module-coverage.ps1

[✗] BudgetExperiment.Api: 77.2% (Target: 80%) (Gap: -2.8%) (-1.3%)

----------------------------------------------
Summary: 2 of 5 modules passed

Coverage Regressions:
  - BudgetExperiment.Api: 78.5% → 77.2% (-1.3%)
----------------------------------------------
VALIDATION FAILED: 2 module(s) below threshold or regressed
```

**Result:** Api module regressed by 1.3%. Regression detected even though still below threshold.

---

## Example 4: Passing Module with Regression

Domain module passes threshold (90%) but regressed from 93.5%:

```powershell
PS C:\ws\BudgetExperiment> .\.github\scripts\validate-module-coverage.ps1

[⚠] BudgetExperiment.Domain: 92.77% (Target: 90%) (-0.73%)

Coverage Regressions:
  - BudgetExperiment.Domain: 93.5% → 92.77% (-0.73%)
----------------------------------------------
VALIDATION FAILED: 0 module(s) below threshold, 1 regressed
```

**Result:** Domain still passes threshold but regressed. CI fails to prevent coverage drops.

---

## Example 5: Disable Regression Checking

If regression alerts are too noisy during development:

```powershell
PS C:\ws\BudgetExperiment> .\.github\scripts\validate-module-coverage.ps1 -FailOnRegression $false

[✓] BudgetExperiment.Domain: 92.77% (Target: 90%) (-0.73%)

----------------------------------------------
Summary: 3 of 5 modules passed
----------------------------------------------
VALIDATION FAILED: 2 module(s) below threshold
```

**Result:** Regression ignored. Only threshold violations cause failure.

---

## Example 6: CI Integration (GitHub Actions)

Add to `.github/workflows/ci.yml` after coverage collection:

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

**Result:** Every PR gets a per-module coverage report comment with trend tracking.

---

## Example 7: Reset Coverage State

To start fresh (e.g., after major refactor):

```powershell
PS C:\ws\BudgetExperiment> Remove-Item .\coverage-state.json
PS C:\ws\BudgetExperiment> .\.github\scripts\validate-module-coverage.ps1

[✓] BudgetExperiment.Domain: 92.77% (Target: 90%)

# Trend shows ±0.00% (new baseline)
```

**Result:** All trends reset. Next run will compare against this baseline.

---

## Example 8: Custom Configuration Path

Use a different config file (e.g., for stricter local development):

```powershell
PS C:\ws\BudgetExperiment> .\.github\scripts\validate-module-coverage.ps1 `
  -ConfigPath ".\local-coverage-config.json" `
  -StatePath ".\local-coverage-state.json"
```

**Result:** Script uses custom thresholds and separate state tracking.
