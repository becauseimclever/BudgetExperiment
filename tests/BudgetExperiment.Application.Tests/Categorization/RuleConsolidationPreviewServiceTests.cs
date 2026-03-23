// <copyright file="RuleConsolidationPreviewServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Shouldly;

namespace BudgetExperiment.Application.Tests.Categorization;

/// <summary>
/// Unit tests for <see cref="RuleConsolidationPreviewService"/>.
/// Feature 116 Slice 3: Coverage validation and preview.
/// These tests are RED — <see cref="RuleConsolidationPreviewService"/> does not yet exist.
/// Lucius must implement the class to make them green.
/// </summary>
public class RuleConsolidationPreviewServiceTests
{
    private static readonly Guid GroceryCategoryId = Guid.NewGuid();
    private static readonly Guid RuleId1 = Guid.NewGuid();
    private static readonly Guid RuleId2 = Guid.NewGuid();

    // -------------------------------------------------------------------------
    // 1. Contains — partial matching
    // -------------------------------------------------------------------------
    [Fact]
    public async Task PreviewAsync_ContainsPattern_ReturnsCorrectCoverage()
    {
        // Arrange
        var suggestion = BuildSuggestion(RuleMatchType.Contains, "WALMART");
        var samples = new List<string>
        {
            "WALMART SUPERCENTER",
            "KROGER GROCERY",
            "WALMART.COM",
        };

        var service = new RuleConsolidationPreviewService();

        // Act
        var result = await service.PreviewConsolidationAsync(suggestion, samples);

        // Assert
        result.TotalSamples.ShouldBe(3);
        result.MatchedSamples.ShouldBe(2);
        result.CoveragePercentage.ShouldBe(200.0 / 3.0, tolerance: 0.01);
        result.MatchedDescriptions.ShouldContain("WALMART SUPERCENTER");
        result.MatchedDescriptions.ShouldContain("WALMART.COM");
    }

    // -------------------------------------------------------------------------
    // 2. Regex — alternation pattern
    // -------------------------------------------------------------------------
    [Fact]
    public async Task PreviewAsync_RegexPattern_ReturnsCorrectCoverage()
    {
        // Arrange
        var suggestion = BuildSuggestion(RuleMatchType.Regex, "WALMART|KROGER");
        var samples = new List<string>
        {
            "WALMART SUPERCENTER",
            "KROGER GROCERY",
            "WHOLE FOODS",
        };

        var service = new RuleConsolidationPreviewService();

        // Act
        var result = await service.PreviewConsolidationAsync(suggestion, samples);

        // Assert
        result.TotalSamples.ShouldBe(3);
        result.MatchedSamples.ShouldBe(2);
        result.UnmatchedDescriptions.ShouldContain("WHOLE FOODS");
    }

    // -------------------------------------------------------------------------
    // 3. Exact — only exact match is counted
    // -------------------------------------------------------------------------
    [Fact]
    public async Task PreviewAsync_ExactPattern_OnlyExactMatchCounted()
    {
        // Arrange
        var suggestion = BuildSuggestion(RuleMatchType.Exact, "WALMART");
        var samples = new List<string>
        {
            "WALMART",
            "WALMART SUPERCENTER",
        };

        var service = new RuleConsolidationPreviewService();

        // Act
        var result = await service.PreviewConsolidationAsync(suggestion, samples);

        // Assert
        result.MatchedSamples.ShouldBe(1);
        result.MatchedDescriptions.ShouldContain("WALMART");
        result.UnmatchedDescriptions.ShouldContain("WALMART SUPERCENTER");
    }

    // -------------------------------------------------------------------------
    // 4. StartsWith
    // -------------------------------------------------------------------------
    [Fact]
    public async Task PreviewAsync_StartsWithPattern_CorrectMatches()
    {
        // Arrange
        var suggestion = BuildSuggestion(RuleMatchType.StartsWith, "WALMART");
        var samples = new List<string>
        {
            "WALMART SUPERCENTER",
            "BEST WALMART",
            "WALMART",
        };

        var service = new RuleConsolidationPreviewService();

        // Act
        var result = await service.PreviewConsolidationAsync(suggestion, samples);

        // Assert
        result.MatchedSamples.ShouldBe(2);
        result.MatchedDescriptions.ShouldContain("WALMART SUPERCENTER");
        result.MatchedDescriptions.ShouldContain("WALMART");
        result.UnmatchedDescriptions.ShouldContain("BEST WALMART");
    }

    // -------------------------------------------------------------------------
    // 5. EndsWith
    // -------------------------------------------------------------------------
    [Fact]
    public async Task PreviewAsync_EndsWithPattern_CorrectMatches()
    {
        // Arrange
        var suggestion = BuildSuggestion(RuleMatchType.EndsWith, "WALMART");
        var samples = new List<string>
        {
            "BEST WALMART",
            "WALMART SUPERCENTER",
            "WALMART",
        };

        var service = new RuleConsolidationPreviewService();

        // Act
        var result = await service.PreviewConsolidationAsync(suggestion, samples);

        // Assert
        result.MatchedSamples.ShouldBe(2);
        result.MatchedDescriptions.ShouldContain("BEST WALMART");
        result.MatchedDescriptions.ShouldContain("WALMART");
        result.UnmatchedDescriptions.ShouldContain("WALMART SUPERCENTER");
    }

    // -------------------------------------------------------------------------
    // 6. Empty sample list
    // -------------------------------------------------------------------------
    [Fact]
    public async Task PreviewAsync_EmptySamples_ReturnsZeroCoverage()
    {
        // Arrange
        var suggestion = BuildSuggestion(RuleMatchType.Contains, "WALMART");
        var samples = Array.Empty<string>();

        var service = new RuleConsolidationPreviewService();

        // Act
        var result = await service.PreviewConsolidationAsync(suggestion, samples);

        // Assert
        result.TotalSamples.ShouldBe(0);
        result.MatchedSamples.ShouldBe(0);
        result.CoveragePercentage.ShouldBe(0.0);
    }

    // -------------------------------------------------------------------------
    // 7. No matches
    // -------------------------------------------------------------------------
    [Fact]
    public async Task PreviewAsync_NoMatches_ReturnsZeroCoverage()
    {
        // Arrange
        var suggestion = BuildSuggestion(RuleMatchType.Contains, "WALMART");
        var samples = new List<string>
        {
            "KROGER",
            "SAFEWAY",
        };

        var service = new RuleConsolidationPreviewService();

        // Act
        var result = await service.PreviewConsolidationAsync(suggestion, samples);

        // Assert
        result.MatchedSamples.ShouldBe(0);
        result.CoveragePercentage.ShouldBe(0.0);
        result.UnmatchedDescriptions.Count.ShouldBe(2);
    }

    // -------------------------------------------------------------------------
    // 8. Case-insensitive matching
    // -------------------------------------------------------------------------
    [Fact]
    public async Task PreviewAsync_CaseInsensitive_MatchesRegardlessOfCase()
    {
        // Arrange
        var suggestion = BuildSuggestion(RuleMatchType.Contains, "walmart");
        var samples = new List<string>
        {
            "WALMART SUPERCENTER",
        };

        var service = new RuleConsolidationPreviewService();

        // Act
        var result = await service.PreviewConsolidationAsync(suggestion, samples);

        // Assert
        result.MatchedSamples.ShouldBe(1);
    }

    // -------------------------------------------------------------------------
    // 9. Coverage percentage calculation
    // -------------------------------------------------------------------------
    [Fact]
    public async Task PreviewAsync_CoveragePercentage_CalculatedCorrectly()
    {
        // Arrange — 4 samples, 3 of which contain "WALMART"
        var suggestion = BuildSuggestion(RuleMatchType.Contains, "WALMART");
        var samples = new List<string>
        {
            "WALMART SUPERCENTER",
            "KROGER GROCERY",
            "WALMART.COM",
            "WALMART NEIGHBORHOOD MARKET",
        };

        var service = new RuleConsolidationPreviewService();

        // Act
        var result = await service.PreviewConsolidationAsync(suggestion, samples);

        // Assert
        result.TotalSamples.ShouldBe(4);
        result.MatchedSamples.ShouldBe(3);
        result.CoveragePercentage.ShouldBe(75.0, tolerance: 0.001);
    }

    // -------------------------------------------------------------------------
    // Helper — build a minimal ConsolidationSuggestion for a given match type
    // -------------------------------------------------------------------------
    private static ConsolidationSuggestion BuildSuggestion(RuleMatchType matchType, string pattern)
    {
        return new ConsolidationSuggestion
        {
            SourceRuleIds = new List<Guid> { RuleId1, RuleId2 },
            MergedPattern = pattern,
            MergedMatchType = matchType,
            Confidence = 1.0,
        };
    }
}
