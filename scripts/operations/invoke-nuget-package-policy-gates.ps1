[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RepositoryRoot,

    [Parameter(Mandatory = $false)]
    [string]$ArtifactDirectory,

    [Parameter(Mandatory = $false)]
    [string]$StyleCopRegistrationUrl = 'https://api.nuget.org/v3/registration5-semver2/stylecop.analyzers/index.json'
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

function Get-PackageVersionText {
    param(
        [Parameter(Mandatory = $true)]
        [System.Xml.XmlElement]$Node
    )

    $version = [string]$Node.GetAttribute('Version')
    if ([string]::IsNullOrWhiteSpace($version)) {
        $versionNode = $Node.SelectSingleNode('./Version')
        if ($null -ne $versionNode) {
            $version = [string]$versionNode.InnerText
        }
    }

    return $version
}

function Resolve-MsBuildValue {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value,
        [Parameter(Mandatory = $true)]
        [hashtable]$PropertyMap
    )

    $resolved = $Value.Trim()
    $iteration = 0
    while ($iteration -lt 10) {
        $matches = [System.Text.RegularExpressions.Regex]::Matches($resolved, '\$\((?<name>[A-Za-z_][A-Za-z0-9_.-]*)\)')
        if (@($matches).Count -eq 0) {
            break
        }

        $replacementMade = $false
        foreach ($match in $matches) {
            $name = [string]$match.Groups['name'].Value
            if (-not $PropertyMap.ContainsKey($name)) {
                continue
            }

            $replacement = [string]$PropertyMap[$name]
            if ([string]::IsNullOrWhiteSpace($replacement)) {
                continue
            }

            $token = '$(' + $name + ')'
            if ($resolved.Contains($token)) {
                $resolved = $resolved.Replace($token, $replacement)
                $replacementMade = $true
            }
        }

        if (-not $replacementMade) {
            break
        }

        $iteration++
    }

    if ($resolved -match '\$\([A-Za-z_][A-Za-z0-9_.-]*\)') {
        return $null
    }

    return $resolved.Trim()
}

function Add-ContextFromFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        [Parameter(Mandatory = $true)]
        [hashtable]$PropertyMap,
        [Parameter(Mandatory = $true)]
        [hashtable]$PackageVersionMap
    )

    if (-not (Test-Path -Path $FilePath -PathType Leaf)) {
        return
    }

    [xml]$xml = Get-Content -Path $FilePath -Raw

    $propertyNodes = $xml.SelectNodes('/Project/PropertyGroup/*')
    foreach ($propertyNode in @($propertyNodes)) {
        if ($null -eq $propertyNode) {
            continue
        }

        if ($propertyNode -isnot [System.Xml.XmlElement]) {
            continue
        }

        if ($propertyNode.ChildNodes.Count -gt 1) {
            continue
        }

        $name = [string]$propertyNode.Name
        if ([string]::IsNullOrWhiteSpace($name)) {
            continue
        }

        $rawValue = [string]$propertyNode.InnerText
        if ([string]::IsNullOrWhiteSpace($rawValue)) {
            continue
        }

        $resolvedValue = Resolve-MsBuildValue -Value $rawValue -PropertyMap $PropertyMap
        if ([string]::IsNullOrWhiteSpace($resolvedValue)) {
            continue
        }

        $PropertyMap[$name] = $resolvedValue
    }

    $declarationNodes = @(
        @($xml.SelectNodes('//PackageVersion')) +
        @($xml.SelectNodes('//PackageReference[@Update]'))
    )

    foreach ($node in $declarationNodes) {
        if ($null -eq $node -or $node -isnot [System.Xml.XmlElement]) {
            continue
        }

        $packageId = [string]$node.GetAttribute('Include')
        if ([string]::IsNullOrWhiteSpace($packageId)) {
            $packageId = [string]$node.GetAttribute('Update')
        }

        if ([string]::IsNullOrWhiteSpace($packageId)) {
            continue
        }

        $rawVersion = Get-PackageVersionText -Node $node
        if ([string]::IsNullOrWhiteSpace($rawVersion)) {
            continue
        }

        $resolvedVersion = Resolve-MsBuildValue -Value $rawVersion -PropertyMap $PropertyMap
        if ([string]::IsNullOrWhiteSpace($resolvedVersion)) {
            continue
        }

        $PackageVersionMap[$packageId.Trim().ToLowerInvariant()] = $resolvedVersion
    }
}

function Get-ProjectVersionContext {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectFilePath,
        [Parameter(Mandatory = $true)]
        [string]$RootPath,
        [Parameter(Mandatory = $true)]
        [hashtable]$ContextCache
    )

    if ($ContextCache.ContainsKey($ProjectFilePath)) {
        return $ContextCache[$ProjectFilePath]
    }

    $propertyMap = @{}
    $packageVersionMap = @{}

    $rootBuildProps = Join-Path -Path $RootPath -ChildPath 'Directory.Build.props'
    $rootBuildTargets = Join-Path -Path $RootPath -ChildPath 'Directory.Build.targets'
    $rootPackagesProps = Join-Path -Path $RootPath -ChildPath 'Directory.Packages.props'

    Add-ContextFromFile -FilePath $rootBuildProps -PropertyMap $propertyMap -PackageVersionMap $packageVersionMap
    Add-ContextFromFile -FilePath $rootBuildTargets -PropertyMap $propertyMap -PackageVersionMap $packageVersionMap
    Add-ContextFromFile -FilePath $rootPackagesProps -PropertyMap $propertyMap -PackageVersionMap $packageVersionMap
    Add-ContextFromFile -FilePath $ProjectFilePath -PropertyMap $propertyMap -PackageVersionMap $packageVersionMap

    $context = [pscustomobject]@{
        PropertyMap = $propertyMap
        PackageVersionMap = $packageVersionMap
    }

    $ContextCache[$ProjectFilePath] = $context
    return $context
}

function Get-DeclarationFiles {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RootPath
    )

    $declarationPatterns = @('*.csproj', '*.props', '*.targets')
    $files = New-Object System.Collections.Generic.List[System.IO.FileInfo]

    $gitCommand = Get-Command -Name 'git' -ErrorAction SilentlyContinue
    if ($null -ne $gitCommand) {
        $gitFilePaths = @(& git -C $RootPath ls-files -- '*.csproj' '*.props' '*.targets' 2>$null)
        if (($LASTEXITCODE -eq 0) -and (@($gitFilePaths).Count -gt 0)) {
            foreach ($relativePath in $gitFilePaths) {
                if ([string]::IsNullOrWhiteSpace($relativePath)) {
                    continue
                }

                $fullPath = Join-Path -Path $RootPath -ChildPath $relativePath.Trim()
                if (Test-Path -Path $fullPath -PathType Leaf) {
                    $fileItem = Get-Item -Path $fullPath
                    if ($fileItem -is [System.IO.FileInfo]) {
                        $files.Add($fileItem)
                    }
                }
            }

            return @($files | Sort-Object -Property FullName -Unique)
        }
    }

    $excludedDirectories = @(
        '.git',
        '.worktrees',
        'bin',
        'obj',
        'artifacts',
        'TestResults',
        '.vs',
        'node_modules'
    )

    $escapedDirectoryPattern = ($excludedDirectories | ForEach-Object { [System.Text.RegularExpressions.Regex]::Escape($_) }) -join '|'
    $excludePattern = "[\\/]($escapedDirectoryPattern)[\\/]"

    return @(Get-ChildItem -Path $RootPath -Recurse -File |
            Where-Object {
                ($_.Name -like '*.csproj' -or $_.Name -like '*.props' -or $_.Name -like '*.targets') -and
                ($_.FullName -notmatch $excludePattern)
            } |
            Sort-Object -Property FullName -Unique)
}

function Get-PackageReferences {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RootPath
    )

    $files = Get-DeclarationFiles -RootPath $RootPath
    $references = New-Object System.Collections.Generic.List[object]
    $unresolved = New-Object System.Collections.Generic.List[object]
    $contextCache = @{}

    foreach ($file in $files) {
        [xml]$xml = Get-Content -Path $file.FullName -Raw
        $context = Get-ProjectVersionContext -ProjectFilePath $file.FullName -RootPath $RootPath -ContextCache $contextCache
        $nodeList = $xml.SelectNodes('//PackageReference')
        if ($null -eq $nodeList -or @($nodeList).Length -eq 0) {
            continue
        }

        foreach ($node in $nodeList) {
            if ($null -eq $node -or -not ($node -is [System.Xml.XmlElement])) {
                continue
            }

            $packageId = [string]$node.GetAttribute('Include')
            if ([string]::IsNullOrWhiteSpace($packageId)) {
                $packageId = [string]$node.GetAttribute('Update')
            }

            if ([string]::IsNullOrWhiteSpace($packageId)) {
                continue
            }

            $rawVersion = Get-PackageVersionText -Node $node
            $version = $null

            if (-not [string]::IsNullOrWhiteSpace($rawVersion)) {
                $version = Resolve-MsBuildValue -Value $rawVersion -PropertyMap $context.PropertyMap
            }

            if ([string]::IsNullOrWhiteSpace($version)) {
                $packageKey = $packageId.Trim().ToLowerInvariant()
                if ($context.PackageVersionMap.ContainsKey($packageKey)) {
                    $version = [string]$context.PackageVersionMap[$packageKey]
                }
            }

            if ([string]::IsNullOrWhiteSpace($version)) {
                $reason = if ([string]::IsNullOrWhiteSpace($rawVersion)) {
                    'missing Version on PackageReference and no matching local PackageVersion/PackageReference Update declaration found'
                }
                else {
                    "unable to resolve MSBuild properties in version '$rawVersion'"
                }

                $unresolved.Add([pscustomobject]@{
                        PackageId = $packageId.Trim()
                        FilePath = $file.FullName
                        Reason = $reason
                    })
                continue
            }

            $references.Add([pscustomobject]@{
                    PackageId = $packageId.Trim()
                    Version = $version.Trim()
                    FilePath = $file.FullName
                })
        }
    }

    if ($unresolved.Count -gt 0) {
        $messageLines = New-Object System.Collections.Generic.List[string]
        $messageLines.Add('Package policy gate could not determine package versions for one or more PackageReference entries.')
        $messageLines.Add('Action: set Version inline, or define a matching PackageVersion/PackageReference Update with Version in local declarations (for example Directory.Build.props).')
        $messageLines.Add('Unresolved entries:')
        foreach ($item in $unresolved) {
            $relativeFile = [System.IO.Path]::GetRelativePath($RootPath, $item.FilePath)
            $messageLines.Add("- $($item.PackageId) in ${relativeFile}: $($item.Reason)")
        }

        throw ($messageLines -join [Environment]::NewLine)
    }

    return $references
}

function Test-IsPreReleaseVersion {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Version
    )

    return $Version -match '^[0-9]+\.[0-9]+\.[0-9]+-[0-9A-Za-z\.-]+'
}

function Parse-SemVer {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Version
    )

    if ($Version -notmatch '^(?<major>0|[1-9][0-9]*)\.(?<minor>0|[1-9][0-9]*)\.(?<patch>0|[1-9][0-9]*)(?:-(?<pre>[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+(?<build>[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?$') {
        throw "Invalid SemVer version: $Version"
    }

    return [pscustomobject]@{
        Original = $Version
        Major = [int]$Matches.major
        Minor = [int]$Matches.minor
        Patch = [int]$Matches.patch
        PreRelease = if ([string]::IsNullOrWhiteSpace($Matches.pre)) { @() } else { $Matches.pre.Split('.') }
        HasPreRelease = -not [string]::IsNullOrWhiteSpace($Matches.pre)
    }
}

function Compare-SemVer {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Left,
        [Parameter(Mandatory = $true)]
        [string]$Right
    )

    $leftParsed = Parse-SemVer -Version $Left
    $rightParsed = Parse-SemVer -Version $Right

    foreach ($name in @('Major', 'Minor', 'Patch')) {
        if ($leftParsed.$name -lt $rightParsed.$name) {
            return -1
        }

        if ($leftParsed.$name -gt $rightParsed.$name) {
            return 1
        }
    }

    if (-not $leftParsed.HasPreRelease -and -not $rightParsed.HasPreRelease) {
        return 0
    }

    if (-not $leftParsed.HasPreRelease -and $rightParsed.HasPreRelease) {
        return 1
    }

    if ($leftParsed.HasPreRelease -and -not $rightParsed.HasPreRelease) {
        return -1
    }

    $leftPreReleaseCount = @($leftParsed.PreRelease).Length
    $rightPreReleaseCount = @($rightParsed.PreRelease).Length

    $index = 0
    while ($index -lt $leftPreReleaseCount -and $index -lt $rightPreReleaseCount) {
        $leftIdentifier = $leftParsed.PreRelease[$index]
        $rightIdentifier = $rightParsed.PreRelease[$index]

        if ($leftIdentifier -eq $rightIdentifier) {
            $index++
            continue
        }

        $leftNumeric = $leftIdentifier -match '^[0-9]+$'
        $rightNumeric = $rightIdentifier -match '^[0-9]+$'

        if ($leftNumeric -and $rightNumeric) {
            $leftNumber = [long]$leftIdentifier
            $rightNumber = [long]$rightIdentifier
            if ($leftNumber -lt $rightNumber) {
                return -1
            }

            if ($leftNumber -gt $rightNumber) {
                return 1
            }
        }
        elseif ($leftNumeric -and -not $rightNumeric) {
            return -1
        }
        elseif (-not $leftNumeric -and $rightNumeric) {
            return 1
        }
        else {
            $comparison = [string]::CompareOrdinal($leftIdentifier, $rightIdentifier)
            if ($comparison -lt 0) {
                return -1
            }

            if ($comparison -gt 0) {
                return 1
            }
        }

        $index++
    }

    if ($leftPreReleaseCount -lt $rightPreReleaseCount) {
        return -1
    }

    if ($leftPreReleaseCount -gt $rightPreReleaseCount) {
        return 1
    }

    return 0
}

function Get-ObjectPropertyValue {
    param(
        [Parameter(Mandatory = $false)]
        [object]$InputObject,
        [Parameter(Mandatory = $true)]
        [string]$PropertyName
    )

    if ($null -eq $InputObject) {
        return $null
    }

    $property = $InputObject.PSObject.Properties[$PropertyName]
    if ($null -eq $property) {
        return $null
    }

    return $property.Value
}

function Invoke-RestMethodSafe {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Uri,
        [Parameter(Mandatory = $false)]
        [int]$MaximumRetryCount = 2,
        [Parameter(Mandatory = $false)]
        [int]$RetryIntervalSec = 2
    )

    try {
        return Invoke-RestMethod -Uri $Uri -Method Get -MaximumRetryCount $MaximumRetryCount -RetryIntervalSec $RetryIntervalSec
    }
    catch {
        return $null
    }
}

function Get-RegistrationCandidates {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RegistrationUrl,
        [Parameter(Mandatory = $true)]
        [string]$PackageId
    )

    $candidates = New-Object System.Collections.Generic.List[string]

    if (-not [string]::IsNullOrWhiteSpace($RegistrationUrl)) {
        $candidates.Add($RegistrationUrl)
    }

    $normalizedPackageId = $PackageId.ToLowerInvariant()
    $wellKnownBases = @(
        'https://api.nuget.org/v3/registration5-gz-semver2/',
        'https://api.nuget.org/v3/registration5-semver2/'
    )

    foreach ($baseUrl in $wellKnownBases) {
        $candidates.Add("$baseUrl$normalizedPackageId/index.json")
    }

    $serviceIndex = Invoke-RestMethodSafe -Uri 'https://api.nuget.org/v3/index.json'
    if ($null -ne $serviceIndex) {
        $resources = @()
        $indexResources = Get-ObjectPropertyValue -InputObject $serviceIndex -PropertyName 'resources'
        if ($null -ne $indexResources) {
            $resources = @($indexResources)
        }

        foreach ($resource in $resources) {
            $resourceType = [string](Get-ObjectPropertyValue -InputObject $resource -PropertyName '@type')
            if ($resourceType -notlike 'RegistrationsBaseUrl*') {
                continue
            }

            $resourceId = [string](Get-ObjectPropertyValue -InputObject $resource -PropertyName '@id')
            if ([string]::IsNullOrWhiteSpace($resourceId)) {
                continue
            }

            if (-not $resourceId.EndsWith('/')) {
                $resourceId = "$resourceId/"
            }

            $candidates.Add("$resourceId$normalizedPackageId/index.json")
        }
    }

    return @($candidates | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique)
}

function Get-RegistrationVersions {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RegistrationUrl,
        [Parameter(Mandatory = $true)]
        [string]$PackageId
    )

    $response = $null
    $attempted = New-Object System.Collections.Generic.List[string]
    $candidates = Get-RegistrationCandidates -RegistrationUrl $RegistrationUrl -PackageId $PackageId
    foreach ($candidate in $candidates) {
        $attempted.Add($candidate)
        $candidateResponse = Invoke-RestMethodSafe -Uri $candidate
        if ($null -ne $candidateResponse) {
            $response = $candidateResponse
            break
        }
    }

    if ($null -eq $response) {
        $attemptedText = $attempted -join '; '
        throw "Unable to retrieve NuGet registration metadata. Attempted endpoints: $attemptedText"
    }

    $versions = New-Object System.Collections.Generic.List[string]
    $pages = @()
    $responseItems = Get-ObjectPropertyValue -InputObject $response -PropertyName 'items'
    if ($null -ne $responseItems) {
        $pages = @($responseItems)
    }

    foreach ($page in $pages) {
        $leafItems = @()
        $pageItems = Get-ObjectPropertyValue -InputObject $page -PropertyName 'items'
        if ($null -ne $pageItems) {
            $leafItems = @($pageItems)
        }

        if (@($leafItems).Length -eq 0) {
            $pageId = [string](Get-ObjectPropertyValue -InputObject $page -PropertyName '@id')
            if (-not [string]::IsNullOrWhiteSpace($pageId)) {
                $pageResponse = Invoke-RestMethodSafe -Uri $pageId
                if ($null -eq $pageResponse) {
                    continue
                }

                $pageResponseItems = Get-ObjectPropertyValue -InputObject $pageResponse -PropertyName 'items'
                if ($null -ne $pageResponseItems) {
                    $leafItems = @($pageResponseItems)
                }
            }
        }

        foreach ($leaf in $leafItems) {
            $catalogEntry = Get-ObjectPropertyValue -InputObject $leaf -PropertyName 'catalogEntry'
            if ($null -eq $catalogEntry) {
                continue
            }

            $listed = Get-ObjectPropertyValue -InputObject $catalogEntry -PropertyName 'listed'
            if ($listed -eq $false) {
                continue
            }

            $version = [string](Get-ObjectPropertyValue -InputObject $catalogEntry -PropertyName 'version')
            if ([string]::IsNullOrWhiteSpace($version)) {
                continue
            }

            $versions.Add($version)
        }
    }

    return $versions
}

$resolvedRoot = (Resolve-Path -Path $RepositoryRoot).Path
$resolvedArtifactDirectory = if ([string]::IsNullOrWhiteSpace($ArtifactDirectory)) {
    Join-Path -Path $resolvedRoot -ChildPath 'artifacts/nuget-audit'
}
else {
    [string](Resolve-Path -Path $ArtifactDirectory -ErrorAction SilentlyContinue)
}

if ([string]::IsNullOrWhiteSpace($resolvedArtifactDirectory)) {
    $resolvedArtifactDirectory = [System.IO.Path]::GetFullPath($ArtifactDirectory)
}

if (-not (Test-Path -Path $resolvedArtifactDirectory -PathType Container)) {
    New-Item -Path $resolvedArtifactDirectory -ItemType Directory -Force | Out-Null
}

$preReleaseLogPath = Join-Path -Path $resolvedArtifactDirectory -ChildPath 'prerelease-policy.log'
$styleCopLogPath = Join-Path -Path $resolvedArtifactDirectory -ChildPath 'stylecop-latest-preview.log'

$preReleaseExit = 0
$styleCopExit = 0

try {
    $packageReferences = Get-PackageReferences -RootPath $resolvedRoot

    $preReleaseViolations = @($packageReferences |
        Where-Object {
            (Test-IsPreReleaseVersion -Version $_.Version) -and
            ($_.PackageId -ne 'StyleCop.Analyzers')
        } |
        Sort-Object -Property PackageId, Version, FilePath -Unique)

    if ($preReleaseViolations.Count -gt 0) {
        $preReleaseExit = 1
        $lines = New-Object System.Collections.Generic.List[string]
        $lines.Add('Pre-release policy gate failed: only StyleCop.Analyzers may use a pre-release version.')
        $lines.Add('Violations:')
        foreach ($item in $preReleaseViolations) {
            $relativeFile = [System.IO.Path]::GetRelativePath($resolvedRoot, $item.FilePath)
            $lines.Add("- $($item.PackageId) $($item.Version) in $relativeFile")
        }

        Set-Content -Path $preReleaseLogPath -Value ($lines -join [Environment]::NewLine) -Encoding utf8NoBOM
    }
    else {
        Set-Content -Path $preReleaseLogPath -Value 'Pre-release policy gate passed: no unauthorized pre-release package references were found.' -Encoding utf8NoBOM
    }
}
catch {
    $preReleaseExit = 2
    Set-Content -Path $preReleaseLogPath -Value "Pre-release policy gate encountered an error: $($_.Exception.Message)" -Encoding utf8NoBOM
}

try {
    $styleCopReferences = @(Get-PackageReferences -RootPath $resolvedRoot |
        Where-Object { $_.PackageId -eq 'StyleCop.Analyzers' })

    if ($styleCopReferences.Count -eq 0) {
        throw 'No StyleCop.Analyzers package references were found.'
    }

    $distinctPinnedVersions = @($styleCopReferences.Version | Sort-Object -Unique)
    if ($distinctPinnedVersions.Count -ne 1) {
        $distinct = $distinctPinnedVersions -join ', '
        throw "Multiple StyleCop.Analyzers versions were found: $distinct"
    }

    $pinnedVersion = [string]$distinctPinnedVersions[0]
    if (-not (Test-IsPreReleaseVersion -Version $pinnedVersion)) {
        throw "Pinned StyleCop.Analyzers version is not pre-release: $pinnedVersion"
    }

    $registrationVersions = @(Get-RegistrationVersions -RegistrationUrl $StyleCopRegistrationUrl -PackageId 'StyleCop.Analyzers')
    $previewVersions = @($registrationVersions |
        Where-Object { Test-IsPreReleaseVersion -Version $_ } |
        Sort-Object -Unique)

    if ($previewVersions.Count -eq 0) {
        throw 'No listed pre-release versions were found in nuget.org registration metadata.'
    }

    $latestPreview = [string]$previewVersions[0]
    foreach ($candidate in $previewVersions) {
        if ((Compare-SemVer -Left $candidate -Right $latestPreview) -gt 0) {
            $latestPreview = [string]$candidate
        }
    }

    $comparison = Compare-SemVer -Left $pinnedVersion -Right $latestPreview
    if ($comparison -ne 0) {
        $styleCopExit = 1
        $message = @(
            'StyleCop latest-preview gate failed.'
            "Pinned version: $pinnedVersion"
            "Latest listed preview: $latestPreview"
            "Registration source: $StyleCopRegistrationUrl"
        ) -join [Environment]::NewLine
        Set-Content -Path $styleCopLogPath -Value $message -Encoding utf8NoBOM
    }
    else {
        $message = @(
            'StyleCop latest-preview gate passed.'
            "Pinned version: $pinnedVersion"
            "Latest listed preview: $latestPreview"
            "Registration source: $StyleCopRegistrationUrl"
        ) -join [Environment]::NewLine
        Set-Content -Path $styleCopLogPath -Value $message -Encoding utf8NoBOM
    }
}
catch {
    $styleCopExit = 2
    Set-Content -Path $styleCopLogPath -Value "StyleCop latest-preview gate encountered an error: $($_.Exception.Message)" -Encoding utf8NoBOM
}

Set-GitHubOutput -Name 'prerelease_exit' -Value ([string]$preReleaseExit)
Set-GitHubOutput -Name 'stylecop_preview_exit' -Value ([string]$styleCopExit)

if (($preReleaseExit -ne 0) -or ($styleCopExit -ne 0)) {
    exit 1
}

exit 0