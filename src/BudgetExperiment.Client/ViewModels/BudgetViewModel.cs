// <copyright file="BudgetViewModel.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Shared.Budgeting;
using Microsoft.AspNetCore.Components;

namespace BudgetExperiment.Client.ViewModels;

/// <summary>
/// ViewModel for the Budget page. Contains all handler logic, state fields,
/// and computed properties extracted from the Budget.razor @code block.
/// </summary>
public sealed class BudgetViewModel : IDisposable
{
    private readonly IBudgetApiService _apiService;
    private readonly NavigationManager _navigationManager;
    private readonly ScopeService _scopeService;
    private readonly IApiErrorContext _apiErrorContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetViewModel"/> class.
    /// </summary>
    /// <param name="apiService">The budget API service.</param>
    /// <param name="navigationManager">The navigation manager.</param>
    /// <param name="scopeService">The budget scope service.</param>
    /// <param name="apiErrorContext">The API error context for traceId capture.</param>
    public BudgetViewModel(
        IBudgetApiService apiService,
        NavigationManager navigationManager,
        ScopeService scopeService,
        IApiErrorContext apiErrorContext)
    {
        this._apiService = apiService;
        this._navigationManager = navigationManager;
        this._scopeService = scopeService;
        this._apiErrorContext = apiErrorContext;
    }

    /// <summary>
    /// Gets or sets the callback to notify the Razor page that state has changed and it should re-render.
    /// </summary>
    public Action? OnStateChanged { get; set; }

    /// <summary>
    /// Gets a value indicating whether budget data is loading.
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
    /// Gets the current error message, if any.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the traceId from the API error response that caused the current error, if any.
    /// </summary>
    public string? ErrorTraceId { get; private set; }

    /// <summary>
    /// Gets the budget summary data.
    /// </summary>
    public BudgetSummaryDto? Summary { get; private set; }

    /// <summary>
    /// Gets the current date used for month navigation.
    /// </summary>
    public DateTime CurrentDate { get; private set; } = DateTime.Today;

    /// <summary>
    /// Gets a value indicating whether the edit goal modal is visible.
    /// </summary>
    public bool ShowEditGoalModal { get; private set; }

    /// <summary>
    /// Gets the budget progress item being edited.
    /// </summary>
    public BudgetProgressDto? EditingProgress { get; private set; }

    /// <summary>
    /// Gets or sets the target amount for the goal being edited.
    /// </summary>
    public decimal EditTargetAmount { get; set; }

    /// <summary>
    /// Gets the overall budget status based on percent used.
    /// </summary>
    public string OverallStatus
    {
        get
        {
            if (this.Summary == null)
            {
                return "NoBudgetSet";
            }

            if (this.Summary.OverallPercentUsed >= 100)
            {
                return "OverBudget";
            }

            if (this.Summary.OverallPercentUsed >= 80)
            {
                return "Warning";
            }

            return "OnTrack";
        }
    }

    /// <summary>
    /// Gets the modal title based on whether a new goal is being created or an existing one edited.
    /// </summary>
    public string ModalTitle => this.IsCreatingNewGoal ? "Set Budget Goal" : "Edit Budget Goal";

    /// <summary>
    /// Gets a value indicating whether the current edit operation is creating a new goal.
    /// </summary>
    public bool IsCreatingNewGoal => this.EditingProgress?.Status == "NoBudgetSet";

    /// <summary>
    /// Initializes the ViewModel: subscribes to scope changes and loads budget data.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task InitializeAsync()
    {
        this._scopeService.ScopeChanged += this.OnScopeChanged;
        await this.LoadBudgetAsync();
    }

    /// <summary>
    /// Loads budget summary data from the API.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task LoadBudgetAsync()
    {
        this.IsLoading = true;
        this.ErrorMessage = null;
        this.ErrorTraceId = null;

        try
        {
            this.Summary = await this._apiService.GetBudgetSummaryAsync(this.CurrentDate.Year, this.CurrentDate.Month);
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to load budget: {ex.Message}";
            this.ErrorTraceId = this._apiErrorContext.LastTraceId;
        }
        finally
        {
            this.IsLoading = false;
        }
    }

    /// <summary>
    /// Retries loading budget data after a failure.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task RetryLoadAsync()
    {
        this.IsRetrying = true;
        this.NotifyStateChanged();

        try
        {
            await this.LoadBudgetAsync();
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
    /// Navigates to the previous month and reloads budget data.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task PreviousMonthAsync()
    {
        this.CurrentDate = this.CurrentDate.AddMonths(-1);
        await this.LoadBudgetAsync();
    }

    /// <summary>
    /// Navigates to the next month and reloads budget data.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task NextMonthAsync()
    {
        this.CurrentDate = this.CurrentDate.AddMonths(1);
        await this.LoadBudgetAsync();
    }

    /// <summary>
    /// Opens the edit goal modal for the specified category progress.
    /// </summary>
    /// <param name="progress">The budget progress to edit.</param>
    public void ShowEditGoal(BudgetProgressDto progress)
    {
        this.EditingProgress = progress;
        this.EditTargetAmount = progress.TargetAmount.Amount;
        this.ShowEditGoalModal = true;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Closes the edit goal modal.
    /// </summary>
    public void HideEditGoal()
    {
        this.ShowEditGoalModal = false;
        this.EditingProgress = null;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Saves the budget goal (create or update) via the API.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task SaveGoalAsync()
    {
        if (this.EditingProgress == null)
        {
            return;
        }

        this.IsSubmitting = true;

        try
        {
            var goalDto = new BudgetGoalSetDto
            {
                Year = this.CurrentDate.Year,
                Month = this.CurrentDate.Month,
                TargetAmount = new MoneyDto
                {
                    Amount = this.EditTargetAmount,
                    Currency = this.EditingProgress.TargetAmount.Currency ?? "USD",
                },
            };

            var result = await this._apiService.SetBudgetGoalAsync(this.EditingProgress.CategoryId, goalDto);
            if (result.IsSuccess)
            {
                this.ShowEditGoalModal = false;
                this.EditingProgress = null;
                await this.LoadBudgetAsync();
            }
            else
            {
                this.ErrorMessage = "Failed to save budget goal.";
            }
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to save budget goal: {ex.Message}";
            this.ErrorTraceId = this._apiErrorContext.LastTraceId;
        }
        finally
        {
            this.IsSubmitting = false;
        }
    }

    /// <summary>
    /// Deletes the budget goal for the currently editing category via the API.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task DeleteGoalAsync()
    {
        if (this.EditingProgress == null)
        {
            return;
        }

        this.IsSubmitting = true;

        try
        {
            var success = await this._apiService.DeleteBudgetGoalAsync(
                this.EditingProgress.CategoryId,
                this.CurrentDate.Year,
                this.CurrentDate.Month);

            if (success)
            {
                this.ShowEditGoalModal = false;
                this.EditingProgress = null;
                await this.LoadBudgetAsync();
            }
            else
            {
                this.ErrorMessage = "Failed to delete budget goal.";
            }
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to delete budget goal: {ex.Message}";
            this.ErrorTraceId = this._apiErrorContext.LastTraceId;
        }
        finally
        {
            this.IsSubmitting = false;
        }
    }

    /// <summary>
    /// Navigates to the categories page.
    /// </summary>
    public void NavigateToCategories()
    {
        this._navigationManager.NavigateTo("/categories");
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this._scopeService.ScopeChanged -= this.OnScopeChanged;
    }

    private async void OnScopeChanged(BudgetScope? scope)
    {
        await this.LoadBudgetAsync();
        this.NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        this.OnStateChanged?.Invoke();
    }
}
