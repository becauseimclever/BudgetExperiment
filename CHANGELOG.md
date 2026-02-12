# Changelog

All notable changes to Budget Experiment.

## [Unreleased]

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

### Documentation

- **docs:** Update Feature 053 spec for custom report builder layout, endpoints, and Phase 7 status
- **docs:** Feature 053 updates for grid layout, presets, tests, and deferred items
- **docs:** Chart component standards added to component guidelines
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
- **docs:** Add Feature 060 - Silent Token Refresh (deferred from 052)

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
