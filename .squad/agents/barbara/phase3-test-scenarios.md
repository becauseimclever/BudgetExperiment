# Phase 3: High-ROI bUnit Test Scenarios (Client Module)

**Goal:** Increase Client coverage from 68% → 75% through surgical, behavior-focused tests  
**Estimated Tests:** 50-75 test scenarios  
**Focus:** Budget creation, Transaction import, AI suggestions UI

---

## Coverage Gap Analysis Summary

**Current State:**
- Client module: 73.8% coverage (Phase 1B result)
- ViewModel tests exist but lack edge case coverage
- Many components have basic render tests but no behavior validation
- Critical user flows (budget goal creation, import wizard, AI suggestions) undertested

**Zero-Coverage Components (Priority 1 — Critical User Workflows):**
1. `Pages/Calendar.razor` — complex month view with budget panels (0%)
2. `Pages/Reconciliation.razor` — statement matching workflow (0%)
3. `Components/AI/UnifiedSuggestionCard.razor` — suggestion accept/dismiss (untested)
4. `Components/AI/SuggestionGroup.razor` — batch operations (untested)
5. `Components/AI/AnalysisInlineProgress.razor` — progress states (untested)
6. `Components/AI/AiSetupBanner.razor` — setup flow states (untested)
7. `Components/Import/ColumnMappingEditor.razor` — partial coverage, missing validation edge cases
8. `Components/Forms/BudgetGoalModal.razor` — partial coverage, missing validation/error paths

---

## Flow 1: Budget Creation (Target: 18-22 tests)

### **1.1 BudgetGoalModal Component (8 tests)**
*Component:* `Components/Forms/BudgetGoalModal.razor`  
*Existing Coverage:* Partial (basic render, title display)  
*Missing Tests:*

1. **Validation: Empty amount shows error message**
   - Arrange: Modal in Create mode, TargetAmount = null/empty
   - Act: Click Save
   - Assert: ValidationMessage displayed, Save callback NOT invoked

2. **Validation: Negative amount shows error message**
   - Arrange: Modal in Create mode, TargetAmount = -50
   - Act: Click Save
   - Assert: ValidationMessage displayed, Save callback NOT invoked

3. **Validation: Zero amount allowed (income tracking use case)**
   - Arrange: Modal in Create mode, TargetAmount = 0
   - Act: Click Save
   - Assert: OnSave callback invoked with 0 amount, no validation error

4. **Create mode: Save triggers OnSave with correct parameters**
   - Arrange: Modal visible, Mode=Create, TargetAmount=100, CategoryId/Year/Month set
   - Act: Click Save
   - Assert: OnSave callback invoked with correct CreateBudgetGoalDto parameters

5. **Edit mode: Delete button visible and triggers confirmation**
   - Arrange: Modal in Edit mode, existing budget goal loaded
   - Act: Click Delete Goal button
   - Assert: ConfirmDialog becomes visible with correct delete message

6. **Edit mode: Confirm delete triggers OnDelete callback**
   - Arrange: Modal in Edit mode, delete confirmation shown
   - Act: Click Confirm in ConfirmDialog
   - Assert: OnDelete callback invoked with correct CategoryId/Year/Month

7. **IsSubmitting state: Disables Save/Delete/Cancel buttons**
   - Arrange: Modal visible, IsSubmitting = true
   - Assert: Save, Delete, Cancel buttons all have IsDisabled=true

8. **Close callback: Invoked when Cancel clicked**
   - Arrange: Modal visible
   - Act: Click Cancel button
   - Assert: OnClose callback invoked

---

### **1.2 BudgetViewModel (6 tests)**
*Component:* `ViewModels/BudgetViewModel.cs`  
*Existing Coverage:* Basic scaffolding tests only  
*Missing Tests:*

9. **LoadDataAsync: Handles API error gracefully**
   - Arrange: BudgetApiService throws HttpRequestException
   - Act: Call LoadDataAsync
   - Assert: ErrorMessage populated, IsLoading = false, Summary remains null

10. **NextMonthAsync: Increments month and reloads data**
    - Arrange: CurrentDate = Jan 2026, mock API returns Feb summary
    - Act: Call NextMonthAsync
    - Assert: CurrentDate = Feb 2026, Summary updated to Feb data

11. **PreviousMonthAsync: Decrements month and reloads data**
    - Arrange: CurrentDate = Jan 2026, mock API returns Dec 2025 summary
    - Act: Call PreviousMonthAsync
    - Assert: CurrentDate = Dec 2025, Summary updated to Dec data

12. **OverallStatus: Returns Success when all categories on track**
    - Arrange: Summary with CategoriesOnTrack=5, CategoriesWarning=0, CategoriesOverBudget=0
    - Assert: OverallStatus == "success"

13. **OverallStatus: Returns Warning when any category in warning state**
    - Arrange: Summary with CategoriesOnTrack=3, CategoriesWarning=2, CategoriesOverBudget=0
    - Assert: OverallStatus == "warning"

14. **OverallStatus: Returns Danger when any category over budget**
    - Arrange: Summary with CategoriesOnTrack=2, CategoriesWarning=1, CategoriesOverBudget=1
    - Assert: OverallStatus == "danger"

---

### **1.3 Budget Page Integration (4 tests)**
*Component:* `Pages/Budget.razor`  
*Existing Coverage:* Basic render, empty message  
*Missing Tests:*

15. **Budget.razor: Displays summary stats when Summary loaded**
    - Arrange: Render Budget page with mock Summary data (TotalBudgeted=1000, TotalSpent=600, TotalRemaining=400)
    - Assert: Markup contains "Total Budgeted", "Total Spent", "Remaining" with correct MoneyDisplay components

16. **Budget.razor: Shows status counts with correct icons**
    - Arrange: Summary with CategoriesOnTrack=2, CategoriesWarning=1, CategoriesOverBudget=1, CategoriesNoBudgetSet=1
    - Assert: Markup contains "✓ 2 on track", "⚠️ 1 warning", "🔴 1 over budget", "⬚ 1 no budget"

17. **Budget.razor: Clicking Previous Month calls ViewModel.PreviousMonthAsync**
    - Arrange: Render Budget page, capture ViewModel.PreviousMonthAsync call
    - Act: Click button with chevron-left icon
    - Assert: ViewModel.PreviousMonthAsync invoked once

18. **Budget.razor: Clicking Next Month calls ViewModel.NextMonthAsync**
    - Arrange: Render Budget page, capture ViewModel.NextMonthAsync call
    - Act: Click button with chevron-right icon
    - Assert: ViewModel.NextMonthAsync invoked once

---

### **1.4 CategoryBudgetCard Component (4 tests)**
*Component:* `Components/Display/CategoryBudgetCard.razor`  
*Existing Coverage:* Partial  
*Missing Tests:*

19. **CategoryBudgetCard: Displays category name and budget/spent/remaining amounts**
    - Arrange: Render with Progress={CategoryName="Groceries", Budgeted=500, Spent=350, Remaining=150}
    - Assert: Markup contains "Groceries", "$500.00", "$350.00", "$150.00" (formatted via MoneyDisplay)

20. **CategoryBudgetCard: Applies warning class when PercentUsed > 80%**
    - Arrange: Render with Progress={PercentUsed=85}
    - Assert: BudgetProgressBar has Status="warning"

21. **CategoryBudgetCard: Applies danger class when over budget (PercentUsed > 100%)**
    - Arrange: Render with Progress={PercentUsed=110, Remaining=-50}
    - Assert: BudgetProgressBar has Status="danger", Remaining displayed in red text

22. **CategoryBudgetCard: Shows no-budget state when Budgeted = 0**
    - Arrange: Render with Progress={Budgeted=0, Spent=100}
    - Assert: Markup indicates "No budget set" or similar empty state

---

## Flow 2: Transaction Import (Target: 20-24 tests)

### **2.1 Import Page Wizard Flow (8 tests)**
*Component:* `Pages/Import.razor`  
*Existing Coverage:* Tab navigation, basic render  
*Missing Tests:*

23. **Import.razor: Step 1 — FileUploadZone visible on initial load**
    - Arrange: Render Import page, wizardState.CurrentStep = 1
    - Assert: FileUploadZone component rendered

24. **Import.razor: Step 1 → Step 2 — Successful CSV parse advances wizard**
    - Arrange: wizardState.CurrentStep = 1, CSV parsed successfully
    - Act: Trigger HandleFileSelected with valid parse result
    - Assert: wizardState.CurrentStep = 2, ColumnMappingEditor visible

25. **Import.razor: Step 2 → Step 3 — Valid column mapping advances to preview**
    - Arrange: wizardState.CurrentStep = 2, columns mapped
    - Act: Trigger ProceedToPreview
    - Assert: wizardState.CurrentStep = 3, ImportPreviewTable visible

26. **Import.razor: Step 3 → Step 4 — Confirm preview advances to import**
    - Arrange: wizardState.CurrentStep = 3, preview confirmed
    - Act: Trigger ProceedToImport
    - Assert: wizardState.CurrentStep = 4, ImportSummaryCard visible

27. **Import.razor: Step 4 — Successful import shows success message**
    - Arrange: wizardState.CurrentStep = 4, import API returns success
    - Act: Trigger PerformImport
    - Assert: Success message displayed, imported transaction count shown

28. **Import.razor: Error state — API failure shows ErrorAlert with retry**
    - Arrange: Import API throws exception
    - Act: Trigger PerformImport
    - Assert: ErrorAlert visible with error message and Retry button

29. **Import.razor: Start Over button resets wizard to Step 1**
    - Arrange: wizardState.CurrentStep = 3
    - Act: Click "Start Over" button
    - Assert: wizardState.CurrentStep = 1, all wizard state cleared

30. **Import.razor: Tab switch — History tab loads import history list**
    - Arrange: Render Import page, activeTab = "wizard"
    - Act: Click "Import History" tab
    - Assert: activeTab = "history", ImportHistoryList component rendered

---

### **2.2 ColumnMappingEditor Component (6 tests)**
*Component:* `Components/Import/ColumnMappingEditor.razor`  
*Existing Coverage:* Partial  
*Missing Tests:*

31. **ColumnMappingEditor: All required fields mapped enables Continue button**
    - Arrange: Date, Description, Amount mapped to valid columns
    - Assert: Continue button IsDisabled = false

32. **ColumnMappingEditor: Missing required field (Date) disables Continue**
    - Arrange: Date not mapped, Description and Amount mapped
    - Assert: Continue button IsDisabled = true, validation message shown

33. **ColumnMappingEditor: Missing required field (Description) disables Continue**
    - Arrange: Date and Amount mapped, Description not mapped
    - Assert: Continue button IsDisabled = true

34. **ColumnMappingEditor: Missing required field (Amount) disables Continue**
    - Arrange: Date and Description mapped, Amount not mapped
    - Assert: Continue button IsDisabled = true

35. **ColumnMappingEditor: Optional fields (Category, Account) can remain unmapped**
    - Arrange: Only Date, Description, Amount mapped
    - Assert: Continue button IsDisabled = false (optional fields don't block)

36. **ColumnMappingEditor: Saved mapping loaded populates all dropdowns**
    - Arrange: Load saved mapping with Date→col1, Description→col2, Amount→col3
    - Assert: All three dropdowns show correct selected values

---

### **2.3 ImportPreviewTable Component (5 tests)**
*Component:* `Components/Import/ImportPreviewTable.razor`  
*Existing Coverage:* Partial  
*Missing Tests:*

37. **ImportPreviewTable: Displays parsed row data with mapped columns**
    - Arrange: Preview with 5 rows, Date/Description/Amount columns
    - Assert: Table shows 5 rows, correct headers, formatted amounts

38. **ImportPreviewTable: Duplicate warning shown when duplicates detected**
    - Arrange: Preview contains 2 duplicate transactions (same date/description/amount)
    - Assert: DuplicateWarningCard visible, count = 2

39. **ImportPreviewTable: Toggle show duplicates only filters table rows**
    - Arrange: 10 rows, 3 duplicates
    - Act: Toggle "Show duplicates only"
    - Assert: Table displays only 3 duplicate rows

40. **ImportPreviewTable: Category column shows "Uncategorized" when mapping missing**
    - Arrange: Category column not mapped in wizard
    - Assert: All preview rows show "Uncategorized" in Category column

41. **ImportPreviewTable: Pagination shows correct page indicators**
    - Arrange: 150 rows, pageSize = 50
    - Assert: "Page 1 of 3" indicator visible, Next button enabled

---

### **2.4 CsvParserService Tests (5 tests)**
*Component:* `Services/CsvParserService.cs`  
*Existing Coverage:* Basic parse tests  
*Missing Tests:*

42. **CsvParserService: ParseCsvAsync handles BOM (byte order mark) correctly**
    - Arrange: CSV with UTF-8 BOM header
    - Act: ParseCsvAsync
    - Assert: BOM stripped, first header correctly parsed

43. **CsvParserService: ParseCsvAsync with skipRows=1 excludes first data row**
    - Arrange: CSV with 5 rows, skipRows = 1
    - Act: ParseCsvAsync(skipRows: 1)
    - Assert: RowCount = 4 (header + 3 data rows, 1 skipped)

44. **CsvParserService: Empty CSV returns empty result, no exception**
    - Arrange: CSV file with only headers, no data rows
    - Act: ParseCsvAsync
    - Assert: RowCount = 0, Headers populated, no exception

45. **CsvParserService: Malformed CSV row (unquoted comma in field) throws parse error**
    - Arrange: CSV row with unescaped comma breaking field structure
    - Act: ParseCsvAsync
    - Assert: Exception thrown OR error result with parse failure message

46. **CsvParserService: Very large CSV (10,000 rows) completes without timeout**
    - Arrange: Generate CSV with 10,000 rows
    - Act: ParseCsvAsync
    - Assert: RowCount = 10,000, completes in <5 seconds (performance check)
    - **Note:** This is a borderline performance test — may exclude from default runs

---

## Flow 3: AI Category Suggestions UI (Target: 16-20 tests)

### **3.1 UnifiedSuggestionCard Component (7 tests)**
*Component:* `Components/AI/UnifiedSuggestionCard.razor`  
*Existing Coverage:* **ZERO** — untested  
*Missing Tests:*

47. **UnifiedSuggestionCard: Displays rule suggestion title and description**
    - Arrange: Render with RuleSuggestion={Title="Auto-categorize Coffee", Description="Pattern: *STARBUCKS*"}
    - Assert: Markup contains title and description text

48. **UnifiedSuggestionCard: Confidence indicator shows High for confidence > 0.8**
    - Arrange: RuleSuggestion with Confidence = 0.85
    - Assert: Confidence badge displays "High", green indicator dot

49. **UnifiedSuggestionCard: Confidence indicator shows Medium for 0.5 < confidence ≤ 0.8**
    - Arrange: RuleSuggestion with Confidence = 0.65
    - Assert: Confidence badge displays "Medium", yellow indicator

50. **UnifiedSuggestionCard: Confidence indicator shows Low for confidence ≤ 0.5**
    - Arrange: RuleSuggestion with Confidence = 0.4
    - Assert: Confidence badge displays "Low", orange/red indicator

51. **UnifiedSuggestionCard: Accept button triggers OnAccept callback with correct ID**
    - Arrange: Render with RuleSuggestion, Id = Guid.NewGuid()
    - Act: Click Accept button
    - Assert: OnAccept callback invoked with correct suggestion ID

52. **UnifiedSuggestionCard: Dismiss button triggers OnDismiss callback**
    - Arrange: Render with RuleSuggestion
    - Act: Click Dismiss button
    - Assert: OnDismiss callback invoked

53. **UnifiedSuggestionCard: Details toggle expands/collapses detail section**
    - Arrange: Render with RuleSuggestion, _showDetails = false
    - Act: Click Details toggle button
    - Assert: _showDetails = true, detail section visible with pattern/reasoning
    - Act: Click Details toggle again
    - Assert: _showDetails = false, detail section hidden

---

### **3.2 SuggestionGroup Component (5 tests)**
*Component:* `Components/AI/SuggestionGroup.razor`  
*Existing Coverage:* **ZERO** — untested  
*Missing Tests:*

54. **SuggestionGroup: Renders group title with item count**
    - Arrange: Group={Title="Rule Suggestions", Items.Count=5}
    - Assert: Markup contains "Rule Suggestions" and count badge "5"

55. **SuggestionGroup: Accept All High-Confidence button visible when HighConfidenceCount > 0**
    - Arrange: Group with HighConfidenceCount = 3
    - Assert: "Accept All High-Confidence (3)" button visible

56. **SuggestionGroup: Accept All button hidden when HighConfidenceCount = 0**
    - Arrange: Group with HighConfidenceCount = 0
    - Assert: "Accept All High-Confidence" button not rendered

57. **SuggestionGroup: Clicking Accept All invokes batch accept callback**
    - Arrange: Group with 3 high-confidence items
    - Act: Click "Accept All High-Confidence (3)"
    - Assert: OnAcceptAllHighConfidence callback invoked once

58. **SuggestionGroup: Renders UnifiedSuggestionCard for each item**
    - Arrange: Group with 4 items
    - Assert: 4 UnifiedSuggestionCard components rendered

---

### **3.3 AnalysisInlineProgress Component (4 tests)**
*Component:* `Components/AI/AnalysisInlineProgress.razor`  
*Existing Coverage:* **ZERO** — untested  
*Missing Tests:*

59. **AnalysisInlineProgress: Shows analyzing state with elapsed time**
    - Arrange: IsAnalyzing=true, ElapsedTime=TimeSpan.FromSeconds(12)
    - Assert: LoadingSpinner visible, "Analyzing..." text, elapsed time "0:12"

60. **AnalysisInlineProgress: Shows error state with retry button**
    - Arrange: IsAnalyzing=false, ErrorMessage="Connection timeout"
    - Assert: Error icon visible, error message displayed, Retry button present

61. **AnalysisInlineProgress: Shows complete state with result summary**
    - Arrange: IsAnalyzing=false, RuleResult={NewRuleSuggestions=2, OptimizationSuggestions=1, ConflictSuggestions=0}
    - Assert: Check-circle icon, "Analysis complete!", result detail text "2 new rules, 1 optimizations, 0 conflicts"

62. **AnalysisInlineProgress: Retry button invokes OnRetry callback**
    - Arrange: Error state with ErrorMessage set
    - Act: Click Retry button
    - Assert: OnRetry callback invoked

---

### **3.4 AiSuggestionsViewModel (4 tests)**
*Component:* `ViewModels/AiSuggestionsViewModel.cs`  
*Existing Coverage:* Partial  
*Missing Tests:*

63. **AiSuggestionsViewModel: StartAnalysisAsync sets IsAnalyzing during API call**
    - Arrange: Mock AiApiService with 2-second delay
    - Act: Call StartAnalysisAsync, check state mid-call
    - Assert: IsAnalyzing = true (before API completes)

64. **AiSuggestionsViewModel: StartAnalysisAsync populates RuleAnalysisResult on success**
    - Arrange: Mock API returns AnalysisResponseDto with NewRuleSuggestions=3
    - Act: Call StartAnalysisAsync, await completion
    - Assert: RuleAnalysisResult.NewRuleSuggestions == 3, IsAnalyzing = false

65. **AiSuggestionsViewModel: StartAnalysisAsync sets AnalysisError on failure**
    - Arrange: Mock API throws HttpRequestException
    - Act: Call StartAnalysisAsync
    - Assert: AnalysisError populated with error message, IsAnalyzing = false

66. **AiSuggestionsViewModel: DismissAllAsync clears all suggestions and reloads**
    - Arrange: 5 pending suggestions loaded
    - Act: Call DismissAllAsync
    - Assert: All 5 suggestions dismissed via API, TotalSuggestionCount = 0 after reload

---

## Additional High-Value Test Targets (8-10 tests)

### **4.1 Calendar Page (Zero Coverage — Medium Priority)**

67. **Calendar.razor: Renders month grid with correct day count**
    - Arrange: Render Calendar page for Feb 2026 (28 days)
    - Assert: CalendarGrid component shows 28 day cells

68. **Calendar.razor: Budget panel shows categories with spending for current month**
    - Arrange: Budget summary loaded with 3 categories
    - Assert: CalendarBudgetPanel rendered with 3 CategoryBudgetCard components

---

### **4.2 FormStateService (Partially Covered)**

69. **FormStateService: SaveFormState persists state to localStorage**
    - Arrange: Mock IJSRuntime, form state object
    - Act: Call SaveFormState("budget-form", state)
    - Assert: JSRuntime.InvokeVoidAsync called with correct localStorage key/value

70. **FormStateService: GetFormState retrieves state from localStorage**
    - Arrange: Mock IJSRuntime returns serialized form state
    - Act: Call GetFormState<T>("budget-form")
    - Assert: Deserialized object matches expected state

71. **FormStateService: ClearFormState removes state from localStorage**
    - Arrange: Form state exists in localStorage
    - Act: Call ClearFormState("budget-form")
    - Assert: JSRuntime.InvokeVoidAsync called with removeItem

---

### **4.3 TransactionTable Component (Partially Covered)**

72. **TransactionTable: Bulk selection checkbox selects all visible rows**
    - Arrange: Table with 10 transactions
    - Act: Click header "Select all" checkbox
    - Assert: All 10 rows have IsSelected = true

73. **TransactionTable: Bulk categorize button enabled when rows selected**
    - Arrange: 3 rows selected
    - Assert: "Categorize" button in BulkActionBar enabled

74. **TransactionTable: Sort by date (ascending) reorders rows**
    - Arrange: Table with unsorted transactions
    - Act: Click "Date" column header
    - Assert: Rows sorted by date ascending, UI shows ascending indicator

75. **TransactionTable: Sort by date (descending) on second click**
    - Arrange: Table sorted ascending
    - Act: Click "Date" column header again
    - Assert: Rows sorted descending, UI shows descending indicator

---

## Test Execution Strategy

### **Priority Tiers**
1. **Tier 1 (Critical — 30 tests):** Budget creation flow (tests 1-22), Import wizard core flow (23-30), AI suggestions core components (47-58)
2. **Tier 2 (High — 25 tests):** Import column mapping/preview (31-46), AI ViewModel/progress (59-66)
3. **Tier 3 (Medium — 15 tests):** Additional components (67-75)

### **Estimated Coverage Gain**
- **Tier 1:** ~4% coverage increase (30 tests × ~0.13% per test) = 73.8% → 77.8%
- **Tier 2:** ~3% coverage increase (25 tests × ~0.12% per test) = 77.8% → 80.8%
- **Tier 3:** ~2% coverage increase (15 tests × ~0.13% per test) = 80.8% → 82.8%

**Total:** 70 tests → ~9% coverage gain → **82-83% Client coverage** (exceeds 75% target)

### **Implementation Order**
1. **Week 3, Day 1-2:** BudgetGoalModal + BudgetViewModel (tests 1-14)
2. **Week 3, Day 3:** Budget page integration + CategoryBudgetCard (tests 15-22)
3. **Week 3, Day 4-5:** Import wizard flow + ColumnMappingEditor (tests 23-36)
4. **Week 4, Day 1:** ImportPreviewTable + CsvParserService (tests 37-46)
5. **Week 4, Day 2-3:** AI components (UnifiedSuggestionCard, SuggestionGroup, AnalysisInlineProgress) (tests 47-62)
6. **Week 4, Day 4:** AI ViewModel (tests 63-66)
7. **Week 4, Day 5:** Additional high-value tests (tests 67-75) — only if time allows

---

## Test Quality Guidelines (Barbara's Standards)

### **What Makes a Test High-ROI?**
✅ **GOOD:**
- Tests that would FAIL if the user-visible behavior broke
- Validation edge cases (empty input, negative values, malformed data)
- Error handling paths (API failures, network timeouts)
- State transitions (wizard step progression, modal open/close)
- User interactions (button clicks, form submissions, bulk actions)

❌ **BAD (Vanity Tests — DO NOT WRITE):**
- `Assert.NotNull(component)` — useless, render failure would throw anyway
- Checking that a component renders without errors when there's no conditional logic
- Testing that a property setter sets a property (obvious behavior)
- Asserting hardcoded strings that never change

### **Assertion Intent Rule**
- Each test should verify **one logical behavior** (can include multiple related assertions)
- Example: "Budget goal modal validates empty amount" — assert ValidationMessage shown AND Save not invoked (same intent: validation blocks save)

### **Culture-Sensitivity**
- ALL tests that assert formatted currency/numbers MUST set `CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US")` in constructor
- Use `FormatCurrency()` extension method with explicit culture in production code when output is user-visible

### **bUnit Test Patterns**
- Use `Render<T>()` for component tests with parameter builder
- Use `Find()` / `FindAll()` for element selection
- Use `Shouldly` assertions (no FluentAssertions)
- Mock services with hand-written fakes (StubBudgetApiService pattern)

---

## Methodology Notes

### **Component Testing Approaches**

**1. Isolated Component Tests (Unit):**
- Best for: Common components (Button, Badge, Icon), small display components (MoneyDisplay, CategoryCard)
- Approach: Render component with parameters, assert markup/state
- Example: BudgetGoalModal validation tests

**2. Component-with-ViewModel Tests (Integration-lite):**
- Best for: Pages with ViewModels (Budget.razor + BudgetViewModel)
- Approach: Inject real ViewModel with stubbed API services, test user interactions
- Example: Budget page month navigation tests

**3. Multi-Component Workflow Tests:**
- Best for: Wizard flows (Import page step progression)
- Approach: Render parent page, simulate user actions, assert child component visibility
- Example: Import wizard step 1 → step 2 transition

**4. Service Logic Tests (No bUnit):**
- Best for: Pure C# services (CsvParserService, FormStateService)
- Approach: Standard xUnit tests, mock IJSRuntime where needed
- Example: CsvParserService edge case tests

---

## Exclusions (Intentional Low-Priority)

**Components NOT included in Phase 3 (defer to Phase 4 or accept low coverage):**
- **Reconciliation.razor** — complex workflow, low usage frequency, zero coverage acceptable until user demand increases
- **ComponentShowcase.razor** — demo page, not production code, exclude from coverage
- **Chart components** (already partially covered, diminishing returns)
- **Report pages** (Reports/MonthlyTrendsReport, etc.) — low user impact, defer
- **Calendar components** (CalendarDay, CalendarWeekView) — complex rendering logic, defer to Phase 4

**Rationale:** Phase 3 focuses on **core user workflows** (budget goals, import, AI suggestions). Reporting and calendar features are lower priority for coverage until Phase 4.

---

## Success Metrics

**Phase 3 Complete When:**
1. ✅ Client module coverage ≥ 75% (CI gate passes)
2. ✅ All 70 high-ROI tests written and passing
3. ✅ Zero vanity tests (Barbara quality review passes)
4. ✅ Budget creation, import wizard, AI suggestions flows have comprehensive coverage
5. ✅ All tests use culture-aware formatting (no CI failures on Linux)

**Coverage Quality Check (Barbara's Audit):**
- Run mutation testing sample on 10 random new tests → must kill ≥70% of mutants
- Manual review: Each test must assert meaningful behavior (not just "renders without error")
- CI stability: All 70 new tests must pass 10 consecutive CI runs (no flakiness)

---

## Appendix: Component Coverage Map

| Component/Page | Current Coverage | Target Tests | Priority |
|----------------|------------------|--------------|----------|
| BudgetGoalModal | Partial (~40%) | 8 | High |
| BudgetViewModel | Low (~20%) | 6 | High |
| Budget.razor | Low (~30%) | 4 | High |
| CategoryBudgetCard | Partial (~50%) | 4 | Medium |
| Import.razor | Low (~25%) | 8 | High |
| ColumnMappingEditor | Partial (~40%) | 6 | High |
| ImportPreviewTable | Partial (~50%) | 5 | Medium |
| CsvParserService | Good (~70%) | 5 | Medium |
| UnifiedSuggestionCard | **Zero** | 7 | **Critical** |
| SuggestionGroup | **Zero** | 5 | **Critical** |
| AnalysisInlineProgress | **Zero** | 4 | High |
| AiSuggestionsViewModel | Partial (~30%) | 4 | High |
| Calendar.razor | **Zero** | 2 | Low (defer) |
| FormStateService | Good (~75%) | 3 | Medium |
| TransactionTable | Good (~65%) | 4 | Medium |

**Total:** 15 components/pages, 70 test scenarios

---

**Document Status:** Draft — Ready for review  
**Author:** Barbara (Tester)  
**Date:** 2026-01-09  
**Next Steps:** Review with Alfred, prioritize Tier 1 tests, begin implementation Week 3
