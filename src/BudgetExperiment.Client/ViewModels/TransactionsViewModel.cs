// <copyright file="TransactionsViewModel.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Client.ViewModels;

/// <summary>
/// ViewModel for the unified Transactions page. Manages filter state, pagination,
/// sorting, selection, and data loading for the /transactions route.
/// </summary>
public sealed class TransactionsViewModel : IDisposable
{
    private readonly IBudgetApiService _apiService;
    private readonly IToastService _toastService;
    private readonly Microsoft.AspNetCore.Components.NavigationManager _navigation;
    private readonly ScopeService _scopeService;
    private readonly IApiErrorContext _apiErrorContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionsViewModel"/> class.
    /// </summary>
    /// <param name="apiService">The budget API service.</param>
    /// <param name="toastService">The toast notification service.</param>
    /// <param name="navigation">The navigation manager.</param>
    /// <param name="scopeService">The budget scope service.</param>
    /// <param name="apiErrorContext">The API error context for traceId capture.</param>
    public TransactionsViewModel(
        IBudgetApiService apiService,
        IToastService toastService,
        Microsoft.AspNetCore.Components.NavigationManager navigation,
        ScopeService scopeService,
        IApiErrorContext apiErrorContext)
    {
        _apiService = apiService;
        _toastService = toastService;
        _navigation = navigation;
        _scopeService = scopeService;
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
    /// Gets the current filter, sort, and pagination state.
    /// </summary>
    public UnifiedTransactionFilterDto Filter { get; private set; } = new();

    /// <summary>
    /// Gets the current page of transaction data.
    /// </summary>
    public UnifiedTransactionPageDto PageData { get; private set; } = new();

    /// <summary>
    /// Gets the list of accounts for the filter dropdown.
    /// </summary>
    public List<AccountDto> Accounts { get; private set; } = new();

    /// <summary>
    /// Gets the list of categories for the filter dropdown.
    /// </summary>
    public List<BudgetCategoryDto> Categories { get; private set; } = new();

    /// <summary>
    /// Gets the set of selected transaction IDs for bulk operations.
    /// </summary>
    public HashSet<Guid> SelectedTransactionIds { get; private set; } = new();

    /// <summary>
    /// Gets the category suggestions for uncategorized transactions, keyed by transaction ID.
    /// </summary>
    public Dictionary<Guid, InlineCategorySuggestionDto> Suggestions { get; private set; } = new();

    /// <summary>
    /// Gets a value indicating whether suggestions are currently loading.
    /// </summary>
    public bool IsLoadingSuggestions
    {
        get; private set;
    }

    /// <summary>
    /// Gets the number of selected transactions.
    /// </summary>
    public int SelectedCount => this.SelectedTransactionIds.Count;

    /// <summary>
    /// Gets a value indicating whether all items on the current page are selected.
    /// </summary>
    public bool AllSelected =>
        this.PageData.Items.Count > 0 &&
        this.PageData.Items.All(item => this.SelectedTransactionIds.Contains(item.Id));

    /// <summary>
    /// Gets a value indicating whether a bulk operation is in progress.
    /// </summary>
    public bool IsBulkOperationInProgress
    {
        get; private set;
    }

    /// <summary>
    /// Gets a value indicating whether the bulk categorize picker is visible.
    /// </summary>
    public bool ShowBulkCategorize
    {
        get; private set;
    }

    /// <summary>
    /// Gets or sets the selected category ID for bulk categorization.
    /// </summary>
    public Guid? BulkCategoryId
    {
        get; set;
    }

    /// <summary>
    /// Gets a value indicating whether the create-rule modal is visible.
    /// </summary>
    public bool ShowCreateRule
    {
        get; private set;
    }

    /// <summary>
    /// Gets a value indicating whether rule creation is in progress.
    /// </summary>
    public bool IsCreatingRule
    {
        get; private set;
    }

    /// <summary>
    /// Gets the new rule model pre-filled from a transaction description.
    /// </summary>
    public CategorizationRuleCreateDto NewRule { get; private set; } = new();

    /// <summary>
    /// Gets a value indicating whether the apply-rules prompt is visible after creating a rule.
    /// </summary>
    public bool ShowApplyRulesPrompt
    {
        get; private set;
    }

    /// <summary>
    /// Gets a value indicating whether rules are being applied to matching transactions.
    /// </summary>
    public bool IsApplyingRules
    {
        get; private set;
    }

    /// <summary>
    /// Gets the number of active filter criteria.
    /// </summary>
    public int ActiveFilterCount
    {
        get
        {
            int count = 0;
            if (this.Filter.AccountId.HasValue)
            {
                count++;
            }

            if (this.Filter.CategoryId.HasValue)
            {
                count++;
            }

            if (this.Filter.Uncategorized == true)
            {
                count++;
            }

            if (this.Filter.StartDate.HasValue)
            {
                count++;
            }

            if (this.Filter.EndDate.HasValue)
            {
                count++;
            }

            if (!string.IsNullOrWhiteSpace(this.Filter.Description))
            {
                count++;
            }

            if (this.Filter.MinAmount.HasValue)
            {
                count++;
            }

            if (this.Filter.MaxAmount.HasValue)
            {
                count++;
            }

            if (!string.IsNullOrWhiteSpace(this.Filter.KakeiboCategory))
            {
                count++;
            }

            return count;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the balance column should be shown (single account filtered with balance info).
    /// </summary>
    public bool ShowBalanceColumn =>
        this.Filter.AccountId.HasValue && this.PageData.BalanceInfo != null;

    /// <summary>
    /// Applies filter state from URL query parameters. Call before <see cref="InitializeAsync"/>.
    /// </summary>
    /// <param name="accountId">Account ID filter.</param>
    /// <param name="categoryId">Category ID filter.</param>
    /// <param name="uncategorized">Uncategorized filter flag.</param>
    /// <param name="startDate">Start date (yyyy-MM-dd).</param>
    /// <param name="endDate">End date (yyyy-MM-dd).</param>
    /// <param name="description">Description search text.</param>
    /// <param name="minAmount">Minimum amount.</param>
    /// <param name="maxAmount">Maximum amount.</param>
    /// <param name="sortBy">Sort field.</param>
    /// <param name="sortDesc">Sort descending flag.</param>
    /// <param name="page">Page number.</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="kakeiboCategory">Kakeibo category filter.</param>
    public void ApplyQueryParameters(
        string? accountId,
        string? categoryId,
        bool? uncategorized,
        string? startDate,
        string? endDate,
        string? description,
        decimal? minAmount,
        decimal? maxAmount,
        string? sortBy,
        bool? sortDesc,
        int? page,
        int? pageSize,
        string? kakeiboCategory = null)
    {
        if (Guid.TryParse(accountId, out var aid))
        {
            this.Filter.AccountId = aid;
        }

        if (Guid.TryParse(categoryId, out var cid))
        {
            this.Filter.CategoryId = cid;
        }

        if (uncategorized == true)
        {
            this.Filter.Uncategorized = true;
        }

        if (DateOnly.TryParse(startDate, out var sd))
        {
            this.Filter.StartDate = sd;
        }

        if (DateOnly.TryParse(endDate, out var ed))
        {
            this.Filter.EndDate = ed;
        }

        if (!string.IsNullOrWhiteSpace(description))
        {
            this.Filter.Description = description;
        }

        if (minAmount.HasValue)
        {
            this.Filter.MinAmount = minAmount;
        }

        if (maxAmount.HasValue)
        {
            this.Filter.MaxAmount = maxAmount;
        }

        if (!string.IsNullOrWhiteSpace(sortBy))
        {
            this.Filter.SortBy = sortBy;
        }

        if (sortDesc.HasValue)
        {
            this.Filter.SortDescending = sortDesc.Value;
        }

        if (page.HasValue && page.Value > 0)
        {
            this.Filter.Page = page.Value;
        }

        if (pageSize.HasValue && pageSize.Value > 0)
        {
            this.Filter.PageSize = pageSize.Value;
        }

        if (!string.IsNullOrWhiteSpace(kakeiboCategory))
        {
            this.Filter.KakeiboCategory = kakeiboCategory;
        }
    }

    /// <summary>
    /// Builds a query string from the current filter state for URL synchronization.
    /// Only includes non-default values.
    /// </summary>
    /// <returns>A URL path with query string (e.g., "/transactions?account=...&amp;uncategorized=true").</returns>
    public string BuildUrlWithFilters()
    {
        var parts = new List<string>();

        if (this.Filter.AccountId.HasValue)
        {
            parts.Add($"account={this.Filter.AccountId}");
        }

        if (this.Filter.CategoryId.HasValue)
        {
            parts.Add($"category={this.Filter.CategoryId}");
        }

        if (this.Filter.Uncategorized == true)
        {
            parts.Add("uncategorized=true");
        }

        if (this.Filter.StartDate.HasValue)
        {
            parts.Add($"startDate={this.Filter.StartDate.Value:yyyy-MM-dd}");
        }

        if (this.Filter.EndDate.HasValue)
        {
            parts.Add($"endDate={this.Filter.EndDate.Value:yyyy-MM-dd}");
        }

        if (!string.IsNullOrWhiteSpace(this.Filter.Description))
        {
            parts.Add($"description={Uri.EscapeDataString(this.Filter.Description)}");
        }

        if (this.Filter.MinAmount.HasValue)
        {
            parts.Add($"minAmount={this.Filter.MinAmount.Value}");
        }

        if (this.Filter.MaxAmount.HasValue)
        {
            parts.Add($"maxAmount={this.Filter.MaxAmount.Value}");
        }

        if (!string.IsNullOrWhiteSpace(this.Filter.KakeiboCategory))
        {
            parts.Add($"kakeiboCategory={Uri.EscapeDataString(this.Filter.KakeiboCategory)}");
        }

        if (this.Filter.SortBy != "date")
        {
            parts.Add($"sortBy={this.Filter.SortBy}");
        }

        if (!this.Filter.SortDescending)
        {
            parts.Add("sortDesc=false");
        }

        if (this.Filter.Page > 1)
        {
            parts.Add($"page={this.Filter.Page}");
        }

        if (this.Filter.PageSize != 50)
        {
            parts.Add($"pageSize={this.Filter.PageSize}");
        }

        return parts.Count > 0
            ? $"/transactions?{string.Join("&", parts)}"
            : "/transactions";
    }

    /// <summary>
    /// Updates the browser URL to reflect the current filter state without triggering navigation.
    /// </summary>
    public void UpdateUrl()
    {
        var url = this.BuildUrlWithFilters();
        _navigation.NavigateTo(url, replace: true);
    }

    /// <summary>
    /// Initializes the ViewModel: subscribes to scope changes, loads reference data and transactions.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task InitializeAsync()
    {
        _scopeService.ScopeChanged += this.OnScopeChanged;
        await this.LoadAllDataAsync();
    }

    /// <summary>
    /// Loads transactions from the API using the current filter state.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task LoadTransactionsAsync()
    {
        this.IsLoading = true;
        this.ErrorMessage = null;
        this.ErrorTraceId = null;
        this.NotifyStateChanged();

        try
        {
            this.PageData = await _apiService.GetUnifiedTransactionsAsync(this.Filter);
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to load transactions: {ex.Message}";
            this.ErrorTraceId = _apiErrorContext.LastTraceId;
        }
        finally
        {
            this.IsLoading = false;
            this.NotifyStateChanged();
        }

        await this.LoadSuggestionsAsync();
    }

    /// <summary>
    /// Applies the current filters, resets to page 1, clears selection, and reloads.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task ApplyFiltersAsync()
    {
        this.Filter.Page = 1;
        this.ClearSelection();
        await this.LoadTransactionsAsync();
        this.UpdateUrl();
    }

    /// <summary>
    /// Clears all filter values and reloads transactions.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task ClearFiltersAsync()
    {
        this.Filter.AccountId = null;
        this.Filter.CategoryId = null;
        this.Filter.Uncategorized = null;
        this.Filter.StartDate = null;
        this.Filter.EndDate = null;
        this.Filter.Description = null;
        this.Filter.MinAmount = null;
        this.Filter.MaxAmount = null;
        this.Filter.KakeiboCategory = null;
        this.Filter.Page = 1;
        this.ClearSelection();
        await this.LoadTransactionsAsync();
        this.UpdateUrl();
    }

    /// <summary>
    /// Toggles sorting by the specified field. If already sorting by this field, reverses direction.
    /// </summary>
    /// <param name="field">The sort field name.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task ToggleSortAsync(string field)
    {
        if (this.Filter.SortBy == field)
        {
            this.Filter.SortDescending = !this.Filter.SortDescending;
        }
        else
        {
            this.Filter.SortBy = field;
            this.Filter.SortDescending = true;
        }

        await this.LoadTransactionsAsync();
        this.UpdateUrl();
    }

    /// <summary>
    /// Navigates to the specified page number.
    /// </summary>
    /// <param name="page">The page number (1-based).</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task GoToPageAsync(int page)
    {
        this.Filter.Page = page;
        this.ClearSelection();
        await this.LoadTransactionsAsync();
        this.UpdateUrl();
    }

    /// <summary>
    /// Changes the page size, resets to page 1, and reloads.
    /// </summary>
    /// <param name="pageSize">The new page size.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task ChangePageSizeAsync(int pageSize)
    {
        this.Filter.PageSize = pageSize;
        this.Filter.Page = 1;
        this.ClearSelection();
        await this.LoadTransactionsAsync();
        this.UpdateUrl();
    }

    /// <summary>
    /// Updates the category on a single transaction via the PATCH endpoint.
    /// </summary>
    /// <param name="transactionId">The transaction ID.</param>
    /// <param name="categoryId">The category ID, or null to clear.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task UpdateTransactionCategoryAsync(Guid transactionId, Guid? categoryId)
    {
        var result = await _apiService.UpdateTransactionCategoryAsync(transactionId, categoryId);
        if (result != null)
        {
            var categoryName = categoryId.HasValue
                ? this.Categories.FirstOrDefault(c => c.Id == categoryId)?.Name
                : null;
            _toastService.ShowSuccess($"Category updated to {categoryName ?? "Uncategorized"}.");
            await this.LoadTransactionsAsync();
        }
        else
        {
            _toastService.ShowError("Failed to update category.");
        }
    }

    /// <summary>
    /// Deletes a transaction, shows toast, and reloads.
    /// </summary>
    /// <param name="transactionId">The transaction ID.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task DeleteTransactionAsync(Guid transactionId)
    {
        var deleted = await _apiService.DeleteTransactionAsync(transactionId);
        if (deleted)
        {
            this.SelectedTransactionIds.Remove(transactionId);
            _toastService.ShowSuccess("Transaction deleted.");
            await this.LoadTransactionsAsync();
        }
        else
        {
            _toastService.ShowError("Failed to delete transaction.");
        }
    }

    /// <summary>
    /// Toggles selection of a transaction by ID.
    /// </summary>
    /// <param name="transactionId">The transaction ID.</param>
    public void ToggleSelection(Guid transactionId)
    {
        if (!this.SelectedTransactionIds.Remove(transactionId))
        {
            this.SelectedTransactionIds.Add(transactionId);
        }

        this.NotifyStateChanged();
    }

    /// <summary>
    /// Toggles select-all for the current page items.
    /// </summary>
    public void ToggleSelectAll()
    {
        if (this.AllSelected)
        {
            this.SelectedTransactionIds.Clear();
        }
        else
        {
            foreach (var item in this.PageData.Items)
            {
                this.SelectedTransactionIds.Add(item.Id);
            }
        }

        this.NotifyStateChanged();
    }

    /// <summary>
    /// Returns whether a transaction is currently selected.
    /// </summary>
    /// <param name="transactionId">The transaction ID.</param>
    /// <returns>True if the transaction is selected.</returns>
    public bool IsSelected(Guid transactionId) => this.SelectedTransactionIds.Contains(transactionId);

    /// <summary>
    /// Opens the bulk categorize picker UI.
    /// </summary>
    public void OpenBulkCategorize()
    {
        this.BulkCategoryId = null;
        this.ShowBulkCategorize = true;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Closes the bulk categorize picker UI.
    /// </summary>
    public void CloseBulkCategorize()
    {
        this.ShowBulkCategorize = false;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Applies the selected category to all selected transactions.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task ExecuteBulkCategorizeAsync()
    {
        if (!this.BulkCategoryId.HasValue || this.SelectedTransactionIds.Count == 0)
        {
            return;
        }

        this.IsBulkOperationInProgress = true;
        this.NotifyStateChanged();

        try
        {
            var request = new BulkCategorizeRequest
            {
                TransactionIds = this.SelectedTransactionIds.ToList(),
                CategoryId = this.BulkCategoryId.Value,
            };
            var result = await _apiService.BulkCategorizeTransactionsAsync(request);
            var categoryName = this.Categories.FirstOrDefault(c => c.Id == this.BulkCategoryId)?.Name ?? "Unknown";
            _toastService.ShowSuccess($"Categorized {result.SuccessCount} transaction(s) as {categoryName}.");
            this.ShowBulkCategorize = false;
            this.ClearSelection();
            await this.LoadTransactionsAsync();
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"Bulk categorize failed: {ex.Message}");
        }
        finally
        {
            this.IsBulkOperationInProgress = false;
            this.NotifyStateChanged();
        }
    }

    /// <summary>
    /// Deletes all selected transactions after confirmation.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task ExecuteBulkDeleteAsync()
    {
        if (this.SelectedTransactionIds.Count == 0)
        {
            return;
        }

        this.IsBulkOperationInProgress = true;
        this.NotifyStateChanged();

        try
        {
            var ids = this.SelectedTransactionIds.ToList();
            int successCount = 0;
            foreach (var id in ids)
            {
                var deleted = await _apiService.DeleteTransactionAsync(id);
                if (deleted)
                {
                    successCount++;
                }
            }

            _toastService.ShowSuccess($"Deleted {successCount} transaction(s).");
            this.ClearSelection();
            await this.LoadTransactionsAsync();
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"Bulk delete failed: {ex.Message}");
        }
        finally
        {
            this.IsBulkOperationInProgress = false;
            this.NotifyStateChanged();
        }
    }

    /// <summary>
    /// Opens the create-rule modal pre-filled with the transaction's description.
    /// </summary>
    /// <param name="description">The transaction description to use as the pattern.</param>
    public void OpenCreateRule(string description)
    {
        this.NewRule = new CategorizationRuleCreateDto
        {
            Pattern = description,
            Name = description,
            MatchType = "Contains",
        };
        this.ShowCreateRule = true;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Closes the create-rule modal.
    /// </summary>
    public void CloseCreateRule()
    {
        this.ShowCreateRule = false;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Creates a categorization rule from the current <see cref="NewRule"/> state.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task CreateRuleAsync()
    {
        this.IsCreatingRule = true;
        this.NotifyStateChanged();

        try
        {
            var created = await _apiService.CreateCategorizationRuleAsync(this.NewRule);
            if (created != null)
            {
                _toastService.ShowSuccess($"Rule '{created.Name}' created.");
                this.ShowCreateRule = false;
                this.ShowApplyRulesPrompt = true;
                await this.LoadTransactionsAsync();
            }
            else
            {
                _toastService.ShowError("Failed to create rule.");
            }
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"Failed to create rule: {ex.Message}");
        }
        finally
        {
            this.IsCreatingRule = false;
            this.NotifyStateChanged();
        }
    }

    /// <summary>
    /// Applies categorization rules to uncategorized transactions after a rule was just created.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task ApplyRulesAfterCreationAsync()
    {
        this.IsApplyingRules = true;
        this.NotifyStateChanged();

        try
        {
            var result = await _apiService.ApplyCategorizationRulesAsync(new ApplyRulesRequest());
            if (result != null)
            {
                _toastService.ShowSuccess($"Applied rules: {result.Categorized} transaction(s) categorized.");
                await this.LoadTransactionsAsync();
            }
            else
            {
                _toastService.ShowError("Failed to apply rules.");
            }
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"Failed to apply rules: {ex.Message}");
        }
        finally
        {
            this.ShowApplyRulesPrompt = false;
            this.IsApplyingRules = false;
            this.NotifyStateChanged();
        }
    }

    /// <summary>
    /// Dismisses the apply-rules prompt without applying rules.
    /// </summary>
    public void DismissApplyRulesPrompt()
    {
        this.ShowApplyRulesPrompt = false;
        this.NotifyStateChanged();
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
            await this.LoadAllDataAsync();
        }
        finally
        {
            this.IsRetrying = false;
            this.NotifyStateChanged();
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
    /// Loads category suggestions for uncategorized transactions on the current page.
    /// Suggestions are loaded only when there are uncategorized transactions visible.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task LoadSuggestionsAsync()
    {
        var uncategorizedIds = this.PageData.Items
            .Where(t => t.CategoryId is null)
            .Select(t => t.Id)
            .ToList();

        if (uncategorizedIds.Count == 0)
        {
            this.Suggestions.Clear();
            this.NotifyStateChanged();
            return;
        }

        this.IsLoadingSuggestions = true;
        this.NotifyStateChanged();

        try
        {
            var response = await _apiService.GetBatchCategorySuggestionsAsync(uncategorizedIds);
            this.Suggestions = response.Suggestions;
        }
        catch
        {
            this.Suggestions.Clear();
        }
        finally
        {
            this.IsLoadingSuggestions = false;
            this.NotifyStateChanged();
        }
    }

    /// <summary>
    /// Accepts a category suggestion for a transaction (assigns the suggested category).
    /// </summary>
    /// <param name="transactionId">The transaction ID.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task AcceptSuggestionAsync(Guid transactionId)
    {
        if (!this.Suggestions.TryGetValue(transactionId, out var suggestion))
        {
            return;
        }

        await this.UpdateTransactionCategoryAsync(transactionId, suggestion.CategoryId);
        this.Suggestions.Remove(transactionId);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _scopeService.ScopeChanged -= this.OnScopeChanged;
    }

    private async Task LoadAllDataAsync()
    {
        this.IsLoading = true;
        this.ErrorMessage = null;
        this.ErrorTraceId = null;
        this.NotifyStateChanged();

        try
        {
            var accountsTask = _apiService.GetAccountsAsync();
            var categoriesTask = _apiService.GetCategoriesAsync(activeOnly: true);
            var transactionsTask = _apiService.GetUnifiedTransactionsAsync(this.Filter);

            await Task.WhenAll(accountsTask, categoriesTask, transactionsTask);

            this.Accounts = accountsTask.Result.ToList();
            this.Categories = categoriesTask.Result.ToList();
            this.PageData = transactionsTask.Result;
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to load transactions: {ex.Message}";
            this.ErrorTraceId = _apiErrorContext.LastTraceId;
        }
        finally
        {
            this.IsLoading = false;
            this.NotifyStateChanged();
        }

        await this.LoadSuggestionsAsync();
    }

    private void ClearSelection()
    {
        this.SelectedTransactionIds.Clear();
    }

    private async void OnScopeChanged(BudgetScope? scope)
    {
        await this.LoadAllDataAsync();
        this.NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        this.OnStateChanged?.Invoke();
    }
}
