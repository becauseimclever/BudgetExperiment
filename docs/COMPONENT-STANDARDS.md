# Component Design Standards

> **Version:** 1.0  
> **Last Updated:** 2026-02-01  
> **Related Feature:** [045-ui-component-refactor-and-library.md](045-ui-component-refactor-and-library.md)

This document defines the naming conventions, parameter patterns, and design standards for Blazor components in the BudgetExperiment.Client project. All new components must follow these standards, and existing components should be migrated incrementally.

---

## 1. Component Tiers

| Tier | Purpose | Domain Dependencies | Library Candidate |
|------|---------|---------------------|-------------------|
| **Tier 1** | Atomic UI primitives (Button, Modal, Icon, etc.) | None | ✅ Yes |
| **Tier 2** | Composite patterns (FormField, ConfirmDialog, etc.) | None | ✅ Yes |
| **Tier 3** | Domain components (TransactionForm, AccountCard, etc.) | Domain/Contracts | ❌ No |

**Location:** Tier 1 and 2 components belong in `Components/Common/`. Tier 3 components go in domain-specific folders (`Display/`, `Forms/`, `Calendar/`, etc.).

---

## 2. Parameter Naming Conventions

### 2.1 Boolean Parameters

| Pattern | Use Case | Examples |
|---------|----------|----------|
| `Is*` | State or condition | `IsVisible`, `IsDisabled`, `IsLoading`, `IsSubmitting`, `IsProcessing` |
| `Is*Visible` | Visibility of sub-elements | `IsCloseButtonVisible`, `IsDateVisible`, `IsActionsVisible` |
| `Should*` | Behavioral flags | `ShouldCloseOnOverlayClick`, `ShouldAutoFocus` |

**❌ Avoid:**
- `Show*` prefix (use `Is*Visible` instead)
- Bare adjectives without prefix (`Visible`, `Disabled`, `Compact`) – always prefix with `Is*`

### 2.2 EventCallback Parameters

| Pattern | Examples |
|---------|----------|
| `On{Event}` | `OnClick`, `OnClose`, `OnSubmit`, `OnCancel`, `OnChange` |
| `On{Subject}{Event}` | `OnSortOrderChanged`, `OnItemSelected`, `OnTransactionDeleted` |

**❌ Avoid:**
- `{Subject}Changed` without `On*` prefix
- `Handle*` prefix (use for private methods only)

### 2.3 Size and Variant Parameters

Always use **enum types** for size and variant parameters.

```csharp
// ✅ Good: Enum-based sizing
[Parameter]
public ButtonSize Size { get; set; } = ButtonSize.Medium;

// ❌ Bad: String-based sizing
[Parameter]
public string Size { get; set; } = "medium";

// ❌ Bad: Integer sizing (acceptable only for pixel-precise values like Icon)
[Parameter]
public int Size { get; set; } = 20;
```

### 2.4 Content Parameters

| Parameter | Type | Use Case |
|-----------|------|----------|
| `ChildContent` | `RenderFragment?` | Primary content slot |
| `{Name}Content` | `RenderFragment?` | Named slots (`HeaderContent`, `FooterContent`, `ActionsContent`) |
| `Title` | `string` | Text-only title |
| `Label` | `string` | Form field labels |
| `Message` | `string` | Notification/alert messages |

### 2.5 Additional Attributes

Capture unmatched attributes to allow HTML attribute passthrough:

```csharp
[Parameter(CaptureUnmatchedValues = true)]
public Dictionary<string, object>? AdditionalAttributes { get; set; }
```

---

## 3. Enum Definitions

All component enums should be defined in `Components/Common/ComponentEnums.cs`.

### 3.1 Current Enums

```csharp
public enum ModalSize { Small, Medium, Large }
public enum SpinnerSize { Small, Medium, Large }
```

### 3.2 Enums to Add

```csharp
public enum ButtonSize { Small, Medium, Large }
public enum ButtonVariant { Primary, Secondary, Success, Danger, Warning, Ghost, Outline }
public enum BadgeVariant { Default, Success, Warning, Danger, Info }
public enum IconSize { Small, Medium, Large, ExtraLarge }
public enum AlertVariant { Info, Success, Warning, Danger }
```

---

## 4. Standard Component Structure

### 4.1 File Organization

Each Tier 1/2 component should follow this structure:

```razor
@* ComponentName.razor - Brief description *@

<div class="component-name @VariantClass @SizeClass @AdditionalClasses"
     @attributes="AdditionalAttributes">
    @* Component markup *@
</div>

@code {
    // Parameters grouped by purpose

    // 1. Content parameters
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    // 2. Appearance parameters (variant, size, etc.)
    [Parameter]
    public ComponentVariant Variant { get; set; } = ComponentVariant.Default;

    [Parameter]
    public ComponentSize Size { get; set; } = ComponentSize.Medium;

    // 3. State parameters (booleans)
    [Parameter]
    public bool IsDisabled { get; set; }

    [Parameter]
    public bool IsLoading { get; set; }

    // 4. Event callbacks
    [Parameter]
    public EventCallback OnClick { get; set; }

    // 5. Additional attributes (always last)
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    // 6. Computed properties
    private string VariantClass => Variant switch { ... };
    private string SizeClass => Size switch { ... };
}
```

### 4.2 XML Documentation

All public parameters must have XML documentation:

```csharp
/// <summary>
/// Gets or sets a value indicating whether the button is disabled.
/// </summary>
[Parameter]
public bool IsDisabled { get; set; }
```

---

## 5. CSS Class Naming

### 5.1 Component Root Classes

Use lowercase kebab-case matching the component name:

| Component | Root Class |
|-----------|------------|
| `Button.razor` | `.btn` |
| `Modal.razor` | `.modal-dialog` |
| `FormField.razor` | `.form-group` |
| `Badge.razor` | `.badge` |

### 5.2 Variant/Size Modifier Classes

Use BEM-like suffixes:

```css
/* Size modifiers */
.btn-sm { }
.btn-md { }
.btn-lg { }

/* Variant modifiers */
.btn-primary { }
.btn-secondary { }
.btn-danger { }
```

### 5.3 State Classes

```css
.is-disabled { }
.is-loading { }
.is-active { }
.has-error { }
```

---

## 6. Tier 1 Component Catalog

| Component | Status | CSS File |
|-----------|--------|----------|
| `Button.razor` | 🆕 Planned | `buttons.css` |
| `Badge.razor` | 🆕 Planned | `badges.css` |
| `Card.razor` | 🆕 Planned | `cards.css` |
| `EmptyState.razor` | 🆕 Planned | `empty-state.css` |
| `FormField.razor` | 🆕 Planned | `forms.css` |
| `Icon.razor` | ✅ Exists | N/A (inline SVG) |
| `LoadingSpinner.razor` | ✅ Exists | `loading.css` |
| `Modal.razor` | ✅ Exists | `modal.css` |
| `ErrorAlert.razor` | ✅ Exists | `alerts.css` |
| `ConfirmDialog.razor` | ✅ Exists | Uses Modal |
| `PageHeader.razor` | ✅ Exists | `page-header.css` |
| `ThemeToggle.razor` | ✅ Exists | `theme.css` |

---

## 7. Migration Checklist

When standardizing an existing component:

- [ ] Rename `Show*` parameters to `Is*Visible`
- [ ] Add `Is*` prefix to bare boolean parameters
- [ ] Rename `{Event}Changed` callbacks to `On{Event}Changed`
- [ ] Convert magic strings to enums
- [ ] Add `AdditionalAttributes` parameter if missing
- [ ] Add XML documentation for all parameters
- [ ] Update consuming components/pages

---

## 8. Current Audit: Parameters Requiring Migration

Based on the audit of 29 components (2026-02-01):

### 8.1 `Show*` → `Is*Visible` Migrations (12 parameters)

| Component | Current | Target |
|-----------|---------|--------|
| Modal | `ShowCloseButton` | `IsCloseButtonVisible` |
| PageHeader | `ShowBackButton` | `IsBackButtonVisible` |
| ThemeToggle | `ShowLabel` | `IsLabelVisible` |
| CategoryBudgetCard | `ShowEditButton` | `IsEditButtonVisible` |
| MoneyDisplay | `ShowColor` | `IsColorCoded` |
| MoneyDisplay | `ShowPositiveSign` | `ShouldShowPositiveSign` |
| ScopeBadge | `ShowLabel` | `IsLabelVisible` |
| TransactionTable | `ShowDate` | `IsDateVisible` |
| TransactionTable | `ShowActions` | `IsActionsVisible` |
| TransactionTable | `ShowBalance` | `IsBalanceVisible` |
| CategoryForm | `ShowSortOrder` | `IsSortOrderVisible` |
| TransactionForm | `ShowAccountSelector` | `IsAccountSelectorVisible` |

### 8.2 Missing `Is*` Prefix (3 parameters)

| Component | Current | Target |
|-----------|---------|--------|
| LoadingSpinner | `FullPage` | `IsFullPage` |
| Modal | `CloseOnOverlayClick` | `ShouldCloseOnOverlayClick` |
| BudgetProgressBar | `Compact` | `IsCompact` |

### 8.3 Callback Naming (1 parameter)

| Component | Current | Target |
|-----------|---------|--------|
| CategoryForm | `SortOrderChanged` | `OnSortOrderChanged` |

### 8.4 String → Enum Conversions (3 parameters)

| Component | Parameter | Suggested Enum |
|-----------|-----------|----------------|
| BudgetProgressBar | `Status` | `BudgetStatus` |
| ScopeBadge | `Scope` | `AccountScope` |
| Icon | `Size` (int) | Keep `int` for pixel precision |

---

## 9. Testing Requirements

### 9.1 Required Tests for Tier 1/2 Components

Each component must have bUnit tests covering:

1. **Rendering:** Component renders without errors
2. **Parameters:** Each parameter affects output correctly
3. **Events:** EventCallbacks fire with correct arguments
4. **State:** State changes (loading, disabled) render correctly
5. **Accessibility:** ARIA attributes present where applicable

### 9.2 Test File Naming

```
tests/BudgetExperiment.Client.Tests/Components/Common/ButtonTests.cs
tests/BudgetExperiment.Client.Tests/Components/Common/ModalTests.cs
```

---

## 10. Examples

### 10.1 Button Component (Target Implementation)

```razor
@* Button.razor - Standardized button component *@

<button class="btn @VariantClass @SizeClass"
        type="@Type"
        disabled="@(IsDisabled || IsLoading)"
        @onclick="HandleClick"
        @attributes="AdditionalAttributes">
    @if (IsLoading)
    {
        <LoadingSpinner Size="SpinnerSize.Small" />
    }
    else if (!string.IsNullOrEmpty(IconLeft))
    {
        <Icon Name="@IconLeft" Size="16" />
    }
    @ChildContent
    @if (!string.IsNullOrEmpty(IconRight))
    {
        <Icon Name="@IconRight" Size="16" />
    }
</button>

@code {
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public ButtonVariant Variant { get; set; } = ButtonVariant.Primary;
    [Parameter] public ButtonSize Size { get; set; } = ButtonSize.Medium;
    [Parameter] public string Type { get; set; } = "button";
    [Parameter] public bool IsDisabled { get; set; }
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public string? IconLeft { get; set; }
    [Parameter] public string? IconRight { get; set; }
    [Parameter] public EventCallback OnClick { get; set; }
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    private string VariantClass => Variant switch
    {
        ButtonVariant.Secondary => "btn-secondary",
        ButtonVariant.Success => "btn-success",
        ButtonVariant.Danger => "btn-danger",
        ButtonVariant.Warning => "btn-warning",
        ButtonVariant.Ghost => "btn-ghost",
        ButtonVariant.Outline => "btn-outline",
        _ => "btn-primary"
    };

    private string SizeClass => Size switch
    {
        ButtonSize.Small => "btn-sm",
        ButtonSize.Large => "btn-lg",
        _ => string.Empty
    };

    private async Task HandleClick() => await OnClick.InvokeAsync();
}
```

---

## Revision History

| Date | Version | Changes |
|------|---------|---------|
| 2026-02-01 | 1.0 | Initial standards based on component audit |
