# Feature 031: Navigation Reorganization & UX Improvements

## Overview

This feature focuses on improving the navigation experience in the Budget Experiment UI. The current sidebar navigation has usability issues including content-dependent stretching, always-visible account sub-items, and unclear link naming. This reorganization will create a fixed, scrollable navigation with collapsible account sections, improved link naming, and enhanced icon-only mode support.

## Problem Statement

### Current State

The existing navigation implementation has several UX limitations:

1. **Sidebar stretches with content**: The sidebar height is tied to the main content area rather than being fixed to viewport height. When content is long, the sidebar becomes difficult to use.

2. **Accounts not collapsible**: Account sub-items are always visible when the nav is expanded, cluttering the navigation for users with many accounts.

3. **Inconsistent link naming**: Some links use technical terms ("Recurring Transfers") while others are abbreviated ("Budget"). Link naming could be more intuitive.

4. **Icon-only mode limitations**: While the collapse/expand functionality exists, the transition and UX could be improved for a better icon-only experience.

5. **Navigation scrolls with page**: The navigation should remain fixed while main content scrolls independently.

### Current Navigation Links

| Current Name | Route | Observations |
|--------------|-------|--------------|
| Calendar | `/` | Clear, but could be "Dashboard" or "Calendar View" |
| Recurring | `/recurring` | Shortened from "Recurring Transactions" - may confuse |
| Recurring Transfers | `/recurring-transfers` | Long, verbose |
| Transfers | `/transfers` | Clear |
| Paycheck Planner | `/paycheck-planner` | Clear, descriptive |
| Categories | `/categories` | Could be "Budget Categories" for clarity |
| Rules | `/rules` | Unclear - "Auto-Rules" or "Categorization" better |
| Budget | `/budget` | Clear |
| Accounts | `/accounts` | Clear - parent for sub-items |
| Import | `/import` | Clear |
| AI Suggestions | `/ai/suggestions` | Clear |
| AI Settings | `/ai/settings` | **Remove** - move to Settings page as a tab/section |
| Settings | `/settings` | Clear - will include AI Settings |

### Target State

After implementation:

1. **Fixed sidebar**: Navigation stays fixed to viewport height; main content scrolls independently
2. **Collapsible accounts**: Account section has expand/collapse toggle, remembers state
3. **Improved link naming**: User-friendly, consistent terminology
4. **Better icon-only mode**: Smooth transitions, tooltips, proper spacing
5. **Consistent visual hierarchy**: Clear grouping of related navigation items

---

## User Stories

### Navigation Usability

#### US-031-001: Fixed Sidebar Navigation
**As a** user  
**I want** the navigation sidebar to stay fixed on screen  
**So that** I can always access navigation links without scrolling back to the top

**Acceptance Criteria:**
- [ ] Sidebar remains fixed to viewport height
- [ ] Main content area scrolls independently
- [ ] Navigation items scroll within sidebar if they exceed viewport height
- [ ] Works correctly on desktop and tablet views

#### US-031-002: Collapsible Accounts Section
**As a** user with multiple accounts  
**I want** to collapse the accounts sub-menu  
**So that** I can reduce clutter in the navigation

**Acceptance Criteria:**
- [ ] Account section has expand/collapse toggle
- [ ] Toggle shows visual indicator (chevron/arrow)
- [ ] Collapsed state hides account sub-items
- [ ] State persists across page navigations (session storage)
- [ ] Works in both expanded and collapsed sidebar modes

#### US-031-003: Collapsible Sidebar to Icons
**As a** user  
**I want** to collapse the sidebar to show only icons  
**So that** I have more screen space for main content

**Acceptance Criteria:**
- [ ] Toggle button collapses/expands sidebar smoothly
- [ ] Icons remain visible and clickable in collapsed state
- [ ] Tooltips appear on hover in collapsed state
- [ ] Transition animation is smooth (300ms)
- [ ] Collapsed state persists across page loads (local storage)

#### US-031-004: Intuitive Link Naming
**As a** user  
**I want** navigation links to have clear, friendly names  
**So that** I can quickly find what I'm looking for

**Acceptance Criteria:**
- [ ] All link names reviewed for clarity
- [ ] Consistent naming convention applied
- [ ] Title attributes match visible text (for accessibility)
- [ ] No jargon or technical terms in user-facing labels

---

## Technical Design

### Proposed Link Naming Changes

| Current | Proposed | Rationale |
|---------|----------|-----------|
| Calendar | Calendar | Keep - clear and recognizable |
| Recurring | Recurring Bills | Clarifies these are recurring expenses/bills |
| Recurring Transfers | Auto-Transfers | Shorter, implies automation |
| Transfers | Transfers | Keep - clear |
| Paycheck Planner | Paycheck Planner | Keep - descriptive |
| Categories | Categories | Keep - understood in budget context |
| Rules | Auto-Categorize | Describes the action, not the object |
| Budget | Budget | Keep - clear |
| Accounts | Accounts | Keep - parent section |
| Import | Import | Keep - clear |
| AI Suggestions | Smart Insights | More user-friendly than "AI" |
| AI Settings | *(removed)* | Moved to Settings page as "AI" tab/section |
| Settings | Settings | Keep - will contain AI settings section |

### Architecture Changes

No new components needed. Modifications to:

1. **MainLayout.razor** - Adjust layout structure for fixed sidebar
2. **NavMenu.razor** - Add collapsible accounts section, update link names
3. **NavMenu.razor.css** - Styles for collapsible section, scrolling
4. **layout.css** - Fix sidebar positioning, main content scrolling

### CSS Layout Changes

```css
/* Fixed sidebar with internal scrolling */
.app-content-wrapper {
    display: flex;
    flex: 1;
    height: calc(100vh - 56px); /* Viewport minus header */
    overflow: hidden;
}

.app-sidebar {
    position: sticky;
    top: 0;
    height: 100%;
    overflow-y: auto;
    overflow-x: hidden;
}

.app-main-content {
    flex: 1;
    overflow-y: auto;
    height: 100%;
}
```

### Component Changes

#### NavMenu.razor - Collapsible Accounts

```razor
@* Accounts section with collapsible sub-items *@
<div class="nav-section">
    <button class="nav-section-toggle" @onclick="ToggleAccountsExpanded">
        <span class="nav-icon"><Icon Name="bank" Size="20" /></span>
        @if (!IsCollapsed)
        {
            <span class="nav-text">Accounts</span>
            <span class="nav-chevron">
                <Icon Name="@(accountsExpanded ? "chevron-down" : "chevron-right")" Size="16" />
            </span>
        }
    </button>
    
    @if (!IsCollapsed && accountsExpanded && accounts.Count > 0)
    {
        <div class="nav-subitems">
            @foreach (var account in accounts)
            {
                <NavLink class="nav-item nav-subitem" href="@($"accounts/{account.Id}/transactions")">
                    <span class="nav-icon"><Icon Name="credit-card" Size="16" /></span>
                    <span class="nav-text">@TruncateName(account.Name)</span>
                </NavLink>
            }
        </div>
    }
</div>
```

### State Persistence

- **Sidebar collapsed state**: Store in `localStorage` for persistence across sessions
- **Accounts expanded state**: Store in `sessionStorage` for per-session memory

### UI Components

| Component | Changes |
|-----------|---------|
| MainLayout.razor | Minor CSS class adjustments |
| NavMenu.razor | Collapsible accounts, updated link names, state management |
| NavMenu.razor.css | Collapsible section styles, scrolling nav-items |
| layout.css | Fixed sidebar, scrolling content area |

---

## Implementation Plan

### Phase 1: Fixed Sidebar Layout

**Objective:** Make the sidebar fixed to viewport height with independent content scrolling

**Tasks:**
- [ ] Update `layout.css` `.app-content-wrapper` to use fixed height calculation
- [ ] Update `.app-sidebar` positioning and overflow
- [ ] Update `.app-main-content` to scroll independently
- [ ] Update `NavMenu.razor.css` `.nav-items` for internal scrolling
- [ ] Test on various viewport sizes
- [ ] Verify mobile responsive behavior

**Commit:**
```bash
git add .
git commit -m "feat(client): implement fixed sidebar layout

- Sidebar now stays fixed to viewport height
- Main content area scrolls independently
- Nav items scroll within sidebar when needed
- Mobile responsive behavior maintained

Refs: #031"
```

---

### Phase 2: Collapsible Accounts Section

**Objective:** Add expand/collapse functionality to the accounts section

**Tasks:**
- [ ] Add `accountsExpanded` state to NavMenu.razor
- [ ] Add toggle button/header for accounts section
- [ ] Add chevron icon indicator
- [ ] Implement `sessionStorage` persistence for expanded state
- [ ] Style collapsible section with transition
- [ ] Ensure works when sidebar is collapsed (icon only)

**Commit:**
```bash
git add .
git commit -m "feat(client): add collapsible accounts section

- Accounts section can be expanded/collapsed
- Visual chevron indicator shows state
- State persists via sessionStorage
- Works in both expanded and collapsed sidebar modes

Refs: #031"
```

---

### Phase 3: Improved Link Naming

**Objective:** Update navigation link names for clarity and consistency

**Tasks:**
- [ ] Update "Recurring" → "Recurring Bills"
- [ ] Update "Recurring Transfers" → "Auto-Transfers"
- [ ] Update "Rules" → "Auto-Categorize"
- [ ] Update "AI Suggestions" → "Smart Insights"
- [ ] Update title attributes to match
- [ ] Verify icon choices still appropriate

**Commit:**
```bash
git add .
git commit -m "feat(client): improve navigation link naming

- Renamed links for clarity and consistency
- Updated title attributes for accessibility
- More user-friendly terminology throughout

Refs: #031"
```

---

### Phase 4: Consolidate AI Settings into Settings Page

**Objective:** Move AI Settings from separate nav item into the Settings page

**Tasks:**
- [ ] Remove AI Settings nav link from NavMenu.razor
- [ ] Add "AI" tab or section to Settings page
- [ ] Move AI settings content into Settings page component
- [ ] Update routing if needed (redirect `/ai/settings` → `/settings?tab=ai`)
- [ ] Remove orphaned AI Settings page component if applicable

**Commit:**
```bash
git add .
git commit -m "feat(client): consolidate AI settings into Settings page

- Removed separate AI Settings nav item
- Added AI section/tab to Settings page
- Simplified navigation structure

Refs: #031"
```

---

### Phase 5: Enhanced Icon-Only Mode

**Objective:** Improve the collapsed sidebar experience

**Tasks:**
- [ ] Add/verify tooltips on all nav items in collapsed mode
- [ ] Ensure smooth width transition (already exists, verify timing)
- [ ] Add `localStorage` persistence for collapsed state
- [ ] Test keyboard navigation in collapsed mode
- [ ] Verify touch targets are adequate size

**Commit:**
```bash
git add .
git commit -m "feat(client): enhance icon-only sidebar mode

- Tooltips visible in collapsed mode
- Collapsed state persists via localStorage
- Smooth transition animations
- Improved touch targets

Refs: #031"
```

---

### Phase 6: Testing & Polish

**Objective:** Ensure all changes work together smoothly

**Tasks:**
- [ ] End-to-end testing of navigation flows
- [ ] Test state persistence (refresh, navigate away/back)
- [ ] Test responsive breakpoints
- [ ] Verify accessibility (keyboard nav, screen reader)
- [ ] Fix any visual glitches or edge cases

**Commit:**
```bash
git add .
git commit -m "test(client): add navigation reorganization tests

- Test collapsible accounts behavior
- Test state persistence
- Test responsive layouts
- Verify accessibility

Refs: #031"
```

---

### Phase 7: Documentation & Cleanup

**Objective:** Final polish and documentation

**Tasks:**
- [ ] Update any relevant component documentation
- [ ] Remove TODO comments
- [ ] Final code review
- [ ] Update CHANGELOG if needed

**Commit:**
```bash
git add .
git commit -m "docs(client): document navigation improvements

- Update component documentation
- Clean up any remaining TODOs

Refs: #031"
```

---

## Testing Strategy

### Unit Tests

- [ ] NavMenu state management (collapsed, accounts expanded)
- [ ] Storage service interactions (localStorage, sessionStorage)
- [ ] Link name rendering

### Integration Tests

- [ ] Navigation state persists across page loads
- [ ] Collapsible sections work with account data
- [ ] Responsive behavior at breakpoints

### Manual Testing Checklist

- [ ] Sidebar stays fixed when scrolling main content
- [ ] Accounts section collapses/expands correctly
- [ ] Collapsed sidebar shows tooltips
- [ ] All links navigate to correct pages
- [ ] State persists after browser refresh
- [ ] Works on mobile viewport
- [ ] Keyboard navigation works
- [ ] No visual glitches during transitions

---

## Security Considerations

No security implications - this is a UI/UX enhancement only.

---

## Performance Considerations

- CSS transitions are GPU-accelerated (transform, opacity preferred)
- Storage operations are synchronous but minimal
- No additional API calls required

---

## Future Enhancements

- Drag-and-drop reordering of navigation items
- Customizable navigation (hide/show sections)
- Favorite/pinned accounts at top
- Navigation search/filter
- Keyboard shortcuts for navigation

---

## References

- [MainLayout.razor](../src/BudgetExperiment.Client/Layout/MainLayout.razor)
- [NavMenu.razor](../src/BudgetExperiment.Client/Components/Navigation/NavMenu.razor)
- [layout.css](../src/BudgetExperiment.Client/wwwroot/css/design-system/layout.css)

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-01-19 | Initial draft | Copilot |
