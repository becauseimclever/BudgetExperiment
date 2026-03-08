// <copyright file="RecurringPageTests.cs" company="BecauseImClever">
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
/// Unit tests for the <see cref="Recurring"/> page component.
/// </summary>
public class RecurringPageTests : BunitContext, IAsyncLifetime
{
    private readonly StubBudgetApiService _apiService = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringPageTests"/> class.
    /// </summary>
    public RecurringPageTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<IBudgetApiService>(this._apiService);
        this.Services.AddSingleton<IToastService>(new ToastService());
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
        var cut = Render<Recurring>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the page title is set correctly.
    /// </summary>
    [Fact]
    public void HasCorrectPageTitle()
    {
        var cut = Render<Recurring>();

        cut.Markup.ShouldContain("Recurring Transactions");
    }

    /// <summary>
    /// Verifies empty state message when no recurring transactions exist.
    /// </summary>
    [Fact]
    public void ShowsEmptyState_WhenNoRecurringTransactions()
    {
        var cut = Render<Recurring>();

        cut.Markup.ShouldContain("No Recurring Transactions");
        cut.Markup.ShouldContain("Create Your First Recurring Transaction");
    }

    /// <summary>
    /// Verifies the Add Recurring button is present.
    /// </summary>
    [Fact]
    public void HasAddRecurringButton()
    {
        var cut = Render<Recurring>();

        cut.Markup.ShouldContain("Add Recurring");
    }

    /// <summary>
    /// Verifies recurring cards are shown when data exists.
    /// </summary>
    [Fact]
    public void ShowsRecurringCards_WhenDataExists()
    {
        this._apiService.RecurringTransactions.Add(new RecurringTransactionDto
        {
            Id = Guid.NewGuid(),
            Description = "Monthly Rent",
            Amount = new MoneyDto { Amount = -1200.00m, Currency = "USD" },
            Frequency = "Monthly",
            NextOccurrence = new DateOnly(2026, 4, 1),
            StartDate = new DateOnly(2025, 1, 1),
            IsActive = true,
            AccountId = Guid.NewGuid(),
            AccountName = "Checking",
        });

        var cut = Render<Recurring>();

        cut.Markup.ShouldContain("Monthly Rent");
        cut.Markup.ShouldContain("Checking");
    }

    /// <summary>
    /// Verifies active recurring transactions show Skip and Pause buttons.
    /// </summary>
    [Fact]
    public void ActiveRecurring_ShowsSkipAndPauseButtons()
    {
        this._apiService.RecurringTransactions.Add(new RecurringTransactionDto
        {
            Id = Guid.NewGuid(),
            Description = "Salary",
            Amount = new MoneyDto { Amount = 3000.00m, Currency = "USD" },
            Frequency = "Monthly",
            NextOccurrence = new DateOnly(2026, 4, 15),
            StartDate = new DateOnly(2025, 1, 15),
            IsActive = true,
            AccountId = Guid.NewGuid(),
            AccountName = "Checking",
        });

        var cut = Render<Recurring>();

        cut.Markup.ShouldContain("Skip");
        cut.Markup.ShouldContain("Pause");
    }

    /// <summary>
    /// Verifies paused recurring transactions show Resume button.
    /// </summary>
    [Fact]
    public void PausedRecurring_ShowsResumeButton()
    {
        this._apiService.RecurringTransactions.Add(new RecurringTransactionDto
        {
            Id = Guid.NewGuid(),
            Description = "Gym Membership",
            Amount = new MoneyDto { Amount = -49.99m, Currency = "USD" },
            Frequency = "Monthly",
            NextOccurrence = new DateOnly(2026, 4, 1),
            StartDate = new DateOnly(2025, 6, 1),
            IsActive = false,
            AccountId = Guid.NewGuid(),
            AccountName = "Checking",
        });

        var cut = Render<Recurring>();

        cut.Markup.ShouldContain("Resume");
    }

    /// <summary>
    /// Verifies inactive recurring cards have the inactive CSS class.
    /// </summary>
    [Fact]
    public void InactiveRecurring_HasInactiveCssClass()
    {
        this._apiService.RecurringTransactions.Add(new RecurringTransactionDto
        {
            Id = Guid.NewGuid(),
            Description = "Paused Item",
            Amount = new MoneyDto { Amount = -10.00m, Currency = "USD" },
            Frequency = "Weekly",
            NextOccurrence = new DateOnly(2026, 4, 1),
            StartDate = new DateOnly(2025, 1, 1),
            IsActive = false,
            AccountId = Guid.NewGuid(),
            AccountName = "Savings",
        });

        var cut = Render<Recurring>();

        cut.Markup.ShouldContain("inactive");
        cut.Markup.ShouldContain("Paused");
    }

    /// <summary>
    /// Verifies that end date is shown when present.
    /// </summary>
    [Fact]
    public void ShowsEndDate_WhenPresent()
    {
        this._apiService.RecurringTransactions.Add(new RecurringTransactionDto
        {
            Id = Guid.NewGuid(),
            Description = "Lease",
            Amount = new MoneyDto { Amount = -900.00m, Currency = "USD" },
            Frequency = "Monthly",
            NextOccurrence = new DateOnly(2026, 5, 1),
            StartDate = new DateOnly(2025, 5, 1),
            EndDate = new DateOnly(2027, 5, 1),
            IsActive = true,
            AccountId = Guid.NewGuid(),
            AccountName = "Checking",
        });

        var cut = Render<Recurring>();

        cut.Markup.ShouldContain("Ends:");
    }

    /// <summary>
    /// Verifies the status badge shows Active for active items.
    /// </summary>
    [Fact]
    public void ShowsActiveStatusBadge()
    {
        this._apiService.RecurringTransactions.Add(new RecurringTransactionDto
        {
            Id = Guid.NewGuid(),
            Description = "Active Item",
            Amount = new MoneyDto { Amount = -50.00m, Currency = "USD" },
            Frequency = "Monthly",
            NextOccurrence = new DateOnly(2026, 4, 1),
            StartDate = new DateOnly(2025, 1, 1),
            IsActive = true,
            AccountId = Guid.NewGuid(),
            AccountName = "Checking",
        });

        var cut = Render<Recurring>();

        cut.Markup.ShouldContain("badge-status-active");
        cut.Markup.ShouldContain("Active");
    }
}
