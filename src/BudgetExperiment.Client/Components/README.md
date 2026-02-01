# Component Library

> **Standards:** See [COMPONENT-STANDARDS.md](../../../docs/COMPONENT-STANDARDS.md) for naming conventions and patterns.

This document catalogs the reusable UI components in BudgetExperiment.Client.

---

## Component Tiers

| Tier | Location | Domain Dependencies | Description |
|------|----------|---------------------|-------------|
| **Tier 1** | `Common/` | None | Atomic UI primitives (Button, Modal, Icon) |
| **Tier 2** | `Common/` | None | Composite patterns (FormField, ConfirmDialog) |
| **Tier 3** | `Display/`, `Forms/`, etc. | Yes | Domain-specific components |

---

## Tier 1 Components (Common/)

### Icon

SVG icon component using Lucide icon paths.

**Location:** `Common/Icon.razor`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Name` | `string` | `""` | Icon name (e.g., "plus", "edit", "trash") |
| `Size` | `int` | `20` | Icon size in pixels |
| `Class` | `string?` | `null` | Additional CSS classes |
| `StrokeWidth` | `double` | `2` | SVG stroke width |
| `Title` | `string?` | `null` | Accessible title/tooltip |

**Usage:**
```razor
<Icon Name="plus" Size="24" Title="Add item" />
```

---

### LoadingSpinner

Loading indicator with optional message.

**Location:** `Common/LoadingSpinner.razor`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Message` | `string?` | `null` | Loading message to display |
| `FullPage` | `bool` | `false` | Whether to display as full-page overlay |
| `Size` | `SpinnerSize` | `Medium` | Spinner size (Small, Medium, Large) |

**Usage:**
```razor
<LoadingSpinner Message="Loading accounts..." Size="SpinnerSize.Large" />
```

---

### Modal

Reusable modal dialog wrapper.

**Location:** `Common/Modal.razor`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `IsVisible` | `bool` | `false` | Controls modal visibility |
| `Title` | `string` | `""` | Modal title |
| `Size` | `ModalSize` | `Medium` | Modal size (Small, Medium, Large) |
| `ChildContent` | `RenderFragment?` | `null` | Main content |
| `FooterContent` | `RenderFragment?` | `null` | Footer content (buttons) |
| `ShowCloseButton` | `bool` | `true` | Show X close button |
| `CloseOnOverlayClick` | `bool` | `true` | Close when clicking overlay |
| `OnClose` | `EventCallback` | - | Called when modal closes |

**Usage:**
```razor
<Modal IsVisible="@showModal" Title="Edit Account" OnClose="CloseModal">
    <p>Modal content here</p>
    <FooterContent>
        <button class="btn btn-primary">Save</button>
    </FooterContent>
</Modal>
```

---

### ErrorAlert

Error display with optional retry button.

**Location:** `Common/ErrorAlert.razor`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Message` | `string?` | `null` | Error message |
| `Details` | `string?` | `null` | Additional error details |
| `IsDismissible` | `bool` | `true` | Can be dismissed |
| `IsRetrying` | `bool` | `false` | Retry in progress |
| `OnRetry` | `EventCallback` | - | Retry callback |
| `OnDismiss` | `EventCallback` | - | Dismiss callback |

**Usage:**
```razor
<ErrorAlert Message="@errorMessage" OnRetry="RetryLoad" />
```

---

### ConfirmDialog

Confirmation dialog for destructive actions.

**Location:** `Common/ConfirmDialog.razor`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `IsVisible` | `bool` | `false` | Controls visibility |
| `Title` | `string` | `"Confirm"` | Dialog title |
| `Message` | `string` | `"Are you sure..."` | Confirmation message |
| `ConfirmText` | `string` | `"Delete"` | Confirm button text |
| `CancelText` | `string` | `"Cancel"` | Cancel button text |
| `IsProcessing` | `bool` | `false` | Processing state |
| `OnConfirm` | `EventCallback` | - | Confirm callback |
| `OnCancel` | `EventCallback` | - | Cancel callback |

**Usage:**
```razor
<ConfirmDialog IsVisible="@showConfirm"
               Title="Delete Account"
               Message="This cannot be undone."
               OnConfirm="DeleteAccount"
               OnCancel="CloseConfirm" />
```

---

### PageHeader

Page header with title, optional back button, and actions slot.

**Location:** `Common/PageHeader.razor`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Title` | `string` | `""` | Page title |
| `Subtitle` | `string?` | `null` | Optional subtitle |
| `ShowBackButton` | `bool` | `false` | Show back navigation |
| `Actions` | `RenderFragment?` | `null` | Action buttons slot |
| `OnBack` | `EventCallback` | - | Back button callback |

**Usage:**
```razor
<PageHeader Title="Accounts" Subtitle="Manage your accounts">
    <Actions>
        <button class="btn btn-primary">Add Account</button>
    </Actions>
</PageHeader>
```

---

### ThemeToggle

Theme switcher component.

**Location:** `Common/ThemeToggle.razor`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `ShowLabel` | `bool` | `false` | Show theme label text |

**Usage:**
```razor
<ThemeToggle ShowLabel="true" />
```

---

### Button

Standardized button component with variant and size support.

**Location:** `Common/Button.razor`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Variant` | `ButtonVariant` | `Primary` | Button style (Primary, Secondary, Success, Danger, Warning, Ghost, Outline) |
| `Size` | `ButtonSize` | `Medium` | Button size (Small, Medium, Large) |
| `Type` | `string` | `"button"` | Button type (button, submit, reset) |
| `IsDisabled` | `bool` | `false` | Disabled state |
| `IsLoading` | `bool` | `false` | Loading state with spinner |
| `IsBlock` | `bool` | `false` | Full width button |
| `IconLeft` | `string?` | `null` | Icon name for left side |
| `IconRight` | `string?` | `null` | Icon name for right side |
| `OnClick` | `EventCallback` | - | Click handler |
| `ChildContent` | `RenderFragment?` | `null` | Button label content |

**Usage:**
```razor
<Button Variant="ButtonVariant.Success" OnClick="Save">Save Changes</Button>
<Button Variant="ButtonVariant.Danger" Size="ButtonSize.Small" IsLoading="@isSaving">Delete</Button>
<Button Variant="ButtonVariant.Ghost" IconLeft="plus">Add Item</Button>
```

---

### Badge

Status/label badge component with variants.

**Location:** `Common/Badge.razor`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Variant` | `BadgeVariant` | `Default` | Badge style (Default, Success, Warning, Danger, Info) |
| `Size` | `BadgeSize` | `Medium` | Badge size (Small, Medium, Large) |
| `Text` | `string?` | `null` | Badge text (alternative to ChildContent) |
| `Icon` | `string?` | `null` | Icon name |
| `ChildContent` | `RenderFragment?` | `null` | Badge content |

**Usage:**
```razor
<Badge Variant="BadgeVariant.Success" Text="Active" />
<Badge Variant="BadgeVariant.Warning">Pending</Badge>
```

---

### Card

Card container with optional header and footer.

**Location:** `Common/Card.razor`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Title` | `string?` | `null` | Card title (renders in header) |
| `Subtitle` | `string?` | `null` | Card subtitle |
| `HeaderContent` | `RenderFragment?` | `null` | Custom header content |
| `FooterContent` | `RenderFragment?` | `null` | Footer content |
| `ChildContent` | `RenderFragment?` | `null` | Body content |
| `AdditionalClasses` | `string?` | `null` | Extra CSS classes |

**Usage:**
```razor
<Card Title="Account Summary">
    <p>Account balance: $1,234.56</p>
    <FooterContent>
        <Button Variant="ButtonVariant.Secondary">View Details</Button>
    </FooterContent>
</Card>
```

---

### EmptyState

Empty state placeholder for lists and pages.

**Location:** `Common/EmptyState.razor`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Title` | `string` | `"No items found"` | Empty state title |
| `Description` | `string?` | `null` | Description text |
| `Icon` | `string?` | `null` | Icon name |
| `ChildContent` | `RenderFragment?` | `null` | Action content (buttons) |

**Usage:**
```razor
<EmptyState Title="No transactions"
            Description="Add your first transaction to get started."
            Icon="inbox">
    <Button Variant="ButtonVariant.Primary">Add Transaction</Button>
</EmptyState>
```

---

### FormField

Form field wrapper with label and validation.

**Location:** `Common/FormField.razor`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Label` | `string?` | `null` | Field label |
| `IsRequired` | `bool` | `false` | Shows required indicator |
| `ValidationMessage` | `string?` | `null` | Validation error message |
| `HelpText` | `string?` | `null` | Help text below input |
| `InputId` | `string?` | `null` | ID for label association |
| `ChildContent` | `RenderFragment?` | `null` | Input element |

**Usage:**
```razor
<FormField Label="Email Address" IsRequired="true" InputId="email"
           ValidationMessage="@emailError" HelpText="We'll never share your email.">
    <input type="email" id="email" class="form-control" @bind="email" />
</FormField>
```

---

## Enums (ComponentEnums.cs)

```csharp
public enum ModalSize { Small, Medium, Large }
public enum SpinnerSize { Small, Medium, Large }
public enum ButtonSize { Small, Medium, Large }
public enum ButtonVariant { Primary, Secondary, Success, Danger, Warning, Ghost, Outline }
public enum BadgeSize { Small, Medium, Large }
public enum BadgeVariant { Default, Success, Warning, Danger, Info }
public enum AlertVariant { Info, Success, Warning, Danger }
```

---

## CSS Dependencies

Components depend on the design system CSS files located in `wwwroot/css/design-system/`:

| Component | CSS File | Key Classes |
|-----------|----------|-------------|
| Button | `components/buttons.css` | `.btn`, `.btn-primary`, `.btn-secondary`, `.btn-success`, `.btn-danger`, `.btn-warning`, `.btn-ghost`, `.btn-outline`, `.btn-sm`, `.btn-lg`, `.btn-block` |
| Badge | `components/badges.css` | `.badge`, `.badge-default`, `.badge-success`, `.badge-warning`, `.badge-danger`, `.badge-info`, `.badge-sm`, `.badge-lg` |
| Card | `components/cards.css` | `.card`, `.card-header`, `.card-body`, `.card-footer`, `.card-title`, `.card-subtitle` |
| Modal | `components/modals.css` | `.modal-overlay`, `.modal`, `.modal-sm`, `.modal-lg`, `.modal-header`, `.modal-body`, `.modal-footer` |
| FormField | `components/forms.css` | `.form-group`, `.form-label`, `.form-control`, `.validation-message`, `.help-text`, `.required-indicator` |
| EmptyState | `components/empty-state.css` | `.empty-state`, `.empty-state-icon`, `.empty-state-title`, `.empty-state-description`, `.empty-state-actions` |
| LoadingSpinner | `components/spinners.css` | `.spinner`, `.spinner-sm`, `.spinner-lg`, `.loading-overlay` |
| ErrorAlert | `components/alerts.css` | `.alert`, `.alert-danger`, `.alert-dismissible` |

**Token Dependencies:** All components use CSS variables from `tokens.css` for colors, spacing, and typography.

---

## Migration Guide

### Migrating from Raw `<button>` to `<Button>`

**Before:**
```razor
<button class="btn btn-primary" @onclick="Save" disabled="@isSaving">
    @if (isSaving)
    {
        <span class="spinner-border spinner-border-sm"></span>
        <span>Saving...</span>
    }
    else
    {
        <span>Save</span>
    }
</button>
```

**After:**
```razor
<Button Variant="ButtonVariant.Primary" OnClick="Save" IsLoading="@isSaving">
    Save
</Button>
```

### Migrating to `FormField`

**Before:**
```razor
<div class="form-group">
    <label for="email">Email Address <span class="required">*</span></label>
    <input type="email" id="email" class="form-control" @bind="email" />
    @if (!string.IsNullOrEmpty(emailError))
    {
        <span class="validation-message">@emailError</span>
    }
    <span class="help-text">We'll never share your email.</span>
</div>
```

**After:**
```razor
<FormField Label="Email Address" IsRequired="true" InputId="email"
           ValidationMessage="@emailError" HelpText="We'll never share your email.">
    <input type="email" id="email" class="form-control" @bind="email" />
</FormField>
```

### Migrating to `EmptyState`

**Before:**
```razor
@if (!items.Any())
{
    <div class="text-center p-4">
        <p class="text-muted">No items found</p>
        <p class="text-muted small">Add your first item to get started.</p>
        <button class="btn btn-success" @onclick="AddItem">Add Item</button>
    </div>
}
```

**After:**
```razor
@if (!items.Any())
{
    <EmptyState Title="No items found"
                Description="Add your first item to get started."
                Icon="inbox">
        <Button Variant="ButtonVariant.Success" OnClick="AddItem">Add Item</Button>
    </EmptyState>
}
```

### Variant Mapping Reference

| Old CSS Class | Button Variant |
|---------------|----------------|
| `btn-primary` | `ButtonVariant.Primary` |
| `btn-secondary` | `ButtonVariant.Secondary` |
| `btn-success` | `ButtonVariant.Success` |
| `btn-danger` | `ButtonVariant.Danger` |
| `btn-warning` | `ButtonVariant.Warning` |
| `btn-ghost` | `ButtonVariant.Ghost` |
| `btn-outline-*` | `ButtonVariant.Outline` |

---

## Tier 3 Components

Domain-specific components are documented in their respective folders:

- **Display/** - Read-only display components (MoneyDisplay, TransactionTable, etc.)
- **Forms/** - Form components with validation (TransactionForm, AccountForm, etc.)
- **Calendar/** - Calendar-related components
- **Charts/** - Data visualization components
- **Import/** - CSV import components
- **Reconciliation/** - Transaction reconciliation components

---

## Legacy: Financial Item Dialog Component

## Overview

The `FinancialItemDialog` is a reusable Blazor component designed to handle add/edit operations for various financial entities including recurring schedules and adhoc transactions. This component follows the DRY principle by consolidating common dialog functionality.

## Features

- **Unified Model**: Uses `FinancialItemDialogModel` for all financial entities
- **Flexible Configuration**: Supports different field combinations via parameters
- **Validation**: Built-in client-side validation with error display
- **Responsive**: Works seamlessly across devices
- **FluentUI Integration**: Fully styled with Microsoft FluentUI components
- **Transaction Type Support**: Handles both income and expense transactions

## Usage Examples

### Recurring Schedules (handled by UnifiedScheduleDialog)
Use the `UnifiedScheduleDialog` component for recurring income and expense schedules.

### Adhoc Transactions Management
```razor
<FinancialItemDialog IsVisible="@showDialog"
                     Title="@(isEdit ? "Edit Transaction" : "Add New Transaction")"
                     NameLabel="Description"
                     NamePlaceholder="Enter transaction description..."
                     DateLabel="Transaction Date"
                     SaveButtonText="@(isEdit ? "Update Transaction" : "Add Transaction")"
                     ShowCategoryField="true"
                     ShowRecurrenceField="false"
                     ShowTransactionTypeField="true"
                     Model="@dialogModel"
                     OnCancel="CloseDialog"
                     OnSave="SaveTransaction"
                     OnDelete="DeleteTransaction" />
```

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `IsVisible` | `bool` | `false` | Controls dialog visibility |
| `Title` | `string` | `"Edit Item"` | Dialog title |
| `NameLabel` | `string` | `"Name"` | Label for the name/description field |
| `NamePlaceholder` | `string` | `"Enter name..."` | Placeholder for name field |
| `DateLabel` | `string` | `"Date"` | Label for the date field |
| `SaveButtonText` | `string` | `"Save"` | Text for the save button |
| `ShowCategoryField` | `bool` | `false` | Whether to show category field |
| `ShowRecurrenceField` | `bool` | `false` | Whether to show recurrence field |
| `ShowTransactionTypeField` | `bool` | `false` | Whether to show transaction type radio buttons |
| `RecurrenceOptions` | `List<string>` | `["Monthly"]` | Available recurrence options |
| `Model` | `FinancialItemDialogModel` | `new()` | The data model |
| `OnCancel` | `EventCallback` | - | Callback when cancel is clicked |
| `OnSave` | `EventCallback` | - | Callback when save is clicked |
| `OnDelete` | `EventCallback` | - | Callback when delete is clicked |

## FinancialItemDialogModel

### Properties
- `Id` (Guid?) - Entity ID (null for new items)
- `IsEditMode` (bool) - Whether in edit mode
- `Name` (string) - Name/description of the item
- `Currency` (string) - Currency code (default: "USD")
- `Amount` (decimal?) - Monetary amount
- `DateTime` (DateTime) - Date/time value
- `Category` (string?) - Optional category
- `Recurrence` (string) - Recurrence pattern (for schedules)
- `TransactionType` (TransactionType) - Type of transaction (Income or Expense)
- `IsSaving` (bool) - Loading state during save
- `IsDeleting` (bool) - Loading state during delete

### Methods
- `Validate(bool requireName = true)` - Validates the model
- `ClearErrors()` - Clears all validation errors

## Architecture Benefits

1. **DRY Principle**: Eliminates duplicate dialog code across pages
2. **Consistency**: Uniform UI/UX across all financial dialogs
3. **Maintainability**: Single point of change for dialog behavior
4. **Extensibility**: Easy to add new field types or validation rules
5. **Type Safety**: Strongly-typed model with compile-time checking
6. **Unified Approach**: Single dialog handles both income and expense transactions

## File Locations

- **Component**: `src/BudgetExperiment.Client/Components/FinancialItemDialog.razor`
- **Usage Examples**:
  - `src/BudgetExperiment.Client/Pages/FluentCalendar.razor`
  - `src/BudgetExperiment.Client/Components/UnifiedDayDetailsDialog.razor`

## Dependencies

- Microsoft.FluentUI.AspNetCore.Components
- Microsoft.AspNetCore.Components (included in _Imports.razor)
- BudgetExperiment.Domain (for TransactionType enum)

## Related Components

- `UnifiedScheduleDialog` - For managing recurring income/expense schedules
- `UnifiedDayDetailsDialog` - For viewing and editing all items for a specific day
