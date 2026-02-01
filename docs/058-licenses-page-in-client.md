# Feature 058: Licenses Page in Client Application
> **Status:** 🗒️ Planning

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
- [ ] Licenses page is accessible from the UI (footer or Settings)
- [ ] All third-party licenses are displayed with proper formatting
- [ ] Each license shows: component name, license type, copyright, and full license text
- [ ] Page is accessible without authentication

#### US-058-002: Easy maintenance of license information
**As a** developer  
**I want to** easily add new license entries when adding dependencies  
**So that** the licenses page stays up-to-date

**Acceptance Criteria:**
- [ ] License data is stored in a maintainable format (JSON, Markdown, or code)
- [ ] Adding a new license requires minimal effort
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

### Recommended Approach

**Option A** for initial implementation - render the Markdown file directly. This keeps maintenance simple with a single source of truth.

### Files to Create/Modify

| File | Change |
|------|--------|
| `Pages/LicensesPage.razor` | **NEW** - Page to display licenses |
| `wwwroot/licenses.md` | Copy of THIRD-PARTY-LICENSES.md for client access |
| `Components/Layout/Footer.razor` | Add link to Licenses page |
| `Services/MarkdownService.cs` | **NEW** (optional) - Parse and render Markdown |

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

### Phase 1: Basic Licenses Page

**Objective:** Create a simple page displaying license information

**Tasks:**
- [ ] Create `LicensesPage.razor` at route `/licenses`
- [ ] Copy license content from `THIRD-PARTY-LICENSES.md`
- [ ] Style the page consistently with the rest of the app
- [ ] Make the page publicly accessible (no auth)

**Commit:**
- feat(client): add licenses page with third-party attributions

---

### Phase 2: Footer Link

**Objective:** Make the licenses page discoverable

**Tasks:**
- [ ] Add "Licenses" link to the footer
- [ ] Consider adding to Settings/About section as well
- [ ] Ensure link works on all pages

**Commit:**
- feat(client): add licenses link to footer

---

### Phase 3: Markdown Rendering (Optional Enhancement)

**Objective:** Enable dynamic rendering of THIRD-PARTY-LICENSES.md

**Tasks:**
- [ ] Add Markdown parsing library (e.g., Markdig) or use existing
- [ ] Fetch and render `licenses.md` from wwwroot
- [ ] Ensure proper styling of rendered Markdown

**Commit:**
- feat(client): dynamic markdown rendering for licenses page

---

### Phase 4: Collapsible License Sections (Optional Enhancement)

**Objective:** Improve UX for long license texts

**Tasks:**
- [ ] Add collapsible/expandable sections for each license
- [ ] Show license name and type by default
- [ ] Expand to show full license text on click

**Commit:**
- feat(client): collapsible license sections

---

## Testing Strategy

### Automated Tests

- [ ] LicensesPage renders without errors
- [ ] Page is accessible at `/licenses` route
- [ ] All license entries are displayed

### Manual Testing Checklist

- [ ] Navigate to `/licenses` and verify content displays
- [ ] Verify page is accessible without login
- [ ] Check footer link works from various pages
- [ ] Verify content matches THIRD-PARTY-LICENSES.md
- [ ] Test on mobile devices

---

## Migration Notes

- None (new feature)

---

## Security Considerations

- Page should be publicly accessible (no sensitive data)
- Ensure Markdown rendering (if used) sanitizes HTML to prevent XSS

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
