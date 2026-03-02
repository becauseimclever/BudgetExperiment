# Feature 080: Decompose God Services
> **Status:** Planning
> **Priority:** High (maintainability / SRP)
> **Estimated Effort:** Large (10-12 days)
> **Dependencies:** Write missing unit tests for RecurringTransactionService (prerequisite)

## Overview

The coding standard (§24) forbids "god services" exceeding ~300 lines or having too many responsibilities. An audit found **11 service files** that exceed this threshold, with `ImportService` at **1,076 lines** (3.5× the limit) being the worst offender. Several methods also exceed the ~20-line target (§8), with `ImportService.ProcessRow` at **~248 lines**.

## Problem Statement

### Current State

| Service | Lines | Responsibilities | Severity |
|---------|-------|-----------------|----------|
| `ImportService.cs` | 1,076 | CSV parsing, row processing, amount parsing, date parsing, duplicate detection, categorization, location enrichment, batch management, reconciliation, Levenshtein distance | **Critical** |
| `RuleSuggestionService.cs` | 857 | Rule suggestion generation, AI response parsing, pattern analysis, confidence scoring, transaction grouping | **Critical** |
| `NaturalLanguageParser.cs` | 556 | AI prompt building, response parsing, JSON extraction, action type mapping, parameter extraction | **High** |
| `ReconciliationService.cs` | 545 | Match finding, status calculation, bulk operations, tolerance management, instance linking | **High** |
| `ReportService.cs` | 423 | Category reports, trend reports, location reports, date range processing | **High** |
| `ChatService.cs` | 397 | Message handling, AI integration, action confirmation, session management | **Moderate** |
| `TransactionMatcher.cs` (Domain) | 372 | Match scoring, description similarity, Levenshtein distance, amount/date tolerance | **Moderate** |
| `CategorySuggestionService.cs` | 366 | Suggestion generation, batch operations, rule creation | **Moderate** |
| `MerchantKnowledgeBase.cs` | 362 | Static merchant-to-category mappings (data, not logic) | **Moderate** |
| `RecurringTransactionService.cs` | 355 | CRUD, pause/resume, skip, import patterns | **Borderline** |
| `RecurringTransferService.cs` | 348 | CRUD, pause/resume, skip, import patterns | **Borderline** |

### Long Methods (>30 lines)

| Method | File | Lines |
|--------|------|-------|
| `ProcessRow` | `ImportService.cs` | ~248 |
| `ExecuteAsync` | `ImportService.cs` | ~160 |
| `GetReconciliationStatusAsync` | `ReconciliationService.cs` | ~111 |
| `FindMatchesAsync` | `ReconciliationService.cs` | ~104 |
| `GetSpendingTrendsAsync` | `ReportService.cs` | ~86 |
| `PreviewAsync` | `ImportService.cs` | ~86 |
| `BuildCategoryReportAsync` | `ReportService.cs` | ~85 |
| `CalculateMatch` | `TransactionMatcher.cs` (Domain) | ~83 |
| `ParseAiResponse` | `NaturalLanguageParser.cs` | ~80 |
| `GetSpendingByLocationAsync` | `ReportService.cs` | ~77 |
| `Create` | `RecurringTransfer.cs` (Domain) | ~75 |
| `ConfirmActionAsync` | `ChatService.cs` | ~67 |
| `SendMessageAsync` | `ChatService.cs` | ~56 |

### Target State

- No service exceeds ~300 lines
- No method exceeds ~30 lines (with documented exceptions)
- Each service has a single clear responsibility (SRP)
- Tests are easier to write and maintain

---

## User Stories

### US-080-001: Decompose ImportService
**As a** developer
**I want to** `ImportService` broken into focused sub-services
**So that** each concern is testable independently and the service is maintainable.

**Acceptance Criteria:**
- [ ] `ImportService` orchestrates rather than implements
- [ ] Row processing extracted to `ImportRowProcessor` or similar
- [ ] Amount parsing extracted to `AmountParser`
- [ ] Date parsing extracted to `DateParser`
- [ ] Duplicate detection extracted to `DuplicateDetector`
- [ ] Location enrichment delegated properly
- [ ] Each extracted service has unit tests
- [ ] Total lines of `ImportService` ≤ 300

### US-080-002: Decompose Other Large Services
**As a** developer
**I want to** all god services brought under the 300-line threshold
**So that** the codebase follows SRP consistently.

**Acceptance Criteria:**
- [ ] `RuleSuggestionService` ≤ 300 lines (extract AI parsing, pattern analysis)
- [ ] `NaturalLanguageParser` ≤ 300 lines (extract JSON extraction, action mapping)
- [ ] `ReconciliationService` ≤ 300 lines (extract status calculation, bulk operations)
- [ ] `ReportService` ≤ 300 lines (extract report building per report type)

### US-080-003: Break Down Long Methods
**As a** developer
**I want to** long methods decomposed into shorter, named sub-methods
**So that** each method's purpose is clear and testable.

**Acceptance Criteria:**
- [ ] No method exceeds ~30 lines without documented justification
- [ ] Extracted methods have descriptive names
- [ ] Existing test coverage maintained

---

## Technical Design

### ImportService Decomposition

```
ImportService (orchestrator, ~150 lines)
├── IImportRowProcessor → ImportRowProcessor
│   ├── ParseDate()
│   ├── ParseAmount()
│   ├── MapColumns()
│   └── ValidateRow()
├── IImportDuplicateDetector → ImportDuplicateDetector
│   ├── DetectDuplicatesAsync()
│   └── CalculateSimilarity() (Levenshtein)
├── IImportLocationEnricher → existing LocationParserService
└── IImportCategorizationEnricher → existing CategorizationEngine
```

### ReportService Decomposition

```
ReportService (facade/dispatcher)
├── ICategoryReportBuilder → CategoryReportBuilder
├── ITrendReportBuilder → TrendReportBuilder
└── ILocationReportBuilder → LocationReportBuilder
```

### MerchantKnowledgeBase

This is 332 lines of static data (merchant → category mappings), not logic. Consider:
- Moving to a configuration file or embedded resource
- Keeping as-is with documentation justifying the size (data, not logic)

---

## Implementation Plan

### Phase 1: Decompose ImportService

**Objective:** Break ImportService into orchestrator + focused processors.

**Tasks:**
- [ ] Extract `ImportRowProcessor` with row-level processing logic
- [ ] Extract amount/date parsing into helper methods or services
- [ ] Extract duplicate detection into `ImportDuplicateDetector`
- [ ] Write unit tests for each extracted component
- [ ] Reduce `ImportService` to orchestration only
- [ ] Verify all import functionality unchanged

### Phase 2: Decompose RuleSuggestionService and NaturalLanguageParser

**Objective:** Extract AI parsing and pattern analysis concerns.

**Tasks:**
- [ ] Extract AI response parsing from `RuleSuggestionService`
- [ ] Extract JSON extraction from `NaturalLanguageParser`
- [ ] Write unit tests
- [ ] Verify behavior unchanged

### Phase 3: Decompose ReconciliationService and ReportService

**Objective:** Extract status calculation and report building.

**Tasks:**
- [ ] Extract reconciliation status builder
- [ ] Extract individual report builders
- [ ] Write unit tests
- [ ] Verify behavior unchanged

### Phase 4: Break Down Long Methods

**Objective:** Address remaining long methods across the codebase.

**Tasks:**
- [ ] Extract sub-methods in Domain (`TransactionMatcher.CalculateMatch`, `RecurringTransfer.Create`)
- [ ] Extract sub-methods in remaining Application services
- [ ] Ensure each extraction preserves existing test coverage

**Commit:**
```bash
git commit -m "refactor(app): decompose god services into focused components

- ImportService: Extract ImportRowProcessor, ImportDuplicateDetector
- RuleSuggestionService: Extract AI response parsing
- ReconciliationService: Extract status calculation
- ReportService: Extract per-report-type builders
- All services now ≤ 300 lines
- Long methods broken into descriptive sub-methods

Refs: #080"
```

---

## Testing Strategy

### Unit Tests
- [ ] New tests for each extracted service/component
- [ ] Existing tests updated to test through new interfaces
- [ ] Coverage maintained or improved

### Integration Tests
- [ ] Import flow end-to-end
- [ ] Report generation
- [ ] Reconciliation matching

---

## Risk Assessment

- **Medium risk**: Extracting logic requires careful interface design. Method signatures may change.
- **Behavior preservation**: Each extraction must be verified with existing tests before adding new ones.
- **DI changes**: New services need registration in `DependencyInjection.cs`.

---

## References

- Coding standard §7 (SRP): "One reason to change — extract cohesive services."
- Coding standard §8: "Short methods (< ~20 lines target)."
- Coding standard §24: "God services (> ~300 lines or too many responsibilities)."

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-02-26 | Initial draft from codebase audit | @copilot |
| 2026-03-01 | Updated line counts to actuals (all grew 30-139 lines since audit), added TransactionMatcher (372 lines), corrected effort estimate to 10-12 days, added prerequisite for RecurringTransactionService tests | @copilot |
