// <copyright file="RulesViewModel.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Models;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Shared.Budgeting;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BudgetExperiment.Client.ViewModels;

/// <summary>
/// ViewModel for the Rules page. Contains all handler logic, state fields,
/// and properties extracted from the Rules.razor @code block.
/// </summary>
public sealed class RulesViewModel : IDisposable
{
    private const string ViewModeStorageKey = "budget-experiment-rules-view-mode";
    private const string PageSizeStorageKey = "budget-experiment-rules-page-size";

    private readonly IBudgetApiService _apiService;
    private readonly IToastService _toastService;
    private readonly NavigationManager _navigationManager;
    private readonly ScopeService _scopeService;
    private readonly IApiErrorContext _apiErrorContext;
    private readonly IJSRuntime _jsRuntime;
    private readonly HashSet<string> _collapsedCategories = new(StringComparer.Ordinal);
    private readonly HashSet<Guid> _selectedRuleIds = new();
    private CancellationTokenSource? _searchDebounce;

    /// <summary>
    /// Initializes a new instance of the <see cref="RulesViewModel"/> class.
    /// </summary>
    /// <param name="apiService">The budget API service.</param>
    /// <param name="toastService">The toast notification service.</param>
    /// <param name="navigationManager">The navigation manager.</param>
    /// <param name="scopeService">The budget scope service.</param>
    /// <param name="apiErrorContext">The API error context for traceId capture.</param>
    /// <param name="jsRuntime">The JavaScript runtime for localStorage access.</param>
    public RulesViewModel(
        IBudgetApiService apiService,
        IToastService toastService,
        NavigationManager navigationManager,
        ScopeService scopeService,
        IApiErrorContext apiErrorContext,
        IJSRuntime jsRuntime)
    {
        this._apiService = apiService;
        this._toastService = toastService;
        this._navigationManager = navigationManager;
        this._scopeService = scopeService;
        this._apiErrorContext = apiErrorContext;
        this._jsRuntime = jsRuntime;
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
    /// Gets a value indicating whether a bulk operation is in progress.
    /// </summary>
    public bool IsBulkOperating { get; private set; }

    /// <summary>
    /// Gets the set of currently selected rule IDs.
    /// </summary>
    public IReadOnlySet<Guid> SelectedRuleIds => this._selectedRuleIds;

    /// <summary>
    /// Gets the number of currently selected rules.
    /// </summary>
    public int SelectedCount => this._selectedRuleIds.Count;

    /// <summary>
    /// Gets a value indicating whether any rules are currently selected.
    /// </summary>
    public bool HasSelection => this._selectedRuleIds.Count > 0;

    /// <summary>
    /// Gets a value indicating whether all rules on the current page are selected.
    /// </summary>
    public bool AreAllOnPageSelected =>
        this.Rules.Count > 0 && this.Rules.All(r => this._selectedRuleIds.Contains(r.Id));

    /// <summary>
    /// Gets a value indicating whether the bulk delete confirmation dialog is visible.
    /// </summary>
    public bool ShowBulkDeleteConfirm { get; private set; }

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
    /// Gets the current page number (1-based).
    /// </summary>
    public int CurrentPage { get; private set; } = 1;

    /// <summary>
    /// Gets the page size.
    /// </summary>
    public int PageSize { get; private set; } = 25;

    /// <summary>
    /// Gets the total count of matching rules.
    /// </summary>
    public int TotalCount { get; private set; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages { get; private set; }

    /// <summary>
    /// Gets the current search text filter.
    /// </summary>
    public string? SearchText { get; private set; }

    /// <summary>
    /// Gets the selected category ID filter.
    /// </summary>
    public Guid? SelectedCategoryId { get; private set; }

    /// <summary>
    /// Gets the selected status filter (null = all, "Active", "Inactive").
    /// </summary>
    public string? SelectedStatus { get; private set; }

    /// <summary>
    /// Gets the current sort field (e.g., "priority", "name", "category", "createdAt").
    /// </summary>
    public string? SortBy { get; private set; }

    /// <summary>
    /// Gets the current sort direction ("asc" or "desc").
    /// </summary>
    public string? SortDirection { get; private set; }

    /// <summary>
    /// Gets the current view mode (Table or Card).
    /// </summary>
    public RulesViewMode ViewMode { get; private set; } = RulesViewMode.Table;

    /// <summary>
    /// Gets the number of active rules in the current result set.
    /// </summary>
    public int ActiveRuleCount => this.Rules.Count(r => r.IsActive);

    /// <summary>
    /// Gets the number of rules on the current page (after filtering).
    /// </summary>
    public int FilteredCount => this.Rules.Count;

    /// <summary>
    /// Gets a value indicating whether rules are grouped by category.
    /// </summary>
    public bool IsGroupedByCategory { get; private set; }

    /// <summary>
    /// Gets the rules grouped by category name, sorted by priority within each group.
    /// Returns an empty dictionary when grouping is disabled.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<CategorizationRuleDto>> GroupedRules
    {
        get
        {
            if (!this.IsGroupedByCategory)
            {
                return new Dictionary<string, IReadOnlyList<CategorizationRuleDto>>();
            }

            return this.Rules
                .GroupBy(r => r.CategoryName ?? "Unknown")
                .OrderBy(g => g.Key)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyList<CategorizationRuleDto>)g.OrderBy(r => r.Priority).ToList());
        }
    }

    /// <summary>
    /// Gets a value indicating whether any filters are active.
    /// </summary>
    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(this.SearchText) ||
        this.SelectedCategoryId.HasValue ||
        !string.IsNullOrEmpty(this.SelectedStatus);

    /// <summary>
    /// Gets the number of active filters.
    /// </summary>
    public int ActiveFilterCount
    {
        get
        {
            var count = 0;
            if (!string.IsNullOrWhiteSpace(this.SearchText))
            {
                count++;
            }

            if (this.SelectedCategoryId.HasValue)
            {
                count++;
            }

            if (!string.IsNullOrEmpty(this.SelectedStatus))
            {
                count++;
            }

            return count;
        }
    }

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
        await this.LoadPreferencesAsync();
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
            var request = new CategorizationRuleListRequest
            {
                Page = this.CurrentPage,
                PageSize = this.PageSize,
                Search = this.SearchText,
                CategoryId = this.SelectedCategoryId,
                Status = this.SelectedStatus,
                SortBy = this.SortBy,
                SortDirection = this.SortDirection,
            };

            var rulesTask = this._apiService.GetCategorizationRulesPagedAsync(request);
            var categoriesTask = this._apiService.GetCategoriesAsync(activeOnly: true);

            await Task.WhenAll(rulesTask, categoriesTask);

            var pagedResult = await rulesTask;
            this.Rules = pagedResult.Items.ToList();
            this.TotalCount = pagedResult.TotalCount;
            this.TotalPages = pagedResult.TotalPages;
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

    /// <summary>
    /// Sets the search text with debounced data reload (300ms).
    /// </summary>
    /// <param name="search">The search text.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task SetSearchAsync(string? search)
    {
        this.SearchText = search;
        this._searchDebounce?.Cancel();
        this._searchDebounce = new CancellationTokenSource();

        try
        {
            await Task.Delay(300, this._searchDebounce.Token);
            this.CurrentPage = 1;
            await this.LoadDataAsync();
            this.NotifyStateChanged();
        }
        catch (TaskCanceledException)
        {
            // Debounce cancelled by a newer keystroke — expected
        }
    }

    /// <summary>
    /// Sets the category filter and reloads data.
    /// </summary>
    /// <param name="categoryId">The category ID, or null for all.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task SetCategoryFilterAsync(Guid? categoryId)
    {
        this.SelectedCategoryId = categoryId;
        this.CurrentPage = 1;
        await this.LoadDataAsync();
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Sets the status filter and reloads data.
    /// </summary>
    /// <param name="status">The status string ("Active", "Inactive"), or null for all.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task SetStatusFilterAsync(string? status)
    {
        this.SelectedStatus = status;
        this.CurrentPage = 1;
        await this.LoadDataAsync();
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Clears all filters and reloads data.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task ClearFiltersAsync()
    {
        this.SearchText = null;
        this.SelectedCategoryId = null;
        this.SelectedStatus = null;
        this.SortBy = null;
        this.SortDirection = null;
        this.CurrentPage = 1;
        await this.LoadDataAsync();
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Changes the current page and reloads data.
    /// </summary>
    /// <param name="page">The new page number.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task ChangePageAsync(int page)
    {
        this.CurrentPage = page;
        await this.LoadDataAsync();
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Changes the page size, resets to page 1, and reloads data.
    /// </summary>
    /// <param name="pageSize">The new page size.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task ChangePageSizeAsync(int pageSize)
    {
        this.PageSize = pageSize;
        this.CurrentPage = 1;
        await this.SavePreferenceAsync(PageSizeStorageKey, pageSize.ToString());
        await this.LoadDataAsync();
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Sets the view mode and persists the preference to localStorage.
    /// </summary>
    /// <param name="mode">The view mode to set.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task SetViewModeAsync(RulesViewMode mode)
    {
        if (this.ViewMode == mode)
        {
            return;
        }

        this.ViewMode = mode;
        await this.SavePreferenceAsync(ViewModeStorageKey, mode.ToString());
        this.NotifyStateChanged();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this._scopeService.ScopeChanged -= this.OnScopeChanged;
        this._searchDebounce?.Cancel();
        this._searchDebounce?.Dispose();
    }

    /// <summary>
    /// Toggles selection of a single rule.
    /// </summary>
    /// <param name="ruleId">The rule ID to toggle.</param>
    public void ToggleRuleSelection(Guid ruleId)
    {
        if (!this._selectedRuleIds.Remove(ruleId))
        {
            this._selectedRuleIds.Add(ruleId);
        }

        this.NotifyStateChanged();
    }

    /// <summary>
    /// Returns whether a specific rule is currently selected.
    /// </summary>
    /// <param name="ruleId">The rule ID.</param>
    /// <returns>True if the rule is selected.</returns>
    public bool IsRuleSelected(Guid ruleId) => this._selectedRuleIds.Contains(ruleId);

    /// <summary>
    /// Selects all rules on the current page.
    /// </summary>
    public void SelectAllOnPage()
    {
        foreach (var rule in this.Rules)
        {
            this._selectedRuleIds.Add(rule.Id);
        }

        this.NotifyStateChanged();
    }

    /// <summary>
    /// Deselects all rules on the current page.
    /// </summary>
    public void DeselectAllOnPage()
    {
        foreach (var rule in this.Rules)
        {
            this._selectedRuleIds.Remove(rule.Id);
        }

        this.NotifyStateChanged();
    }

    /// <summary>
    /// Toggles the select-all state: selects all if not all are selected, otherwise deselects all.
    /// </summary>
    public void ToggleSelectAllOnPage()
    {
        if (this.AreAllOnPageSelected)
        {
            this.DeselectAllOnPage();
        }
        else
        {
            this.SelectAllOnPage();
        }
    }

    /// <summary>
    /// Clears all selections.
    /// </summary>
    public void ClearSelection()
    {
        this._selectedRuleIds.Clear();
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Shows the bulk delete confirmation dialog.
    /// </summary>
    public void ConfirmBulkDelete()
    {
        this.ShowBulkDeleteConfirm = true;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Cancels the bulk delete confirmation.
    /// </summary>
    public void CancelBulkDelete()
    {
        this.ShowBulkDeleteConfirm = false;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Bulk deletes the currently selected rules.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task BulkDeleteAsync()
    {
        if (this._selectedRuleIds.Count == 0)
        {
            return;
        }

        this.IsBulkOperating = true;
        this.ShowBulkDeleteConfirm = false;
        this.NotifyStateChanged();

        try
        {
            var ids = this._selectedRuleIds.ToList();
            var result = await this._apiService.BulkDeleteCategorizationRulesAsync(ids);
            if (result is not null)
            {
                this._toastService.ShowSuccess($"Deleted {result.AffectedCount} rule(s).");
                this._selectedRuleIds.Clear();
                await this.LoadDataAsync();
            }
            else
            {
                this.ErrorMessage = "Failed to bulk delete rules.";
            }
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to bulk delete rules: {ex.Message}";
        }
        finally
        {
            this.IsBulkOperating = false;
        }
    }

    /// <summary>
    /// Bulk activates the currently selected rules.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task BulkActivateAsync()
    {
        if (this._selectedRuleIds.Count == 0)
        {
            return;
        }

        this.IsBulkOperating = true;
        this.NotifyStateChanged();

        try
        {
            var ids = this._selectedRuleIds.ToList();
            var result = await this._apiService.BulkActivateCategorizationRulesAsync(ids);
            if (result is not null)
            {
                this._toastService.ShowSuccess($"Activated {result.AffectedCount} rule(s).");
                this._selectedRuleIds.Clear();
                await this.LoadDataAsync();
            }
            else
            {
                this.ErrorMessage = "Failed to bulk activate rules.";
            }
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to bulk activate rules: {ex.Message}";
        }
        finally
        {
            this.IsBulkOperating = false;
        }
    }

    /// <summary>
    /// Bulk deactivates the currently selected rules.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task BulkDeactivateAsync()
    {
        if (this._selectedRuleIds.Count == 0)
        {
            return;
        }

        this.IsBulkOperating = true;
        this.NotifyStateChanged();

        try
        {
            var ids = this._selectedRuleIds.ToList();
            var result = await this._apiService.BulkDeactivateCategorizationRulesAsync(ids);
            if (result is not null)
            {
                this._toastService.ShowSuccess($"Deactivated {result.AffectedCount} rule(s).");
                this._selectedRuleIds.Clear();
                await this.LoadDataAsync();
            }
            else
            {
                this.ErrorMessage = "Failed to bulk deactivate rules.";
            }
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to bulk deactivate rules: {ex.Message}";
        }
        finally
        {
            this.IsBulkOperating = false;
        }
    }

    /// <summary>
    /// Toggles sorting by the specified field. If already sorting by this field, reverses direction.
    /// A new field defaults to ascending.
    /// </summary>
    /// <param name="field">The sort field name (e.g., "priority", "name", "category", "createdAt").</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task ToggleSortAsync(string field)
    {
        if (this.SortBy == field)
        {
            this.SortDirection = this.SortDirection == "asc" ? "desc" : "asc";
        }
        else
        {
            this.SortBy = field;
            this.SortDirection = "asc";
        }

        this.CurrentPage = 1;
        await this.LoadDataAsync();
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Toggles the group-by-category view mode.
    /// </summary>
    public void ToggleGroupByCategory()
    {
        this.IsGroupedByCategory = !this.IsGroupedByCategory;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Toggles the collapsed state of a category group.
    /// </summary>
    /// <param name="categoryName">The category name whose collapse state to toggle.</param>
    public void ToggleCategoryCollapse(string categoryName)
    {
        if (!this._collapsedCategories.Add(categoryName))
        {
            this._collapsedCategories.Remove(categoryName);
        }

        this.NotifyStateChanged();
    }

    /// <summary>
    /// Returns whether a category group is collapsed.
    /// </summary>
    /// <param name="categoryName">The category name.</param>
    /// <returns>True if the category group is collapsed.</returns>
    public bool IsCategoryCollapsed(string categoryName) =>
        this._collapsedCategories.Contains(categoryName);

    private async Task LoadPreferencesAsync()
    {
        try
        {
            var savedViewMode = await this._jsRuntime.InvokeAsync<string?>(
                "localStorage.getItem", ViewModeStorageKey);
            if (!string.IsNullOrEmpty(savedViewMode) && Enum.TryParse<RulesViewMode>(savedViewMode, out var mode))
            {
                this.ViewMode = mode;
            }

            var savedPageSize = await this._jsRuntime.InvokeAsync<string?>(
                "localStorage.getItem", PageSizeStorageKey);
            if (!string.IsNullOrEmpty(savedPageSize) && int.TryParse(savedPageSize, out var size) && size is 10 or 25 or 50 or 100)
            {
                this.PageSize = size;
            }
        }
        catch (JSException)
        {
            // Ignore localStorage errors (SSR / prerender)
        }
    }

    private async Task SavePreferenceAsync(string key, string value)
    {
        try
        {
            await this._jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);
        }
        catch (JSException)
        {
            // Ignore localStorage errors
        }
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
