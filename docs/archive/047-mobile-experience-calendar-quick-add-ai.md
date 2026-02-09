# Feature 047: Mobile Experience - Calendar, Quick Add, and AI Assistant
> **Status:** ✅ Implemented (v3.13.0)  
> **Priority:** High  
> **Dependencies:** Feature 046 (Accessibility) ✅, Feature 045 (Component Refactor) ✅  
> **Estimated Effort:** 3-4 sprints (~6-8 weeks)  
> **Risk Level:** Medium (JS interop complexity, cross-browser touch behaviors)

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

## Effort Estimation

| Phase | Complexity | Estimated Days | Dependencies |
|-------|------------|----------------|--------------|
| Phase 1: Bottom Sheet Component | Medium | 3-4 days | None |
| Phase 2: Floating Action Button | Low | 2 days | Phase 1 |
| Phase 3: Quick Add Form & Flow | Medium | 3-4 days | Phase 1, 2 |
| Phase 4: Swipe Gesture Support | High | 4-5 days | None (parallel) |
| Phase 5: Calendar Touch Optimization | Low | 2 days | None |
| Phase 6: Week View for Mobile | Medium | 3-4 days | Phase 5 |
| Phase 7: AI Assistant Mobile | Medium | 3-4 days | Phase 1 |
| Phase 8: Testing & Polish | Medium | 3-4 days | All |
| **Total** | | **23-31 days** | |

**Note:** Phases 1-3 and 4-6 can run in parallel tracks.

---

## Risk Analysis

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| iOS Safari touch event quirks | High | Medium | Test on real devices early; use `touchstart`/`touchend` with `passive: true` |
| Virtual keyboard obscures inputs | High | Medium | Use `visualViewport` API and CSS `env(safe-area-inset-*)` |
| Swipe conflicts with native scroll | Medium | High | Require horizontal threshold > vertical delta before capturing |
| Bottom sheet performance on low-end devices | Medium | Medium | Use CSS transforms (GPU-accelerated); avoid layout thrashing |
| FAB occludes content at screen bottom | Low | Medium | Add bottom padding to pages; ensure FAB has backdrop or shadow |
| Blazor WASM size impact from new components | Low | Low | Components are small; lazy-load JS modules only on mobile |
| Cross-browser touch API differences | Medium | Medium | Abstract touch handling in `swipe.js`; test on Chrome, Safari, Firefox mobile |

---

## Alternatives Considered

| Approach | Pros | Cons | Decision |
|----------|------|------|----------|
| Use Hammer.js for gestures | Battle-tested, rich API | 7KB+ added, overkill for our needs | ❌ Rejected |
| Native `<dialog>` for bottom sheet | Built-in, accessible | No swipe/drag support; limited styling | ❌ Rejected |
| CSS-only swipe detection | No JS dependency | Limited; can't detect swipe direction reliably | ❌ Rejected |
| Speed dial FAB | Familiar pattern, expandable | More complex; 2+ buttons in small area | ✅ Accepted for AI + Quick Add |
| Single FAB (Quick Add only) | Simpler | AI Assistant less discoverable | ❌ Rejected |
| Tab bar at bottom | iOS-native pattern | Conflicts with existing nav; major refactor | ⏳ Deferred to future |

---

## Success Metrics

| Metric | Baseline | Target | Measurement |
|--------|----------|--------|-------------|
| Mobile calendar day tap accuracy | ~70% (estimated) | >95% | User testing / analytics |
| Time to add transaction (mobile) | ~15 seconds via nav | <8 seconds via FAB | Manual timing |
| Mobile bounce rate | Unknown | Decrease 20% | Analytics |
| Lighthouse mobile score | 75-80 | >90 | CI/CD check |
| Touch target compliance | ~60% meet 48px | 100% | Automated axe-core tests |

---

## User Stories

### Mobile Calendar Navigation

#### US-047-001: Touch-Friendly Calendar View
**As a** mobile user  
**I want to** view the calendar with adequately sized day cells  
**So that** I can easily tap on a specific day without mis-tapping

**Acceptance Criteria:**
- [ ] Calendar day cells have minimum 48x48px touch target on mobile (WCAG 2.5.5)
- [ ] Day numbers are legible (minimum 14px font, `--font-size-sm`)
- [ ] Today indicator and selected day are clearly distinguishable (not relying on color alone)
- [ ] Transaction totals remain visible but use compact format (e.g., "$-150" not "$-150.00")
- [ ] Touch feedback (`:active` state) is immediate and visible
- [ ] No overlap or clipping of content at 320px viewport width

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
- [ ] Bottom sheet slides up from screen bottom with smooth animation (300ms ease-out)
- [ ] Sheet height is 60-80% of viewport (adjustable by drag)
- [ ] Drag handle visible at top of sheet for affordance
- [ ] Form has large touch targets (min 48px height per field)
- [ ] Account defaults to most recently used (persisted in localStorage) or primary
- [ ] Date defaults to today (user can change via native date picker)
- [ ] Sheet dismissible by swipe down (100px threshold) or cancel button
- [ ] Submitting closes sheet and shows success toast notification
- [ ] Sheet respects `safe-area-inset-bottom` for notched devices
- [ ] Virtual keyboard doesn't obscure active input field

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
- [ ] All form inputs have minimum 48px height (via `min-height: var(--touch-target-min)`)
- [ ] Labels are above fields (not inline) on mobile (stacked layout)
- [ ] Select dropdowns use native mobile select (better UX, OS-provided picker)
- [ ] Keyboard type matches field (`inputmode="decimal"` for amount, `type="date"` for date)
- [ ] Form fits in viewport without excessive scrolling (5 fields max visible)
- [ ] Tap on label focuses associated input
- [ ] Clear visual feedback on focus (focus ring visible)
- [ ] Error messages appear inline below field with sufficient contrast

---

## Technical Design

### Current Mobile CSS Audit

The following mobile styles already exist and should be leveraged/extended:

| File | Current Mobile Behavior | Enhancement Needed |
|------|-------------------------|-------------------|
| `calendar.css` | Min-height 60px at 768px, 0.7rem headers at 480px | Increase to 70px, larger fonts |
| `ChatPanel.razor.css` | Fixed position, 100% width at 480px | Convert to bottom sheet pattern |
| `layout.css` | Sidebar collapses, `.hide-mobile` utilities exist | Add FAB positioning utilities |
| `MainLayout.razor.css` | Header shrinks on mobile | Add FAB container slot |
| `forms.css` | Standard form styling | Add 48px min-height for touch |

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

### CSS Design Tokens to Add

Add these tokens to `tokens.css` for mobile-specific values:

```css
:root {
  /* Touch target sizes (WCAG 2.5.5) */
  --touch-target-min: 48px;
  --touch-target-recommended: 56px;
  
  /* FAB sizing */
  --fab-size: 56px;
  --fab-size-mini: 40px;
  --fab-spacing: var(--space-4); /* 16px from edges */
  --fab-shadow: 0 4px 8px rgba(0, 0, 0, 0.2), 0 2px 4px rgba(0, 0, 0, 0.15);
  
  /* Bottom sheet */
  --bottom-sheet-radius: var(--radius-lg);
  --bottom-sheet-handle-width: 40px;
  --bottom-sheet-handle-height: 4px;
  --bottom-sheet-z-index: var(--z-modal);
  
  /* Safe areas for notched devices */
  --safe-area-bottom: env(safe-area-inset-bottom, 0px);
  --safe-area-top: env(safe-area-inset-top, 0px);
}

/* Mobile-specific overrides */
@media (max-width: 768px) {
  :root {
    --calendar-day-min-height: 70px;
    --calendar-day-font-size: var(--font-size-sm);
  }
}
```

### API Endpoints

No new API endpoints required. Existing endpoints:
- `GET /api/v1/calendar/{year}/{month}` - Calendar grid data
- `POST /api/v1/transactions` - Create transaction
- `POST /api/v1/chat` - AI chat messages

---

## Implementation Plan

### Phase 1: Bottom Sheet Component ✅
> **Commit:** `feat(client): add reusable BottomSheet component for mobile`

**Objective:** Create the foundational bottom sheet pattern for mobile interactions

**Tasks:**
- [x] Create `BottomSheet.razor` component with slide-up animation
- [x] Create `bottom-sheet.css` with transitions and height variants
- [x] Implement drag-to-resize functionality via JS interop
- [x] Implement swipe-down-to-close gesture (threshold: 100px drag down)
- [x] Add backdrop overlay with click-to-close
- [x] Add drag handle visual indicator at top of sheet
- [x] Add ARIA attributes (`role="dialog"`, `aria-modal="true"`, `aria-labelledby`)
- [x] Implement focus trap inside sheet when open
- [x] Handle `Escape` key to close
- [x] Write unit tests for component behavior (bUnit)
- [ ] Test on actual mobile devices/emulators (iOS Safari, Android Chrome)

**CSS Animation Specifications:**
```css
/* Bottom sheet enter animation */
.bottom-sheet-enter {
  transform: translateY(100%);
}
.bottom-sheet-enter-active {
  transform: translateY(0);
  transition: transform 300ms cubic-bezier(0.32, 0.72, 0, 1);
}

/* Bottom sheet exit animation */
.bottom-sheet-exit {
  transform: translateY(0);
}
.bottom-sheet-exit-active {
  transform: translateY(100%);
  transition: transform 200ms ease-in;
}
```

**Validation:**
- Bottom sheet opens smoothly from bottom
- Can be dragged to resize
- Swipe down dismisses sheet
- Focus trapped inside sheet when open
- Works with keyboard navigation

---

### Phase 2: Floating Action Button ✅
> **Commit:** `feat(client): add MobileFab floating action button component`

**Objective:** Add persistent FAB for quick transaction entry on mobile

**Tasks:**
- [x] Create `MobileFab.razor` with Quick Add primary button and AI secondary button
- [x] Create `fab.css` with positioning, animation, and theme support
- [x] Implement speed dial expand/collapse animation
- [x] Show FAB only on mobile (< 768px) using CSS media query
- [x] Hide FAB when modal/bottom-sheet is open (via IsHidden parameter)
- [x] Add ripple effect on tap for visual feedback
- [x] Add proper ARIA labels (`aria-label="Add transaction"`, `aria-expanded`)
- [x] Integrate into `MainLayout.razor` outside of `<main>` for proper z-index
- [ ] Test cross-browser (Safari, Chrome mobile, Firefox mobile)

**FAB Layout (Speed Dial Pattern):**
```
                   ┌───────────┐
                   │ AI (mini) │  ← Secondary, appears on expand
                   └───────────┘
                        ↑
                   ┌───────────┐
                   │  + / ×    │  ← Primary FAB, toggles expand
                   └───────────┘
                   16px from edges
```

**Validation:**
- FAB visible on mobile screens
- FAB hidden on desktop
- Tapping FAB triggers Quick Add flow (or expands speed dial)
- FAB accessible via screen reader
- FAB does not obscure critical content

---

### Phase 3: Quick Add Form & Flow
> **Commit:** `feat(client): add QuickAddForm with bottom sheet presentation`

**Objective:** Create streamlined mobile transaction entry experience

**Tasks:**
- [x] Create `QuickAddForm.razor` (simplified TransactionForm for mobile)
- [x] Optimize form fields for touch (48px min height via `--touch-target-min`)
- [x] Use native `<select>` elements on mobile (better UX than custom dropdowns)
- [x] Set input types (`inputmode="decimal"` for amount, `inputmode="text"` for description)
- [x] Wire FAB to open QuickAddForm in BottomSheet
- [x] Default account to most recently used (stored in localStorage)
- [x] Default date to today (pre-populated)
- [x] Add success toast on save (created ToastService + ToastContainer)
- [ ] Implement AI-assisted parsing preview ("Coffee at Starbucks $5" → prefilled fields) *(deferred — future enhancement)*
- [ ] Test form with virtual keyboard open (ensure input stays visible) *(manual test — see Manual Testing Checklist)*
- [x] Handle viewport resize when keyboard appears (`visualViewport` API) *(done in Phase 1 — bottom-sheet.js uses `visualViewport?.height`)*

**Form Field Order (optimized for mobile flow):**
1. Description (first focus, largest input)
2. Amount (numeric keyboard via `inputmode="decimal"`)
3. Category (native select)
4. Account (native select, defaulted)
5. Date (native date picker, defaulted to today)

**Validation:**
- FAB opens Quick Add bottom sheet
- Form fields large enough for comfortable touch (48px+)
- Keyboard doesn't obscure active field
- Transaction saves successfully
- Success feedback displayed

---

### Phase 4: Swipe Gesture Support for Calendar
> **Commit:** `feat(client): add swipe navigation to Calendar`

**Objective:** Enable swipe left/right for month navigation

**Tasks:**
- [x] Create `SwipeContainer.razor` wrapper component with JS interop
- [x] Create `swipe.js` for touch event handling (touchstart/touchmove/touchend)
- [x] Implement swipe detection with configurable threshold (default 50px)
- [x] Require horizontal delta > vertical delta to avoid scroll hijacking
- [x] Wrap `CalendarGrid` in `SwipeContainer` on Calendar.razor page
- [x] Call `PreviousMonth`/`NextMonth` on swipe completion
- [x] Add subtle visual feedback during swipe (translateX parallax effect)
- [x] Use `requestAnimationFrame` for smooth animation
- [x] Ensure vertical scroll still works for day detail content
- [x] Handle edge cases: multi-touch, rapid swipes, interrupted swipes
- [ ] Test on iOS Safari and Android Chrome (different touch behaviors)

**Swipe Detection Algorithm:**
```javascript
// Only trigger swipe if:
// 1. Horizontal distance > threshold (50px)
// 2. Horizontal distance > vertical distance (not scrolling)
// 3. Swipe completed within reasonable time (<500ms)
if (Math.abs(deltaX) > threshold && 
    Math.abs(deltaX) > Math.abs(deltaY) &&
    elapsed < 500) {
    // Trigger swipe callback
}
```

**Validation:**
- Swipe right goes to previous month
- Swipe left goes to next month
- Vertical scrolling still works
- No accidental triggers from taps or slow drags
- Works with screen reader gestures disabled

---

### Phase 5: Calendar Touch Optimization
> **Commit:** `refactor(client): optimize Calendar for touch interaction`

**Objective:** Improve calendar usability on mobile

**Tasks:**
- [x] Increase `calendar-day` min-height to 70px on mobile
- [x] Increase font sizes for legibility
- [x] Add compact transaction display (just count/total)
- [x] Improve today/selected state visibility
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
- [x] Create `CalendarWeekView.razor` (7 columns, taller rows)
- [x] Create `CalendarViewToggle.razor` (Month | Week buttons)
- [x] Show toggle only on mobile (CSS media query)
- [x] Week view shows more transaction detail per day
- [x] Persist view preference in localStorage
- [x] Week navigation via swipe or buttons
- [x] Integrate into `Calendar.razor` page

**Validation:**
- Toggle visible on mobile only
- Week view shows 7 days with detail
- Preference persists across sessions

---

### Phase 7: AI Assistant Mobile Experience
> **Commit:** `feat(client): mobile-optimized AI Assistant with bottom sheet`

**Objective:** Make AI Assistant easily accessible on mobile

**Tasks:**
- [x] Add AI button to MobileFab (speed dial or separate button)
- [x] Modify `ChatPanel` to render as `BottomSheet` on mobile
- [x] Ensure input field stays above keyboard
- [x] Add drag-to-fullscreen for expanded chat
- [x] Test chat flow on mobile (send, receive, scroll)
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
- [x] Add Playwright E2E tests with mobile viewport
- [x] Test all flows: Quick Add, Calendar navigation, AI chat
- [x] Test orientation change (portrait/landscape)
- [x] Run axe-core accessibility checks on mobile views
- [x] Fix any responsive CSS issues found
- [ ] Performance test on lower-end devices
- [x] Update documentation

**Validation:**
- All E2E tests pass on mobile viewport
- No accessibility violations
- Smooth performance on mid-range devices

---

## Testing Strategy

### Unit Tests (bUnit)

- [x] `BottomSheet` renders at correct heights (Small=40%, Medium=60%, Large=80%)
- [x] `BottomSheet` calls `OnClose` when backdrop clicked (if CloseOnOverlayClick=true)
- [x] `BottomSheet` traps focus inside when open
- [x] `BottomSheet` handles Escape key to close
- [x] `MobileFab` calls `OnQuickAddClick` when primary FAB tapped
- [x] `MobileFab` expands/collapses speed dial on primary tap
- [x] `MobileFab` calls `OnAiAssistantClick` when AI button tapped
- [x] `SwipeContainer` fires `OnSwipeLeft`/`OnSwipeRight` with correct direction
- [x] `QuickAddForm` validates required fields (description, amount)
- [x] `QuickAddForm` defaults date to today
- [x] `CalendarWeekView` renders 7 days correctly
- [x] `CalendarViewToggle` persists preference

### Integration Tests

- [x] FAB triggers Quick Add flow end-to-end (FAB → BottomSheet → Form → API → Success)
- [ ] Transaction created via Quick Add appears in calendar grid
- [x] Calendar month changes on swipe and URL updates
- [x] AI Assistant opens as bottom sheet on mobile viewport
- [x] Week view toggle switches calendar display mode

### E2E Tests (Playwright)

```typescript
// Example test structure
test.describe('Mobile Experience', () => {
  test.use({ viewport: { width: 375, height: 667 } }); // iPhone SE

  test('FAB visible on mobile', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('[data-testid="mobile-fab"]')).toBeVisible();
  });

  test('Quick Add flow via FAB', async ({ page }) => {
    await page.goto('/');
    await page.click('[data-testid="mobile-fab"]');
    await expect(page.locator('.bottom-sheet')).toBeVisible();
    await page.fill('[data-testid="quick-add-description"]', 'Coffee');
    await page.fill('[data-testid="quick-add-amount"]', '-5.00');
    await page.click('[data-testid="quick-add-submit"]');
    await expect(page.locator('.toast-success')).toBeVisible();
  });

  test('Swipe navigation changes month', async ({ page }) => {
    await page.goto('/2026/2');
    await page.locator('.calendar-grid').swipe('left');
    await expect(page).toHaveURL('/2026/3');
  });

  test('Calendar day cells meet touch target size', async ({ page }) => {
    await page.goto('/');
    const dayCell = page.locator('.calendar-day').first();
    const box = await dayCell.boundingBox();
    expect(box?.width).toBeGreaterThanOrEqual(48);
    expect(box?.height).toBeGreaterThanOrEqual(48);
  });
});
```

### Accessibility Tests (axe-core)

- [x] BottomSheet passes axe-core with no critical/serious violations
- [x] FAB has accessible name and role
- [x] Focus order correct when bottom sheet opens/closes
- [x] Touch targets meet WCAG 2.5.5 (44x44px minimum)
- [x] Color contrast maintained in all FAB/bottom sheet states

### Manual Testing Checklist

- [ ] Test on iPhone Safari (iOS 16+) – real device preferred
- [ ] Test on Android Chrome (Android 12+) – real device preferred
- [ ] Test on iPad (tablet breakpoint 768px-1024px)
- [ ] Test with VoiceOver (iOS) – FAB and bottom sheet announced correctly
- [ ] Test with TalkBack (Android) – navigation works
- [ ] Test with keyboard only (desktop at 768px viewport) – all features accessible
- [ ] Test in portrait and landscape orientations
- [ ] Test with `prefers-reduced-motion` enabled – animations disabled/reduced
- [ ] Test with slow 3G network throttling – lazy-loaded JS doesn't break UX
- [ ] Test rapid FAB tapping – no duplicate bottom sheets

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
- [Touch Target Size (WCAG 2.5.5)](https://www.w3.org/WAI/WCAG21/Understanding/target-size.html)
- [Feature 046 - Accessibility](./046-accessible-theme-and-wcag-ui-tests.md)
- [Feature 045 - Component Standards](./045-ui-component-refactor-and-library.md)
- [Component Standards Guide](./COMPONENT-STANDARDS.md)
- [Visual Viewport API (MDN)](https://developer.mozilla.org/en-US/docs/Web/API/Visual_Viewport_API)
- [CSS env() safe-area-inset](https://developer.mozilla.org/en-US/docs/Web/CSS/env)

---

## Pre-Implementation Checklist

Before starting development, ensure:

- [ ] Feature 045 (Component Refactor) is complete ✅
- [ ] Feature 046 (Accessibility) is complete ✅
- [ ] Test device(s) available (iPhone, Android phone, or good emulators)
- [ ] Safari on macOS for iOS debugging via Web Inspector
- [ ] Chrome DevTools mobile emulation configured
- [ ] Playwright mobile viewports configured in test setup
- [ ] Design tokens documented in `tokens.css`
- [ ] Component naming follows COMPONENT-STANDARDS.md patterns

---

## Open Questions

| Question | Status | Decision |
|----------|--------|----------|
| Should week view be opt-in or default on mobile? | ✅ Decided | Opt-in via toggle; month view remains default |
| Should FAB have haptic feedback on iOS? | ✅ Decided | Yes, use Vibration API where supported |
| Should AI-assisted Quick Add be MVP or Phase 2? | ✅ Decided | MVP: basic form; Phase 2: AI parsing |
| What's the maximum bottom sheet height? | ✅ Decided | 90% viewport (leave header visible) |
| Should FAB hide on scroll down (like Android)? | ✅ Decided | Start with always-visible; iterate based on feedback |

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-01-26 | Initial draft | @becauseimclever |
| 2026-02-02 | Fleshed out with technical design & implementation phases | @copilot |
| 2026-02-05 | Added effort estimates, risk analysis, alternatives, success metrics, detailed phase tasks, comprehensive testing strategy, pre-implementation checklist, open questions | @copilot |
| 2026-02-07 | Implementation complete; released in v3.13.0. Calendar bug fixes (icons, panel sizing, CSS alignment) included. | @copilot |
