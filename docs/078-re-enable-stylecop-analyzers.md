# Feature 078: Re-enable StyleCop Analyzers
> **Status:** Planning
> **Priority:** High (code quality enforcement)
> **Estimated Effort:** Small–Medium (1-2 days)
> **Dependencies:** None

## Overview

The coding standard (§18) requires StyleCop enforcement via `StyleCop.Analyzers` NuGet, added centrally in `Directory.Build.props`, with all analyzer warnings escalated to errors. An audit found that **StyleCop is completely disabled** — the entire `<ItemGroup>` in `Directory.Build.props` is commented out with the note "StyleCop.Analyzers removed temporarily to fix build issues" but no date, owner, or issue link.

Individual project `.csproj` files reference `StyleCop.Analyzers` with `Update` (version 1.2.0-beta.556), but since the central `Include` is commented out, these have no effect. The codebase is currently building with **zero StyleCop enforcement**.

## Problem Statement

### Current State

```xml
<!-- Directory.Build.props (lines 18-27) -->
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

### Violation Assessment (performed 2026-03-01)

Temporarily uncommenting StyleCop reveals **1,514 violations** across **only 2 projects**:

| Rule | Count | Projects | Description |
|------|-------|----------|-------------|
| **SA1101** | 1,194 | E2E.Tests (1,122) + Domain (72) | Prefix local calls with `this.` |
| **SA1641** | 276 | Domain (194) + E2E.Tests (82) | File header company name mismatch |
| SA1201 | 10 | Domain (8) + E2E.Tests (2) | Member ordering (field after property, etc.) |
| SA1204 | 8 | Domain (6) + E2E.Tests (2) | Static members should appear before non-static |
| SA1316 | 4 | E2E.Tests (4) | Tuple element name casing |
| SA1202 | 4 | Domain (2) + E2E.Tests (2) | Public members before private members |
| SA1210 | 4 | Domain (2) + E2E.Tests (2) | Using directives ordering |
| SA1025 | 2 | Domain | Multiple whitespace characters in a row |
| SA1108 | 2 | E2E.Tests | Block statements with embedded comments |
| SA1118 | 2 | E2E.Tests | Parameter spans multiple lines |
| SA1407 | 2 | Domain | Arithmetic precedence parentheses |
| SA1516 | 2 | E2E.Tests | Elements separated by blank line |
| SA1518 | 2 | Domain | File must end with single newline |
| SA1615 | 2 | E2E.Tests | Return value documentation |

**9 of 11 projects are already fully compliant.** The work is concentrated in Domain (290 violations) and E2E.Tests (1,224 violations).

### Root Causes

1. **SA1101 (1,194 — 79% of all violations):** The project uses `_camelCase` for private fields (§5), making `this.` redundant. SA1309 (underscore prefix) is already disabled in `.editorconfig` to accommodate this convention. SA1101 should be disabled for consistency — the `_` prefix already distinguishes fields from locals.

2. **SA1641 (276 — 18% of all violations):** `stylecop.json` sets `companyName: "Fortinbra"` but 776 of 788 source files use `company="BecauseImClever"`. Only 12 files use "Fortinbra". Fix: update `stylecop.json` to `"BecauseImClever"` and align the 12 outlier files.

3. **Remaining 44 violations (3%):** Member ordering, static ordering, minor formatting — all manual fixes in Domain and E2E.Tests.

### Target State

- StyleCop uncommented and active in `Directory.Build.props` (version `1.2.0-beta.556`)
- SA1101 disabled in `.editorconfig` (deliberate convention decision, not a suppression)
- `stylecop.json` company name corrected to `"BecauseImClever"`
- All 1,514 violations resolved
- All projects build cleanly with zero warnings
- The commented-out block and "temporarily" note removed

---

## Vertical Slices

Each slice is independently deliverable and leaves the build green. Slices are ordered so that early slices eliminate the most violations with the least risk.

---

### Slice 1: Disable SA1101 and fix `stylecop.json` company name

**Risk:** Lowest — configuration-only changes, zero source code edits.
**Impact:** Eliminates 1,470 of 1,514 violations (97%).

**As a** developer
**I want** SA1101 disabled and the `stylecop.json` company name corrected
**So that** re-enabling StyleCop is feasible without massive source churn.

**Rationale:**
- SA1101 (`this.` prefix) conflicts with the project's `_camelCase` field convention (§5). SA1309 is already disabled for the same reason. Disabling SA1101 is a **deliberate convention alignment**, not a workaround.
- 776 of 788 files use `"BecauseImClever"`. The 12 "Fortinbra" files are outliers.

**Changes:**

1. **`.editorconfig`** — Add:
   ```ini
   dotnet_diagnostic.SA1101.severity = none   # Project uses _camelCase fields; this. prefix is redundant
   ```

2. **`stylecop.json`** — Change `companyName` from `"Fortinbra"` to `"BecauseImClever"` and update `copyrightText`:
   ```json
   "companyName": "BecauseImClever",
   "copyrightText": "Copyright (c) BecauseImClever. All rights reserved."
   ```

3. **12 source files** — Update copyright headers from `company="Fortinbra"` / `Copyright (c) 2025 Fortinbra (becauseimclever.com)` to `company="BecauseImClever"` / `Copyright (c) BecauseImClever`.

**Tasks:**
- [ ] Add `SA1101.severity = none` to `.editorconfig`
- [ ] Update `stylecop.json` company name and copyright text
- [ ] Find and fix the 12 files with "Fortinbra" headers
- [ ] Verify these changes resolve 1,470 violations (SA1101 + SA1641)

**Commit:**
```
chore: align StyleCop config with project conventions

- Disable SA1101 (this. prefix) — project uses _camelCase field convention
- Fix stylecop.json companyName from "Fortinbra" to "BecauseImClever"
- Standardize 12 outlier file headers to match majority convention

Refs: #078
```

---

### Slice 2: Fix remaining Domain violations

**Risk:** Low — 20 violations across ~10 files, all mechanical member reordering and minor formatting.
**Depends on:** Slice 1 (SA1101 and SA1641 removed first).

**As a** developer
**I want** all remaining StyleCop violations in the Domain project fixed
**So that** Domain is fully compliant before re-enabling enforcement.

**Violations to fix (20):**
| Rule | Count | Fix |
|------|-------|-----|
| SA1201 | 8 | Reorder members (fields before properties before methods) |
| SA1204 | 6 | Move static members before instance members |
| SA1202 | 2 | Move public members before private members |
| SA1210 | 2 | Alphabetize using directives |
| SA1518 | 2 | Add trailing newline to files |
| SA1407 | 2 | Add parentheses to arithmetic expressions |
| SA1025 | 2 | Remove extra whitespace |

**Tasks:**
- [ ] Run `dotnet format analyzers` on Domain project as first pass
- [ ] Fix remaining violations manually (member ordering)
- [ ] Verify Domain builds with zero SA warnings

**Commit:**
```
style(domain): fix 20 StyleCop violations

- Reorder members per SA1201/SA1202/SA1204 (fields → properties → methods)
- Alphabetize using directives (SA1210)
- Add trailing newlines, arithmetic parentheses, remove extra whitespace

Refs: #078
```

---

### Slice 3: Fix remaining E2E.Tests violations

**Risk:** Low — 24 violations across test files, mechanical fixes.
**Depends on:** Slice 1 (SA1101 and SA1641 removed first).

**As a** developer
**I want** all remaining StyleCop violations in E2E.Tests fixed
**So that** every project in the solution is compliant.

**Violations to fix (24):**
| Rule | Count | Fix |
|------|-------|-----|
| SA1316 | 4 | Rename tuple elements to PascalCase |
| SA1201 | 2 | Reorder members |
| SA1202 | 2 | Public before private ordering |
| SA1204 | 2 | Static before instance ordering |
| SA1210 | 2 | Alphabetize usings |
| SA1108 | 2 | Remove embedded comments from block statements |
| SA1118 | 2 | Refactor multi-line parameters |
| SA1516 | 2 | Add blank lines between elements |
| SA1615 | 2 | Add `<returns>` XML doc |

**Tasks:**
- [ ] Fix violations manually (test files — no `dotnet format` auto-fix for most)
- [ ] Verify E2E.Tests builds with zero SA warnings

**Commit:**
```
style(e2e): fix 24 StyleCop violations in E2E tests

- PascalCase tuple elements, member ordering, blank lines
- Add return value XML docs, refactor multi-line parameters

Refs: #078
```

---

### Slice 4: Uncomment StyleCop in `Directory.Build.props`

**Risk:** Low (all violations already fixed) — but this is the "point of no return" that enables enforcement.
**Depends on:** Slices 1, 2, 3 all complete.

**As a** developer
**I want** StyleCop uncommented and actively enforcing in all builds
**So that** future code style violations are caught at build time.

**Changes:**

1. **`Directory.Build.props`** — Replace the commented-out block:
   ```xml
   <ItemGroup>
     <PackageReference Include="StyleCop.Analyzers" Version="1.2.0-beta.556" PrivateAssets="all" />
   </ItemGroup>
   <ItemGroup>
     <AdditionalFiles Include="$(MSBuildThisFileDirectory)stylecop.json" Link="stylecop.json" />
   </ItemGroup>
   ```
   
2. Remove the `<!-- StyleCop.Analyzers removed temporarily to fix build issues -->` comment.

3. Update version from `1.2.0-beta.507` to `1.2.0-beta.556` (matching individual csproj `Update` refs).

**Tasks:**
- [ ] Uncomment and update StyleCop in `Directory.Build.props`
- [ ] Remove the "temporarily" comment
- [ ] `dotnet build` entire solution — zero warnings, zero errors
- [ ] `dotnet test` — all tests pass
- [ ] Verify CI would pass (no new violations introduced)

**Commit:**
```
chore: re-enable StyleCop analyzers in Directory.Build.props

- Uncomment StyleCop.Analyzers (v1.2.0-beta.556) in Directory.Build.props
- Link stylecop.json as AdditionalFile for all projects
- TreatWarningsAsErrors now catches StyleCop violations
- All 11 projects build cleanly with zero warnings
- Remove "removed temporarily" comment — enforcement is permanent

Refs: #078
```

---

## Configuration Summary

### Rules disabled in `.editorconfig` (deliberate convention decisions)

| Rule | Reason | Status |
|------|--------|--------|
| SA1633 | File header requirement | Already disabled |
| SA1636 | File header company text | Already disabled |
| SA1639 | File header summary tag | Already disabled |
| SA1309 | Underscore prefix for fields | Already disabled (project uses `_camelCase`) |
| SA1412 | BOM requirement | Already disabled |
| SA1600 | Element documentation | Already disabled (temporarily — evaluate later) |
| SA1609 | Property `<value>` doc | Already disabled |
| **SA1101** | `this.` prefix | **To be disabled in Slice 1** (redundant with `_camelCase`) |

### `stylecop.json` settings (after Slice 1)

```json
{
  "settings": {
    "documentationRules": {
      "companyName": "BecauseImClever",
      "copyrightText": "Copyright (c) BecauseImClever. All rights reserved.",
      "documentInterfaces": true,
      "documentInternalElements": false,
      "documentPrivateElements": false,
      "documentExposedElements": true,
      "documentPrivateFields": false
    },
    "orderingRules": {
      "usingDirectivesPlacement": "outsideNamespace",
      "blankLinesBetweenUsingGroups": "allow"
    },
    "layoutRules": {
      "newlineAtEndOfFile": "require"
    },
    "namingRules": {
      "allowCommonHungarianPrefixes": false
    }
  }
}
```

---

## Testing Strategy

### Per-Slice Verification
- [ ] `dotnet build` with zero warnings/errors after each slice
- [ ] All unit tests pass (no behavioral changes)

### Post-Completion Verification (after Slice 4)
- [ ] Full `dotnet test` suite passes
- [ ] Introduce an intentional SA violation → confirm it fails the build
- [ ] `dotnet format --verify-no-changes` passes

---

## Risk Assessment

| Slice | Risk | Violations Resolved | Mitigation |
|-------|------|-------------------|------------|
| 1 – Config alignment | Lowest | 1,470 (97%) | Config-only + 12 trivial header fixes |
| 2 – Domain fixes | Low | 20 | Mechanical reordering; no logic changes |
| 3 – E2E fixes | Low | 24 | Test files only; no production code |
| 4 – Enable enforcement | Low | 0 (already fixed) | Build validates instantly |

Overall: **Low risk** — no behavioral changes. Each slice leaves the build green.

---

## References

- Coding standard §18: "StyleCop enforced via `StyleCop.Analyzers` NuGet."
- Coding standard §5: "Private fields: `_camelCase`."
- `stylecop.json` and `.editorconfig` in repository root.

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-02-26 | Initial draft from codebase audit | @copilot |
| 2026-03-01 | Restructured as vertical slices with violation assessment data | @copilot |
