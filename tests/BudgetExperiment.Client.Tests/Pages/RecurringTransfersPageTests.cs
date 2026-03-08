// <copyright file="RecurringTransfersPageTests.cs" company="BecauseImClever">
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
/// Unit tests for the <see cref="RecurringTransfers"/> page component.
/// </summary>
public class RecurringTransfersPageTests : BunitContext, IAsyncLifetime
{
    private readonly StubBudgetApiService _apiService = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringTransfersPageTests"/> class.
    /// </summary>
    public RecurringTransfersPageTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<IBudgetApiService>(this._apiService);
        this.Services.AddSingleton<IToastService>(new ToastService());
        this.Services.AddSingleton<ScopeService>();
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
        var cut = Render<RecurringTransfers>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the page title is set correctly.
    /// </summary>
    [Fact]
    public void HasCorrectPageTitle()
    {
        var cut = Render<RecurringTransfers>();

        cut.Markup.ShouldContain("Recurring Transfers");
    }

    /// <summary>
    /// Verifies the empty state when no recurring transfers exist.
    /// </summary>
    [Fact]
    public void ShowsEmptyState_WhenNoRecurringTransfers()
    {
        var cut = Render<RecurringTransfers>();

        cut.Markup.ShouldContain("No Recurring Transfers");
        cut.Markup.ShouldContain("Create Your First Recurring Transfer");
    }

    /// <summary>
    /// Verifies the Add Recurring Transfer button is present.
    /// </summary>
    [Fact]
    public void HasAddRecurringTransferButton()
    {
        var cut = Render<RecurringTransfers>();

        cut.Markup.ShouldContain("Add Recurring Transfer");
    }

    /// <summary>
    /// Verifies recurring transfer cards are shown when data exists.
    /// </summary>
    [Fact]
    public void ShowsTransferCards_WhenDataExists()
    {
        this._apiService.RecurringTransfers.Add(new RecurringTransferDto
        {
            Id = Guid.NewGuid(),
            Description = "Monthly Savings",
            Amount = new MoneyDto { Amount = 500.00m, Currency = "USD" },
            Frequency = "Monthly",
            NextOccurrence = new DateOnly(2026, 4, 1),
            StartDate = new DateOnly(2025, 1, 1),
            IsActive = true,
            SourceAccountId = Guid.NewGuid(),
            SourceAccountName = "Checking",
            DestinationAccountId = Guid.NewGuid(),
            DestinationAccountName = "Savings",
        });

        var cut = Render<RecurringTransfers>();

        cut.Markup.ShouldContain("Monthly Savings");
        cut.Markup.ShouldContain("Checking");
        cut.Markup.ShouldContain("Savings");
    }

    /// <summary>
    /// Verifies the transfer flow arrow is shown between account names.
    /// </summary>
    [Fact]
    public void ShowsTransferFlowArrow()
    {
        this._apiService.RecurringTransfers.Add(new RecurringTransferDto
        {
            Id = Guid.NewGuid(),
            Description = "Transfer",
            Amount = new MoneyDto { Amount = 100.00m, Currency = "USD" },
            Frequency = "Monthly",
            NextOccurrence = new DateOnly(2026, 4, 1),
            StartDate = new DateOnly(2025, 1, 1),
            IsActive = true,
            SourceAccountId = Guid.NewGuid(),
            SourceAccountName = "From",
            DestinationAccountId = Guid.NewGuid(),
            DestinationAccountName = "To",
        });

        var cut = Render<RecurringTransfers>();

        cut.Markup.ShouldContain("→");
    }

    /// <summary>
    /// Verifies active transfers show Skip and Pause buttons.
    /// </summary>
    [Fact]
    public void ActiveTransfer_ShowsSkipAndPauseButtons()
    {
        this._apiService.RecurringTransfers.Add(new RecurringTransferDto
        {
            Id = Guid.NewGuid(),
            Description = "Active Transfer",
            Amount = new MoneyDto { Amount = 200.00m, Currency = "USD" },
            Frequency = "Monthly",
            NextOccurrence = new DateOnly(2026, 4, 1),
            StartDate = new DateOnly(2025, 1, 1),
            IsActive = true,
            SourceAccountId = Guid.NewGuid(),
            SourceAccountName = "A",
            DestinationAccountId = Guid.NewGuid(),
            DestinationAccountName = "B",
        });

        var cut = Render<RecurringTransfers>();

        cut.Markup.ShouldContain("Skip");
        cut.Markup.ShouldContain("Pause");
    }

    /// <summary>
    /// Verifies paused transfers show Resume button.
    /// </summary>
    [Fact]
    public void PausedTransfer_ShowsResumeButton()
    {
        this._apiService.RecurringTransfers.Add(new RecurringTransferDto
        {
            Id = Guid.NewGuid(),
            Description = "Paused Transfer",
            Amount = new MoneyDto { Amount = 150.00m, Currency = "USD" },
            Frequency = "Monthly",
            NextOccurrence = new DateOnly(2026, 4, 1),
            StartDate = new DateOnly(2025, 1, 1),
            IsActive = false,
            SourceAccountId = Guid.NewGuid(),
            SourceAccountName = "A",
            DestinationAccountId = Guid.NewGuid(),
            DestinationAccountName = "B",
        });

        var cut = Render<RecurringTransfers>();

        cut.Markup.ShouldContain("Resume");
    }

    /// <summary>
    /// Verifies inactive transfers have the inactive CSS class and show Paused status.
    /// </summary>
    [Fact]
    public void InactiveTransfer_HasInactiveCssAndPausedStatus()
    {
        this._apiService.RecurringTransfers.Add(new RecurringTransferDto
        {
            Id = Guid.NewGuid(),
            Description = "Inactive",
            Amount = new MoneyDto { Amount = 50.00m, Currency = "USD" },
            Frequency = "Weekly",
            NextOccurrence = new DateOnly(2026, 4, 1),
            StartDate = new DateOnly(2025, 1, 1),
            IsActive = false,
            SourceAccountId = Guid.NewGuid(),
            SourceAccountName = "X",
            DestinationAccountId = Guid.NewGuid(),
            DestinationAccountName = "Y",
        });

        var cut = Render<RecurringTransfers>();

        cut.Markup.ShouldContain("inactive");
        cut.Markup.ShouldContain("Paused");
    }

    /// <summary>
    /// Verifies end date is displayed when present.
    /// </summary>
    [Fact]
    public void ShowsEndDate_WhenPresent()
    {
        this._apiService.RecurringTransfers.Add(new RecurringTransferDto
        {
            Id = Guid.NewGuid(),
            Description = "Ending Transfer",
            Amount = new MoneyDto { Amount = 300.00m, Currency = "USD" },
            Frequency = "Monthly",
            NextOccurrence = new DateOnly(2026, 5, 1),
            StartDate = new DateOnly(2025, 1, 1),
            EndDate = new DateOnly(2027, 1, 1),
            IsActive = true,
            SourceAccountId = Guid.NewGuid(),
            SourceAccountName = "A",
            DestinationAccountId = Guid.NewGuid(),
            DestinationAccountName = "B",
        });

        var cut = Render<RecurringTransfers>();

        cut.Markup.ShouldContain("Ends:");
    }
}
