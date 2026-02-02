# Accessibility Guide

This document provides accessibility guidelines and patterns for Budget Experiment. The project aims to meet **WCAG 2.0 AA** compliance.

## Table of Contents

- [Overview](#overview)
- [Accessible Theme](#accessible-theme)
- [Keyboard Navigation](#keyboard-navigation)
- [Screen Reader Support](#screen-reader-support)
- [Component Patterns](#component-patterns)
- [Testing](#testing)
- [Known Limitations](#known-limitations)

---

## Overview

Budget Experiment implements accessibility features to ensure the application is usable by people with disabilities, including:

- **Visual impairments**: High-contrast themes, proper color contrast ratios, visible focus indicators
- **Motor disabilities**: Full keyboard navigation, no time-based interactions
- **Screen reader users**: Proper ARIA landmarks, labels, and live regions

### WCAG 2.0 AA Requirements Summary

| Criterion | Level | Status |
|-----------|-------|--------|
| 1.1.1 Non-text Content | A | ✅ Icons have aria-hidden or labels |
| 1.3.1 Info and Relationships | A | ✅ Semantic HTML, ARIA landmarks |
| 1.4.3 Contrast (Minimum) | AA | ✅ 4.5:1 for text, 3:1 for UI |
| 2.1.1 Keyboard | A | ✅ All functionality keyboard-accessible |
| 2.1.2 No Keyboard Trap | A | ✅ Focus can move freely |
| 2.4.1 Bypass Blocks | A | ✅ Skip-to-main-content link |
| 2.4.2 Page Titled | A | ✅ Descriptive page titles |
| 2.4.3 Focus Order | A | ✅ Logical focus sequence |
| 2.4.4 Link Purpose | A | ✅ Links have clear purpose |
| 4.1.2 Name, Role, Value | A | ✅ ARIA attributes on components |

---

## Accessible Theme

The **Accessible** theme provides maximum contrast and clarity for users with low vision.

### Enabling the Theme

1. Click the theme toggle button in the header
2. Select "Accessible" from the dropdown

### Auto-Detection

The app automatically applies the Accessible theme when it detects:

- **Windows High Contrast Mode** (`forced-colors: active`)
- **User prefers more contrast** (`prefers-contrast: more`)

Users can override this by manually selecting a different theme. The override persists across sessions.

### Theme Features

| Feature | Value |
|---------|-------|
| Text contrast | 7:1 or higher (AAA) |
| Primary color | #0000cc (8.6:1 on white) |
| Focus ring | 3px solid black with 2px white offset |
| Borders | 2px solid black on interactive elements |
| Font sizes | Slightly larger than default |
| Links | Always underlined |

### Color Palette

```css
/* Text Colors */
--color-text-primary: #000000;    /* 21:1 contrast */
--color-text-secondary: #2d2d2d;  /* 12.6:1 contrast */

/* Semantic Colors (on white background) */
--color-brand-primary: #0000cc;   /* 8.6:1 - Blue */
--color-success: #006600;         /* 8.5:1 - Green */
--color-error: #cc0000;           /* 5.9:1 - Red */
--color-warning: #664400;         /* 7.5:1 - Amber */
```

---

## Keyboard Navigation

### Global Shortcuts

| Key | Action |
|-----|--------|
| Tab | Move focus to next focusable element |
| Shift+Tab | Move focus to previous element |
| Enter/Space | Activate buttons and links |
| Escape | Close modals and dropdowns |

### Skip Link

Press Tab immediately after page load to reveal the "Skip to main content" link. This allows keyboard users to bypass the navigation menu.

### Modal Dialogs

- Focus is moved to the modal when opened
- Escape key closes the modal
- Focus returns to the triggering element on close (when implemented)

### Navigation Menu

- All menu items are focusable
- Collapsible sections have `aria-expanded` to indicate state
- Arrow keys can navigate within the menu

---

## Screen Reader Support

### ARIA Landmarks

The main layout includes proper ARIA landmarks:

```html
<header role="banner">...</header>
<aside role="navigation" aria-label="Main navigation">...</aside>
<main id="main-content" role="main">...</main>
```

### Live Regions

Dynamic content updates use `aria-live` to announce changes:

- **Loading states**: `aria-live="polite"` for loading indicators
- **Error messages**: `role="alert"` for form errors
- **Notifications**: `aria-live="polite"` for success messages

### Icon Accessibility

Icons use the `<Icon>` component which:

- Sets `aria-hidden="true"` by default (decorative icons)
- Accepts a `Title` parameter for meaningful icons that adds `role="img"` and `aria-label`

```razor
<!-- Decorative icon (default) -->
<Icon Name="calendar" Size="20" />

<!-- Meaningful icon with label -->
<Icon Name="warning" Size="20" Title="Warning: Low balance" />
```

### Form Accessibility

All form inputs use the `<FormField>` component which:

- Associates labels with inputs via `id` and `for`
- Shows validation messages with `aria-describedby`
- Indicates required fields with `aria-required`

---

## Component Patterns

### Modal Dialog

```razor
<Modal IsVisible="@showModal" 
       Title="Confirm Action" 
       OnClose="@CloseModal">
    <p>Are you sure?</p>
    <FooterContent>
        <Button OnClick="@Confirm">Yes</Button>
        <Button Variant="ButtonVariant.Secondary" OnClick="@CloseModal">Cancel</Button>
    </FooterContent>
</Modal>
```

The Modal component:
- Has `role="dialog"` and `aria-modal="true"`
- Uses `aria-labelledby` linked to the title
- Handles Escape key to close
- Auto-focuses the dialog on open

### Navigation Links

```razor
<NavLink href="accounts" 
         title="Accounts" 
         aria-label="View all accounts">
    <span class="nav-icon" aria-hidden="true">
        <Icon Name="bank" Size="20" />
    </span>
    <span class="nav-text">Accounts</span>
</NavLink>
```

### Expandable Sections

```razor
<button class="nav-section-toggle"
        @onclick="ToggleSection"
        aria-expanded="@isExpanded"
        aria-controls="section-content">
    Section Title
</button>

@if (isExpanded)
{
    <div id="section-content">
        <!-- Content -->
    </div>
}
```

---

## Testing

### Automated Tests (axe-core)

The project includes automated accessibility tests using axe-core via Playwright:

```bash
# Run all E2E tests including accessibility
dotnet test tests/BudgetExperiment.E2E.Tests --filter "Category=Accessibility"
```

Tests scan each major page for WCAG 2.0 AA violations:

- Calendar (Home)
- Accounts
- Categories
- Recurring Bills
- Transfers
- Import
- Settings
- Reports
- Budget
- Paycheck Planner

### Manual Testing Checklist

#### Keyboard Navigation

- [ ] Tab through entire page - all interactive elements reachable
- [ ] Shift+Tab reverses order correctly
- [ ] Enter/Space activates buttons and links
- [ ] Escape closes modals and dropdowns
- [ ] Focus never gets trapped (except in modals)
- [ ] Focus order follows visual order

#### Screen Reader (NVDA/VoiceOver)

- [ ] Page title announced on navigation
- [ ] Landmarks navigable (main, nav, header)
- [ ] Form labels read correctly
- [ ] Button purposes clear
- [ ] Error messages announced
- [ ] Dynamic content changes announced

#### Visual Testing

- [ ] All text readable (no light gray on white)
- [ ] Focus indicators highly visible
- [ ] Interactive elements clearly bounded
- [ ] No information conveyed by color alone

### Browser DevTools Testing

Emulate accessibility preferences in Chrome DevTools:

1. Open DevTools (F12)
2. Press Ctrl+Shift+P and type "Rendering"
3. Scroll to "Emulate CSS media feature"
4. Select `prefers-contrast: more` or `forced-colors: active`

---

## Known Limitations

1. **Chart Accessibility**: SVG-based charts provide visual data representation. Full data table alternatives are planned for a future release.

2. **Calendar Date Picker**: Complex date selection supports basic arrow key navigation. Full ARIA grid pattern is not yet implemented.

3. **Third-Party Content**: Any embedded third-party widgets may not meet accessibility standards and are excluded from automated tests.

4. **Animations**: The `prefers-reduced-motion` preference is respected in CSS, but some JavaScript-based animations may still occur.

---

## Resources

- [WCAG 2.0 Guidelines](https://www.w3.org/WAI/standards-guidelines/wcag/)
- [axe-core Rules](https://dequeuniversity.com/rules/axe/)
- [MDN ARIA Documentation](https://developer.mozilla.org/en-US/docs/Web/Accessibility/ARIA)
- [WebAIM Contrast Checker](https://webaim.org/resources/contrastchecker/)

---

## Contributing

When adding new features or components:

1. Use semantic HTML elements (`<button>`, `<nav>`, `<main>`, etc.)
2. Add appropriate ARIA attributes for custom widgets
3. Ensure keyboard navigability
4. Test with screen reader (NVDA is free for Windows)
5. Run accessibility tests before submitting PR
6. Follow the [Component Standards](./COMPONENT-STANDARDS.md) for consistency
