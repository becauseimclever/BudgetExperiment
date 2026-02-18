# Feature 058: Licenses Page in Client Application
> **Status:** ✅ Done

## Overview

Add a "Licenses" or "Open Source Licenses" page to the Blazor WebAssembly client that displays all third-party license information. This ensures users can view license attributions directly in the application, promoting transparency and compliance with open-source license requirements.

## Problem Statement

While the repository now includes a `THIRD-PARTY-LICENSES.md` file for developer/repository visibility, end users of the deployed application cannot easily access this information. Many open-source licenses require that attribution be made available to users, not just developers.

### Current State

- `THIRD-PARTY-LICENSES.md` exists in the repository root
- License information is only visible to those who access the source code
- No in-app visibility of third-party license attributions
- Users have no way to see what open-source components are used

### Target State

- Dedicated "Licenses" page accessible from the application (e.g., footer link or Settings)
- Displays all third-party licenses in a readable format
- Easy to maintain as new dependencies are added
- Compliant with open-source license attribution requirements

---

## User Stories

### License Visibility

#### US-058-001: View third-party licenses in the app
**As a** user  
**I want to** view the licenses of third-party components used in the app  
**So that** I can understand what open-source software powers the application

**Acceptance Criteria:**
- [x] Licenses page is accessible from the UI (footer link in MainLayout)
- [x] All third-party licenses are displayed with proper formatting
- [x] Each license shows: component name, license type, copyright, and full license text
- [x] Page is accessible without authentication

#### US-058-002: Easy maintenance of license information
**As a** developer  
**I want to** easily add new license entries when adding dependencies  
**So that** the licenses page stays up-to-date

**Acceptance Criteria:**
- [x] License data is stored in a maintainable format (Razor markup in `Licenses.razor`)
- [x] Adding a new license requires minimal effort (copy a `license-entry` div block)
- [ ] Documentation explains how to add new license entries

---

## Technical Design

### Architecture Options

#### Option A: Static Markdown Rendering (Recommended)
Serve the existing `THIRD-PARTY-LICENSES.md` file and render it as HTML in the client.

**Pros:**
- Single source of truth (one file to maintain)
- Markdown already exists
- Simple implementation

**Cons:**
- Requires Markdown parsing in Blazor

#### Option B: JSON/Code-Based License Registry
Create a structured data file (JSON or C# class) with license information.

```csharp
public static class LicenseRegistry
{
    public static readonly List<LicenseInfo> Licenses = new()
    {
        new("Lucide Icons", "ISC", "Cole Bemis, Lucide Contributors", "...full text..."),
        // Add more as needed
    };
}
```

**Pros:**
- Strongly typed
- Easy to render in UI
- Can include metadata (URLs, versions)

**Cons:**
- Duplicate of THIRD-PARTY-LICENSES.md content

#### Option C: Hybrid Approach
Keep `THIRD-PARTY-LICENSES.md` as the source of truth, but also generate a JSON file during build for the client to consume.

### Chosen Approach

**Option B (simplified)** — License content is defined directly in `Licenses.razor` as structured Razor markup with collapsible `<details>` sections. No Markdown parsing needed; no duplicate file to maintain. New licenses are added by copying a `license-entry` block.

### Files Created/Modified

| File | Change |
|------|--------|
| `Pages/Licenses.razor` | **NEW** — Licenses page at `/licenses` with Lucide Icons ISC license |
| `Pages/Licenses.razor.css` | **NEW** — Scoped styles for the licenses page |
| `Layout/MainLayout.razor` | Added `<footer>` with "Open Source Licenses" link |
| `App.razor` | Renamed `IsAuthenticationRoute` → `IsPublicRoute`; added Licenses to public routes |
| `wwwroot/css/design-system/layout.css` | Enhanced `.app-footer` styles (centered, link hover) |

### UI Design

```
┌─────────────────────────────────────────────────────────┐
│  Open Source Licenses                                   │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Budget Experiment uses the following open source       │
│  components. We are grateful to these projects and      │
│  their contributors.                                    │
│                                                         │
│  ─────────────────────────────────────────────────────  │
│                                                         │
│  ## Lucide Icons                                        │
│                                                         │
│  The SVG icon paths used in this application are        │
│  derived from Lucide Icons (https://lucide.dev/).       │
│                                                         │
│  ISC License                                            │
│  Copyright (c) for portions of Lucide are held by       │
│  Cole Bemis 2013-2022 as part of Feather Icons...       │
│                                                         │
│  [Full license text in collapsible section]             │
│                                                         │
│  ─────────────────────────────────────────────────────  │
│                                                         │
│  [Additional licenses as they are added]                │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### Route

- `/licenses` - Public, no authentication required

---

## Implementation Plan

### Phase 1: Licenses Page + Footer Link + Collapsible Sections ✅

**Objective:** Create the licenses page, footer link, and collapsible sections in one pass

**Tasks:**
- [x] Create `Licenses.razor` at route `/licenses`
- [x] Include license content from `THIRD-PARTY-LICENSES.md` as structured Razor markup
- [x] Style the page consistently with the rest of the app (scoped CSS)
- [x] Make the page publicly accessible (no auth) via `IsPublicRoute` in `App.razor`
- [x] Add "Open Source Licenses" link to footer in `MainLayout.razor`
- [x] Add collapsible `<details>` sections for full license text
- [x] Show license name, type badge, and source link by default

---

## Testing Strategy

### Automated Tests (7 bUnit tests — all passing)

- [x] LicensesPage renders without errors
- [x] Page title contains "Open Source Licenses"
- [x] Lucide Icons license entry is displayed
- [x] ISC License type badge is shown
- [x] Collapsible license text section works
- [x] Intro text is present
- [x] Source link points to correct GitHub URL

### Manual Testing Checklist

- [ ] Navigate to `/licenses` and verify content displays
- [ ] Verify page is accessible without login
- [ ] Check footer link works from various pages
- [ ] Verify content matches `THIRD-PARTY-LICENSES.md`
- [ ] Test on mobile devices

---

## Migration Notes

- None (new feature)

---

## Security Considerations

- Page is publicly accessible (no sensitive data)
- No Markdown rendering / no XSS risk — license text is static Razor markup

---

## Performance Considerations

- License content is static and can be cached
- Consider lazy-loading full license texts if page becomes large

---

## Future Enhancements

- Auto-generate license information from NuGet packages
- Include .NET runtime and Blazor framework licenses
- Add license compliance checking to CI/CD
- Version information for each dependency

---

## References

- THIRD-PARTY-LICENSES.md in repository root
- Lucide Icons: https://lucide.dev/
- ISC License: https://opensource.org/licenses/ISC

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-02-01 | Initial draft | @github-copilot |
| 2026-02-18 | Implementation complete (page, footer, tests, public route) | @github-copilot |
