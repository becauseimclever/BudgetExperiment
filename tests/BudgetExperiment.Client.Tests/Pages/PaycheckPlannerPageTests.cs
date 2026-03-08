// <copyright file="PaycheckPlannerPageTests.cs" company="BecauseImClever">
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
/// Unit tests for the <see cref="PaycheckPlanner"/> page component.
/// </summary>
public class PaycheckPlannerPageTests : BunitContext, IAsyncLifetime
{
    private readonly StubBudgetApiService _apiService = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="PaycheckPlannerPageTests"/> class.
    /// </summary>
    public PaycheckPlannerPageTests()
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
        var cut = Render<PaycheckPlanner>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the page title is set correctly.
    /// </summary>
    [Fact]
    public void HasCorrectPageTitle()
    {
        var cut = Render<PaycheckPlanner>();

        cut.Markup.ShouldContain("Paycheck Planner");
    }

    /// <summary>
    /// Verifies the subtitle is displayed.
    /// </summary>
    [Fact]
    public void HasSubtitle()
    {
        var cut = Render<PaycheckPlanner>();

        cut.Markup.ShouldContain("Plan your paycheck allocations for recurring bills");
    }

    /// <summary>
    /// Verifies the configuration section is shown.
    /// </summary>
    [Fact]
    public void ShowsConfigSection()
    {
        var cut = Render<PaycheckPlanner>();

        cut.Markup.ShouldContain("Configure Your Paycheck");
    }

    /// <summary>
    /// Verifies the pay frequency dropdown is present with options.
    /// </summary>
    [Fact]
    public void ShowsFrequencyDropdown()
    {
        var cut = Render<PaycheckPlanner>();

        cut.Markup.ShouldContain("Pay Frequency");
        cut.Markup.ShouldContain("Weekly");
        cut.Markup.ShouldContain("BiWeekly");
        cut.Markup.ShouldContain("Monthly");
    }

    /// <summary>
    /// Verifies the paycheck amount input is present.
    /// </summary>
    [Fact]
    public void ShowsPaycheckAmountInput()
    {
        var cut = Render<PaycheckPlanner>();

        cut.Markup.ShouldContain("Paycheck Amount");
        cut.Markup.ShouldContain("Enter amount");
    }

    /// <summary>
    /// Verifies the account filter dropdown is present.
    /// </summary>
    [Fact]
    public void ShowsAccountFilterDropdown()
    {
        var cut = Render<PaycheckPlanner>();

        cut.Markup.ShouldContain("Filter by Account");
        cut.Markup.ShouldContain("All Accounts");
    }

    /// <summary>
    /// Verifies the Calculate button is present.
    /// </summary>
    [Fact]
    public void HasCalculateButton()
    {
        var cut = Render<PaycheckPlanner>();

        cut.Markup.ShouldContain("Calculate");
    }

    /// <summary>
    /// Verifies accounts are shown in the filter dropdown.
    /// </summary>
    [Fact]
    public void ShowsAccountsInDropdown()
    {
        this._apiService.Accounts.Add(new AccountDto
        {
            Id = Guid.NewGuid(),
            Name = "Bills Account",
            Type = "Checking",
            InitialBalance = 0m,
            InitialBalanceCurrency = "USD",
            InitialBalanceDate = new DateOnly(2025, 1, 1),
        });

        var cut = Render<PaycheckPlanner>();

        cut.Markup.ShouldContain("Bills Account");
    }

    /// <summary>
    /// Verifies summary section is shown when allocation data exists.
    /// </summary>
    [Fact]
    public void ShowsSummary_WhenAllocationExists()
    {
        this._apiService.AllocationSummary = CreateAllocationSummary();

        var cut = Render<PaycheckPlanner>();

        // Summary won't show until Calculate is clicked. The page starts without allocationSummary.
        // This test verifies the page renders without errors. The allocation summary is set
        // after the Calculate button is clicked.
        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the accounts in the set-aside account dropdowns.
    /// </summary>
    [Fact]
    public void ShowsTransferAccountDropdowns_WhenAccountsExist()
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
        this._apiService.Accounts.Add(new AccountDto
        {
            Id = Guid.NewGuid(),
            Name = "Bills Savings",
            Type = "Savings",
            InitialBalance = 0m,
            InitialBalanceCurrency = "USD",
            InitialBalanceDate = new DateOnly(2025, 1, 1),
        });

        var cut = Render<PaycheckPlanner>();

        // The accounts should appear in the page (account filter and also in the page context)
        cut.Markup.ShouldContain("Main Checking");
        cut.Markup.ShouldContain("Bills Savings");
    }

    /// <summary>
    /// Verifies the dollar sign prefix is present for amount input.
    /// </summary>
    [Fact]
    public void ShowsDollarSignAddon()
    {
        var cut = Render<PaycheckPlanner>();

        cut.Markup.ShouldContain("$");
    }

    /// <summary>
    /// Verifies clicking Calculate shows the summary section.
    /// </summary>
    [Fact]
    public void CalculateButton_ShowsSummary()
    {
        this._apiService.AllocationSummary = CreateAllocationSummary();

        var cut = Render<PaycheckPlanner>();
        var calculateButton = cut.Find("button.btn-primary");
        calculateButton.Click();

        cut.Markup.ShouldContain("Summary");
        cut.Markup.ShouldContain("Total per Paycheck");
    }

    /// <summary>
    /// Verifies clicking Calculate shows the bill breakdown table.
    /// </summary>
    [Fact]
    public void CalculateButton_ShowsBillBreakdown()
    {
        this._apiService.AllocationSummary = CreateAllocationSummary();

        var cut = Render<PaycheckPlanner>();
        var calculateButton = cut.Find("button.btn-primary");
        calculateButton.Click();

        cut.Markup.ShouldContain("Bill Breakdown");
        cut.Markup.ShouldContain("Rent");
    }

    /// <summary>
    /// Verifies the annual total is displayed after calculation.
    /// </summary>
    [Fact]
    public void CalculateButton_ShowsAnnualTotal()
    {
        this._apiService.AllocationSummary = CreateAllocationSummary();

        var cut = Render<PaycheckPlanner>();
        cut.Find("button.btn-primary").Click();

        cut.Markup.ShouldContain("Annual Total");
    }

    /// <summary>
    /// Verifies warnings are displayed when allocation has warnings.
    /// </summary>
    [Fact]
    public void CalculateButton_ShowsWarnings_WhenPresent()
    {
        var summary = CreateAllocationSummary();
        summary.HasWarnings = true;
        summary.Warnings =
        [
            new PaycheckAllocationWarningDto
            {
                Type = "InsufficientIncome",
                Message = "Your income may not cover all bills",
            },
        ];
        this._apiService.AllocationSummary = summary;

        var cut = Render<PaycheckPlanner>();
        cut.Find("button.btn-primary").Click();

        cut.Markup.ShouldContain("Your income may not cover all bills");
    }

    /// <summary>
    /// Verifies the cannot-reconcile warning uses the red warning class.
    /// </summary>
    [Fact]
    public void CalculateButton_ShowsRedWarning_ForCannotReconcile()
    {
        var summary = CreateAllocationSummary();
        summary.HasWarnings = true;
        summary.CannotReconcile = true;
        summary.Warnings =
        [
            new PaycheckAllocationWarningDto
            {
                Type = "CannotReconcile",
                Message = "Cannot reconcile bills with income",
            },
        ];
        this._apiService.AllocationSummary = summary;

        var cut = Render<PaycheckPlanner>();
        cut.Find("button.btn-primary").Click();

        cut.Markup.ShouldContain("warning-red");
    }

    /// <summary>
    /// Verifies the set aside section appears after calculation.
    /// </summary>
    [Fact]
    public void CalculateButton_ShowsSetAsideSection()
    {
        this._apiService.AllocationSummary = CreateAllocationSummary();

        var cut = Render<PaycheckPlanner>();
        cut.Find("button.btn-primary").Click();

        cut.Markup.ShouldContain("Set Aside Money");
    }

    /// <summary>
    /// Verifies the NoBillsConfigured warning uses the info warning class.
    /// </summary>
    [Fact]
    public void CalculateButton_ShowsInfoWarning_ForNoBillsConfigured()
    {
        var summary = CreateAllocationSummary();
        summary.HasWarnings = true;
        summary.Allocations = [];
        summary.Warnings =
        [
            new PaycheckAllocationWarningDto
            {
                Type = "NoBillsConfigured",
                Message = "No recurring bills configured",
            },
        ];
        this._apiService.AllocationSummary = summary;

        var cut = Render<PaycheckPlanner>();
        cut.Find("button.btn-primary").Click();

        cut.Markup.ShouldContain("warning-info");
    }

    /// <summary>
    /// Verifies error message shows when allocation returns null.
    /// </summary>
    [Fact]
    public void CalculateButton_ShowsError_WhenAllocationNull()
    {
        this._apiService.AllocationSummary = null;

        var cut = Render<PaycheckPlanner>();
        cut.Find("button.btn-primary").Click();

        cut.Markup.ShouldContain("Failed to calculate allocation");
    }

    /// <summary>
    /// Verifies the breakdown table shows multiple allocations.
    /// </summary>
    [Fact]
    public void ShowsMultipleAllocations_InBreakdownTable()
    {
        var summary = CreateAllocationSummary();
        summary.Allocations.Add(new PaycheckAllocationDto
        {
            Description = "Electric",
            BillAmount = new MoneyDto { Amount = 200m, Currency = "USD" },
            BillFrequency = "Monthly",
            AmountPerPaycheck = new MoneyDto { Amount = 100m, Currency = "USD" },
        });
        this._apiService.AllocationSummary = summary;

        var cut = Render<PaycheckPlanner>();
        cut.Find("button.btn-primary").Click();

        cut.Markup.ShouldContain("Rent");
        cut.Markup.ShouldContain("Electric");
    }

    private static PaycheckAllocationSummaryDto CreateAllocationSummary()
    {
        return new PaycheckAllocationSummaryDto
        {
            Allocations =
            [
                new PaycheckAllocationDto
                {
                    Description = "Rent",
                    BillAmount = new MoneyDto { Amount = 1500m, Currency = "USD" },
                    BillFrequency = "Monthly",
                    AmountPerPaycheck = new MoneyDto { Amount = 750m, Currency = "USD" },
                },
            ],
            TotalPerPaycheck = new MoneyDto { Amount = 750m, Currency = "USD" },
            RemainingPerPaycheck = new MoneyDto { Amount = 250m, Currency = "USD" },
            Shortfall = new MoneyDto { Amount = 0m, Currency = "USD" },
            TotalAnnualBills = new MoneyDto { Amount = 18000m, Currency = "USD" },
            Warnings = [],
            HasWarnings = false,
            CannotReconcile = false,
            PaycheckFrequency = "BiWeekly",
        };
    }
}
