// <copyright file="SuggestionMetricsServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Categorization;
using BudgetExperiment.Domain;
using BudgetExperiment.Domain.Repositories;

using Moq;

namespace BudgetExperiment.Application.Tests.Categorization;

/// <summary>
/// Unit tests for <see cref="SuggestionMetricsService"/>.
/// </summary>
public class SuggestionMetricsServiceTests
{
    private readonly Mock<IRuleSuggestionRepository> _ruleRepoMock;
    private readonly Mock<ICategorySuggestionRepository> _categoryRepoMock;
    private readonly SuggestionMetricsService _service;

    public SuggestionMetricsServiceTests()
    {
        _ruleRepoMock = new Mock<IRuleSuggestionRepository>();
        _categoryRepoMock = new Mock<ICategorySuggestionRepository>();
        _service = new SuggestionMetricsService(_ruleRepoMock.Object, _categoryRepoMock.Object);

        // Default: empty data
        SetupEmptyData();
    }

    [Fact]
    public async Task GetMetricsAsync_With_No_Data_Returns_Zeros()
    {
        // Act
        var result = await _service.GetMetricsAsync();

        // Assert
        Assert.Equal(0, result.TotalGenerated);
        Assert.Equal(0, result.Accepted);
        Assert.Equal(0, result.Dismissed);
        Assert.Equal(0, result.Pending);
        Assert.Null(result.AcceptanceRate);
        Assert.Null(result.AverageAcceptedConfidence);
        Assert.Null(result.AverageDismissedConfidence);
        Assert.Empty(result.ByType);
    }

    [Fact]
    public async Task GetMetricsAsync_Computes_Totals_From_RuleAndCategory_Suggestions()
    {
        // Arrange
        SetupRuleReviewedCounts(new Dictionary<(SuggestionType, SuggestionStatus), int>
        {
            { (SuggestionType.NewRule, SuggestionStatus.Accepted), 8 },
            { (SuggestionType.NewRule, SuggestionStatus.Dismissed), 2 },
        });
        SetupRulePendingCounts(new Dictionary<SuggestionType, int>
        {
            { SuggestionType.NewRule, 5 },
        });
        SetupCategoryCounts(new Dictionary<SuggestionStatus, int>
        {
            { SuggestionStatus.Accepted, 3 },
            { SuggestionStatus.Dismissed, 1 },
            { SuggestionStatus.Pending, 2 },
        });

        // Act
        var result = await _service.GetMetricsAsync();

        // Assert
        Assert.Equal(21, result.TotalGenerated); // 8+2+5 + 3+1+2
        Assert.Equal(11, result.Accepted); // 8+3
        Assert.Equal(3, result.Dismissed); // 2+1
        Assert.Equal(7, result.Pending); // 5+2
    }

    [Fact]
    public async Task GetMetricsAsync_Computes_AcceptanceRate()
    {
        // Arrange
        SetupRuleReviewedCounts(new Dictionary<(SuggestionType, SuggestionStatus), int>
        {
            { (SuggestionType.NewRule, SuggestionStatus.Accepted), 6 },
            { (SuggestionType.NewRule, SuggestionStatus.Dismissed), 4 },
        });

        // Act
        var result = await _service.GetMetricsAsync();

        // Assert: 6 accepted out of 10 reviewed = 0.6
        Assert.NotNull(result.AcceptanceRate);
        Assert.Equal(0.6m, result.AcceptanceRate);
    }

    [Fact]
    public async Task GetMetricsAsync_AcceptanceRate_Null_When_No_Reviewed()
    {
        // Arrange — only pending suggestions
        SetupRulePendingCounts(new Dictionary<SuggestionType, int>
        {
            { SuggestionType.NewRule, 10 },
        });

        // Act
        var result = await _service.GetMetricsAsync();

        // Assert
        Assert.Null(result.AcceptanceRate);
    }

    [Fact]
    public async Task GetMetricsAsync_Includes_ByType_Breakdown()
    {
        // Arrange
        SetupRuleReviewedCounts(new Dictionary<(SuggestionType, SuggestionStatus), int>
        {
            { (SuggestionType.NewRule, SuggestionStatus.Accepted), 10 },
            { (SuggestionType.NewRule, SuggestionStatus.Dismissed), 5 },
            { (SuggestionType.PatternOptimization, SuggestionStatus.Accepted), 3 },
            { (SuggestionType.PatternOptimization, SuggestionStatus.Dismissed), 1 },
        });
        SetupRulePendingCounts(new Dictionary<SuggestionType, int>
        {
            { SuggestionType.NewRule, 2 },
        });

        // Act
        var result = await _service.GetMetricsAsync();

        // Assert
        Assert.Equal(2, result.ByType.Count);

        var newRule = result.ByType.Single(t => t.Type == "NewRule");
        Assert.Equal(10, newRule.Accepted);
        Assert.Equal(5, newRule.Dismissed);
        Assert.Equal(2, newRule.Pending);

        var optimization = result.ByType.Single(t => t.Type == "PatternOptimization");
        Assert.Equal(3, optimization.Accepted);
        Assert.Equal(1, optimization.Dismissed);
        Assert.Equal(0, optimization.Pending);
    }

    [Fact]
    public async Task GetMetricsAsync_Includes_Category_Type_In_ByType()
    {
        // Arrange
        SetupCategoryCounts(new Dictionary<SuggestionStatus, int>
        {
            { SuggestionStatus.Accepted, 5 },
            { SuggestionStatus.Dismissed, 2 },
            { SuggestionStatus.Pending, 1 },
        });

        // Act
        var result = await _service.GetMetricsAsync();

        // Assert
        var categoryType = Assert.Single(result.ByType);
        Assert.Equal("Category", categoryType.Type);
        Assert.Equal(5, categoryType.Accepted);
        Assert.Equal(2, categoryType.Dismissed);
        Assert.Equal(1, categoryType.Pending);
    }

    [Fact]
    public async Task GetMetricsAsync_Computes_PerType_AcceptanceRate()
    {
        // Arrange
        SetupRuleReviewedCounts(new Dictionary<(SuggestionType, SuggestionStatus), int>
        {
            { (SuggestionType.NewRule, SuggestionStatus.Accepted), 3 },
            { (SuggestionType.NewRule, SuggestionStatus.Dismissed), 7 },
        });

        // Act
        var result = await _service.GetMetricsAsync();

        // Assert: 3 / 10 = 0.3
        var newRule = Assert.Single(result.ByType);
        Assert.Equal(0.3m, newRule.AcceptanceRate);
    }

    [Fact]
    public async Task GetMetricsAsync_PerType_AcceptanceRate_Null_When_Only_Pending()
    {
        // Arrange
        SetupRulePendingCounts(new Dictionary<SuggestionType, int>
        {
            { SuggestionType.NewRule, 5 },
        });

        // Act
        var result = await _service.GetMetricsAsync();

        // Assert
        var newRule = Assert.Single(result.ByType);
        Assert.Null(newRule.AcceptanceRate);
    }

    [Fact]
    public async Task GetMetricsAsync_Returns_Confidence_Averages()
    {
        // Arrange
        SetupRuleReviewedCounts(new Dictionary<(SuggestionType, SuggestionStatus), int>
        {
            { (SuggestionType.NewRule, SuggestionStatus.Accepted), 5 },
            { (SuggestionType.NewRule, SuggestionStatus.Dismissed), 3 },
        });
        _ruleRepoMock
            .Setup(r => r.GetAverageConfidenceByStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((0.85m, 0.45m));

        // Act
        var result = await _service.GetMetricsAsync();

        // Assert
        Assert.Equal(0.85m, result.AverageAcceptedConfidence);
        Assert.Equal(0.45m, result.AverageDismissedConfidence);
    }

    [Fact]
    public async Task GetMetricsAsync_Computes_WeightedAverage_Confidence_Across_RuleAndCategory()
    {
        // Arrange
        SetupRuleReviewedCounts(new Dictionary<(SuggestionType, SuggestionStatus), int>
        {
            { (SuggestionType.NewRule, SuggestionStatus.Accepted), 8 },
            { (SuggestionType.NewRule, SuggestionStatus.Dismissed), 4 },
        });
        _ruleRepoMock
            .Setup(r => r.GetAverageConfidenceByStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((0.90m, 0.40m));

        SetupCategoryCounts(new Dictionary<SuggestionStatus, int>
        {
            { SuggestionStatus.Accepted, 2 },
            { SuggestionStatus.Dismissed, 1 },
        });
        _categoryRepoMock
            .Setup(r => r.GetAverageConfidenceByStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((0.70m, 0.30m));

        // Act
        var result = await _service.GetMetricsAsync();

        // Assert: weighted average for accepted = (0.90*8 + 0.70*2) / 10 = (7.2 + 1.4) / 10 = 0.86
        Assert.Equal(0.86m, result.AverageAcceptedConfidence);

        // Assert: weighted average for dismissed = (0.40*4 + 0.30*1) / 5 = (1.6 + 0.3) / 5 = 0.38
        Assert.Equal(0.38m, result.AverageDismissedConfidence);
    }

    [Fact]
    public async Task GetMetricsAsync_Confidence_Null_When_No_Reviewed()
    {
        // Arrange — only pending
        SetupRulePendingCounts(new Dictionary<SuggestionType, int>
        {
            { SuggestionType.NewRule, 3 },
        });

        // Act
        var result = await _service.GetMetricsAsync();

        // Assert
        Assert.Null(result.AverageAcceptedConfidence);
        Assert.Null(result.AverageDismissedConfidence);
    }

    [Fact]
    public async Task GetMetricsAsync_Skips_Types_With_Zero_Activity()
    {
        // Arrange: only NewRule has data
        SetupRuleReviewedCounts(new Dictionary<(SuggestionType, SuggestionStatus), int>
        {
            { (SuggestionType.NewRule, SuggestionStatus.Accepted), 1 },
        });

        // Act
        var result = await _service.GetMetricsAsync();

        // Assert: only NewRule type present, not PatternOptimization etc.
        var single = Assert.Single(result.ByType);
        Assert.Equal("NewRule", single.Type);
    }

    [Fact]
    public async Task GetMetricsAsync_HandlesMixedRuleTypes()
    {
        // Arrange
        SetupRuleReviewedCounts(new Dictionary<(SuggestionType, SuggestionStatus), int>
        {
            { (SuggestionType.NewRule, SuggestionStatus.Accepted), 5 },
            { (SuggestionType.RuleConflict, SuggestionStatus.Dismissed), 2 },
            { (SuggestionType.UnusedRule, SuggestionStatus.Accepted), 1 },
        });
        SetupRulePendingCounts(new Dictionary<SuggestionType, int>
        {
            { SuggestionType.RuleConsolidation, 3 },
        });

        // Act
        var result = await _service.GetMetricsAsync();

        // Assert
        Assert.Equal(11, result.TotalGenerated);
        Assert.Equal(4, result.ByType.Count);
        Assert.Contains(result.ByType, t => t.Type == "NewRule");
        Assert.Contains(result.ByType, t => t.Type == "RuleConflict");
        Assert.Contains(result.ByType, t => t.Type == "UnusedRule");
        Assert.Contains(result.ByType, t => t.Type == "RuleConsolidation");
    }

    private void SetupEmptyData()
    {
        _ruleRepoMock
            .Setup(r => r.GetReviewedCountsByTypeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<(SuggestionType, SuggestionStatus), int>());
        _ruleRepoMock
            .Setup(r => r.GetPendingCountsByTypeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<SuggestionType, int>());
        _ruleRepoMock
            .Setup(r => r.GetAverageConfidenceByStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((AcceptedAvgConfidence: (decimal?)null, DismissedAvgConfidence: (decimal?)null));
        _categoryRepoMock
            .Setup(r => r.GetCountsByStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<SuggestionStatus, int>());
        _categoryRepoMock
            .Setup(r => r.GetAverageConfidenceByStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((AcceptedAvgConfidence: (decimal?)null, DismissedAvgConfidence: (decimal?)null));
    }

    private void SetupRuleReviewedCounts(Dictionary<(SuggestionType Type, SuggestionStatus Status), int> counts)
    {
        _ruleRepoMock
            .Setup(r => r.GetReviewedCountsByTypeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(counts);
    }

    private void SetupRulePendingCounts(Dictionary<SuggestionType, int> counts)
    {
        _ruleRepoMock
            .Setup(r => r.GetPendingCountsByTypeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(counts);
    }

    private void SetupCategoryCounts(Dictionary<SuggestionStatus, int> counts)
    {
        _categoryRepoMock
            .Setup(r => r.GetCountsByStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(counts);
    }
}
