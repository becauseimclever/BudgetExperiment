// <copyright file="KaizenDashboardViewTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;

using BudgetExperiment.Client.Pages.Reports;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Client.Tests.TestHelpers;
using BudgetExperiment.Contracts.Dtos;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Pages.Reports;

/// <summary>
/// Unit tests for the <see cref="KaizenDashboardView"/> page component.
/// Tests data loading, error handling, empty states, and chart rendering for 12-week spending trends.
/// </summary>
public class KaizenDashboardViewTests : BunitContext, IAsyncLifetime
{
    private readonly StubBudgetApiService _fakeApiService = new();
    private readonly StubFeatureFlagClientService _fakeFeatureFlags = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="KaizenDashboardViewTests"/> class.
    /// </summary>
    public KaizenDashboardViewTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<IBudgetApiService>(_fakeApiService);
        this.Services.AddSingleton<IFeatureFlagClientService>(_fakeFeatureFlags);
        this.Services.AddSingleton<ThemeService>();
        this.Services.AddSingleton<CultureService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
        _fakeFeatureFlags.Flags["Kaizen:Dashboard"] = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies the loading spinner is displayed while data is being fetched.
    /// </summary>
    [Fact]
    public void KaizenDashboard_ShowsLoadingSpinner_DuringInitialLoad()
    {
        // Arrange - block the API call to keep component in loading state
        _fakeApiService.KaizenDashboardTaskSource = new TaskCompletionSource<KaizenDashboardDto?>();

        // Act
        var cut = Render<KaizenDashboardView>();

        // Assert
        Assert.Contains("Loading Kaizen dashboard", cut.Markup);
    }

    /// <summary>
    /// Verifies the feature disabled message is shown when feature flag is disabled.
    /// </summary>
    [Fact]
    public void KaizenDashboard_ShowsFeatureDisabledMessage_WhenFlagDisabled()
    {
        // Arrange
        _fakeFeatureFlags.Flags["Kaizen:Dashboard"] = false;

        // Act
        var cut = Render<KaizenDashboardView>();

        // Assert
        Assert.Contains("Feature not available", cut.Markup);
    }

    /// <summary>
    /// Verifies the empty state is displayed when no weeks of data are available.
    /// </summary>
    [Fact]
    public void KaizenDashboard_ShowsEmptyState_WhenNoWeeksAvailable()
    {
        // Arrange
        var emptyDashboard = new KaizenDashboardDto { Weeks = new List<WeeklyKakeiboSummaryDto>() };
        _fakeApiService.KaizenDashboardTaskSource = new TaskCompletionSource<KaizenDashboardDto?>();
        _fakeApiService.KaizenDashboardTaskSource.SetResult(emptyDashboard);

        // Act
        var cut = Render<KaizenDashboardView>();
        cut.WaitForState(() => !cut.Markup.Contains("Loading Kaizen dashboard"), TimeSpan.FromSeconds(1));

        // Assert
        Assert.Contains("No spending data available for the past 12 weeks", cut.Markup);
    }

    /// <summary>
    /// Verifies error message is displayed when API call fails.
    /// </summary>
    [Fact]
    public void KaizenDashboard_ShowsErrorMessage_WhenApiThrows()
    {
        // Arrange
        var exception = new HttpRequestException("Network error");
        _fakeApiService.KaizenDashboardTaskSource = new TaskCompletionSource<KaizenDashboardDto?>();
        _fakeApiService.KaizenDashboardTaskSource.SetException(exception);

        // Act
        var cut = Render<KaizenDashboardView>();
        cut.WaitForState(() => cut.Markup.Contains("Failed to load Kaizen Dashboard"), TimeSpan.FromSeconds(1));

        // Assert
        Assert.Contains("Failed to load Kaizen Dashboard", cut.Markup);
        Assert.Contains("Network error", cut.Markup);
    }

    /// <summary>
    /// Verifies the legend displays all four Kakeibo categories.
    /// </summary>
    [Fact]
    public void KaizenDashboard_DisplaysLegend_WithAllCategories()
    {
        // Arrange
        var dashboard = new KaizenDashboardDto
        {
            Weeks = new List<WeeklyKakeiboSummaryDto>
            {
                new()
                {
                    WeekLabel = "Week 1",
                    Essentials = 100m,
                    Wants = 50m,
                    Culture = 30m,
                    Unexpected = 20m,
                },
            },
        };
        _fakeApiService.KaizenDashboardTaskSource = new TaskCompletionSource<KaizenDashboardDto?>();
        _fakeApiService.KaizenDashboardTaskSource.SetResult(dashboard);

        // Act
        var cut = Render<KaizenDashboardView>();
        cut.WaitForState(() => !cut.Markup.Contains("Loading Kaizen dashboard"), TimeSpan.FromSeconds(1));

        // Assert
        Assert.Contains("Essentials", cut.Markup);
        Assert.Contains("Wants", cut.Markup);
        Assert.Contains("Culture", cut.Markup);
        Assert.Contains("Unexpected", cut.Markup);
    }

    /// <summary>
    /// Verifies week labels are displayed in the chart.
    /// </summary>
    [Fact]
    public void KaizenDashboard_DisplaysWeekLabels()
    {
        // Arrange
        var dashboard = new KaizenDashboardDto
        {
            Weeks = new List<WeeklyKakeiboSummaryDto>
            {
                new() { WeekLabel = "Week 1", Essentials = 100m, Wants = 50m, Culture = 30m, Unexpected = 20m },
                new() { WeekLabel = "Week 2", Essentials = 110m, Wants = 55m, Culture = 32m, Unexpected = 22m },
            },
        };
        _fakeApiService.KaizenDashboardTaskSource = new TaskCompletionSource<KaizenDashboardDto?>();
        _fakeApiService.KaizenDashboardTaskSource.SetResult(dashboard);

        // Act
        var cut = Render<KaizenDashboardView>();
        cut.WaitForState(() => cut.Markup.Contains("Week 1"), TimeSpan.FromSeconds(1));

        // Assert
        Assert.Contains("Week 1", cut.Markup);
        Assert.Contains("Week 2", cut.Markup);
    }

    /// <summary>
    /// Verifies the data table displays weekly breakdown with correct amounts.
    /// </summary>
    [Fact]
    public void KaizenDashboard_DisplaysDataTable_WithWeeklyBreakdown()
    {
        // Arrange
        var dashboard = new KaizenDashboardDto
        {
            Weeks = new List<WeeklyKakeiboSummaryDto>
            {
                new()
                {
                    WeekLabel = "Week 1",
                    Essentials = 100m,
                    Wants = 50m,
                    Culture = 30m,
                    Unexpected = 20m,
                },
            },
        };
        _fakeApiService.KaizenDashboardTaskSource = new TaskCompletionSource<KaizenDashboardDto?>();
        _fakeApiService.KaizenDashboardTaskSource.SetResult(dashboard);

        // Act
        var cut = Render<KaizenDashboardView>();
        cut.WaitForState(() => cut.Markup.Contains("Weekly Breakdown"), TimeSpan.FromSeconds(1));

        // Assert
        Assert.Contains("Weekly Breakdown", cut.Markup);
        Assert.Contains("$100.00", cut.Markup); // Essentials
        Assert.Contains("$50.00", cut.Markup); // Wants
        Assert.Contains("$30.00", cut.Markup); // Culture
        Assert.Contains("$20.00", cut.Markup); // Unexpected
    }

    /// <summary>
    /// Verifies the goal badge displays checkmark when goal is achieved.
    /// </summary>
    [Fact]
    public void KaizenDashboard_ShowsGoalAchievedBadge_WhenGoalMet()
    {
        // Arrange
        var dashboard = new KaizenDashboardDto
        {
            Weeks = new List<WeeklyKakeiboSummaryDto>
            {
                new()
                {
                    WeekLabel = "Week 1",
                    Essentials = 100m,
                    Wants = 50m,
                    Culture = 30m,
                    Unexpected = 20m,
                    KaizenGoalDescription = "Save $50",
                    KaizenGoalAchieved = true,
                },
            },
        };
        _fakeApiService.KaizenDashboardTaskSource = new TaskCompletionSource<KaizenDashboardDto?>();
        _fakeApiService.KaizenDashboardTaskSource.SetResult(dashboard);

        // Act
        var cut = Render<KaizenDashboardView>();
        cut.WaitForState(() => cut.Markup.Contains("✓"), TimeSpan.FromSeconds(1));

        // Assert
        Assert.Contains("✓", cut.Markup);
        Assert.Contains("goal-achieved", cut.Markup);
    }

    /// <summary>
    /// Verifies the goal badge displays X mark when goal is not achieved.
    /// </summary>
    [Fact]
    public void KaizenDashboard_ShowsGoalMissedBadge_WhenGoalNotMet()
    {
        // Arrange
        var dashboard = new KaizenDashboardDto
        {
            Weeks = new List<WeeklyKakeiboSummaryDto>
            {
                new()
                {
                    WeekLabel = "Week 1",
                    Essentials = 100m,
                    Wants = 50m,
                    Culture = 30m,
                    Unexpected = 20m,
                    KaizenGoalDescription = "Save $50",
                    KaizenGoalAchieved = false,
                },
            },
        };
        _fakeApiService.KaizenDashboardTaskSource = new TaskCompletionSource<KaizenDashboardDto?>();
        _fakeApiService.KaizenDashboardTaskSource.SetResult(dashboard);

        // Act
        var cut = Render<KaizenDashboardView>();
        cut.WaitForState(() => cut.Markup.Contains("✗"), TimeSpan.FromSeconds(1));

        // Assert
        Assert.Contains("✗", cut.Markup);
        Assert.Contains("goal-missed", cut.Markup);
    }

    /// <summary>
    /// Verifies the goal badge shows empty state when no goal is set for the week.
    /// </summary>
    [Fact]
    public void KaizenDashboard_ShowsEmptyGoalBadge_WhenNoGoal()
    {
        // Arrange
        var dashboard = new KaizenDashboardDto
        {
            Weeks = new List<WeeklyKakeiboSummaryDto>
            {
                new()
                {
                    WeekLabel = "Week 1",
                    Essentials = 100m,
                    Wants = 50m,
                    Culture = 30m,
                    Unexpected = 20m,
                    KaizenGoalDescription = null,
                    KaizenGoalAchieved = null,
                },
            },
        };
        _fakeApiService.KaizenDashboardTaskSource = new TaskCompletionSource<KaizenDashboardDto?>();
        _fakeApiService.KaizenDashboardTaskSource.SetResult(dashboard);

        // Act
        var cut = Render<KaizenDashboardView>();
        cut.WaitForState(() => cut.Markup.Contains("goal-none"), TimeSpan.FromSeconds(1));

        // Assert
        Assert.Contains("goal-none", cut.Markup);
    }

    /// <summary>
    /// Verifies the stacked bar displays zero-spending week correctly.
    /// </summary>
    [Fact]
    public void KaizenDashboard_ShowsEmptyBar_WhenWeekHasZeroSpending()
    {
        // Arrange
        var dashboard = new KaizenDashboardDto
        {
            Weeks = new List<WeeklyKakeiboSummaryDto>
            {
                new()
                {
                    WeekLabel = "Week 1",
                    Essentials = 0m,
                    Wants = 0m,
                    Culture = 0m,
                    Unexpected = 0m,
                },
            },
        };
        _fakeApiService.KaizenDashboardTaskSource = new TaskCompletionSource<KaizenDashboardDto?>();
        _fakeApiService.KaizenDashboardTaskSource.SetResult(dashboard);

        // Act
        var cut = Render<KaizenDashboardView>();
        cut.WaitForState(() => cut.Markup.Contains("bar-empty"), TimeSpan.FromSeconds(1));

        // Assert
        Assert.Contains("bar-empty", cut.Markup);
    }

    /// <summary>
    /// Verifies the back navigation link to reports is present.
    /// </summary>
    [Fact]
    public void KaizenDashboard_RendersBackLink_ToReports()
    {
        // Arrange & Act
        var dashboard = new KaizenDashboardDto { Weeks = new List<WeeklyKakeiboSummaryDto>() };
        _fakeApiService.KaizenDashboardTaskSource = new TaskCompletionSource<KaizenDashboardDto?>();
        _fakeApiService.KaizenDashboardTaskSource.SetResult(dashboard);
        var cut = Render<KaizenDashboardView>();

        // Assert
        Assert.Contains("/reports", cut.Markup);
    }
}
