# Changelog

All notable changes to Budget Experiment.

## [3.23.0] - 2026-03-11

### Features

- **client:** Unified `/transactions` page with paginated, filtered, sorted view of all transactions — inline category assignment, bulk categorize/delete, create-rule-from-transaction, AI suggestion chips, deep-link URL sync, running balance for single-account views (Feature 107)
- **api:** `GET /api/v1/transactions/paged` endpoint with full filter/sort/pagination support; `PATCH /api/v1/transactions/{id}/category` for quick category assignment; `POST /api/v1/transactions/suggest-categories` for batch AI suggestions
- **client:** AI Suggestions UX redesign — unified suggestions page with grouped suggestions, review mode, AiSetupBanner, AnalysisInlineProgress, SuggestionGroup, UnifiedSuggestionCard, AiSuggestionsViewModel extraction (Feature 105)
- **ai:** AI suggestion quality improvements — TransactionDescriptionCleaner, DescriptionAggregator, enriched prompts with frequency/amounts and few-shot examples, hardened response parsing with `ParseResult<T>` diagnostics, feedback-informed suggestions, SuggestionMetricsService with `GET /api/v1/suggestions/metrics` endpoint (Feature 106)

### Testing

- **api:** 23 integration tests for unified transaction endpoints — GET /paged (12), POST /suggest-categories (5), PATCH /{id}/category (6)
- **application:** 12 unit tests for UnifiedTransactionService — paging, filtering, summary, balance info, running balance
- **client:** 68 TransactionsViewModel unit tests — initialization, filters, sorting, pagination, selection, bulk operations, suggestions, create-rule, error handling
- **client:** 9 InlineCategoryPicker + 9 TransactionFilterBar component tests

### Documentation

- **docs:** Feature 105 — AI suggestions UX redesign (complete)
- **docs:** Feature 107 — Actionable transaction lists (complete)

## [3.22.0] - 2026-03-08

### Refactoring

- **client:** Extract `CategoriesViewModel` from Categories.razor `@code` block — 19 handler methods, 16 state properties, 3 computed properties; Razor file reduced to thin binding layer (Feature 097)
- **client:** Extract `RulesViewModel` from Rules.razor `@code` block — 16 handler methods, 13 state properties; comprehensive ViewModel unit tests (Feature 098)
- **client:** Extract `AccountsViewModel` from Accounts.razor `@code` block — 14 handler methods, 11 state properties; ViewModel unit tests (Feature 099)
- **client:** Extract `BudgetViewModel` from Budget.razor `@code` block — handler methods, state properties, computed properties; ViewModel unit tests (Feature 100)
- **client:** Extract `RecurringViewModel` from Recurring.razor `@code` block — handler methods and state into testable ViewModel class (Feature 101)
- **client:** Extract `RecurringTransfersViewModel` from RecurringTransfers.razor `@code` block (Feature 102)
- **client:** Extract `TransfersViewModel` from Transfers.razor `@code` block (Feature 103)
- **client:** Extract `OnboardingViewModel` from Onboarding.razor `@code` block — 6 handler methods, 7 state fields, 1 computed property; 33 ViewModel unit tests (Feature 104)

### Features

- **docs:** Establish localization infrastructure for multi-language support (Feature 096)

### Bug Fixes

- **import:** Handle sanitized negative amounts in CSV import — amounts with leading formula-trigger characters now parsed correctly (Feature 094)
- **test:** Set en-US culture in ChatMessageBubbleTests for CI compatibility
- **test:** Replace flaky `Task.Delay` with `WaitForAssertion` in MobileChatSheetTests

### Testing

- **client:** Client test coverage phase 2 — API services, P1/P2/P3 page components, chat and display components, service and model tests (Feature 095)
- **client:** Deepen page handler coverage to reach 65% target
- **ci:** Add code coverage collection, reporting, and quality gates (Feature 092)
- **ci:** Exclude SDK/library namespaces and boilerplate from code coverage metrics

### Dependencies

- **deps:** Update EntityFrameworkCore packages to 10.0.3
- **deps:** Update dotnet-ef tool to 10.0.3
- **deps:** Bump Microsoft.Build.Tasks.Core 18.0.2 → 18.3.3
- **deps(ci):** Bump actions/checkout 4 → 6, actions/setup-dotnet 4 → 5, actions/download-artifact 4 → 8, dorny/test-reporter 1 → 2, docker/build-push-action 5 → 6

### Documentation

- **docs:** Add culture-sensitive formatting guidance to copilot instructions (§37)
- **docs:** Feature docs 097–104 — ViewModel extraction pattern for all page components (all complete)

## [3.21.0] - 2026-03-07

### Dependencies

- **deps:** Update Microsoft ASP.NET Core & Extensions packages 10.0.0 → 10.0.3 — JwtBearer, OpenApi, WebAssembly, WebAssembly.Server, WebAssembly.Authentication, WebAssembly.DevServer, Extensions.Http, DependencyInjection.Abstractions, Mvc.Testing (Feature 093 Phase 1)
- **deps:** Update Scalar.AspNetCore 2.10.3 → 2.13.1, bunit 2.5.3 → 2.6.2, Microsoft.Playwright 1.49.0 → 1.58.0, Deque.AxeCore.Playwright 4.11.0 → 4.11.1, Microsoft.Build.Tasks.Core 18.0.2 → 18.3.3 (Feature 093 Phase 2)
- **deps:** Update Microsoft.NET.Test.Sdk 18.0.1 → 18.3.0, coverlet.collector 6.0.4 → 8.0.0 across all 6 test projects (Feature 093 Phase 3)
- **deps:** Update MinVer 6.0.0 → 7.0.0 centrally in Directory.Build.props (Feature 093 Phase 4)

### Documentation

- **docs:** Feature 093 — NuGet package updates (non-EF Core) (complete)

## [3.20.0] - 2026-03-07

### Testing

- **client:** bUnit tests for 5 common UI components — ConfirmDialog (7), ErrorAlert (8), LoadingSpinner (7), PageHeader (8), ThemeToggle (8) (Feature 091 Phase 3)
- **client:** bUnit tests for 13 import workflow components — AmountModeSelector, ColumnMappingEditor, CsvPreviewTable, DateFormatSelector, DuplicateWarningCard, FileUploadZone, ImportHistoryList, ImportPreviewTable, ImportSummaryCard, IndicatorSettingsEditor, SavedMappingSelector, SavedMappingsManager, SkipRowsInput — 120 tests (Feature 091 Phase 4)
- **client:** bUnit tests for 9 AI components — AiOnboardingPanel, AiSettingsForm, AiStatusBadge, AnalysisProgressDialog, AnalysisSummaryCard, CategorySuggestionCard, SuggestionCard, SuggestionDetailDialog, SuggestionList — 74 tests (Feature 091 Phase 4)
- **client:** bUnit tests for 6 reconciliation components — ConfidenceBadge (11), MatchReviewModal (12), ToleranceSettingsPanel (9), ImportPatternsDialog (9), LinkableInstancesDialog (8), ManualMatchDialog (9) — 58 tests (Feature 091 Phase 5)
- **client:** bUnit tests for 2 navigation components — NavMenu (15), ScopeSwitcher (11) — 26 tests (Feature 091 Phase 5)
- **client:** Total test count: 2,686 passed (1 skipped), up from 1,059

### Documentation

- **docs:** Feature 091 — Client component & service test coverage (complete)

## [3.19.0] - 2026-03-06

### Testing

- **infrastructure:** 36 integration tests for 5 previously untested repositories — BudgetCategoryRepository (9 tests: scope filtering, GetByName, GetActive, GetByType, GetByIds), BudgetGoalRepository (6 tests: scope filtering, GetByCategoryAndMonth, GetByMonth), CategorySuggestionRepository (6 tests: case-normalization, fetch-then-remove, AddRange), LearnedMerchantMappingRepository (5 tests: pattern normalization, GetByOwner ordering), RecurringTransactionRepository (10 tests: exception CRUD, date-range queries, bulk delete, scope filtering) (Feature 087)
- **application:** Unit tests for untested services, handlers, and projectors — CustomReportLayoutService, ImportPreviewEnricher, RecurringInstanceProjector, RecurringTransferInstanceProjector (Feature 088)
- **application:** Unit tests for mappers with non-trivial logic — ChatMapper (polymorphic action mapping), RecurringMapper (exception resolution logic); simple 1:1 property mappers excluded (Feature 088)
- **domain:** 24 unit tests for CustomReportLayout entity — CreateShared, CreatePersonal, validation (empty name, max length, empty userId), NormalizeLayoutJson, UpdateName, UpdateLayout, trimming, unique IDs (Feature 089)
- **api:** MerchantMappingsController integration tests — GET (list), POST (learn), DELETE (remove), 404 for non-existent mappings (Feature 090)
- **api:** VersionController integration tests — GET returns 200 with version info shape (Feature 090)

### Documentation

- **docs:** Consolidate features 081–090 into single archive document (API versioning, concurrency, hygiene & test coverage)

## [3.18.0] - 2026-03-06

### Refactoring

- **domain:** Rename `Account.CreatedAt`/`UpdatedAt` and `Transaction.CreatedAt`/`UpdatedAt` to `CreatedAtUtc`/`UpdatedAtUtc` per §30 naming convention — all entities now consistently use `Utc` suffix for UTC timestamps (Feature 085)
- **contracts:** Rename `AccountDto`, `TransactionDto`, `TransactionListItemDto`, `DayDetailItemDto` timestamp properties to `CreatedAtUtc`/`UpdatedAtUtc` (Feature 085)
- **application:** Update `AccountMapper`, `TransferService`, `TransactionListService`, `DayDetailService` for `Utc` suffix (Feature 085)
- **client:** Update `TransactionListItem` model and `AccountTransactions.razor` for `Utc` suffix (Feature 085)

### Database

- **infrastructure:** EF migration `Feature085_RenameCreatedAtUpdatedAtToUtcSuffix` — renames `CreatedAt`→`CreatedAtUtc` and `UpdatedAt`→`UpdatedAtUtc` columns on `Accounts` and `Transactions` tables (Feature 085)

### Documentation

- **docs:** Feature 085 — DateTime naming consistency (complete)

## [3.17.0] - 2026-03-06

### Features

- **api:** Add pagination metadata to transfer list endpoint — `TransferListPageResponse` paged DTO with `Items`, `TotalCount`, `Page`, `PageSize`, `TotalPages`; `X-Pagination-TotalCount` response header on `GET /api/v1/transfers` (Feature 084 Slice 3)
- **api:** Runtime API versioning with `Asp.Versioning.Mvc` — `[ApiVersion("1.0")]` on all 26 controllers, routes use `v{version:apiVersion}` template, `api-supported-versions` header in responses, `AssumeDefaultVersionWhenUnspecified` for backward compatibility (Feature 081)
- **api:** Normalize `SuggestionsController` route from `api/v1/ai/[controller]` to standard `api/v{version:apiVersion}/[controller]` pattern; add version segment to `VersionController` (Feature 081)
- **domain:** `ICurrencyProvider` abstraction for global currency resolution — single interface in Domain layer with `UserSettingsCurrencyProvider` implementation in Application layer, falls back to USD when preference is unset or user is unauthenticated (Feature 064)
- **domain:** Add `FirstDayOfWeek` and `IsOnboarded` to `UserSettings` — restricted to Sunday/Monday, domain validation via `DomainException` (Feature 066)
- **client:** Client-side CSV parsing — CSV files parsed entirely in Blazor WebAssembly with zero server round-trip, auto-delimiter detection, quote-aware parsing, and BOM removal (Feature 063)
- **client:** CSV injection sanitization — cells starting with formula-trigger characters (`=`, `@`, `+`, `-`, `\t`, `\r`) prefixed with `'` for safe display (Feature 063)
- **client:** First-run onboarding wizard — step-based wizard (Welcome → Currency → First Day of Week → Confirm) with skip option and onboarding guard redirect (Feature 066)
- **client:** Settings page user preferences — currency dropdown and first-day-of-week toggle editable post-onboarding (Feature 066)
- **api:** Preview endpoint row count validation — rejects requests exceeding 10,000 rows (400 with ProblemDetails) (Feature 063)
- **api:** `POST api/v1/user/settings/complete-onboarding` convenience endpoint (Feature 066)
- **api:** Optimistic concurrency with ETags — `ExceptionHandlingMiddleware` maps `DbUpdateConcurrencyException` → 409 Conflict; `IUnitOfWork` gains `GetConcurrencyToken`/`SetExpectedConcurrencyToken`; `xmin` configured on `Account`, `Transaction`, `BudgetCategory`, `BudgetGoal`, `RecurringTransaction`, `RecurringTransfer`, `CategorizationRule`, `CustomReportLayout`, and `ImportMapping`; GET endpoints return `ETag` header; PUT/PATCH endpoints validate `If-Match` header (backward compatible — missing header accepted); `AccountDto`, `TransactionDto`, `BudgetCategoryDto`, `BudgetGoalDto`, `RecurringTransactionDto`, `RecurringTransferDto`, `CategorizationRuleDto`, `CustomReportLayoutDto`, and `ImportMappingDto` gain `Version` property (Feature 082 Slices 1–6)
- **client:** Optimistic concurrency client integration — `ApiResult<T>` result type for update operations distinguishing success/conflict/failure; `BudgetApiService` and `ImportApiService` send `If-Match` header with entity version on PUT/PATCH; all 10 mutable-entity pages handle 409 Conflict with toast notification and automatic data reload; `SendUpdateAsync<T>` private helper centralizes ETag wire protocol (Feature 082 Slice 7)
- **api:** Restore dismissed category suggestions — `GET /dismissed`, `POST /{id}/restore`, `DELETE /dismissed-patterns` endpoints (Feature 061)

### Bug Fixes

- **api:** Fix exception middleware pipeline ordering — move `ExceptionHandlingMiddleware` registration before `MapControllers()` so controller exceptions are properly caught; replace non-standard HTTP 499 with connection abort for cancelled requests; use `ProblemDetails` class with `Instance` field instead of anonymous object (Feature 084 Slice 1)
- **api:** Fix `CustomReportsController.CreateAsync` `CreatedAtAction` route resolution — use `\"GetById\"` string instead of `nameof(GetByIdAsync)` to match API versioning route conventions (Feature 082 Slice 6)
- **infrastructure:** Fix scope filtering in `GetByDateRangeAsync` and `GetByIdWithExceptionsAsync` — reports now respect Personal/Shared/All budget scope (Feature 065)
- **application:** Fix CSV import double-skip bug — `RowsToSkip` was applied twice (parser and preview service), causing silent data loss (Feature 069)
- **application:** Fix AI suggestions JSON extraction — `ExtractJson()` method handles markdown code blocks and preamble text in AI responses, restoring non-functional Smart Insights feature (Feature 070)
- **client:** Fix `TransactionForm.razor` currency default — `MoneyDto.Currency` empty string not caught by null check; now uses `string.IsNullOrWhiteSpace()` (Feature 068)
- **client:** Align Reports donut chart `StrokeWidth` from 50 to 20 for consistent ring proportions with Calendar (Feature 067)
- **client:** Pass actual currency to Reports donut chart instead of defaulting to `"USD"` (Feature 067)
- **client:** Standardize donut chart fallback color to `#6b7280` and filter/sort segment data consistently (Feature 067)

### Security

- **api:** Remove server-side CSV parse endpoint (`POST /api/v1/import/parse`) — eliminates file upload attack surface entirely (**BREAKING**) (Feature 063)
- **api:** Request size limits on import endpoints — 5 MB execute, 10 MB preview (413 on oversized requests) (Feature 063)
- **api:** Import execute validation — max 5,000 transactions, field length limits, date/amount range checks (400/422 with ProblemDetails) (Feature 063)
- **application:** Delete server-side `ICsvParserService` and `CsvParserService` — no file bytes reach the server (Feature 063)

### Refactoring

- **api:** Add `Async` suffix to `MerchantMappingsController` methods — `GetLearned` → `GetLearnedAsync`, `Learn` → `LearnAsync`, `Delete` → `DeleteAsync`; routes unchanged (ASP.NET strips suffix) (Feature 084 Slice 2)
- **docs:** Document `Contracts` and `Shared` projects in `copilot-instructions.md` §2, §3, §21; add `AddDomain()` DI extension for consistency with `AddApplication()`/`AddInfrastructure()` (Feature 084 Slice 4)
- **all:** Decouple Client from Domain — create `BudgetExperiment.Shared` project, move 10 domain enums (`BudgetScope`, `CategorySource`, `DescriptionMatchMode`, `ChatActionStatus`, `ChatActionType`, `ChatRole`, `AmountParseMode`, `ImportBatchStatus`, `ImportField`, `ImportRowStatus`) to Shared; remove Client → Domain project reference; remove 13 domain namespace imports from Client; Client now depends only on Contracts → Shared (Feature 079)
- **all:** Re-enable StyleCop analyzers — fix ~1,500+ violations across all 11 projects: company headers (`BecauseImClever`), member ordering (SA1201/SA1202/SA1203/SA1204), parameter formatting (SA1117/SA1118), XML doc completeness (SA1611/SA1615/SA1623/SA1629), using placement (SA1200/SA1210), file hygiene (SA1402/SA1507/SA1515/SA1518), arithmetic parentheses (SA1407); extract types to satisfy one-type-per-file rule; `TreatWarningsAsErrors=true` enforced (Feature 078)
- **domain:** Organize domain interfaces — move `IAutoRealizeService`, `ITransactionMatcher`, `IRecurringInstanceProjector`, `IRecurringTransferInstanceProjector` from `Repositories/` to `Services/`; move `IUserContext` from `Repositories/` to `Identity/`; `Repositories/` now contains only data access abstractions (Feature 077)
- **domain:** Rename 17 value objects with `Value` suffix per §5 naming convention — `GeoCoordinateValue`, `TransactionLocationValue`, `MatchingTolerancesValue`, `DailyTotalValue`, `ColumnMappingValue`, `SkipRowsSettingsValue`, `DebitCreditIndicatorSettingsValue`, `DuplicateDetectionSettingsValue`, `BillInfoValue`, `RecurrencePatternValue`, `RecurringInstanceInfoValue`, `RecurringTransferInstanceInfoValue`, `ImportPatternValue`, `PaycheckAllocationValue`, `PaycheckAllocationSummaryValue`, `PaycheckAllocationWarningValue`, `TransactionMatchResultValue` (Feature 076)
- **application:** Decompose `RuleSuggestionService` (857 → 260 lines) — extract `RuleSuggestionResponseParser` (~350 lines, AI JSON → domain objects), `RuleSuggestionPromptBuilder` (~115 lines, static prompt building), `SuggestionAcceptanceHandler` (~150 lines, accept/dismiss/feedback lifecycle) with `IRuleSuggestionResponseParser` and `ISuggestionAcceptanceHandler` interfaces (Feature 080 Phase 2)
- **application:** Decompose `NaturalLanguageParser` (556 → 130 lines) — extract `ChatActionParser` (~450 lines, static response → ChatAction parsing); orchestrator now delegates all response parsing (Feature 080 Phase 2)
- **application:** Decompose `ReconciliationService` (545 → 335 lines) — extract `ReconciliationStatusBuilder` (171 lines, period status reports), `ReconciliationMatchActionHandler` (208 lines, accept/reject/unlink/bulk/manual-link lifecycle) with `IReconciliationStatusBuilder` and `IReconciliationMatchActionHandler` interfaces (Feature 080 Phase 3)
- **application:** Decompose `ReportService` (423 → 252 lines) — extract `TrendReportBuilder` (165 lines, monthly spending trends), `LocationReportBuilder` (118 lines, geographic spending grouping) with `ITrendReportBuilder` and `ILocationReportBuilder` interfaces (Feature 080 Phase 3)
- **application:** Replace 51 hardcoded `"USD"` strings across 11 Application services with `ICurrencyProvider` — all monetary values now derive currency from user's `PreferredCurrency` setting (Feature 064)
- **application:** Decompose `ImportService` (1,076 → 247 lines) — extract `ImportRowProcessor` (512 lines, CSV row parsing), `ImportDuplicateDetector` (112 lines), `ImportPreviewEnricher` (213 lines), `ImportBatchManager` (~140 lines, batch history/deletion), `ImportTransactionCreator` (~130 lines) with corresponding interfaces (Feature 080 Phases 1 & 5)
- **application:** Decompose `ChatService` (397 → 286 lines) — extract `ChatActionExecutor` (~110 lines, action dispatch to domain services) with `IChatActionExecutor` interface; break down `SendMessageAsync` (56 → 25) and `ConfirmActionAsync` (66 → 14) (Feature 080 Phases 4 & 5)
- **application:** Decompose `CategorySuggestionService` (366 → 309 lines) — extract `CategorySuggestionDismissalHandler` (~120 lines, dismiss/restore/clear lifecycle); break down `AnalyzeTransactionsAsync` (73 → 28) (Feature 080 Phases 4 & 5)
- **application:** Extract `RecurrencePatternFactory` (~50 lines) — shared static factory for `RecurringTransactionService` (355 → 316) and `RecurringTransferService` (348 → 309) (Feature 080 Phase 5)
- **application:** Extract `LinkableInstanceFinder` (~120 lines) from `ReconciliationService` (335 → 294 lines) — linkable instance projection and confidence (Feature 080 Phase 5)
- **domain:** Decompose `TransactionMatcher` (372 → 256 lines) — extract `DescriptionSimilarityCalculator` (121 lines, description normalization, containment matching, Levenshtein distance); break down `CalculateMatch` (83 → 27), extract `PassesHardFilters` + `CalculateOverallConfidence` (Feature 080 Phases 4 & 6)
- **domain:** Break down `RecurringTransfer.Create` (67 → 30 lines) — extract `ValidateAccountIds`, `ValidateCommonFields`, `ValidateEndDate` (Feature 080 Phase 4)
- **application:** Document `MerchantKnowledgeBase` (369 lines) as exempt from ~300-line guideline — static data declarations, not logic (Feature 080 Phase 6)
- **contracts:** Change `MoneyDto.Currency` default from `"USD"` to `string.Empty` — all callers now set currency explicitly via `ICurrencyProvider` (Feature 064)
- **contracts:** Centralize family user identity values into `FamilyUserDefaults` — `FamilyUserContext` (Api) and `NoAuthAuthenticationStateProvider` (Client) now reference shared constants, eliminating cross-project duplication (Feature 083 Slice 7)

### Testing

- **api:** 7 unit tests for `ExceptionHandlingMiddleware` — ProblemDetails shape, traceId, Instance field, cancellation abort, DomainException 400/404 mapping (Feature 084 Slice 1)
- **application:** Unit test for `TransferService.ListAsync` paged response with correct `TotalCount`, `Page`, `PageSize`, `TotalPages` (Feature 084 Slice 3)
- **api:** Integration test for `TransfersController.ListAsync` — verifies `X-Pagination-TotalCount` header and `TransferListPageResponse` body shape (Feature 084 Slice 3)
- **e2e:** Functional Playwright test suite — calendar navigation, transaction CRUD, account management, CSV import flow (Feature 062)
- **e2e:** Local critical Playwright tests — 6 tests for transaction, account, and category creation flows with `LocalCritical` trait (Feature 068)
- **infrastructure:** 4 integration tests for scope filtering isolation in `TransactionRepository` (Feature 065)
- **application:** Regression test for CSV import double-skip bug (Feature 069)
- **application:** 6 unit tests + 2 integration tests for AI JSON extraction (Feature 070)
- **application:** 36 unit tests for `RecurringTransactionService` — covers all 12 public methods (CRUD, pause/resume, skip, import patterns), all frequency types, error paths; prerequisite for Feature 080 god service decomposition (Feature 080)
- **application:** 42 unit tests for Phase 2 extracted components — 13 `RuleSuggestionResponseParserTests`, 9 `SuggestionAcceptanceHandlerTests`, 20 `ChatActionParserTests`; all 2,748 tests passing (Feature 080 Phase 2)
- **application:** 35 unit tests for Phase 3 extracted components — 8 `ReconciliationStatusBuilderTests`, 11 `ReconciliationMatchActionHandlerTests`, 8 `TrendReportBuilderTests`, 8 `LocationReportBuilderTests`; all 2,783 tests passing (Feature 080 Phase 3)
- **application:** 23 unit tests for Phase 5 extracted components — `ChatActionExecutorTests`, `ImportBatchManagerTests`, `ImportTransactionCreatorTests`, `LinkableInstanceFinderTests`, `CategorySuggestionDismissalHandlerTests`; all 2,809 tests passing (Feature 080 Phase 5)
- **domain:** 16 unit tests for `DescriptionSimilarityCalculator` — similarity scoring, normalization, containment matching, punctuation handling, edge cases; all 2,826 tests passing (Feature 080 Phase 6)
- **client:** Unit tests for donut chart segment filtering, sorting, and fallback color (Feature 067)
- **api:** 10 API tests for optimistic concurrency — ETag returned on GET, valid/stale/missing `If-Match` on PUT/PATCH for Account (4 tests) and Transaction (6 tests) (Feature 082)
- **api:** 12 API tests for secondary aggregate concurrency — ETag/If-Match for CategorizationRule (4 tests), CustomReportLayout (4 tests), and ImportMapping (4 tests) (Feature 082 Slice 6)

### Documentation

- **docs:** Consolidate features 061–070 into archive
- **docs:** Consolidate features 071–080 into archive

## [3.16.1] - 2026-02-22

### Bug Fixes

- **application:** Include initial balance in running balance when transaction list starts on account's InitialBalanceDate
- **tests:** Make `TransactionTableSortTests` culture-agnostic — assert sort order by description instead of formatted currency strings to fix CI failures on Linux runners

### Features

- **client:** Sortable columns in transaction table — clickable headers for Date, Description, Amount, Balance with toggle ascending/descending and arrow indicators
- **client:** Client-side pagination for transaction table — default page size 50, selectable 25/50/100, page navigation bar with item count display
- **client:** Silent token refresh handling — `TokenRefreshHandler` DelegatingHandler intercepts 401 responses, attempts silent token refresh, retries original request on success
- **client:** Graceful session expiry — toast notification via existing `IToastService` when silent refresh fails, with return URL preserved for re-authentication
- **client:** Form data preservation on session expiry — `IFormStateService`/`FormStateService` with localStorage persistence, opt-in form registration, automatic save on token refresh failure

### Accessibility

- **client:** Keyboard navigation for sortable column headers (Enter/Space activation, tabindex, focus-visible outline)
- **client:** ARIA attributes on sort headers (`aria-sort`) and pagination controls (`aria-label`, `<nav>` landmark)

### Testing

- **application:** Unit tests for running balance initial-balance boundary condition (4 tests)
- **client:** bUnit tests for sortable column headers (14 tests)
- **client:** bUnit tests for client-side pagination (20 tests)
- **client:** Unit tests for `TokenRefreshHandler` (9 tests — 401 refresh, retry, concurrency, form state save, auth route skipping)
- **client:** Unit tests for `FormStateService` (11 tests — save/restore/clear, multiple forms, error handling, duplicate keys)
- **e2e:** Playwright E2E tests for session expiry scenarios (5 tests — toast on expiry, no duplicate toasts, form state preservation, re-authentication flow, valid session baseline)

### Documentation

- **docs:** Feature 071 — transaction list running balance bug, sorting & pagination
- **docs:** Feature 054 — silent token refresh handling (complete)

## [3.15.2] - 2026-02-15

### Bug Fixes

- **application:** Fix CSV import skip-rows double-skip bug — `RowsToSkip` was applied in both `CsvParserService` and `ImportService`, causing the first N data rows to be silently discarded

### Testing

- **application:** Add regression test for skip-rows double-skip bug (`PreviewAsync_WithRowsToSkip_DoesNotDoubleSkipAlreadyParsedRows`)
- **application:** Update two existing skip-rows tests to assert correct (non-double-skip) behavior

### Documentation

- **docs:** Feature 069 — CSV import skip-rows double-skip bug fix

## [3.15.1] - 2026-02-14

### Bug Fixes

- **client:** Fix modal stealing keyboard focus on every re-render, preventing typing in inputs

### Testing

- **client:** bUnit tests for Modal component (focus behavior, re-render regression, Escape/overlay close, sizing, accessibility)

## [3.15.0] - 2026-02-14

### Features

- **client:** LineChart component for trend visualization with multi-series support, axes, grid, and reference lines
- **client:** Shared chart primitives (ChartAxis, ChartGrid, ChartTooltip) for SVG charts
- **client:** GroupedBarChart and StackedBarChart components for multi-series comparisons and composition views
- **client:** AreaChart component with optional gradient fills
- **client:** SparkLine, ProgressBar, and RadialGauge components for compact trend and progress visualization
- **api:** CSV export endpoint for monthly category report
- **application:** Export service infrastructure with CSV formatter
- **client:** Export button download handling with loading state and error feedback
- **domain:** Custom report layout entity and repository contract
- **infrastructure:** EF Core configuration, DbSet, and repository for custom report layouts
- **application:** Custom report layout service with scope-aware CRUD operations
- **api:** Custom report layout CRUD endpoints (`/api/v1/custom-reports`)
- **client:** Custom report builder page with widget palette, canvas, and placeholder widgets
- **client:** Custom report layout CRUD methods in BudgetApiService
- **client:** Reports index card for custom report builder
- **client:** Custom report builder grid layout with drag/resize snapping and widget presets
- **client:** Widget configuration panel with title editing and per-widget options
- **client:** Report widget actions for selection, duplicate, and delete
- **client:** BarChart click events for drill-down
- **client:** Component showcase page for charts and exports

### Bug Fixes

- **client:** Fix Razor parser failure with escaped quotes in ReportCanvas interpolated strings
- **client:** Fix `@layout` directive conflict in CustomReportBuilder by renaming variable
- **client:** Fix AreaChart string parameter bindings missing `@` prefix
- **client:** Fix ExportDownloadService async disposal pattern
- **infra:** Add missing `Domain.Reports` global using in Infrastructure and Application
- **api:** Add missing XML doc param tag in ExportController
- **api:** Fix invalid `ChatActionType.Unknown` enum reference in tests

### Testing

- **client:** bUnit tests for LineChart (empty state, paths, points, multi-series, ARIA)
- **client:** bUnit tests for GroupedBarChart and StackedBarChart (empty state, rendering, legend, ARIA)
- **client:** bUnit tests for AreaChart and large dataset rendering
- **client:** bUnit tests for SparkLine, ProgressBar, and RadialGauge
- **api:** ExportController integration test for CSV export
- **application:** Export service unit tests for CSV formatter and routing
- **client:** bUnit tests for ExportButton download flow
- **client:** bUnit tests for BarChart click/colors, LineChart smooth interpolation, StackedBarChart segment heights, ProgressBar thresholds, and RadialGauge dash offsets
- **client:** bUnit tests for report builder components (ReportWidget, WidgetConfigPanel)
- **api:** Export controller auth tests for CSV endpoints
- **e2e:** Report chart interaction tests (tooltips, drill-down)
- **e2e:** Report export flow test for CSV download
- **e2e:** Custom report builder tests for drag/select and save/reload
- **e2e:** Accessibility coverage expanded to report pages
- **client:** Fix missing IBudgetApiService stub methods and service registrations in test classes
- **client:** Fix ThemeService IAsyncDisposable handling with IAsyncLifetime in bUnit tests

### Documentation

- **docs:** Update Feature 053 spec for custom report builder layout, endpoints, and Phase 7 status
- **docs:** Feature 053 updates for grid layout, presets, tests, and deferred items
- **docs:** Chart component standards added to component guidelines
- **docs:** Feature 053.1 — build and test fix catalog for features 051–053
- **readme:** Reports and component showcase documentation updates

## [3.14.1] - 2026-02-08

### Bug Fixes

- **client:** Consolidate donut chart styling across Calendar and Reports pages — align StrokeWidth, currency, fallback color, and segment filtering

### Testing

- **client:** 2 bUnit tests for CalendarInsightsPanel chart segment filtering (zero-amount exclusion, descending sort order)

### Documentation

- **docs:** Feature 067 spec — consolidate donut chart styling across pages

## [3.14.0] - 2026-02-08

### Features

- **client:** DateRangePicker component with quick presets (This Month, Last Month, Last 7/30 Days, Custom)
- **client:** BarChart component — pure SVG grouped bar chart with legend, tooltips, and ARIA labels
- **client:** MonthlyTrendsReport page (`/reports/trends`) with month count selector, category filter, bar chart, summary card, and data table
- **client:** Enhanced MonthlyCategoriesReport with DateRangePicker, URL query params, and flexible date range support
- **client:** Activate Monthly Trends card on ReportsIndex as live link
- **client:** Add `GetCategoryReportByRangeAsync` and `GetSpendingTrendsAsync` to BudgetApiService
- **client:** BudgetComparisonReport page (`/reports/budget-comparison`) with grouped BarChart, data table, summary card, month navigation, and ScopeService subscription
- **client:** Enable Budget vs. Actual card on ReportsIndex as live link
- **client:** CalendarInsightsPanel — collapsible monthly analytics panel on calendar page with income/spending/net, top categories, mini DonutChart, and TrendIndicator (% change vs. previous month)
- **client:** TrendIndicator component for color-coded percentage changes with InvertColors support
- **client:** DaySummary component — day-level analytics (income, spending, net, top categories) integrated into DayDetail panel
- **client:** Add "View Reports" button to Calendar header for calendar-to-reports navigation with month context
- **client:** Add `GetDaySummaryAsync` to IBudgetApiService and BudgetApiService
- **client:** WeekSummary component — week-level analytics panel with income/spending/net, daily average, and daily breakdown
- **client:** CalendarGrid week row selection — week-number buttons (W1–W6) with visual highlight and aria-pressed
- **api:** Enhanced ReportsController XML docs with `<remarks>` examples for Scalar/OpenAPI documentation

### Bug Fixes

- **client:** Fix "Back to Calendar" links on MonthlyCategoriesReport and BudgetComparisonReport to preserve report month context instead of always navigating to the current month

### Testing

- **client:** 13 bUnit tests for DateRangePicker (presets, date inputs, validation, sizing)
- **client:** 10 bUnit tests for BarChart (empty state, grouped bars, legend, ARIA labels, colors)
- **client:** 16 bUnit tests for BudgetComparisonReport (summary, chart, data table, navigation, empty states, error handling)
- **client:** 15 bUnit tests for CalendarInsightsPanel (collapsed/expanded state, data loading, trend indicators, categories)
- **client:** 11 bUnit tests for TrendIndicator (positive/negative/zero trends, InvertColors, formatting)
- **client:** 11 bUnit tests for DaySummary (income/spending/net display, categories, empty state, null response, accessibility)
- **client:** 15 bUnit tests for WeekSummary (visibility, income/spending, net, daily breakdown, date range, accessibility, today highlight, daily average)
- **client:** 8 bUnit tests for CalendarGrid week selection (week click events, visual highlight, aria-pressed, header cells, aria labels)
- **e2e:** 10 Playwright tests for report navigation (page loads, URL params, calendar→reports flow, console errors, accessibility audits)
- **e2e:** 2 navigation tests for MonthlyTrends and BudgetComparison pages

### Documentation

- **docs:** Feature 050 spec updated to All 6 Phases Complete with implementation notes
- **readme:** Enhanced reports description with date range filtering, week summaries, and quick insights

## [3.13.0] - 2026-02-07

### Features

- **client:** Mobile-optimized calendar experience with swipe navigation and week view (Feature 047)
- **client:** SwipeContainer component with JS interop for horizontal swipe gesture detection
- **client:** CalendarWeekView component with 7-day grid, navigation, and overflow support
- **client:** CalendarViewToggle component (Month/Week) with localStorage persistence
- **client:** Floating Action Button (FAB) with speed dial for Quick Add and AI Assistant
- **client:** QuickAddForm component with touch-optimized inputs (48px min-height, native keyboards)
- **client:** BottomSheet component for mobile modal presentation with swipe-to-dismiss
- **client:** MobileChatSheet component for AI Assistant in bottom-sheet presentation
- **client:** ToastContainer and ToastService for app-wide toast notifications with auto-dismiss
- **client:** Accessible theme with WCAG 2.0 AA high-contrast colors (Feature 046)
- **client:** Auto-detect Windows High Contrast Mode and prefers-contrast settings
- **client:** Calendar-first budget editing with inline goal management (Feature 048)
- **client:** CalendarBudgetPanel component - collapsible month summary with category list
- **client:** CalendarBudgetCategoryRow component - category row with progress bar and edit actions
- **client:** BudgetGoalModal component - create/edit/delete budget goals
- **api:** POST /api/v1/budgets/copy endpoint to copy goals between months

### Bug Fixes

- **client:** Fix CalendarBudgetCategoryRow rendering category icons as raw text instead of SVG
- **client:** Add 14 missing SVG icon paths to Icon.razor (cart, car, bolt, film, utensils, heart, etc.)
- **client:** Fix CSS class mismatch in CalendarBudgetCategoryRow (category-row → budget-category-row)
- **client:** Fix category icons rendering at 40px instead of 16px due to global CSS override
- **client:** Fix budget panel defaulting to expanded, pushing calendar below viewport fold
- **client:** Fix budget panel two-tone color when collapsed (panel-header → budget-panel-header alignment)
- **client:** Add button reset styles (width: 100%, border: none) to budget panel header

### Accessibility

- **client:** Skip-link for keyboard users to bypass navigation
- **client:** ARIA landmarks on MainLayout (banner, navigation, main)
- **client:** Modal component with role="dialog", aria-modal, Escape key close
- **client:** NavMenu with semantic list structure and aria-expanded/aria-controls
- **client:** WCAG 2.5.5 touch target compliance (44px minimum) for all mobile interactive elements
- **e2e:** Add axe-core accessibility tests for all major pages (14 tests)
- **e2e:** Add mobile accessibility tests for FAB, BottomSheet, Week View, and AI Chat

### Documentation

- **docs:** Feature 047 mobile experience technical design and implementation plan
- **docs:** ACCESSIBILITY.md guide with WCAG requirements, testing checklists
- **docs:** THEMING.md updated with Accessible theme and auto-detection details

### Testing

- **client:** Add bUnit tests for SwipeContainer, ToastContainer, QuickAddForm, CalendarViewToggle, CalendarWeekView, MobileChatSheet
- **client:** Add ToastService unit tests (10 tests)
- **client:** Add 35 bUnit tests for budget panel components
- **e2e:** Add mobile E2E tests: FAB, Quick Add, Calendar, Week View, AI Chat, Orientation, Touch Targets
- **api:** Add 7 integration tests for copy goals endpoint
- **application:** Add 4 unit tests for CopyGoalsAsync service method

## [3.12.0] - 2026-02-01

### Features

- **client:** Button component with variant/size support, loading states, and icon slots (Feature 045)
- **client:** Badge component for status indicators with 5 variants
- **client:** Card component with header/body/footer sections
- **client:** EmptyState component for consistent empty list displays
- **client:** FormField component for standardized form labels, help text, and validation

### Refactoring

- **client:** Migrate 15 pages from raw `<button>` to `<Button>` component
- **client:** Migrate 13 form components to use FormField and Button components
- **client:** Standardize button variants: Primary, Secondary, Success, Danger, Warning, Ghost, Outline

### Documentation

- **client:** Component catalog in Components/README.md with API docs
- **client:** CSS dependencies documentation for all Tier 1 components
- **client:** Migration guide for Button, FormField, and EmptyState patterns
- **docs:** COMPONENT-STANDARDS.md for naming conventions and patterns

### Testing

- **client:** Add 87 bUnit tests for new components (Button: 20, Badge: 13, Card: 11, EmptyState: 11, FormField: 14)

## [3.11.0] - 2026-02-01

### Features

- **client:** Uncategorized Transactions page with bulk categorize functionality (Feature 040)
- **api:** GET /api/v1/transactions/uncategorized endpoint with filtering, sorting, paging
- **api:** POST /api/v1/transactions/bulk-categorize endpoint for bulk category assignment
- **client:** Zero-flash authentication flow - no more "Checking authentication" or "Redirecting to login" flashes (Feature 052)
- **client:** AuthInitializer component resolves auth state before rendering any UI
- **client:** Branded loading overlay in index.html with theme support and reduced-motion
- **client:** Preload hints for critical CSS/JS assets (app.css, blazor.webassembly.js)
- **client:** New themes: Windows 95, macOS, GeoCities, Crayon Box
- **client:** ThemedIconRegistry for theme-specific icon customization

### Documentation

- **docs:** Add THEMING.md guide for creating and customizing themes
- **docs:** Add Feature 059 - Performance E2E Tests (deferred from 052)
- **docs:** Add Feature 054 - Silent Token Refresh (deferred from 052, reprioritized from 060)

### Refactoring

- **client:** Simplify MainLayout by removing redundant AuthorizeView wrapper
- **client:** Remove unused Bootstrap library (~1MB savings)

## [3.10.1] - 2026-02-01

### Documentation

- **license:** Add THIRD-PARTY-LICENSES.md with Lucide Icons ISC license attribution
- **client:** Add license reference comments to Icon.razor component

## [3.10.0] - 2026-02-01

### Features

- **client:** Consolidate AI features in UI with centralized availability service (Feature 043)
- **client:** Three-state AI availability model: Disabled, Unavailable, Available
- **client:** Grouped AI navigation under expandable "AI Tools" section
- **client:** Warning indicators when AI is enabled but Ollama unavailable
- **client:** AI Assistant button conditional visibility with warning badge
- **client:** AiAvailabilityService with cached status and StatusChanged events

### CI/CD

- **ci:** Optimize CI/CD pipeline with parallel matrix Docker builds (Feature 042)
- **ci:** Add build-and-test job running 1623 tests with NuGet caching
- **ci:** Add Dockerfile.prebuilt for pre-compiled artifact Docker builds
- **ci:** Use native `ubuntu-24.04-arm` runner for arm64 builds (no QEMU)
- **ci:** Parallel amd64/arm64 Docker builds with manifest merge
- **ci:** Tag strategy: `preview` on main branch, `latest` only on releases
- **ci:** Auto-cancel in-progress workflows on new pushes
- **ci:** TRX test result reporting with dorny/test-reporter

## [3.8.5] - 2026-01-31

### Bug Fixes

- **calendar:** Fix initial balance not appearing when account starts within calendar grid (Feature 057)
- **calendar:** Accounts with InitialBalanceDate on/after grid start now correctly show initial balance on their start date
- **calendar:** Days before an account's InitialBalanceDate correctly show $0 for that account
- **balance:** Add GetOpeningBalanceForDateAsync for calendar opening balance calculation
- **balance:** Add GetInitialBalancesByDateRangeAsync to handle accounts starting within visible grid

## [3.8.4] - 2026-01-26

### Bug Fixes

- **reconciliation:** Align client status request parameters with API (year/month)

## [3.8.3] - 2026-01-25

### Bug Fixes

- **import:** Fix "Account ID is required" error during CSV import preview
- **import:** CreatePreviewTransaction now uses valid GUID for temporary transaction matching

## [3.8.2] - 2026-01-23

### Bug Fixes

- **versioning:** Fix Docker builds showing 0.0.0-preview instead of actual version
- **ci:** Add MinVer CLI to GitHub Actions workflow to calculate version before Docker build
- **ci:** Fetch full git history in CI for accurate version calculation from tags
- **docker:** Pass VERSION build argument to override MinVer during container builds

## [3.8.0] - 2026-01-21

### Features

- **suggestions:** AI-Powered Category Suggestions system (Feature 032)
- **suggestions:** Analyze uncategorized transactions to suggest new budget categories
- **suggestions:** Merchant knowledge base with 100+ default merchant-to-category mappings
- **suggestions:** Accept/dismiss suggestions with optional category customization
- **suggestions:** Bulk accept multiple category suggestions at once
- **suggestions:** Auto-create categorization rules when accepting suggestions
- **suggestions:** Learning system records manual categorizations for future suggestions
- **suggestions:** CategorySuggestionsPage with card layout and modal dialogs
- **api:** Category suggestion REST endpoints (analyze, accept, dismiss, bulk-accept, preview-rules)
- **api:** Merchant mappings endpoints for managing learned patterns

## [3.7.0] - 2026-01-21

### Features

- **navigation:** Navigation Reorganization & UX Improvements (Feature 031)
- **navigation:** SessionStorage persistence for Reports/Accounts expand/collapse states
- **navigation:** Collapsible Accounts section with chevron indicator
- **navigation:** Improved link naming (Recurring Bills, Auto-Transfers, Auto-Categorize, Smart Insights)
- **navigation:** AI Settings consolidated into Settings page as AI tab
- **navigation:** Fixed sidebar layout with independent content scrolling
- **navigation:** LocalStorage persistence for sidebar collapsed state
- **navigation:** Tooltips on nav items in collapsed mode
- **import:** CSV Import Enhancements - Skip Rows & Debit/Credit Indicators (Feature 030)
- **import:** Skip rows setting to handle bank metadata rows before transaction data
- **import:** Debit/Credit indicator column support with configurable indicator values
- **import:** New AmountParseMode.IndicatorColumn for single amount with separate indicator
- **import:** SkipRowsSettings and DebitCreditIndicatorSettings domain value objects
- **import:** SkipRowsInput and IndicatorSettingsEditor UI components
- **import:** Saved mapping templates now include skip rows and indicator settings

## [3.6.0] - 2026-01-20

### Features

- **reports:** Reports Dashboard with monthly category spending analysis (Feature 029)
- **reports:** Pure SVG DonutChart component with interactive segments and hover tooltips
- **reports:** Monthly Categories Report page with spending breakdown and navigation
- **reports:** Reports landing page with cards for available and upcoming reports
- **reports:** Collapsible Reports section in navigation menu
- **api:** GET /api/v1/reports/categories/monthly endpoint for category spending data

## [3.5.0] - 2026-01-20

### Features

- **reconciliation:** Recurring Transaction Reconciliation system (Feature 028)
- **reconciliation:** Automatic matching of imported transactions to recurring instances
- **reconciliation:** Confidence scoring with High/Medium/Low match quality levels
- **reconciliation:** Reconciliation dashboard showing matched, pending, and missing instances
- **reconciliation:** Match review modal with accept/reject actions
- **reconciliation:** Manual match dialog for linking unlinked transactions
- **reconciliation:** Tolerance settings panel with strict/moderate/loose presets
- **reconciliation:** Import preview enhancement showing recurring match suggestions
- **reconciliation:** Variance tracking for amount differences between expected and actual
- **api:** Reconciliation REST endpoints for matches, status, and tolerances
- **infra:** Persistence layer for reconciliation matches and settings

## [3.4.0] - 2026-01-19

### Features

- **chat:** AI Chat Assistant for natural language transaction entry (Feature 026)
- **chat:** Create transactions, transfers, and recurring items via conversational commands
- **chat:** VS Code-style side panel that shrinks main content when open
- **chat:** Page context awareness - automatically detects current account/page
- **chat:** Action preview cards with confirm/cancel before execution
- **chat:** Persisted chat sessions with history

## [3.3.0] - 2026-01-19

### Features

- **import:** Intelligent CSV import system for bank transaction data (Feature 027)
- **import:** User-defined column mappings with source presets
- **import:** Support for multiple date and amount formats
- **import:** Import preview with validation and duplicate detection
- **import:** Saved mapping configurations per import source
- **import:** Integration with auto-categorization rules engine

## [3.2.0] - 2026-01-18

### Features

- **ai:** AI-powered rule suggestions using local Ollama models (Feature 025)
- **ai:** Pattern analysis for uncategorized transactions
- **ai:** Rule optimization recommendations
- **ai:** Configurable Ollama endpoint and model selection
- **e2e:** Playwright end-to-end test framework (Feature 024.4)
- **e2e:** Page object model for maintainable UI tests
- **e2e:** Smoke tests for application accessibility
- **e2e:** Navigation tests for all main pages
- **e2e:** Account management tests (CRUD operations)
- **e2e:** Budget page tests with month navigation

## [3.1.0] - 2026-01-17

### Features

- **rules:** Auto-categorization rules engine with pattern matching (Feature 024)
- **rules:** Rule management UI with create, edit, delete, and reorder
- **rules:** Bulk categorization of existing transactions by rule
- **rules:** Rule testing interface to preview matches before applying
- **api:** Categorization rules REST endpoints with priority ordering
- **app:** Automatic category assignment on transaction creation

### Bug Fixes

- **auth:** Handle Authentik 64-char hex sub claim format for user identification
- **ui:** Display category names instead of GUIDs in transaction lists
- **api:** Add CategoryName to transaction list API response for calendar view

### Miscellaneous

- **docs:** Remove references to FluentUI-Blazor components
- **client:** Implement IDisposable for scope change handlers

## [3.0.0] - 2026-01-17

### Features

- **auth:** Authentik OIDC integration for multi-user authentication
- **auth:** Personal and shared budget scopes with X-Budget-Scope header
- **auth:** User context service for accessing authenticated user info
- **api:** JWT Bearer authentication with configurable Authentik options
- **client:** OIDC authentication with PKCE flow in Blazor WASM
- **client:** User profile component with login/logout functionality
- **infra:** Multi-user data isolation with UserId on transactions, accounts, and recurring items
- **version:** Semantic versioning with MinVer from Git tags
- **version:** Automated GitHub Releases with changelog generation
- **version:** Runtime version endpoint at /api/version
- **version:** Version display in client UI footer

### Miscellaneous

- **ci:** Added release workflow for automated GitHub Releases
- **ci:** Configured git-cliff for conventional commit changelog

## [2.0.0] - 2026-01-10

### Features

- **categories:** Budget category management with CRUD operations
- **goals:** Budget goal creation with monthly/yearly targets
- **goals:** Budget progress tracking against spending goals
- **api:** Category and goal REST API endpoints
- **client:** Category management UI with create/edit/delete
- **client:** Goal setting interface with progress visualization
- **client:** Budget progress cards on dashboard

## [1.0.0] - 2025-12-15

### Features

- **accounts:** Multi-account support with running balances
- **transactions:** Transaction management with CRUD operations
- **transactions:** Transaction import from CSV (Bank of America, Capital One, UHCU formats)
- **recurring:** Recurring transactions with auto-realization
- **recurring:** Recurring transfers between accounts
- **transfers:** Internal transfer support between accounts
- **calendar:** Interactive calendar view with daily transaction summaries
- **calendar:** Day detail modal with transaction list
- **settings:** Application settings management
- **settings:** Allocation warning thresholds
- **client:** Blazor WebAssembly UI with custom design system
- **client:** Dark/light theme support with system preference detection
- **client:** Responsive layout with collapsible navigation
- **api:** RESTful API with OpenAPI documentation
- **api:** Scalar API reference UI
- **infra:** PostgreSQL database with EF Core
- **infra:** Docker multi-architecture builds (amd64, arm64)
- **infra:** Automated CI/CD with GitHub Actions
- **health:** Health check endpoints for liveness and readiness
