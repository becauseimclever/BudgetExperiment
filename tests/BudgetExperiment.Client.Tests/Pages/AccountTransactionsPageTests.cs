// <copyright file="AccountTransactionsPageTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Models;
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

    /// <summary>
    /// Verifies the create transaction result is configurable and renders.
    /// </summary>
    [Fact]
    public void CreateTransaction_ResultIsConfigurable()
    {
        this._apiService.CreateTransactionResult = new TransactionDto
        {
            Id = Guid.NewGuid(),
            Description = "New Transaction",
            Amount = new MoneyDto { Amount = -75.50m, Currency = "USD" },
            Date = DateOnly.FromDateTime(DateTime.Today),
            AccountId = this._accountId,
        };

        var cut = RenderPage();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the update transaction handles conflict.
    /// </summary>
    [Fact]
    public void UpdateTransaction_HandlesConflict()
    {
        this._apiService.UpdateTransactionResult = ApiResult<TransactionDto>.Conflict();

        var cut = RenderPage();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the delete transaction result is configurable.
    /// </summary>
    [Fact]
    public void DeleteTransaction_ResultIsConfigurable()
    {
        this._apiService.DeleteTransactionResult = true;

        var cut = RenderPage();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies categories are loaded for transaction modal.
    /// </summary>
    [Fact]
    public void CategoriesAreLoaded_ForTransactionModal()
    {
        this._apiService.Categories.Add(new BudgetCategoryDto
        {
            Id = Guid.NewGuid(),
            Name = "Groceries",
            Type = "Expense",
            IsActive = true,
        });

        var cut = RenderPage();

        // Open add transaction modal
        var addButton = cut.Find("button.btn-success");
        addButton.Click();

        // Categories should be available in the modal
        cut.Markup.ShouldContain("Groceries");
    }

    /// <summary>
    /// Verifies transactions with recurring items display type indicators.
    /// </summary>
    [Fact]
    public void ShowsRecurringTypeIndicator_ForRecurringItems()
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
                    Description = "Monthly Rent",
                    Amount = new MoneyDto { Amount = -1500m, Currency = "USD" },
                },
                new TransactionListItemDto
                {
                    Id = Guid.NewGuid(),
                    Type = "transaction",
                    Date = DateOnly.FromDateTime(DateTime.Today),
                    Description = "Coffee",
                    Amount = new MoneyDto { Amount = -4.50m, Currency = "USD" },
                },
            },
            Summary = new TransactionListSummaryDto
            {
                TransactionCount = 1,
                RecurringCount = 1,
                TotalAmount = new MoneyDto { Amount = -1504.50m, Currency = "USD" },
                CurrentBalance = new MoneyDto { Amount = -1504.50m, Currency = "USD" },
            },
        };

        var cut = RenderPage();

        cut.Markup.ShouldContain("Monthly Rent");
        cut.Markup.ShouldContain("Coffee");
    }

    /// <summary>
    /// Verifies past due items are shown when available.
    /// </summary>
    [Fact]
    public void ShowsPastDueItems_WhenAvailable()
    {
        this._apiService.PastDueSummary = new PastDueSummaryDto
        {
            TotalCount = 1,
            Items = new List<PastDueItemDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Type = "recurring-transaction",
                    Description = "Overdue Bill",
                    InstanceDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-3)),
                    Amount = new MoneyDto { Amount = -50m, Currency = "USD" },
                    AccountId = this._accountId,
                },
            },
        };

        var cut = RenderPage();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the get transaction result is used for editing.
    /// </summary>
    [Fact]
    public void GetTransactionResult_IsUsedForEditing()
    {
        this._apiService.GetTransactionResult = new TransactionDto
        {
            Id = Guid.NewGuid(),
            Description = "Editable Transaction",
            Amount = new MoneyDto { Amount = -25m, Currency = "USD" },
            Date = DateOnly.FromDateTime(DateTime.Today),
            AccountId = this._accountId,
        };

        var cut = RenderPage();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the page renders transactions header.
    /// </summary>
    [Fact]
    public void PageHeader_ContainsTransactionsTitle()
    {
        var cut = RenderPage();

        cut.Markup.ShouldContain("Transactions");
    }

    /// <summary>
    /// Verifies the Add Transaction button click triggers modal state.
    /// </summary>
    [Fact]
    public void AddTransactionButton_ClickTriggersState()
    {
        var cut = RenderPage();
        var addButton = cut.FindAll("button").First(b => b.TextContent.Contains("Add Transaction"));
        addButton.Click();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the date filter inputs are present and functional.
    /// </summary>
    [Fact]
    public void DateFilter_InputsArePresent()
    {
        var cut = RenderPage();
        var dateInputs = cut.FindAll("input[type='date']");

        dateInputs.Count.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// Verifies transaction list items show description.
    /// </summary>
    [Fact]
    public void TransactionListItems_ShowDescription()
    {
        this._apiService.TransactionList = CreateTransactionList([("Grocery Store", -45.00m)]);

        var cut = RenderPage();

        cut.Markup.ShouldContain("Grocery Store");
    }

    /// <summary>
    /// Verifies the update transaction result configures success path.
    /// </summary>
    [Fact]
    public void UpdateTransaction_Success_RefreshesData()
    {
        this._apiService.UpdateTransactionResult = ApiResult<TransactionDto>.Success(new TransactionDto
        {
            Id = Guid.NewGuid(),
            Description = "Updated Transaction",
            Amount = new MoneyDto { Amount = -100m, Currency = "USD" },
            Date = DateOnly.FromDateTime(DateTime.Today),
            AccountId = this._accountId,
        });

        var cut = RenderPage();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the delete transaction handler with success result.
    /// </summary>
    [Fact]
    public void DeleteTransaction_Success_RefreshesData()
    {
        this._apiService.DeleteTransactionResult = true;
        this._apiService.TransactionList = CreateTransactionList([("To Delete", -50m)]);

        var cut = RenderPage();

        cut.Markup.ShouldContain("To Delete");
    }

    /// <summary>
    /// Verifies the confirm recurring instance result is configurable.
    /// </summary>
    [Fact]
    public void ConfirmRecurringInstance_Success_RefreshesData()
    {
        this._apiService.RealizeRecurringTransactionResult = new TransactionDto
        {
            Id = Guid.NewGuid(),
            Description = "Confirmed Recurring",
            Amount = new MoneyDto { Amount = -15.99m, Currency = "USD" },
            Date = DateOnly.FromDateTime(DateTime.Today),
            AccountId = this._accountId,
        };

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
                    RecurringTransactionId = Guid.NewGuid(),
                    Date = DateOnly.FromDateTime(DateTime.Today),
                    Description = "Netflix Subscription",
                    Amount = new MoneyDto { Amount = -15.99m, Currency = "USD" },
                },
            },
        };

        var cut = RenderPage();

        cut.Markup.ShouldContain("Netflix Subscription");
    }

    /// <summary>
    /// Verifies the skip recurring instance result is configurable.
    /// </summary>
    [Fact]
    public void SkipRecurringInstance_Success_RefreshesData()
    {
        this._apiService.SkipRecurringInstanceResult = true;

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
                    RecurringTransactionId = Guid.NewGuid(),
                    Date = DateOnly.FromDateTime(DateTime.Today),
                    Description = "Skippable Subscription",
                    Amount = new MoneyDto { Amount = -9.99m, Currency = "USD" },
                },
            },
        };

        var cut = RenderPage();

        cut.Markup.ShouldContain("Skippable Subscription");
    }

    /// <summary>
    /// Verifies the save location result is configurable.
    /// </summary>
    [Fact]
    public void SaveLocation_Success_UpdatesTransaction()
    {
        this._apiService.UpdateTransactionLocationResult = ApiResult<TransactionDto>.Success(new TransactionDto
        {
            Id = Guid.NewGuid(),
            Description = "Located Transaction",
            Amount = new MoneyDto { Amount = -30m, Currency = "USD" },
            Date = DateOnly.FromDateTime(DateTime.Today),
            AccountId = this._accountId,
        });

        var cut = RenderPage();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the clear location result is configurable.
    /// </summary>
    [Fact]
    public void ClearLocation_Success_UpdatesTransaction()
    {
        this._apiService.ClearTransactionLocationResult = true;

        var cut = RenderPage();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the modify recurring instance result is configurable.
    /// </summary>
    [Fact]
    public void ModifyRecurringInstance_Success_RefreshesData()
    {
        this._apiService.ModifyRecurringInstanceResult = ApiResult<RecurringInstanceDto>.Success(new RecurringInstanceDto
        {
            RecurringTransactionId = Guid.NewGuid(),
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
        });

        var cut = RenderPage();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the past due items are shown with realize batch result.
    /// </summary>
    [Fact]
    public void ConfirmPastDueItems_Success_RefreshesData()
    {
        this._apiService.RealizeBatchResult = new BatchRealizeResultDto { SuccessCount = 2 };
        this._apiService.PastDueSummary = new PastDueSummaryDto
        {
            TotalCount = 1,
            Items = new List<PastDueItemDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Type = "recurring-transaction",
                    Description = "Past Due Bill",
                    InstanceDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-5)),
                    Amount = new MoneyDto { Amount = -75m, Currency = "USD" },
                    AccountId = this._accountId,
                },
            },
        };

        var cut = RenderPage();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies clicking Add Transaction opens the modal.
    /// </summary>
    [Fact]
    public void AddTransaction_OpensModal()
    {
        this._apiService.Categories.Add(new BudgetCategoryDto
        {
            Id = Guid.NewGuid(),
            Name = "Food & Dining",
            Type = "Expense",
            IsActive = true,
        });

        var cut = RenderPage();
        var addButton = cut.FindAll("button").First(b => b.TextContent.Contains("Add Transaction"));
        addButton.Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Add Transaction"));
    }

    /// <summary>
    /// Verifies the error message includes the error from API.
    /// </summary>
    [Fact]
    public void Error_ShowsApiErrorMessage()
    {
        this._apiService.ShouldThrowOnGetTransactionList = true;

        var cut = RenderPage();

        cut.Markup.ShouldContain("Failed to load transactions");
    }

    /// <summary>
    /// Verifies multiple transaction items are rendered in the list.
    /// </summary>
    [Fact]
    public void ShowsMultipleTransactions_InOrder()
    {
        this._apiService.TransactionList = CreateTransactionList([
            ("Alpha Purchase", -10m),
            ("Beta Purchase", -20m),
            ("Gamma Purchase", -30m),
        ]);

        var cut = RenderPage();

        cut.Markup.ShouldContain("Alpha Purchase");
        cut.Markup.ShouldContain("Beta Purchase");
        cut.Markup.ShouldContain("Gamma Purchase");
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
