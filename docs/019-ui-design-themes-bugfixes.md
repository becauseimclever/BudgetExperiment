# Feature 019: UI Design, Themes & Bug Fixes

## Overview

Revisit the application's UI design, color scheme, and theming system to ensure visual consistency, address accessibility concerns, and fix multiple UI-related bugs discovered during usage. This document serves as a comprehensive audit and improvement plan for the client-side user experience.

## Goals

1. **Visual Consistency**: Ensure all components follow the established design system
2. **Theme Improvements**: Enhance light/dark mode support and color palette
3. **Bug Fixes**: Address reported UI bugs and inconsistencies
4. **Accessibility**: Verify and improve color contrast and interactive element accessibility
5. **Polish**: Improve overall visual polish and professional appearance

---

## Current State Analysis

### Existing Design System
The application has a design system established in [011-design-system.md](011-design-system.md) with:
- CSS custom properties (tokens) for theming
- Light and dark theme support
- Component-specific styles
- Utility classes

### Areas Requiring Review
1. **Color Palette**: Review current color choices for consistency and accessibility
2. **Theme Toggle**: Verify theme switching works correctly across all components
3. **Component Styles**: Audit each component for design system compliance
4. **Responsive Design**: Ensure mobile and tablet layouts function correctly
5. **Interactive States**: Verify hover, focus, and active states are consistent

---

## Known Bugs & Issues

### Bug Tracking

| ID | Component | Description | Severity | Status |
|----|-----------|-------------|----------|--------|
| BUG-001 | AccountTransactions | Column headers don't align with data columns in the transaction list | Medium | Open |
| BUG-002 | Theme Dropdown | Theme selector dropdown is transparent and unreadable in most contexts | High | Open |
| BUG-003 | Calendar Page | White background persists in dark mode (calendar itself is dark but page background is white) | High | Open |

---

## User Stories

### US-001: Consistent Color Usage
**As a** user  
**I want** colors to be used consistently throughout the application  
**So that** the interface feels cohesive and professional

### US-002: Improved Theme Switching
**As a** user  
**I want** seamless theme switching without visual glitches  
**So that** I can change themes without disrupting my workflow

### US-003: Accessible Color Contrast
**As a** user with visual impairments  
**I want** all text and interactive elements to have sufficient contrast  
**So that** I can read and interact with the application comfortably

### US-004: Visual Feedback on Interactions
**As a** user  
**I want** clear visual feedback when I hover, click, or focus on elements  
**So that** I know the interface is responding to my actions

### US-005: Mobile-Friendly UI
**As a** mobile user  
**I want** the application to be fully functional on smaller screens  
**So that** I can manage my budget on the go

### US-006: Loading States
**As a** user  
**I want** clear loading indicators during data fetches  
**So that** I know the application is working

### US-007: Error State Styling
**As a** user  
**I want** errors to be clearly visible and distinguishable  
**So that** I can quickly identify and address issues

---

## Design Audit Checklist

### Global Elements
- [ ] Navigation menu styling
- [ ] Header/branding consistency
- [ ] Footer (if applicable)
- [ ] Page layout structure
- [ ] Typography hierarchy
- [ ] Icon usage and consistency

### Theme System
- [ ] Light theme color palette review
- [ ] Dark theme color palette review
- [ ] Theme toggle functionality
- [ ] Theme persistence (localStorage)
- [ ] System preference detection
- [ ] No flash of wrong theme on load

### Components Audit
- [ ] Buttons (primary, secondary, danger, ghost)
- [ ] Form inputs (text, number, date, select)
- [ ] Cards and panels
- [ ] Tables and data grids
- [ ] Modals and dialogs
- [ ] Alerts and notifications
- [ ] Badges and tags
- [ ] Tooltips
- [ ] Dropdowns and menus

### Pages Audit
- [ ] Calendar page
- [ ] Accounts page
- [ ] Account Transactions page
- [ ] Recurring Transactions page
- [ ] Settings page
- [ ] Any other pages

### Accessibility
- [ ] Color contrast ratios (WCAG AA minimum)
- [ ] Focus indicators visible
- [ ] Keyboard navigation works
- [ ] Screen reader compatibility
- [ ] Touch targets adequate size (44x44px minimum)

### Responsive Design
- [ ] Mobile layout (< 768px)
- [ ] Tablet layout (768px - 1024px)
- [ ] Desktop layout (> 1024px)
- [ ] Navigation collapse/expand on mobile
- [ ] Table responsiveness
- [ ] Modal sizing on small screens

---

## Color Palette Review

### Current Palette
*Document the current CSS custom properties and their values here after audit.*

```css
/* Example structure - to be filled in */
:root {
  /* Primary */
  --color-primary: #???;
  --color-primary-hover: #???;
  --color-primary-active: #???;
  
  /* Semantic */
  --color-success: #???;
  --color-warning: #???;
  --color-danger: #???;
  --color-info: #???;
  
  /* Neutral */
  --color-background: #???;
  --color-surface: #???;
  --color-text: #???;
  --color-text-muted: #???;
  --color-border: #???;
}
```

### Proposed Changes
*Document any proposed color changes after review.*

---

## Implementation Tasks

### Phase 1: Audit & Discovery
- [ ] Complete design audit checklist
- [ ] Document all bugs found
- [ ] Screenshot current state of each page/theme
- [ ] Review CSS files for inconsistencies
- [ ] Test on multiple screen sizes

### Phase 2: Bug Fixes
- [ ] Address high-severity bugs
- [ ] Address medium-severity bugs
- [ ] Address low-severity bugs
- [ ] Verify fixes don't introduce regressions

### Phase 3: Design Improvements
- [ ] Update color palette if needed
- [ ] Improve component consistency
- [ ] Enhance interactive states
- [ ] Add missing loading states
- [ ] Improve error state styling

### Phase 4: Accessibility
- [ ] Fix any contrast issues
- [ ] Ensure focus indicators are visible
- [ ] Test keyboard navigation
- [ ] Add ARIA attributes where needed

### Phase 5: Testing & Polish
- [ ] Cross-browser testing (Chrome, Firefox, Edge, Safari)
- [ ] Mobile device testing
- [ ] Theme switching testing
- [ ] Final visual review

---

## Technical Notes

### Files to Review
- `src/BudgetExperiment.Client/wwwroot/css/` - All CSS files
- `src/BudgetExperiment.Client/Layout/` - Layout components
- `src/BudgetExperiment.Client/Pages/` - All page components
- `src/BudgetExperiment.Client/Components/` - Shared components
- `src/BudgetExperiment.Client/wwwroot/index.html` - CSS imports

### CSS Architecture
The design system uses:
- CSS custom properties (variables) for theming
- Component-scoped styles where appropriate
- Utility classes for common patterns
- Mobile-first responsive approach

### Theme Implementation
- Theme preference stored in localStorage
- JavaScript detects system preference on first load
- CSS custom properties swap values based on `data-theme` attribute
- No FOUC (Flash of Unstyled Content) handling

---

## Testing Strategy

### Manual Testing
1. Visual inspection of each page in both themes
2. Interaction testing (hover, focus, click states)
3. Responsive testing using browser DevTools
4. Real device testing on mobile/tablet

### Automated Testing (Optional)
- Consider visual regression testing tools
- Accessibility auditing with Lighthouse or axe

---

## Success Criteria

1. All identified bugs are fixed and verified
2. Design audit checklist is 100% complete
3. Color contrast meets WCAG AA standards
4. Theme switching works without visual glitches
5. Application is fully functional on mobile devices
6. Visual consistency across all pages and components

---

## Notes & Decisions

*Document any design decisions, trade-offs, or discussions here as the feature progresses.*

---

## References

- [011-design-system.md](011-design-system.md) - Original design system documentation
- [013-css-consolidation.md](013-css-consolidation.md) - CSS consolidation efforts
- [WCAG 2.1 Guidelines](https://www.w3.org/WAI/WCAG21/quickref/) - Accessibility standards
