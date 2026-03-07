# Feature 091: Test Coverage Gaps — Client Components & Services

> **Status:** Done
> **Priority:** Medium (60+ untested Blazor components, 7+ untested services)
> **Dependencies:** None

## Overview

A test coverage audit identified significant gaps in Blazor component and client service test coverage. While many critical pages, charts, and services have tests, the majority of form components, common UI components, import components, AI components, and reconciliation components lack bUnit tests. Per §15, optional bUnit tests for component logic are recommended — thin components may be excluded, but components with meaningful logic should be tested.

## Problem Statement

### Current State

**Client Test Coverage Summary:**
- Pages: Mostly covered via API integration tests
- Charts: Well covered (9/9 chart types)
- Forms: 2/16 tested (BudgetGoalModal, QuickAddForm only)
- Common UI: 0/14+ tested
- Import: 0/13 tested
- AI: 0/9 tested
- Reconciliation: 0/6+ tested
- Navigation: 0/2 tested
- Client Services: 13/20+ tested

### Target State

All components with meaningful logic (form validation, state management, conditional rendering) have bUnit tests. Pure display-only components may be excluded with justification.

---

## Untested Components by Category

### Forms (14 untested)
- AccountForm, ApplyRulesDialog, CategoryForm, EditInstanceDialog, EditRecurringForm, EditRecurringTransferForm, PastDueReviewModal, RecurringTransactionForm, RecurringTransferForm, RuleForm, RuleTestPanel, TransactionForm, TransferDialog

### Common/Shared UI (14+ untested)
- Badge, BottomSheet, Button, Card, ConfirmDialog, EmptyState, ErrorAlert, FormField, Icon, LoadingSpinner, Modal, PageHeader, SwipeContainer, ThemeToggle, ToastContainer

### Import (13 untested)
- AmountModeSelector, ColumnMappingEditor, CsvPreviewTable, DateFormatSelector, DuplicateWarningCard, FileUploadZone, ImportHistoryList, ImportPreviewTable, ImportSummaryCard, IndicatorSettingsEditor, SavedMappingSelector, SavedMappingsManager, SkipRowsInput

### AI (9 untested)
- AiOnboardingPanel, AiSettingsForm, AiStatusBadge, AnalysisProgressDialog, AnalysisSummaryCard, CategorySuggestionCard, SuggestionCard, SuggestionDetailDialog, SuggestionList

### Reconciliation (6+ untested)
- ToleranceSettingsPanel, MatchReviewModal, ManualMatchDialog, LinkableInstancesDialog, ImportPatternsDialog, ConfidenceBadge

### Navigation (2 untested)
- NavMenu, ScopeSwitcher

### Client Services (7+ untested)
- AiApiService, ChatApiService, ExportDownloadService, ImportApiService, ScopeService, ThemeService, VersionService

---

## User Stories

### US-091-001: Test Form Components
**As a** developer  
**I want** form components with validation and state management to have bUnit tests  
**So that** form behavior, validation, and submission logic are verified.

### US-091-002: Test Common UI Components
**As a** developer  
**I want** reusable UI primitives with logic (Modal, ConfirmDialog, Toast) to have bUnit tests  
**So that** shared component behavior is verified.

### US-091-003: Test Import Components
**As a** developer  
**I want** import workflow components to have bUnit tests  
**So that** the complex multi-step import flow is verified.

### US-091-004: Test Client API Services
**As a** developer  
**I want** all client API service classes to have unit tests  
**So that** HTTP call construction, error handling, and response parsing are verified.

---

## Implementation Plan

### Phase 1: Client API Services (Highest Impact)

**Objective:** Test untested API service classes that wrap HTTP calls.

**Tasks:**
- [x] Create `AiApiServiceTests.cs`
- [x] Create `ChatApiServiceTests.cs`
- [x] Create `ExportDownloadServiceTests.cs`
- [x] Create `ImportApiServiceTests.cs`
- [x] Create `ScopeServiceTests.cs`
- [x] Create `ThemeServiceTests.cs`
- [x] Create `VersionServiceTests.cs`
- [x] All tests pass

### Phase 2: Form Components (High Interaction)

**Objective:** Test form components with validation logic.

**Tasks:**
- [x] Create tests for AccountForm, TransactionForm, CategoryForm
- [x] Create tests for RecurringTransactionForm, RecurringTransferForm
- [x] Create tests for TransferDialog, RuleForm
- [x] Create tests for EditRecurringForm, EditRecurringTransferForm
- [x] Create tests for remaining form dialogs (RuleTestPanel, ApplyRulesDialog, PastDueReviewModal, EditInstanceDialog)
- [x] All tests pass

### Phase 3: Common UI Components

**Objective:** Test reusable components with behavioral logic.

**Tasks:**
- [x] Create tests for Modal, ConfirmDialog, BottomSheet (interaction logic)
- [x] Create tests for ToastContainer (state management)
- [x] Create tests for FormField (validation display)
- [x] Create tests for ErrorAlert (conditional rendering, retry/dismiss callbacks)
- [x] Create tests for PageHeader (back button, subtitle, actions)
- [x] Create tests for ThemeToggle (dropdown state, theme selection)
- [x] Create tests for LoadingSpinner (size classes, message, full-page mode)
- [x] Evaluate remaining components — skip pure display-only components
- [x] All tests pass

**Evaluation Notes (Skipped):**
- **Icon**: Skipped — pure display component with large SVG path switch. ThemeService integration tested indirectly by components that use Icon.
- **Badge, Button, Card, EmptyState, SwipeContainer**: Already had tests from prior work.

### Phase 4: Import & AI Components

**Objective:** Test import workflow and AI suggestion components.

**Tasks:**
- [x] Create tests for import components (AmountModeSelector, ColumnMappingEditor, CsvPreviewTable, DateFormatSelector, DuplicateWarningCard, FileUploadZone, ImportHistoryList, ImportPreviewTable, ImportSummaryCard, IndicatorSettingsEditor, SavedMappingSelector, SavedMappingsManager, SkipRowsInput) — 120 tests
- [x] Create tests for AI suggestion components (AiOnboardingPanel, AiSettingsForm, AiStatusBadge, AnalysisProgressDialog, AnalysisSummaryCard, CategorySuggestionCard, SuggestionCard, SuggestionDetailDialog, SuggestionList) — 74 tests
- [x] All tests pass

### Phase 5: Reconciliation & Navigation

**Objective:** Test remaining component categories.

**Tasks:**
- [x] Create tests for reconciliation components (ConfidenceBadge, MatchReviewModal, ToleranceSettingsPanel, ImportPatternsDialog, LinkableInstancesDialog, ManualMatchDialog) — 58 tests
- [x] Create tests for NavMenu, ScopeSwitcher — 26 tests
- [x] All tests pass

---

## Prioritization Notes

Not all 60+ components necessarily need individual test files. Apply these guidelines:
- **Must test:** Components with form validation, state management, conditional rendering, event handling
- **Should test:** Components with non-trivial parameter-driven behavior
- **May skip:** Pure display-only components that just render parameters (Badge, Icon, LoadingSpinner) — document rationale if skipped
