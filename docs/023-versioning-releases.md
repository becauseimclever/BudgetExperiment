# Feature 023: Semantic Versioning & Release Management

## Overview

Implement a comprehensive versioning strategy using Semantic Versioning (SemVer) that automatically flows into Docker image tags, GitHub Releases, and runtime version reporting. This enables clear communication of breaking changes, feature additions, and bug fixes across deployments.

## Problem Statement

Currently:
- Version is hardcoded as `0.1.0-preview` in the API project
- No automated correlation between Git tags, Docker images, and GitHub Releases
- Users cannot easily determine which version is deployed
- No changelog or release notes mechanism

### Target State

After implementation:
- Single source of truth for version (Git tags)
- Automated Docker image tagging with semver patterns
- GitHub Releases created automatically with changelogs
- API exposes version information via health/info endpoint
- Client displays current version in UI

---

## Semantic Versioning Strategy

### Version Format

```
MAJOR.MINOR.PATCH[-PRERELEASE][+BUILD]
```

| Component | When to Increment | Example |
|-----------|-------------------|---------|
| **MAJOR** | Breaking changes, significant feature overhauls | `2.0.0`, `3.0.0` |
| **MINOR** | New features, backward-compatible additions | `1.1.0`, `2.3.0` |
| **PATCH** | Bug fixes, minor improvements | `1.0.1`, `2.3.5` |
| **PRERELEASE** | Pre-release versions (optional) | `2.0.0-beta.1`, `3.0.0-rc.1` |
| **BUILD** | Build metadata (optional, not for comparison) | `1.0.0+20260115` |

### Planned Major Releases

| Version | Milestone | Key Features |
|---------|-----------|--------------|
| **1.0.0** | Initial Release | Current feature set (transactions, recurring, transfers, settings) |
| **2.0.0** | Budget Categories | Category management, budget goals, spending tracking (Feature 021) |
| **3.0.0** | Multi-User Auth | Authentik integration, shared/personal budgets (Feature 022) |

### Pre-release Conventions

- `alpha` - Early development, unstable APIs
- `beta` - Feature complete, testing phase
- `rc` - Release candidate, final testing
- `preview` - Current state before 1.0

Example progression: `2.0.0-alpha.1` → `2.0.0-beta.1` → `2.0.0-rc.1` → `2.0.0`

---

## Technical Implementation

### 1. Version Source of Truth

**Git Tags** are the single source of truth for versioning.

```bash
# Create a release tag
git tag -a v2.0.0 -m "Release 2.0.0 - Budget Categories"
git push origin v2.0.0

# Create a pre-release tag
git tag -a v2.0.0-beta.1 -m "Beta release for Budget Categories"
git push origin v2.0.0-beta.1
```

### 2. .NET Version Injection

#### Option A: MSBuild Property via CI (Recommended)

Pass version to `dotnet build` via command line:

```yaml
# In GitHub Actions workflow
- name: Build with version
  run: dotnet build -p:Version=${{ steps.version.outputs.version }}
```

#### Option B: Directory.Build.props with Git Versioning

Use [MinVer](https://github.com/adamralph/minver) or similar for automatic versioning:

```xml
<!-- Directory.Build.props -->
<PropertyGroup>
  <!-- MinVer automatically derives version from Git tags -->
  <MinVerDefaultPreReleaseIdentifiers>preview</MinVerDefaultPreReleaseIdentifiers>
  <MinVerTagPrefix>v</MinVerTagPrefix>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="MinVer" Version="6.0.0" PrivateAssets="all" />
</ItemGroup>
```

MinVer automatically:
- Uses the nearest Git tag as the version
- Increments patch + adds commit count for commits after a tag
- Works locally and in CI without additional configuration

### 3. Docker Image Tagging (Already Configured)

The existing CI workflow already extracts semver tags:

```yaml
# .github/workflows/docker-build-publish.yml (existing)
- name: Extract metadata (tags, labels)
  id: meta
  uses: docker/metadata-action@v5
  with:
    images: ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}
    tags: |
      type=ref,event=branch
      type=semver,pattern={{version}}      # v2.0.0 → 2.0.0
      type=semver,pattern={{major}}.{{minor}}  # v2.0.0 → 2.0
      type=semver,pattern={{major}}        # v2.0.0 → 2
      type=sha,prefix=
      type=raw,value=latest,enable={{is_default_branch}}
```

When you push `v2.0.0` tag, Docker images are tagged:
- `ghcr.io/becauseimclever/budgetexperiment:2.0.0`
- `ghcr.io/becauseimclever/budgetexperiment:2.0`
- `ghcr.io/becauseimclever/budgetexperiment:2`
- `ghcr.io/becauseimclever/budgetexperiment:latest`

### 4. GitHub Releases Automation

Create a new workflow for automated releases:

```yaml
# .github/workflows/release.yml
name: Create Release

on:
  push:
    tags:
      - 'v*'

permissions:
  contents: write

jobs:
  release:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - name: Generate Changelog
        id: changelog
        uses: orhun/git-cliff-action@v3
        with:
          config: cliff.toml
          args: --latest --strip header
        env:
          OUTPUT: CHANGELOG.md

      - name: Create GitHub Release
        uses: softprops/action-gh-release@v2
        with:
          body: ${{ steps.changelog.outputs.content }}
          draft: false
          prerelease: ${{ contains(github.ref, '-alpha') || contains(github.ref, '-beta') || contains(github.ref, '-rc') }}
          generate_release_notes: true
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

### 5. Changelog Configuration

Create `cliff.toml` for conventional commit changelog generation:

```toml
# cliff.toml
[changelog]
header = """
# Changelog\n
All notable changes to Budget Experiment.\n
"""
body = """
{% if version %}\
    ## [{{ version | trim_start_matches(pat="v") }}] - {{ timestamp | date(format="%Y-%m-%d") }}
{% else %}\
    ## [Unreleased]
{% endif %}\
{% for group, commits in commits | group_by(attribute="group") %}
    ### {{ group | striptags | trim | upper_first }}
    {% for commit in commits %}
        - {% if commit.scope %}**{{ commit.scope }}:** {% endif %}\
            {{ commit.message | upper_first }}\
            {% if commit.github.username %} by @{{ commit.github.username }}{%- endif %}\
    {% endfor %}
{% endfor %}\n
"""
footer = ""
trim = true

[git]
conventional_commits = true
filter_unconventional = true
split_commits = false
commit_parsers = [
  { message = "^feat", group = "Features" },
  { message = "^fix", group = "Bug Fixes" },
  { message = "^doc", group = "Documentation" },
  { message = "^perf", group = "Performance" },
  { message = "^refactor", group = "Refactoring" },
  { message = "^style", group = "Styling" },
  { message = "^test", group = "Testing" },
  { message = "^chore", group = "Miscellaneous" },
]
filter_commits = false
tag_pattern = "v[0-9].*"
```

### 6. Version Info API Endpoint

Expose version information via the API:

```csharp
// GET /api/version or included in /health
{
  "version": "2.0.0",
  "buildDate": "2026-01-15T10:30:00Z",
  "commitHash": "abc1234",
  "environment": "Production"
}
```

Implementation:

```csharp
// VersionController.cs or extend health endpoint
public record VersionInfo(
    string Version,
    DateTime BuildDateUtc,
    string? CommitHash,
    string Environment);

app.MapGet("/api/version", (IHostEnvironment env) =>
{
    var assembly = typeof(Program).Assembly;
    var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
        ?.InformationalVersion ?? "unknown";
    
    return new VersionInfo(
        Version: version,
        BuildDateUtc: DateTime.UtcNow, // Or embed at build time
        CommitHash: Environment.GetEnvironmentVariable("GIT_COMMIT"),
        Environment: env.EnvironmentName);
});
```

### 7. Client Version Display

Display version in the Blazor client footer or settings page:

```razor
@* In MainLayout.razor or Footer component *@
<div class="app-version">
    v@VersionService.CurrentVersion
</div>
```

```csharp
// VersionService.cs
public class VersionService
{
    private readonly HttpClient _http;
    
    public string CurrentVersion { get; private set; } = "loading...";
    
    public async Task LoadVersionAsync()
    {
        var info = await _http.GetFromJsonAsync<VersionInfo>("/api/version");
        CurrentVersion = info?.Version ?? "unknown";
    }
}
```

---

## Release Workflow

### Creating a Release

1. **Ensure all features merged** to `main`
2. **Update CHANGELOG.md** (or rely on auto-generation)
3. **Create and push tag**:
   ```bash
   git checkout main
   git pull origin main
   git tag -a v2.0.0 -m "Release 2.0.0 - Budget Categories & Goals"
   git push origin v2.0.0
   ```
4. **CI automatically**:
   - Builds Docker images with version tags
   - Creates GitHub Release with changelog
   - Publishes to ghcr.io

### Pre-release Workflow

For beta testing:

```bash
git tag -a v2.0.0-beta.1 -m "Beta 1: Budget Categories testing"
git push origin v2.0.0-beta.1
```

This creates:
- Docker image: `ghcr.io/becauseimclever/budgetexperiment:2.0.0-beta.1`
- GitHub Release marked as "Pre-release"

### Hotfix Workflow

For urgent fixes to a released version:

```bash
# Create hotfix branch from tag
git checkout -b hotfix/2.0.1 v2.0.0
# Make fixes, commit
git commit -m "fix: critical bug in budget calculation"
# Tag and push
git tag -a v2.0.1 -m "Hotfix: Budget calculation"
git push origin v2.0.1
git push origin hotfix/2.0.1
# Merge back to main
git checkout main
git merge hotfix/2.0.1
```

---

## Implementation Tasks

### Phase 1: Version Infrastructure

- [ ] Add MinVer package to Directory.Build.props
- [ ] Remove hardcoded version from BudgetExperiment.Api.csproj
- [ ] Create initial v1.0.0 tag after current features stabilize
- [ ] Verify Docker image tagging works with semver tags

### Phase 2: Release Automation

- [ ] Create `.github/workflows/release.yml` for GitHub Releases
- [ ] Add `cliff.toml` for changelog generation
- [ ] Configure conventional commit linting (optional)

### Phase 3: Runtime Version Exposure

- [ ] Add `/api/version` endpoint
- [ ] Include version in health check response
- [ ] Embed build metadata (commit hash, build date) via CI
- [ ] Display version in Blazor client UI

### Phase 4: Documentation

- [ ] Document release process in CONTRIBUTING.md or docs/
- [ ] Create CHANGELOG.md with initial entries
- [ ] Update README with version badge

---

## Docker Compose Version Pinning

For production deployments, pin to specific versions:

```yaml
# docker-compose.pi.yml
services:
  budgetexperiment:
    image: ghcr.io/becauseimclever/budgetexperiment:2.0.0  # Pinned version
    # or for auto-updates within major version:
    image: ghcr.io/becauseimclever/budgetexperiment:2      # Latest 2.x
```

### Version Selection Strategy

| Tag | Use Case | Auto-Updates |
|-----|----------|--------------|
| `latest` | Development/testing | All updates |
| `2` | Production, major version lock | Minor + patch |
| `2.0` | Production, minor version lock | Patch only |
| `2.0.0` | Production, exact version | None |

---

## Conventional Commits

To maximize changelog automation, follow conventional commit format:

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

### Types

| Type | Description | SemVer Impact |
|------|-------------|---------------|
| `feat` | New feature | Minor |
| `fix` | Bug fix | Patch |
| `docs` | Documentation only | None |
| `style` | Formatting, no logic change | None |
| `refactor` | Code change, no feature/fix | None |
| `perf` | Performance improvement | Patch |
| `test` | Adding/fixing tests | None |
| `chore` | Build, CI, dependencies | None |

### Breaking Changes

Append `!` or add `BREAKING CHANGE:` footer:

```
feat!: redesign category API

BREAKING CHANGE: Category endpoints now require authentication
```

---

## Version History (Planned)

| Version | Status | Features |
|---------|--------|----------|
| 0.1.0-preview | Current | Initial development |
| 1.0.0 | Planned | Transaction management, recurring items, transfers, settings |
| 1.1.0 | Planned | UI polish, running balance, allocation improvements |
| 2.0.0 | Planned | Budget categories & goals (Feature 021) |
| 2.1.0 | Planned | Category analytics, reports |
| 3.0.0 | Planned | Authentik auth, multi-user (Feature 022) |
| 3.1.0 | Planned | User preferences sync, sharing features |

---

## Testing Version Integration

### Verify Version Extraction

```bash
# After implementing MinVer
dotnet build
# Check output for version

# Or query assembly
dotnet run --project src/BudgetExperiment.Api
curl http://localhost:5099/api/version
```

### Verify Docker Tags

```bash
# After pushing a tag
# Check GitHub Container Registry for expected tags
# ghcr.io/becauseimclever/budgetexperiment:2.0.0
# ghcr.io/becauseimclever/budgetexperiment:2.0
# ghcr.io/becauseimclever/budgetexperiment:2
```

---

## Security Considerations

- **Never expose** internal paths or sensitive build info in version endpoint
- **Commit hashes** are public (Git is public), safe to expose
- **Environment names** should be generic (Production, Staging, Development)
- Consider **rate limiting** version endpoint if public

---

## References

- [Semantic Versioning 2.0.0](https://semver.org/)
- [MinVer - Minimalistic Versioning](https://github.com/adamralph/minver)
- [Git-Cliff Changelog Generator](https://github.com/orhun/git-cliff)
- [Conventional Commits](https://www.conventionalcommits.org/)
- [Docker Metadata Action](https://github.com/docker/metadata-action)
- [GitHub Release Action](https://github.com/softprops/action-gh-release)
