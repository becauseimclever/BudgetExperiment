# Feature 046: Accessible Theme and WCAG 2.0 UI Tests
> **Status:** 🗒️ Planning  
> **Priority:** High  
> **Dependencies:** Feature 044 (Theming), Feature 045 (Component Refactor)

## Overview

Design and implement a new high-contrast accessible UI theme and add automated accessibility tests using axe-core to ensure WCAG 2.0 AA compliance. Ensure all UI components are fully accessible via keyboard and screen readers. This builds on the theming infrastructure from Feature 044 and component patterns from Feature 045.

## Problem Statement

The current UI may not fully meet accessibility standards (WCAG 2.0 AA), and there is no dedicated high-contrast accessible theme. Automated accessibility tests are not in place to catch regressions. Users with disabilities—including those with visual impairments, motor disabilities, or using assistive technologies—may have difficulty using the app.

### Current State

**✅ Already Working:**
- 9 themes available with CSS variable-based theming (from Feature 044)
- `prefers-color-scheme` detection for system theme
- `prefers-reduced-motion` respected in reset.css and index.html
- Some ARIA attributes present in components (ThemeToggle, Icon, Modal, FormField)
- Focus ring CSS variable defined (`--focus-ring`)

**⚠️ Gaps Identified:**
- No dedicated high-contrast accessible theme
- No detection of `forced-colors`, `prefers-contrast: more`, or Windows High Contrast Mode
- No automated accessibility testing (axe-core not integrated)
- Inconsistent ARIA labeling across components
- Some interactive elements missing accessible names
- Skip-to-main-content link not implemented
- Focus management in modals/dialogs may not trap focus correctly
- Keyboard navigation not systematically verified

### Target State

- **High-contrast accessible theme** available and auto-applied when accessibility preferences detected
- **Automated axe-core tests** verify WCAG 2.0 AA compliance on key pages
- **All UI navigable** by keyboard with visible focus indicators
- **Screen reader compatible** with proper ARIA landmarks, roles, and labels
- **CI integration** blocks accessibility regressions

---

## User Stories

### Theme & Detection

#### US-046-001: High-contrast accessible theme
**As a** user with low vision or visual impairments  
**I want to** have a high-contrast theme with larger text and clear visual boundaries  
**So that** I can easily read and interact with the application

**Acceptance Criteria:**
- [ ] High-contrast theme (`accessible`) added to theme list
- [ ] Color contrast ratios meet WCAG 2.0 AA (4.5:1 for text, 3:1 for large text/UI)
- [ ] Focus indicators are highly visible (minimum 3px solid outline)
- [ ] Interactive elements have clear boundaries (not relying on color alone)
- [ ] Font sizes are slightly larger than default themes
- [ ] Theme icon clearly indicates accessibility purpose (e.g., `accessibility` or `eye`)

#### US-046-002: Auto-detect accessibility preferences
**As a** user who has enabled high-contrast or accessibility settings in my OS/browser  
**I want** the app to automatically apply the accessible theme  
**So that** I get an optimal experience without manual configuration

**Acceptance Criteria:**
- [ ] Detect `forced-colors: active` (Windows High Contrast Mode)
- [ ] Detect `prefers-contrast: more` (user prefers increased contrast)
- [ ] Auto-apply accessible theme when detected (unless user has explicit override)
- [ ] User can override and select any theme regardless of detected preferences
- [ ] Override persists in localStorage and takes precedence on future visits
- [ ] Display subtle indicator if accessible theme was auto-applied

### Keyboard & Screen Reader

#### US-046-003: Complete keyboard navigation
**As a** user who relies on keyboard navigation  
**I want** to access all features using only the keyboard  
**So that** I can use the app without a mouse

**Acceptance Criteria:**
- [ ] All interactive elements are focusable via Tab/Shift+Tab
- [ ] Focus order follows logical reading order (top-to-bottom, left-to-right)
- [ ] Focus is visible with high-contrast focus ring on all themes
- [ ] Escape closes modals and dropdowns
- [ ] Enter/Space activates buttons and links
- [ ] Arrow keys navigate within components (dropdowns, date pickers, calendars)
- [ ] Skip-to-main-content link implemented

#### US-046-004: Screen reader compatibility
**As a** user who relies on a screen reader  
**I want** all content to be properly announced  
**So that** I can understand and interact with the UI

**Acceptance Criteria:**
- [ ] All pages have proper ARIA landmarks (`<main>`, `<nav>`, `<header>`, etc.)
- [ ] All images have alt text (or `aria-hidden="true"` if decorative)
- [ ] All form inputs have associated labels (`<label for>` or `aria-label`)
- [ ] All buttons have accessible names
- [ ] Dynamic content changes announced via `aria-live` regions
- [ ] Modal focus is trapped and return focus on close
- [ ] Error messages associated with inputs via `aria-describedby`

### Automated Testing

#### US-046-005: Automated accessibility tests with axe-core
**As a** developer  
**I want** automated accessibility tests to run on every PR  
**So that** accessibility regressions are caught before merge

**Acceptance Criteria:**
- [ ] axe-core integrated with Playwright E2E tests (via `Deque.AxeCore.Playwright` NuGet)
- [ ] Tests scan key pages: Home/Calendar, Accounts, Categories, Transactions, Settings
- [ ] Tests configured for WCAG 2.0 AA rules (`wcag2a`, `wcag2aa`)
- [ ] Violations cause test failures (with clear error messages)
- [ ] Test results include violation details for debugging
- [ ] Tests run in CI pipeline (GitHub Actions)

---

## Technical Design

### Architecture Changes

```
┌─────────────────────────────────────────────────────────────────┐
│  theme.js (Enhanced)                                            │
│    ├── detectAccessibilityPreferences(): boolean                │
│    │     └── Checks forced-colors, prefers-contrast             │
│    ├── getEffectiveTheme(): Considers a11y prefs + override     │
│    └── setThemeOverride(): Explicit user preference             │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  ThemeService.cs (Enhanced)                                     │
│    ├── IsAccessibilityPreferenceDetected: bool                  │
│    ├── WasThemeAutoApplied: bool                                │
│    └── SetExplicitThemeOverrideAsync(): User override           │
└─────────────────────────────────────────────────────────────────┘
```

### New Files

| File | Description |
|------|-------------|
| `wwwroot/css/themes/accessible.css` | High-contrast accessible theme |
| `tests/BudgetExperiment.E2E.Tests/Helpers/AccessibilityHelper.cs` | axe-core test utilities |
| `tests/BudgetExperiment.E2E.Tests/Tests/AccessibilityTests.cs` | WCAG compliance tests |
| `docs/ACCESSIBILITY.md` | Accessibility guidelines and patterns |

### Modified Files

| File | Changes |
|------|---------|
| `wwwroot/js/theme.js` | Add accessibility preference detection |
| `Services/ThemeService.cs` | Add `IsAccessibilityPreferenceDetected` property |
| `wwwroot/css/app.css` | Import accessible theme |
| `Components/Common/*.razor` | Add missing ARIA attributes |
| `Shared/MainLayout.razor` | Add skip-link and ARIA landmarks |

### Accessible Theme Specification

**Theme: `accessible`**
- **Purpose:** High contrast for users with low vision
- **Icon:** `accessibility` (Lucide icon)

**Color Palette (WCAG 2.0 AA compliant):**
```css
/* Backgrounds */
--color-background: #ffffff;
--color-surface: #ffffff;
--color-surface-secondary: #f8f8f8;

/* Text - minimum 7:1 contrast ratio (AAA) */
--color-text-primary: #000000;
--color-text-secondary: #333333;

/* Borders - clear visual boundaries */
--color-border: #000000;
--color-border-subtle: #666666;

/* Brand/Action - 4.5:1 minimum on white */
--color-brand-primary: #0000cc;  /* Blue - 8.6:1 */
--color-brand-primary-hover: #000099;

/* Semantic - all meet 4.5:1 */
--color-success: #006600;  /* Dark green */
--color-error: #cc0000;    /* Dark red */
--color-warning: #664400;  /* Dark amber */

/* Focus - highly visible */
--focus-ring: 0 0 0 3px #000000, 0 0 0 5px #ffffff;
```

### Accessibility Preference Detection (theme.js)

```javascript
/**
 * Detects if user has accessibility preferences enabled.
 * @returns {boolean} True if accessibility theme should be auto-applied.
 */
export function detectAccessibilityPreferences() {
    // Windows High Contrast Mode
    if (window.matchMedia('(forced-colors: active)').matches) {
        return true;
    }
    // User prefers more contrast
    if (window.matchMedia('(prefers-contrast: more)').matches) {
        return true;
    }
    return false;
}

/**
 * Gets effective theme considering accessibility preferences.
 * @returns {string} Theme to apply.
 */
export function getEffectiveTheme() {
    const savedTheme = localStorage.getItem(STORAGE_KEY);
    const hasExplicitOverride = localStorage.getItem(OVERRIDE_KEY) === 'true';
    
    // If user explicitly chose a theme, respect it
    if (hasExplicitOverride && savedTheme) {
        return savedTheme;
    }
    
    // Auto-apply accessible theme if preferences detected
    if (detectAccessibilityPreferences()) {
        return 'accessible';
    }
    
    // Fall back to saved or system theme
    return savedTheme || 'system';
}
```

### axe-core Integration

**Package:** `Deque.AxeCore.Playwright` (NuGet)

```csharp
// AccessibilityHelper.cs
public static class AccessibilityHelper
{
    public static async Task<AxeResult> AnalyzePageAsync(
        IPage page,
        string[]? include = null,
        string[]? exclude = null)
    {
        var axe = new AxeBuilder(page)
            .WithTags("wcag2a", "wcag2aa", "wcag21a", "wcag21aa");
        
        if (include != null)
            axe.Include(include);
        if (exclude != null)
            axe.Exclude(exclude);
            
        return await axe.AnalyzeAsync();
    }
    
    public static void AssertNoViolations(AxeResult result)
    {
        if (result.Violations.Any())
        {
            var message = FormatViolations(result.Violations);
            throw new XunitException($"Accessibility violations found:\n{message}");
        }
    }
}
```

### Domain Model

No domain model changes required.

### Database Changes

None required.

---

## Implementation Plan

### Phase 1: High-Contrast Accessible Theme
> **Commit:** `feat(client): add high-contrast accessible theme`

**Objective:** Create and integrate the accessible theme

**Tasks:**
- [ ] Create `wwwroot/css/themes/accessible.css` with WCAG AA colors
- [ ] Add theme to `wwwroot/css/app.css` imports
- [ ] Register theme in `ThemeService.cs` AvailableThemes
- [ ] Add theme color to `theme.js` meta theme-color map
- [ ] Test all color combinations meet 4.5:1 contrast ratio
- [ ] Verify theme in theme dropdown

**Validation:**
- Use browser DevTools or WebAIM Contrast Checker to verify ratios
- Test with Windows High Contrast Mode enabled

---

### Phase 2: Accessibility Preference Detection
> **Commit:** `feat(client): auto-detect accessibility preferences and apply theme`

**Objective:** Auto-apply accessible theme when OS/browser preferences indicate need

**Tasks:**
- [ ] Add `detectAccessibilityPreferences()` to `theme.js`
- [ ] Add `getEffectiveTheme()` logic considering preferences
- [ ] Add explicit override storage key (`budget-experiment-theme-override`)
- [ ] Update `ThemeService.cs` to expose detection state
- [ ] Add `setExplicitOverride()` function for user choice
- [ ] Test with Chrome DevTools emulation (Rendering > Emulate CSS media feature)

**Validation:**
- Emulate `prefers-contrast: more` → accessible theme auto-applied
- Emulate `forced-colors: active` → accessible theme auto-applied
- User selects different theme → preference persisted, override flag set
- Next visit respects user override, not auto-detection

---

### Phase 3: Component Accessibility Audit & Fixes
> **Commit:** `refactor(client): improve component accessibility (ARIA, focus, keyboard)`

**Objective:** Ensure all components meet accessibility requirements

**Tasks:**
- [ ] Add skip-to-main-content link in `MainLayout.razor`
- [ ] Add ARIA landmarks (`<main>`, `<nav role="navigation">`, etc.)
- [ ] Audit and fix Modal.razor focus trapping
- [ ] Audit ThemeToggle.razor keyboard navigation (arrow keys in dropdown)
- [ ] Audit all form components for label associations
- [ ] Add `aria-live="polite"` regions for dynamic content (loading states, errors)
- [ ] Ensure all Icon components have proper `aria-hidden` or `aria-label`
- [ ] Add `aria-describedby` for form validation messages
- [ ] Test with screen reader (NVDA or VoiceOver)

**Key Components to Audit:**

| Component | Issues to Check |
|-----------|-----------------|
| `Modal.razor` | Focus trap, focus return, Escape to close |
| `ThemeToggle.razor` | Arrow key navigation, role="menu" |
| `Button.razor` | Loading state announcement |
| `FormField.razor` | Error association, required indication |
| `Calendar*.razor` | Arrow key date navigation, announcements |
| `NavMenu.razor` | Current page indication, landmarks |
| `ErrorAlert.razor` | role="alert" for announcements |

---

### Phase 4: Automated Accessibility Tests
> **Commit:** `test(e2e): add axe-core accessibility tests for WCAG 2.0 AA`

**Objective:** Integrate axe-core and create accessibility test suite

**Tasks:**
- [ ] Add `Deque.AxeCore.Playwright` NuGet package to E2E project
- [ ] Create `AccessibilityHelper.cs` with axe utilities
- [ ] Create `AccessibilityTests.cs` with page tests
- [ ] Add tests for each major page/route
- [ ] Configure CI to run accessibility tests
- [ ] Document known issues and exclusions (if any)

**Test Coverage:**

| Page | Route | Notes |
|------|-------|-------|
| Calendar (Home) | `/` | Main interaction surface |
| Accounts | `/accounts` | List and forms |
| Categories | `/categories` | List and forms |
| Recurring Bills | `/recurring` | Complex forms |
| Transfers | `/transfers` | Form with multiple inputs |
| Import | `/import` | File upload, mapping UI |
| Settings | `/settings` | All settings forms |
| Reports | `/reports` | Charts (may need exclusions) |

**Sample Test:**
```csharp
[Fact]
[Trait("Category", "Accessibility")]
public async Task CalendarPage_ShouldHaveNoAccessibilityViolations()
{
    // Arrange
    await fixture.Page.GotoAsync($"{fixture.BaseUrl}/");
    await fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    
    // Act
    var result = await AccessibilityHelper.AnalyzePageAsync(fixture.Page);
    
    // Assert
    AccessibilityHelper.AssertNoViolations(result);
}
```

---

### Phase 5: Documentation & CI Integration
> **Commit:** `docs: add accessibility guide and update CI for a11y tests`

**Objective:** Document accessibility practices and ensure CI enforcement

**Tasks:**
- [ ] Create `docs/ACCESSIBILITY.md` with:
  - WCAG 2.0 AA requirements summary
  - Component accessibility checklist
  - Testing instructions (manual + automated)
  - Known limitations
- [ ] Update `THEMING.md` with accessible theme notes
- [ ] Update `COMPONENT-STANDARDS.md` with accessibility requirements
- [ ] Add accessibility test job to GitHub Actions workflow
- [ ] Ensure accessibility failures block PR merge

---

## Testing Strategy

### Automated Tests (axe-core via Playwright)

| Test | Validates |
|------|-----------|
| Full page scans | WCAG 2.0 AA compliance per page |
| Component-specific | Modal focus trap, form labels |
| Theme variations | Accessible theme color contrast |

**axe-core Rules Enabled:**
- `wcag2a` - WCAG 2.0 Level A
- `wcag2aa` - WCAG 2.0 Level AA
- `wcag21a` - WCAG 2.1 Level A
- `wcag21aa` - WCAG 2.1 Level AA

**Exclusions (if needed):**
- Chart components (SVG accessibility is complex, may need `aria-label` only)
- Third-party embedded content

### Manual Testing Checklist

**Keyboard Navigation:**
- [ ] Tab through entire page - all interactive elements reachable
- [ ] Shift+Tab reverses order correctly
- [ ] Enter/Space activates buttons and links
- [ ] Escape closes modals and dropdowns
- [ ] Arrow keys work in dropdowns and date pickers
- [ ] Focus never gets trapped (except intentionally in modals)

**Screen Reader (NVDA/VoiceOver):**
- [ ] Page title announced on navigation
- [ ] Landmarks navigable (main, nav, header)
- [ ] Form labels read correctly
- [ ] Button purposes clear
- [ ] Error messages announced
- [ ] Loading states announced
- [ ] Dynamic content changes announced

**High Contrast / Accessibility Theme:**
- [ ] All text readable (no light gray on white)
- [ ] Focus indicators highly visible
- [ ] Interactive elements clearly bounded
- [ ] Icons have sufficient contrast
- [ ] No information conveyed by color alone

**Browser Emulation Testing:**
- [ ] Chrome DevTools: Rendering → Emulate `prefers-contrast: more`
- [ ] Chrome DevTools: Rendering → Emulate `forced-colors: active`
- [ ] Firefox: `about:config` → `ui.prefersContrast` → 1

---

## Known Limitations

1. **Chart Accessibility:** SVG-based charts are visually complex. We will provide `aria-label` summaries but full data table alternatives are out of scope for this feature.

2. **Calendar Date Picker:** Full ARIA grid pattern for calendars is complex. Initial implementation will support arrow key navigation but may not announce all date states.

3. **Third-Party Content:** Any embedded third-party widgets are not covered by our accessibility tests.

---

## Dependencies

- **Deque.AxeCore.Playwright** NuGet package (for E2E tests)
- Feature 044 (Theming) - ✅ Complete
- Feature 045 (Component Refactor) - ✅ Complete

---

## Migration Notes

None required. This is an additive feature.

---

## Security Considerations

None. Accessibility features do not introduce security concerns.

---

## Success Metrics

- [ ] Zero critical/serious axe-core violations on main pages
- [ ] Accessible theme passes WCAG 2.0 AA contrast checks
- [ ] All pages navigable via keyboard only
- [ ] Screen reader can navigate and understand all key flows
- [ ] CI blocks PRs with accessibility regressions

---

## Performance Considerations

- Accessibility tests should not significantly slow CI

---

## Future Enhancements

- User-customizable accessibility settings
- Automated screen reader simulation tests

---

## References

- See 044: UI theme rework and theming
- See 045: UI component refactor and library
- WCAG 2.0 guidelines: https://www.w3.org/WAI/standards-guidelines/wcag/

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-01-26 | Initial draft | @github-copilot |
