# Feature 095: Client Test Coverage — Phase 2 (Pages, Services, Chat, Display)

> **Status:** In Progress
> **Priority:** Medium-High (45.3% line coverage → target 65%+)
> **Dependencies:** Feature 091 (Done)

## Overview

A follow-up coverage audit after Feature 091 reveals that while individual components and services gained strong coverage, **page-level components** (which orchestrate loading, state, and child components) remain almost entirely untested. Several API services also have significant uncovered code paths. This feature addresses the remaining gaps to bring overall Client test coverage from **41.3%** to a target of **65%+**.

## Problem Statement

### Current State (Post-Feature 091)

| Metric               | Value                    |
|-----------------------|--------------------------|
| Line coverage         | **41.3%** (7,093 / 17,144) |
| Branch coverage       | **46.1%** (2,962 / 6,424)  |
| Method coverage       | **59.1%** (1,496 / 2,531)  |
| Total tests (Client)  | 1,337 passing, 1 skipped   |

**Key Gaps:**
- **18 of 21 page components** have 0% coverage (these are the largest files in the project, 241–1,099 lines each)
- **BudgetApiService** (1,410 lines) has only 10.3% coverage — it is the single largest service
- **Chat feature cluster** (ChatPanel 0%, ChatInput 22%, ChatMessageBubble 17.8%, MobileChatSheet 38.4%) totals 953 lines with minimal coverage
- **6 Display components** at 0% (BudgetAlert, CategoryBudgetCard, CategoryCard, PastDueAlert, RuleCard, ScopeBadge)
- **Several models** with meaningful logic at 0% or low coverage (ColumnMappingState 26.6%, TransactionListItem 14.4%, ImportWizardState 0%)

### Target State

- Overall Client line coverage ≥ **65%**
- All pages with >200 lines have at least basic bUnit tests covering initialization, loading state, and primary user interactions
- BudgetApiService coverage ≥ 70%
- Chat components ≥ 60%
- Display components with logic ≥ 80%

---

## Coverage Gap Inventory

### Pages at 0% (Sorted by Complexity)

| Page                    | Lines | @code starts | Complexity | Priority |
|-------------------------|-------|-------------|------------|----------|
| Import                  | 1,099 | L373        | Very High  | P1       |
| Calendar                | 925   | L166        | Very High  | P1       |
| AccountTransactions     | 756   | L162        | High       | P1       |
| CategorySuggestions     | 648   | L197        | High       | P1       |
| Reconciliation          | 565   | L258        | High       | P2       |
| Settings                | 564   | L309        | High       | P2       |
| Uncategorized           | 526   | L175        | High       | P2       |
| PaycheckPlanner         | 500   | L270        | High       | P2       |
| Rules                   | 442   | L98         | Medium     | P2       |
| Categories              | 430   | L123        | Medium     | P2       |
| Recurring               | 423   | L149        | Medium     | P3       |
| AiSuggestions           | 386   | L70         | Medium     | P3       |
| RecurringTransfers      | 378   | L137        | Medium     | P3       |
| Budget                  | 374   | L162        | Medium     | P3       |
| Accounts                | 349   | L99         | Medium     | P3       |
| Transfers               | 322   | L126        | Medium     | P3       |
| Onboarding              | 241   | L149        | Low-Med    | P3       |
| ComponentShowcase       | 144   | L77         | Low        | Skip     |

### Report Pages at 0%

| Report Page              | Lines | Priority |
|--------------------------|-------|----------|
| CustomReportBuilder      | 472   | P2       |
| MonthlyTrendsReport      | 329   | P3       |
| MonthlyCategoriesReport  | 278   | P3       |

### Services with Low Coverage

| Service                        | Lines | Current Coverage | Priority |
|--------------------------------|-------|-----------------|----------|
| BudgetApiService               | 1,410 | 10.3%           | P1       |
| CategorySuggestionApiService   | 319   | 10.6%           | P1       |
| ReconciliationApiService       | 244   | 25%             | P2       |
| AiApiService                   | 281   | 31.3%           | P2       |
| ImportApiService               | 267   | 31.5%           | P2       |
| ExportDownloadService          | 133   | 28.5%           | P2       |
| ThemeService                   | 228   | 58.1%           | P3       |
| ChatApiService                 | 187   | 49.1%           | P3       |

### Chat Components (Feature Cluster)

| Component         | Lines | Current Coverage | Priority |
|-------------------|-------|-----------------|----------|
| ChatPanel         | 321   | 0%              | P2       |
| MobileChatSheet   | 337   | 38.4%           | P2       |
| ChatMessageBubble | 231   | 17.8%           | P2       |
| ChatInput         | 64    | 22.2%           | P3       |

### Display Components at 0%

| Component          | Lines | Priority |
|--------------------|-------|----------|
| CategoryBudgetCard | 137   | P2       |
| CategoryCard       | 109   | P2       |
| RuleCard           | 100   | P2       |
| BudgetAlert        | 73    | P3       |
| ScopeBadge         | 56    | P3       |
| PastDueAlert       | 41    | P3       |

### Models with Low/No Coverage

| Model                | Lines | Current Coverage | Priority |
|----------------------|-------|-----------------|----------|
| TransactionListItem  | 136   | 14.4%           | P2       |
| ImportWizardState    | 118   | 0%              | P2       |
| ColumnMappingState   | 52    | 26.6%           | P3       |
| CsvParseResultModel  | 41    | 0%              | P3       |

### Components with Moderate Coverage Worth Improving

| Component               | Current | Target | Priority |
|--------------------------|---------|--------|----------|
| ImportPatternsDialog     | 45%     | 75%    | P3       |
| LocationFields           | 47.3%   | 75%    | P3       |
| ManualMatchDialog        | 48.4%   | 70%    | P3       |
| ToleranceSettingsPanel   | 39.1%   | 70%    | P3       |
| LinkableInstancesDialog  | 57%     | 75%    | P3       |
| ChartTooltip             | 31.2%   | 70%    | P3       |
| ChartLegend              | 43.2%   | 70%    | P3       |

### Layouts (0% — May Skip)

| Layout          | Lines | Decision   |
|-----------------|-------|------------|
| MainLayout      | ~80   | May skip — thin shell with navigation/auth wiring |
| CalendarLayout  | ~30   | May skip — minimal wrapper |
| EmptyLayout     | ~10   | Skip — trivial |

---

## User Stories

### US-095-001: Test Page Components
**As a** developer
**I want** page components to have bUnit tests covering initialization, loading states, error handling, and primary user interactions
**So that** page-level orchestration logic (data loading, CRUD operations, filtering, sorting) is verified.

**Acceptance Criteria:**
- [ ] All P1 pages have bUnit tests (Import, Calendar, AccountTransactions, CategorySuggestions)
- [ ] All P2 pages have bUnit tests (Reconciliation, Settings, Uncategorized, PaycheckPlanner, Rules, Categories, CustomReportBuilder)
- [x] All P3 pages have bUnit tests (Recurring, AiSuggestions, RecurringTransfers, Budget, Accounts, Transfers, Onboarding, MonthlyTrendsReport, MonthlyCategoriesReport)

### US-095-002: Improve API Service Coverage
**As a** developer
**I want** BudgetApiService and CategorySuggestionApiService to have comprehensive unit tests
**So that** all HTTP call paths, error handling, and ETag/concurrency logic are verified.

**Acceptance Criteria:**
- [x] BudgetApiService coverage ≥ 70% (76.2%)
- [x] CategorySuggestionApiService coverage ≥ 70% (76.4%)
- [x] ReconciliationApiService coverage ≥ 60%
- [x] AiApiService coverage ≥ 60%
- [x] ImportApiService coverage ≥ 60%
- [x] ExportDownloadService coverage ≥ 60%

### US-095-003: Test Chat Feature Cluster
**As a** developer
**I want** chat components to have bUnit tests
**So that** the chat panel lifecycle, message rendering, input handling, and mobile sheet behavior are verified.

**Acceptance Criteria:**
- [x] ChatPanel has tests covering initialization, message display, send flow
- [x] ChatMessageBubble has tests for different message types/roles
- [x] MobileChatSheet has tests for open/close behavior
- [x] ChatInput has tests for input, submit, and disabled states

### US-095-004: Test Display Components
**As a** developer
**I want** display components with conditional rendering or interaction logic to have bUnit tests
**So that** cards, alerts, and badges render correctly for various parameter combinations.

**Acceptance Criteria:**
- [x] CategoryBudgetCard, CategoryCard, RuleCard tested (parameter rendering, click handlers)
- [x] BudgetAlert, PastDueAlert tested (conditional display logic)
- [x] ScopeBadge tested (scope-based rendering)

### US-095-005: Test Models with Logic
**As a** developer
**I want** model classes with computed properties, validation, or state management to have unit tests
**So that** client-side data transformation logic is verified.

**Acceptance Criteria:**
- [x] TransactionListItem computed properties tested
- [x] ImportWizardState state transitions tested
- [x] ColumnMappingState mapping logic tested

---

## Implementation Plan

### Phase 1: API Services — BudgetApiService & CategorySuggestionApiService ✅
**Objective:** Cover the two largest, most critical API services that back most of the UI.

**Tasks:**
- [x] Add tests for BudgetApiService CRUD operations (accounts, transactions, calendar, recurring, categories, budget goals, rules, transfers, import patterns)
- [x] Add tests for BudgetApiService settings & reports (settings, user settings, onboarding, paycheck allocation, all report types, custom report layouts)
- [x] Add tests for BudgetApiService ETag/concurrency paths (pre-existing)
- [x] Add tests for BudgetApiService filtering, pagination, sorting
- [x] Add tests for CategorySuggestionApiService — all endpoints (analyze, pending, dismissed, getById, accept, dismiss, restore, bulkAccept, previewRules, createRules)
- [x] Verify coverage ≥ 70% for both (BudgetApiService: 76.2%, CategorySuggestionApiService: 76.4%)

**Results:** +149 tests, overall Client coverage 41.3% → 45.3%

**Commit:** `test(client): add BudgetApiService and CategorySuggestionApiService coverage`

### Phase 2: Core Pages — Calendar, Accounts, Categories, CategorySuggestions ✅
**Objective:** Test core page components that orchestrate major user workflows.

**Tasks:**
- [x] Create shared `StubChatContextService` and `StubCategorySuggestionApiService` test helpers
- [x] Create `CalendarPageTests.cs` — test month navigation, day-of-week headers, view rendering, explicit year/month params
- [x] Create `AccountsPageTests.cs` — test account cards, empty state, action buttons, balance display, scope badge
- [x] Create `CategoriesPageTests.cs` — test category grouping by type (Expense/Income/Transfer), badges, empty state
- [x] Create `CategorySuggestionsPageTests.cs` — test suggestion loading, tabs, analyze button, empty states
- [x] All tests pass

**Results:** +45 tests (1,292 → 1,337), overall Client coverage 45.3% → 48.3%

**Commit:** `test(client): add tests for Calendar, Accounts, Categories, CategorySuggestions pages`

### Phase 3: Secondary Pages — Reconciliation, Settings, Uncategorized, PaycheckPlanner, Rules, CustomReportBuilder ✅
**Objective:** Test P2 page components.

**Tasks:**
- [x] Create `ReconciliationPageTests.cs` — 16 tests (filters, summary cards, pending matches, confidence badges, instance actions)
- [x] Create `SettingsPageTests.cs` — 14 tests (tabs, recurring items, location, user prefs, version)
- [x] Create `UncategorizedPageTests.cs` — 14 tests (filters, table, sorting, selection, pagination, accounts)
- [x] Create `PaycheckPlannerPageTests.cs` — 12 tests (config form, frequency, amount, account dropdowns)
- [x] Create `RulesPageTests.cs` — 11 tests (empty state, rule display, priority info, action buttons)
- [x] Create `CustomReportBuilderPageTests.cs` — 12 tests (layout selector, templates, toolbar, builder grid)
- [x] Create `StubReconciliationApiService` and `StubAiApiService` test helpers
- [x] Extend `StubBudgetApiService` with configurable AppSettings, UserSettings, Rules, UncategorizedPage, AllocationSummary
- [x] All tests pass (1,337 → 1,416)

**Results:** +79 tests, overall Client coverage 48.3% → estimated ~51%

**Commit:** `test(client): add tests for P2 page components`

### Phase 4: Chat Components & Display Components ✅
**Objective:** Cover the chat feature cluster and remaining display components.

**Tasks:**
- [x] Create shared `StubChatApiService` test helper
- [x] Create `ChatPanelTests.cs` — 14 tests (open/close, session lifecycle, loading/error states, messaging, context)
- [x] Create `ChatMessageBubbleTests.cs` — 24 tests (role variants, action cards, confirm/cancel, clarification options, status badges, processing)
- [x] Create `ChatInputTests.cs` — 10 tests (placeholder, submit, disabled state, Enter key, input clearing)
- [x] Create `CategoryBudgetCardTests.cs` — 16 tests (status classes, amounts, edit button, icon emoji mapping)
- [x] Create `CategoryCardTests.cs` — 14 tests (active/inactive, action buttons, icon emoji, callbacks)
- [x] Create `RuleCardTests.cs` — 17 tests (pattern, priority, match type badges, case sensitivity, callbacks)
- [x] Create `BudgetAlertTests.cs` — 11 tests (null/empty, danger/warning, singular/plural, button callback)
- [x] Create `PastDueAlertTests.cs` — 8 tests (null/zero, singular/plural, amount display, callback)
- [x] Create `ScopeBadgeTests.cs` — 8 tests (null/empty, shared/personal CSS, tooltips, label toggle)
- [x] MobileChatSheet already covered (11 existing tests)
- [x] All tests pass (1,416 → 1,544)

**Results:** +128 tests, overall Client coverage estimated ~53%

**Commit:** `test(client): add chat and display component tests`

### Phase 5: Remaining Pages & Services
**Objective:** Cover P3 pages, remaining services, and models.

**Tasks:**
- [x] Create page tests for: Recurring, AiSuggestions, RecurringTransfers, Budget, Accounts, Transfers, Onboarding
- [x] Create page tests for: MonthlyTrendsReport, MonthlyCategoriesReport
- [x] Add tests for ReconciliationApiService, AiApiService, ImportApiService, ExportDownloadService
- [x] Add tests for TransactionListItem, ImportWizardState, ColumnMappingState
- [x] All tests pass (1,544 → 1,694, +150 tests)

**Results:** +150 tests, overall Client coverage estimated ~57%

**Commit:** `test(client): add P3 page, service, and model tests`

### Phase 6: Coverage Improvement for Moderate Components
**Objective:** Improve coverage for components in the 30-57% range.

**Tasks:**
- [ ] Expand ImportPatternsDialog tests (45% → 75%)
- [ ] Expand ManualMatchDialog tests (48.4% → 70%)
- [ ] Expand LocationFields tests (47.3% → 75%)
- [ ] Expand ToleranceSettingsPanel tests (39.1% → 70%)
- [ ] Expand ChartTooltip tests (31.2% → 70%)
- [ ] Expand ChartLegend tests (43.2% → 70%)
- [ ] Expand ChatApiService, ThemeService coverage
- [ ] Run final coverage report, validate ≥ 65% overall
- [ ] All tests pass

**Commit:** `test(client): improve coverage for moderate-coverage components`

---

## Prioritization Notes

- **Phase 1 (Services)** delivers the highest testability-per-effort: API services are pure C# with mocked `HttpClient`, no bUnit complexity.
- **Phase 2 (Core Pages)** covers the most complex user workflows but requires more bUnit setup (mocked services, navigation, authorization).
- **Phases 3-5** can be parallelized across developers if needed.
- **Phase 6** is polish — only pursue if the 65% target isn't met after phases 1-5.
- **Layouts** (MainLayout, CalendarLayout, EmptyLayout) are excluded — they are thin shells best validated by integration/E2E tests.
- **ComponentShowcase** is excluded — it's a developer tool, not production functionality.

## Exclusions (Documented)

| Item              | Reason                                                    |
|-------------------|-----------------------------------------------------------|
| ComponentShowcase | Developer reference page, not production code             |
| EmptyLayout       | Trivial wrapper (~10 lines, no logic)                     |
| CalendarLayout    | Minimal wrapper (~30 lines)                               |
| MainLayout        | Shell component; auth/nav wiring better tested via E2E    |
| Program.cs        | Composition root; covered by API integration tests        |
| App.razor         | Routing shell; covered by E2E tests                       |
