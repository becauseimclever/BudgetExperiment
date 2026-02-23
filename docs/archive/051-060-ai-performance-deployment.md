# AI, Performance & Deployment (051-060) - Consolidated Summary

**Consolidated:** 2026-02-22  
**Original Features:** 051 through 060 (including 053.1)  
**Status:** All Completed

---

## Overview

This document consolidates features 051–060, which focused on AI assistant context awareness, performance optimization (zero-flash auth, Core Web Vitals), a comprehensive reporting and data portability overhaul, silent token refresh, flexible deployment with optional auth and multiple OIDC providers, an API config endpoint, calendar balance bug fixes, an in-app licenses page, performance E2E tests, and transaction location data with choropleth map reporting.

---

## 051: AI Assistant Calendar Context

**Completed:** 2026-02-10

Made the AI assistant aware of the user's current calendar view, so date-sensitive actions (e.g., "add a transaction") use the selected calendar date instead of defaulting to today.

**Key Outcomes:**
- `ChatPageContext` extended with `CalendarViewedYear`, `CalendarViewedMonth`, `SelectedDate`
- Calendar page injects `IChatContextService` and updates context on date/month changes
- `ChatContextDto` added to Contracts; `SendMessageRequest` carries context to API
- API controller passes context through to `ChatService` (previously passed `null`)
- `NaturalLanguageParser.FormatContext` emits "Viewing [Month Year], selected: [Date]" in AI prompts
- AI falls back to context date when it omits a date; explicit AI dates take priority
- Chat panel displays a context hint showing the active calendar date
- Context cleared when navigating away from the calendar

**Files Changed:**
- `Services/ChatContextService.cs`, `Pages/Calendar.razor`, `Components/Chat/ChatPanel.razor`
- `Services/ChatApiService.cs`, `Contracts/Dtos/ChatDtos.cs`, `Controllers/ChatController.cs`
- `Application/Chat/NaturalLanguageParser.cs`

---

## 052: Performance — Time to First Paint & Page Load

**Completed:** 2026-02-01

Eliminated authentication flashes and improved perceived load time with a branded loading skeleton and an `AuthInitializer` component that resolves auth state before rendering any UI.

**Key Outcomes:**
- Branded loading spinner in `index.html` with preconnect hints and `prefers-reduced-motion` support
- `AuthInitializer` component resolves auth before any Blazor UI renders
- Unauthenticated users redirect immediately to Authentik — zero intermediate messages
- Removed flashing `<Authorizing>` / `<NotAuthorized>` templates from `App.razor` and `MainLayout.razor`
- Removed unused Bootstrap (~1 MB savings)
- 7 bUnit tests covering all `AuthInitializer` scenarios

**Performance Targets:**
| Metric | Target |
|--------|--------|
| First Contentful Paint | < 1.5 s |
| Time to Interactive | < 3 s |
| Largest Contentful Paint | < 2.5 s |
| Auth Flash Count | 0 |

---

## 053: Reporting & Data Portability Overhaul

**Completed:** 2026-02-10

Delivered a comprehensive chart component library, export infrastructure, and a custom report builder.

**Key Outcomes:**
- **Chart Components (pure SVG):** BarChart, GroupedBarChart, StackedBarChart, LineChart, AreaChart, SparkLine, ProgressBar, RadialGauge
- **Shared Chart Primitives:** ChartAxis, ChartGrid, ChartTooltip for consistent rendering
- **Export Infrastructure:** CSV export endpoint (`/api/v1/exports/categories/monthly`); Excel and PDF deferred
- **Custom Report Builder:** Widget palette, canvas with save/load layouts, CRUD API at `/api/v1/custom-reports`
- All charts accessible with ARIA labels, keyboard navigation, and high-contrast support

---

## 053.1: Build & Test Fixes for 051–053

**Completed:** 2026-02-10

Post-implementation fix-up that resolved 188 build errors and 23 test failures introduced across features 051–053.

**Issues Fixed:**
1. **Razor parser failure** — escaped quotes in interpolated strings inside `@code` blocks; extracted to local variables
2. **`@layout` directive conflict** — renamed `layout` variable to `savedLayout` in `CustomReportBuilder.razor`
3. **Missing global usings** — added `BudgetExperiment.Domain.Reports` to Infrastructure and Application
4. **Missing interface stubs** — 5 new `IBudgetApiService` methods added to 3 test stub classes
5. **Missing service registrations** — `ThemeService`, `IToastService`, `IExportDownloadService` registered in test DI containers

**Result:** 0 errors, 0 warnings, all 1994 tests passing.

---

## 054: Silent Token Refresh

**Completed:** 2026-02-19

Added graceful handling for mid-session token expiry so users aren't interrupted.

**Key Outcomes:**
- `TokenRefreshHandler` (DelegatingHandler) intercepts 401 responses and attempts silent OIDC token refresh
- Thread-safe refresh via `SemaphoreSlim` — prevents parallel refresh storms
- On successful refresh, original request is retried transparently
- On failure, "Session expired" toast notification shown before redirect to login
- `IFormStateService` preserves unsaved form data to local storage before redirect; forms restore on re-auth
- Return URL preserved for seamless post-login navigation
- 5 Playwright E2E tests covering toast, no duplicates, form preservation, re-auth, and valid session baseline

---

## 055: Easy Deployment — Optional Auth & Flexible Providers

**Completed:** 2026-02-22

Lowered the barrier to entry with a zero-config demo mode, optional authentication, and support for multiple OIDC providers.

**Key Outcomes:**
- **Demo Docker Compose** (`docker-compose.demo.yml`): bundled PostgreSQL 16, auth-off default, health checks, persistent volume — single `docker compose up` to run
- **Auth-Off Mode** (`Authentication__Mode=None`): `NoAuthHandler` auto-authenticates all requests as a deterministic "family" user; `AuthOffBanner` in client; login/profile UI hidden
- **OIDC Providers:** Authentik (default), Google, Microsoft Entra ID, Generic OIDC (Keycloak, Auth0, Okta)
- Per-provider claim mappers: `GoogleClaimMapper`, `MicrosoftClaimMapper`, `GenericOidcClaimMapper`
- `AuthenticationOptions` refactored with `Mode` (None / OIDC) and `Provider` enum
- `docs/AUTH-PROVIDERS.md` — per-provider setup guide with troubleshooting
- 9 phases delivered with 200+ new tests, zero regressions

---

## 056: API Config Endpoint

**Completed:** 2026-01-30

Created a single source of truth for client configuration, eliminating the need for a static `wwwroot/appsettings.json` in the Blazor WASM client.

**Key Outcomes:**
- `GET /api/v1/config` (unauthenticated) returns client-relevant configuration (auth mode, OIDC settings)
- Client `Program.cs` fetches config from API at startup — no more baked-in static config
- Docker operators configure everything via environment variables; no custom image builds needed
- Endpoint versioned, documented in OpenAPI spec with examples
- Only safe, non-secret settings exposed (no connection strings or API keys)

---

## 057: Calendar Initial Balance Bug Fix

**Status:** ✅ Done

Fixed incorrect running balance on the calendar when an account's `InitialBalanceDate` falls on or after the calendar grid's start date.

**Key Outcomes:**
- Root cause: `BalanceCalculationService.GetBalanceBeforeDateAsync` used strict `<` comparison, excluding initial balance when `InitialBalanceDate == gridStartDate`
- Fix adjusted calendar logic so the initial balance is correctly incorporated from `InitialBalanceDate` onward
- End-of-day balance on `InitialBalanceDate` now equals initial balance + transactions on that day
- Aggregated "All Accounts" view correctly includes each account's initial balance from its respective start date

---

## 058: Licenses Page in Client

**Status:** ✅ Done

Added an in-app "Licenses" page so end users can view third-party license attributions directly in the deployed application.

**Key Outcomes:**
- Dedicated `/licenses` page accessible from footer link in `MainLayout`
- License content defined as structured Razor markup with collapsible `<details>` sections
- Each entry shows: component name, license type, copyright, and full license text
- Page accessible without authentication
- Adding a new license requires copying a `license-entry` div block in `Licenses.razor`

---

## 059: Performance E2E Tests with Core Web Vitals

**Completed:** 2026-02-19

Added automated Playwright-based performance testing capturing Core Web Vitals and verifying zero-flash authentication.

**Key Outcomes:**
- Playwright tests capture FCP, LCP, TTI, and CLS metrics on every run
- **Thresholds enforced:** FCP < 1.5 s, LCP < 2.5 s, TTI < 3.0 s, CLS < 0.1
- Zero-flash auth verification: test fails if "Checking authentication", "Loading...", or "Redirecting to login" appears during load
- Metrics logged to CI output for trend tracking
- CI enablement tracked in GitHub issue #17

**Test Structure:**
```
tests/BudgetExperiment.E2E.Tests/Tests/
├── PerformanceTests.cs       # Core Web Vitals
└── ZeroFlashAuthTests.cs     # Auth flash verification
```

---

## 060: Transaction Location Data & Choropleth Reporting

**Completed:** 2026-02-22

Added optional geographic location data to transactions with choropleth map visualizations for spending-by-location insights.

**Key Outcomes (10 vertical slices):**
1. **Domain Primitives** — `GeoCoordinate`, `LocationSource` enum, `TransactionLocation` sealed records; `Transaction.SetLocation()` / `ClearLocation()`
2. **Feature Toggle** — `EnableLocationData` user setting gates all location features
3. **Persist & Expose via API** — EF Core configuration, location columns in transactions table, API endpoints for read/write
4. **Manual Location Entry UI** — Location input fields on transaction form (coordinates, city, state, country)
5. **Location Parser** — Extracts city/state from transaction descriptions (e.g., "AMAZON.COM SEATTLE WA")
6. **Reverse Geocoding & GPS Capture** — Browser Geolocation API + Nominatim reverse geocoding (no API key required)
7. **Location Spending Report API** — Aggregated spending by state/region endpoint
8. **Choropleth Map & Report Page** — Interactive SVG US state map with drill-down, color-scaled by spending
9. **Import Pipeline Integration** — Location parser runs during CSV import when feature is enabled
10. **Bulk Delete Location Data** — Privacy control to clear all stored location data

**Privacy:** Feature is opt-in (default off), with clear disclosure and easy bulk deletion.

---
