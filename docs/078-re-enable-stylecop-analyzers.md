# Feature 078: Re-enable StyleCop Analyzers
> **Status:** Planning
> **Priority:** High (code quality enforcement)
> **Estimated Effort:** Medium (2-3 days)
> **Dependencies:** None

## Overview

The coding standard (§18) requires StyleCop enforcement via `StyleCop.Analyzers` NuGet, added centrally in `Directory.Build.props`, with all analyzer warnings escalated to errors. An audit found that **StyleCop is completely disabled** — the entire `<ItemGroup>` in `Directory.Build.props` is commented out with the note "StyleCop.Analyzers removed temporarily to fix build issues" but no date, owner, or issue link.

Individual project `.csproj` files reference `StyleCop.Analyzers` with `Update` (version 1.2.0-beta.556), but since the central `Include` is commented out, these have no effect. The codebase is currently building with **zero StyleCop enforcement**.

## Problem Statement

### Current State

```xml
<!-- Directory.Build.props (lines 14-22) -->
<!-- StyleCop.Analyzers removed temporarily to fix build issues -->
<!--
<ItemGroup>
  <PackageReference Include="StyleCop.Analyzers" Version="1.2.0-beta.507" PrivateAssets="all" />
</ItemGroup>
<ItemGroup>
  <AdditionalFiles Include="$(MSBuildThisFileDirectory)stylecop.json" Link="stylecop.json" />
</ItemGroup>
-->
```

- Root `stylecop.json` and `.editorconfig` exist but are not enforced
- `TreatWarningsAsErrors` is `true` but without StyleCop producing warnings, there's nothing to escalate
- No date or tracking for when/why this was disabled

### Target State

- StyleCop uncommented and active in `Directory.Build.props`
- Version updated to latest stable (or latest beta if needed for .NET 10)
- All projects build cleanly with StyleCop enabled
- Any existing violations are fixed (not suppressed)

---

## User Stories

### US-078-001: Re-enable StyleCop in Build
**As a** developer
**I want to** have StyleCop analyzers running during builds
**So that** code style is enforced consistently and violations are caught before PR review.

**Acceptance Criteria:**
- [ ] `Directory.Build.props` has StyleCop uncommented and active
- [ ] `stylecop.json` is linked as an `AdditionalFile` for all projects
- [ ] All projects build without StyleCop errors (violations fixed, not suppressed)
- [ ] `TreatWarningsAsErrors` catches StyleCop violations
- [ ] The commented-out block and "temporarily" note are removed

### US-078-002: Fix Existing StyleCop Violations
**As a** developer
**I want to** fix all existing code style violations surfaced by StyleCop
**So that** the codebase is clean when enforcement is re-enabled.

**Acceptance Criteria:**
- [ ] All SA-prefixed warnings resolved across all projects
- [ ] No global suppressions added (scoped only, with justification)
- [ ] `dotnet format` applied as a first pass
- [ ] Remaining issues fixed manually

---

## Technical Design

### Approach

1. **Uncomment** the StyleCop block in `Directory.Build.props`
2. **Update version** from `1.2.0-beta.507` to match the `Update` version in individual projects (`1.2.0-beta.556`) or to latest available
3. **Build** and collect all violations
4. **Run `dotnet format`** to auto-fix formatting issues (ordering, spacing, usings)
5. **Fix remaining** violations manually (XML docs, naming, etc.)
6. **Verify** all tests still pass

### Expected Violation Categories

Based on common StyleCop rules:
- **SA1200**: Using directives placement (likely handled by `GlobalUsings.cs`)
- **SA1101**: Prefix local calls with `this.` (may conflict with project style)
- **SA1600-SA1650**: Documentation rules (XML docs for public API)
- **SA1300-SA1311**: Naming rules
- **SA1500-SA1520**: Brace/bracket rules

### Configuration Review

- Review `stylecop.json` settings to ensure they match project conventions
- Review `.editorconfig` for any conflicts
- Ensure `documentationRules.documentInterfaces` and similar settings are appropriate

---

## Implementation Plan

### Phase 1: Assess Violation Count

**Objective:** Uncomment StyleCop, build, and catalog violations without fixing.

**Tasks:**
- [ ] Uncomment StyleCop in `Directory.Build.props`
- [ ] Update to latest version
- [ ] Build solution and redirect warnings to a log
- [ ] Categorize violations by rule ID and count
- [ ] Plan fix approach per category

### Phase 2: Auto-Fix with dotnet format

**Objective:** Apply automated fixes for formatting and ordering rules.

**Tasks:**
- [ ] Run `dotnet format` with analyzers enabled
- [ ] Review auto-applied changes
- [ ] Build and verify

### Phase 3: Fix Documentation Violations

**Objective:** Add XML documentation for public API surface.

**Tasks:**
- [ ] Add XML docs to public controller methods
- [ ] Add XML docs to public service interfaces
- [ ] Add XML docs to public DTO types
- [ ] Verify build

### Phase 4: Fix Remaining Violations

**Objective:** Address any remaining naming, brace, or other style violations.

**Tasks:**
- [ ] Fix naming violations
- [ ] Fix brace/formatting issues not caught by auto-fix
- [ ] Add scoped suppressions only where justified (with TODO + issue link)
- [ ] Final build verification
- [ ] Full test suite green

**Commit:**
```bash
git commit -m "chore: re-enable StyleCop analyzers

- Uncomment StyleCop.Analyzers in Directory.Build.props
- Fix all existing style violations
- No global suppressions added
- All projects build cleanly with TreatWarningsAsErrors

Refs: #078"
```

---

## Testing Strategy

### Verification
- [ ] `dotnet build` with zero warnings/errors
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] CI build passes

---

## Risk Assessment

- **Medium risk**: May surface a large number of violations requiring manual intervention.
- **Scope creep**: XML documentation requirement could be time-consuming for the public API surface.
- **Mitigation**: Phase 1 assessment will quantify the work before committing to full fix.

---

## References

- Coding standard §18: "StyleCop enforced via `StyleCop.Analyzers` NuGet."
- `stylecop.json` and `.editorconfig` in repository root.

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-02-26 | Initial draft from codebase audit | @copilot |
