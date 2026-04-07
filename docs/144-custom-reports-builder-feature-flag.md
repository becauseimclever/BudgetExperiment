# Feature 144: Custom Reports Builder — Feature Flag

> **Status:** Planned

## Prerequisites

- Feature 129b (Feature Flag Implementation) must be merged before implementation begins.

---

## Feature Flag

**Flag Name:** `Features:Reports:CustomReportBuilder`  
**Default Value:** `false` (disabled by default)  
**When Enabled:** The Custom Reports Builder route is accessible and the nav menu item is visible  
**When Disabled:** The route `/reports/custom-report-builder` returns 404 or redirects to `/reports/dashboard`, and the nav menu item is hidden

---

## Overview

The Custom Reports Builder (`/reports/custom-report-builder`) allows power users to create arbitrary, complex data queries and visualizations. While powerful, this feature philosophically **conflicts with the Kakeibo calendar-first approach** established in Feature 128 — it encourages endless data exploration and report customization, moving focus away from the calendar as the primary reflection surface.

**Decision:** Feature-flag the Custom Reports Builder off by default. Users who explicitly opt in can enable it via the feature flag system. When enabled, the page displays an educational note reminding users that the calendar is the primary reflection surface.

This balances power-user needs (some users do want custom reporting) with the Kakeibo philosophy (calendar-first, intentional reflection).

---

## Domain Model Changes

**None.** No database changes. The Custom Reports Builder is a client-side and service-side feature that exists or doesn't based on the flag state.

---

## API Changes

**No new endpoints.** The existing custom report builder APIs (if any) are guarded by the feature flag check.

**Middleware/Guard Pattern:**

In `BudgetExperiment.Api`, any endpoint serving the custom report builder feature (e.g., `GET /api/v1/reports/custom/*` or similar) should check the feature flag:

```csharp
app.MapGet("/api/v1/reports/custom/{...}", CustomReportBuilder)
    .WithName("CustomReport")
    .RequireFeatureFlag("Features:Reports:CustomReportBuilder");
```

Or in the endpoint handler:

```csharp
var isEnabled = await featureFlagService.IsEnabledAsync("Features:Reports:CustomReportBuilder");
if (!isEnabled)
{
    return Results.NotFound();
}
// ... proceed with custom report logic
```

---

## UI Changes

**Modified Components:**
1. Navigation menu (sidebar or top nav)
2. `/reports` dashboard landing page
3. Route `/reports/custom-report-builder` (if it exists) or redirect target

**Changes:**

1. **Navigation Menu — Conditional Link**
   - The "Custom Report Builder" or similar menu item is hidden when the flag is disabled
   - Shown when the flag is enabled
   - Controlled by the `IFeatureFlagClientService` available to the layout/nav component
   - Example:
     ```csharp
     @if (featureFlags?.IsEnabled("Features:Reports:CustomReportBuilder") ?? false)
     {
         <NavLink href="/reports/custom-report-builder">
             Custom Report Builder
         </NavLink>
     }
     ```

2. **Reports Dashboard**
   - No change to the main dashboard when flag is disabled
   - When flag is enabled, add a card/tile: "Custom Report Builder" with description: "Build custom reports and queries"
   - Card links to `/reports/custom-report-builder`

3. **Custom Report Builder Page** — Educational Note
   - When accessed (flag enabled), display a prominent note at the top:
     > "The calendar is your primary reflection surface. Custom reports are for deep-dive analysis only. If you find yourself spending more time here than on the calendar, consider whether the report is serving your budgeting goals."
   - Use a subtle warning/info color (blue or amber background)
   - Allow dismissal (store in `localStorage`) but display on page load

4. **Unavailable State** — When Flag is Disabled
   - If user attempts to navigate to `/reports/custom-report-builder` and the flag is disabled:
     - Option A: Return a 404 page with a message: "This feature is not available."
     - Option B: Redirect to `/reports/dashboard`
   - Either approach is acceptable

---

## Acceptance Criteria

- [ ] Feature flag `Features:Reports:CustomReportBuilder` is defined in the `FeatureFlags` table with default `false`
- [ ] API endpoints serving the custom report builder are guarded by the feature flag check
- [ ] Requests to custom report endpoints return 404 or error when the flag is disabled
- [ ] Navigation menu item "Custom Report Builder" is conditionally hidden/shown based on flag state
- [ ] Reports dashboard tile "Custom Report Builder" only appears when flag is enabled
- [ ] Attempting to navigate to `/reports/custom-report-builder` while disabled returns 404 or redirects to `/reports/dashboard`
- [ ] When flag is enabled, the Custom Report Builder page displays the educational note about the calendar
- [ ] Educational note can be dismissed and the dismissal is persisted to `localStorage`
- [ ] The note re-appears on subsequent page loads (or only once per session, depending on UX preference)
- [ ] All unit and integration tests pass; OpenAPI spec is updated
- [ ] Feature flag toggle works at runtime (admin can enable/disable without app restart)

---

## Implementation Notes

- **Philosophy Alignment:** This feature is intentionally gated to preserve the Kakeibo calendar-first philosophy. The default-off approach encourages users to develop a habit of calendar reflection before reaching for powerful but potentially distracting custom reporting tools.
- **Runtime Toggle:** The feature flag can be toggled at runtime via the `PUT /api/v1/features/Features:Reports:CustomReportBuilder` admin endpoint (Feature 129b). No app restart needed.
- **Client-Side Gate:** The Blazor client checks the feature flag before rendering the nav item or allowing route access. This improves UX (no 404 pages if the client knows the feature is disabled) and reduces API load.
- **Educational Message:** The note on the page should be friendly and non-judgmental — it's a reminder, not a restriction. Many users will benefit from custom reports; the goal is to ensure the calendar remains the primary surface.
- **Dismissal Persistence:** Store dismissal in `localStorage` keyed by `customReportBuilderEducationalNoteDismissed`. Optionally, reset the dismissal when the flag is toggled on (so users see the note again if it's re-enabled).
- **Admin Access:** Admins can enable the flag via the `/api/v1/features` admin endpoint. Once enabled, it's visible to all users (unless further per-user controls are added in a future feature).

