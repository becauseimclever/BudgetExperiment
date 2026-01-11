# Feature: Graceful Error Handling with Retry

## Overview
Add user-friendly error handling across all UI pages so that API failures display clear error messages with the ability to retry failed operations, rather than leaving users stuck on broken screens.

## User Stories

### US-001: See Clear Error Messages
**As a** user  
**I want to** see a clear error message when something goes wrong  
**So that** I understand what happened and know the app isn't just frozen

### US-002: Retry Failed Operations
**As a** user  
**I want to** retry a failed operation with a single click  
**So that** I can recover from temporary network issues without refreshing the page

### US-003: Dismiss Errors
**As a** user  
**I want to** dismiss an error message  
**So that** I can continue using the app even if some data failed to load

---

## Implementation

### ErrorAlert Component

A reusable Blazor component for displaying errors with retry functionality.

**Location:** `src/BudgetExperiment.Client/Components/Common/ErrorAlert.razor`

**Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `Message` | `string?` | The error message to display (null = hidden) |
| `Details` | `string?` | Optional additional details |
| `IsDismissible` | `bool` | Whether the dismiss button is shown |
| `IsRetrying` | `bool` | Shows loading spinner on retry button |
| `OnRetry` | `EventCallback` | Called when retry button is clicked |
| `OnDismiss` | `EventCallback` | Called when dismiss button is clicked |

**Usage:**
```razor
<ErrorAlert Message="@errorMessage"
            IsDismissible="true"
            IsRetrying="@isRetrying"
            OnRetry="RetryLoad"
            OnDismiss="DismissError" />
```

---

### Pages Updated

All major pages now have consistent error handling:

| Page | File | Load Method(s) Protected |
|------|------|-------------------------|
| Calendar | `Calendar.razor` | `LoadAccounts()`, `LoadCalendarData()` |
| Accounts | `Accounts.razor` | `LoadAccounts()` |
| Account Transactions | `AccountTransactions.razor` | `LoadAccount()`, `LoadData()`, `SaveTransaction()` |
| Recurring | `Recurring.razor` | `LoadRecurringTransactions()` |
| Transfers | `Transfers.razor` | `LoadData()` |

### Forms Updated

| Form | File | Change |
|------|------|--------|
| TransactionForm | `TransactionForm.razor` | Added `ErrorMessage` parameter for inline error display |

---

## Code Pattern

Each page follows this consistent pattern:

### State Variables
```csharp
private string? errorMessage;
private bool isRetrying;
```

### Load Method with Try-Catch
```csharp
private async Task LoadData()
{
    isLoading = true;
    errorMessage = null;

    try
    {
        // API calls
        data = await ApiService.GetDataAsync();
    }
    catch (Exception ex)
    {
        errorMessage = $"Failed to load data: {ex.Message}";
    }
    finally
    {
        isLoading = false;
    }
}
```

### Retry Method
```csharp
private async Task RetryLoad()
{
    isRetrying = true;
    StateHasChanged();

    try
    {
        await LoadData();
    }
    finally
    {
        isRetrying = false;
    }
}
```

### Dismiss Method
```csharp
private void DismissError()
{
    errorMessage = null;
}
```

### Conditional Rendering
```razor
@if (isLoading)
{
    <LoadingSpinner />
}
else if (errorMessage != null)
{
    @* Error displayed by ErrorAlert component *@
}
else
{
    @* Normal content *@
}
```

---

## UI/UX Design

### Error Alert Appearance
- Red border and background tint
- Warning icon (⚠️)
- Clear error message text
- "Try Again" button with loading state
- Optional dismiss button (×)

### Behavior
- Error alert appears in place of content when data fails to load
- Retry button shows spinner during retry attempt
- Error clears automatically on successful retry
- User can dismiss error to see partial content if available

---

## Files Created/Modified

### New Files
- `src/BudgetExperiment.Client/Components/Common/ErrorAlert.razor`
- `src/BudgetExperiment.Client/Components/Common/ErrorAlert.razor.css`

### Modified Files
- `src/BudgetExperiment.Client/Pages/Calendar.razor`
- `src/BudgetExperiment.Client/Pages/Accounts.razor`
- `src/BudgetExperiment.Client/Pages/AccountTransactions.razor`
- `src/BudgetExperiment.Client/Pages/Recurring.razor`
- `src/BudgetExperiment.Client/Pages/Transfers.razor`
- `src/BudgetExperiment.Client/Components/Forms/TransactionForm.razor`
- `src/BudgetExperiment.Client/Components/Forms/TransactionForm.razor.css`

---

## Testing

Error handling is primarily tested through manual testing:
1. Stop the API server
2. Navigate to each page
3. Verify error message appears
4. Click "Try Again" and verify spinner shows
5. Start the API server
6. Click "Try Again" and verify data loads

Future consideration: Add integration tests that mock API failures to verify error handling behavior.
