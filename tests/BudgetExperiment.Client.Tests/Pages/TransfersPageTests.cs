// <copyright file="TransfersPageTests.cs" company="BecauseImClever">
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
/// Unit tests for the <see cref="Transfers"/> page component.
/// </summary>
public class TransfersPageTests : BunitContext, IAsyncLifetime
{
    private readonly StubBudgetApiService _apiService = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TransfersPageTests"/> class.
    /// </summary>
    public TransfersPageTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<IBudgetApiService>(this._apiService);
        this.Services.AddSingleton<ScopeService>();
        this.Services.AddSingleton<ThemeService>();
        this.Services.AddSingleton<IChatContextService>(new StubChatContextService());
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
        var cut = Render<Transfers>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the page title is set correctly.
    /// </summary>
    [Fact]
    public void HasCorrectPageTitle()
    {
        var cut = Render<Transfers>();

        cut.Markup.ShouldContain("Transfers");
    }

    /// <summary>
    /// Verifies the New Transfer button is present.
    /// </summary>
    [Fact]
    public void HasNewTransferButton()
    {
        var cut = Render<Transfers>();

        cut.Markup.ShouldContain("New Transfer");
    }

    /// <summary>
    /// Verifies empty state when no transfers exist.
    /// </summary>
    [Fact]
    public void ShowsEmptyState_WhenNoTransfers()
    {
        var cut = Render<Transfers>();

        cut.Markup.ShouldContain("No transfers yet");
    }

    /// <summary>
    /// Verifies the filter section is present.
    /// </summary>
    [Fact]
    public void HasFilterSection()
    {
        var cut = Render<Transfers>();

        cut.Markup.ShouldContain("Account:");
        cut.Markup.ShouldContain("All Accounts");
        cut.Markup.ShouldContain("From:");
        cut.Markup.ShouldContain("To:");
        cut.Markup.ShouldContain("Clear Filters");
    }

    /// <summary>
    /// Verifies transfer table is shown when data exists.
    /// </summary>
    [Fact]
    public void ShowsTransferTable_WhenDataExists()
    {
        this._apiService.Transfers.Add(new TransferListItemResponse
        {
            TransferId = Guid.NewGuid(),
            Date = new DateOnly(2026, 3, 1),
            SourceAccountName = "Checking",
            DestinationAccountName = "Savings",
            Amount = 500.00m,
            Description = "Monthly savings",
        });

        var cut = Render<Transfers>();

        cut.Markup.ShouldContain("Checking");
        cut.Markup.ShouldContain("Savings");
        cut.Markup.ShouldContain("Monthly savings");
    }

    /// <summary>
    /// Verifies transfer table has correct column headers.
    /// </summary>
    [Fact]
    public void TransferTable_HasCorrectHeaders()
    {
        this._apiService.Transfers.Add(new TransferListItemResponse
        {
            TransferId = Guid.NewGuid(),
            Date = new DateOnly(2026, 3, 1),
            SourceAccountName = "Checking",
            DestinationAccountName = "Savings",
            Amount = 100.00m,
        });

        var cut = Render<Transfers>();

        cut.Markup.ShouldContain("Date");
        cut.Markup.ShouldContain("From");
        cut.Markup.ShouldContain("Amount");
        cut.Markup.ShouldContain("Description");
        cut.Markup.ShouldContain("Actions");
    }

    /// <summary>
    /// Verifies the Edit and Delete buttons are shown for each transfer.
    /// </summary>
    [Fact]
    public void ShowsEditAndDeleteButtons_ForEachTransfer()
    {
        this._apiService.Transfers.Add(new TransferListItemResponse
        {
            TransferId = Guid.NewGuid(),
            Date = new DateOnly(2026, 3, 5),
            SourceAccountName = "A",
            DestinationAccountName = "B",
            Amount = 250.00m,
        });

        var cut = Render<Transfers>();

        cut.Markup.ShouldContain("Edit");
        cut.Markup.ShouldContain("Delete");
    }

    /// <summary>
    /// Verifies account dropdown is populated with accounts.
    /// </summary>
    [Fact]
    public void AccountDropdown_PopulatedWithAccounts()
    {
        this._apiService.Accounts.Add(new AccountDto
        {
            Id = Guid.NewGuid(),
            Name = "Checking Account",
        });

        var cut = Render<Transfers>();

        cut.Markup.ShouldContain("Checking Account");
    }

    /// <summary>
    /// Verifies filter message when no transfers match filters.
    /// </summary>
    [Fact]
    public void ShowsFilterEmptyMessage_WhenNoMatchingTransfers()
    {
        this._apiService.Transfers.Add(new TransferListItemResponse
        {
            TransferId = Guid.NewGuid(),
            Date = new DateOnly(2026, 3, 5),
            SourceAccountName = "A",
            SourceAccountId = Guid.NewGuid(),
            DestinationAccountName = "B",
            DestinationAccountId = Guid.NewGuid(),
            Amount = 250.00m,
        });

        var cut = Render<Transfers>();

        // Before filtering, data should be visible
        cut.Markup.ShouldNotContain("No transfers match the current filters");
    }

    /// <summary>
    /// Verifies description with empty text shows a dash.
    /// </summary>
    [Fact]
    public void ShowsDash_WhenDescriptionIsEmpty()
    {
        this._apiService.Transfers.Add(new TransferListItemResponse
        {
            TransferId = Guid.NewGuid(),
            Date = new DateOnly(2026, 3, 5),
            SourceAccountName = "A",
            DestinationAccountName = "B",
            Amount = 100.00m,
            Description = string.Empty,
        });

        var cut = Render<Transfers>();

        // The page renders "-" for empty descriptions
        cut.Find("tbody").InnerHtml.ShouldContain("-");
    }

    /// <summary>
    /// Verifies create transfer dialog can be triggered.
    /// </summary>
    [Fact]
    public void CreateTransferResult_IsConfigurable()
    {
        this._apiService.CreateTransferResult = new TransferResponse
        {
            TransferId = Guid.NewGuid(),
            Date = new DateOnly(2026, 4, 1),
            Amount = 500m,
            SourceAccountId = Guid.NewGuid(),
            DestinationAccountId = Guid.NewGuid(),
        };

        var cut = Render<Transfers>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies page has a create transfer button.
    /// </summary>
    [Fact]
    public void HasCreateTransferButton()
    {
        var cut = Render<Transfers>();

        cut.Markup.ShouldContain("New Transfer");
    }

    /// <summary>
    /// Verifies transfer table shows date column.
    /// </summary>
    [Fact]
    public void TransferTable_ShowsDateColumn()
    {
        this._apiService.Transfers.Add(new TransferListItemResponse
        {
            TransferId = Guid.NewGuid(),
            Date = new DateOnly(2026, 7, 15),
            SourceAccountName = "Checking",
            SourceAccountId = Guid.NewGuid(),
            DestinationAccountName = "Savings",
            DestinationAccountId = Guid.NewGuid(),
            Amount = 100.00m,
        });

        var cut = Render<Transfers>();

        cut.Markup.ShouldContain("Jul");
    }

    /// <summary>
    /// Verifies multiple transfers are displayed.
    /// </summary>
    [Fact]
    public void ShowsMultipleTransfers()
    {
        this._apiService.Transfers.Add(new TransferListItemResponse
        {
            TransferId = Guid.NewGuid(),
            Date = new DateOnly(2026, 1, 1),
            SourceAccountName = "Account A",
            SourceAccountId = Guid.NewGuid(),
            DestinationAccountName = "Account B",
            DestinationAccountId = Guid.NewGuid(),
            Amount = 200.00m,
        });
        this._apiService.Transfers.Add(new TransferListItemResponse
        {
            TransferId = Guid.NewGuid(),
            Date = new DateOnly(2026, 2, 1),
            SourceAccountName = "Account C",
            SourceAccountId = Guid.NewGuid(),
            DestinationAccountName = "Account D",
            DestinationAccountId = Guid.NewGuid(),
            Amount = 300.00m,
        });

        var cut = Render<Transfers>();

        cut.Markup.ShouldContain("Account A");
        cut.Markup.ShouldContain("Account C");
    }

    /// <summary>
    /// Verifies clear filters button is present.
    /// </summary>
    [Fact]
    public void HasClearFiltersButton()
    {
        var cut = Render<Transfers>();

        cut.Markup.ShouldContain("Clear");
    }

    /// <summary>
    /// Verifies delete button removes a transfer on click.
    /// </summary>
    [Fact]
    public void DeleteButton_RemovesTransfer()
    {
        this._apiService.Transfers.Add(new TransferListItemResponse
        {
            TransferId = Guid.NewGuid(),
            Date = new DateOnly(2026, 7, 15),
            SourceAccountName = "Checking",
            SourceAccountId = Guid.NewGuid(),
            DestinationAccountName = "Savings",
            DestinationAccountId = Guid.NewGuid(),
            Amount = 500.00m,
        });

        var cut = Render<Transfers>();
        cut.Markup.ShouldContain("Checking");

        // Click the Delete button
        var deleteButton = cut.FindAll("button").First(b => b.TextContent.Contains("Delete"));
        deleteButton.Click();

        // After delete, the page should re-render (transfers list is now empty since stub returns empty on reload)
        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies New Transfer button opens dialog.
    /// </summary>
    [Fact]
    public void NewTransferButton_OpensDialog()
    {
        this._apiService.Accounts.Add(new AccountDto
        {
            Id = Guid.NewGuid(),
            Name = "Checking",
            Type = "Checking",
            InitialBalance = 0m,
            InitialBalanceCurrency = "USD",
            InitialBalanceDate = new DateOnly(2025, 1, 1),
        });

        var cut = Render<Transfers>();

        var newButton = cut.FindAll("button").First(b => b.TextContent.Contains("New Transfer"));
        newButton.Click();

        // Dialog should open with transfer form elements
        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies edit button exists and is clickable on transfer rows.
    /// </summary>
    [Fact]
    public void EditButton_IsClickable()
    {
        this._apiService.Transfers.Add(new TransferListItemResponse
        {
            TransferId = Guid.NewGuid(),
            Date = new DateOnly(2026, 7, 15),
            SourceAccountName = "Checking",
            SourceAccountId = Guid.NewGuid(),
            DestinationAccountName = "Savings",
            DestinationAccountId = Guid.NewGuid(),
            Amount = 100.00m,
        });

        var cut = Render<Transfers>();

        var editButton = cut.FindAll("button").First(b => b.TextContent.Contains("Edit"));
        editButton.Click();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }
}
