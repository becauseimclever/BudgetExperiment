# Feature 044: UI Theme Rework and Theming Improvements

## Overview

Revisit and improve the UI theme system. Ensure the Theme dropdown is always styled for visibility with the selected theme (fixing transparency/contrast issues). Refactor theming to make it easy to add new themes in the future. This feature does not include a UI theme builder, but should lay the groundwork for it.

## Problem Statement

The current Theme dropdown sometimes displays as transparent or unreadable in certain themes. Adding new themes is cumbersome and error-prone. The theming system should be robust, maintainable, and extensible for future enhancements.

### Current State

- Theme dropdown may be transparent or have poor contrast in some themes
- Theming is not modular; adding new themes is difficult
- No clear structure for future theme expansion

### Target State

- Theme dropdown is always visible and styled correctly for the selected theme
- Theming system is modular and easy to extend
- Adding new themes requires minimal effort and no duplication
- Code is structured to support a future UI theme builder

---

## User Stories

### Theme Dropdown and Theming

#### US-044-001: Theme dropdown is always visible
**As a** user  
**I want to** see the Theme dropdown clearly in any theme  
**So that** I can easily change themes without UI issues

**Acceptance Criteria:**
- [ ] Theme dropdown is styled for visibility and contrast in all themes
- [ ] No transparency or unreadable text in any theme

#### US-044-002: Easy to add new themes
**As a** developer  
**I want to** add new themes with minimal effort  
**So that** the app can support more customization in the future

**Acceptance Criteria:**
- [ ] Theming system is modular and supports new themes easily
- [ ] Adding a theme does not require duplicating code or styles
- [ ] Theme structure is documented for future UI theme builder

---

## Technical Design

### Architecture Changes

- Refactor theme system to use modular, composable theme definitions (e.g., CSS variables, theme objects)
- Ensure Theme dropdown uses theme-aware styles for background, border, and text
- Document theme structure and extension points
- Prepare codebase for future UI theme builder (but do not implement it yet)

### Domain Model

- No changes required

### API Endpoints

- No changes required

### Database Changes

- No changes required

### UI Components

- Theme dropdown: ensure proper styling and theme awareness
- Theme provider/context: refactor for modularity and extensibility

---

## Implementation Plan

### Phase 1: Theme dropdown styling fix

**Objective:** Ensure Theme dropdown is always visible and styled correctly

**Tasks:**
- [ ] Audit current dropdown styles in all themes
- [ ] Fix background, border, and text color issues
- [ ] Test in all supported themes

**Commit:**
- fix(client): theme dropdown visibility and contrast

---

### Phase 2: Modular theming system

**Objective:** Refactor theming for easy extension

**Tasks:**
- [ ] Refactor theme definitions to be modular (e.g., CSS variables, theme objects)
- [ ] Document how to add a new theme
- [ ] Ensure all UI components use theme-aware styles

**Commit:**
- refactor(client): modular theming system

---

### Phase 3: Documentation and cleanup

**Objective:** Document theming structure and prepare for future builder

**Tasks:**
- [ ] Update theming documentation
- [ ] Add notes for future UI theme builder
- [ ] Final review and cleanup

**Commit:**
- docs: document theming improvements

---

## Testing Strategy

### Unit/Integration Tests

- [ ] Theme dropdown renders correctly in all themes
- [ ] Adding a new theme works as documented

### Manual Testing Checklist

- [ ] Switch between all themes and verify dropdown visibility
- [ ] Add a new theme and verify it appears and works

---

## Migration Notes

- None

---

## Security Considerations

- None

---

## Performance Considerations

- No significant impact expected

---

## Future Enhancements

- UI theme builder for custom themes
- User-specific theme preferences

---

## References

- Related: UI theming, dropdown styling

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-01-26 | Initial draft | @github-copilot |
