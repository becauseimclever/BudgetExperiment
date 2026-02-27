# Feature 076: Value Object Naming Convention
> **Status:** Planning
> **Priority:** Medium (naming consistency / domain clarity)
> **Estimated Effort:** Medium (2-3 days)
> **Dependencies:** None

## Overview

The coding standard (§5) requires domain value objects to end with the suffix `Value` (e.g., `MoneyValue`). An audit found **13 value objects** that lack this suffix. Only `MoneyValue` is compliant. Renaming these types improves domain readability and instantly signals to developers that a type is a value object rather than an entity.

## Problem Statement

### Current State

| Current Name | File | Is Value Object? | Proposed Name |
|-------------|------|-------------------|---------------|
| `GeoCoordinate` | `Domain/Common/GeoCoordinate.cs` | Yes — sealed record, private ctor, factory with validation | `GeoCoordinateValue` |
| `TransactionLocation` | `Domain/Common/TransactionLocation.cs` | Yes — sealed record, private ctor, factory with validation | `TransactionLocationValue` |
| `MatchingTolerances` | `Domain/Reconciliation/MatchingTolerances.cs` | Yes — sealed class, IEquatable, private ctor, factory | `MatchingTolerancesValue` |
| `DailyTotal` | `Domain/Reconciliation/DailyTotal.cs` | Yes — sealed positional record | `DailyTotalValue` |
| `ColumnMapping` | `Domain/Import/ColumnMapping.cs` | Yes — sealed record, init-only | `ColumnMappingValue` |
| `SkipRowsSettings` | `Domain/Import/SkipRowsSettings.cs` | Yes — sealed record, factory with validation | `SkipRowsSettingsValue` |
| `DebitCreditIndicatorSettings` | `Domain/Import/DebitCreditIndicatorSettings.cs` | Yes — sealed record, factory with validation | `DebitCreditIndicatorSettingsValue` |
| `DuplicateDetectionSettings` | `Domain/Import/DuplicateDetectionSettings.cs` | Yes — sealed record | `DuplicateDetectionSettingsValue` |
| `BillInfo` | `Domain/Recurring/BillInfo.cs` | Yes — sealed record, private ctor, factory | `BillInfoValue` |
| `RecurrencePattern` | `Domain/Recurring/RecurrencePattern.cs` | Yes — sealed record, private ctor, factory | `RecurrencePatternValue` |
| `RecurringInstanceInfo` | `Domain/Recurring/RecurringInstanceInfo.cs` | Yes — sealed positional record | `RecurringInstanceInfoValue` |
| `RecurringTransferInstanceInfo` | `Domain/Recurring/RecurringTransferInstanceInfo.cs` | Yes — sealed positional record | `RecurringTransferInstanceInfoValue` |
| `ImportPattern` | `Domain/Recurring/ImportPattern.cs` | Yes — sealed record, private ctor, factory, behavior | `ImportPatternValue` |

`MoneyValue` (Domain/Common) is the only compliant value object.

### Target State

All 13 value objects are renamed with the `Value` suffix. All references across all layers (Domain, Application, Infrastructure, Contracts, Api, Client, Tests) are updated.

---

## User Stories

### US-076-001: Rename Domain Value Objects
**As a** developer
**I want to** value objects to consistently end with `Value`
**So that** I can instantly identify value objects vs entities in the domain model.

**Acceptance Criteria:**
- [ ] All 13 value objects renamed with `Value` suffix
- [ ] Files renamed to match new type names
- [ ] All references updated across all projects (Domain, Application, Infrastructure, Contracts, Api, Client, Tests)
- [ ] EF Core configurations updated (column names, JSON converters)
- [ ] Database migration generated if column names change (evaluate if rename is schema-breaking)
- [ ] All tests pass

---

## Technical Design

### Impact Analysis

Each rename touches:
1. **Domain**: Type declaration + file rename
2. **Application**: Service usages, mappers
3. **Infrastructure**: EF configurations, repositories
4. **Contracts**: DTO property types (if referencing domain types — audit needed)
5. **Client**: Razor/service files using domain types directly (see Feature 079)
6. **Tests**: All test files referencing these types

### Database Considerations

Value objects stored as owned types or JSON columns should NOT change database column names. The EF Fluent API configuration decouples type names from column names. Verify no migration is needed by running `dotnet ef migrations has-pending-model-changes`.

### Risk Mitigation

- Rename one type at a time with full build verification between each
- Use IDE rename refactoring (Shift+F2) to catch all references
- Run full test suite after each rename

---

## Implementation Plan

### Phase 1: Rename Common Value Objects

**Objective:** Rename `GeoCoordinate` → `GeoCoordinateValue`, `TransactionLocation` → `TransactionLocationValue`

**Tasks:**
- [ ] Rename types and files
- [ ] Update all references
- [ ] Verify build and tests

### Phase 2: Rename Import Value Objects

**Objective:** Rename `ColumnMapping`, `SkipRowsSettings`, `DebitCreditIndicatorSettings`, `DuplicateDetectionSettings`

**Tasks:**
- [ ] Rename types and files
- [ ] Update all references
- [ ] Verify build and tests

### Phase 3: Rename Recurring Value Objects

**Objective:** Rename `BillInfo`, `RecurrencePattern`, `RecurringInstanceInfo`, `RecurringTransferInstanceInfo`, `ImportPattern`

**Tasks:**
- [ ] Rename types and files
- [ ] Update all references
- [ ] Verify build and tests

### Phase 4: Rename Reconciliation Value Objects

**Objective:** Rename `MatchingTolerances`, `DailyTotal`

**Tasks:**
- [ ] Rename types and files
- [ ] Update all references
- [ ] Verify build and tests

### Phase 5: Verify No Schema Changes

**Tasks:**
- [ ] Run `dotnet ef migrations has-pending-model-changes`
- [ ] Ensure no new migration is needed (column names should be stable)
- [ ] Full test suite green

**Commit:**
```bash
git commit -m "refactor(domain): rename value objects with Value suffix

- GeoCoordinate → GeoCoordinateValue
- TransactionLocation → TransactionLocationValue
- MatchingTolerances → MatchingTolerancesValue
- DailyTotal → DailyTotalValue
- ColumnMapping → ColumnMappingValue
- SkipRowsSettings → SkipRowsSettingsValue
- DebitCreditIndicatorSettings → DebitCreditIndicatorSettingsValue
- DuplicateDetectionSettings → DuplicateDetectionSettingsValue
- BillInfo → BillInfoValue
- RecurrencePattern → RecurrencePatternValue
- RecurringInstanceInfo → RecurringInstanceInfoValue
- RecurringTransferInstanceInfo → RecurringTransferInstanceInfoValue
- ImportPattern → ImportPatternValue
- Enforces §5 naming convention for domain value objects

Refs: #076"
```

---

## Testing Strategy

### Unit Tests
- No new tests — rename only. All existing tests must pass after rename.

### Verification
- [ ] Full `dotnet build` green
- [ ] All unit/integration tests green
- [ ] No pending EF migration changes

---

## Risk Assessment

- **Medium risk**: Wide rename across all layers. High file count but mechanically safe with IDE rename.
- **Merge conflicts**: Significant — any branch touching domain types will need rebasing.
- **Database**: Low risk if EF Fluent API is used for column naming (verified — no data annotations in Domain).

---

## References

- Coding standard §5: "Domain value objects end with `Value`."
- Coding standard §12: "Value objects: Immutable, equality by components."

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-02-26 | Initial draft from codebase audit | @copilot |
