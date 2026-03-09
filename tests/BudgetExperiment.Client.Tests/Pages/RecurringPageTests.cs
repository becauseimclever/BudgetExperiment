// <copyright file="RecurringPageTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

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
        this.Services.AddTransient<RecurringViewModel>();
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

    /// <summary>
    /// Verifies card shows account name.
    /// </summary>
    [Fact]
    public void Card_ShowsAccountName()
    {
        this._apiService.RecurringTransactions.Add(CreateRecurring("Gym Membership", "Checking"));

        var cut = Render<Recurring>();

        cut.Markup.ShouldContain("Checking");
    }

    /// <summary>
    /// Verifies card shows frequency info.
    /// </summary>
    [Fact]
    public void Card_ShowsFrequency()
    {
        this._apiService.RecurringTransactions.Add(CreateRecurring("Insurance", "Main"));

        var cut = Render<Recurring>();

        cut.Markup.ShouldContain("Monthly");
    }

    /// <summary>
    /// Verifies card shows next occurrence date.
    /// </summary>
    [Fact]
    public void Card_ShowsNextOccurrence()
    {
        this._apiService.RecurringTransactions.Add(CreateRecurring("Rent", "Checking"));

        var cut = Render<Recurring>();

        cut.Markup.ShouldContain("Next:");
    }

    /// <summary>
    /// Verifies multiple recurring items are rendered.
    /// </summary>
    [Fact]
    public void ShowsMultipleRecurringItems()
    {
        this._apiService.RecurringTransactions.Add(CreateRecurring("Rent", "Checking"));
        this._apiService.RecurringTransactions.Add(CreateRecurring("Electric", "Savings"));

        var cut = Render<Recurring>();

        cut.Markup.ShouldContain("Rent");
        cut.Markup.ShouldContain("Electric");
    }

    /// <summary>
    /// Verifies Skip button triggers the skip action.
    /// </summary>
    [Fact]
    public void SkipButton_IsClickable()
    {
        this._apiService.RecurringTransactions.Add(CreateRecurring("Rent", "Checking"));

        var cut = Render<Recurring>();
        var skipButton = cut.FindAll("button").First(b => b.TextContent.Contains("Skip"));
        skipButton.Click();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies Pause button triggers the pause action.
    /// </summary>
    [Fact]
    public void PauseButton_IsClickable()
    {
        this._apiService.RecurringTransactions.Add(CreateRecurring("Rent", "Checking"));

        var cut = Render<Recurring>();
        var pauseButton = cut.FindAll("button").First(b => b.TextContent.Contains("Pause"));
        pauseButton.Click();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies Resume button triggers the resume action on paused items.
    /// </summary>
    [Fact]
    public void ResumeButton_IsClickable()
    {
        this._apiService.RecurringTransactions.Add(new RecurringTransactionDto
        {
            Id = Guid.NewGuid(),
            Description = "Paused Item",
            Amount = new MoneyDto { Amount = -75.00m, Currency = "USD" },
            Frequency = "Monthly",
            NextOccurrence = new DateOnly(2026, 5, 1),
            StartDate = new DateOnly(2025, 1, 1),
            IsActive = false,
            AccountId = Guid.NewGuid(),
            AccountName = "Checking",
        });

        var cut = Render<Recurring>();
        var resumeButton = cut.FindAll("button").First(b => b.TextContent.Contains("Resume"));
        resumeButton.Click();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies Delete button opens confirm dialog.
    /// </summary>
    [Fact]
    public void DeleteButton_ShowsConfirmDialog()
    {
        this._apiService.RecurringTransactions.Add(CreateRecurring("Rent", "Checking"));

        var cut = Render<Recurring>();
        var deleteButton = cut.FindAll("button").First(b => b.TextContent.Contains("Delete"));
        deleteButton.Click();

        cut.Markup.ShouldContain("Are you sure");
    }

    /// <summary>
    /// Verifies Edit button opens edit form.
    /// </summary>
    [Fact]
    public void EditButton_OpensEditForm()
    {
        this._apiService.RecurringTransactions.Add(CreateRecurring("Rent", "Checking"));

        var cut = Render<Recurring>();
        var editButton = cut.FindAll("button").First(b => b.TextContent.Contains("Edit"));
        editButton.Click();

        cut.Markup.ShouldContain("Edit Recurring Transaction");
    }

    /// <summary>
    /// Verifies Add button opens add form.
    /// </summary>
    [Fact]
    public void AddButton_OpensAddForm()
    {
        var cut = Render<Recurring>();
        var addButton = cut.FindAll("button").First(b => b.TextContent.Contains("Add Recurring"));
        addButton.Click();

        cut.Markup.ShouldContain("Add Recurring Transaction");
    }

    private static RecurringTransactionDto CreateRecurring(string description, string account)
    {
        return new RecurringTransactionDto
        {
            Id = Guid.NewGuid(),
            Description = description,
            Amount = new MoneyDto { Amount = -100.00m, Currency = "USD" },
            Frequency = "Monthly",
            NextOccurrence = new DateOnly(2026, 5, 1),
            StartDate = new DateOnly(2025, 1, 1),
            IsActive = true,
            AccountId = Guid.NewGuid(),
            AccountName = account,
        };
    }
}
