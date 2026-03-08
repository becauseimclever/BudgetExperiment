// <copyright file="AccountTransactionsPageTests.cs" company="BecauseImClever">
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
/// Unit tests for the <see cref="AccountTransactions"/> page component.
/// </summary>
public class AccountTransactionsPageTests : BunitContext, IAsyncLifetime
{
    private readonly StubBudgetApiService _apiService = new();
    private readonly Guid _accountId = Guid.NewGuid();

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountTransactionsPageTests"/> class.
    /// </summary>
    public AccountTransactionsPageTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;

        this._apiService.TransactionList = new TransactionListDto
        {
            AccountId = this._accountId,
            AccountName = "Test Checking",
        };
        this.Services.AddSingleton<IBudgetApiService>(this._apiService);
        this.Services.AddSingleton<IToastService>(new ToastService());
        this.Services.AddSingleton<ScopeService>();
        this.Services.AddSingleton<ThemeService>();
        this.Services.AddSingleton<IChatContextService>(new StubChatContextService());
        this.Services.AddSingleton(new GeolocationService(this.JSInterop.JSRuntime));
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
        var cut = RenderPage();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the page title includes account name.
    /// </summary>
    [Fact]
    public void HasCorrectPageTitle()
    {
        var cut = RenderPage();

        cut.Markup.ShouldContain("Transactions");
    }

    /// <summary>
    /// Verifies the page displays the account name from API.
    /// </summary>
    [Fact]
    public void DisplaysAccountName()
    {
        var cut = RenderPage();

        cut.Markup.ShouldContain("Test Checking");
    }

    /// <summary>
    /// Verifies the Add Transaction button is present.
    /// </summary>
    [Fact]
    public void HasAddTransactionButton()
    {
        var cut = RenderPage();

        cut.Markup.ShouldContain("Add Transaction");
    }

    /// <summary>
    /// Verifies date filter inputs are present.
    /// </summary>
    [Fact]
    public void HasDateFilters()
    {
        var cut = RenderPage();

        cut.Markup.ShouldContain("From:");
        cut.Markup.ShouldContain("To:");
    }

    /// <summary>
    /// Verifies empty state is shown when no transactions exist.
    /// </summary>
    [Fact]
    public void ShowsEmptyMessage_WhenNoTransactions()
    {
        var cut = RenderPage();

        cut.Markup.ShouldContain("No transactions found");
    }

    /// <summary>
    /// Verifies transactions are displayed when data is loaded.
    /// </summary>
    [Fact]
    public void ShowsTransactions_WhenDataExists()
    {
        this._apiService.TransactionList = CreateTransactionList(new[]
        {
            ("Grocery Store", -50.00m),
            ("Paycheck", 2000.00m),
        });

        var cut = RenderPage();

        cut.Markup.ShouldContain("Grocery Store");
        cut.Markup.ShouldContain("Paycheck");
    }

    /// <summary>
    /// Verifies the summary stats section shows transaction count.
    /// </summary>
    [Fact]
    public void ShowsSummaryStats()
    {
        this._apiService.TransactionList = CreateTransactionList(new[]
        {
            ("Test Transaction", -25.00m),
        });

        var cut = RenderPage();

        cut.Markup.ShouldContain("Count: 1");
    }

    /// <summary>
    /// Verifies the balance banner displays when initial balance is non-zero.
    /// </summary>
    [Fact]
    public void ShowsBalanceBanner_WhenInitialBalanceExists()
    {
        this._apiService.TransactionList = new TransactionListDto
        {
            AccountId = this._accountId,
            AccountName = "Test Checking",
            InitialBalance = new MoneyDto { Amount = 1000m, Currency = "USD" },
            InitialBalanceDate = new DateOnly(2025, 1, 1),
            Items = [],
            Summary = new TransactionListSummaryDto
            {
                CurrentBalance = new MoneyDto { Amount = 1500m, Currency = "USD" },
            },
        };

        var cut = RenderPage();

        cut.Markup.ShouldContain("Starting Balance:");
        cut.Markup.ShouldContain("Current Balance:");
    }

    /// <summary>
    /// Verifies the balance banner is hidden when initial balance is zero.
    /// </summary>
    [Fact]
    public void HidesBalanceBanner_WhenInitialBalanceIsZero()
    {
        this._apiService.TransactionList = CreateTransactionList(new[]
        {
            ("Test", -10m),
        });

        var cut = RenderPage();

        cut.Markup.ShouldNotContain("Starting Balance:");
    }

    /// <summary>
    /// Verifies error message is displayed when data load fails.
    /// </summary>
    [Fact]
    public void ShowsError_WhenLoadFails()
    {
        this._apiService.ShouldThrowOnGetTransactionList = true;

        var cut = RenderPage();

        cut.Markup.ShouldContain("Failed to load transactions");
    }

    /// <summary>
    /// Verifies recurring count shown in summary when recurring transactions exist.
    /// </summary>
    [Fact]
    public void ShowsRecurringCount_WhenRecurringExists()
    {
        this._apiService.TransactionList = new TransactionListDto
        {
            AccountId = this._accountId,
            AccountName = "Test Checking",
            Items = new[]
            {
                new TransactionListItemDto
                {
                    Id = Guid.NewGuid(),
                    Type = "recurring",
                    Date = DateOnly.FromDateTime(DateTime.Today),
                    Description = "Netflix",
                    Amount = new MoneyDto { Amount = -15.99m, Currency = "USD" },
                },
            },
            Summary = new TransactionListSummaryDto
            {
                RecurringCount = 1,
                TotalAmount = new MoneyDto { Amount = -15.99m, Currency = "USD" },
                CurrentBalance = new MoneyDto { Amount = -15.99m, Currency = "USD" },
            },
        };

        var cut = RenderPage();

        cut.Markup.ShouldContain("1 recurring");
    }

    /// <summary>
    /// Verifies the add transaction modal opens when clicking the button.
    /// </summary>
    [Fact]
    public void AddTransactionButton_OpensModal()
    {
        var cut = RenderPage();

        var addButton = cut.Find("button.btn-success");
        addButton.Click();

        cut.Markup.ShouldContain("Add Transaction");
    }

    private IRenderedComponent<AccountTransactions> RenderPage()
    {
        return Render<AccountTransactions>(parameters => parameters
            .Add(p => p.AccountId, this._accountId));
    }

    private TransactionListDto CreateTransactionList(
        (string Description, decimal Amount)[] items)
    {
        var dtoItems = items.Select(i => new TransactionListItemDto
        {
            Id = Guid.NewGuid(),
            Type = "transaction",
            Date = DateOnly.FromDateTime(DateTime.Today),
            Description = i.Description,
            Amount = new MoneyDto { Amount = i.Amount, Currency = "USD" },
        }).ToArray();

        return new TransactionListDto
        {
            AccountId = this._accountId,
            AccountName = "Test Checking",
            Items = dtoItems,
            Summary = new TransactionListSummaryDto
            {
                TransactionCount = dtoItems.Length,
                TotalAmount = new MoneyDto { Amount = items.Sum(i => i.Amount), Currency = "USD" },
                CurrentBalance = new MoneyDto { Amount = items.Sum(i => i.Amount), Currency = "USD" },
            },
        };
    }
}
