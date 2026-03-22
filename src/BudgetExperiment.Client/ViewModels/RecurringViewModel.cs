// <copyright file="RecurringViewModel.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Shared.Budgeting;

namespace BudgetExperiment.Client.ViewModels;

/// <summary>
/// ViewModel for the Recurring Transactions page. Contains all handler logic, state fields,
/// and formatting helpers extracted from the Recurring.razor @code block.
/// </summary>
public sealed class RecurringViewModel : IDisposable
{
    private readonly IBudgetApiService _apiService;
    private readonly IToastService _toastService;
    private readonly ScopeService _scopeService;
    private readonly IChatContextService _chatContextService;
    private readonly IApiErrorContext _apiErrorContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringViewModel"/> class.
    /// </summary>
    /// <param name="apiService">The budget API service.</param>
    /// <param name="toastService">The toast notification service.</param>
    /// <param name="scopeService">The budget scope service.</param>
    /// <param name="chatContextService">The chat context service.</param>
    /// <param name="apiErrorContext">The API error context for traceId capture.</param>
    public RecurringViewModel(
        IBudgetApiService apiService,
        IToastService toastService,
        ScopeService scopeService,
        IChatContextService chatContextService,
        IApiErrorContext apiErrorContext)
    {
        _apiService = apiService;
        _toastService = toastService;
        _scopeService = scopeService;
        _chatContextService = chatContextService;
        _apiErrorContext = apiErrorContext;
    }

    /// <summary>
    /// Gets or sets the callback to notify the Razor page that state has changed and it should re-render.
    /// </summary>
    public Action? OnStateChanged
    {
        get; set;
    }

    /// <summary>
    /// Gets a value indicating whether data is loading.
    /// </summary>
    public bool IsLoading { get; private set; } = true;

    /// <summary>
    /// Gets a value indicating whether a retry load is in progress.
    /// </summary>
    public bool IsRetrying
    {
        get; private set;
    }

    /// <summary>
    /// Gets a value indicating whether a form submission is in progress.
    /// </summary>
    public bool IsSubmitting
    {
        get; private set;
    }

    /// <summary>
    /// Gets a value indicating whether a delete operation is in progress.
    /// </summary>
    public bool IsDeleting
    {
        get; private set;
    }

    /// <summary>
    /// Gets the current error message, if any.
    /// </summary>
    public string? ErrorMessage
    {
        get; private set;
    }

    /// <summary>
    /// Gets the traceId from the API error response that caused the current error, if any.
    /// </summary>
    public string? ErrorTraceId
    {
        get; private set;
    }

    /// <summary>
    /// Gets the list of recurring transactions.
    /// </summary>
    public List<RecurringTransactionDto> RecurringTransactions { get; private set; } = new();

    /// <summary>
    /// Gets the list of categories.
    /// </summary>
    public List<BudgetCategoryDto> Categories { get; private set; } = new();

    /// <summary>
    /// Gets a value indicating whether the add form modal is visible.
    /// </summary>
    public bool ShowAddForm
    {
        get; private set;
    }

    /// <summary>
    /// Gets a value indicating whether the edit form modal is visible.
    /// </summary>
    public bool ShowEditForm
    {
        get; private set;
    }

    /// <summary>
    /// Gets a value indicating whether the delete confirmation dialog is visible.
    /// </summary>
    public bool ShowDeleteConfirm
    {
        get; private set;
    }

    /// <summary>
    /// Gets a value indicating whether the import patterns dialog is visible.
    /// </summary>
    public bool ShowImportPatterns
    {
        get; private set;
    }

    /// <summary>
    /// Gets or sets the new recurring transaction form model.
    /// </summary>
    public RecurringTransactionCreateDto NewRecurring { get; set; } = new();

    /// <summary>
    /// Gets or sets the edit recurring transaction form model.
    /// </summary>
    public RecurringTransactionUpdateDto EditModel { get; set; } = new();

    /// <summary>
    /// Gets the ID of the recurring transaction being edited.
    /// </summary>
    public Guid EditingId
    {
        get; private set;
    }

    /// <summary>
    /// Gets the concurrency version of the recurring transaction being edited.
    /// </summary>
    public string? EditingVersion
    {
        get; private set;
    }

    /// <summary>
    /// Gets the recurring transaction pending deletion.
    /// </summary>
    public RecurringTransactionDto? DeletingRecurring
    {
        get; private set;
    }

    /// <summary>
    /// Gets the ID of the recurring transaction for import patterns.
    /// </summary>
    public Guid ImportPatternsRecurringId
    {
        get; private set;
    }

    /// <summary>
    /// Gets the description of the recurring transaction for import patterns.
    /// </summary>
    public string ImportPatternsDescription { get; private set; } = string.Empty;

    /// <summary>
    /// Formats the frequency display string for a recurring transaction.
    /// </summary>
    /// <param name="recurring">The recurring transaction.</param>
    /// <returns>A human-readable frequency string.</returns>
    public static string FormatFrequency(RecurringTransactionDto recurring)
    {
        return recurring.Frequency switch
        {
            "Daily" => "Daily",
            "Weekly" => $"Weekly on {recurring.DayOfWeek}",
            "BiWeekly" => $"Bi-Weekly on {recurring.DayOfWeek}",
            "Monthly" => recurring.DayOfMonth.HasValue ? $"Monthly on day {recurring.DayOfMonth}" : "Monthly",
            "Quarterly" => recurring.DayOfMonth.HasValue ? $"Quarterly on day {recurring.DayOfMonth}" : "Quarterly",
            "Yearly" => recurring.DayOfMonth.HasValue ? $"Yearly on day {recurring.DayOfMonth}" : "Yearly",
            _ => recurring.Frequency,
        };
    }

    /// <summary>
    /// Formats a money value for display.
    /// </summary>
    /// <param name="money">The money DTO.</param>
    /// <returns>A formatted money string.</returns>
    public static string FormatMoney(MoneyDto money)
    {
        var sign = money.Amount >= 0 ? "+" : string.Empty;
        return $"{sign}{money.Currency} {money.Amount:N2}";
    }

    /// <summary>
    /// Initializes the ViewModel: subscribes to scope changes, sets chat context, and loads data.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task InitializeAsync()
    {
        _scopeService.ScopeChanged += this.OnScopeChanged;
        _chatContextService.SetPageType("recurring transactions");
        await this.LoadRecurringTransactionsAsync();
        await this.LoadCategoriesAsync();
    }

    /// <summary>
    /// Loads recurring transactions from the API.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task LoadRecurringTransactionsAsync()
    {
        this.IsLoading = true;
        this.ErrorMessage = null;
        this.ErrorTraceId = null;

        try
        {
            this.RecurringTransactions = (await _apiService.GetRecurringTransactionsAsync()).ToList();
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to load recurring transactions: {ex.Message}";
            this.ErrorTraceId = _apiErrorContext.LastTraceId;
        }
        finally
        {
            this.IsLoading = false;
        }
    }

    /// <summary>
    /// Loads categories from the API.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task LoadCategoriesAsync()
    {
        try
        {
            this.Categories = (await _apiService.GetCategoriesAsync()).ToList();
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to load categories: {ex.Message}";
            this.ErrorTraceId = _apiErrorContext.LastTraceId;
        }
    }

    /// <summary>
    /// Retries loading recurring transactions after a failure.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task RetryLoadAsync()
    {
        this.IsRetrying = true;
        this.NotifyStateChanged();

        try
        {
            await this.LoadRecurringTransactionsAsync();
        }
        finally
        {
            this.IsRetrying = false;
        }
    }

    /// <summary>
    /// Dismisses the current error message.
    /// </summary>
    public void DismissError()
    {
        this.ErrorMessage = null;
        this.ErrorTraceId = null;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Opens the add recurring transaction form.
    /// </summary>
    public void OpenAddForm()
    {
        this.NewRecurring = new RecurringTransactionCreateDto
        {
            Frequency = "Monthly",
            StartDate = DateOnly.FromDateTime(DateTime.Today),
        };
        this.ShowAddForm = true;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Opens the edit form for the specified recurring transaction.
    /// </summary>
    /// <param name="recurring">The recurring transaction to edit.</param>
    public void OpenEditForm(RecurringTransactionDto recurring)
    {
        this.EditingId = recurring.Id;
        this.EditingVersion = recurring.Version;
        this.EditModel = new RecurringTransactionUpdateDto
        {
            Description = recurring.Description,
            Amount = recurring.Amount,
            EndDate = recurring.EndDate,
            CategoryId = recurring.CategoryId,
        };
        this.ShowEditForm = true;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Hides the add or edit form.
    /// </summary>
    public void HideForm()
    {
        this.ShowAddForm = false;
        this.ShowEditForm = false;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Creates a new recurring transaction via the API.
    /// </summary>
    /// <param name="model">The creation data.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task CreateRecurringAsync(RecurringTransactionCreateDto model)
    {
        this.IsSubmitting = true;

        try
        {
            var result = await _apiService.CreateRecurringTransactionAsync(model);
            if (result != null)
            {
                this.ShowAddForm = false;
                await this.LoadRecurringTransactionsAsync();
            }
        }
        finally
        {
            this.IsSubmitting = false;
        }
    }

    /// <summary>
    /// Updates a recurring transaction via the API.
    /// </summary>
    /// <param name="model">The update data.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task UpdateRecurringAsync(RecurringTransactionUpdateDto model)
    {
        this.IsSubmitting = true;

        try
        {
            var result = await _apiService.UpdateRecurringTransactionAsync(this.EditingId, model, this.EditingVersion);
            if (result.IsConflict)
            {
                _toastService.ShowWarning("This recurring transaction was modified by another user. Data has been refreshed.", "Conflict");
                this.HideForm();
                await this.LoadRecurringTransactionsAsync();
                return;
            }

            if (result.IsSuccess)
            {
                this.ShowEditForm = false;
                await this.LoadRecurringTransactionsAsync();
            }
        }
        finally
        {
            this.IsSubmitting = false;
        }
    }

    /// <summary>
    /// Skips the next occurrence of a recurring transaction.
    /// </summary>
    /// <param name="recurring">The recurring transaction to skip.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task SkipNextAsync(RecurringTransactionDto recurring)
    {
        var result = await _apiService.SkipNextRecurringAsync(recurring.Id);
        if (result != null)
        {
            await this.LoadRecurringTransactionsAsync();
        }
    }

    /// <summary>
    /// Pauses a recurring transaction.
    /// </summary>
    /// <param name="recurring">The recurring transaction to pause.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task PauseAsync(RecurringTransactionDto recurring)
    {
        var result = await _apiService.PauseRecurringTransactionAsync(recurring.Id);
        if (result != null)
        {
            await this.LoadRecurringTransactionsAsync();
        }
    }

    /// <summary>
    /// Resumes a paused recurring transaction.
    /// </summary>
    /// <param name="recurring">The recurring transaction to resume.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task ResumeAsync(RecurringTransactionDto recurring)
    {
        var result = await _apiService.ResumeRecurringTransactionAsync(recurring.Id);
        if (result != null)
        {
            await this.LoadRecurringTransactionsAsync();
        }
    }

    /// <summary>
    /// Shows the delete confirmation dialog for the specified recurring transaction.
    /// </summary>
    /// <param name="recurring">The recurring transaction to delete.</param>
    public void OpenDeleteConfirm(RecurringTransactionDto recurring)
    {
        this.DeletingRecurring = recurring;
        this.ShowDeleteConfirm = true;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Confirms deletion of the pending recurring transaction.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task ConfirmDeleteAsync()
    {
        if (this.DeletingRecurring == null)
        {
            return;
        }

        this.IsDeleting = true;
        this.NotifyStateChanged();

        try
        {
            var deleted = await _apiService.DeleteRecurringTransactionAsync(this.DeletingRecurring.Id);
            if (deleted)
            {
                this.ShowDeleteConfirm = false;
                this.DeletingRecurring = null;
                await this.LoadRecurringTransactionsAsync();
            }
        }
        finally
        {
            this.IsDeleting = false;
        }
    }

    /// <summary>
    /// Cancels the delete confirmation.
    /// </summary>
    public void CancelDelete()
    {
        this.ShowDeleteConfirm = false;
        this.DeletingRecurring = null;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Opens the import patterns dialog for the specified recurring transaction.
    /// </summary>
    /// <param name="recurring">The recurring transaction to import patterns for.</param>
    public void OpenImportPatterns(RecurringTransactionDto recurring)
    {
        this.ImportPatternsRecurringId = recurring.Id;
        this.ImportPatternsDescription = recurring.Description;
        this.ShowImportPatterns = true;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Hides the import patterns dialog.
    /// </summary>
    public void HideImportPatterns()
    {
        this.ShowImportPatterns = false;
        this.ImportPatternsRecurringId = Guid.Empty;
        this.ImportPatternsDescription = string.Empty;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Handles the import patterns saved event.
    /// </summary>
    public void HandleImportPatternsSaved()
    {
        this.HideImportPatterns();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _scopeService.ScopeChanged -= this.OnScopeChanged;
        _chatContextService.ClearContext();
    }

    private async void OnScopeChanged(BudgetScope? scope)
    {
        await this.LoadRecurringTransactionsAsync();
        await this.LoadCategoriesAsync();
        this.NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        this.OnStateChanged?.Invoke();
    }
}
