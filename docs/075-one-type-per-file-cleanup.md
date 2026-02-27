# Feature 075: One Type Per File Cleanup
> **Status:** Planning
> **Priority:** Medium (code standards / maintainability)
> **Estimated Effort:** Small-Medium (1-2 days)
> **Dependencies:** None

## Overview

The coding standard (§18) requires "one top-level class/record/struct per file" with the file name matching the type name. An audit found **10 files containing 69 top-level types** that violate this rule. These multi-type files make it harder to locate types, increase merge conflicts, and break the convention that `Ctrl+P` on a type name finds its file.

## Problem Statement

### Current State

The following files contain multiple top-level types:

| File | Types | Count |
|------|-------|-------|
| `Contracts/Dtos/ImportDtos.cs` | `ImportPreviewRequest`, `ColumnMappingDto`, `DuplicateDetectionSettingsDto`, `DebitCreditIndicatorSettingsDto`, `ImportPreviewResult`, `ImportPreviewRow`, `ImportLocationPreview`, `ImportRecurringMatchPreview`, `ImportExecuteRequest`, `ImportTransactionData`, `ImportResult`, `ImportBatchDto`, `ImportMappingDto`, `CreateImportMappingRequest`, `UpdateImportMappingRequest` | 15 |
| `Contracts/Dtos/ReconciliationDtos.cs` | `ReconciliationMatchDto`, `MatchingTolerancesDto`, `ReconciliationStatusDto`, `RecurringInstanceStatusDto`, `ManualMatchRequest`, `FindMatchesRequest`, `FindMatchesResult`, `BulkMatchActionRequest`, `BulkMatchActionResult`, `LinkableInstanceDto` | 10 |
| `Contracts/Dtos/ComponentEnums.cs` (Client) | `ModalSize`, `SpinnerSize`, `ButtonSize`, `ButtonVariant`, `BadgeVariant`, `BadgeSize`, `AlertVariant`, `BottomSheetHeight` | 8 |
| `Contracts/Dtos/AiDtos.cs` | `AiStatusDto`, `AiModelDto`, `AiSettingsDto`, `GenerateSuggestionsRequest`, `AnalysisResponseDto`, `DismissSuggestionRequest`, `FeedbackRequest` | 7 |
| `Contracts/Dtos/ChatDtos.cs` | `ChatSessionDto`, `ChatMessageDto`, `ChatActionDto`, `ClarificationOptionDto`, `SendMessageRequest`, `SendMessageResponse`, `ConfirmActionResponse` | 7 |
| `Contracts/Dtos/ReportDtos.cs` | `MonthlyCategoryReportDto`, `CategorySpendingDto`, `DateRangeCategoryReportDto`, `MonthlyTrendPointDto`, `SpendingTrendsReportDto`, `DayTopCategoryDto`, `DaySummaryDto` | 7 |
| `Contracts/Dtos/CategorySuggestionDtos.cs` | `CategorySuggestionDto`, `AcceptCategorySuggestionRequest`, `AcceptCategorySuggestionResultDto`, `BulkAcceptCategorySuggestionsRequest`, `SuggestedCategoryRuleDto`, `CreateRulesFromSuggestionRequest`, `CreateRulesFromSuggestionResult` | 7 |
| `Contracts/Dtos/UncategorizedTransactionDtos.cs` | `UncategorizedTransactionFilterDto`, `UncategorizedTransactionPageDto`, `BulkCategorizeRequest`, `BulkCategorizeResponse` | 4 |
| `Client/Models/ImportModels.cs` | `CsvParseResultModel`, `DeleteBatchResultModel`, `ColumnMappingState`, `ImportWizardState` | 4 |

### Target State

Each top-level type lives in its own file named after the type (e.g., `ImportPreviewRequest.cs`, `ColumnMappingDto.cs`). Multi-type files are removed.

---

## User Stories

### US-075-001: Split Multi-Type DTO Files
**As a** developer
**I want to** have one DTO per file in `BudgetExperiment.Contracts`
**So that** I can locate types quickly and reduce merge conflicts.

**Acceptance Criteria:**
- [ ] Each of the 7 multi-type Contracts/Dtos files is split into individual files
- [ ] File names match type names exactly
- [ ] Namespaces remain unchanged
- [ ] All existing tests and builds pass

### US-075-002: Split Client Multi-Type Files
**As a** developer
**I want to** have one type per file in `BudgetExperiment.Client`
**So that** the Client project follows the same convention as all other projects.

**Acceptance Criteria:**
- [ ] `ComponentEnums.cs` is split into 8 individual enum files (e.g., `ModalSize.cs`, `SpinnerSize.cs`)
- [ ] `ImportModels.cs` is split into 4 individual files
- [ ] All component references updated if needed
- [ ] Build succeeds

---

## Implementation Plan

### Phase 1: Split Contracts DTO Files

**Objective:** Extract all 57 types from 7 multi-type DTO files into individual files.

**Tasks:**
- [ ] Split `ImportDtos.cs` → 15 files
- [ ] Split `ReconciliationDtos.cs` → 10 files
- [ ] Split `AiDtos.cs` → 7 files
- [ ] Split `ChatDtos.cs` → 7 files
- [ ] Split `ReportDtos.cs` → 7 files
- [ ] Split `CategorySuggestionDtos.cs` → 7 files
- [ ] Split `UncategorizedTransactionDtos.cs` → 4 files
- [ ] Delete original multi-type files
- [ ] Verify build passes

**Commit:**
```bash
git commit -m "refactor(contracts): split multi-type DTO files into one-per-file

- Split 7 multi-type files into 57 individual files
- Each file named after its single top-level type
- Enforces one-type-per-file coding standard (§18)

Refs: #075"
```

### Phase 2: Split Client Files

**Objective:** Extract types from `ComponentEnums.cs` and `ImportModels.cs`.

**Tasks:**
- [ ] Split `ComponentEnums.cs` → 8 enum files in `Components/Common/`
- [ ] Split `ImportModels.cs` → 4 files in `Models/`
- [ ] Verify build and UI functionality

**Commit:**
```bash
git commit -m "refactor(client): split component enums and import models into individual files

- ComponentEnums.cs → 8 individual enum files
- ImportModels.cs → 4 individual files

Refs: #075"
```

---

## Testing Strategy

### Unit Tests
- No new tests needed — this is a pure file restructuring with no behavior changes.

### Verification
- [ ] `dotnet build` succeeds for entire solution
- [ ] All existing unit tests pass
- [ ] All existing integration tests pass

---

## Risk Assessment

- **Low risk**: Pure file-level refactoring with no code changes. Namespaces stay the same.
- **Merge conflicts**: Any in-flight branches touching these files will need rebasing.

---

## References

- Coding standard §18: "One top-level class/record/struct per file."

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-02-26 | Initial draft from codebase audit | @copilot |
