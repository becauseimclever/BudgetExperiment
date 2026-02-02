# Feature 047: Mobile Experience - Calendar, Quick Add, and AI Assistant
> **Status:** 🗒️ Planning  
> **Priority:** High  
> **Dependencies:** Feature 046 (Accessibility), Feature 045 (Component Refactor)

## Overview

Deliver a first-class mobile experience for Budget Experiment users by optimizing the Calendar view for touch navigation, adding a persistent floating action button (FAB) for quick transaction entry, and ensuring the AI Assistant is easily accessible on mobile devices with bottom-sheet presentation.

## Problem Statement

Many users access Budget Experiment from mobile devices. The current UI, while functional, is optimized for desktop workflows, making some interactions cumbersome on smaller screens.

### Current State

**✅ Already Working:**
- Basic responsive breakpoints exist (`768px`, `480px`)
- Calendar grid shrinks on mobile but becomes cramped
- ChatPanel has mobile styles (full-width on small screens)
- Sidebar collapses on mobile (hamburger menu)
- Forms use standard HTML inputs (work on mobile but not optimized)

**⚠️ Gaps Identified:**
- No swipe gesture support for month navigation
- Calendar day cells too small for comfortable touch (60px min-height at 768px)
- No persistent "Quick Add" FAB visible across all pages
- Transaction form requires scrolling with standard field sizes
- AI Assistant button only visible in header (easy to miss on mobile)
- No bottom-sheet pattern for modals on mobile
- No week-view option for mobile calendar
- Month navigation buttons small and close together

### Target State

- **Swipe navigation** on Calendar (left/right to change months)
- **Touch-optimized Calendar** with larger tap targets and optional week view
- **Floating Action Button (FAB)** for Quick Add visible on all mobile screens
- **Bottom-sheet modals** for transaction entry and AI Assistant on mobile
- **Optimized form inputs** with minimum 48px touch targets
- **AI Assistant** accessible via FAB with one tap
- All mobile UI meets **WCAG AA** accessibility standards

---

## User Stories

### Mobile Calendar Navigation

#### US-047-001: Touch-Friendly Calendar View
**As a** mobile user  
**I want to** view the calendar with adequately sized day cells  
**So that** I can easily tap on a specific day without mis-tapping

**Acceptance Criteria:**
- [ ] Calendar day cells have minimum 48x48px touch target on mobile
- [ ] Day numbers are legible (minimum 14px font)
- [ ] Today indicator and selected day are clearly distinguishable
- [ ] Transaction totals remain visible but use compact format

#### US-047-002: Swipe Month Navigation
**As a** mobile user  
**I want to** swipe left/right to navigate between months  
**So that** I can quickly browse my calendar without reaching for buttons

**Acceptance Criteria:**
- [ ] Swipe left advances to next month
- [ ] Swipe right goes to previous month
- [ ] Swipe detection uses minimum 50px threshold to avoid accidental triggers
- [ ] Visual feedback during swipe (subtle parallax or fade)
- [ ] Button navigation still works for accessibility

#### US-047-003: Week View Toggle (Mobile Only)
**As a** mobile user  
**I want to** toggle between month and week view  
**So that** I can see more detail for a single week on my small screen

**Acceptance Criteria:**
- [ ] Toggle visible only on mobile (< 768px)
- [ ] Week view shows 7 days with larger cells and more transaction detail
- [ ] Week advances/decreases by 7 days on swipe
- [ ] Tapping a day in week view shows day detail
- [ ] View preference persists in localStorage

### Quick Add Transaction

#### US-047-004: Floating Action Button
**As a** mobile user  
**I want to** see a persistent "+" button on every screen  
**So that** I can quickly add a transaction from anywhere in the app

**Acceptance Criteria:**
- [ ] FAB positioned bottom-right, 16px from edges
- [ ] FAB uses consistent brand color with 4.5:1 contrast
- [ ] FAB diameter is 56px (standard FAB size)
- [ ] FAB visible on all pages except during active modal
- [ ] FAB has clear "+" icon and accessible label
- [ ] Tapping FAB opens Quick Add bottom sheet

#### US-047-005: Quick Add Bottom Sheet
**As a** mobile user  
**I want to** add a transaction in a bottom sheet  
**So that** I can quickly enter data without full-screen disruption

**Acceptance Criteria:**
- [ ] Bottom sheet slides up from screen bottom
- [ ] Sheet height is 60-80% of viewport (adjustable by drag)
- [ ] Form has large touch targets (min 48px height)
- [ ] Account defaults to most recently used or primary
- [ ] Date defaults to today (can be changed)
- [ ] Sheet dismissible by swipe down or cancel button
- [ ] Submitting closes sheet and shows success toast

#### US-047-006: AI-Assisted Quick Add
**As a** mobile user  
**I want to** use AI to help fill transaction details  
**So that** I can enter transactions faster with minimal typing

**Acceptance Criteria:**
- [ ] Quick Add sheet has "Ask AI" button or text input option
- [ ] User can type natural language: "Coffee at Starbucks $5"
- [ ] AI parses and pre-fills: Description="Coffee at Starbucks", Amount=-5.00, Category=Dining
- [ ] User can review and edit before saving
- [ ] AI errors show graceful fallback to manual entry

### Mobile AI Assistant

#### US-047-007: AI Assistant FAB
**As a** mobile user  
**I want to** access the AI Assistant with one tap  
**So that** I can get help without hunting for the button

**Acceptance Criteria:**
- [ ] Secondary FAB (or split FAB) for AI Assistant
- [ ] AI button uses distinct icon (sparkles/message-circle)
- [ ] Tapping opens AI bottom sheet (not sidebar)
- [ ] Position does not conflict with Quick Add FAB

#### US-047-008: AI Chat Bottom Sheet
**As a** mobile user  
**I want** the AI chat to open as a bottom sheet  
**So that** it feels native to mobile interaction patterns

**Acceptance Criteria:**
- [ ] Bottom sheet opens at 70% height
- [ ] Can be dragged to full-screen
- [ ] Input field stays above keyboard (viewport-fit safe area)
- [ ] Messages scrollable with momentum
- [ ] Sheet closes on swipe down

### Form Optimization

#### US-047-009: Touch-Optimized Transaction Form
**As a** mobile user  
**I want** form fields to be large and easy to tap  
**So that** I can enter transactions without frustration

**Acceptance Criteria:**
- [ ] All form inputs have minimum 48px height
- [ ] Labels are above fields (not inline) on mobile
- [ ] Select dropdowns use native mobile select (better UX)
- [ ] Keyboard type matches field (numeric for amount)
- [ ] Form fits in viewport without excessive scrolling

---

## Technical Design

### Architecture Changes

```
┌─────────────────────────────────────────────────────────────────┐
│  MainLayout.razor                                               │
│    ├── Existing: Header, Sidebar, Main, ChatPanel               │
│    └── NEW: <MobileFab /> (visible only on mobile)              │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  Components/Common/                                             │
│    ├── BottomSheet.razor (new reusable component)               │
│    ├── MobileFab.razor (floating action button)                 │
│    └── SwipeDetector.razor (touch gesture wrapper)              │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  Components/Calendar/                                           │
│    ├── CalendarGrid.razor (add swipe support)                   │
│    ├── CalendarWeekView.razor (new mobile-only view)            │
│    └── CalendarViewToggle.razor (month/week switcher)           │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  wwwroot/js/                                                    │
│    ├── swipe.js (touch gesture detection)                       │
│    └── bottom-sheet.js (sheet drag/dismiss logic)               │
└─────────────────────────────────────────────────────────────────┘
```

### New Files

| File | Description |
|------|-------------|
| `Components/Common/BottomSheet.razor` | Reusable bottom sheet modal for mobile |
| `Components/Common/BottomSheet.razor.css` | Bottom sheet styles with animations |
| `Components/Common/MobileFab.razor` | Floating action button(s) |
| `Components/Common/MobileFab.razor.css` | FAB styles |
| `Components/Common/SwipeContainer.razor` | Wrapper for swipe gesture detection |
| `Components/Calendar/CalendarWeekView.razor` | Mobile-optimized week view |
| `Components/Calendar/CalendarViewToggle.razor` | Month/week toggle component |
| `Components/Forms/QuickAddForm.razor` | Simplified transaction form for mobile |
| `wwwroot/js/swipe.js` | JavaScript for touch gesture detection |
| `wwwroot/js/bottom-sheet.js` | JavaScript for bottom sheet interactions |
| `wwwroot/css/design-system/components/fab.css` | FAB component styles |
| `wwwroot/css/design-system/components/bottom-sheet.css` | Bottom sheet styles |

### Modified Files

| File | Changes |
|------|---------|
| `Layout/MainLayout.razor` | Add `<MobileFab />` component |
| `Pages/Calendar.razor` | Integrate swipe navigation, week view toggle |
| `Components/Calendar/CalendarGrid.razor` | Wrap in SwipeContainer |
| `Components/Calendar/CalendarDay.razor` | Increase touch target sizes |
| `Components/Chat/ChatPanel.razor` | Add bottom sheet mode for mobile |
| `Components/Forms/TransactionForm.razor` | Add mobile-optimized variant |
| `wwwroot/css/app.css` | Import new component CSS files |
| `wwwroot/css/design-system/layout.css` | Add FAB positioning utilities |
| `wwwroot/css/design-system/components/forms.css` | Mobile form optimizations |

### Component Specifications

#### BottomSheet.razor Parameters

```csharp
[Parameter] public bool IsVisible { get; set; }
[Parameter] public EventCallback OnClose { get; set; }
[Parameter] public RenderFragment? ChildContent { get; set; }
[Parameter] public string Title { get; set; } = string.Empty;
[Parameter] public BottomSheetHeight Height { get; set; } = BottomSheetHeight.Medium;
[Parameter] public bool IsDraggable { get; set; } = true;
[Parameter] public bool IsCloseOnSwipeDown { get; set; } = true;

public enum BottomSheetHeight { Small, Medium, Large, FullScreen }
// Small = 40%, Medium = 60%, Large = 80%, FullScreen = 100%
```

#### MobileFab.razor Parameters

```csharp
[Parameter] public EventCallback OnQuickAddClick { get; set; }
[Parameter] public EventCallback OnAiAssistantClick { get; set; }
[Parameter] public bool IsAiEnabled { get; set; }
[Parameter] public bool IsExpanded { get; set; } = false; // Speed dial mode
```

#### SwipeContainer.razor Parameters

```csharp
[Parameter] public RenderFragment? ChildContent { get; set; }
[Parameter] public EventCallback OnSwipeLeft { get; set; }
[Parameter] public EventCallback OnSwipeRight { get; set; }
[Parameter] public EventCallback OnSwipeUp { get; set; }
[Parameter] public EventCallback OnSwipeDown { get; set; }
[Parameter] public int ThresholdPx { get; set; } = 50;
```

### JavaScript Interop

**swipe.js:**
```javascript
export function initSwipeDetection(element, dotNetRef, options) {
    let startX = 0, startY = 0;
    const threshold = options.threshold || 50;
    
    element.addEventListener('touchstart', (e) => {
        startX = e.touches[0].clientX;
        startY = e.touches[0].clientY;
    }, { passive: true });
    
    element.addEventListener('touchend', (e) => {
        const deltaX = e.changedTouches[0].clientX - startX;
        const deltaY = e.changedTouches[0].clientY - startY;
        
        if (Math.abs(deltaX) > Math.abs(deltaY) && Math.abs(deltaX) > threshold) {
            dotNetRef.invokeMethodAsync(deltaX > 0 ? 'OnSwipeRight' : 'OnSwipeLeft');
        } else if (Math.abs(deltaY) > Math.abs(deltaX) && Math.abs(deltaY) > threshold) {
            dotNetRef.invokeMethodAsync(deltaY > 0 ? 'OnSwipeDown' : 'OnSwipeUp');
        }
    }, { passive: true });
}
```

### Mobile Detection

Use CSS media queries for styling; use JavaScript for behavior-specific detection:

```javascript
// In layout or service
export function isMobileViewport() {
    return window.innerWidth <= 768;
}

export function isTouchDevice() {
    return ('ontouchstart' in window) || (navigator.maxTouchPoints > 0);
}
```

### API Endpoints

No new API endpoints required. Existing endpoints:
- `GET /api/v1/calendar/{year}/{month}` - Calendar grid data
- `POST /api/v1/transactions` - Create transaction
- `POST /api/v1/chat` - AI chat messages

---

## Implementation Plan

### Phase 1: Bottom Sheet Component
> **Commit:** `feat(client): add reusable BottomSheet component for mobile`

**Objective:** Create the foundational bottom sheet pattern for mobile interactions

**Tasks:**
- [ ] Create `BottomSheet.razor` component with slide-up animation
- [ ] Create `bottom-sheet.css` with transitions and height variants
- [ ] Implement drag-to-resize functionality
- [ ] Implement swipe-down-to-close gesture
- [ ] Add backdrop overlay with click-to-close
- [ ] Add ARIA attributes for accessibility
- [ ] Write unit tests for component behavior
- [ ] Test on actual mobile devices/emulators

**Validation:**
- Bottom sheet opens smoothly from bottom
- Can be dragged to resize
- Swipe down dismisses sheet
- Focus trapped inside sheet when open

---

### Phase 2: Floating Action Button
> **Commit:** `feat(client): add MobileFab component with Quick Add and AI buttons`

**Objective:** Add persistent FAB for quick transaction entry on mobile

**Tasks:**
- [ ] Create `MobileFab.razor` with Quick Add and optional AI button
- [ ] Create `fab.css` with positioning and animation
- [ ] Show FAB only on mobile (< 768px) using CSS media query
- [ ] Hide FAB when modal/bottom-sheet is open
- [ ] Implement "speed dial" expand pattern for multiple actions
- [ ] Add proper ARIA labels and roles
- [ ] Integrate into `MainLayout.razor`
- [ ] Test cross-browser (Safari, Chrome mobile)

**Validation:**
- FAB visible on mobile screens
- FAB hidden on desktop
- Tapping FAB triggers Quick Add flow
- FAB accessible via screen reader

---

### Phase 3: Quick Add Form & Flow
> **Commit:** `feat(client): add QuickAddForm with bottom sheet presentation`

**Objective:** Create streamlined mobile transaction entry experience

**Tasks:**
- [ ] Create `QuickAddForm.razor` (simplified TransactionForm)
- [ ] Optimize form fields for touch (48px min height)
- [ ] Use native select elements on mobile
- [ ] Set input types (`inputmode="decimal"` for amount)
- [ ] Wire FAB to open QuickAddForm in BottomSheet
- [ ] Default account to most recently used
- [ ] Default date to today
- [ ] Add success toast on save
- [ ] Test form with virtual keyboard open

**Validation:**
- FAB opens Quick Add bottom sheet
- Form fields large enough for comfortable touch
- Keyboard doesn't obscure active field
- Transaction saves successfully

---

### Phase 4: Swipe Gesture Support for Calendar
> **Commit:** `feat(client): add swipe navigation to Calendar`

**Objective:** Enable swipe left/right for month navigation

**Tasks:**
- [ ] Create `SwipeContainer.razor` wrapper component
- [ ] Create `swipe.js` for touch event handling
- [ ] Wrap `CalendarGrid` in `SwipeContainer`
- [ ] Call `PreviousMonth`/`NextMonth` on swipe
- [ ] Add subtle visual feedback during swipe
- [ ] Ensure vertical scroll still works (not hijacked)
- [ ] Test on iOS Safari and Android Chrome

**Validation:**
- Swipe right goes to previous month
- Swipe left goes to next month
- Vertical scrolling still works
- No accidental triggers from taps

---

### Phase 5: Calendar Touch Optimization
> **Commit:** `refactor(client): optimize Calendar for touch interaction`

**Objective:** Improve calendar usability on mobile

**Tasks:**
- [ ] Increase `calendar-day` min-height to 70px on mobile
- [ ] Increase font sizes for legibility
- [ ] Add compact transaction display (just count/total)
- [ ] Improve today/selected state visibility
- [ ] Add haptic feedback on day tap (if available)
- [ ] Test with finger vs. stylus

**Validation:**
- Day cells easy to tap without mis-taps
- Content readable without zooming
- Selected state clearly visible

---

### Phase 6: Week View for Mobile
> **Commit:** `feat(client): add CalendarWeekView for mobile`

**Objective:** Provide higher-detail week view for mobile users

**Tasks:**
- [ ] Create `CalendarWeekView.razor` (7 columns, taller rows)
- [ ] Create `CalendarViewToggle.razor` (Month | Week buttons)
- [ ] Show toggle only on mobile (CSS media query)
- [ ] Week view shows more transaction detail per day
- [ ] Persist view preference in localStorage
- [ ] Week navigation via swipe or buttons
- [ ] Integrate into `Calendar.razor` page

**Validation:**
- Toggle visible on mobile only
- Week view shows 7 days with detail
- Preference persists across sessions

---

### Phase 7: AI Assistant Mobile Experience
> **Commit:** `feat(client): mobile-optimized AI Assistant with bottom sheet`

**Objective:** Make AI Assistant easily accessible on mobile

**Tasks:**
- [ ] Add AI button to MobileFab (speed dial or separate button)
- [ ] Modify `ChatPanel` to render as `BottomSheet` on mobile
- [ ] Ensure input field stays above keyboard
- [ ] Add drag-to-fullscreen for expanded chat
- [ ] Test chat flow on mobile (send, receive, scroll)
- [ ] Integrate AI-assisted Quick Add (parse natural language)

**Validation:**
- AI accessible with one tap from FAB
- Chat opens as bottom sheet
- Messages readable and scrollable
- Input doesn't get hidden by keyboard

---

### Phase 8: Testing & Polish
> **Commit:** `test(client): add mobile experience E2E tests and polish`

**Objective:** Comprehensive testing and final refinements

**Tasks:**
- [ ] Add Playwright E2E tests with mobile viewport
- [ ] Test all flows: Quick Add, Calendar navigation, AI chat
- [ ] Test orientation change (portrait/landscape)
- [ ] Run axe-core accessibility checks on mobile views
- [ ] Fix any responsive CSS issues found
- [ ] Performance test on lower-end devices
- [ ] Update documentation

**Validation:**
- All E2E tests pass on mobile viewport
- No accessibility violations
- Smooth performance on mid-range devices

---

## Testing Strategy

### Unit Tests

- [ ] `BottomSheet` renders at correct heights
- [ ] `BottomSheet` calls `OnClose` when swiped down
- [ ] `MobileFab` calls correct callback when tapped
- [ ] `SwipeContainer` detects left/right swipes correctly
- [ ] `QuickAddForm` validates required fields

### Integration Tests

- [ ] FAB triggers Quick Add flow end-to-end
- [ ] Transaction created via Quick Add appears in calendar
- [ ] Calendar month changes on swipe

### E2E Tests (Playwright)

- [ ] Mobile viewport (375x667): FAB visible, calendar touchable
- [ ] Quick Add flow: FAB → form → submit → success
- [ ] Swipe navigation: calendar month changes
- [ ] AI Assistant: FAB → chat → send message → receive response

### Manual Testing Checklist

- [ ] Test on iPhone Safari (iOS 16+)
- [ ] Test on Android Chrome
- [ ] Test on iPad (tablet breakpoint)
- [ ] Test with VoiceOver (iOS) / TalkBack (Android)
- [ ] Test with keyboard only (accessibility)
- [ ] Test in portrait and landscape orientations

---

## Decisions Made

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Modal pattern on mobile | Bottom sheet | Native mobile UX pattern; less disruptive than full modal |
| FAB position | Bottom-right | Standard Material Design placement; thumb-reachable |
| Swipe detection | Custom JS | Avoid heavy libraries; simple implementation sufficient |
| Week view | Mobile only | Desktop has room for month view; mobile benefits from detail |
| AI Assistant trigger | Speed dial FAB | Single FAB entry point; expandable for multiple actions |

---

## Security Considerations

- No new API endpoints; uses existing authenticated endpoints
- Bottom sheet prevents interaction with page behind (no clickjacking)
- Form validation same as desktop (server-side validation unchanged)

---

## Performance Considerations

- Lazy-load `swipe.js` and `bottom-sheet.js` only on mobile
- Use CSS transforms for animations (GPU-accelerated)
- Avoid layout thrashing in swipe detection
- Week view lazy-renders only visible days
- FAB uses CSS positioning (no JS position calculation)

---

## Future Enhancements

- Offline transaction queue (save locally, sync when online)
- Pull-to-refresh on calendar
- Quick Add voice input
- Geolocation-based merchant suggestions
- Biometric authentication for Quick Add

---

## References

- [Material Design FAB Guidelines](https://material.io/components/buttons-floating-action-button)
- [Bottom Sheet Pattern](https://material.io/components/sheets-bottom)
- [Touch Target Size (WCAG)](https://www.w3.org/WAI/WCAG21/Understanding/target-size.html)
- [Feature 046 - Accessibility](./046-accessible-theme-and-wcag-ui-tests.md)
- [Feature 045 - Component Standards](./045-ui-component-refactor-and-library.md)

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-01-26 | Initial draft | @becauseimclever |
| 2026-02-02 | Fleshed out with technical design & implementation phases | @copilot |
