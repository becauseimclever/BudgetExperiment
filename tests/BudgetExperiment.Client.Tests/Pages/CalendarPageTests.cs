// <copyright file="CalendarPageTests.cs" company="BecauseImClever">
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
/// Unit tests for the <see cref="Calendar"/> page component.
/// </summary>
public class CalendarPageTests : BunitContext, IAsyncLifetime
{
    private readonly StubBudgetApiService _apiService = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarPageTests"/> class.
    /// </summary>
    public CalendarPageTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<IBudgetApiService>(this._apiService);
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
    /// Verifies the page renders without errors when using default parameters (today).
    /// </summary>
    [Fact]
    public void Renders_WithDefaultParameters()
    {
        var cut = Render<Calendar>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the page title is set.
    /// </summary>
    [Fact]
    public void HasMonthYearInHeading()
    {
        var today = DateTime.Today;

        var cut = Render<Calendar>();

        // The heading should contain the current month name and year
        cut.Markup.ShouldContain(today.ToString("MMMM"));
        cut.Markup.ShouldContain(today.Year.ToString());
    }

    /// <summary>
    /// Verifies the Previous button is present.
    /// </summary>
    [Fact]
    public void HasPreviousButton()
    {
        var cut = Render<Calendar>();

        cut.Markup.ShouldContain("Previous");
    }

    /// <summary>
    /// Verifies the Next button is present.
    /// </summary>
    [Fact]
    public void HasNextButton()
    {
        var cut = Render<Calendar>();

        cut.Markup.ShouldContain("Next");
    }

    /// <summary>
    /// Verifies the Reports link is present.
    /// </summary>
    [Fact]
    public void HasReportsLink()
    {
        var cut = Render<Calendar>();

        cut.Markup.ShouldContain("Reports");
    }

    /// <summary>
    /// Verifies the page renders with explicit year/month parameters.
    /// </summary>
    [Fact]
    public void Renders_WithExplicitYearMonth()
    {
        var cut = Render<Calendar>(parameters => parameters
            .Add(p => p.Year, 2025)
            .Add(p => p.Month, 3));

        cut.Markup.ShouldContain("March");
        cut.Markup.ShouldContain("2025");
    }

    /// <summary>
    /// Verifies account filter dropdown is rendered when accounts exist.
    /// </summary>
    [Fact]
    public void ShowsAccountFilter_WhenAccountsExist()
    {
        this._apiService.Accounts.Add(new AccountDto
        {
            Id = Guid.NewGuid(),
            Name = "Checking",
            Type = "Checking",
            InitialBalance = 1000m,
            InitialBalanceCurrency = "USD",
            InitialBalanceDate = new DateOnly(2025, 1, 1),
        });

        var cut = Render<Calendar>();

        cut.Markup.ShouldContain("Checking");
    }

    /// <summary>
    /// Verifies that calendar grid renders day-of-week headers.
    /// </summary>
    [Fact]
    public void ShowsDayOfWeekHeaders()
    {
        var cut = Render<Calendar>();

        // The calendar should show day abbreviations
        cut.Markup.ShouldContain("Sun");
        cut.Markup.ShouldContain("Mon");
    }

    /// <summary>
    /// Verifies the add transaction modal starts hidden.
    /// </summary>
    [Fact]
    public void AddTransactionModal_IsHiddenByDefault()
    {
        var cut = Render<Calendar>();

        // Modal for adding transactions shouldn't be visible initially
        cut.Markup.ShouldNotContain("Add Transaction");
    }
}
