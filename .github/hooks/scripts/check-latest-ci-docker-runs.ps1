param()

# Read and ignore hook payload from stdin when provided by hook runtime.
if ([Console]::IsInputRedirected) {
    $null = [Console]::In.ReadToEnd()
}

function Write-HookOutput {
    param(
        [string]$SystemMessage
    )

    $output = @{
        continue = $true
    }

    if (-not [string]::IsNullOrWhiteSpace($SystemMessage)) {
        $output.systemMessage = $SystemMessage
    }

    $output | ConvertTo-Json -Depth 5 -Compress
}

function Get-LatestRunForFilter {
    param(
        [System.Collections.IEnumerable]$Runs,
        [scriptblock]$Filter
    )

    $filtered = @($Runs | Where-Object $Filter | Sort-Object { [DateTime]$_.createdAt } -Descending)
    if ($filtered.Count -gt 0) {
        return $filtered[0]
    }

    return $null
}

function Get-LatestRunForWorkflow {
    param(
        [string]$WorkflowName
    )

    $runJson = gh run list --workflow "$WorkflowName" --branch "main" --limit 1 --json databaseId,name,workflowName,status,conclusion,createdAt 2>$null
    if ([string]::IsNullOrWhiteSpace($runJson)) {
        return $null
    }

    $workflowRuns = $runJson | ConvertFrom-Json
    if ($null -eq $workflowRuns) {
        return $null
    }

    $runList = @($workflowRuns)
    if ($runList.Count -eq 0) {
        return $null
    }

    return $runList[0]
}

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-HookOutput -SystemMessage "SessionStart check: GitHub CLI (gh) is not installed, so CI/Docker run status could not be checked."
    exit 0
}

try {
    $ciWorkflowName = 'CI'
    $dockerWorkflowName = 'Build and Publish Docker Images'

    $latestCi = Get-LatestRunForWorkflow -WorkflowName $ciWorkflowName
    $latestDocker = Get-LatestRunForWorkflow -WorkflowName $dockerWorkflowName

    $failedChecks = @()
    $warnings = @()

    if ($null -ne $latestCi) {
        if ($latestCi.status -eq 'completed' -and $latestCi.conclusion -eq 'failure') {
            $failedChecks += "CI failed (run $($latestCi.databaseId), workflow '$($latestCi.workflowName)', name '$($latestCi.name)')."
        }
    }
    else {
        $warnings += "CI workflow/run could not be determined."
    }

    if ($null -ne $latestDocker) {
        if ($latestDocker.status -eq 'completed' -and $latestDocker.conclusion -eq 'failure') {
            $failedChecks += "Docker workflow failed (run $($latestDocker.databaseId), workflow '$($latestDocker.workflowName)', name '$($latestDocker.name)')."
        }
    }
    else {
        $warnings += "No run history found for workflow '$dockerWorkflowName' on main branch."
    }

    if ($failedChecks.Count -gt 0) {
        $message = "SessionStart run health check: " + ($failedChecks -join ' ')
        Write-HookOutput -SystemMessage $message
        exit 0
    }

    if ($warnings.Count -gt 0) {
        $message = "SessionStart run health check: " + ($warnings -join ' ')
        Write-HookOutput -SystemMessage $message
        exit 0
    }

    Write-HookOutput
    exit 0
}
catch {
    Write-HookOutput -SystemMessage "SessionStart check: Failed to query GitHub runs (`$($_.Exception.Message))."
    exit 0
}
