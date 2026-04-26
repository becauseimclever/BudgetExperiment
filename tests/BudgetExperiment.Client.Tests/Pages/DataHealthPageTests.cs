// <copyright file="DataHealthPageTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;

using BudgetExperiment.Client.Pages.DataHealth;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Client.Tests.TestHelpers;
using BudgetExperiment.Contracts.Dtos;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace BudgetExperiment.Client.Tests.Pages;

/// <summary>
/// Unit tests for the <see cref="DataHealth"/> page component.
/// </summary>
public class DataHealthPageTests : BunitContext, IAsyncLifetime
{
    private readonly StubBudgetApiService _apiService = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DataHealthPageTests"/> class.
    /// </summary>
    public DataHealthPageTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<IBudgetApiService>(_apiService);
        this.Services.AddSingleton<ThemeService>();
        this.Services.AddSingleton<CultureService>();
        this.Services.AddSingleton<IFeatureFlagClientService>(new StubFeatureFlagClientService());
        this.Services.AddSingleton<IApiErrorContext>(new ApiErrorContext());
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
    /// Verifies the page renders without error when no data health issues exist.
    /// </summary>
    [Fact]
    public void Renders_WhenNoDataHealthIssues()
    {
        // Arrange
        _apiService.DataHealthReport = new DataHealthReportDto
        {
            Duplicates = new List<DuplicateClusterDto>(),
            Outliers = new List<AmountOutlierDto>(),
            DateGaps = new List<DateGapDto>(),
            Uncategorized = new UncategorizedSummaryDto { TotalCount = 0, },
        };

        // Act
        var cut = this.Render<DataHealth>();

        // Assert
        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the page renders the Refresh button.
    /// </summary>
    [Fact]
    public void ShowsRefreshButton()
    {
        // Arrange
        _apiService.DataHealthReport = new DataHealthReportDto
        {
            Duplicates = new List<DuplicateClusterDto>(),
            Outliers = new List<AmountOutlierDto>(),
            DateGaps = new List<DateGapDto>(),
            Uncategorized = new UncategorizedSummaryDto { TotalCount = 0, },
        };

        // Act
        var cut = this.Render<DataHealth>();

        // Assert
        cut.Markup.ShouldContain("Refresh");
    }

    /// <summary>
    /// Verifies the page shows all four stat cards (Duplicates, Outliers, DateGaps, Uncategorized).
    /// </summary>
    [Fact]
    public void ShowsAllStatCards()
    {
        // Arrange
        _apiService.DataHealthReport = new DataHealthReportDto
        {
            Duplicates = new List<DuplicateClusterDto>(),
            Outliers = new List<AmountOutlierDto>(),
            DateGaps = new List<DateGapDto>(),
            Uncategorized = new UncategorizedSummaryDto { TotalCount = 0, },
        };

        // Act
        var cut = this.Render<DataHealth>();

        // Assert
        cut.Markup.ShouldContain("Duplicates");
        cut.Markup.ShouldContain("Outliers");
        cut.Markup.ShouldContain("Date Gaps");
        cut.Markup.ShouldContain("Uncategorized");
    }

    /// <summary>
    /// Verifies duplicate count is displayed correctly in stat card.
    /// </summary>
    [Fact]
    public void DisplaysDuplicateCount()
    {
        // Arrange
        var transaction = new TransactionDto
        {
            Id = Guid.NewGuid(),
            Description = "Test",
            Amount = new MoneyDto { Amount = 100m, Currency = "USD" },
            Date = DateOnly.FromDateTime(DateTime.Today),
        };
        _apiService.DataHealthReport = new DataHealthReportDto
        {
            Duplicates = new List<DuplicateClusterDto>
            {
                new()
                {
                    GroupKey = "test-key",
                    Transactions = new List<TransactionDto> { transaction },
                },
            },
            Outliers = new List<AmountOutlierDto>(),
            DateGaps = new List<DateGapDto>(),
            Uncategorized = new UncategorizedSummaryDto { TotalCount = 0, },
        };

        // Act
        var cut = this.Render<DataHealth>();

        // Assert
        cut.Markup.ShouldContain(">1<");
    }

    /// <summary>
    /// Verifies outlier count is displayed correctly in stat card.
    /// </summary>
    [Fact]
    public void DisplaysOutlierCount()
    {
        // Arrange
        var transaction = new TransactionDto
        {
            Id = Guid.NewGuid(),
            Description = "High",
            Amount = new MoneyDto { Amount = 5000m, Currency = "USD" },
            Date = DateOnly.FromDateTime(DateTime.Today),
        };
        _apiService.DataHealthReport = new DataHealthReportDto
        {
            Duplicates = new List<DuplicateClusterDto>(),
            Outliers = new List<AmountOutlierDto>
            {
                new()
                {
                    Transaction = transaction,
                    HistoricalMean = 100m,
                    DeviationFactor = 5m,
                    MerchantGroup = "merchant",
                },
            },
            DateGaps = new List<DateGapDto>(),
            Uncategorized = new UncategorizedSummaryDto { TotalCount = 0, },
        };

        // Act
        var cut = this.Render<DataHealth>();

        // Assert
        cut.Markup.ShouldContain(">1<");
    }

    /// <summary>
    /// Verifies uncategorized count is displayed correctly in stat card.
    /// </summary>
    [Fact]
    public void DisplaysUncategorizedCount()
    {
        // Arrange
        _apiService.DataHealthReport = new DataHealthReportDto
        {
            Duplicates = new List<DuplicateClusterDto>(),
            Outliers = new List<AmountOutlierDto>(),
            DateGaps = new List<DateGapDto>(),
            Uncategorized = new UncategorizedSummaryDto { TotalCount = 5, },
        };

        // Act
        var cut = this.Render<DataHealth>();

        // Assert
        cut.Markup.ShouldContain(">5<");
    }

    /// <summary>
    /// Verifies the page displays the success message when all health checks pass.
    /// </summary>
    [Fact]
    public void ShowsSuccessMessage_WhenAllHealthChecksPassed()
    {
        // Arrange
        _apiService.DataHealthReport = new DataHealthReportDto
        {
            Duplicates = new List<DuplicateClusterDto>(),
            Outliers = new List<AmountOutlierDto>(),
            DateGaps = new List<DateGapDto>(),
            Uncategorized = new UncategorizedSummaryDto { TotalCount = 0, },
        };

        // Act
        var cut = this.Render<DataHealth>();

        // Assert
        cut.Markup.ShouldContain("All data health checks passed");
    }

    /// <summary>
    /// Verifies the Duplicate Transactions section is hidden when no duplicates exist.
    /// </summary>
    [Fact]
    public void HidesDuplicateSection_WhenNoDuplicates()
    {
        // Arrange
        _apiService.DataHealthReport = new DataHealthReportDto
        {
            Duplicates = new List<DuplicateClusterDto>(),
            Outliers = new List<AmountOutlierDto>(),
            DateGaps = new List<DateGapDto>(),
            Uncategorized = new UncategorizedSummaryDto { TotalCount = 0, },
        };

        // Act
        var cut = this.Render<DataHealth>();

        // Assert
        // The section with h2.section-title containing "Duplicate Transactions" should not exist
        cut.FindAll("h2.section-title").Any(h => h.TextContent.Contains("Duplicate Transactions")).ShouldBeFalse();
    }

    /// <summary>
    /// Verifies the Amount Outliers section is hidden when no outliers exist.
    /// </summary>
    [Fact]
    public void HidesOutlierSection_WhenNoOutliers()
    {
        // Arrange
        _apiService.DataHealthReport = new DataHealthReportDto
        {
            Duplicates = new List<DuplicateClusterDto>(),
            Outliers = new List<AmountOutlierDto>(),
            DateGaps = new List<DateGapDto>(),
            Uncategorized = new UncategorizedSummaryDto { TotalCount = 0, },
        };

        // Act
        var cut = this.Render<DataHealth>();

        // Assert
        cut.Markup.ShouldNotContain("Amount Outliers");
    }

    /// <summary>
    /// Verifies the Date Gaps section is hidden when no date gaps exist.
    /// </summary>
    [Fact]
    public void HidesDateGapSection_WhenNoDateGaps()
    {
        // Arrange
        _apiService.DataHealthReport = new DataHealthReportDto
        {
            Duplicates = new List<DuplicateClusterDto>(),
            Outliers = new List<AmountOutlierDto>(),
            DateGaps = new List<DateGapDto>(),
            Uncategorized = new UncategorizedSummaryDto { TotalCount = 0, },
        };

        // Act
        var cut = this.Render<DataHealth>();

        // Assert
        // The section with h2.section-title containing "Date Gaps" should not exist
        cut.FindAll("h2.section-title").Any(h => h.TextContent.Contains("Date Gaps")).ShouldBeFalse();
    }

    /// <summary>
    /// Verifies the Uncategorized Transactions section is always shown.
    /// </summary>
    [Fact]
    public void ShowsUncategorizedSection_Always()
    {
        // Arrange
        _apiService.DataHealthReport = new DataHealthReportDto
        {
            Duplicates = new List<DuplicateClusterDto>(),
            Outliers = new List<AmountOutlierDto>(),
            DateGaps = new List<DateGapDto>(),
            Uncategorized = new UncategorizedSummaryDto { TotalCount = 0, },
        };

        // Act
        var cut = this.Render<DataHealth>();

        // Assert
        cut.Markup.ShouldContain("Uncategorized Transactions");
    }

    /// <summary>
    /// Verifies the page displays error message when API call fails.
    /// </summary>
    [Fact]
    public void ShowsErrorMessage_WhenApiCallFails()
    {
        // Arrange
        _apiService.DataHealthReportException = new Exception("Network error");

        // Act
        var cut = this.Render<DataHealth>();

        // Assert
        cut.Markup.ShouldContain("Network error");
    }

    /// <summary>
    /// Verifies the page displays loading spinner while data is being loaded.
    /// </summary>
    [Fact]
    public void ShowsLoadingSpinner_DuringDataLoad()
    {
        // Arrange
        var loadingTcs = new TaskCompletionSource<DataHealthReportDto?>();
        _apiService.GetDataHealthReportAsyncOverride = _ => loadingTcs.Task;

        // Act
        var cut = this.Render<DataHealth>();

        // Assert
        cut.Markup.ShouldContain("loading-spinner");

        // Cleanup
        loadingTcs.SetResult(new DataHealthReportDto
        {
            Duplicates = new List<DuplicateClusterDto>(),
            Outliers = new List<AmountOutlierDto>(),
            DateGaps = new List<DateGapDto>(),
            Uncategorized = new UncategorizedSummaryDto { TotalCount = 0, },
        });
    }

    /// <summary>
    /// Verifies the Refresh button is disabled while loading.
    /// </summary>
    [Fact]
    public void RefreshButton_IsDisabledDuringLoad()
    {
        // Arrange
        var loadingTcs = new TaskCompletionSource<DataHealthReportDto?>();
        _apiService.GetDataHealthReportAsyncOverride = _ => loadingTcs.Task;

        // Act
        var cut = this.Render<DataHealth>();

        // Assert
        var button = cut.Find("button.btn-secondary");
        button.HasAttribute("disabled").ShouldBeTrue();

        // Cleanup
        loadingTcs.SetResult(new DataHealthReportDto
        {
            Duplicates = new List<DuplicateClusterDto>(),
            Outliers = new List<AmountOutlierDto>(),
            DateGaps = new List<DateGapDto>(),
            Uncategorized = new UncategorizedSummaryDto { TotalCount = 0, },
        });
    }

    /// <summary>
    /// Verifies the page page header displays the correct title.
    /// </summary>
    [Fact]
    public void ShowsPageTitle()
    {
        // Arrange
        _apiService.DataHealthReport = new DataHealthReportDto
        {
            Duplicates = new List<DuplicateClusterDto>(),
            Outliers = new List<AmountOutlierDto>(),
            DateGaps = new List<DateGapDto>(),
            Uncategorized = new UncategorizedSummaryDto { TotalCount = 0, },
        };

        // Act
        var cut = this.Render<DataHealth>();

        // Assert
        cut.Markup.ShouldContain("Data Health");
    }

    /// <summary>
    /// Verifies stat card colors change based on count (warning when count > 0, success when 0).
    /// </summary>
    [Fact]
    public void StatCards_ShowWarningColorWhenCountGreaterThanZero()
    {
        // Arrange
        var transaction = new TransactionDto
        {
            Id = Guid.NewGuid(),
            Description = "Test",
            Amount = new MoneyDto { Amount = 100m, Currency = "USD" },
            Date = DateOnly.FromDateTime(DateTime.Today),
        };
        _apiService.DataHealthReport = new DataHealthReportDto
        {
            Duplicates = new List<DuplicateClusterDto>
            {
                new()
                {
                    GroupKey = "test-key",
                    Transactions = new List<TransactionDto> { transaction },
                },
            },
            Outliers = new List<AmountOutlierDto>(),
            DateGaps = new List<DateGapDto>(),
            Uncategorized = new UncategorizedSummaryDto { TotalCount = 0, },
        };

        // Act
        var cut = this.Render<DataHealth>();

        // Assert
        cut.Markup.ShouldContain("text-warning");
    }

    /// <summary>
    /// Verifies stat card colors show success when count is zero.
    /// </summary>
    [Fact]
    public void StatCards_ShowSuccessColorWhenCountIsZero()
    {
        // Arrange
        _apiService.DataHealthReport = new DataHealthReportDto
        {
            Duplicates = new List<DuplicateClusterDto>(),
            Outliers = new List<AmountOutlierDto>(),
            DateGaps = new List<DateGapDto>(),
            Uncategorized = new UncategorizedSummaryDto { TotalCount = 0, },
        };

        // Act
        var cut = this.Render<DataHealth>();

        // Assert
        cut.Markup.ShouldContain("text-success");
    }
}
