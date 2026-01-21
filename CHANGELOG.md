# Changelog

All notable changes to Budget Experiment.

## [Unreleased]

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
