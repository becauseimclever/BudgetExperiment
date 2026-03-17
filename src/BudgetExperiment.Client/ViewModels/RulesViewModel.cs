// <copyright file="RulesViewModel.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Shared.Budgeting;
using Microsoft.AspNetCore.Components;

namespace BudgetExperiment.Client.ViewModels;

/// <summary>
/// ViewModel for the Rules page. Contains all handler logic, state fields,
/// and properties extracted from the Rules.razor @code block.
/// </summary>
public sealed class RulesViewModel : IDisposable
{
    private readonly IBudgetApiService _apiService;
    private readonly IToastService _toastService;
    private readonly NavigationManager _navigationManager;
    private readonly ScopeService _scopeService;
    private readonly IApiErrorContext _apiErrorContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="RulesViewModel"/> class.
    /// </summary>
    /// <param name="apiService">The budget API service.</param>
    /// <param name="toastService">The toast notification service.</param>
    /// <param name="navigationManager">The navigation manager.</param>
    /// <param name="scopeService">The budget scope service.</param>
    /// <param name="apiErrorContext">The API error context for traceId capture.</param>
    public RulesViewModel(
        IBudgetApiService apiService,
        IToastService toastService,
        NavigationManager navigationManager,
        ScopeService scopeService,
        IApiErrorContext apiErrorContext)
    {
        this._apiService = apiService;
        this._toastService = toastService;
        this._navigationManager = navigationManager;
        this._scopeService = scopeService;
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
    /// Gets a value indicating whether a pattern test is in progress.
    /// </summary>
    public bool IsTesting { get; private set; }

    /// <summary>
    /// Gets the current error message, if any.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the traceId from the API error response that caused the current error, if any.
    /// </summary>
    public string? ErrorTraceId { get; private set; }

    /// <summary>
    /// Gets the list of categorization rules.
    /// </summary>
    public List<CategorizationRuleDto> Rules { get; private set; } = new();

    /// <summary>
    /// Gets the list of categories for rule assignment.
    /// </summary>
    public List<BudgetCategoryDto> Categories { get; private set; } = new();

    /// <summary>
    /// Gets a value indicating whether the add rule form is visible.
    /// </summary>
    public bool ShowAddForm { get; private set; }

    /// <summary>
    /// Gets or sets the new rule form model.
    /// </summary>
    public CategorizationRuleCreateDto NewRule { get; set; } = new() { MatchType = "Contains" };

    /// <summary>
    /// Gets the test pattern result.
    /// </summary>
    public TestPatternResponse? TestResult { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the edit rule form is visible.
    /// </summary>
    public bool ShowEditForm { get; private set; }

    /// <summary>
    /// Gets or sets the edit rule form model.
    /// </summary>
    public CategorizationRuleCreateDto EditRule { get; set; } = new();

    /// <summary>
    /// Gets the ID of the rule currently being edited.
    /// </summary>
    public Guid? EditingRuleId { get; private set; }

    /// <summary>
    /// Gets the concurrency version of the rule being edited.
    /// </summary>
    public string? EditingVersion { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the delete confirmation dialog is visible.
    /// </summary>
    public bool ShowDeleteConfirm { get; private set; }

    /// <summary>
    /// Gets the rule pending deletion.
    /// </summary>
    public CategorizationRuleDto? DeletingRule { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the apply rules dialog is visible.
    /// </summary>
    public bool ShowApplyRulesDialog { get; private set; }

    /// <summary>
    /// Initializes the ViewModel: subscribes to scope changes and loads data.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task InitializeAsync()
    {
        this._scopeService.ScopeChanged += this.OnScopeChanged;
        await this.LoadDataAsync();
    }

    /// <summary>
    /// Loads rules and categories from the API.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task LoadDataAsync()
    {
        this.IsLoading = true;
        this.ErrorMessage = null;
        this.ErrorTraceId = null;

        try
        {
            var rulesTask = this._apiService.GetCategorizationRulesAsync();
            var categoriesTask = this._apiService.GetCategoriesAsync(activeOnly: true);

            await Task.WhenAll(rulesTask, categoriesTask);

            this.Rules = (await rulesTask).ToList();
            this.Categories = (await categoriesTask).ToList();
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to load data: {ex.Message}";
            this.ErrorTraceId = this._apiErrorContext.LastTraceId;
        }
        finally
        {
            this.IsLoading = false;
        }
    }

    /// <summary>
    /// Retries loading data after a failure.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task RetryLoadAsync()
    {
        this.IsRetrying = true;
        this.NotifyStateChanged();

        try
        {
            await this.LoadDataAsync();
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
    /// Navigates to the AI suggestions page.
    /// </summary>
    public void NavigateToAiSuggestions()
    {
        this._navigationManager.NavigateTo("/ai/suggestions");
    }

    /// <summary>
    /// Opens the add rule form with default values.
    /// </summary>
    public void ShowAddRule()
    {
        this.NewRule = new CategorizationRuleCreateDto { MatchType = "Contains" };
        this.TestResult = null;
        this.ShowAddForm = true;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Closes the add rule form.
    /// </summary>
    public void HideAddRule()
    {
        this.ShowAddForm = false;
        this.TestResult = null;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Creates a new rule via the API.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task CreateRuleAsync()
    {
        this.IsSubmitting = true;

        try
        {
            var created = await this._apiService.CreateCategorizationRuleAsync(this.NewRule);
            if (created != null)
            {
                this.Rules.Add(created);
                this.ShowAddForm = false;
                this.TestResult = null;
            }
            else
            {
                this.ErrorMessage = "Failed to create rule.";
            }
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to create rule: {ex.Message}";
        }
        finally
        {
            this.IsSubmitting = false;
        }
    }

    /// <summary>
    /// Opens the edit rule form for the specified rule.
    /// </summary>
    /// <param name="rule">The rule to edit.</param>
    public void ShowEditRule(CategorizationRuleDto rule)
    {
        this.EditingRuleId = rule.Id;
        this.EditingVersion = rule.Version;
        this.EditRule = new CategorizationRuleCreateDto
        {
            Name = rule.Name,
            Pattern = rule.Pattern,
            MatchType = rule.MatchType,
            CaseSensitive = rule.CaseSensitive,
            CategoryId = rule.CategoryId,
        };
        this.TestResult = null;
        this.ShowEditForm = true;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Closes the edit rule form.
    /// </summary>
    public void HideEditRule()
    {
        this.ShowEditForm = false;
        this.EditingRuleId = null;
        this.TestResult = null;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Updates a rule via the API.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task UpdateRuleAsync()
    {
        if (this.EditingRuleId is null)
        {
            return;
        }

        this.IsSubmitting = true;

        try
        {
            var updateDto = new CategorizationRuleUpdateDto
            {
                Name = this.EditRule.Name,
                Pattern = this.EditRule.Pattern,
                MatchType = this.EditRule.MatchType,
                CaseSensitive = this.EditRule.CaseSensitive,
                CategoryId = this.EditRule.CategoryId,
            };

            var updated = await this._apiService.UpdateCategorizationRuleAsync(this.EditingRuleId.Value, updateDto, this.EditingVersion);
            if (updated.IsConflict)
            {
                this._toastService.ShowWarning("This rule was modified by another user. Data has been refreshed.", "Conflict");
                this.HideEditRule();
                await this.LoadDataAsync();
                return;
            }

            if (updated.IsSuccess)
            {
                var index = this.Rules.FindIndex(r => r.Id == this.EditingRuleId.Value);
                if (index >= 0)
                {
                    this.Rules[index] = updated.Data!;
                }

                this.ShowEditForm = false;
                this.EditingRuleId = null;
                this.TestResult = null;
            }
            else
            {
                this.ErrorMessage = "Failed to update rule.";
            }
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to update rule: {ex.Message}";
        }
        finally
        {
            this.IsSubmitting = false;
        }
    }

    /// <summary>
    /// Shows the delete confirmation dialog for the specified rule.
    /// </summary>
    /// <param name="rule">The rule to delete.</param>
    public void ConfirmDeleteRule(CategorizationRuleDto rule)
    {
        this.DeletingRule = rule;
        this.ShowDeleteConfirm = true;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Cancels the delete confirmation.
    /// </summary>
    public void CancelDelete()
    {
        this.ShowDeleteConfirm = false;
        this.DeletingRule = null;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Deletes the rule pending deletion via the API.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task DeleteRuleAsync()
    {
        if (this.DeletingRule is null)
        {
            return;
        }

        this.IsDeleting = true;

        try
        {
            var success = await this._apiService.DeleteCategorizationRuleAsync(this.DeletingRule.Id);
            if (success)
            {
                this.Rules.RemoveAll(r => r.Id == this.DeletingRule.Id);
                this.ShowDeleteConfirm = false;
                this.DeletingRule = null;
            }
            else
            {
                this.ErrorMessage = "Failed to delete rule.";
            }
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to delete rule: {ex.Message}";
        }
        finally
        {
            this.IsDeleting = false;
        }
    }

    /// <summary>
    /// Activates a rule via the API.
    /// </summary>
    /// <param name="rule">The rule to activate.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task ActivateRuleAsync(CategorizationRuleDto rule)
    {
        try
        {
            var success = await this._apiService.ActivateCategorizationRuleAsync(rule.Id);
            if (success)
            {
                var index = this.Rules.FindIndex(r => r.Id == rule.Id);
                if (index >= 0)
                {
                    this.Rules[index].IsActive = true;
                }
            }
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to activate rule: {ex.Message}";
        }
    }

    /// <summary>
    /// Deactivates a rule via the API.
    /// </summary>
    /// <param name="rule">The rule to deactivate.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task DeactivateRuleAsync(CategorizationRuleDto rule)
    {
        try
        {
            var success = await this._apiService.DeactivateCategorizationRuleAsync(rule.Id);
            if (success)
            {
                var index = this.Rules.FindIndex(r => r.Id == rule.Id);
                if (index >= 0)
                {
                    this.Rules[index].IsActive = false;
                }
            }
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to deactivate rule: {ex.Message}";
        }
    }

    /// <summary>
    /// Tests a pattern against existing transactions.
    /// </summary>
    /// <param name="request">The test pattern request.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task TestPatternAsync(TestPatternRequest request)
    {
        this.IsTesting = true;
        this.TestResult = null;
        this.NotifyStateChanged();

        try
        {
            this.TestResult = await this._apiService.TestPatternAsync(request);
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to test pattern: {ex.Message}";
        }
        finally
        {
            this.IsTesting = false;
        }
    }

    /// <summary>
    /// Shows the apply rules dialog.
    /// </summary>
    public void ShowApplyRules()
    {
        this.ShowApplyRulesDialog = true;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Hides the apply rules dialog.
    /// </summary>
    public void HideApplyRules()
    {
        this.ShowApplyRulesDialog = false;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Handles the response when rules have been applied.
    /// </summary>
    /// <param name="response">The apply rules response.</param>
    public void OnRulesApplied(ApplyRulesResponse response)
    {
        // The dialog shows the results, so we just need to handle any side effects
    }

    /// <summary>
    /// Creates a rule from the test panel pattern info.
    /// </summary>
    /// <param name="patternInfo">The pattern info tuple (Pattern, MatchType, CaseSensitive).</param>
    public void CreateRuleFromTest((string Pattern, string MatchType, bool CaseSensitive) patternInfo)
    {
        this.NewRule = new CategorizationRuleCreateDto
        {
            Pattern = patternInfo.Pattern,
            MatchType = patternInfo.MatchType,
            CaseSensitive = patternInfo.CaseSensitive,
        };
        this.TestResult = null;
        this.ShowAddForm = true;
        this.NotifyStateChanged();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this._scopeService.ScopeChanged -= this.OnScopeChanged;
    }

    private async void OnScopeChanged(BudgetScope? scope)
    {
        await this.LoadDataAsync();
        this.NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        this.OnStateChanged?.Invoke();
    }
}
