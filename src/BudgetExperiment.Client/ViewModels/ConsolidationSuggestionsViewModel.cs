// <copyright file="ConsolidationSuggestionsViewModel.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Client.ViewModels;

/// <summary>
/// ViewModel for the Rule Consolidation Suggestions page.
/// </summary>
public sealed class ConsolidationSuggestionsViewModel
{
    private readonly IBudgetApiService _apiService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsolidationSuggestionsViewModel"/> class.
    /// </summary>
    /// <param name="apiService">The budget API service.</param>
    public ConsolidationSuggestionsViewModel(IBudgetApiService apiService)
    {
        _apiService = apiService;
    }

    /// <summary>
    /// Gets or sets the callback to notify the Razor page that state has changed.
    /// </summary>
    public Action? OnStateChanged { get; set; }

    /// <summary>
    /// Gets a value indicating whether the consolidation analysis is running.
    /// </summary>
    public bool IsAnalyzing { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the initial data is loading.
    /// </summary>
    public bool IsLoading { get; private set; }

    /// <summary>
    /// Gets a value indicating whether analysis has been run at least once this session.
    /// </summary>
    public bool HasRunAnalysis { get; private set; }

    /// <summary>
    /// Gets the current error message, if any.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the traceId from the API error response, if available.
    /// </summary>
    public string? ErrorTraceId { get; private set; }

    /// <summary>
    /// Gets the list of consolidation suggestions.
    /// </summary>
    public IReadOnlyList<RuleSuggestionDto> Suggestions { get; private set; } = [];

    /// <summary>
    /// Gets a value indicating whether all suggestions have been accepted or dismissed.
    /// True only when analysis has been run and no suggestions remain in the list.
    /// </summary>
    public bool AllAcceptedOrDismissed => HasRunAnalysis && !Suggestions.Any();

    /// <summary>
    /// Loads existing consolidation suggestions from the API.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task InitializeAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        NotifyStateChanged();

        try
        {
            var existing = await _apiService.GetConsolidationSuggestionsAsync();
            if (existing.Count > 0)
            {
                Suggestions = existing;
                HasRunAnalysis = true;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load suggestions: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Runs the consolidation analysis and updates the suggestions list.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task RunAnalysisAsync()
    {
        IsAnalyzing = true;
        ErrorMessage = null;
        NotifyStateChanged();

        try
        {
            var results = await _apiService.AnalyzeConsolidationAsync();
            Suggestions = results;
            HasRunAnalysis = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Analysis failed: {ex.Message}";
        }
        finally
        {
            IsAnalyzing = false;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Accepts a consolidation suggestion, creating the merged rule.
    /// </summary>
    /// <param name="id">The suggestion identifier.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task AcceptAsync(Guid id)
    {
        ErrorMessage = null;
        NotifyStateChanged();

        try
        {
            var result = await _apiService.AcceptConsolidationSuggestionAsync(id);
            if (result is not null)
            {
                var updated = Suggestions.FirstOrDefault(s => s.Id == id);
                if (updated is not null)
                {
                    updated.Status = "Accepted";
                }

                Suggestions = Suggestions.ToList();
            }
            else
            {
                ErrorMessage = "Failed to accept the consolidation suggestion.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to accept suggestion: {ex.Message}";
        }
        finally
        {
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Dismisses a consolidation suggestion.
    /// </summary>
    /// <param name="id">The suggestion identifier.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task DismissAsync(Guid id)
    {
        ErrorMessage = null;
        NotifyStateChanged();

        try
        {
            var success = await _apiService.DismissConsolidationSuggestionAsync(id);
            if (success)
            {
                Suggestions = Suggestions.Where(s => s.Id != id).ToList();
            }
            else
            {
                ErrorMessage = "Failed to dismiss the suggestion.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to dismiss suggestion: {ex.Message}";
        }
        finally
        {
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Undoes an accepted consolidation, restoring original rules and reopening the suggestion.
    /// </summary>
    /// <param name="id">The suggestion identifier.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task UndoAsync(Guid id)
    {
        ErrorMessage = null;
        NotifyStateChanged();

        try
        {
            var success = await _apiService.UndoConsolidationAsync(id);
            if (success)
            {
                var updated = Suggestions.FirstOrDefault(s => s.Id == id);
                if (updated is not null)
                {
                    updated.Status = "Pending";
                }

                Suggestions = Suggestions.ToList();
            }
            else
            {
                ErrorMessage = "Failed to undo the consolidation.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to undo consolidation: {ex.Message}";
        }
        finally
        {
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Dismisses the current error message.
    /// </summary>
    public void DismissError()
    {
        ErrorMessage = null;
        ErrorTraceId = null;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnStateChanged?.Invoke();
}
