// <copyright file="RecurringTransfersViewModel.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Shared.Budgeting;

namespace BudgetExperiment.Client.ViewModels;

/// <summary>
/// ViewModel for the Recurring Transfers page. Contains all handler logic, state fields,
/// and formatting helpers extracted from the RecurringTransfers.razor @code block.
/// </summary>
public sealed class RecurringTransfersViewModel : IDisposable
{
    private readonly IBudgetApiService _apiService;
    private readonly IToastService _toastService;
    private readonly ScopeService _scopeService;
    private readonly IChatContextService _chatContextService;
    private readonly IApiErrorContext _apiErrorContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringTransfersViewModel"/> class.
    /// </summary>
    /// <param name="apiService">The budget API service.</param>
    /// <param name="toastService">The toast notification service.</param>
    /// <param name="scopeService">The budget scope service.</param>
    /// <param name="chatContextService">The chat context service.</param>
    /// <param name="apiErrorContext">The API error context for traceId capture.</param>
    public RecurringTransfersViewModel(
        IBudgetApiService apiService,
        IToastService toastService,
        ScopeService scopeService,
        IChatContextService chatContextService,
        IApiErrorContext apiErrorContext)
    {
        this._apiService = apiService;
        this._toastService = toastService;
        this._scopeService = scopeService;
        this._chatContextService = chatContextService;
        this._apiErrorContext = apiErrorContext;
    }

    /// <summary>
    /// Gets or sets the callback to notify the Razor page that state has changed and it should re-render.
    /// </summary>
    public Action? OnStateChanged { get; set; }

    /// <summary>
    /// Gets a value indicating whether data is loading.
    /// </summary>
    public bool IsLoading { get; private set; } = true;

    /// <summary>
    /// Gets a value indicating whether a retry load is in progress.
    /// </summary>
    public bool IsRetrying { get; private set; }

    /// <summary>
    /// Gets a value indicating whether a form submission is in progress.
    /// </summary>
    public bool IsSubmitting { get; private set; }

    /// <summary>
    /// Gets a value indicating whether a delete operation is in progress.
    /// </summary>
    public bool IsDeleting { get; private set; }

    /// <summary>
    /// Gets the current error message, if any.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the traceId from the API error response that caused the current error, if any.
    /// </summary>
    public string? ErrorTraceId { get; private set; }

    /// <summary>
    /// Gets the list of recurring transfers.
    /// </summary>
    public List<RecurringTransferDto> RecurringTransfers { get; private set; } = new();

    /// <summary>
    /// Gets a value indicating whether the add form modal is visible.
    /// </summary>
    public bool ShowAddForm { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the edit form modal is visible.
    /// </summary>
    public bool ShowEditForm { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the delete confirmation dialog is visible.
    /// </summary>
    public bool ShowDeleteConfirm { get; private set; }

    /// <summary>
    /// Gets or sets the new recurring transfer form model.
    /// </summary>
    public RecurringTransferCreateDto NewRecurring { get; set; } = new();

    /// <summary>
    /// Gets or sets the edit recurring transfer form model.
    /// </summary>
    public RecurringTransferUpdateDto EditModel { get; set; } = new();

    /// <summary>
    /// Gets the ID of the recurring transfer being edited.
    /// </summary>
    public Guid EditingId { get; private set; }

    /// <summary>
    /// Gets the concurrency version of the recurring transfer being edited.
    /// </summary>
    public string? EditingVersion { get; private set; }

    /// <summary>
    /// Gets the recurring transfer pending deletion.
    /// </summary>
    public RecurringTransferDto? DeletingTransfer { get; private set; }

    /// <summary>
    /// Formats the frequency display string for a recurring transfer.
    /// </summary>
    /// <param name="transfer">The recurring transfer.</param>
    /// <returns>A human-readable frequency string.</returns>
    public static string FormatFrequency(RecurringTransferDto transfer)
    {
        return transfer.Frequency switch
        {
            "Daily" => "Daily",
            "Weekly" => $"Weekly on {transfer.DayOfWeek}",
            "BiWeekly" => $"Bi-Weekly on {transfer.DayOfWeek}",
            "Monthly" => transfer.DayOfMonth.HasValue ? $"Monthly on day {transfer.DayOfMonth}" : "Monthly",
            "Quarterly" => transfer.DayOfMonth.HasValue ? $"Quarterly on day {transfer.DayOfMonth}" : "Quarterly",
            "Yearly" => transfer.DayOfMonth.HasValue ? $"Yearly on day {transfer.DayOfMonth}" : "Yearly",
            _ => transfer.Frequency,
        };
    }

    /// <summary>
    /// Formats a money value for display.
    /// </summary>
    /// <param name="money">The money DTO.</param>
    /// <returns>A formatted money string.</returns>
    public static string FormatMoney(MoneyDto money)
    {
        return $"{money.Currency} {money.Amount:N2}";
    }

    /// <summary>
    /// Initializes the ViewModel: subscribes to scope changes, sets chat context, and loads data.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task InitializeAsync()
    {
        this._scopeService.ScopeChanged += this.OnScopeChanged;
        this._chatContextService.SetPageType("recurring transfers");
        await this.LoadRecurringTransfersAsync();
    }

    /// <summary>
    /// Loads recurring transfers from the API.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task LoadRecurringTransfersAsync()
    {
        this.IsLoading = true;
        this.ErrorMessage = null;
        this.ErrorTraceId = null;

        try
        {
            this.RecurringTransfers = (await this._apiService.GetRecurringTransfersAsync()).ToList();
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to load recurring transfers: {ex.Message}";
            this.ErrorTraceId = this._apiErrorContext.LastTraceId;
        }
        finally
        {
            this.IsLoading = false;
        }
    }

    /// <summary>
    /// Retries loading recurring transfers after a failure.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task RetryLoadAsync()
    {
        this.IsRetrying = true;
        this.NotifyStateChanged();

        try
        {
            await this.LoadRecurringTransfersAsync();
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
    /// Opens the add recurring transfer form.
    /// </summary>
    public void OpenAddForm()
    {
        this.NewRecurring = new RecurringTransferCreateDto
        {
            Frequency = "Monthly",
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            Amount = new MoneyDto { Currency = "USD", Amount = 0 },
        };
        this.ShowAddForm = true;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Opens the edit form for the specified recurring transfer.
    /// </summary>
    /// <param name="transfer">The recurring transfer to edit.</param>
    public void OpenEditForm(RecurringTransferDto transfer)
    {
        this.EditingId = transfer.Id;
        this.EditingVersion = transfer.Version;
        this.EditModel = new RecurringTransferUpdateDto
        {
            Description = transfer.Description,
            Amount = transfer.Amount,
            Frequency = transfer.Frequency,
            Interval = transfer.Interval,
            DayOfMonth = transfer.DayOfMonth,
            DayOfWeek = transfer.DayOfWeek,
            MonthOfYear = transfer.MonthOfYear,
            EndDate = transfer.EndDate,
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
    /// Creates a new recurring transfer via the API.
    /// </summary>
    /// <param name="model">The creation data.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task CreateRecurringAsync(RecurringTransferCreateDto model)
    {
        this.IsSubmitting = true;

        try
        {
            var result = await this._apiService.CreateRecurringTransferAsync(model);
            if (result != null)
            {
                this.ShowAddForm = false;
                await this.LoadRecurringTransfersAsync();
            }
        }
        finally
        {
            this.IsSubmitting = false;
        }
    }

    /// <summary>
    /// Updates a recurring transfer via the API.
    /// </summary>
    /// <param name="model">The update data.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task UpdateRecurringAsync(RecurringTransferUpdateDto model)
    {
        this.IsSubmitting = true;

        try
        {
            var result = await this._apiService.UpdateRecurringTransferAsync(this.EditingId, model, this.EditingVersion);
            if (result.IsConflict)
            {
                this._toastService.ShowWarning("This recurring transfer was modified by another user. Data has been refreshed.", "Conflict");
                this.HideForm();
                await this.LoadRecurringTransfersAsync();
                return;
            }

            if (result.IsSuccess)
            {
                this.ShowEditForm = false;
                await this.LoadRecurringTransfersAsync();
            }
        }
        finally
        {
            this.IsSubmitting = false;
        }
    }

    /// <summary>
    /// Skips the next occurrence of a recurring transfer.
    /// </summary>
    /// <param name="transfer">The recurring transfer to skip.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task SkipNextAsync(RecurringTransferDto transfer)
    {
        var result = await this._apiService.SkipNextRecurringTransferAsync(transfer.Id);
        if (result != null)
        {
            await this.LoadRecurringTransfersAsync();
        }
    }

    /// <summary>
    /// Pauses a recurring transfer.
    /// </summary>
    /// <param name="transfer">The recurring transfer to pause.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task PauseAsync(RecurringTransferDto transfer)
    {
        var result = await this._apiService.PauseRecurringTransferAsync(transfer.Id);
        if (result != null)
        {
            await this.LoadRecurringTransfersAsync();
        }
    }

    /// <summary>
    /// Resumes a paused recurring transfer.
    /// </summary>
    /// <param name="transfer">The recurring transfer to resume.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task ResumeAsync(RecurringTransferDto transfer)
    {
        var result = await this._apiService.ResumeRecurringTransferAsync(transfer.Id);
        if (result != null)
        {
            await this.LoadRecurringTransfersAsync();
        }
    }

    /// <summary>
    /// Shows the delete confirmation dialog for the specified recurring transfer.
    /// </summary>
    /// <param name="transfer">The recurring transfer to delete.</param>
    public void OpenDeleteConfirm(RecurringTransferDto transfer)
    {
        this.DeletingTransfer = transfer;
        this.ShowDeleteConfirm = true;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Confirms deletion of the pending recurring transfer.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task ConfirmDeleteAsync()
    {
        if (this.DeletingTransfer == null)
        {
            return;
        }

        this.IsDeleting = true;
        this.NotifyStateChanged();

        try
        {
            var deleted = await this._apiService.DeleteRecurringTransferAsync(this.DeletingTransfer.Id);
            if (deleted)
            {
                this.ShowDeleteConfirm = false;
                this.DeletingTransfer = null;
                await this.LoadRecurringTransfersAsync();
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
        this.DeletingTransfer = null;
        this.NotifyStateChanged();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this._scopeService.ScopeChanged -= this.OnScopeChanged;
        this._chatContextService.ClearContext();
    }

    private async void OnScopeChanged(BudgetScope? scope)
    {
        await this.LoadRecurringTransfersAsync();
        this.NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        this.OnStateChanged?.Invoke();
    }
}
