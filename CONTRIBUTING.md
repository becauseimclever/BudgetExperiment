# Contributing to Budget Experiment

Thank you for considering contributing to Budget Experiment! This document outlines the development practices and release process.

## 📋 Development Guidelines

Please review the comprehensive engineering guide in [`.github/copilot-instructions.md`](.github/copilot-instructions.md) for:

- Architecture principles (Clean/Onion hybrid)
- Naming conventions
- TDD workflow requirements
- SOLID enforcement
- REST API design patterns
- Testing strategy

## 🔄 Development Workflow

### 1. Create a Feature Branch

```bash
git checkout main
git pull origin main
git checkout -b feature/your-feature-name
```

### 2. Follow TDD (Test-Driven Development)

1. **RED**: Write a failing unit test first
2. **GREEN**: Implement minimal code to pass
3. **REFACTOR**: Clean up while keeping tests green

### 3. Run Tests

```bash
# Run all tests
dotnet test

# Run specific project tests
dotnet test tests/BudgetExperiment.Domain.Tests
```

### 4. Format Code

```bash
dotnet format
```

### 5. Commit with Conventional Commits

Use [Conventional Commits](https://www.conventionalcommits.org/) format for automatic changelog generation:

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

**Types:**
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
| `ci` | CI/CD changes | None |

**Breaking Changes:** Append `!` after the type:
```
feat!: redesign category API

BREAKING CHANGE: Category endpoints now require authentication
```

**Examples:**
```bash
git commit -m "feat(transactions): add bulk import from CSV"
git commit -m "fix(calendar): correct date display in non-UTC timezones"
git commit -m "docs: update API examples in README"
```

### 6. Submit Pull Request

- Ensure all tests pass
- Include tests for new functionality
- Update documentation if needed
- Link related issues

---

## 🚀 Release Process

Budget Experiment uses [Semantic Versioning](https://semver.org/) (SemVer) with Git tags as the single source of truth.

### Version Format

```
MAJOR.MINOR.PATCH[-PRERELEASE]
```

| Component | When to Increment |
|-----------|-------------------|
| **MAJOR** | Breaking changes, significant overhauls |
| **MINOR** | New features, backward-compatible additions |
| **PATCH** | Bug fixes, minor improvements |
| **PRERELEASE** | Pre-release versions (`-alpha.1`, `-beta.1`, `-rc.1`) |

### Creating a Release

1. **Ensure all changes are merged to `main`**:
   ```bash
   git checkout main
   git pull origin main
   ```

2. **Run tests to verify stability**:
   ```bash
   dotnet test
   ```

3. **Create an annotated tag**:
   ```bash
   git tag -a v3.1.0 -m "Release 3.1.0 - Feature description"
   ```

4. **Push the tag**:
   ```bash
   git push origin v3.1.0
   ```

5. **Automation takes over**:
   - GitHub Actions builds Docker images tagged with `3.1.0`, `3.1`, `3`, and `latest`
   - A GitHub Release is created with auto-generated changelog
   - Images are published to `ghcr.io/becauseimclever/budgetexperiment`

### Pre-release Workflow

For testing before a major release:

```bash
# Alpha (early, unstable)
git tag -a v4.0.0-alpha.1 -m "Alpha 1: New feature testing"
git push origin v4.0.0-alpha.1

# Beta (feature complete, testing)
git tag -a v4.0.0-beta.1 -m "Beta 1: Ready for testing"
git push origin v4.0.0-beta.1

# Release Candidate (final testing)
git tag -a v4.0.0-rc.1 -m "RC 1: Final testing"
git push origin v4.0.0-rc.1

# Final release
git tag -a v4.0.0 -m "Release 4.0.0"
git push origin v4.0.0
```

Pre-release tags create GitHub Releases marked as "Pre-release".

### Hotfix Workflow

For urgent fixes to a released version:

```bash
# Create hotfix branch from the release tag
git checkout -b hotfix/3.0.1 v3.0.0

# Make fixes and commit
git commit -m "fix: critical bug in budget calculation"

# Tag and push
git tag -a v3.0.1 -m "Hotfix: Critical bug fix"
git push origin v3.0.1
git push origin hotfix/3.0.1

# Merge back to main
git checkout main
git merge hotfix/3.0.1
git push origin main
```

### Version Information

- **API**: `GET /api/version` returns current version, build date, and commit hash
- **Client UI**: Version displayed in the footer
- **Docker Images**: Tagged with semver patterns at `ghcr.io/becauseimclever/budgetexperiment`

---

## 🚫 Forbidden Libraries

The following libraries are **not allowed** in this project:

- **FluentAssertions** - Use Shouldly or built-in Assert
- **AutoFixture** - Write explicit test data

---

## 📬 Questions?

Open an issue on GitHub for questions or discussions about contributing.
