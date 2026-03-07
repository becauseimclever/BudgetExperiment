# Feature 093: NuGet Package Updates (Non-EF Core)

> **Status:** Done
> **Priority:** Medium
> **Dependencies:** None

## Overview

Update all outdated NuGet packages across the solution to their latest stable versions. Entity Framework Core packages are explicitly excluded because `Npgsql.EntityFrameworkCore.PostgreSQL` is at 10.0.0 (its latest release) and has not yet published a version compatible with EF Core 10.0.3. Upgrading EF Core packages without a matching Npgsql provider would break the PostgreSQL integration.

## Problem Statement

### Current State

As of 2026-03-07, 17 non-EF packages across 13 projects have updates available. Two packages have major version bumps (MinVer 6→7, coverlet.collector 6→8) that may include breaking changes. The remaining packages are minor or patch updates.

### Excluded Packages (EF Core — Blocked by Npgsql)

| Package | Current | Latest | Project | Reason |
|---|---|---|---|---|
| Microsoft.EntityFrameworkCore.Design | 10.0.0-rc.2 | 10.0.3 | Api | Npgsql 10.0.0 requires EF Core 10.0.x compat |
| Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore | 10.0.0 | 10.0.3 | Api | Tied to EF Core version |
| Microsoft.EntityFrameworkCore.InMemory | 10.0.0-rc.2 | 10.0.3 | Api.Tests | Tied to EF Core version |
| Microsoft.EntityFrameworkCore.Sqlite | 10.0.1 | 10.0.3 | Infrastructure.Tests | Tied to EF Core version |

> These will be updated together in a separate feature once Npgsql publishes a compatible release.

### Target State

All non-EF packages at their latest stable versions. All tests passing. No regressions.

---

## Packages to Update

### Phase 1 — Microsoft Patch Updates (Low Risk)

ASP.NET Core and Microsoft.Extensions packages, all 10.0.0 → 10.0.3 patch updates.

| Package | Current | Latest | Project |
|---|---|---|---|
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.0 | 10.0.3 | Api |
| Microsoft.AspNetCore.Components.WebAssembly.Server | 10.0.0 | 10.0.3 | Api |
| Microsoft.AspNetCore.OpenApi | 10.0.0 | 10.0.3 | Api |
| Microsoft.AspNetCore.Components.WebAssembly | 10.0.0 | 10.0.3 | Client |
| Microsoft.AspNetCore.Components.WebAssembly.Authentication | 10.0.0 | 10.0.3 | Client |
| Microsoft.AspNetCore.Components.WebAssembly.DevServer | 10.0.0 | 10.0.3 | Client |
| Microsoft.Extensions.Http | 10.0.0 | 10.0.3 | Client |
| Microsoft.Extensions.DependencyInjection.Abstractions | 10.0.0 | 10.0.3 | Domain |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.0 | 10.0.3 | Api.Tests |

### Phase 2 — Third-Party Updates (Low–Medium Risk)

| Package | Current | Latest | Project | Notes |
|---|---|---|---|---|
| Scalar.AspNetCore | 2.10.3 | 2.13.1 | Api | Minor bump, review changelog |
| bunit | 2.5.3 | 2.6.2 | Client.Tests | Minor bump |
| Deque.AxeCore.Playwright | 4.11.0 | 4.11.1 | E2E.Tests | Patch |
| Microsoft.Playwright | 1.49.0 | 1.58.0 | E2E.Tests | Minor bump, may need `playwright install` |
| Microsoft.Build.Tasks.Core | 18.0.2 | 18.3.3 | Infrastructure | Minor bump |

### Phase 3 — Test Infrastructure Updates (Medium Risk)

| Package | Current | Latest | Project | Notes |
|---|---|---|---|---|
| Microsoft.NET.Test.Sdk | 18.0.1 | 18.3.0 | All 6 test projects | Minor bump |
| coverlet.collector | 6.0.4 | 8.0.0 | All 6 test projects | **Major** — review breaking changes |

### Phase 4 — MinVer Major Update (Medium Risk)

| Package | Current | Latest | Project | Notes |
|---|---|---|---|---|
| MinVer | 6.0.0 | 7.0.0 | All 13 projects | **Major** — review migration guide for config changes |

---

## User Stories

### US-093-001: Update Microsoft Patch Packages
**As a** developer
**I want to** update all Microsoft ASP.NET Core and Extensions packages to 10.0.3
**So that** the solution picks up security patches and bug fixes.

**Acceptance Criteria:**
- [x] All 9 Microsoft packages updated to 10.0.3
- [x] All unit tests pass
- [ ] API starts and serves Blazor client correctly
- [ ] Authentication flow works

### US-093-002: Update Third-Party Packages
**As a** developer
**I want to** update Scalar, bunit, Playwright, and MSBuild packages
**So that** tooling stays current and benefits from fixes.

**Acceptance Criteria:**
- [x] Scalar.AspNetCore updated to 2.13.1; Scalar UI accessible at `/scalar`
- [x] bunit updated to 2.6.2; client tests pass
- [x] Playwright updated to 1.58.0; `playwright install` run if needed; E2E tests pass
- [x] Microsoft.Build.Tasks.Core updated to 18.3.3; build succeeds

### US-093-003: Update Test Infrastructure
**As a** developer
**I want to** update Microsoft.NET.Test.Sdk and coverlet.collector
**So that** test execution and coverage collection use current tooling.

**Acceptance Criteria:**
- [x] Microsoft.NET.Test.Sdk updated to 18.3.0 in all 6 test projects
- [x] coverlet.collector updated to 8.0.0 in all 6 test projects
- [x] All tests pass; coverage collection works with `coverlet.runsettings`

### US-093-004: Update MinVer
**As a** developer
**I want to** update MinVer from 6.x to 7.x
**So that** versioning tooling is current.

**Acceptance Criteria:**
- [x] MinVer updated to 7.0.0 in all 13 projects (centrally in Directory.Build.props)
- [x] Review MinVer 7.0 migration guide for any configuration changes
- [x] Build produces correct version output
- [x] All tests pass

---

## Implementation Plan

### Phase 1: Microsoft Patch Updates

**Objective:** Update all Microsoft 10.0.0 → 10.0.3 packages.

**Tasks:**
- [ ] Update 5 Api packages (JwtBearer, WebAssembly.Server, OpenApi, Mvc.Testing indirectly)
- [ ] Update 4 Client packages (WebAssembly, WebAssembly.Authentication, WebAssembly.DevServer, Extensions.Http)
- [ ] Update Domain package (DependencyInjection.Abstractions)
- [ ] Update Api.Tests package (Mvc.Testing)
- [ ] Run full test suite
- [ ] Smoke test API + Client startup

**Commit:**
```
chore(deps): update Microsoft packages to 10.0.3

- ASP.NET Core packages 10.0.0 → 10.0.3 (Api, Client)
- Microsoft.Extensions packages 10.0.0 → 10.0.3 (Domain, Client)
- Microsoft.AspNetCore.Mvc.Testing 10.0.0 → 10.0.3 (Api.Tests)

Refs: #093
```

---

### Phase 2: Third-Party Updates

**Objective:** Update Scalar, bunit, Playwright, and MSBuild packages.

**Tasks:**
- [ ] Update Scalar.AspNetCore 2.10.3 → 2.13.1
- [ ] Update bunit 2.5.3 → 2.6.2
- [ ] Update Microsoft.Playwright 1.49.0 → 1.58.0
- [ ] Update Deque.AxeCore.Playwright 4.11.0 → 4.11.1
- [ ] Update Microsoft.Build.Tasks.Core 18.0.2 → 18.3.3
- [ ] Run `pwsh bin/Debug/net10.0/playwright.ps1 install` if Playwright version requires it
- [ ] Run full test suite

**Commit:**
```
chore(deps): update third-party packages

- Scalar.AspNetCore 2.10.3 → 2.13.1
- bunit 2.5.3 → 2.6.2
- Microsoft.Playwright 1.49.0 → 1.58.0
- Deque.AxeCore.Playwright 4.11.0 → 4.11.1
- Microsoft.Build.Tasks.Core 18.0.2 → 18.3.3

Refs: #093
```

---

### Phase 3: Test Infrastructure Updates

**Objective:** Update test SDK and coverage collector across all test projects.

**Tasks:**
- [ ] Review coverlet.collector 8.0 changelog for breaking changes
- [ ] Update Microsoft.NET.Test.Sdk 18.0.1 → 18.3.0 in 6 test projects
- [ ] Update coverlet.collector 6.0.4 → 8.0.0 in 6 test projects
- [ ] Verify `coverlet.runsettings` is still compatible
- [ ] Run full test suite with coverage collection

**Commit:**
```
chore(deps): update test infrastructure packages

- Microsoft.NET.Test.Sdk 18.0.1 → 18.3.0
- coverlet.collector 6.0.4 → 8.0.0
- Applied across all 6 test projects

Refs: #093
```

---

### Phase 4: MinVer Major Update

**Objective:** Update MinVer to 7.x across all projects.

**Tasks:**
- [ ] Review MinVer 7.0 release notes and migration guide
- [ ] Update MinVer 6.0.0 → 7.0.0 in all 13 projects
- [ ] Verify version output in build (`dotnet build` produces expected version)
- [ ] Check for any `.csproj` property changes (e.g., `MinVerTagPrefix`, `MinVerMinimumMajorMinor`)
- [ ] Run full test suite

**Commit:**
```
chore(deps): update MinVer 6.0.0 → 7.0.0

- Major version update across all 13 projects
- Reviewed migration guide; [note any config changes here]

Refs: #093
```

---

## Risk Assessment

| Risk | Impact | Mitigation |
|---|---|---|
| MinVer 7.0 breaking changes | Build versioning breaks | Review migration guide before updating; test version output |
| coverlet.collector 8.0 breaking changes | Coverage collection fails | Review changelog; verify runsettings compatibility |
| Playwright browser version mismatch | E2E tests fail | Run `playwright install` after update |
| Scalar API changes | Scalar UI breaks | Verify `/scalar` page loads after update |

## Notes

- EF Core packages (Design, InMemory, Sqlite, HealthChecks.EFCore) are intentionally excluded. They are pinned by `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.0, which has not yet released a version compatible with EF Core 10.0.3. Track separately.
- All updates should use `dotnet add <project> package <name> --version <version>` per §33.
