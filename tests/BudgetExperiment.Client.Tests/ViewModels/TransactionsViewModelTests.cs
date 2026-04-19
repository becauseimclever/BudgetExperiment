// <copyright file="TransactionsViewModelTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Models;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Client.Tests.TestHelpers;
using BudgetExperiment.Client.ViewModels;
using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Components;

using Shouldly;

namespace BudgetExperiment.Client.Tests.ViewModels;

/// <summary>
/// Unit tests for <see cref="TransactionsViewModel"/>.
/// </summary>
public sealed class TransactionsViewModelTests
{
    private readonly StubBudgetApiService _apiService = new();
    private readonly StubToastService _toastService = new();
    private readonly StubNavigationManager _navigationManager = new();
    private readonly StubApiErrorContext _apiErrorContext = new();
    private readonly TransactionsViewModel _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionsViewModelTests"/> class.
    /// </summary>
    public TransactionsViewModelTests()
    {
        _sut = new TransactionsViewModel(
            _apiService,
            _toastService,
            _navigationManager,
            _apiErrorContext);
    }

    // --- Initialization ---

    /// <summary>
    /// Verifies that InitializeAsync loads transactions from the API.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_LoadsTransactions()
    {
        _apiService.UnifiedPage = CreatePageWithItems(3);

        await _sut.InitializeAsync();

        _sut.PageData.Items.Count.ShouldBe(3);
    }

    /// <summary>
    /// Verifies that InitializeAsync sets IsLoading to false after loading.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_SetsIsLoadingToFalse()
    {
        await _sut.InitializeAsync();

        _sut.IsLoading.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that InitializeAsync loads accounts for the filter dropdown.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_LoadsAccounts()
    {
        _apiService.Accounts.Add(new AccountDto { Id = Guid.NewGuid(), Name = "Checking" });

        await _sut.InitializeAsync();

        _sut.Accounts.Count.ShouldBe(1);
        _sut.Accounts[0].Name.ShouldBe("Checking");
    }

    /// <summary>
    /// Verifies that InitializeAsync loads categories for the filter dropdown.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_LoadsCategories()
    {
        _apiService.Categories.Add(new BudgetCategoryDto { Id = Guid.NewGuid(), Name = "Groceries" });

        await _sut.InitializeAsync();

        _sut.Categories.Count.ShouldBe(1);
        _sut.Categories[0].Name.ShouldBe("Groceries");
    }

    /// <summary>
    /// Verifies that InitializeAsync handles API failure gracefully.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_SetsErrorMessage_WhenApiFails()
    {
        _apiService.GetAccountsException = new HttpRequestException("Server error");

        await _sut.InitializeAsync();

        _sut.ErrorMessage.ShouldNotBeNull();
        _sut.IsLoading.ShouldBeFalse();
    }

    // --- Filter State ---

    /// <summary>
    /// Verifies that filter defaults are applied correctly.
    /// </summary>
    [Fact]
    public void Filter_HasCorrectDefaults()
    {
        _sut.Filter.SortBy.ShouldBe("date");
        _sut.Filter.SortDescending.ShouldBeTrue();
        _sut.Filter.Page.ShouldBe(1);
        _sut.Filter.PageSize.ShouldBe(50);
        _sut.Filter.AccountId.ShouldBeNull();
        _sut.Filter.CategoryId.ShouldBeNull();
        _sut.Filter.Uncategorized.ShouldBeNull();
    }

    // --- ApplyFiltersAsync ---

    /// <summary>
    /// Verifies that ApplyFiltersAsync resets page to 1 and reloads.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ApplyFiltersAsync_ResetsPageToOne()
    {
        _sut.Filter.Page = 3;

        await _sut.ApplyFiltersAsync();

        _sut.Filter.Page.ShouldBe(1);
    }

    /// <summary>
    /// Verifies that ApplyFiltersAsync passes the filter to the API service.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ApplyFiltersAsync_PassesFilterToApi()
    {
        var accountId = Guid.NewGuid();
        _sut.Filter.AccountId = accountId;
        _sut.Filter.Description = "coffee";

        await _sut.ApplyFiltersAsync();

        _apiService.LastUnifiedFilter.ShouldNotBeNull();
        _apiService.LastUnifiedFilter!.AccountId.ShouldBe(accountId);
        _apiService.LastUnifiedFilter!.Description.ShouldBe("coffee");
    }

    /// <summary>
    /// Verifies that ApplyFiltersAsync clears selection.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ApplyFiltersAsync_ClearsSelection()
    {
        _sut.SelectedTransactionIds.Add(Guid.NewGuid());

        await _sut.ApplyFiltersAsync();

        _sut.SelectedTransactionIds.ShouldBeEmpty();
    }

    // --- Sorting ---

    /// <summary>
    /// Verifies that ToggleSortAsync sets sort field when different from current.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ToggleSortAsync_SetsSortField_WhenNewField()
    {
        await _sut.ToggleSortAsync("amount");

        _apiService.LastUnifiedFilter.ShouldNotBeNull();
        _apiService.LastUnifiedFilter!.SortBy.ShouldBe("amount");
        _apiService.LastUnifiedFilter!.SortDescending.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that ToggleSortAsync toggles direction when same field.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ToggleSortAsync_TogglesDirection_WhenSameField()
    {
        _sut.Filter.SortBy = "date";
        _sut.Filter.SortDescending = true;

        await _sut.ToggleSortAsync("date");

        _sut.Filter.SortDescending.ShouldBeFalse();
    }

    // --- Pagination ---

    /// <summary>
    /// Verifies that GoToPageAsync navigates to specified page.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GoToPageAsync_NavigatesToSpecifiedPage()
    {
        _apiService.UnifiedPage = new UnifiedTransactionPageDto { TotalCount = 100, PageSize = 50, Page = 1 };

        await _sut.GoToPageAsync(2);

        _apiService.LastUnifiedFilter.ShouldNotBeNull();
        _apiService.LastUnifiedFilter!.Page.ShouldBe(2);
    }

    /// <summary>
    /// Verifies that GoToPageAsync clears selection.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GoToPageAsync_ClearsSelection()
    {
        _sut.SelectedTransactionIds.Add(Guid.NewGuid());

        await _sut.GoToPageAsync(2);

        _sut.SelectedTransactionIds.ShouldBeEmpty();
    }

    /// <summary>
    /// Verifies that ChangePageSizeAsync updates page size and resets to page 1.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ChangePageSizeAsync_ResetsToPageOne()
    {
        _sut.Filter.Page = 3;

        await _sut.ChangePageSizeAsync(25);

        _sut.Filter.PageSize.ShouldBe(25);
        _apiService.LastUnifiedFilter.ShouldNotBeNull();
        _apiService.LastUnifiedFilter!.Page.ShouldBe(1);
    }

    // --- Selection ---

    /// <summary>
    /// Verifies that ToggleSelection adds an item when not selected.
    /// </summary>
    [Fact]
    public void ToggleSelection_AddsItem_WhenNotSelected()
    {
        var id = Guid.NewGuid();

        _sut.ToggleSelection(id);

        _sut.SelectedTransactionIds.ShouldContain(id);
    }

    /// <summary>
    /// Verifies that ToggleSelection removes an item when already selected.
    /// </summary>
    [Fact]
    public void ToggleSelection_RemovesItem_WhenAlreadySelected()
    {
        var id = Guid.NewGuid();
        _sut.SelectedTransactionIds.Add(id);

        _sut.ToggleSelection(id);

        _sut.SelectedTransactionIds.ShouldNotContain(id);
    }

    /// <summary>
    /// Verifies that ToggleSelectAll selects all current page items.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ToggleSelectAll_SelectsAllItems_WhenNoneSelected()
    {
        _apiService.UnifiedPage = CreatePageWithItems(3);
        await _sut.InitializeAsync();

        _sut.ToggleSelectAll();

        _sut.SelectedTransactionIds.Count.ShouldBe(3);
    }

    /// <summary>
    /// Verifies that ToggleSelectAll deselects all when all are selected.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ToggleSelectAll_DeselectsAll_WhenAllSelected()
    {
        _apiService.UnifiedPage = CreatePageWithItems(2);
        await _sut.InitializeAsync();

        // Select all
        foreach (var item in _sut.PageData.Items)
        {
            _sut.SelectedTransactionIds.Add(item.Id);
        }

        _sut.ToggleSelectAll();

        _sut.SelectedTransactionIds.ShouldBeEmpty();
    }

    /// <summary>
    /// Verifies IsSelected returns correct state.
    /// </summary>
    [Fact]
    public void IsSelected_ReturnsTrueForSelectedItem()
    {
        var id = Guid.NewGuid();
        _sut.SelectedTransactionIds.Add(id);

        _sut.IsSelected(id).ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that SelectedCount reflects the count of selected items.
    /// </summary>
    [Fact]
    public void SelectedCount_ReturnsCorrectCount()
    {
        _sut.SelectedTransactionIds.Add(Guid.NewGuid());
        _sut.SelectedTransactionIds.Add(Guid.NewGuid());

        _sut.SelectedCount.ShouldBe(2);
    }

    /// <summary>
    /// Verifies that AllSelected returns true when all page items are selected.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AllSelected_ReturnsTrue_WhenAllPageItemsSelected()
    {
        _apiService.UnifiedPage = CreatePageWithItems(2);
        await _sut.InitializeAsync();

        foreach (var item in _sut.PageData.Items)
        {
            _sut.SelectedTransactionIds.Add(item.Id);
        }

        _sut.AllSelected.ShouldBeTrue();
    }

    // --- ClearFilters ---

    /// <summary>
    /// Verifies that ClearFiltersAsync resets all filter fields and reloads.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ClearFiltersAsync_ResetsAllFilters()
    {
        _sut.Filter.AccountId = Guid.NewGuid();
        _sut.Filter.CategoryId = Guid.NewGuid();
        _sut.Filter.Description = "search";
        _sut.Filter.MinAmount = 10m;
        _sut.Filter.MaxAmount = 100m;
        _sut.Filter.Uncategorized = true;
        _sut.Filter.StartDate = new DateOnly(2025, 1, 1);
        _sut.Filter.EndDate = new DateOnly(2025, 12, 31);

        await _sut.ClearFiltersAsync();

        _sut.Filter.AccountId.ShouldBeNull();
        _sut.Filter.CategoryId.ShouldBeNull();
        _sut.Filter.Description.ShouldBeNull();
        _sut.Filter.MinAmount.ShouldBeNull();
        _sut.Filter.MaxAmount.ShouldBeNull();
        _sut.Filter.Uncategorized.ShouldBeNull();
        _sut.Filter.StartDate.ShouldBeNull();
        _sut.Filter.EndDate.ShouldBeNull();
        _sut.Filter.Page.ShouldBe(1);
    }

    // --- ActiveFilterCount ---

    /// <summary>
    /// Verifies that ActiveFilterCount returns 0 when no filters are set.
    /// </summary>
    [Fact]
    public void ActiveFilterCount_ReturnsZero_WhenNoFilters()
    {
        _sut.ActiveFilterCount.ShouldBe(0);
    }

    /// <summary>
    /// Verifies that ActiveFilterCount counts each active filter.
    /// </summary>
    [Fact]
    public void ActiveFilterCount_CountsActiveFilters()
    {
        _sut.Filter.AccountId = Guid.NewGuid();
        _sut.Filter.Description = "test";
        _sut.Filter.MinAmount = 5m;

        _sut.ActiveFilterCount.ShouldBe(3);
    }

    // --- ShowBalanceColumn ---

    /// <summary>
    /// Verifies that ShowBalanceColumn is true when a single account is filtered.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ShowBalanceColumn_ReturnsTrue_WhenSingleAccountFiltered()
    {
        _sut.Filter.AccountId = Guid.NewGuid();
        _apiService.UnifiedPage = new UnifiedTransactionPageDto
        {
            BalanceInfo = new AccountBalanceInfoDto
            {
                InitialBalance = new MoneyDto { Amount = 1000m, Currency = "USD" },
                CurrentBalance = new MoneyDto { Amount = 1500m, Currency = "USD" },
            },
        };

        await _sut.LoadTransactionsAsync();

        _sut.ShowBalanceColumn.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that ShowBalanceColumn is false when no account filter is set.
    /// </summary>
    [Fact]
    public void ShowBalanceColumn_ReturnsFalse_WhenNoAccountFilter()
    {
        _sut.ShowBalanceColumn.ShouldBeFalse();
    }

    // --- Error Handling ---

    /// <summary>
    /// Verifies that RetryLoadAsync reloads data and clears error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RetryLoadAsync_ReloadsData()
    {
        // Cause initial failure
        _apiService.GetAccountsException = new HttpRequestException("fail");
        await _sut.InitializeAsync();
        _sut.ErrorMessage.ShouldNotBeNull();

        // Fix the API and retry
        _apiService.GetAccountsException = null;
        _apiService.UnifiedPage = CreatePageWithItems(2);
        await _sut.RetryLoadAsync();

        _sut.ErrorMessage.ShouldBeNull();
        _sut.PageData.Items.Count.ShouldBe(2);
    }

    /// <summary>
    /// Verifies that DismissError clears the error message.
    /// </summary>
    [Fact]
    public void DismissError_ClearsErrorMessage()
    {
        // Simulate error state via reflection or public setter
        _sut.DismissError();

        _sut.ErrorMessage.ShouldBeNull();
    }

    // --- State Change Notification ---

    /// <summary>
    /// Verifies that OnStateChanged is invoked when state changes.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_InvokesOnStateChanged()
    {
        var stateChangedCount = 0;
        _sut.OnStateChanged = () => stateChangedCount++;

        await _sut.InitializeAsync();

        stateChangedCount.ShouldBeGreaterThan(0);
    }

    // --- URL Query Parameter Binding ---

    /// <summary>
    /// Verifies that ApplyQueryParameters sets account filter from query string.
    /// </summary>
    [Fact]
    public void ApplyQueryParameters_SetsAccountId()
    {
        var id = Guid.NewGuid();
        _sut.ApplyQueryParameters(id.ToString(), null, null, null, null, null, null, null, null, null, null, null, null);

        _sut.Filter.AccountId.ShouldBe(id);
    }

    /// <summary>
    /// Verifies that ApplyQueryParameters sets uncategorized filter.
    /// </summary>
    [Fact]
    public void ApplyQueryParameters_SetsUncategorizedFilter()
    {
        _sut.ApplyQueryParameters(null, null, true, null, null, null, null, null, null, null, null, null, null);

        _sut.Filter.Uncategorized.ShouldBe(true);
    }

    /// <summary>
    /// Verifies that ApplyQueryParameters sets date range from query strings.
    /// </summary>
    [Fact]
    public void ApplyQueryParameters_SetsDateRange()
    {
        _sut.ApplyQueryParameters(null, null, null, "2026-01-01", "2026-01-31", null, null, null, null, null, null, null, null);

        _sut.Filter.StartDate.ShouldBe(new DateOnly(2026, 1, 1));
        _sut.Filter.EndDate.ShouldBe(new DateOnly(2026, 1, 31));
    }

    /// <summary>
    /// Verifies that ApplyQueryParameters sets description filter.
    /// </summary>
    [Fact]
    public void ApplyQueryParameters_SetsDescription()
    {
        _sut.ApplyQueryParameters(null, null, null, null, null, "coffee", null, null, null, null, null, null, null);

        _sut.Filter.Description.ShouldBe("coffee");
    }

    /// <summary>
    /// Verifies that ApplyQueryParameters sets amount range.
    /// </summary>
    [Fact]
    public void ApplyQueryParameters_SetsAmountRange()
    {
        _sut.ApplyQueryParameters(null, null, null, null, null, null, 10m, 100m, null, null, null, null, null);

        _sut.Filter.MinAmount.ShouldBe(10m);
        _sut.Filter.MaxAmount.ShouldBe(100m);
    }

    /// <summary>
    /// Verifies that ApplyQueryParameters sets sort parameters.
    /// </summary>
    [Fact]
    public void ApplyQueryParameters_SetsSortParameters()
    {
        _sut.ApplyQueryParameters(null, null, null, null, null, null, null, null, "amount", false, null, null, null);

        _sut.Filter.SortBy.ShouldBe("amount");
        _sut.Filter.SortDescending.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that ApplyQueryParameters sets pagination.
    /// </summary>
    [Fact]
    public void ApplyQueryParameters_SetsPagination()
    {
        _sut.ApplyQueryParameters(null, null, null, null, null, null, null, null, null, null, 3, 25, null);

        _sut.Filter.Page.ShouldBe(3);
        _sut.Filter.PageSize.ShouldBe(25);
    }

    /// <summary>
    /// Verifies that ApplyQueryParameters ignores invalid account ID strings.
    /// </summary>
    [Fact]
    public void ApplyQueryParameters_IgnoresInvalidAccountId()
    {
        _sut.ApplyQueryParameters("not-a-guid", null, null, null, null, null, null, null, null, null, null, null, null);

        _sut.Filter.AccountId.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that BuildUrlWithFilters returns base path when no filters active.
    /// </summary>
    [Fact]
    public void BuildUrlWithFilters_ReturnsBasePath_WhenNoFilters()
    {
        _sut.BuildUrlWithFilters().ShouldBe("/transactions");
    }

    /// <summary>
    /// Verifies that BuildUrlWithFilters includes account filter in URL.
    /// </summary>
    [Fact]
    public void BuildUrlWithFilters_IncludesAccountFilter()
    {
        var id = Guid.NewGuid();
        _sut.Filter.AccountId = id;

        _sut.BuildUrlWithFilters().ShouldContain($"account={id}");
    }

    /// <summary>
    /// Verifies that BuildUrlWithFilters includes uncategorized flag.
    /// </summary>
    [Fact]
    public void BuildUrlWithFilters_IncludesUncategorizedFlag()
    {
        _sut.Filter.Uncategorized = true;

        _sut.BuildUrlWithFilters().ShouldContain("uncategorized=true");
    }

    /// <summary>
    /// Verifies that BuildUrlWithFilters includes date range.
    /// </summary>
    [Fact]
    public void BuildUrlWithFilters_IncludesDateRange()
    {
        _sut.Filter.StartDate = new DateOnly(2026, 3, 1);
        _sut.Filter.EndDate = new DateOnly(2026, 3, 31);

        var url = _sut.BuildUrlWithFilters();
        url.ShouldContain("startDate=2026-03-01");
        url.ShouldContain("endDate=2026-03-31");
    }

    /// <summary>
    /// Verifies that ApplyFiltersAsync updates the URL.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ApplyFiltersAsync_UpdatesUrl()
    {
        _sut.Filter.AccountId = Guid.NewGuid();

        await _sut.ApplyFiltersAsync();

        _navigationManager.LastNavigatedUri.ShouldNotBeNull();
        _navigationManager.LastNavigatedUri.ShouldContain("account=");
    }

    /// <summary>
    /// Verifies that ClearFiltersAsync resets URL to base path.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ClearFiltersAsync_ResetsUrl()
    {
        _sut.Filter.AccountId = Guid.NewGuid();
        await _sut.ApplyFiltersAsync();

        await _sut.ClearFiltersAsync();

        _navigationManager.LastNavigatedUri.ShouldBe("/transactions");
    }

    // --- Inline Actions ---

    /// <summary>
    /// Verifies that UpdateTransactionCategoryAsync shows success toast when API returns a result.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateTransactionCategoryAsync_ShowsSuccessToast_WhenApiReturnsResult()
    {
        var categoryId = Guid.NewGuid();
        _apiService.UpdateTransactionCategoryResult = new TransactionDto { Id = Guid.NewGuid() };
        _sut.Categories.Add(new BudgetCategoryDto { Id = categoryId, Name = "Groceries" });

        await _sut.UpdateTransactionCategoryAsync(Guid.NewGuid(), categoryId);

        _toastService.LastSuccessMessage.ShouldNotBeNull();
        _toastService.LastSuccessMessage.ShouldContain("Groceries");
    }

    /// <summary>
    /// Verifies that UpdateTransactionCategoryAsync shows success toast with "Uncategorized" when clearing category.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateTransactionCategoryAsync_ShowsUncategorizedToast_WhenClearingCategory()
    {
        _apiService.UpdateTransactionCategoryResult = new TransactionDto { Id = Guid.NewGuid() };

        await _sut.UpdateTransactionCategoryAsync(Guid.NewGuid(), null);

        _toastService.LastSuccessMessage.ShouldNotBeNull();
        _toastService.LastSuccessMessage.ShouldContain("Uncategorized");
    }

    /// <summary>
    /// Verifies that UpdateTransactionCategoryAsync shows error toast when API returns null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateTransactionCategoryAsync_ShowsErrorToast_WhenApiFails()
    {
        _apiService.UpdateTransactionCategoryResult = null;

        await _sut.UpdateTransactionCategoryAsync(Guid.NewGuid(), Guid.NewGuid());

        _toastService.LastErrorMessage.ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies that UpdateTransactionCategoryAsync reloads transactions on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateTransactionCategoryAsync_ReloadsTransactions_OnSuccess()
    {
        _apiService.UpdateTransactionCategoryResult = new TransactionDto { Id = Guid.NewGuid() };
        _apiService.UnifiedPage = CreatePageWithItems(3);

        await _sut.UpdateTransactionCategoryAsync(Guid.NewGuid(), Guid.NewGuid());

        _sut.PageData.Items.Count.ShouldBe(3);
    }

    /// <summary>
    /// Verifies that DeleteTransactionAsync shows success toast and reloads.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteTransactionAsync_ShowsSuccessToast_WhenApiReturnsTrue()
    {
        _apiService.DeleteTransactionResult = true;
        _apiService.UnifiedPage = CreatePageWithItems(2);

        await _sut.DeleteTransactionAsync(Guid.NewGuid());

        _toastService.LastSuccessMessage.ShouldNotBeNull();
        _toastService.LastSuccessMessage.ShouldContain("deleted");
    }

    /// <summary>
    /// Verifies that DeleteTransactionAsync shows error toast when API returns false.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteTransactionAsync_ShowsErrorToast_WhenApiFails()
    {
        _apiService.DeleteTransactionResult = false;

        await _sut.DeleteTransactionAsync(Guid.NewGuid());

        _toastService.LastErrorMessage.ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies that DeleteTransactionAsync removes the ID from selected set.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteTransactionAsync_RemovesFromSelection()
    {
        var txnId = Guid.NewGuid();
        _apiService.DeleteTransactionResult = true;
        _sut.SelectedTransactionIds.Add(txnId);

        await _sut.DeleteTransactionAsync(txnId);

        _sut.SelectedTransactionIds.ShouldNotContain(txnId);
    }

    // --- Bulk Categorize ---

    /// <summary>
    /// Verifies that OpenBulkCategorize clears category and shows picker.
    /// </summary>
    [Fact]
    public void OpenBulkCategorize_ShowsPickerAndClearsCategoryId()
    {
        _sut.BulkCategoryId = Guid.NewGuid();

        _sut.OpenBulkCategorize();

        _sut.ShowBulkCategorize.ShouldBeTrue();
        _sut.BulkCategoryId.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that CloseBulkCategorize hides the picker.
    /// </summary>
    [Fact]
    public void CloseBulkCategorize_HidesPicker()
    {
        _sut.OpenBulkCategorize();

        _sut.CloseBulkCategorize();

        _sut.ShowBulkCategorize.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that ExecuteBulkCategorizeAsync does nothing when no category selected.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteBulkCategorizeAsync_DoesNothing_WhenNoCategorySelected()
    {
        _sut.SelectedTransactionIds.Add(Guid.NewGuid());
        _sut.BulkCategoryId = null;

        await _sut.ExecuteBulkCategorizeAsync();

        _apiService.LastBulkCategorizeRequest.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that ExecuteBulkCategorizeAsync does nothing when no transactions selected.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteBulkCategorizeAsync_DoesNothing_WhenNoSelection()
    {
        _sut.BulkCategoryId = Guid.NewGuid();

        await _sut.ExecuteBulkCategorizeAsync();

        _apiService.LastBulkCategorizeRequest.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that ExecuteBulkCategorizeAsync sends correct request, shows toast, clears selection, and reloads.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteBulkCategorizeAsync_CategorizesAndShowsToast()
    {
        var categoryId = Guid.NewGuid();
        var txnId1 = Guid.NewGuid();
        var txnId2 = Guid.NewGuid();
        _sut.SelectedTransactionIds.Add(txnId1);
        _sut.SelectedTransactionIds.Add(txnId2);
        _sut.BulkCategoryId = categoryId;
        _sut.Categories.Add(new BudgetCategoryDto { Id = categoryId, Name = "Food" });
        _apiService.BulkCategorizeResult = new BulkCategorizeResponse { SuccessCount = 2 };
        _apiService.UnifiedPage = CreatePageWithItems(1);

        await _sut.ExecuteBulkCategorizeAsync();

        _apiService.LastBulkCategorizeRequest.ShouldNotBeNull();
        _apiService.LastBulkCategorizeRequest!.CategoryId.ShouldBe(categoryId);
        _apiService.LastBulkCategorizeRequest.TransactionIds.ShouldContain(txnId1);
        _apiService.LastBulkCategorizeRequest.TransactionIds.ShouldContain(txnId2);
        _toastService.LastSuccessMessage.ShouldNotBeNull();
        _toastService.LastSuccessMessage.ShouldContain("Food");
        _sut.SelectedTransactionIds.ShouldBeEmpty();
        _sut.ShowBulkCategorize.ShouldBeFalse();
        _sut.IsBulkOperationInProgress.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that ExecuteBulkCategorizeAsync resets IsBulkOperationInProgress on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteBulkCategorizeAsync_ShowsErrorToast_OnException()
    {
        _sut.SelectedTransactionIds.Add(Guid.NewGuid());
        _sut.BulkCategoryId = Guid.NewGuid();
        _apiService.BulkCategorizeException = new InvalidOperationException("API error");

        await _sut.ExecuteBulkCategorizeAsync();

        _toastService.LastErrorMessage.ShouldNotBeNull();
        _sut.IsBulkOperationInProgress.ShouldBeFalse();
    }

    // --- Bulk Delete ---

    /// <summary>
    /// Verifies that ExecuteBulkDeleteAsync does nothing when no transactions selected.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteBulkDeleteAsync_DoesNothing_WhenNoSelection()
    {
        _apiService.DeleteTransactionResult = true;

        await _sut.ExecuteBulkDeleteAsync();

        _toastService.LastSuccessMessage.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that ExecuteBulkDeleteAsync deletes all selected transactions.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteBulkDeleteAsync_DeletesAndShowsToast()
    {
        _sut.SelectedTransactionIds.Add(Guid.NewGuid());
        _sut.SelectedTransactionIds.Add(Guid.NewGuid());
        _apiService.DeleteTransactionResult = true;
        _apiService.UnifiedPage = CreatePageWithItems(0);

        await _sut.ExecuteBulkDeleteAsync();

        _toastService.LastSuccessMessage.ShouldNotBeNull();
        _toastService.LastSuccessMessage.ShouldContain("2");
        _sut.SelectedTransactionIds.ShouldBeEmpty();
        _sut.IsBulkOperationInProgress.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that ExecuteBulkDeleteAsync shows error toast on exception.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteBulkDeleteAsync_ShowsErrorToast_OnException()
    {
        _sut.SelectedTransactionIds.Add(Guid.NewGuid());
        _apiService.DeleteTransactionException = new InvalidOperationException("fail");

        await _sut.ExecuteBulkDeleteAsync();

        _toastService.LastErrorMessage.ShouldNotBeNull();
        _sut.IsBulkOperationInProgress.ShouldBeFalse();
    }

    // --- Create Rule ---

    /// <summary>
    /// Verifies that OpenCreateRule pre-fills rule with description and shows modal.
    /// </summary>
    [Fact]
    public void OpenCreateRule_PrefillsRuleAndShowsModal()
    {
        _sut.OpenCreateRule("GROCERY STORE");

        _sut.ShowCreateRule.ShouldBeTrue();
        _sut.NewRule.Pattern.ShouldBe("GROCERY STORE");
        _sut.NewRule.Name.ShouldBe("GROCERY STORE");
        _sut.NewRule.MatchType.ShouldBe("Contains");
    }

    /// <summary>
    /// Verifies that CloseCreateRule hides the modal.
    /// </summary>
    [Fact]
    public void CloseCreateRule_HidesModal()
    {
        _sut.OpenCreateRule("Test");

        _sut.CloseCreateRule();

        _sut.ShowCreateRule.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that CreateRuleAsync shows success toast and closes modal on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateRuleAsync_ShowsSuccessToast_OnSuccess()
    {
        _apiService.CreateRuleResult = new CategorizationRuleDto
        {
            Id = Guid.NewGuid(),
            Name = "Grocery Rule",
        };
        _sut.OpenCreateRule("GROCERY STORE");

        await _sut.CreateRuleAsync();

        _toastService.LastSuccessMessage.ShouldNotBeNull();
        _toastService.LastSuccessMessage.ShouldContain("Grocery Rule");
        _sut.ShowCreateRule.ShouldBeFalse();
        _sut.IsCreatingRule.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that CreateRuleAsync shows error toast when API returns null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateRuleAsync_ShowsErrorToast_WhenApiReturnsNull()
    {
        _apiService.CreateRuleResult = null;
        _sut.OpenCreateRule("Test");

        await _sut.CreateRuleAsync();

        _toastService.LastErrorMessage.ShouldNotBeNull();
        _sut.IsCreatingRule.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that CreateRuleAsync shows error toast on exception.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateRuleAsync_ShowsErrorToast_OnException()
    {
        _apiService.CreateRuleException = new InvalidOperationException("API down");
        _sut.OpenCreateRule("Test");

        await _sut.CreateRuleAsync();

        _toastService.LastErrorMessage.ShouldNotBeNull();
        _toastService.LastErrorMessage.ShouldContain("API down");
        _sut.IsCreatingRule.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that CreateRuleAsync reloads transactions on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateRuleAsync_ReloadsTransactions_OnSuccess()
    {
        _apiService.CreateRuleResult = new CategorizationRuleDto { Id = Guid.NewGuid(), Name = "R" };
        _apiService.UnifiedPage = CreatePageWithItems(5);
        _sut.OpenCreateRule("Test");

        await _sut.CreateRuleAsync();

        _sut.PageData.Items.Count.ShouldBe(5);
    }

    /// <summary>
    /// Verifies that CreateRuleAsync shows the apply-rules prompt after successful creation.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateRuleAsync_ShowsApplyRulesPrompt_OnSuccess()
    {
        _apiService.CreateRuleResult = new CategorizationRuleDto { Id = Guid.NewGuid(), Name = "Grocery Rule" };
        _sut.OpenCreateRule("GROCERY STORE");

        await _sut.CreateRuleAsync();

        _sut.ShowApplyRulesPrompt.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that CreateRuleAsync does not show the apply-rules prompt on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateRuleAsync_DoesNotShowApplyRulesPrompt_OnFailure()
    {
        _apiService.CreateRuleResult = null;
        _sut.OpenCreateRule("Test");

        await _sut.CreateRuleAsync();

        _sut.ShowApplyRulesPrompt.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that ApplyRulesAfterCreationAsync calls the API and shows a success toast.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ApplyRulesAfterCreationAsync_CallsApi_ShowsSuccessToast()
    {
        _apiService.ApplyRulesResult = new ApplyRulesResponse { Categorized = 3 };

        await _sut.ApplyRulesAfterCreationAsync();

        _toastService.LastSuccessMessage.ShouldNotBeNull();
        _toastService.LastSuccessMessage.ShouldContain("3");
        _sut.ShowApplyRulesPrompt.ShouldBeFalse();
        _sut.IsApplyingRules.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that ApplyRulesAfterCreationAsync shows error toast on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ApplyRulesAfterCreationAsync_ShowsErrorToast_OnFailure()
    {
        _apiService.ApplyRulesResult = null;

        await _sut.ApplyRulesAfterCreationAsync();

        _toastService.LastErrorMessage.ShouldNotBeNull();
        _sut.ShowApplyRulesPrompt.ShouldBeFalse();
        _sut.IsApplyingRules.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that DismissApplyRulesPrompt hides the prompt.
    /// </summary>
    [Fact]
    public void DismissApplyRulesPrompt_HidesPrompt()
    {
        _sut.DismissApplyRulesPrompt();

        _sut.ShowApplyRulesPrompt.ShouldBeFalse();
    }

    // --- Suggestions ---

    /// <summary>
    /// Verifies that LoadSuggestionsAsync populates Suggestions when uncategorized transactions exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task LoadSuggestionsAsync_PopulatesSuggestions_WhenUncategorizedExist()
    {
        var txnId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        _apiService.UnifiedPage = new UnifiedTransactionPageDto
        {
            Items = new List<UnifiedTransactionItemDto>
            {
                new() { Id = txnId, Date = new DateOnly(2026, 1, 15), Description = "WALMART", Amount = new MoneyDto { Amount = -25m, Currency = "USD" }, AccountId = Guid.NewGuid(), AccountName = "Checking", CategoryId = null },
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 50,
        };
        _apiService.BatchSuggestionsResult = new BatchSuggestCategoriesResponse
        {
            Suggestions = new Dictionary<Guid, InlineCategorySuggestionDto>
            {
                [txnId] = new() { TransactionId = txnId, CategoryId = categoryId, CategoryName = "Groceries" },
            },
        };

        await _sut.InitializeAsync();

        _sut.Suggestions.ShouldContainKey(txnId);
        _sut.Suggestions[txnId].CategoryName.ShouldBe("Groceries");
    }

    /// <summary>
    /// Verifies that LoadSuggestionsAsync clears suggestions when no uncategorized transactions exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task LoadSuggestionsAsync_ClearsSuggestions_WhenNoUncategorized()
    {
        _apiService.UnifiedPage = new UnifiedTransactionPageDto
        {
            Items = new List<UnifiedTransactionItemDto>
            {
                new() { Id = Guid.NewGuid(), Date = new DateOnly(2026, 1, 15), Description = "WALMART", Amount = new MoneyDto { Amount = -25m, Currency = "USD" }, AccountId = Guid.NewGuid(), AccountName = "Checking", CategoryId = Guid.NewGuid(), CategoryName = "Groceries" },
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 50,
        };

        await _sut.InitializeAsync();

        _sut.Suggestions.ShouldBeEmpty();
    }

    /// <summary>
    /// Verifies that LoadSuggestionsAsync sets IsLoadingSuggestions to false after loading.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task LoadSuggestionsAsync_SetsIsLoadingSuggestionsToFalse()
    {
        _apiService.UnifiedPage = CreatePageWithItems(1);

        await _sut.InitializeAsync();

        _sut.IsLoadingSuggestions.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that AcceptSuggestionAsync assigns the suggested category and removes the suggestion.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AcceptSuggestionAsync_AssignsCategoryAndRemovesSuggestion()
    {
        var txnId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        _apiService.UnifiedPage = new UnifiedTransactionPageDto
        {
            Items = new List<UnifiedTransactionItemDto>
            {
                new() { Id = txnId, Date = new DateOnly(2026, 1, 15), Description = "WALMART", Amount = new MoneyDto { Amount = -25m, Currency = "USD" }, AccountId = Guid.NewGuid(), AccountName = "Checking", CategoryId = null },
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 50,
        };
        _apiService.BatchSuggestionsResult = new BatchSuggestCategoriesResponse
        {
            Suggestions = new Dictionary<Guid, InlineCategorySuggestionDto>
            {
                [txnId] = new() { TransactionId = txnId, CategoryId = categoryId, CategoryName = "Groceries" },
            },
        };
        _apiService.UpdateTransactionCategoryResult = new TransactionDto
        {
            Id = txnId,
            Date = new DateOnly(2026, 1, 15),
            Description = "WALMART",
            Amount = new MoneyDto { Amount = -25m, Currency = "USD" },
            AccountId = Guid.NewGuid(),
        };

        await _sut.InitializeAsync();
        await _sut.AcceptSuggestionAsync(txnId);

        _sut.Suggestions.ShouldNotContainKey(txnId);
        _toastService.LastSuccessMessage.ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies that AcceptSuggestionAsync does nothing when no suggestion exists for the transaction.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AcceptSuggestionAsync_DoesNothing_WhenNoSuggestion()
    {
        _apiService.UnifiedPage = CreatePageWithItems(1);

        await _sut.InitializeAsync();
        await _sut.AcceptSuggestionAsync(Guid.NewGuid());

        _sut.Suggestions.ShouldBeEmpty();
    }

    private static UnifiedTransactionPageDto CreatePageWithItems(int count)
    {
        var items = new List<UnifiedTransactionItemDto>();
        for (int i = 0; i < count; i++)
        {
            items.Add(new UnifiedTransactionItemDto
            {
                Id = Guid.NewGuid(),
                Date = new DateOnly(2026, 1, 15),
                Description = $"Transaction {i + 1}",
                Amount = new MoneyDto { Amount = -25.00m + i, Currency = "USD" },
                AccountId = Guid.NewGuid(),
                AccountName = "Checking",
            });
        }

        return new UnifiedTransactionPageDto
        {
            Items = items,
            TotalCount = count,
            Page = 1,
            PageSize = 50,
        };
    }

    /// <summary>
    /// Stub toast service for ViewModel testing.
    /// </summary>
    private sealed class StubToastService : IToastService
    {
        /// <inheritdoc/>
        public event Action? OnChange;

        /// <inheritdoc/>
        public IReadOnlyList<ToastItem> Toasts { get; } = [];

        public string? LastSuccessMessage
        {
            get; private set;
        }

        public string? LastErrorMessage
        {
            get; private set;
        }

        /// <inheritdoc/>
        public void ShowSuccess(string message, string? title = null)
        {
            this.LastSuccessMessage = message;
        }

        /// <inheritdoc/>
        public void ShowError(string message, string? title = null)
        {
            this.LastErrorMessage = message;
        }

        /// <inheritdoc/>
        public void ShowInfo(string message, string? title = null)
        {
        }

        /// <inheritdoc/>
        public void ShowWarning(string message, string? title = null)
        {
            this.OnChange?.Invoke();
        }

        /// <inheritdoc/>
        public void Remove(Guid id)
        {
        }
    }

    /// <summary>
    /// Stub NavigationManager for testing navigation calls.
    /// </summary>
    private sealed class StubNavigationManager : NavigationManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StubNavigationManager"/> class.
        /// </summary>
        public StubNavigationManager()
        {
            this.Initialize("https://localhost/", "https://localhost/");
        }

        /// <summary>
        /// Gets the last URI navigated to.
        /// </summary>
        public string? LastNavigatedUri
        {
            get; private set;
        }

        /// <inheritdoc/>
        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            this.LastNavigatedUri = uri;
        }

        /// <inheritdoc/>
        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
            this.LastNavigatedUri = uri;
        }
    }
}
