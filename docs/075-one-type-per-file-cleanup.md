# Feature 075: One Type Per File Cleanup
> **Status:** Complete
> **Priority:** Medium (code standards / maintainability)
> **Estimated Effort:** Medium (3-4 days)
> **Dependencies:** None

## Overview

The coding standard (§18) requires "one top-level class/record/struct per file" with the file name matching the type name. A comprehensive audit found **39 files containing ~175 top-level types** that violate this rule across all five `src/` projects. These multi-type files make it harder to locate types, increase merge conflicts, and break the convention that `Ctrl+P` on a type name finds its file.

## Problem Statement

### Current State — Complete Audit

Violations are grouped by project layer. Each slice will be delivered as an independent commit.

#### Contracts (21 files, ~107 types)

**Large multi-type files (7+ types):**

| File | Types | Count |
|------|-------|-------|
| `Contracts/Dtos/ImportDtos.cs` | `ImportPreviewRequest`, `ColumnMappingDto`, `DuplicateDetectionSettingsDto`, `DebitCreditIndicatorSettingsDto`, `ImportPreviewResult`, `ImportPreviewRow`, `ImportLocationPreview`, `ImportRecurringMatchPreview`, `ImportExecuteRequest`, `ImportTransactionData`, `ImportResult`, `ImportBatchDto`, `ImportMappingDto`, `CreateImportMappingRequest`, `UpdateImportMappingRequest` | 15 |
| `Contracts/Dtos/ReconciliationDtos.cs` | `ReconciliationMatchDto`, `MatchingTolerancesDto`, `ReconciliationStatusDto`, `RecurringInstanceStatusDto`, `ManualMatchRequest`, `FindMatchesRequest`, `FindMatchesResult`, `BulkMatchActionRequest`, `BulkMatchActionResult`, `LinkableInstanceDto` | 10 |
| `Contracts/Dtos/CategorizationRuleDto.cs` | `CategorizationRuleDto`, `CategorizationRuleCreateDto`, `CategorizationRuleUpdateDto`, `TestPatternRequest`, `TestPatternResponse`, `ApplyRulesRequest`, `ApplyRulesResponse`, `ReorderRulesRequest` | 8 |
| `Contracts/Dtos/AiDtos.cs` | `AiStatusDto`, `AiModelDto`, `AiSettingsDto`, `GenerateSuggestionsRequest`, `AnalysisResponseDto`, `DismissSuggestionRequest`, `FeedbackRequest` | 7 |
| `Contracts/Dtos/ChatDtos.cs` | `ChatSessionDto`, `ChatMessageDto`, `ChatActionDto`, `ClarificationOptionDto`, `SendMessageRequest`, `SendMessageResponse`, `ConfirmActionResponse` | 7 |
| `Contracts/Dtos/ReportDtos.cs` | `MonthlyCategoryReportDto`, `CategorySpendingDto`, `DateRangeCategoryReportDto`, `MonthlyTrendPointDto`, `SpendingTrendsReportDto`, `DayTopCategoryDto`, `DaySummaryDto` | 7 |
| `Contracts/Dtos/CategorySuggestionDtos.cs` | `CategorySuggestionDto`, `AcceptCategorySuggestionRequest`, `AcceptCategorySuggestionResultDto`, `BulkAcceptCategorySuggestionsRequest`, `SuggestedCategoryRuleDto`, `CreateRulesFromSuggestionRequest`, `CreateRulesFromSuggestionResult` | 7 |

**Medium multi-type files (3–6 types):**

| File | Types | Count |
|------|-------|-------|
| `Contracts/Dtos/RecurringTransactionDto.cs` | `RecurringTransactionDto`, `RecurringTransactionCreateDto`, `RecurringTransactionUpdateDto`, `RecurringInstanceDto`, `RecurringInstanceModifyDto`, `ImportPatternsDto` | 6 |
| `Contracts/Dtos/RecurringTransferDto.cs` | `RecurringTransferDto`, `RecurringTransferCreateDto`, `RecurringTransferUpdateDto`, `RecurringTransferInstanceDto`, `RecurringTransferInstanceModifyDto` | 5 |
| `Contracts/Dtos/BudgetGoalDto.cs` | `BudgetGoalDto`, `BudgetGoalSetDto`, `CopyBudgetGoalsRequest`, `CopyBudgetGoalsResult` | 4 |
| `Contracts/Dtos/TransferDto.cs` | `CreateTransferRequest`, `UpdateTransferRequest`, `TransferResponse`, `TransferListItemResponse` | 4 |
| `Contracts/Dtos/UncategorizedTransactionDtos.cs` | `UncategorizedTransactionFilterDto`, `UncategorizedTransactionPageDto`, `BulkCategorizeRequest`, `BulkCategorizeResponse` | 4 |
| `Contracts/Dtos/UserDto.cs` | `UserProfileDto`, `UserSettingsDto`, `UserSettingsUpdateDto`, `ScopeDto` | 4 |
| `Contracts/Dtos/AccountDto.cs` | `AccountDto`, `AccountCreateDto`, `AccountUpdateDto` | 3 |
| `Contracts/Dtos/BudgetCategoryDto.cs` | `BudgetCategoryDto`, `BudgetCategoryCreateDto`, `BudgetCategoryUpdateDto` | 3 |
| `Contracts/Dtos/ClientConfigDto.cs` | `ClientConfigDto`, `AuthenticationConfigDto`, `OidcConfigDto` | 3 |
| `Contracts/Dtos/TransactionDto.cs` | `TransactionDto`, `TransactionCreateDto`, `TransactionUpdateDto` | 3 |

**Small multi-type files (2 types):**

| File | Types | Count |
|------|-------|-------|
| `Contracts/Dtos/AppSettingsDto.cs` | `AppSettingsDto`, `AppSettingsUpdateDto` | 2 |
| `Contracts/Dtos/BatchRealizeRequest.cs` | `BatchRealizeRequest`, `BatchRealizeItemRequest` | 2 |
| `Contracts/Dtos/BatchRealizeResultDto.cs` | `BatchRealizeResultDto`, `BatchRealizeFailure` | 2 |
| `Contracts/Dtos/BudgetProgressDto.cs` | `BudgetProgressDto`, `BudgetSummaryDto` | 2 |

#### Application (7 files, ~26 types)

| File | Types | Count |
|------|-------|-------|
| `Application/Ai/IAiService.cs` | `IAiService`, `AiServiceStatus`, `AiModelInfo`, `AiPrompt`, `AiResponse` | 5 |
| `Application/Chat/INaturalLanguageParser.cs` | `INaturalLanguageParser`, `ParseResult`, `AccountInfo`, `CategoryInfo`, `ChatContext` | 5 |
| `Application/Categorization/ICategorySuggestionService.cs` | `ICategorySuggestionService`, `AcceptSuggestionResult`, `SuggestedRule` | 3 |
| `Application/Categorization/IMerchantMappingService.cs` | `IMerchantMappingService`, `PatternMatch`, `LearnedMerchantMappingInfo` | 3 |
| `Application/Categorization/IRuleSuggestionService.cs` | `IRuleSuggestionService`, `RuleSuggestionAnalysis`, `AnalysisProgress` | 3 |
| `Application/Chat/IChatService.cs` | `IChatService`, `ChatResult`, `ActionExecutionResult` | 3 |
| `Application/Import/ImportExecuteRequestValidator.cs` | `ImportExecuteRequestValidator`, `ImportValidationResult` | 2 |
| `Application/Settings/IAppSettingsService.cs` | `IAppSettingsService`, `AiSettingsData` | 2 |

#### Domain (1 file, 7 types)

| File | Types | Count |
|------|-------|-------|
| `Domain/Chat/ChatAction.cs` | `ChatAction`, `CreateTransactionAction`, `CreateTransferAction`, `CreateRecurringTransactionAction`, `CreateRecurringTransferAction`, `ClarificationNeededAction`, `ClarificationOption` | 7 |

> **Design note:** `ChatAction` is a base class with derived types forming a sealed hierarchy. Consider whether these should remain co-located (nested types inside `ChatAction`) or split. Decision deferred to implementation.

#### Api (2 files, 6 types)

| File | Types | Count |
|------|-------|-------|
| `Api/Controllers/ImportController.cs` | `ImportController`, `SuggestMappingRequest`, `DeleteBatchResult` | 3 |
| `Api/Controllers/MerchantMappingsController.cs` | `MerchantMappingsController`, `LearnedMerchantMappingDto`, `LearnMerchantMappingRequest` | 3 |

> **Design note:** These files embed request/response DTOs inside controller files. The DTOs should be extracted to `BudgetExperiment.Contracts` (or kept Api-local in a dedicated `Models/` folder), which is a minor design decision beyond pure file splitting.

#### Client (8 files, ~29 types)

| File | Types | Count |
|------|-------|-------|
| `Client/Components/Common/ComponentEnums.cs` | `ModalSize`, `SpinnerSize`, `ButtonSize`, `ButtonVariant`, `BadgeVariant`, `BadgeSize`, `AlertVariant`, `BottomSheetHeight` | 8 |
| `Client/Models/ImportModels.cs` | `CsvParseResultModel`, `DeleteBatchResultModel`, `ColumnMappingState`, `ImportWizardState` | 4 |
| `Client/Components/Charts/BarChartData.cs` | `BarChartGroup`, `BarChartValue`, `BarChartSeries` | 3 |
| `Client/Services/ChatContextService.cs` | `ChatPageContext`, `IChatContextService`, `ChatContextService` | 3 |
| `Client/Services/IToastService.cs` | `ToastLevel`, `ToastItem`, `IToastService` | 3 |
| `Client/Services/ScopeService.cs` | `ScopeService`, `ScopeOption` | 2 |
| `Client/Services/ThemeService.cs` | `ThemeService`, `AccessibilityState` | 2 |

### Target State

Every top-level type lives in its own file named after the type (e.g., `ImportPreviewRequest.cs`, `ColumnMappingDto.cs`). Multi-type files are removed. Namespaces remain unchanged.

---

## User Stories

### US-075-001: Split Contracts DTO Files (Large)
**As a** developer
**I want to** have one DTO per file in `BudgetExperiment.Contracts`
**So that** I can locate types quickly and reduce merge conflicts.

**Acceptance Criteria:**
- [x] All 21 multi-type Contracts/Dtos files are split into individual files
- [x] File names match type names exactly
- [x] Namespaces remain unchanged
- [x] All existing tests and builds pass

### US-075-002: Split Application Service Interface Files
**As a** developer
**I want to** have one type per file in `BudgetExperiment.Application`
**So that** helper types (result records, value types) are discoverable independently.

**Acceptance Criteria:**
- [x] All 7 multi-type Application files are split
- [x] Interfaces remain in files named after the interface
- [x] Helper types (e.g., `AcceptSuggestionResult`) get their own files
- [x] Build succeeds and tests pass

### US-075-003: Split Domain ChatAction Hierarchy
**As a** developer
**I want to** either split or nest the `ChatAction` type hierarchy
**So that** the Domain project follows the one-type-per-file rule.

**Acceptance Criteria:**
- [x] `ChatAction.cs` contains only one top-level type, OR derived types are nested inside `ChatAction`
- [x] No behavioral changes
- [x] Build and tests pass

### US-075-004: Extract Inline DTOs from Api Controllers
**As a** developer
**I want to** move inline request/response types out of controller files
**So that** controllers contain only routing/orchestration logic.

**Acceptance Criteria:**
- [x] `SuggestMappingRequest` and `DeleteBatchResult` extracted from `ImportController.cs`
- [x] `LearnedMerchantMappingDto` and `LearnMerchantMappingRequest` extracted from `MerchantMappingsController.cs`
- [x] Extracted types placed in `Contracts/Dtos/` or `Api/Models/` (decide during implementation)
- [x] Build succeeds

### US-075-005: Split Client Multi-Type Files
**As a** developer
**I want to** have one type per file in `BudgetExperiment.Client`
**So that** the Client project follows the same convention as all other projects.

**Acceptance Criteria:**
- [x] `ComponentEnums.cs` → 8 individual enum files
- [x] `ImportModels.cs` → 4 individual files
- [x] `BarChartData.cs` → 3 individual files
- [x] Service files (`ChatContextService.cs`, `IToastService.cs`, `ScopeService.cs`, `ThemeService.cs`) split
- [x] All component references compile; build succeeds

---

## Implementation Plan — Vertical Slices

Each slice is a self-contained commit that leaves the solution in a buildable, test-passing state. Slices are ordered from **lowest risk → highest risk** and **fewest cross-project references → most**.

### Slice 1: Contracts — Small DTO Files (2 types each)

**Risk: Minimal** — smallest files, fewest consumers, easy to verify.

**Files (4):**
- [x] `AppSettingsDto.cs` → `AppSettingsDto.cs` + `AppSettingsUpdateDto.cs`
- [x] `BatchRealizeRequest.cs` → `BatchRealizeRequest.cs` + `BatchRealizeItemRequest.cs`
- [x] `BatchRealizeResultDto.cs` → `BatchRealizeResultDto.cs` + `BatchRealizeFailure.cs`
- [x] `BudgetProgressDto.cs` → `BudgetProgressDto.cs` + `BudgetSummaryDto.cs`

**Verification:** `dotnet build` + `dotnet test` (full solution). ✅ Done (commit 81ff8e1)

### Slice 2: Contracts — CRUD DTO Files (3 types each)

**Risk: Low** — standard Create/Update/Read pattern, no cross-cutting references.

**Files (4):**
- [x] `AccountDto.cs` → `AccountDto.cs` + `AccountCreateDto.cs` + `AccountUpdateDto.cs`
- [x] `BudgetCategoryDto.cs` → `BudgetCategoryDto.cs` + `BudgetCategoryCreateDto.cs` + `BudgetCategoryUpdateDto.cs`
- [x] `TransactionDto.cs` → `TransactionDto.cs` + `TransactionCreateDto.cs` + `TransactionUpdateDto.cs`
- [x] `ClientConfigDto.cs` → `ClientConfigDto.cs` + `AuthenticationConfigDto.cs` + `OidcConfigDto.cs`

**Verification:** Build + test. ✅ Done

### Slice 3: Contracts — Medium DTO Files (4–6 types)

**Risk: Low** — more files to create but still purely mechanical.

**Files (6):**
- [x] `UncategorizedTransactionDtos.cs` → 4 files (original deleted — no type matched filename)
- [x] `UserDto.cs` → 4 files (original deleted — no type matched filename)
- [x] `BudgetGoalDto.cs` → 4 files
- [x] `TransferDto.cs` → 4 files (original deleted — no type matched filename)
- [x] `RecurringTransferDto.cs` → 5 files
- [x] `RecurringTransactionDto.cs` → 6 files

**Verification:** Build + test. ✅ Done

### Slice 4: Contracts — Large DTO Files (7+ types)

**Risk: Low-Medium** — largest number of new files; higher chance of a typo or missed reference.

**Files (7):**
- [x] `ImportDtos.cs` → 15 files (original deleted — filename ≠ type name)
- [x] `ReconciliationDtos.cs` → 10 files (original deleted — filename ≠ type name)
- [x] `CategorizationRuleDto.cs` → 8 files (original trimmed to keep `CategorizationRuleDto`)
- [x] `AiDtos.cs` → 7 files (original deleted — filename ≠ type name)
- [x] `ChatDtos.cs` → 7 files (original deleted — filename ≠ type name)
- [x] `ReportDtos.cs` → 7 files (original deleted — filename ≠ type name)
- [x] `CategorySuggestionDtos.cs` → 7 files (original deleted — filename ≠ type name)

**Verification:** Build + test. ✅ Done

### Slice 5: Application — Service Interfaces

**Risk: Low-Medium** — helper types alongside interfaces are used across the Application layer, but splitting is still purely structural.

**Files (7):**
- [x] `IAiService.cs` → `IAiService.cs` + `AiServiceStatus.cs` + `AiModelInfo.cs` + `AiPrompt.cs` + `AiResponse.cs`
- [x] `INaturalLanguageParser.cs` → 5 files
- [x] `ICategorySuggestionService.cs` → 3 files
- [x] `IMerchantMappingService.cs` → 4 files
- [x] `IRuleSuggestionService.cs` → 3 files
- [x] `IChatService.cs` → 3 files
- [x] `ImportExecuteRequestValidator.cs` → 2 files
- [x] `IAppSettingsService.cs` → 2 files

**Verification:** Build + test. ✅ Done

### Slice 6: Client — Enums, Models & Services

**Risk: Low-Medium** — Blazor components reference enum types by name; no namespace change means no Razor file edits needed.

**Files (7):**
- [x] `ComponentEnums.cs` → 8 enum files in `Components/Common/`
- [x] `ImportModels.cs` → 4 files in `Models/`
- [x] `BarChartData.cs` → 3 files in `Components/Charts/`
- [x] `IToastService.cs` → `ToastLevel.cs` + `ToastItem.cs` + `IToastService.cs` in `Services/`
- [x] `ChatContextService.cs` → `ChatPageContext.cs` + `IChatContextService.cs` + `ChatContextService.cs`
- [x] `ScopeService.cs` → `ScopeService.cs` + `ScopeOption.cs`
- [x] `ThemeService.cs` → `ThemeService.cs` + `AccessibilityState.cs`

**Verification:** Build + test. ✅ Done

### Slice 7: Domain — ChatAction Hierarchy

**Risk: Medium** — requires a design decision (split vs. nest). Inheritance hierarchy means each derived type's file needs a `using` for the base namespace (already shared, so no change needed).

**Options:**
1. **Split** into 7 files: `ChatAction.cs`, `CreateTransactionAction.cs`, etc.
2. **Nest** derived types inside `ChatAction` as nested classes (keeps hierarchy co-located, satisfies the "one top-level type" rule).

**Decision:** Split into 7 files (types referenced directly by name across projects).

**Files (1):**
- [x] `ChatAction.cs` → `ChatAction.cs` + `CreateTransactionAction.cs` + `CreateTransferAction.cs` + `CreateRecurringTransactionAction.cs` + `CreateRecurringTransferAction.cs` + `ClarificationNeededAction.cs` + `ClarificationOption.cs`

**Verification:** Build + test. ✅ Done

### Slice 8: Api — Extract Inline DTOs from Controllers

**Risk: Medium** — the extracted types become part of the public API contract. Must decide target location:
- **Option A:** Move to `BudgetExperiment.Contracts/Dtos/` (preferred if used by Client).
- **Option B:** Keep in `BudgetExperiment.Api/Models/` (if Api-internal only).

**Files (2):**
- [x] `ImportController.cs` → extract `SuggestMappingRequest`, `DeleteBatchResult` to `Api/Models/`
- [x] `MerchantMappingsController.cs` → extract `LearnedMerchantMappingDto`, `LearnMerchantMappingRequest` to `Api/Models/`

**Verification:** Build + test + verify OpenAPI spec unchanged. ✅ Done

---

## Testing Strategy

### Unit Tests
- No new tests needed — this is a pure file restructuring with no behavior changes.

### Verification (per slice)
- [x] `dotnet build c:\ws\BudgetExperiment\BudgetExperiment.sln` succeeds
- [x] `dotnet test c:\ws\BudgetExperiment\BudgetExperiment.sln` — all tests pass
- [x] For Client slices: manual UI smoke test to confirm no regressions
- [x] For Api slices: verify OpenAPI spec output is unchanged

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| **Build break from missed reference** | Low | Low | Build verification after each slice; namespaces don't change |
| **Merge conflicts in active branches** | Medium | Medium | Communicate timing; do during low-activity window; each slice is small enough to rebase against |
| **Api contract change** (Slice 8) | Low | High | Compare OpenAPI spec before/after; keep types in same namespace |
| **Domain hierarchy breakage** (Slice 7) | Low | Medium | Choose nest-vs-split based on usage analysis; run domain tests |
| **Razor component breakage** (Slice 6) | Low | Low | Namespaces unchanged; `@using` directives already import by namespace |

---

## References

- Coding standard §18: "One top-level class/record/struct per file."
- Audit script: PowerShell regex scan of `src/**/*.cs` for files with >1 top-level type declaration.

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-02-26 | Initial draft from codebase audit (9 files identified) | @copilot |
| 2026-02-28 | Comprehensive re-audit: expanded to 39 files / ~175 types across all layers; restructured into 8 vertical slices ordered by risk | @copilot |
| 2026-02-28 | Slice 1 complete: split 4 small (2-type) Contracts DTO files | @copilot |
| 2026-02-28 | Slice 2 complete: split 4 CRUD (3-type) Contracts DTO files | @copilot |
| 2026-02-28 | Slice 3 complete: split 6 medium (4–6 type) Contracts DTO files; 3 originals deleted (filename ≠ type name) | @copilot |
| 2026-02-28 | Slice 4 complete: split 7 large (7–15 type) Contracts DTO files; 54 new files, 6 originals deleted, 1 trimmed | @copilot |
| 2026-02-28 | Slice 5 complete: split 8 Application service interface files; 19 new files, 8 originals trimmed | @copilot |
| 2026-03-01 | Slice 6 complete: split 7 Client multi-type files; 22 new files, 3 originals deleted, 4 originals trimmed | @copilot |
| 2026-03-01 | Slice 7 complete: split Domain ChatAction.cs into 7 files (split approach chosen) | @copilot |
| 2026-03-01 | Slice 8 complete: extracted 4 inline DTOs from 2 Api controllers to Api/Models/ | @copilot |
