# Feature 048: Calendar-First Budget Editing
> **Status:** 🗒️ Planning
> **Depends on:** Calendar View (complete), Budget Goals (complete)

## Overview

Enable users to view, create, and edit monthly budget goals directly from the calendar view without leaving the primary navigation context. The calendar becomes the central hub for all budget interactions, with an expandable month summary panel that shows budget progress and allows inline goal management.

This feature transforms the calendar from a transaction-only view into a complete budget command center, maintaining the calendar-first philosophy while giving users full control over their monthly financial targets.

## Problem Statement

Currently, users must navigate away from the calendar to the dedicated Budget page to view progress or edit budget goals. This breaks the calendar-centric workflow and forces context switching, especially when users want to quickly check or adjust budgets while reviewing their transactions.

### Current State

- Calendar page displays transactions and recurring items per day
- `BudgetAlert` component shows over-budget/warning notifications but requires clicking "View Budget" to act
- Budget goals can only be edited on the `/budget` page
- No budget progress visibility within the calendar view itself
- Users cannot see at-a-glance how their spending relates to their goals for the displayed month

### Target State

- Calendar page includes an expandable **Month Budget Panel** showing:
  - Overall monthly budget progress (total budgeted vs. spent)
  - Category-by-category progress bars with quick edit capability
  - Visual status indicators (on track, warning, over budget)
- Users can create/edit budget goals inline via a modal triggered from the panel
- Budget changes reflect immediately in the calendar view
- Mobile-friendly: panel is collapsible and touch-optimized
- Desktop: panel can dock to the side or appear as an overlay

---

## User Stories

### View Budget Progress in Calendar

#### US-048-001: View monthly budget summary in calendar
**As a** user  
**I want to** see my monthly budget progress directly in the calendar view  
**So that** I can understand my spending status without leaving the calendar

**Acceptance Criteria:**
- [ ] Calendar page displays a collapsible "Budget Summary" panel
- [ ] Panel shows overall monthly progress (total budgeted, spent, remaining)
- [ ] Panel shows status counts (X on track, Y warning, Z over budget)
- [ ] Panel is collapsed by default on mobile, expanded on desktop
- [ ] Panel state persists across page navigation

#### US-048-002: View category breakdown in calendar
**As a** user  
**I want to** see budget progress for each category within the calendar  
**So that** I can identify which categories need attention

**Acceptance Criteria:**
- [ ] Expandable section shows per-category progress
- [ ] Each category displays: name, icon, spent/target, progress bar, status
- [ ] Categories are sorted by status (over budget first, then warning, then on track)
- [ ] Clicking a category reveals edit option

### Edit Budget Goals from Calendar

#### US-048-003: Edit existing budget goal from calendar
**As a** user  
**I want to** edit a budget goal directly from the calendar view  
**So that** I can adjust my targets without navigating away

**Acceptance Criteria:**
- [ ] Each category in the panel has an edit button
- [ ] Clicking edit opens a modal with current goal amount
- [ ] User can update the target amount
- [ ] Changes save successfully and panel refreshes
- [ ] Success/error feedback displayed

#### US-048-004: Create new budget goal from calendar
**As a** user  
**I want to** set a budget goal for a category that doesn't have one  
**So that** I can start tracking spending limits for that category

**Acceptance Criteria:**
- [ ] Categories without goals show "Set Budget" action
- [ ] Clicking opens a modal to set the target amount
- [ ] Goal is created for the currently displayed month
- [ ] Panel updates to show the new goal

#### US-048-005: Delete budget goal from calendar
**As a** user  
**I want to** remove a budget goal I no longer need  
**So that** I can keep my budget organized

**Acceptance Criteria:**
- [ ] Edit modal includes a "Delete Goal" option
- [ ] Confirmation prompt before deletion
- [ ] Goal is deleted and panel refreshes

### Copy Budget Goals

#### US-048-006: Copy goals from previous month
**As a** user  
**I want to** copy budget goals from a previous month  
**So that** I can quickly set up a new month without re-entering values

**Acceptance Criteria:**
- [ ] "Copy from previous month" button available when no goals exist for current month
- [ ] Copies all goals from the most recent month that has goals
- [ ] Confirmation shown with count of goals to copy
- [ ] Panel refreshes with new goals

---

## Technical Design

### Architecture Changes

The feature adds a new panel component to the Calendar page and enhances the existing budget editing flow. No new domain entities are required—this leverages existing `BudgetGoal` and `BudgetCategory` entities.

```
┌─────────────────────────────────────────────────────────────┐
│                      Calendar Page                          │
├─────────────────────────────────────────────────────────────┤
│  [< Prev]     February 2026      [Next >]                  │
│  Filter: [All Accounts ▼]                                   │
├─────────────────────────────────────────────────────────────┤
│  ┌──────────────────────────────────────────────────────┐  │
│  │ 📊 Budget Summary for February            [▼ Expand] │  │
│  │                                                       │  │
│  │ Total: $1,200 / $2,000  |  60% used  |  ███████░░░   │  │
│  │ Status: 3 on track, 1 warning, 1 over budget         │  │
│  │                                                       │  │
│  │ ─────────────────────────────────────────────────────│  │
│  │ 🍔 Food        $350 / $400   ████████░░ 87% ⚠️ [Edit]│  │
│  │ 🚗 Transport   $180 / $150   ██████████ 120% 🔴 [Edit]│  │
│  │ 🛒 Shopping    $200 / $500   ████░░░░░░ 40%  ✓ [Edit]│  │
│  │ 🎮 Entertainment $50 / $100  █████░░░░░ 50%  ✓ [Edit]│  │
│  │ ☕ Coffee      (no budget)                   [+ Set] │  │
│  │                                                       │  │
│  │ [Copy from January]                                   │  │
│  └──────────────────────────────────────────────────────┘  │
├─────────────────────────────────────────────────────────────┤
│  [Calendar Grid...]                                         │
├─────────────────────────────────────────────────────────────┤
│  [Day Detail Panel...]                                      │
└─────────────────────────────────────────────────────────────┘
```

### UI Components

| Component | Location | Purpose |
|-----------|----------|---------|
| `CalendarBudgetPanel.razor` | `Components/Calendar/` | **NEW** - Collapsible budget summary panel |
| `CalendarBudgetCategoryRow.razor` | `Components/Calendar/` | **NEW** - Individual category row with progress and edit |
| `BudgetGoalModal.razor` | `Components/Forms/` | **NEW** - Modal for creating/editing/deleting budget goals |
| `Calendar.razor` | `Pages/` | **MODIFY** - Integrate CalendarBudgetPanel |

### Component Specifications

#### CalendarBudgetPanel.razor

```razor
@* CalendarBudgetPanel.razor - Collapsible budget summary for calendar view *@

<div class="calendar-budget-panel @(IsExpanded ? "expanded" : "collapsed")">
    <div class="panel-header" @onclick="ToggleExpanded">
        <div class="panel-title">
            <Icon Name="chart-pie" Size="18" />
            <span>Budget Summary for @MonthName</span>
        </div>
        <div class="panel-summary-inline" @onclick:stopPropagation="true">
            @if (Summary != null)
            {
                <span class="summary-quick">
                    @Summary.TotalSpent.Amount.ToString("C") / @Summary.TotalBudgeted.Amount.ToString("C")
                </span>
                <span class="summary-percent @GetOverallStatusClass()">
                    @Summary.OverallPercentUsed.ToString("N0")%
                </span>
            }
        </div>
        <Icon Name="@(IsExpanded ? "chevron-up" : "chevron-down")" Size="16" Class="toggle-icon" />
    </div>
    
    @if (IsExpanded)
    {
        <div class="panel-body">
            @* Full budget progress and category list *@
        </div>
    }
</div>
```

**Parameters:**
- `Summary` (BudgetSummaryDto): The budget data for the month
- `Year` (int): Current year
- `Month` (int): Current month
- `OnEditGoal` (EventCallback<BudgetProgressDto>): Called when edit is requested
- `OnCreateGoal` (EventCallback<BudgetProgressDto>): Called when set budget is requested
- `OnCopyFromPrevious` (EventCallback): Called when copy from previous month is clicked

#### BudgetGoalModal.razor

```razor
@* BudgetGoalModal.razor - Create/Edit/Delete budget goal modal *@

<Modal IsVisible="@IsVisible" Title="@GetTitle()" OnClose="HandleClose">
    <div class="form-group">
        <label class="form-label">Category</label>
        <p class="form-static">@CategoryName</p>
    </div>
    <div class="form-group">
        <label class="form-label">Month</label>
        <p class="form-static">@MonthDisplay</p>
    </div>
    <div class="form-group">
        <label for="targetAmount" class="form-label">Budget Amount</label>
        <div class="input-group">
            <span class="input-group-text">$</span>
            <input id="targetAmount" type="number" class="form-control" 
                   step="0.01" min="0" @bind="TargetAmount" />
        </div>
    </div>
    <div class="form-actions form-actions-right">
        @if (Mode == GoalEditMode.Edit)
        {
            <button class="btn btn-danger" @onclick="HandleDelete" disabled="@IsSubmitting">
                Delete
            </button>
        }
        <button class="btn btn-secondary" @onclick="HandleClose" disabled="@IsSubmitting">
            Cancel
        </button>
        <button class="btn btn-primary" @onclick="HandleSave" disabled="@IsSubmitting">
            @(IsSubmitting ? "Saving..." : "Save")
        </button>
    </div>
</Modal>
```

**Parameters:**
- `IsVisible` (bool): Controls modal visibility
- `Mode` (GoalEditMode): Create or Edit
- `CategoryId` (Guid): The category being edited
- `CategoryName` (string): Display name
- `GoalId` (Guid?): Existing goal ID (for edit mode)
- `InitialAmount` (decimal): Pre-populated amount
- `Year` (int): Target year
- `Month` (int): Target month
- `OnSave` (EventCallback<decimal>): Called on save with new amount
- `OnDelete` (EventCallback): Called on delete
- `OnClose` (EventCallback): Called when modal closes

### API Endpoints

No new endpoints required. Existing endpoints support all operations:

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/api/v1/budgets/summary/{year}/{month}` | Get budget summary (already used by BudgetAlert) |
| GET | `/api/v1/budgets/progress/{categoryId}/{year}/{month}` | Get single category progress |
| POST | `/api/v1/budgets/goals` | Create new goal |
| PUT | `/api/v1/budgets/goals/{id}` | Update existing goal |
| DELETE | `/api/v1/budgets/goals/{id}` | Delete goal |
| POST | `/api/v1/budgets/goals/copy-from-month` | Copy goals from previous month (if not exists) |

**New Endpoint Needed:**

| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | `/api/v1/budgets/goals/copy` | Copy goals from a source month to target month |

Request body:
```json
{
  "sourceYear": 2026,
  "sourceMonth": 1,
  "targetYear": 2026,
  "targetMonth": 2
}
```

### State Management

The Calendar page already loads `budgetSummary` via `LoadBudgetSummary()`. This feature extends that state:

```csharp
// Existing state in Calendar.razor
private BudgetSummaryDto? budgetSummary;

// New state additions
private bool isBudgetPanelExpanded = true; // Default expanded on desktop
private bool isGoalModalVisible = false;
private GoalEditMode goalEditMode = GoalEditMode.Create;
private BudgetProgressDto? editingGoalProgress;
```

### Responsive Behavior

| Viewport | Panel Behavior |
|----------|----------------|
| Desktop (>1024px) | Panel expanded by default, full category list visible |
| Tablet (768-1024px) | Panel collapsed by default, expands to full width overlay |
| Mobile (<768px) | Panel collapsed by default, expands to full width, categories scrollable |

---

## Implementation Plan

### Phase 1: CalendarBudgetPanel Component

**Objective:** Create the collapsible budget summary panel component

**Tasks:**
- [ ] Create `CalendarBudgetPanel.razor` with header and toggle functionality
- [ ] Display overall budget progress (total budgeted/spent/remaining)
- [ ] Add status counts (on track, warning, over budget)
- [ ] Style panel for collapse/expand states
- [ ] Write bUnit tests for panel rendering and toggle behavior

**Commit:**
```
feat(client): add CalendarBudgetPanel component

- Collapsible panel showing monthly budget summary
- Toggle between collapsed (header only) and expanded views
- Displays total budgeted, spent, remaining with progress bar
- bUnit tests for component states
```

---

### Phase 2: Category Progress List

**Objective:** Display per-category budget progress within the panel

**Tasks:**
- [ ] Create `CalendarBudgetCategoryRow.razor` for individual category display
- [ ] Show category icon, name, spent/target, progress bar, status indicator
- [ ] Add edit button for categories with goals
- [ ] Add "Set Budget" button for categories without goals
- [ ] Sort categories by status (problems first)
- [ ] Write bUnit tests for category row rendering

**Commit:**
```
feat(client): add category progress rows to budget panel

- CalendarBudgetCategoryRow component for each category
- Progress bar and status indicators per category
- Edit/Set Budget action buttons
- Sorted by status (over budget first)
```

---

### Phase 3: Budget Goal Modal

**Objective:** Create reusable modal for budget goal CRUD operations

**Tasks:**
- [ ] Create `BudgetGoalModal.razor` component
- [ ] Support Create mode (new goal for category/month)
- [ ] Support Edit mode (update target amount)
- [ ] Support Delete action with confirmation
- [ ] Wire up API calls for save/delete
- [ ] Write bUnit tests for modal interactions

**Commit:**
```
feat(client): add BudgetGoalModal for inline goal editing

- Modal for creating/editing budget goals
- Delete goal with confirmation
- Form validation for target amount
- API integration for CRUD operations
```

---

### Phase 4: Calendar Page Integration

**Objective:** Integrate budget panel into Calendar page

**Tasks:**
- [ ] Add CalendarBudgetPanel to Calendar.razor
- [ ] Wire up existing `budgetSummary` state to panel
- [ ] Handle OnEditGoal/OnCreateGoal events to show modal
- [ ] Refresh budget data after save/delete
- [ ] Persist panel expanded/collapsed state
- [ ] Test full flow end-to-end

**Commit:**
```
feat(client): integrate budget panel into calendar page

- CalendarBudgetPanel added below month navigation
- Edit/create goals via modal from panel
- Data refresh after budget changes
- Panel state persistence
```

---

### Phase 5: Copy Goals from Previous Month

**Objective:** Enable bulk copying of goals from a prior month

**Tasks:**
- [ ] Add API endpoint `POST /api/v1/budgets/goals/copy`
- [ ] Write unit tests for copy service method
- [ ] Add "Copy from [Month]" button to panel (shown when no goals exist)
- [ ] Call API and refresh panel after copy
- [ ] Show confirmation with goal count

**Commit:**
```
feat(api): add copy budget goals endpoint

- POST /api/v1/budgets/goals/copy
- Copies all goals from source month to target month
- Returns count of goals copied
```

```
feat(client): add copy goals from previous month

- Button shown when current month has no goals
- Calls copy API and refreshes panel
- Confirmation toast with count
```

---

### Phase 6: Responsive & Mobile Optimization

**Objective:** Ensure excellent mobile experience

**Tasks:**
- [ ] Adjust panel layout for mobile (full-width, touch targets)
- [ ] Collapse panel by default on mobile
- [ ] Make category list scrollable within panel
- [ ] Test on various screen sizes
- [ ] Ensure modal works well on mobile keyboards

**Commit:**
```
style(client): responsive budget panel for mobile

- Panel collapsed by default on mobile
- Full-width expansion on small screens
- Touch-optimized edit buttons
- Scrollable category list
```

---

### Phase 7: Documentation & Cleanup

**Objective:** Final polish and documentation

**Tasks:**
- [ ] Add XML comments to new components and methods
- [ ] Update component README if applicable
- [ ] Remove any TODO comments
- [ ] Final accessibility review (keyboard navigation, ARIA)

**Commit:**
```
docs(client): add documentation for calendar budget panel

- XML comments for public parameters
- Accessibility improvements
- Code cleanup
```

---

## Testing Strategy

### Unit Tests (bUnit)

| Test | Component | Assertion |
|------|-----------|-----------|
| Panel renders collapsed state | CalendarBudgetPanel | Header visible, body hidden |
| Panel expands on click | CalendarBudgetPanel | Body becomes visible |
| Summary displays correct totals | CalendarBudgetPanel | Matches BudgetSummaryDto values |
| Category row shows progress | CalendarBudgetCategoryRow | Progress bar width matches percent |
| Edit button triggers callback | CalendarBudgetCategoryRow | OnEditGoal invoked with correct dto |
| Modal saves new goal | BudgetGoalModal | OnSave invoked with amount |
| Modal deletes goal | BudgetGoalModal | OnDelete invoked after confirm |

### Integration Tests

| Test | Scope | Assertion |
|------|-------|-----------|
| Calendar loads budget summary | Calendar.razor | Panel shows data from API |
| Edit goal flow | Full flow | Goal updated in database |
| Create goal flow | Full flow | New goal appears in panel |
| Copy goals flow | Full flow | Goals copied, panel refreshed |

### E2E Tests (Playwright)

| Test | User Flow |
|------|-----------|
| View budget in calendar | Navigate to calendar, verify panel shows budget data |
| Edit goal from calendar | Click edit, change amount, save, verify updated |
| Set budget for new category | Click "Set Budget", enter amount, save |
| Copy from previous month | Click copy, verify goals appear |

---

## Accessibility Considerations

- Panel toggle button has `aria-expanded` attribute
- Edit/Set Budget buttons have descriptive `aria-label`
- Modal traps focus while open
- Keyboard navigation: Enter/Space to toggle panel, Tab through categories
- Progress bars have `role="progressbar"` with `aria-valuenow`

---

## Performance Considerations

- Budget summary already loaded by Calendar page (no extra API call for basic view)
- Category details lazy-load only when panel expanded
- Panel collapse state stored in localStorage to avoid flicker on page load

---

## Migration Notes

- No database migrations required
- Feature is additive—existing Budget page remains functional
- Consider deprecating BudgetAlert component once panel is stable

---

## Future Enhancements

- Drill-down: Click category to see transactions filtered to that category
- Quick category toggle: Show/hide categories without budget from panel
- Budget templates: Save/apply budget templates across months
- Budget variance alerts: Notify when spending velocity exceeds expected pace

---

## References

- [046.1-calendar-centric-navigation-audit.md](046.1-calendar-centric-navigation-audit.md) - Audit identifying this gap
- [Budget.razor](../src/BudgetExperiment.Client/Pages/Budget.razor) - Existing budget page implementation
- [BudgetGoal.cs](../src/BudgetExperiment.Domain/Budgeting/BudgetGoal.cs) - Domain entity
- [BudgetProgressDto.cs](../src/BudgetExperiment.Contracts/Dtos/BudgetProgressDto.cs) - Progress DTO

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-01-26 | Initial stub | @becauseimclever |
| 2026-02-01 | Fleshed out full specification | @github-copilot |
