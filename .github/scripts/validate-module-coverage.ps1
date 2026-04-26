<#
.SYNOPSIS
Validates per-module code coverage thresholds from Cobertura XML report.

.DESCRIPTION
Parses the merged Cobertura.xml coverage report and validates each module against
its minimum coverage threshold. Tracks previous coverage to prevent retroactive drops.
Fails if any module drops below target or regresses from previous run.

Per-module thresholds (Vic's recommendations):
- Domain: 90%
- Application: 85%
- Api: 80%
- Client: 75%
- Infrastructure: 70%
- Contracts: 60%

.PARAMETER CoberturaPath
Path to the merged Cobertura.xml coverage report (default: ./CoverageReport/Cobertura.xml)

.PARAMETER ConfigPath
Path to module coverage configuration JSON (default: .github/scripts/module-coverage-config.json)

.PARAMETER StatePath
Path to previous coverage state JSON (default: coverage-state.json - gitignored)

.PARAMETER OutputStatePath
Path to write current coverage state (default: same as StatePath). Useful for testing.

.PARAMETER OutputFormat
Output format: 'text' (default) or 'github-markdown' for PR comments

.PARAMETER FailOnRegression
Fail if any module coverage drops from previous run (default: true)

.EXAMPLE
.\validate-module-coverage.ps1 -CoberturaPath ./CoverageReport/Cobertura.xml -OutputFormat github-markdown

.EXAMPLE
.\validate-module-coverage.ps1 -FailOnRegression $false
#>

param(
    [string]$CoberturaPath = "./CoverageReport/Cobertura.xml",
    [string]$ConfigPath = "./.github/scripts/module-coverage-config.json",
    [string]$StatePath = "./coverage-state.json",
    [string]$OutputStatePath = "",
    [ValidateSet('text', 'github-markdown')]
    [string]$OutputFormat = 'text',
    [bool]$FailOnRegression = $true
)

# Default output state path to input state path if not specified
if ([string]::IsNullOrWhiteSpace($OutputStatePath)) {
    $OutputStatePath = $StatePath
}

# Load module coverage thresholds from config (or use defaults)
$thresholds = @{
    'BudgetExperiment.Domain'         = 90
    'BudgetExperiment.Application'    = 85
    'BudgetExperiment.Api'            = 80
    'BudgetExperiment.Client'         = 75
    'BudgetExperiment.Infrastructure' = 70
    'BudgetExperiment.Contracts'      = 60
}

if (Test-Path $ConfigPath) {
    try {
        $config = Get-Content $ConfigPath -Raw | ConvertFrom-Json
        foreach ($prop in $config.modules.PSObject.Properties) {
            $thresholds[$prop.Name] = $prop.Value.threshold
        }
        Write-Verbose "Loaded thresholds from $ConfigPath"
    }
    catch {
        Write-Warning "Failed to load config from $ConfigPath, using defaults: $_"
    }
}

# Load previous coverage state if exists
$previousState = @{}
if (Test-Path $StatePath) {
    try {
        $stateData = Get-Content $StatePath -Raw | ConvertFrom-Json
        if ($null -ne $stateData) {
            foreach ($prop in $stateData.PSObject.Properties) {
                $previousState[$prop.Name] = $prop.Value
            }
        }
        Write-Verbose "Loaded previous coverage from $StatePath"
    }
    catch {
        Write-Warning "Failed to load previous coverage state: $_"
    }
}

# Validation state
$allPassed = $true
$results = @()
$regressions = @()
$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss UTC"

# Check if coverage report exists
if (-not (Test-Path $CoberturaPath)) {
    Write-Error "Coverage report not found: $CoberturaPath"
    exit 1
}

# Load and parse Cobertura XML
try {
    [xml]$coverage = Get-Content $CoberturaPath
}
catch {
    Write-Error "Failed to parse Cobertura XML: $_"
    exit 1
}

# Build map of actual coverage from Cobertura XML
$actualCoverage = @{}
foreach ($package in $coverage.coverage.packages.package) {
    $moduleName = $package.name
    
    # Skip if not a tracked module
    if (-not $thresholds.ContainsKey($moduleName)) {
        continue
    }
    
    # Calculate line coverage percentage
    $lineRate = [double]$package.'line-rate'
    $coveragePercent = [math]::Round($lineRate * 100, 2)
    $actualCoverage[$moduleName] = $coveragePercent
}

# Process ALL configured modules (not just those in Cobertura.xml)
foreach ($moduleName in $thresholds.Keys | Sort-Object) {
    $threshold = $thresholds[$moduleName]
    
    # Check if module has coverage data
    if ($actualCoverage.ContainsKey($moduleName)) {
        $coveragePercent = $actualCoverage[$moduleName]
    } else {
        # Module missing from coverage report - treat as 0%
        $coveragePercent = 0.0
        Write-Warning "Module '$moduleName' not found in coverage report - treating as 0% coverage"
    }
    
    # Get previous coverage for this module
    $previousCoverage = if ($previousState.ContainsKey($moduleName)) {
        $previousState[$moduleName]
    } else {
        $null
    }
    
    # Determine pass/fail
    $passed = $coveragePercent -ge $threshold
    $regressed = $false
    
    if (-not $passed) {
        $allPassed = $false
    }
    elseif ($FailOnRegression -and $null -ne $previousCoverage -and $coveragePercent -lt $previousCoverage) {
        $regressed = $true
        $allPassed = $false
        Write-Verbose "Regression detected for $moduleName : $previousCoverage% → $coveragePercent%"
        $regressions += [PSCustomObject]@{
            Module = $moduleName
            Current = $coveragePercent
            Previous = $previousCoverage
            Drop = [math]::Round($previousCoverage - $coveragePercent, 2)
        }
    }
    
    # Store result
    $results += [PSCustomObject]@{
        Module    = $moduleName
        Coverage  = $coveragePercent
        Threshold = $threshold
        Previous  = $previousCoverage
        Passed    = $passed
        Regressed = $regressed
        Gap       = if ($passed) { 0 } else { [math]::Round($threshold - $coveragePercent, 2) }
        Trend     = if ($null -ne $previousCoverage) {
            [math]::Round($coveragePercent - $previousCoverage, 2)
        } else {
            $null
        }
    }
}

# Sort results by module name
$results = $results | Sort-Object Module

# Calculate summary
$passCount = ($results | Where-Object { $_.Passed }).Count
$totalCount = $results.Count
$failCount = $totalCount - $passCount

# Output results
if ($OutputFormat -eq 'github-markdown') {
    Write-Output "## 📊 Per-Module Coverage Report"
    Write-Output ""
    Write-Output "_Generated: $timestamp_"
    Write-Output ""
    
    if ($allPassed) {
        Write-Output "### ✅ All modules passed coverage thresholds"
        Write-Output ""
        Write-Output "**Summary:** $passCount of $totalCount modules meet their coverage targets."
    }
    else {
        Write-Output "### ❌ Coverage validation failed"
        Write-Output ""
        Write-Output "**Summary:** $passCount of $totalCount modules passed, **$failCount failed**."
    }
    
    Write-Output ""
    Write-Output "| Module | Coverage % | Target % | Status | Trend |"
    Write-Output "|--------|------------|----------|--------|-------|"
    
    foreach ($result in $results) {
        if ($result.Regressed) {
            $status = "⚠️ Regress"
        }
        elseif ($result.Passed) {
            $status = "✅ Pass"
        }
        else {
            $status = "❌ Fail"
        }
        
        $trend = if ($null -ne $result.Trend) {
            if ($result.Trend -gt 0) { "↑ +$($result.Trend)%" }
            elseif ($result.Trend -lt 0) { "↓ $($result.Trend)%" }
            else { "→ ±0.00%" }
        } else {
            "-"
        }
        
        Write-Output "| $($result.Module) | $($result.Coverage)% | $($result.Threshold)% | $status | $trend |"
    }
    
    Write-Output ""
    
    if ($regressions.Count -gt 0) {
        Write-Output "**⚠️ Coverage Regressions Detected:**"
        Write-Output ""
        foreach ($reg in $regressions) {
            Write-Output "- **$($reg.Module)**: Dropped from $($reg.Previous)% to $($reg.Current)% (-$($reg.Drop)%)"
        }
        Write-Output ""
    }
    
    if (-not $allPassed) {
        Write-Output "**Action Required:** Increase test coverage for failing modules before merge."
        Write-Output ""
        Write-Output "<details>"
        Write-Output "<summary>📖 Module Coverage Targets (Vic's Recommendations)</summary>"
        Write-Output ""
        Write-Output "- **Domain (90%):** Financial invariants, arithmetic, core entities — must be exhaustive"
        Write-Output "- **Application (85%):** Business logic orchestration — critical paths coverage"
        Write-Output "- **Api (80%):** Controller orchestration, error paths, concurrency conflicts"
        Write-Output "- **Client (75%):** UI components — high-traffic pages prioritized"
        Write-Output "- **Infrastructure (70%):** Data access, integration-heavy, Testcontainer-backed"
        Write-Output "- **Contracts (60%):** DTOs with minimal logic, low risk"
        Write-Output ""
        Write-Output "</details>"
    }
}
else {
    # Text format for console output
    Write-Output "=============================================="
    Write-Output "  Per-Module Coverage Validation Report"
    Write-Output "  $timestamp"
    Write-Output "=============================================="
    Write-Output ""
    
    foreach ($result in $results) {
        if ($result.Regressed) {
            $status = "REGRESS"
            $statusSymbol = "⚠"
        }
        elseif ($result.Passed) {
            $status = "PASS"
            $statusSymbol = "✓"
        }
        else {
            $status = "FAIL"
            $statusSymbol = "✗"
        }
        
        $gap = if ($result.Gap -eq 0) { "" } else { " (Gap: -$($result.Gap)%)" }
        $trend = if ($null -ne $result.Trend) {
            if ($result.Trend -gt 0) { " (+$($result.Trend)%)" }
            elseif ($result.Trend -lt 0) { " ($($result.Trend)%)" }
            else { "" }
        } else {
            ""
        }
        
        Write-Output "[$statusSymbol] $($result.Module): $($result.Coverage)% (Target: $($result.Threshold)%)$gap$trend"
    }
    
    Write-Output ""
    Write-Output "----------------------------------------------"
    Write-Output "Summary: $passCount of $totalCount modules passed"
    
    if ($regressions.Count -gt 0) {
        Write-Output ""
        Write-Output "Coverage Regressions:"
        foreach ($reg in $regressions) {
            Write-Output "  - $($reg.Module): $($reg.Previous)% → $($reg.Current)% (-$($reg.Drop)%)"
        }
    }
    
    if (-not $allPassed) {
        Write-Output "----------------------------------------------"
        Write-Output "VALIDATION FAILED: $failCount module(s) below threshold or regressed"
    }
    Write-Output "=============================================="
}

# Exit with appropriate code
if ($allPassed) {
    # Save current coverage state for next run
    $currentState = @{}
    foreach ($result in $results) {
        $currentState[$result.Module] = $result.Coverage
    }
    $currentState | ConvertTo-Json | Set-Content $OutputStatePath -Encoding UTF8
    Write-Verbose "Coverage state saved to $OutputStatePath"
    
    exit 0
}
else {
    # Still save state even on failure for tracking
    $currentState = @{}
    foreach ($result in $results) {
        $currentState[$result.Module] = $result.Coverage
    }
    $currentState | ConvertTo-Json | Set-Content $OutputStatePath -Encoding UTF8
    Write-Verbose "Coverage state saved to $OutputStatePath (validation failed)"
    
    exit 1
}
