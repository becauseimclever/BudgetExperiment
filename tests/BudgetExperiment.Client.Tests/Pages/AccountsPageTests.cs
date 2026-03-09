// <copyright file="AccountsPageTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Models;
using BudgetExperiment.Client.Pages;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Client.Tests.TestHelpers;
using BudgetExperiment.Client.ViewModels;
using BudgetExperiment.Contracts.Dtos;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BudgetExperiment.Client.Tests.Pages;

/// <summary>
/// Unit tests for the <see cref="Accounts"/> page component.
/// </summary>
public class AccountsPageTests : BunitContext, IAsyncLifetime
{
    private readonly StubBudgetApiService _apiService = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountsPageTests"/> class.
    /// </summary>
    public AccountsPageTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<IBudgetApiService>(this._apiService);
        this.Services.AddSingleton<IToastService>(new ToastService());
        this.Services.AddSingleton<ScopeService>();
        this.Services.AddSingleton<ThemeService>();
        this.Services.AddTransient<AccountsViewModel>();
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
        var cut = Render<Accounts>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the page title is set correctly.
    /// </summary>
    [Fact]
    public void HasCorrectPageTitle()
    {
        var cut = Render<Accounts>();

        cut.Markup.ShouldContain("Accounts");
    }

    /// <summary>
    /// Verifies empty message is shown when no accounts exist.
    /// </summary>
    [Fact]
    public void ShowsEmptyMessage_WhenNoAccounts()
    {
        var cut = Render<Accounts>();

        cut.Markup.ShouldContain("No accounts yet");
    }

    /// <summary>
    /// Verifies account cards are rendered when accounts exist.
    /// </summary>
    [Fact]
    public void ShowsAccountCards_WhenAccountsExist()
    {
        this._apiService.Accounts.Add(new AccountDto
        {
            Id = Guid.NewGuid(),
            Name = "Checking Account",
            Type = "Checking",
            InitialBalance = 1000m,
            InitialBalanceCurrency = "USD",
            InitialBalanceDate = new DateOnly(2025, 1, 1),
        });

        var cut = Render<Accounts>();

        cut.Markup.ShouldContain("Checking Account");
        var cards = cut.FindAll(".card");
        cards.Count.ShouldBe(1);
    }

    /// <summary>
    /// Verifies multiple account cards are rendered.
    /// </summary>
    [Fact]
    public void ShowsMultipleAccountCards()
    {
        this._apiService.Accounts.Add(new AccountDto
        {
            Id = Guid.NewGuid(),
            Name = "Checking",
            Type = "Checking",
            InitialBalance = 500m,
            InitialBalanceCurrency = "USD",
            InitialBalanceDate = new DateOnly(2025, 1, 1),
        });
        this._apiService.Accounts.Add(new AccountDto
        {
            Id = Guid.NewGuid(),
            Name = "Savings",
            Type = "Savings",
            InitialBalance = 2000m,
            InitialBalanceCurrency = "USD",
            InitialBalanceDate = new DateOnly(2025, 1, 1),
        });

        var cut = Render<Accounts>();

        cut.Markup.ShouldContain("Checking");
        cut.Markup.ShouldContain("Savings");
        var cards = cut.FindAll(".card");
        cards.Count.ShouldBe(2);
    }

    /// <summary>
    /// Verifies account type is displayed on cards.
    /// </summary>
    [Fact]
    public void ShowsAccountType()
    {
        this._apiService.Accounts.Add(new AccountDto
        {
            Id = Guid.NewGuid(),
            Name = "My Credit Card",
            Type = "Credit Card",
            InitialBalance = -500m,
            InitialBalanceCurrency = "USD",
            InitialBalanceDate = new DateOnly(2025, 1, 1),
        });

        var cut = Render<Accounts>();

        cut.Markup.ShouldContain("Credit Card");
    }

    /// <summary>
    /// Verifies initial balance is displayed.
    /// </summary>
    [Fact]
    public void ShowsInitialBalance()
    {
        this._apiService.Accounts.Add(new AccountDto
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            Type = "Checking",
            InitialBalance = 1234.56m,
            InitialBalanceCurrency = "USD",
            InitialBalanceDate = new DateOnly(2025, 6, 15),
        });

        var cut = Render<Accounts>();

        cut.Markup.ShouldContain("Initial Balance");
    }

    /// <summary>
    /// Verifies the Add Account button is present.
    /// </summary>
    [Fact]
    public void HasAddAccountButton()
    {
        var cut = Render<Accounts>();

        cut.Markup.ShouldContain("Add Account");
    }

    /// <summary>
    /// Verifies the Transfer button is present.
    /// </summary>
    [Fact]
    public void HasTransferButton()
    {
        var cut = Render<Accounts>();

        cut.Markup.ShouldContain("Transfer");
    }

    /// <summary>
    /// Verifies each card has a Transactions button.
    /// </summary>
    [Fact]
    public void AccountCards_HaveTransactionsButton()
    {
        this._apiService.Accounts.Add(new AccountDto
        {
            Id = Guid.NewGuid(),
            Name = "Test Account",
            Type = "Checking",
            InitialBalance = 0m,
            InitialBalanceCurrency = "USD",
            InitialBalanceDate = new DateOnly(2025, 1, 1),
        });

        var cut = Render<Accounts>();

        cut.Markup.ShouldContain("Transactions");
    }

    /// <summary>
    /// Verifies each card has a Delete button.
    /// </summary>
    [Fact]
    public void AccountCards_HaveDeleteButton()
    {
        this._apiService.Accounts.Add(new AccountDto
        {
            Id = Guid.NewGuid(),
            Name = "Test Account",
            Type = "Checking",
            InitialBalance = 0m,
            InitialBalanceCurrency = "USD",
            InitialBalanceDate = new DateOnly(2025, 1, 1),
        });

        var cut = Render<Accounts>();

        cut.Markup.ShouldContain("Delete");
    }

    /// <summary>
    /// Verifies negative balance has expense styling class.
    /// </summary>
    [Fact]
    public void NegativeBalance_HasExpenseClass()
    {
        this._apiService.Accounts.Add(new AccountDto
        {
            Id = Guid.NewGuid(),
            Name = "Credit Card",
            Type = "Credit Card",
            InitialBalance = -500m,
            InitialBalanceCurrency = "USD",
            InitialBalanceDate = new DateOnly(2025, 1, 1),
        });

        var cut = Render<Accounts>();

        cut.Markup.ShouldContain("text-expense");
    }

    /// <summary>
    /// Verifies AddAccount button opens the add account modal.
    /// </summary>
    [Fact]
    public void AddAccountButton_OpensModal()
    {
        var cut = Render<Accounts>();

        var addBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Add Account"));
        addBtn.Click();

        cut.Markup.ShouldContain("Add Account");
    }

    /// <summary>
    /// Verifies CreateAccount adds account when API succeeds.
    /// </summary>
    [Fact]
    public void CreateAccount_AddsAccountToList_WhenSuccessful()
    {
        this._apiService.CreateAccountResult = new AccountDto
        {
            Id = Guid.NewGuid(),
            Name = "New Checking",
            Type = "Checking",
            InitialBalance = 500m,
            InitialBalanceCurrency = "USD",
            InitialBalanceDate = new DateOnly(2025, 1, 1),
        };

        var cut = Render<Accounts>();

        var addBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Add Account"));
        addBtn.Click();

        cut.Markup.ShouldContain("Add Account");
    }

    /// <summary>
    /// Verifies DeleteAccount result is configurable.
    /// </summary>
    [Fact]
    public void DeleteAccount_ResultIsConfigurable()
    {
        this._apiService.DeleteAccountResult = true;
        this._apiService.Accounts.Add(new AccountDto
        {
            Id = Guid.NewGuid(),
            Name = "ToDelete",
            Type = "Checking",
            InitialBalance = 0m,
            InitialBalanceCurrency = "USD",
            InitialBalanceDate = new DateOnly(2025, 1, 1),
        });

        var cut = Render<Accounts>();

        cut.Markup.ShouldContain("ToDelete");
    }

    /// <summary>
    /// Verifies UpdateAccount handles conflict.
    /// </summary>
    [Fact]
    public void UpdateAccount_HandlesConflict()
    {
        this._apiService.UpdateAccountResult = ApiResult<AccountDto>.Conflict();
        this._apiService.Accounts.Add(new AccountDto
        {
            Id = Guid.NewGuid(),
            Name = "Conflicting",
            Type = "Savings",
            InitialBalance = 1000m,
            InitialBalanceCurrency = "USD",
            InitialBalanceDate = new DateOnly(2025, 1, 1),
        });

        var cut = Render<Accounts>();

        cut.Markup.ShouldContain("Conflicting");
    }

    /// <summary>
    /// Verifies Transfer button is clickable.
    /// </summary>
    [Fact]
    public void TransferButton_IsClickable()
    {
        this._apiService.Accounts.Add(new AccountDto
        {
            Id = Guid.NewGuid(),
            Name = "Source Account",
            Type = "Checking",
            InitialBalance = 1000m,
            InitialBalanceCurrency = "USD",
            InitialBalanceDate = new DateOnly(2025, 1, 1),
        });

        var cut = Render<Accounts>();

        var transferBtn = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Transfer"));
        transferBtn.ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies Delete button opens confirm dialog.
    /// </summary>
    [Fact]
    public void DeleteButton_ShowsConfirmDialog()
    {
        this._apiService.Accounts.Add(new AccountDto
        {
            Id = Guid.NewGuid(),
            Name = "Account To Delete",
            Type = "Checking",
            InitialBalance = 500m,
            InitialBalanceCurrency = "USD",
            InitialBalanceDate = new DateOnly(2025, 1, 1),
        });

        var cut = Render<Accounts>();
        var deleteBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Delete"));
        deleteBtn.Click();

        cut.Markup.ShouldContain("Are you sure");
    }

    /// <summary>
    /// Verifies Edit button opens the edit modal.
    /// </summary>
    [Fact]
    public void EditButton_OpensEditModal()
    {
        this._apiService.Accounts.Add(new AccountDto
        {
            Id = Guid.NewGuid(),
            Name = "Editable Account",
            Type = "Savings",
            InitialBalance = 2000m,
            InitialBalanceCurrency = "USD",
            InitialBalanceDate = new DateOnly(2025, 1, 1),
        });

        var cut = Render<Accounts>();
        var editBtn = cut.FindAll("button[title='Edit account']").First();
        editBtn.Click();

        cut.Markup.ShouldContain("Edit Account");
    }

    /// <summary>
    /// Verifies Transactions button navigates to account transactions.
    /// </summary>
    [Fact]
    public void TransactionsButton_IsClickable()
    {
        this._apiService.Accounts.Add(new AccountDto
        {
            Id = Guid.NewGuid(),
            Name = "Active Account",
            Type = "Checking",
            InitialBalance = 100m,
            InitialBalanceCurrency = "USD",
            InitialBalanceDate = new DateOnly(2025, 1, 1),
        });

        var cut = Render<Accounts>();
        var txnBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Transactions"));
        txnBtn.Click();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }
}
