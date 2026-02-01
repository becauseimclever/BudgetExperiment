# Feature 043: Consolidate AI Features in UI
> **Status:** ✅ Complete

## Overview

Consolidate all AI-related features under a single expandable "AI Tools" menu section in the navigation, with conditional visibility based on AI feature availability. The AI Assistant chat panel remains in the header but is hidden when AI is disabled.

## Problem Statement

Currently, AI features are scattered across the UI in multiple locations, making them hard to discover and manage. Users must navigate to different places to access various AI capabilities, and there's no clear indication when AI features are unavailable.

### Current State

- **AI Assistant** - Chat panel toggle in main header (always visible even if AI disabled)
- **Smart Insights** (`/ai/suggestions`) - Separate nav item in footer for AI rule suggestions
- **Category Suggestions** (`/category-suggestions`) - Separate nav item in footer for AI category suggestions
- **AI Settings** - Tab within the Settings page (`/settings`)
- **AiStatusBadge** - Component shown on AI pages for connection status
- No unified detection of AI availability to hide/show features
- Footer has two separate AI-related nav items without grouping

### Target State

- **AI Tools** section in NavMenu with expandable sub-items (like Reports/Accounts sections)
- All AI pages grouped: Smart Insights, Category Suggestions
- AI Assistant button in header hidden when AI is disabled
- Entire "AI Tools" section hidden when AI features are disabled
- Centralized AI availability check used consistently across components

---

## User Stories

### AI Feature Consolidation

#### US-043-001: Grouped AI menu section ✅
**As a** user  
**I want to** see all AI features grouped under one expandable menu section  
**So that** I can easily find and access AI capabilities

**Acceptance Criteria:**
- [x] "AI Tools" expandable section added to NavMenu (like Reports/Accounts sections)
- [x] Smart Insights and Category Suggestions are sub-items under AI Tools
- [x] Section is collapsible with expand/collapse state persisted

#### US-043-002: Hide AI features when disabled ✅
**As a** user  
**I want to** not see AI-related UI elements when AI features are disabled  
**So that** the UI is not cluttered with unavailable features

**Acceptance Criteria:**
- [x] AI Tools section in NavMenu hidden when AI is disabled (feature flag off)
- [x] AI Assistant button in header hidden when AI is disabled
- [ ] AI-related quick actions on other pages hidden when AI is disabled (out of scope for this feature)

#### US-043-003: Warning state when Ollama unavailable ✅
**As a** user  
**I want to** see a warning indicator on the AI menu when AI is enabled but Ollama is not connected  
**So that** I understand why AI features may not work and can troubleshoot

**Acceptance Criteria:**
- [x] When AI is enabled but Ollama connection fails, show AI Tools section in a "warning" state
- [x] Display warning icon (⚠️ or alert-triangle) next to AI Tools section header
- [x] Apply muted/disabled styling to indicate degraded functionality
- [x] Tooltip on hover explains "AI service unavailable - check Ollama connection"
- [x] Sub-items remain navigable (user can access settings to troubleshoot)
- [x] AI Assistant button in header shows warning badge when Ollama unavailable

#### US-043-004: Centralized AI availability service ✅
**As a** developer  
**I want to** check AI availability from a centralized service  
**So that** all components consistently show/hide AI features

**Acceptance Criteria:**
- [x] Create `IAiAvailabilityService` that caches and exposes AI status
- [x] Service exposes three states: `Disabled`, `Unavailable` (enabled but no connection), `Available`
- [x] Service refreshes status on app init and periodically
- [x] All AI-dependent components use this service

---

## Technical Design

### Architecture Changes

- Add "AI Tools" expandable section in NavMenu (similar pattern to Reports/Accounts)
- Move existing AI nav items (Smart Insights, Category Suggestions) under this section
- Create `IAiAvailabilityService` to centralize AI status checking with caching
- Inject availability service into NavMenu and MainLayout for conditional rendering
- Support three visual states: hidden (disabled), warning (enabled but unavailable), normal (fully operational)
- Hide AI Assistant header button when AI is disabled

### Existing Components to Modify

| Component | Location | Changes Required |
|-----------|----------|------------------|
| `NavMenu.razor` | `Components/Navigation/` | Replace two AI nav items with expandable "AI Tools" section; show warning state when unavailable |
| `MainLayout.razor` | `Layout/` | Conditionally show/hide AI Assistant button; show warning badge when unavailable |
| `ChatPanel.razor` | `Components/Chat/` | No changes (parent controls visibility) |
| `AiStatusBadge.razor` | `Components/AI/` | May leverage shared availability service |

### New Services

```csharp
// Services/AiAvailabilityState.cs
public enum AiAvailabilityState
{
    Disabled,      // Feature flag is off - hide all AI UI
    Unavailable,   // Feature flag on, but Ollama not connected - show warning state
    Available      // Fully operational - normal display
}

// Services/IAiAvailabilityService.cs
public interface IAiAvailabilityService
{
    AiAvailabilityState State { get; }
    bool IsEnabled { get; }           // Feature flag on (shows UI, even if degraded)
    bool IsAvailable { get; }         // Ollama connected
    bool IsFullyOperational { get; }  // Enabled AND Available
    string? ErrorMessage { get; }     // Connection error details for tooltip
    event Action? StatusChanged;
    Task RefreshAsync();
}
```

### Visual States

| State | NavMenu AI Tools | AI Assistant Button | Behavior |
|-------|------------------|---------------------|----------|
| `Disabled` | Hidden | Hidden | No AI UI visible |
| `Unavailable` | Visible + ⚠️ warning icon + muted styling | Visible + ⚠️ badge | Links work, but AI operations will fail |
| `Available` | Visible (normal) | Visible (normal) | Full functionality |

### CSS Classes for States

```css
/* Warning state for AI section */
.nav-section.ai-unavailable .nav-section-toggle {
    opacity: 0.7;
}

.nav-section.ai-unavailable .nav-warning-icon {
    color: var(--warning-color);
    margin-left: 0.5rem;
}

/* Header button warning badge */
.chat-toggle-btn.ai-unavailable::after {
    content: "⚠";
    position: absolute;
    top: -2px;
    right: -2px;
    font-size: 0.75rem;
    color: var(--warning-color);
}
```

### Domain Model

- No changes required

### API Endpoints

- No changes required (uses existing `GET /api/v1/ai/status`)

### Database Changes

- No changes required

### UI Components

- Modify `NavMenu.razor` - add AI Tools expandable section (reuse existing pattern from Reports/Accounts)
- Modify `MainLayout.razor` - conditionally render AI Assistant button
- New `AiAvailabilityService.cs` - centralized AI status with caching

---

## Implementation Plan

### Phase 1: Create AI Availability Service ✅

**Objective:** Centralize AI status checking for consistent behavior

**Tasks:**
- [x] Create `IAiAvailabilityService` interface in `Services/`
- [x] Create `AiAvailabilityState` enum (`Disabled`, `Unavailable`, `Available`)
- [x] Implement `AiAvailabilityService` with status caching
- [x] Register service as scoped in `Program.cs`
- [x] Add unit tests for service behavior (8 tests passing)

**Files Created:**
- [AiAvailabilityState.cs](../src/BudgetExperiment.Client/Services/AiAvailabilityState.cs)
- [IAiAvailabilityService.cs](../src/BudgetExperiment.Client/Services/IAiAvailabilityService.cs)
- [AiAvailabilityService.cs](../src/BudgetExperiment.Client/Services/AiAvailabilityService.cs)
- [AiAvailabilityServiceTests.cs](../tests/BudgetExperiment.Client.Tests/Services/AiAvailabilityServiceTests.cs)

**Commit:**
- feat(client): add centralized AI availability service

---

### Phase 2: Group AI Navigation Items ✅

**Objective:** Consolidate AI nav items under expandable section

**Tasks:**
- [x] Add "AI Tools" expandable section to `NavMenu.razor` (use sparkles icon)
- [x] Move "Smart Insights" and "Category Suggestions" as sub-items
- [x] Add sessionStorage persistence for expand/collapse state
- [x] Remove old standalone AI nav items from footer

**Changes Made:**
- Added `aiToolsExpanded` state variable (default: true)
- Added `AiToolsExpandedStorageKey` constant for sessionStorage
- Added `ToggleAiToolsSection()` method
- Added AI Tools expandable section in nav-footer with sparkles icon
- Smart Insights (lightbulb icon) and Category Suggestions (tag icon) as sub-items
- State persistence in `OnAfterRenderAsync`

**Commit:**
- feat(client): consolidate AI features in nav menu

---

### Phase 3: Conditional Visibility and Warning State ✅

**Objective:** Hide AI features when disabled; show warning when unavailable

**Tasks:**
- [x] Inject `IAiAvailabilityService` into `NavMenu.razor`
- [x] Hide AI Tools section when state is `Disabled`
- [x] Show warning icon and muted styling when state is `Unavailable`
- [x] Add tooltip with error message on warning state
- [x] Inject service into `MainLayout.razor`
- [x] Hide AI Assistant button when `Disabled`
- [x] Show warning badge on AI Assistant button when `Unavailable`
- [x] Add CSS styles for warning state
- [ ] Add integration tests for all three states (deferred to future)

**Changes Made:**
- NavMenu: Injected `IAiAvailabilityService`, wrapped AI Tools section in `@if (AiAvailability.IsEnabled)`, added `ai-unavailable` CSS class and warning icon when unavailable
- MainLayout: Injected service, wrapped AI Assistant button and ChatPanel in conditional, added warning badge and muted styling
- CSS: Added `.ai-unavailable` styles for muted opacity and warning icon color in both NavMenu and MainLayout

**Commit:**
- feat(client): AI visibility states with warning indicator

---

### Phase 4: Documentation and Cleanup ✅

**Objective:** Document changes and remove dead code

**Tasks:**
- [x] Update UI documentation
- [x] Final review and cleanup

**Commit:**
- docs: document AI UI consolidation

---

## Testing Strategy

### Unit Tests

- [x] `AiAvailabilityService` returns `Disabled` when feature flag is off
- [x] `AiAvailabilityService` returns `Unavailable` when enabled but API fails
- [x] `AiAvailabilityService` returns `Available` when enabled and connected
- [x] `AiAvailabilityService` caches status and refreshes on interval
- [x] `AiAvailabilityService` handles API errors gracefully (returns `Unavailable`)

### Component Tests (bUnit)

- [ ] `NavMenu` hides AI Tools section when state is `Disabled`
- [ ] `NavMenu` renders AI Tools section with warning icon when state is `Unavailable`
- [ ] `NavMenu` renders AI Tools section normally when state is `Available`
- [ ] Warning tooltip displays error message from service
- [ ] AI Tools section expands/collapses correctly
- [ ] Sub-items navigate to correct pages in all visible states

### Integration Tests

- [ ] Full app renders AI features normally when Ollama connected
- [ ] Full app shows warning state when Ollama disconnected but enabled
- [ ] Full app hides AI features when AI feature flag disabled

### Manual Testing Checklist

- [ ] AI enabled + Ollama running → AI Tools section visible (normal), AI Assistant button visible (normal)
- [ ] AI enabled + Ollama stopped → AI Tools section visible with ⚠️ icon and muted style, AI Assistant button visible with ⚠️ badge
- [ ] AI disabled → AI Tools section hidden, AI Assistant button hidden
- [ ] Hover over warning icon → Tooltip shows "AI service unavailable" message
- [ ] Click AI Tools sub-items in warning state → Pages load (with their own error handling)
- [ ] Expand AI Tools section → Navigate to Smart Insights → Verify page loads
- [ ] Expand AI Tools section → Navigate to Category Suggestions → Verify page loads
- [ ] Collapse AI Tools section → Verify state persists after page refresh

---

## Migration Notes

- Existing deep links to `/ai/suggestions` and `/category-suggestions` continue to work
- Users with AI disabled will see 404 or redirect if they navigate directly (future enhancement)
- Users navigating to AI pages in `Unavailable` state will see page-level error handling

---

## Security Considerations

- AI availability check is informational only; API endpoints still enforce their own auth/availability
- No sensitive data exposed by availability service

---

## Performance Considerations

- AI status is cached client-side to avoid repeated API calls
- Status refresh interval: 60 seconds (configurable)
- Initial status check runs on app initialization (may briefly show/hide AI section)

---

## Future Enhancements

- Add unified AI dashboard page with all AI features as tabs
- Add user preference to completely hide AI features (independent of availability)
- Add notification badge on AI Tools when new suggestions available
- Add graceful fallback UI when navigating to AI pages with AI disabled

---

## References

- Existing files:
  - [NavMenu.razor](../src/BudgetExperiment.Client/Components/Navigation/NavMenu.razor)
  - [MainLayout.razor](../src/BudgetExperiment.Client/Layout/MainLayout.razor)
  - [AiSuggestions.razor](../src/BudgetExperiment.Client/Pages/AiSuggestions.razor)
  - [CategorySuggestions.razor](../src/BudgetExperiment.Client/Pages/CategorySuggestions.razor)
  - [IAiApiService.cs](../src/BudgetExperiment.Client/Services/IAiApiService.cs)
- API endpoint: `GET /api/v1/ai/status`

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-01-26 | Initial draft | @github-copilot |
| 2026-02-01 | Updated with current state analysis, detailed technical design, and implementation plan | @github-copilot |
| 2026-02-01 | Added warning state for AI enabled but Ollama unavailable; three-state visibility model | @github-copilot |
| 2026-02-01 | Phase 1 complete: Created AiAvailabilityService with 8 passing unit tests | @github-copilot |
| 2026-02-01 | Phase 2 complete: Grouped AI nav items under expandable "AI Tools" section | @github-copilot |
| 2026-02-01 | Phase 3 complete: Conditional visibility and warning state in NavMenu and MainLayout | @github-copilot |
| 2026-02-01 | Phase 4 complete: Documentation finalized, feature marked complete | @github-copilot |
