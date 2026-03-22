// <copyright file="RecurringChargeSuggestionsViewModel.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Client.ViewModels;

/// <summary>
/// ViewModel for the Recurring Charge Suggestions page.
/// </summary>
public sealed class RecurringChargeSuggestionsViewModel
{
    private readonly IRecurringChargeSuggestionApiService _apiService;
    private readonly IToastService _toastService;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringChargeSuggestionsViewModel"/> class.
    /// </summary>
    /// <param name="apiService">The recurring charge suggestion API service.</param>
    /// <param name="toastService">The toast notification service.</param>
    public RecurringChargeSuggestionsViewModel(
        IRecurringChargeSuggestionApiService apiService,
        IToastService toastService)
    {
        this._apiService = apiService;
        this._toastService = toastService;
    }

    /// <summary>
    /// Gets or sets the callback to notify the Razor page that state has changed.
    /// </summary>
    public Action? OnStateChanged { get; set; }

    /// <summary>
    /// Gets a value indicating whether initial data is loading.
    /// </summary>
    public bool IsLoading { get; private set; } = true;

    /// <summary>
    /// Gets a value indicating whether detection is in progress.
    /// </summary>
    public bool IsDetecting { get; private set; }

    /// <summary>
    /// Gets a value indicating whether an accept/dismiss operation is in progress.
    /// </summary>
    public bool IsProcessing { get; private set; }

    /// <summary>
    /// Gets the current error message, if any.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the list of suggestions.
    /// </summary>
    public IReadOnlyList<RecurringChargeSuggestionDto> Suggestions { get; private set; } = [];

    /// <summary>
    /// Gets or sets the current status filter.
    /// </summary>
    public string StatusFilter { get; set; } = "Pending";

    /// <summary>
    /// Formats a confidence value as a percentage string.
    /// </summary>
    /// <param name="confidence">The confidence value (0-1).</param>
    /// <returns>Formatted percentage string.</returns>
    public static string FormatConfidence(decimal confidence)
    {
        return $"{confidence * 100:F0}%";
    }

    /// <summary>
    /// Gets a CSS class for the confidence level.
    /// </summary>
    /// <param name="confidence">The confidence value (0-1).</param>
    /// <returns>A CSS class name.</returns>
    public static string GetConfidenceClass(decimal confidence)
    {
        return confidence switch
        {
            >= 0.8m => "confidence-high",
            >= 0.6m => "confidence-medium",
            _ => "confidence-low",
        };
    }

    /// <summary>
    /// Loads suggestions from the API.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task InitializeAsync()
    {
        this.IsLoading = true;
        this.ErrorMessage = null;
        this.NotifyStateChanged();

        try
        {
            this.Suggestions = await this._apiService.GetSuggestionsAsync(
                status: this.StatusFilter);
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to load suggestions: {ex.Message}";
        }
        finally
        {
            this.IsLoading = false;
            this.NotifyStateChanged();
        }
    }

    /// <summary>
    /// Triggers recurring charge detection.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task DetectAsync()
    {
        this.IsDetecting = true;
        this.ErrorMessage = null;
        this.NotifyStateChanged();

        try
        {
            var count = await this._apiService.DetectAsync();
            if (count > 0)
            {
                this._toastService.ShowSuccess($"Found {count} recurring charge pattern(s).");
            }
            else
            {
                this._toastService.ShowInfo("No new recurring charge patterns detected.");
            }

            await this.InitializeAsync();
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Detection failed: {ex.Message}";
        }
        finally
        {
            this.IsDetecting = false;
            this.NotifyStateChanged();
        }
    }

    /// <summary>
    /// Accepts a suggestion, creating a RecurringTransaction.
    /// </summary>
    /// <param name="id">The suggestion identifier.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task AcceptAsync(Guid id)
    {
        this.IsProcessing = true;
        this.NotifyStateChanged();

        try
        {
            var result = await this._apiService.AcceptAsync(id);
            if (result != null)
            {
                this._toastService.ShowSuccess(
                    $"Created recurring transaction. {result.LinkedTransactionCount} existing transaction(s) linked.");
                await this.InitializeAsync();
            }
            else
            {
                this._toastService.ShowError("Failed to accept suggestion.");
            }
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to accept: {ex.Message}";
        }
        finally
        {
            this.IsProcessing = false;
            this.NotifyStateChanged();
        }
    }

    /// <summary>
    /// Dismisses a suggestion.
    /// </summary>
    /// <param name="id">The suggestion identifier.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task DismissAsync(Guid id)
    {
        this.IsProcessing = true;
        this.NotifyStateChanged();

        try
        {
            var success = await this._apiService.DismissAsync(id);
            if (success)
            {
                this._toastService.ShowSuccess("Suggestion dismissed.");
                await this.InitializeAsync();
            }
            else
            {
                this._toastService.ShowError("Failed to dismiss suggestion.");
            }
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to dismiss: {ex.Message}";
        }
        finally
        {
            this.IsProcessing = false;
            this.NotifyStateChanged();
        }
    }

    /// <summary>
    /// Changes the status filter and reloads suggestions.
    /// </summary>
    /// <param name="status">The new status filter.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task FilterByStatusAsync(string status)
    {
        this.StatusFilter = status;
        await this.InitializeAsync();
    }

    /// <summary>
    /// Dismisses the current error message.
    /// </summary>
    public void DismissError()
    {
        this.ErrorMessage = null;
        this.NotifyStateChanged();
    }

    private void NotifyStateChanged() => this.OnStateChanged?.Invoke();
}
