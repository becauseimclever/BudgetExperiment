# Feature 045: UI Component Refactor and Library Preparation
> **Status:** � In Progress

## Overview

Refactor the Blazor UI to establish consistent component patterns, consolidate design system usage, and prepare the architecture for potential extraction of a reusable UI component library. This feature builds on the completed theming work from feature 044, which established a solid CSS variable foundation.

## Problem Statement

While the design system infrastructure (CSS variables, tokens, component styles) is well-established, the Razor components themselves have inconsistent patterns, varying levels of abstraction, and mixed approaches to prop design. This makes maintenance harder and blocks future library extraction.

### Current State (Audit: 2026-02-01)

**✅ Strengths (from Feature 044 work):**
- Robust CSS variable-based theming with `tokens.css` foundation
- 9 themes working: system, light, dark, vscode-dark, monopoly, win95, macos, geocities, crayons
- Well-organized design system in `wwwroot/css/design-system/` with:
  - 20+ component CSS files (buttons, cards, forms, tables, modals, etc.)
  - Token-based spacing, typography, and color scales
- Component folder structure exists: `Components/{AI,Auth,Calendar,Charts,Chat,Common,Display,Forms,Import,Navigation,Reconciliation}`
- Some well-designed components: `Modal`, `LoadingSpinner`, `MoneyDisplay`, `Icon`

**⚠️ Issues Identified:**

1. **Inconsistent Component Patterns:**
   - Some components use enums for variants (`SpinnerSize`), others use strings
   - Parameter naming varies: `IsVisible` vs `Visible` vs `Show`
   - Some have size classes, others don't support sizing

2. **Mixed Abstraction Levels:**
   - `Common/` has 8 generic components
   - `Forms/` has 13 domain-specific form components that could share patterns
   - No base component classes or shared abstractions

3. **CSS/Razor Coupling Issues:**
   - Most components rely on design system CSS classes (`btn`, `card`, `form-control`)
   - Some have inline styles or component-specific CSS
   - Class naming inconsistencies between CSS and Razor usage

4. **Documentation Gaps:**
   - `Components/README.md` documents only `FinancialItemDialog`
   - No component catalog or API documentation
   - No usage examples for reusable components

5. **Button Pattern Fragmentation:**
   - No `Button.razor` component; raw `<button class="btn ...">` usage everywhere
   - Button variants applied via CSS classes directly in pages

6. **Form Components:**
   - 13 form components in `Forms/` folder
   - Each handles its own validation display
   - No shared `FormField`, `InputField`, or validation components

### Target State

- **Tier 1 (Core Components):** Generic, domain-agnostic, library-ready
  - Button, Icon, Modal, LoadingSpinner, ErrorAlert, Badge, Card, etc.
- **Tier 2 (Composite Components):** Built on Tier 1, still generic
  - ConfirmDialog, FormField, DataTable, EmptyState, PageHeader
- **Tier 3 (Domain Components):** App-specific, not library candidates
  - TransactionForm, AccountForm, CategoryCard, MoneyDisplay, etc.
- All Tier 1/2 components follow consistent patterns
- Component API documented in `Components/README.md`
- Clear path to extract `BudgetExperiment.Components` package

---

## User Stories

### UI Consistency and Reusability

#### US-045-001: Consistent button component
**As a** developer  
**I want to** use a `Button` component with standardized props  
**So that** button styling is consistent and easy to maintain

**Acceptance Criteria:**
- [ ] `Button.razor` component in `Components/Common/`
- [ ] Supports: `Variant` (primary, secondary, success, danger, ghost), `Size` (sm, md, lg)
- [ ] Supports: `IsLoading`, `IsDisabled`, `IconLeft`, `IconRight`
- [ ] All pages migrate from raw `<button>` to `<Button>`

#### US-045-002: Unified form field component
**As a** developer  
**I want to** use a `FormField` wrapper component  
**So that** labels, inputs, and validation messages are consistently styled

**Acceptance Criteria:**
- [ ] `FormField.razor` in `Components/Common/`
- [ ] Supports: `Label`, `ChildContent`, `ValidationMessage`, `IsRequired`, `HelpText`
- [ ] Works with `<input>`, `<select>`, `<textarea>` and custom inputs
- [ ] Form components in `Forms/` refactored to use `FormField`

#### US-045-003: Component API documentation
**As a** developer  
**I want to** reference component documentation  
**So that** I know available props and usage patterns

**Acceptance Criteria:**
- [ ] `Components/README.md` updated with all Tier 1 components
- [ ] Each component lists: parameters, events, CSS dependencies, usage example
- [ ] Documentation follows consistent template

#### US-045-004: Standardized parameter naming
**As a** developer  
**I want to** components to use consistent parameter names  
**So that** the API is predictable and learnable

**Acceptance Criteria:**
- [ ] Visibility: Always `IsVisible` (not `Visible`, `Show`, `IsOpen`)
- [ ] Disabled: Always `IsDisabled` (not `Disabled`)
- [ ] Loading: Always `IsLoading`
- [ ] Size: Always `Size` with enum type per component
- [ ] Callbacks: `On{Event}` pattern (e.g., `OnClick`, `OnClose`, `OnChange`)

#### US-045-005: Library extraction readiness
**As a** developer  
**I want to** Tier 1 components to have zero domain dependencies  
**So that** they can be extracted to a separate package

**Acceptance Criteria:**
- [ ] Tier 1 components reference only CSS classes and standard Blazor types
- [ ] No references to `BudgetExperiment.Domain`, `BudgetExperiment.Contracts`, or app services
- [ ] CSS for Tier 1 components isolated in `design-system/components/`

---

## Technical Design

### Component Tier Classification

| Tier | Purpose | Domain Dependencies | Library Candidate |
|------|---------|---------------------|-------------------|
| Tier 1 | Atomic UI primitives | None | ✅ Yes |
| Tier 2 | Composite patterns | None | ✅ Yes |
| Tier 3 | Domain components | Domain/Contracts | ❌ No |

### Tier 1 Components (New/Refactored)

| Component | Status | Notes |
|-----------|--------|-------|
| `Button.razor` | 🆕 New | Replace all raw `<button>` usage |
| `Icon.razor` | ✅ Exists | Review for consistency |
| `Modal.razor` | ✅ Exists | Already well-designed |
| `LoadingSpinner.razor` | ✅ Exists | Has `SpinnerSize` enum ✓ |
| `ErrorAlert.razor` | ✅ Exists | Review props |
| `Badge.razor` | 🆕 New | For status indicators |
| `Card.razor` | 🆕 New | Wrap card pattern |
| `EmptyState.razor` | 🆕 New | Consistent empty state pattern |

### Tier 2 Components (New/Refactored)

| Component | Status | Notes |
|-----------|--------|-------|
| `ConfirmDialog.razor` | ✅ Exists | Review for consistency |
| `FormField.razor` | 🆕 New | Label + input + validation wrapper |
| `PageHeader.razor` | ✅ Exists | Review props |
| `DataTable.razor` | 🔄 Consider | Abstract from `TransactionTable` |
| `Tabs.razor` | 🔄 Consider | If needed |

### Standardized Parameter Patterns

```csharp
// Size enum pattern (per component)
public enum ButtonSize { Small, Medium, Large }

// Standard parameter names
[Parameter] public bool IsVisible { get; set; }
[Parameter] public bool IsDisabled { get; set; }
[Parameter] public bool IsLoading { get; set; }
[Parameter] public ButtonSize Size { get; set; } = ButtonSize.Medium;
[Parameter] public EventCallback OnClick { get; set; }
[Parameter] public RenderFragment? ChildContent { get; set; }
```

### Button Component Design

```razor
@* Button.razor *@
<button class="btn @VariantClass @SizeClass @AdditionalClasses"
        disabled="@(IsDisabled || IsLoading)"
        @onclick="OnClick"
        @attributes="AdditionalAttributes">
    @if (IsLoading)
    {
        <LoadingSpinner Size="SpinnerSize.Small" />
    }
    else if (IconLeft != null)
    {
        <Icon Name="@IconLeft" Size="16" />
    }
    @ChildContent
    @if (IconRight != null)
    {
        <Icon Name="@IconRight" Size="16" />
    }
</button>

@code {
    [Parameter] public ButtonVariant Variant { get; set; } = ButtonVariant.Primary;
    [Parameter] public ButtonSize Size { get; set; } = ButtonSize.Medium;
    [Parameter] public bool IsDisabled { get; set; }
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public string? IconLeft { get; set; }
    [Parameter] public string? IconRight { get; set; }
    [Parameter] public string? AdditionalClasses { get; set; }
    [Parameter] public EventCallback OnClick { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }
}
```

### FormField Component Design

```razor
@* FormField.razor *@
<div class="form-group @(HasError ? "has-error" : "")">
    @if (!string.IsNullOrEmpty(Label))
    {
        <label class="form-label">
            @Label
            @if (IsRequired) { <span class="required-indicator">*</span> }
        </label>
    }
    @ChildContent
    @if (!string.IsNullOrEmpty(ValidationMessage))
    {
        <span class="validation-message">@ValidationMessage</span>
    }
    @if (!string.IsNullOrEmpty(HelpText))
    {
        <span class="help-text">@HelpText</span>
    }
</div>
```

### Folder Structure (Target)

```
Components/
├── README.md                 # Component catalog & API docs
├── _ComponentImports.razor   # Shared imports for components
├── Common/                   # Tier 1: Atomic primitives
│   ├── Button.razor
│   ├── Badge.razor
│   ├── Card.razor
│   ├── ComponentEnums.cs     # All component enums
│   ├── ConfirmDialog.razor
│   ├── EmptyState.razor
│   ├── ErrorAlert.razor
│   ├── FormField.razor
│   ├── Icon.razor
│   ├── LoadingSpinner.razor
│   ├── Modal.razor
│   ├── PageHeader.razor
│   └── ThemeToggle.razor
├── Display/                  # Tier 3: Domain display components
├── Forms/                    # Tier 3: Domain form components
├── Calendar/                 # Tier 3: Calendar-specific
├── Charts/                   # Tier 3: Charting
└── ...                       # Other domain folders
```

### Domain Model Changes

- No changes required

### API Endpoints

- No changes required

### Database Changes

- No changes required

---

## Implementation Plan

### Phase 1: Audit and Standards Definition ✅

**Objective:** Document current state, define standards, create pattern templates

**Tasks:**
- [x] Complete audit of all 50+ components (parameters, patterns, CSS usage)
- [x] Define parameter naming conventions document
- [x] Create component template for new Tier 1 components
- [x] Update `Components/README.md` structure
- [x] Add new enums to `ComponentEnums.cs` (ButtonSize, ButtonVariant, BadgeVariant, AlertVariant)

**Deliverables:**
- `docs/COMPONENT-STANDARDS.md` - naming conventions and patterns ✅
- Updated `Components/README.md` template ✅

**Commit:**
- docs(client): define component design standards and patterns

---

### Phase 2: Core Component Creation

**Objective:** Create new Tier 1 components with consistent patterns

**Tasks:**
- [ ] Create `Button.razor` with full variant/size support
- [ ] Create `Badge.razor` component
- [ ] Create `Card.razor` wrapper component
- [ ] Create `EmptyState.razor` component
- [ ] Create `FormField.razor` component
- [ ] Add component enums to `ComponentEnums.cs`

**Commit:**
- feat(client): add Button, Badge, Card, EmptyState, FormField components

---

### Phase 3: Existing Component Standardization

**Objective:** Refactor existing Tier 1/2 components for consistency

**Tasks:**
- [ ] Standardize `Modal.razor` parameters (already good, verify)
- [ ] Standardize `LoadingSpinner.razor` (already has enum, verify naming)
- [ ] Standardize `ErrorAlert.razor` parameters
- [ ] Standardize `ConfirmDialog.razor` parameters
- [ ] Standardize `PageHeader.razor` parameters
- [ ] Standardize `Icon.razor` parameters

**Commit:**
- refactor(client): standardize existing component parameters

---

### Phase 4: Page Migration - Buttons

**Objective:** Replace raw `<button>` elements with `<Button>` component

**Tasks:**
- [ ] Migrate `Pages/Accounts.razor` buttons
- [ ] Migrate `Pages/Categories.razor` buttons
- [ ] Migrate `Pages/Recurring.razor` buttons
- [ ] Migrate `Pages/Rules.razor` buttons
- [ ] Migrate remaining pages
- [ ] Migrate `Layout/MainLayout.razor` buttons
- [ ] Migrate form component buttons

**Commit:**
- refactor(client): migrate pages to Button component

---

### Phase 5: Form Component Refactoring

**Objective:** Use `FormField` in domain form components

**Tasks:**
- [ ] Refactor `AccountForm.razor` to use `FormField`
- [ ] Refactor `CategoryForm.razor` to use `FormField`
- [ ] Refactor `TransactionForm.razor` to use `FormField`
- [ ] Refactor `RuleForm.razor` to use `FormField`
- [ ] Refactor remaining form components (9 more)

**Commit:**
- refactor(client): use FormField in form components

---

### Phase 6: Documentation and Component Catalog

**Objective:** Complete documentation for all Tier 1/2 components

**Tasks:**
- [ ] Document all Tier 1 components in README.md
- [ ] Add usage examples for each component
- [ ] Document CSS dependencies
- [ ] Add migration guide for existing patterns

**Commit:**
- docs(client): complete component catalog documentation

---

### Phase 7: Library Extraction Prep (Optional)

**Objective:** Prepare for future `BudgetExperiment.Components` package

**Tasks:**
- [ ] Verify Tier 1/2 components have zero domain dependencies
- [ ] Create `BudgetExperiment.Components.csproj` (empty, for future)
- [ ] Document extraction process in `docs/COMPONENT-LIBRARY.md`
- [ ] Add build validation that Tier 1 has no domain refs

**Commit:**
- chore(client): prepare for component library extraction

---

## Testing Strategy

### Unit/Integration Tests (bUnit)

- [ ] `Button.razor` renders all variants and sizes correctly
- [ ] `Button.razor` handles loading and disabled states
- [ ] `FormField.razor` displays label, validation, help text
- [ ] `Modal.razor` shows/hides based on `IsVisible`
- [ ] `Badge.razor` applies correct variant classes
- [ ] Component parameter changes trigger re-render

### Visual Regression (Manual)

- [ ] All button variants visible in all 9 themes
- [ ] Form fields render consistently across themes
- [ ] Modals/dialogs display correctly
- [ ] Loading spinners animate properly

### Manual Testing Checklist

- [ ] Create account flow uses new Button component
- [ ] Edit transaction flow uses FormField
- [ ] All dialogs open/close correctly
- [ ] Theme switching doesn't break component styles
- [ ] Keyboard navigation works (focus states)

---

## Migration Notes

### Breaking Changes

- None expected; all changes are additive then migration

### Migration Path

1. New components added alongside existing patterns
2. Pages/components migrated incrementally per phase
3. Old inline patterns deprecated but not removed until fully migrated

### Rollback Plan

- Each phase is independently revertible
- Feature flag not needed (incremental migration)

---

## Security Considerations

- Button component must preserve `type="button"` vs `type="submit"` handling
- FormField must not interfere with form validation/submission
- No user input handling changes

---

## Performance Considerations

- **Minimal impact expected**
- Component abstraction adds negligible overhead
- CSS remains unchanged (same classes, same styles)
- Consider `@key` for list-rendered components

---

## Risks and Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Button migration breaks form submissions | Medium | High | Test all forms after migration |
| CSS class changes break theming | Low | High | No CSS changes planned |
| Scope creep to Tier 3 refactoring | Medium | Medium | Strict phase boundaries |
| Time estimate underrun | Medium | Low | Prioritize Phases 1-4 |

---

## Effort Estimate

| Phase | Effort | Priority |
|-------|--------|----------|
| Phase 1: Standards | 2-3 hours | P0 |
| Phase 2: Core components | 4-6 hours | P0 |
| Phase 3: Standardization | 2-3 hours | P1 |
| Phase 4: Button migration | 4-6 hours | P1 |
| Phase 5: Form refactoring | 4-6 hours | P2 |
| Phase 6: Documentation | 2-3 hours | P1 |
| Phase 7: Library prep | 2-3 hours | P3 |

**Total: 20-30 hours**

---

## Future Enhancements

- Extract `BudgetExperiment.Components` as separate NuGet package
- Add Storybook-like component preview page (`/component-gallery`)
- Consider source generators for component boilerplate
- Add accessibility testing (axe-core integration)
- Add visual regression testing (Playwright screenshots)

---

## Dependencies

- Feature 044 (UI Theme Rework) - ✅ Complete
- Design system CSS - ✅ In place

---

## References

- [044-ui-theme-rework-and-theming.md](044-ui-theme-rework-and-theming.md)
- [THEMING.md](THEMING.md)
- [Blazor Component Best Practices](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/)
- [CSS Design Tokens](https://www.w3.org/community/design-tokens/)

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-02-01 | Expanded with audit findings and detailed implementation plan | @github-copilot |
| 2026-01-26 | Initial draft | @github-copilot |
