[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RepositoryRoot,

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[a-z0-9\-]+$')]
    [string]$TriggerSource,

    [Parameter(Mandatory = $false)]
    [ValidatePattern('^\d{4}-\d{2}$')]
    [string]$Cycle = (Get-Date -AsUtc -Format 'yyyy-MM'),

    [Parameter(Mandatory = $false)]
    [switch]$RequiredUpgradesDetected
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Set-GitHubOutput {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    if ([string]::IsNullOrWhiteSpace($env:GITHUB_OUTPUT)) {
        return
    }

    Add-Content -Path $env:GITHUB_OUTPUT -Value "$Name=$Value"
}

function Get-NextFeatureNumber {
    param(
        [Parameter(Mandatory = $true)]
        [string]$DocsPath
    )

    $maxNumber = 0
    foreach ($file in Get-ChildItem -Path $DocsPath -Filter '*.md' -File) {
        if ($file.BaseName -match '^(\d+)-') {
            $currentNumber = [int]$Matches[1]
            if ($currentNumber -gt $maxNumber) {
                $maxNumber = $currentNumber
            }
        }
    }

    return ($maxNumber + 1)
}

$resolvedRoot = (Resolve-Path -Path $RepositoryRoot).Path
$docsPath = Join-Path -Path $resolvedRoot -ChildPath 'docs'

if (-not (Test-Path -Path $docsPath -PathType Container)) {
    throw "Docs path not found: $docsPath"
}

$normalizedSource = $TriggerSource.Trim().ToLowerInvariant()

if (-not $RequiredUpgradesDetected.IsPresent) {
    Set-GitHubOutput -Name 'doc_created' -Value 'false'
    Set-GitHubOutput -Name 'doc_path' -Value ''
    Set-GitHubOutput -Name 'doc_cycle' -Value $Cycle
    Set-GitHubOutput -Name 'doc_trigger_source' -Value $normalizedSource

    Write-Host "Required upgrades not detected; skipping upgrade cycle document generation for cycle ${Cycle}."
    exit 0
}

$existingGeneratedDocs = Get-ChildItem -Path $docsPath -Filter '*-nuget-upgrade-cycle-????-??.md' -File

foreach ($doc in $existingGeneratedDocs) {
    $docContent = Get-Content -Path $doc.FullName -Raw
    if ($docContent -match "Upgrade Cycle:\s*$Cycle") {
        $relativePath = "docs/$($doc.Name)"
        Set-GitHubOutput -Name 'doc_created' -Value 'false'
        Set-GitHubOutput -Name 'doc_path' -Value $relativePath
        Set-GitHubOutput -Name 'doc_cycle' -Value $Cycle
        Set-GitHubOutput -Name 'doc_trigger_source' -Value $normalizedSource

        Write-Host "Upgrade cycle document already exists for cycle ${Cycle}: $relativePath"
        exit 0
    }
}

$nextFeatureNumber = Get-NextFeatureNumber -DocsPath $docsPath
$fileName = "$nextFeatureNumber-nuget-upgrade-cycle-$Cycle.md"
$relativePath = "docs/$fileName"
$filePath = Join-Path -Path $resolvedRoot -ChildPath $relativePath
$generatedOnUtc = Get-Date -AsUtc -Format 'yyyy-MM-dd HH:mm:ss'

$template = @"
# Feature $($nextFeatureNumber): NuGet Upgrade Cycle $Cycle

> **Status:** Planning

## Overview

This feature tracks the NuGet upgrade cycle for $Cycle. It is generated automatically from CI when the monthly schedule runs or when Dependabot pull request activity is detected.

## Trigger Metadata

- Upgrade Cycle: $Cycle
- Trigger Source: $normalizedSource
- Generated On (UTC): $generatedOnUtc

## Policy Context (Feature 167)

- Stable package versions are required across the solution.
- `StyleCop.Analyzers` is the only allowed pre-release package and must stay on the latest preview.
- Vulnerability checks must include direct and transitive dependencies.

## Upgrade Tasks

- [ ] Review restore vulnerability gate output.
- [ ] Review vulnerable package report (`dotnet list package --vulnerable --include-transitive`).
- [ ] Review outdated package report (`dotnet list package --outdated --include-transitive`).
- [ ] Prepare update plan that keeps stable-only policy compliance.
- [ ] Confirm `StyleCop.Analyzers` remains on latest preview.
- [ ] Capture rollback notes and required follow-up actions.

## Definition of Done

- [ ] Audit artifacts are attached to the workflow run.
- [ ] Upgrade plan is documented and linked to implementation work.
- [ ] Feature 167 package policy compliance is confirmed.
"@

Set-Content -Path $filePath -Value $template -Encoding utf8NoBOM
Write-Host "Created upgrade cycle document: $relativePath"

Set-GitHubOutput -Name 'doc_created' -Value 'true'
Set-GitHubOutput -Name 'doc_path' -Value $relativePath
Set-GitHubOutput -Name 'doc_cycle' -Value $Cycle
Set-GitHubOutput -Name 'doc_trigger_source' -Value $normalizedSource
