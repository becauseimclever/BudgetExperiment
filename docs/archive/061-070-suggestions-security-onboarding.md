# Suggestions, Security, Onboarding & Bug Fixes (061-070) - Consolidated Summary

**Consolidated:** 2026-02-28  
**Original Features:** 061 through 070  
**Status:** All Completed

---

## Overview

This document consolidates features 061–070, which covered restoring dismissed category suggestions, a comprehensive functional E2E test suite, secure file upload hardening (client-side CSV parsing), global currency derivation from user settings, report scope filtering audit and fix, initial user onboarding (currency & first day of week), donut chart styling consolidation, Playwright local critical user flow tests, a CSV import double-skip bug fix, and an AI suggestions JSON extraction bug fix.

---

## 061: Restore Dismissed Category Suggestions

**Completed:** 2026-02-28

Added the ability for users to view, restore, and clear dismissed category suggestions — previously, dismissing a suggestion was irreversible without direct database access.

**Key Outcomes:**
- **Slice 1 — View Dismissed:** `GET /api/v1/categorysuggestions/dismissed` endpoint returns dismissed suggestions; client shows a "Dismissed" tab with muted styling
- **Slice 2 — Restore:** `POST /api/v1/categorysuggestions/{id}/restore` transitions a suggestion from Dismissed back to Pending; removes matching `DismissedSuggestionPattern` entries so future analysis can re-suggest
- **Slice 3 — Clear Patterns:** `DELETE /api/v1/categorysuggestions/dismissed-patterns` bulk-deletes dismissed pattern memory for the user, enabling re-analysis to produce fresh suggestions
- Domain `Restore()` method enforces Dismissed → Pending transition only
- Full TDD coverage across domain, application, and API layers

---

## 062: Functional E2E Test Suite

**Completed:** 2026-02-14

Established a comprehensive functional E2E test suite using Playwright, consolidating fragmented E2E scenarios (including cancelled Feature 041) into a maintainable, extensible suite.

**Key Outcomes:**
- Calendar navigation and display tests (month navigation, transaction display, daily totals)
- Starting balance validation (running balance starts from account starting balance, accumulates correctly)
- Transaction CRUD operations (create, read, update, delete via calendar)
- Account management tests (create account, switch accounts, filter correctly)
- CSV import flow tests (upload, field mapping, preview, import execution)
- VS Code task runner integration for local and DemoSafe execution
- Tests run against demo environment with seeded data; idempotent with unique identifiers

---

## 063: Secure File Upload Hardening

**Completed:** 2026-02-28

Moved all CSV parsing to the client (Blazor WebAssembly), eliminating the server-side file upload attack surface entirely. Delivered across 6 vertical slices.

**Key Outcomes:**
- **Slice 1:** Client-side `CsvParserService` in Blazor WASM — instant preview with zero server calls for parsing; supports BOM removal, auto-delimiter detection, quote-aware splitting, skipRows
- **Slice 2:** `CsvSanitizer` prefixes formula-trigger characters (`=`, `@`, `+`, `-`, `\t`, `\r`) with `'` during parsing to prevent CSV injection
- **Slice 3:** Server-side import execute validation — max 5000 transactions, field length limits (description 500, category 100), date range (365 days future), amount range (±99,999,999.99), `[RequestSizeLimit]` on endpoints
- **Slice 4:** Removed `POST /api/v1/import/parse` endpoint, deleted server-side `ICsvParserService`/`CsvParserService`, cleaned up DI and tests
- **Slice 5:** Preview endpoint max-rows validation (10,000 rows limit)
- **Slice 6:** Documentation and cleanup
- 34 client-side parser tests ported; all import tests passing

---

## 064: Global Currency — Replace Hardcoded USD

**Completed:** 2026-02-23

Replaced all ~50+ hardcoded `"USD"` strings across Application services with the user's global `PreferredCurrency` from `UserSettings` via a new `ICurrencyProvider` abstraction.

**Key Outcomes:**
- `ICurrencyProvider` interface in Domain; `UserSettingsCurrencyProvider` implementation in Application resolves currency from `UserSettings.PreferredCurrency` with `"USD"` fallback
- Systematic replacement across 6 slices: ReportService (14), CalendarGridService (8), DayDetailService (3), PastDueService (1), TransactionListService (8), BalanceCalculationService (6), ChatService (4), ImportService (2), BudgetProgressService (2), BudgetCategoryService (1), PaycheckAllocationService (1)
- `MoneyDto.Currency` default changed from `"USD"` to `string.Empty`
- 562 total Application tests passing; zero hardcoded `"USD"` in Application layer
- Complementary to Feature 066 (onboarding sets the currency)

---

## 065: Report Scope Filtering Audit & Fix

**Completed:** 2026-02-28

Fixed a data isolation bug where `TransactionRepository.GetByDateRangeAsync` did not apply `ApplyScopeFilter`, causing all report endpoints to leak data across budget scopes (Personal/Shared/All).

**Key Outcomes:**
- Fixed `GetByDateRangeAsync` — added `ApplyScopeFilter` call so reports respect Personal/Shared/All scope
- Fixed `GetByIdWithExceptionsAsync` — same missing scope filter
- Full audit of all repositories: `RecurringTransactionRepository`, `RecurringTransferRepository`, `BudgetGoalRepository`, `ReconciliationMatchRepository` confirmed clean
- 4 integration tests added to verify scope isolation
- Prevention comments added on all `ApplyScopeFilter` methods to remind developers

---

## 066: Initial User Onboarding — Currency & First Day of Week

**Completed:** 2026-02-28

Introduced a first-run onboarding wizard that captures preferred currency and first day of the week before users begin using the app.

**Key Outcomes:**
- Domain: `FirstDayOfWeek` (restricted to Sunday/Monday) and `IsOnboarded` properties added to `UserSettings`; validation via `DomainException`
- Infrastructure: EF migration for new columns with defaults (Sunday, false)
- API: `POST api/v1/user/settings/complete-onboarding` convenience endpoint; existing GET/PUT endpoints carry new fields
- Client: Step-based onboarding wizard (Welcome → Currency → First Day of Week → Confirm) with skip option
- Onboarding guard redirects to `/onboarding` when `IsOnboarded == false`; cached client-side after first check
- Searchable currency dropdown with common ISO 4217 codes
- Settings page updated with "User Preferences" section for post-onboarding editing
- Calendar `FirstDayOfWeek` rendering deferred to future work

---

## 067: Consolidate Donut Chart Styling

**Completed:** 2026-02-08

Aligned donut chart visual styling between the Calendar Insights Panel and Monthly Categories Report page for a consistent look.

**Key Outcomes:**
- Reports page `StrokeWidth` changed from 50 to 20, matching Calendar proportions (ring-to-center-hole ratio now consistent)
- Reports page now passes actual `Currency` parameter instead of defaulting to `"USD"`
- Fallback color standardized to `#6b7280` on both pages
- Calendar Insights Panel updated to filter out zero-amount categories and sort segments by amount descending
- Unit tests for segment filtering, sorting, and fallback color

---

## 068: Playwright Local Critical User Flows

**Completed:** 2026-02-28

Implemented a focused Playwright E2E suite for the three most critical creation workflows, designed for local development use.

**Key Outcomes:**
- 6 Playwright tests covering: transaction creation (list + calendar verification), account creation (accounts page + calendar filter), category creation (categories page + transaction form picker)
- All tests use `LocalCritical` trait category; excluded from CI via existing filter
- Tests use `TestDataHelper.CreateUniqueName()` for isolation and clean up after themselves
- **Bug fix discovered:** `TransactionForm.razor` currency default — `MoneyDto.Currency` empty string was not caught by null check; fixed with `string.IsNullOrWhiteSpace()` and initialization in `OnParametersSet()`
- Local run: `dotnet test --filter "Category=LocalCritical"` with `RUN_E2E_TESTS=true`

---

## 069: CSV Import Skip Rows Double-Skip Bug

**Completed:** 2026-02-28

Fixed a bug where setting "rows to skip" during CSV import caused the first N data rows to be silently discarded in addition to the correct metadata skip.

**Key Outcomes:**
- Root cause: `RowsToSkip` was applied twice — once in `CsvParserService.ParseAsync` (correct) and again in `ImportService.PreviewAsync` (redundant)
- Fix: Removed redundant skip logic in `PreviewAsync`; `RowsToSkip` now only used for display row-index offset calculation
- Regression test added: `PreviewAsync_WithRowsToSkip_DoesNotDoubleSkipAlreadyParsedRows`
- 2 existing tests updated that were asserting the buggy behavior
- 45 ImportService tests + 34 CsvParserService tests all passing

---

## 070: AI Suggestions JSON Extraction Bug

**Completed:** 2026-02-28

Fixed a bug that rendered the entire AI Suggestions feature non-functional — suggestions always showed "No Suggestions" because AI responses wrapped in markdown code blocks were silently discarded during JSON parsing.

**Key Outcomes:**
- Root cause: `RuleSuggestionService` parsed raw AI text directly with `JsonSerializer.Deserialize`, which threw `JsonException` on markdown-wrapped responses; exceptions were silently caught and returned empty arrays
- Fix: Added `ExtractJson()` method that locates first `{` and last `}` to extract the JSON object, matching the pattern already used in `NaturalLanguageParser`
- Applied to all three parsing methods: `ParseNewRuleSuggestions`, `ParseOptimizationSuggestions`, `ParseConflictSuggestions`
- Handles: pure JSON, markdown code blocks, preamble text, trailing text
- 6 unit tests for `ExtractJson` + 2 integration tests for markdown-wrapped responses
- 42 RuleSuggestionService tests passing
