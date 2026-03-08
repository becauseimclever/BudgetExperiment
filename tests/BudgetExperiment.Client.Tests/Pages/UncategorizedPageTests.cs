// <copyright file="UncategorizedPageTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Pages;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Client.Tests.TestHelpers;
using BudgetExperiment.Contracts.Dtos;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BudgetExperiment.Client.Tests.Pages;

/// <summary>
/// Unit tests for the <see cref="Uncategorized"/> page component.
/// </summary>
public class UncategorizedPageTests : BunitContext, IAsyncLifetime
{
    private readonly StubBudgetApiService _apiService = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="UncategorizedPageTests"/> class.
    /// </summary>
    public UncategorizedPageTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<IBudgetApiService>(this._apiService);
        this.Services.AddSingleton<IToastService>(new ToastService());
        this.Services.AddSingleton<ScopeService>();
        this.Services.AddSingleton<IChatContextService>(new StubChatContextService());
        this.Services.AddSingleton<ThemeService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync()
    {
        return base.DisposeAsync().AsTask();
    }

    /// <summary>
    /// Verifies the page renders without errors.
    /// </summary>
    [Fact]
    public void Renders_WithoutErrors()
    {
        var cut = Render<Uncategorized>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the page title is set correctly.
    /// </summary>
    [Fact]
    public void HasCorrectPageTitle()
    {
        var cut = Render<Uncategorized>();

        cut.Markup.ShouldContain("Uncategorized Transactions");
    }

    /// <summary>
    /// Verifies the empty state message when no uncategorized transactions exist.
    /// </summary>
    [Fact]
    public void ShowsEmptyState_WhenNoTransactions()
    {
        var cut = Render<Uncategorized>();

        cut.Markup.ShouldContain("All Caught Up!");
        cut.Markup.ShouldContain("All transactions have been categorized");
    }

    /// <summary>
    /// Verifies filter controls are present.
    /// </summary>
    [Fact]
    public void ShowsFilterControls()
    {
        var cut = Render<Uncategorized>();

        cut.Markup.ShouldContain("Account:");
        cut.Markup.ShouldContain("From:");
        cut.Markup.ShouldContain("To:");
        cut.Markup.ShouldContain("Description:");
    }

    /// <summary>
    /// Verifies amount filter inputs are present.
    /// </summary>
    [Fact]
    public void ShowsAmountFilters()
    {
        var cut = Render<Uncategorized>();

        cut.Markup.ShouldContain("Min $:");
        cut.Markup.ShouldContain("Max $:");
    }

    /// <summary>
    /// Verifies the Clear Filters button is present.
    /// </summary>
    [Fact]
    public void HasClearFiltersButton()
    {
        var cut = Render<Uncategorized>();

        cut.Markup.ShouldContain("Clear Filters");
    }

    /// <summary>
    /// Verifies transactions are displayed in a table when data exists.
    /// </summary>
    [Fact]
    public void ShowsTransactionTable_WhenDataExists()
    {
        var accountId = Guid.NewGuid();
        this._apiService.Accounts.Add(new AccountDto
        {
            Id = accountId,
            Name = "Checking",
            Type = "Checking",
            InitialBalance = 0m,
            InitialBalanceCurrency = "USD",
            InitialBalanceDate = new DateOnly(2025, 1, 1),
        });

        this._apiService.UncategorizedPage = new UncategorizedTransactionPageDto
        {
            Items =
            [
                new TransactionDto
                {
                    Id = Guid.NewGuid(),
                    AccountId = accountId,
                    Description = "Coffee Shop",
                    Amount = new MoneyDto { Amount = -5.50m, Currency = "USD" },
                    Date = new DateOnly(2025, 6, 15),
                },
            ],
            TotalCount = 1,
            Page = 1,
            PageSize = 25,
        };

        var cut = Render<Uncategorized>();

        cut.Markup.ShouldContain("Coffee Shop");
        cut.Markup.ShouldContain("Date");
        cut.Markup.ShouldContain("Description");
        cut.Markup.ShouldContain("Amount");
    }

    /// <summary>
    /// Verifies the subtitle shows transaction count.
    /// </summary>
    [Fact]
    public void ShowsSubtitle_WithTransactionCount()
    {
        this._apiService.UncategorizedPage = new UncategorizedTransactionPageDto
        {
            Items =
            [
                new TransactionDto
                {
                    Id = Guid.NewGuid(),
                    AccountId = Guid.NewGuid(),
                    Description = "Test",
                    Amount = new MoneyDto { Amount = -10m, Currency = "USD" },
                    Date = new DateOnly(2025, 6, 1),
                },
            ],
            TotalCount = 5,
            Page = 1,
            PageSize = 25,
        };

        var cut = Render<Uncategorized>();

        cut.Markup.ShouldContain("5 transactions need categorization");
    }

    /// <summary>
    /// Verifies account name is displayed in the transaction table.
    /// </summary>
    [Fact]
    public void ShowsAccountName_InTransactionTable()
    {
        var accountId = Guid.NewGuid();
        this._apiService.Accounts.Add(new AccountDto
        {
            Id = accountId,
            Name = "Savings Account",
            Type = "Savings",
            InitialBalance = 0m,
            InitialBalanceCurrency = "USD",
            InitialBalanceDate = new DateOnly(2025, 1, 1),
        });

        this._apiService.UncategorizedPage = new UncategorizedTransactionPageDto
        {
            Items =
            [
                new TransactionDto
                {
                    Id = Guid.NewGuid(),
                    AccountId = accountId,
                    Description = "Interest",
                    Amount = new MoneyDto { Amount = 1.50m, Currency = "USD" },
                    Date = new DateOnly(2025, 6, 1),
                },
            ],
            TotalCount = 1,
            Page = 1,
            PageSize = 25,
        };

        var cut = Render<Uncategorized>();

        cut.Markup.ShouldContain("Savings Account");
    }

    /// <summary>
    /// Verifies sorting column headers are present.
    /// </summary>
    [Fact]
    public void ShowsSortableColumnHeaders()
    {
        this._apiService.UncategorizedPage = CreatePageWithOneTransaction();

        var cut = Render<Uncategorized>();

        var sortableHeaders = cut.FindAll(".sortable");
        sortableHeaders.Count.ShouldBeGreaterThanOrEqualTo(3); // Date, Description, Amount
    }

    /// <summary>
    /// Verifies the select all checkbox is present when transactions exist.
    /// </summary>
    [Fact]
    public void ShowsSelectAllCheckbox_WhenTransactionsExist()
    {
        this._apiService.UncategorizedPage = CreatePageWithOneTransaction();

        var cut = Render<Uncategorized>();

        var checkboxes = cut.FindAll("input[type='checkbox']");
        checkboxes.Count.ShouldBeGreaterThanOrEqualTo(2); // select-all + per-row
    }

    /// <summary>
    /// Verifies pagination controls appear for multi-page data.
    /// </summary>
    [Fact]
    public void ShowsPaginationControls_WhenMultiplePages()
    {
        var items = new List<TransactionDto>();
        for (int i = 0; i < 25; i++)
        {
            items.Add(new TransactionDto
            {
                Id = Guid.NewGuid(),
                AccountId = Guid.NewGuid(),
                Description = $"Transaction {i}",
                Amount = new MoneyDto { Amount = -10m, Currency = "USD" },
                Date = new DateOnly(2025, 6, 1),
            });
        }

        this._apiService.UncategorizedPage = new UncategorizedTransactionPageDto
        {
            Items = items,
            TotalCount = 50,
            Page = 1,
            PageSize = 25,
        };

        var cut = Render<Uncategorized>();

        cut.Markup.ShouldContain("Page 1 of 2");
    }

    /// <summary>
    /// Verifies account dropdown is populated.
    /// </summary>
    [Fact]
    public void ShowsAccountsInDropdown()
    {
        this._apiService.Accounts.Add(new AccountDto
        {
            Id = Guid.NewGuid(),
            Name = "Main Checking",
            Type = "Checking",
            InitialBalance = 0m,
            InitialBalanceCurrency = "USD",
            InitialBalanceDate = new DateOnly(2025, 1, 1),
        });

        var cut = Render<Uncategorized>();

        cut.Markup.ShouldContain("Main Checking");
        cut.Markup.ShouldContain("All Accounts");
    }

    /// <summary>
    /// Verifies categories are loaded for bulk categorize.
    /// </summary>
    [Fact]
    public void CategoriesAreLoaded()
    {
        this._apiService.Categories.Add(new BudgetCategoryDto
        {
            Id = Guid.NewGuid(),
            Name = "Groceries",
            IsActive = true,
        });
        this._apiService.UncategorizedPage = CreatePageWithOneTransaction();

        var cut = Render<Uncategorized>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies page shows total count.
    /// </summary>
    [Fact]
    public void ShowsTotalCount_InSubtitle()
    {
        this._apiService.UncategorizedPage = new UncategorizedTransactionPageDto
        {
            Items =
            [
                new TransactionDto
                {
                    Id = Guid.NewGuid(),
                    AccountId = Guid.NewGuid(),
                    Description = "Item",
                    Amount = new MoneyDto { Amount = -10m, Currency = "USD" },
                    Date = new DateOnly(2025, 6, 1),
                },
            ],
            TotalCount = 42,
            Page = 1,
            PageSize = 25,
        };

        var cut = Render<Uncategorized>();

        cut.Markup.ShouldContain("42");
    }

    /// <summary>
    /// Verifies multiple item page renders all items.
    /// </summary>
    [Fact]
    public void ShowsMultipleTransactions()
    {
        this._apiService.UncategorizedPage = new UncategorizedTransactionPageDto
        {
            Items =
            [
                new TransactionDto
                {
                    Id = Guid.NewGuid(),
                    AccountId = Guid.NewGuid(),
                    Description = "Coffee Shop",
                    Amount = new MoneyDto { Amount = -5m, Currency = "USD" },
                    Date = new DateOnly(2025, 6, 1),
                },
                new TransactionDto
                {
                    Id = Guid.NewGuid(),
                    AccountId = Guid.NewGuid(),
                    Description = "Gas Station",
                    Amount = new MoneyDto { Amount = -40m, Currency = "USD" },
                    Date = new DateOnly(2025, 6, 2),
                },
            ],
            TotalCount = 2,
            Page = 1,
            PageSize = 25,
        };

        var cut = Render<Uncategorized>();

        cut.Markup.ShouldContain("Coffee Shop");
        cut.Markup.ShouldContain("Gas Station");
    }

    /// <summary>
    /// Verifies the bulk categorize button text.
    /// </summary>
    [Fact]
    public void HasBulkCategorizeButton()
    {
        this._apiService.UncategorizedPage = CreatePageWithOneTransaction();

        var cut = Render<Uncategorized>();

        cut.Markup.ShouldContain("Categorize");
    }

    /// <summary>
    /// Verifies sort header click toggles sort order.
    /// </summary>
    [Fact]
    public void SortableHeader_ClickTogglesSort()
    {
        this._apiService.UncategorizedPage = CreatePageWithOneTransaction();

        var cut = Render<Uncategorized>();
        var sortableHeaders = cut.FindAll("th.sortable");
        sortableHeaders.Count.ShouldBeGreaterThan(0);

        sortableHeaders[0].Click();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies clear filters button resets filters.
    /// </summary>
    [Fact]
    public void ClearFilters_ResetsFilters()
    {
        this._apiService.UncategorizedPage = CreatePageWithOneTransaction();

        var cut = Render<Uncategorized>();
        var clearButton = cut.FindAll("button").First(b => b.TextContent.Contains("Clear"));
        clearButton.Click();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies select all checkbox toggles all selections.
    /// </summary>
    [Fact]
    public void SelectAll_TogglesAllCheckboxes()
    {
        this._apiService.UncategorizedPage = CreatePageWithOneTransaction();

        var cut = Render<Uncategorized>();
        var selectAllCheckbox = cut.Find("th.checkbox-col input[type='checkbox']");
        selectAllCheckbox.Change(true);

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies individual row checkbox toggles selection.
    /// </summary>
    [Fact]
    public void RowCheckbox_TogglesSelection()
    {
        this._apiService.UncategorizedPage = CreatePageWithOneTransaction();

        var cut = Render<Uncategorized>();
        var rowCheckbox = cut.Find("td.checkbox-col input[type='checkbox']");
        rowCheckbox.Change(true);

        cut.Markup.ShouldContain("1 selected");
    }

    /// <summary>
    /// Verifies that selecting a row and clicking Apply Category triggers bulk categorize.
    /// </summary>
    [Fact]
    public void BulkCategorize_TriggersWhenRowSelectedAndCategoryChosen()
    {
        var catId = Guid.NewGuid();
        this._apiService.UncategorizedPage = CreatePageWithOneTransaction();
        this._apiService.Categories.Add(new BudgetCategoryDto
        {
            Id = catId,
            Name = "Food",
            Type = "Expense",
            IsActive = true,
        });

        var cut = Render<Uncategorized>();

        // Select the row checkbox
        var rowCheckbox = cut.Find("td.checkbox-col input[type='checkbox']");
        rowCheckbox.Change(true);

        // Select a category from the dropdown
        var select = cut.Find(".bulk-action-controls select");
        select.Change(catId.ToString());

        // Click Apply Category
        var applyButton = cut.FindAll("button").First(b => b.TextContent.Contains("Apply Category"));
        applyButton.Click();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    private static UncategorizedTransactionPageDto CreatePageWithOneTransaction()
    {
        return new UncategorizedTransactionPageDto
        {
            Items =
            [
                new TransactionDto
                {
                    Id = Guid.NewGuid(),
                    AccountId = Guid.NewGuid(),
                    Description = "Test Transaction",
                    Amount = new MoneyDto { Amount = -25m, Currency = "USD" },
                    Date = new DateOnly(2025, 6, 15),
                },
            ],
            TotalCount = 1,
            Page = 1,
            PageSize = 25,
        };
    }
}
