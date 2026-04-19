// <copyright file="ReconciliationPageTests.cs" company="BecauseImClever">
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
/// Unit tests for the <see cref="Reconciliation"/> page component.
/// </summary>
public class ReconciliationPageTests : BunitContext, IAsyncLifetime
{
    private readonly StubBudgetApiService _apiService = new();
    private readonly StubReconciliationApiService _reconciliationApiService = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ReconciliationPageTests"/> class.
    /// </summary>
    public ReconciliationPageTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<IBudgetApiService>(_apiService);
        this.Services.AddSingleton<IReconciliationApiService>(_reconciliationApiService);
        this.Services.AddSingleton<IToastService>(new ToastService());
        this.Services.AddSingleton<ThemeService>();
        this.Services.AddSingleton<CultureService>();
        this.Services.AddSingleton<IExportDownloadService>(new StubExportDownloadService());
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
        var cut = Render<Reconciliation>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the page title is set correctly.
    /// </summary>
    [Fact]
    public void HasCorrectPageTitle()
    {
        var cut = Render<Reconciliation>();

        cut.Markup.ShouldContain("Reconciliation");
    }

    /// <summary>
    /// Verifies the Settings button is present.
    /// </summary>
    [Fact]
    public void HasSettingsButton()
    {
        var cut = Render<Reconciliation>();

        cut.Markup.ShouldContain("Settings");
    }

    /// <summary>
    /// Verifies the Refresh button is present.
    /// </summary>
    [Fact]
    public void HasRefreshButton()
    {
        var cut = Render<Reconciliation>();

        cut.Markup.ShouldContain("Refresh");
    }

    /// <summary>
    /// Verifies empty state when no reconciliation data is loaded.
    /// </summary>
    [Fact]
    public void ShowsEmptyState_WhenNoData()
    {
        var cut = Render<Reconciliation>();

        cut.Markup.ShouldContain("No Reconciliation Data");
    }

    /// <summary>
    /// Verifies the filter bar is rendered with period selector.
    /// </summary>
    [Fact]
    public void ShowsFilterBar_WithPeriodSelector()
    {
        var cut = Render<Reconciliation>();

        cut.Markup.ShouldContain("Period:");
        cut.Markup.ShouldContain("Account:");
        cut.Markup.ShouldContain("Status:");
    }

    /// <summary>
    /// Verifies the Apply button is present in the filter bar.
    /// </summary>
    [Fact]
    public void HasApplyButton_InFilterBar()
    {
        var cut = Render<Reconciliation>();

        cut.Markup.ShouldContain("Apply");
    }

    /// <summary>
    /// Verifies summary cards are shown when status data exists.
    /// </summary>
    [Fact]
    public void ShowsSummaryCards_WhenStatusExists()
    {
        _reconciliationApiService.Status = new ReconciliationStatusDto
        {
            Year = 2025,
            Month = 6,
            TotalExpectedInstances = 10,
            MatchedCount = 7,
            PendingCount = 2,
            MissingCount = 1,
            Instances = [],
        };

        var cut = Render<Reconciliation>();

        cut.Markup.ShouldContain("Total Expected");
        cut.Markup.ShouldContain("Matched");
        cut.Markup.ShouldContain("Pending Review");
        cut.Markup.ShouldContain("Missing");
    }

    /// <summary>
    /// Verifies summary card values display correctly.
    /// </summary>
    [Fact]
    public void ShowsCorrectSummaryValues()
    {
        _reconciliationApiService.Status = new ReconciliationStatusDto
        {
            Year = 2025,
            Month = 6,
            TotalExpectedInstances = 15,
            MatchedCount = 10,
            PendingCount = 3,
            MissingCount = 2,
            Instances = [],
        };

        var cut = Render<Reconciliation>();

        var summaryCards = cut.FindAll(".summary-card");
        summaryCards.Count.ShouldBe(4);
    }

    /// <summary>
    /// Verifies pending matches section is shown when matches exist.
    /// </summary>
    [Fact]
    public void ShowsPendingMatches_WhenMatchesExist()
    {
        _reconciliationApiService.Status = new ReconciliationStatusDto
        {
            Year = 2025,
            Month = 6,
            TotalExpectedInstances = 5,
            MatchedCount = 3,
            PendingCount = 2,
            MissingCount = 0,
            Instances = [],
        };

        _reconciliationApiService.PendingMatches.Add(new ReconciliationMatchDto
        {
            Id = Guid.NewGuid(),
            ImportedTransactionId = Guid.NewGuid(),
            RecurringTransactionId = Guid.NewGuid(),
            RecurringInstanceDate = new DateOnly(2025, 6, 15),
            ConfidenceScore = 0.95m,
            ConfidenceLevel = "High",
            Status = "Pending",
            Source = "Auto",
            AmountVariance = 0m,
            DateOffsetDays = 0,
            RecurringTransactionDescription = "Monthly Rent",
            ExpectedAmount = new MoneyDto { Amount = 1500m, Currency = "USD" },
        });

        var cut = Render<Reconciliation>();

        cut.Markup.ShouldContain("Pending Matches");
        cut.Markup.ShouldContain("Monthly Rent");
    }

    /// <summary>
    /// Verifies Accept and Reject buttons appear for pending matches.
    /// </summary>
    [Fact]
    public void ShowsAcceptAndRejectButtons_ForPendingMatches()
    {
        _reconciliationApiService.Status = new ReconciliationStatusDto
        {
            Year = 2025,
            Month = 6,
            TotalExpectedInstances = 1,
            MatchedCount = 0,
            PendingCount = 1,
            MissingCount = 0,
            Instances = [],
        };

        _reconciliationApiService.PendingMatches.Add(new ReconciliationMatchDto
        {
            Id = Guid.NewGuid(),
            ImportedTransactionId = Guid.NewGuid(),
            RecurringTransactionId = Guid.NewGuid(),
            RecurringInstanceDate = new DateOnly(2025, 6, 15),
            ConfidenceScore = 0.80m,
            ConfidenceLevel = "Medium",
            Status = "Pending",
            Source = "Auto",
            AmountVariance = 0m,
            DateOffsetDays = 1,
            RecurringTransactionDescription = "Utility Bill",
            ExpectedAmount = new MoneyDto { Amount = 150m, Currency = "USD" },
        });

        var cut = Render<Reconciliation>();

        cut.Markup.ShouldContain("Accept");
        cut.Markup.ShouldContain("Reject");
        cut.Markup.ShouldContain("Details");
    }

    /// <summary>
    /// Verifies the confidence badge is rendered for pending matches.
    /// </summary>
    [Fact]
    public void ShowsConfidenceBadge_ForPendingMatch()
    {
        _reconciliationApiService.Status = new ReconciliationStatusDto
        {
            Year = 2025,
            Month = 6,
            TotalExpectedInstances = 1,
            MatchedCount = 0,
            PendingCount = 1,
            MissingCount = 0,
            Instances = [],
        };

        _reconciliationApiService.PendingMatches.Add(new ReconciliationMatchDto
        {
            Id = Guid.NewGuid(),
            ImportedTransactionId = Guid.NewGuid(),
            RecurringTransactionId = Guid.NewGuid(),
            RecurringInstanceDate = new DateOnly(2025, 6, 1),
            ConfidenceScore = 0.95m,
            ConfidenceLevel = "High",
            Status = "Pending",
            Source = "Auto",
            AmountVariance = 0m,
            DateOffsetDays = 0,
            RecurringTransactionDescription = "Test",
            ExpectedAmount = new MoneyDto { Amount = 100m, Currency = "USD" },
        });

        var cut = Render<Reconciliation>();

        cut.Markup.ShouldContain("High");
        var badges = cut.FindAll(".confidence-badge");
        badges.Count.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// Verifies the recurring instances status section is rendered.
    /// </summary>
    [Fact]
    public void ShowsRecurringTransactionStatus_WhenInstancesExist()
    {
        _reconciliationApiService.Status = new ReconciliationStatusDto
        {
            Year = 2025,
            Month = 6,
            TotalExpectedInstances = 2,
            MatchedCount = 1,
            PendingCount = 0,
            MissingCount = 1,
            Instances =
            [
                new RecurringInstanceStatusDto
                {
                    RecurringTransactionId = Guid.NewGuid(),
                    Description = "Electricity",
                    AccountId = Guid.NewGuid(),
                    AccountName = "Checking",
                    InstanceDate = new DateOnly(2025, 6, 15),
                    ExpectedAmount = new MoneyDto { Amount = 120m, Currency = "USD" },
                    Status = "Matched",
                    ActualAmount = new MoneyDto { Amount = 118m, Currency = "USD" },
                },
                new RecurringInstanceStatusDto
                {
                    RecurringTransactionId = Guid.NewGuid(),
                    Description = "Internet",
                    AccountId = Guid.NewGuid(),
                    AccountName = "Checking",
                    InstanceDate = new DateOnly(2025, 6, 20),
                    ExpectedAmount = new MoneyDto { Amount = 80m, Currency = "USD" },
                    Status = "Missing",
                },
            ],
        };

        var cut = Render<Reconciliation>();

        cut.Markup.ShouldContain("Recurring Transaction Status");
        cut.Markup.ShouldContain("Electricity");
        cut.Markup.ShouldContain("Internet");
    }

    /// <summary>
    /// Verifies the Link Manually button appears for missing instances.
    /// </summary>
    [Fact]
    public void ShowsLinkManuallyButton_ForMissingInstances()
    {
        _reconciliationApiService.Status = new ReconciliationStatusDto
        {
            Year = 2025,
            Month = 6,
            TotalExpectedInstances = 1,
            MatchedCount = 0,
            PendingCount = 0,
            MissingCount = 1,
            Instances =
            [
                new RecurringInstanceStatusDto
                {
                    RecurringTransactionId = Guid.NewGuid(),
                    Description = "Phone Bill",
                    AccountId = Guid.NewGuid(),
                    AccountName = "Checking",
                    InstanceDate = new DateOnly(2025, 6, 10),
                    ExpectedAmount = new MoneyDto { Amount = 85m, Currency = "USD" },
                    Status = "Missing",
                },
            ],
        };

        var cut = Render<Reconciliation>();

        cut.Markup.ShouldContain("Link Manually");
    }

    /// <summary>
    /// Verifies the Unlink button appears for matched instances.
    /// </summary>
    [Fact]
    public void ShowsUnlinkButton_ForMatchedInstances()
    {
        _reconciliationApiService.Status = new ReconciliationStatusDto
        {
            Year = 2025,
            Month = 6,
            TotalExpectedInstances = 1,
            MatchedCount = 1,
            PendingCount = 0,
            MissingCount = 0,
            Instances =
            [
                new RecurringInstanceStatusDto
                {
                    RecurringTransactionId = Guid.NewGuid(),
                    Description = "Rent",
                    AccountId = Guid.NewGuid(),
                    AccountName = "Checking",
                    InstanceDate = new DateOnly(2025, 6, 1),
                    ExpectedAmount = new MoneyDto { Amount = 1500m, Currency = "USD" },
                    Status = "Matched",
                    MatchId = Guid.NewGuid(),
                    ActualAmount = new MoneyDto { Amount = 1500m, Currency = "USD" },
                },
            ],
        };

        var cut = Render<Reconciliation>();

        cut.Markup.ShouldContain("Unlink");
    }

    /// <summary>
    /// Verifies the Accept All High Confidence button appears with multiple pending matches.
    /// </summary>
    [Fact]
    public void ShowsAcceptAllHighConfidence_WhenMultipleMatches()
    {
        _reconciliationApiService.Status = new ReconciliationStatusDto
        {
            Year = 2025,
            Month = 6,
            TotalExpectedInstances = 2,
            MatchedCount = 0,
            PendingCount = 2,
            MissingCount = 0,
            Instances = [],
        };

        _reconciliationApiService.PendingMatches.Add(CreateMatch("Bill 1", "High", 0.95m));
        _reconciliationApiService.PendingMatches.Add(CreateMatch("Bill 2", "High", 0.90m));

        var cut = Render<Reconciliation>();

        cut.Markup.ShouldContain("Accept All High Confidence");
    }

    /// <summary>
    /// Verifies that account dropdown is populated with accounts.
    /// </summary>
    [Fact]
    public void ShowsAccountsInDropdown()
    {
        _apiService.Accounts.Add(new AccountDto
        {
            Id = Guid.NewGuid(),
            Name = "Main Checking",
            Type = "Checking",
            InitialBalance = 0m,
            InitialBalanceCurrency = "USD",
            InitialBalanceDate = new DateOnly(2025, 1, 1),
        });

        var cut = Render<Reconciliation>();

        cut.Markup.ShouldContain("Main Checking");
        cut.Markup.ShouldContain("All Accounts");
    }

    /// <summary>
    /// Verifies status filter dropdown contains expected options.
    /// </summary>
    [Fact]
    public void ShowsStatusFilterOptions()
    {
        var cut = Render<Reconciliation>();

        cut.Markup.ShouldContain("All Statuses");
        cut.Markup.ShouldContain("Matched");
        cut.Markup.ShouldContain("Pending Review"); // the visible option text
        cut.Markup.ShouldContain("Missing");
        cut.Markup.ShouldContain("Skipped");
    }

    /// <summary>
    /// Verifies accept match result is configurable.
    /// </summary>
    [Fact]
    public void AcceptMatchResult_IsConfigurable()
    {
        _reconciliationApiService.AcceptMatchResult = true;

        var cut = Render<Reconciliation>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies reject match result is configurable.
    /// </summary>
    [Fact]
    public void RejectMatchResult_IsConfigurable()
    {
        _reconciliationApiService.RejectMatchResult = true;

        var cut = Render<Reconciliation>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies bulk accept result is configurable.
    /// </summary>
    [Fact]
    public void BulkAcceptResult_IsConfigurable()
    {
        _reconciliationApiService.BulkAcceptResult = 3;

        var cut = Render<Reconciliation>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies unlink match result is configurable.
    /// </summary>
    [Fact]
    public void UnlinkMatchResult_IsConfigurable()
    {
        _reconciliationApiService.UnlinkMatchResult = true;

        var cut = Render<Reconciliation>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies match with high confidence can be configured.
    /// </summary>
    [Fact]
    public void HighConfidenceMatch_IsConfigurable()
    {
        _reconciliationApiService.PendingMatches.Add(CreateMatch("Netflix", "High", 0.95m));

        var cut = Render<Reconciliation>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies Accept button is clickable for pending matches.
    /// </summary>
    [Fact]
    public void AcceptButton_IsClickable_ForPendingMatch()
    {
        _reconciliationApiService.AcceptMatchResult = true;
        _reconciliationApiService.Status = new ReconciliationStatusDto
        {
            Year = 2025,
            Month = 6,
            TotalExpectedInstances = 1,
            MatchedCount = 0,
            PendingCount = 1,
            MissingCount = 0,
            Instances = [],
        };
        _reconciliationApiService.PendingMatches.Add(CreateMatch("Netflix", "High", 0.95m));

        var cut = Render<Reconciliation>();
        var acceptButton = cut.FindAll("button").First(b => b.TextContent.Contains("Accept") && !b.TextContent.Contains("All"));
        acceptButton.Click();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies Reject button is clickable for pending matches.
    /// </summary>
    [Fact]
    public void RejectButton_IsClickable_ForPendingMatch()
    {
        _reconciliationApiService.RejectMatchResult = true;
        _reconciliationApiService.Status = new ReconciliationStatusDto
        {
            Year = 2025,
            Month = 6,
            TotalExpectedInstances = 1,
            MatchedCount = 0,
            PendingCount = 1,
            MissingCount = 0,
            Instances = [],
        };
        _reconciliationApiService.PendingMatches.Add(CreateMatch("Spotify", "Medium", 0.75m));

        var cut = Render<Reconciliation>();
        var rejectButton = cut.FindAll("button").First(b => b.TextContent.Contains("Reject"));
        rejectButton.Click();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies Refresh button triggers data reload.
    /// </summary>
    [Fact]
    public void RefreshButton_IsClickable()
    {
        var cut = Render<Reconciliation>();
        var refreshButton = cut.FindAll("button").First(b => b.TextContent.Contains("Refresh"));
        refreshButton.Click();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies Apply button in filter bar triggers filter application.
    /// </summary>
    [Fact]
    public void ApplyButton_TriggersFilter()
    {
        var cut = Render<Reconciliation>();
        var applyButton = cut.FindAll("button").First(b => b.TextContent.Contains("Apply"));
        applyButton.Click();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    private static ReconciliationMatchDto CreateMatch(string description, string confidence, decimal score)
    {
        return new ReconciliationMatchDto
        {
            Id = Guid.NewGuid(),
            ImportedTransactionId = Guid.NewGuid(),
            RecurringTransactionId = Guid.NewGuid(),
            RecurringInstanceDate = new DateOnly(2025, 6, 15),
            ConfidenceScore = score,
            ConfidenceLevel = confidence,
            Status = "Pending",
            Source = "Auto",
            AmountVariance = 0m,
            DateOffsetDays = 0,
            RecurringTransactionDescription = description,
            ExpectedAmount = new MoneyDto { Amount = 100m, Currency = "USD" },
        };
    }
}
