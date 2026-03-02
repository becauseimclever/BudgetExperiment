# Feature 076: Value Object Naming Convention
> **Status:** Done
> **Priority:** Medium (naming consistency / domain clarity)
> **Estimated Effort:** Medium-High (3-5 days)
> **Dependencies:** None

## Overview

The coding standard (§5) requires domain value objects to end with the suffix `Value` (e.g., `MoneyValue`). An audit found **17 value objects** that lack this suffix. Only `MoneyValue` is compliant. Renaming these types improves domain readability and instantly signals to developers that a type is a value object rather than an entity.

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
| `PaycheckAllocation` | `Domain/Paycheck/PaycheckAllocation.cs` | Yes — sealed record, private ctor, factory | `PaycheckAllocationValue` |
| `PaycheckAllocationSummary` | `Domain/Paycheck/PaycheckAllocationSummary.cs` | Yes — sealed record, private ctor, factory | `PaycheckAllocationSummaryValue` |
| `PaycheckAllocationWarning` | `Domain/Paycheck/PaycheckAllocationWarning.cs` | Yes — sealed record, private ctor, factory | `PaycheckAllocationWarningValue` |
| `TransactionMatchResult` | `Domain/Reconciliation/TransactionMatchResult.cs` | Yes — sealed positional record | `TransactionMatchResultValue` |

`MoneyValue` (Domain/Common) is the only compliant value object.

#### Excluded (Borderline Cases)

| Type | File | Reason for Exclusion |
|------|------|---------------------|
| `BudgetProgress` | `Domain/Budgeting/BudgetProgress.cs` | Structurally a VO but semantically a projection/read-model; has `CategoryId` as external reference, not own identity. Revisit if domain model evolves. |
| `ClarificationOption` | `Domain/Chat/ClarificationOption.cs` | Sealed record with init properties but semantically closer to a DTO within the Chat domain. |

### Target State

All 17 value objects are renamed with the `Value` suffix. All references across Domain, Application, Infrastructure, and Tests are updated.

---

## User Stories

### US-076-001: Rename Domain Value Objects
**As a** developer
**I want to** value objects to consistently end with `Value`
**So that** I can instantly identify value objects vs entities in the domain model.

**Acceptance Criteria:**
- [ ] All 17 value objects renamed with `Value` suffix
- [ ] Files renamed to match new type names
- [ ] All references updated across impacted projects (Domain, Application, Infrastructure, Tests)
- [ ] EF Core configurations updated (owned types, JSON converters)
- [ ] `BudgetDbContextModelSnapshot.cs` regenerated
- [ ] No schema-breaking migration generated (verify with `dotnet ef migrations has-pending-model-changes`)
- [ ] All tests pass

---

## Technical Design

### Impact Analysis

Verified reference counts per layer (0 references found in Contracts, Api, or Client — those layers correctly use DTOs):

| Type | Domain | App | Infra | Tests | **Total Refs** |
|------|-------:|----:|------:|------:|---------------:|
| `RecurrencePattern` | 52 | 37 | 95 | 115 | **299** |
| `ImportPattern` | 13 | 1 | 4 | 53 | **71** |
| `ColumnMapping` | 7 | 4 | 3 | 46 | **60** |
| `RecurringInstanceInfo` | 8 | 9 | 0 | 43 | **60** |
| `BillInfo` | 16 | 1 | 0 | 36 | **53** |
| `MatchingTolerances` | 16 | 9 | 0 | 24 | **49** |
| `TransactionLocation` | 14 | 9 | 3 | 18 | **44** |
| `SkipRowsSettings` | 9 | 3 | 11 | 21 | **44** |
| `DebitCreditIndicatorSettings` | 8 | 5 | 3 | 24 | **40** |
| `DailyTotal` | 3 | 10 | 2 | 20 | **35** |
| `RecurringTransferInstanceInfo` | 4 | 11 | 0 | 15 | **30** |
| `GeoCoordinate` | 11 | 2 | 3 | 13 | **29** |
| `DuplicateDetectionSettings` | 5 | 3 | 3 | 8 | **19** |
| `PaycheckAllocation` | est. | est. | est. | est. | est. |
| `PaycheckAllocationSummary` | est. | est. | est. | est. | est. |
| `PaycheckAllocationWarning` | est. | est. | est. | est. | est. |
| `TransactionMatchResult` | est. | est. | est. | est. | est. |

**Total files impacted: ~108+** across Domain (~26), Application (~17), Infrastructure (~26, includes migration snapshots), and Tests (~39).

Each rename touches:
1. **Domain**: Type declaration + file rename
2. **Application**: Service usages, mappers
3. **Infrastructure**: EF configurations, repositories, migration snapshot
4. **Tests**: All test files referencing these types

> **Note:** Contracts, Api, and Client have **zero references** to these domain types — they use DTOs properly.

### Database Considerations

Value objects stored as owned types or JSON columns should NOT change database column names. The EF Fluent API configuration decouples type names from column names. Verify no migration is needed by running `dotnet ef migrations has-pending-model-changes`.

**Verified EF storage mechanisms:**

| EF Config File | Types Referenced | Storage |
|----------------|------------------|---------|
| `ImportMappingConfiguration.cs` | `ColumnMapping`, `DuplicateDetectionSettings`, `SkipRowsSettings`, `DebitCreditIndicatorSettings` | JSON column (ValueConverter + ValueComparer) |
| `RecurringTransactionConfiguration.cs` | `RecurrencePattern` | Owned type (`OwnsOne`) |
| `RecurringTransferConfiguration.cs` | `RecurrencePattern` | Owned type (`OwnsOne`) |
| `RecurrencePatternJsonConverter.cs` | `RecurrencePattern` | Custom `JsonConverter<RecurrencePattern>` |
| `TransactionRepository.cs` | `DailyTotal` | LINQ projection |

**Migration snapshots:** ~24 migration Designer.cs files contain fully-qualified type name strings. These are historical snapshots and do NOT need modification. However, `BudgetDbContextModelSnapshot.cs` (~16 references) **will need regeneration** after renames.

**Key detail:** `RecurrencePatternJsonConverter` hardcodes factory method calls (`RecurrencePattern.CreateDaily()`, etc.) and its generic type registration — this must be updated alongside the type rename.

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

### Phase 3: Rename Recurring Value Objects (excl. RecurrencePattern)

**Objective:** Rename `BillInfo`, `RecurringInstanceInfo`, `RecurringTransferInstanceInfo`, `ImportPattern`

**Tasks:**
- [ ] Rename types and files
- [ ] Update all references
- [ ] Verify build and tests

### Phase 4: Rename `RecurrencePattern` (Isolated — Highest Risk)

**Objective:** Rename `RecurrencePattern` → `RecurrencePatternValue`

> ⚠️ **This is the highest-impact rename** (~299 references, 95 in Infrastructure alone). It touches two EF owned-type configurations, a custom JSON converter (`RecurrencePatternJsonConverter`), and the migration model snapshot. Execute in isolation with dedicated verification.

**Tasks:**
- [ ] Rename type and file
- [ ] Update `RecurrencePatternJsonConverter` (type name, generic parameter, factory calls)
- [ ] Update `RecurringTransactionConfiguration.cs` and `RecurringTransferConfiguration.cs` owned-type configs
- [ ] Update all Application/Domain/Test references
- [ ] Regenerate `BudgetDbContextModelSnapshot.cs`
- [ ] Verify build and tests

### Phase 5: Rename Reconciliation Value Objects

**Objective:** Rename `MatchingTolerances`, `DailyTotal`, `TransactionMatchResult`

> **Note:** `MatchingTolerances` is a `sealed class` (not a record) that manually implements `IEquatable<MatchingTolerances>`. Ensure the generic parameter is also updated to `IEquatable<MatchingTolerancesValue>`.

**Tasks:**
- [ ] Rename types and files
- [ ] Update `IEquatable<>` generic parameter on `MatchingTolerances`
- [ ] Update all references
- [ ] Verify build and tests

### Phase 6: Rename Paycheck Value Objects

**Objective:** Rename `PaycheckAllocation`, `PaycheckAllocationSummary`, `PaycheckAllocationWarning`

**Tasks:**
- [ ] Rename types and files
- [ ] Update all references
- [ ] Verify build and tests

### Phase 7: Verify No Schema Changes

**Tasks:**
- [ ] Run `dotnet ef migrations has-pending-model-changes`
- [ ] Ensure no new migration is needed (column names should be stable)
- [ ] Regenerate `BudgetDbContextModelSnapshot.cs` if type name strings changed
- [ ] Full test suite green

**Commit:**
```bash
git commit -m "refactor(domain): rename value objects with Value suffix

- GeoCoordinate → GeoCoordinateValue
- TransactionLocation → TransactionLocationValue
- MatchingTolerances → MatchingTolerancesValue
- DailyTotal → DailyTotalValue
- TransactionMatchResult → TransactionMatchResultValue
- ColumnMapping → ColumnMappingValue
- SkipRowsSettings → SkipRowsSettingsValue
- DebitCreditIndicatorSettings → DebitCreditIndicatorSettingsValue
- DuplicateDetectionSettings → DuplicateDetectionSettingsValue
- BillInfo → BillInfoValue
- RecurrencePattern → RecurrencePatternValue
- RecurringInstanceInfo → RecurringInstanceInfoValue
- RecurringTransferInstanceInfo → RecurringTransferInstanceInfoValue
- ImportPattern → ImportPatternValue
- PaycheckAllocation → PaycheckAllocationValue
- PaycheckAllocationSummary → PaycheckAllocationSummaryValue
- PaycheckAllocationWarning → PaycheckAllocationWarningValue
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

- **Medium-High risk**: Wide rename across ~108+ files. Mechanically safe with IDE rename but `RecurrencePattern` (~299 refs) requires careful attention to EF configs and custom JSON converter.
- **Merge conflicts**: Significant — any branch touching domain types will need rebasing. Execute when no other domain-touching branches are in flight.
- **Database**: Low risk — EF Fluent API decouples column names from type names (verified). `BudgetDbContextModelSnapshot.cs` must be regenerated but no schema migration is expected.
- **`RecurrencePattern` is isolated risk**: Custom JSON converter, two owned-type configs, 95 Infrastructure references. Isolated in its own phase (Phase 4) for dedicated verification.

---

## References

- Coding standard §5: "Domain value objects end with `Value`."
- Coding standard §12: "Value objects: Immutable, equality by components."

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-02-26 | Initial draft from codebase audit | @copilot |
| 2026-03-01 | Evaluation: added 4 missed VOs (Paycheck + TransactionMatchResult), updated count to 17, added reference count table, split RecurrencePattern into isolated phase, revised effort to 3-5 days, removed Contracts/Api/Client from impact (zero refs), added EF storage detail table, added borderline exclusions section | @copilot |
