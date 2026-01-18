# Changelog

All notable changes to Budget Experiment.

## [Unreleased]

### Features

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
