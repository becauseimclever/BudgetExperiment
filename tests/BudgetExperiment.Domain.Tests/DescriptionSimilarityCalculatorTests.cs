// <copyright file="DescriptionSimilarityCalculatorTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the DescriptionSimilarityCalculator static helper.
/// </summary>
public class DescriptionSimilarityCalculatorTests
{
    [Fact]
    public void CalculateSimilarity_ExactMatch_ReturnsOne()
    {
        var result = DescriptionSimilarityCalculator.CalculateSimilarity("Netflix", "Netflix");

        Assert.Equal(1.0m, result);
    }

    [Fact]
    public void CalculateSimilarity_CaseInsensitiveMatch_ReturnsOne()
    {
        var result = DescriptionSimilarityCalculator.CalculateSimilarity("NETFLIX", "netflix");

        Assert.Equal(1.0m, result);
    }

    [Fact]
    public void CalculateSimilarity_NullOrWhitespaceTransaction_ReturnsZero()
    {
        Assert.Equal(0m, DescriptionSimilarityCalculator.CalculateSimilarity(null!, "Netflix"));
        Assert.Equal(0m, DescriptionSimilarityCalculator.CalculateSimilarity(string.Empty, "Netflix"));
        Assert.Equal(0m, DescriptionSimilarityCalculator.CalculateSimilarity("   ", "Netflix"));
    }

    [Fact]
    public void CalculateSimilarity_NullOrWhitespaceCandidate_ReturnsZero()
    {
        Assert.Equal(0m, DescriptionSimilarityCalculator.CalculateSimilarity("Netflix", null!));
        Assert.Equal(0m, DescriptionSimilarityCalculator.CalculateSimilarity("Netflix", string.Empty));
        Assert.Equal(0m, DescriptionSimilarityCalculator.CalculateSimilarity("Netflix", "   "));
    }

    [Fact]
    public void CalculateSimilarity_ContainmentMatch_ReturnsProportionalScore()
    {
        // "Netflix" is contained in "Netflix Subscription" after normalization
        var result = DescriptionSimilarityCalculator.CalculateSimilarity("Netflix", "Netflix Subscription");

        Assert.True(result > 0m && result < 1.0m, $"Expected proportional score, got {result}");
    }

    [Fact]
    public void CalculateSimilarity_ReverseContainment_ReturnsProportionalScore()
    {
        // Longer string contains shorter
        var result = DescriptionSimilarityCalculator.CalculateSimilarity("Netflix Subscription", "Netflix");

        Assert.True(result > 0m && result < 1.0m, $"Expected proportional score, got {result}");
    }

    [Fact]
    public void CalculateSimilarity_SimilarStrings_ReturnsFuzzyScore()
    {
        // Similar but not exact/contained
        var result = DescriptionSimilarityCalculator.CalculateSimilarity("Netflix Inc", "Netflix LLC");

        Assert.True(result > 0.5m, $"Expected moderate similarity, got {result}");
    }

    [Fact]
    public void CalculateSimilarity_VeryDifferentStrings_ReturnsLowScore()
    {
        var result = DescriptionSimilarityCalculator.CalculateSimilarity("Amazon Marketplace", "Netflix");

        Assert.True(result < 0.5m, $"Expected low similarity, got {result}");
    }

    [Theory]
    [InlineData("Netflix-Sub", "NETFLIX SUB")]
    [InlineData("NETFLIX_SUB", "NETFLIX SUB")]
    [InlineData("*NETFLIX*", "NETFLIX")]
    [InlineData("NET#FLIX", "NETFLIX")]
    public void CalculateSimilarity_PunctuationVariants_NormalizedToMatch(string desc1, string desc2)
    {
        var result = DescriptionSimilarityCalculator.CalculateSimilarity(desc1, desc2);

        Assert.Equal(1.0m, result);
    }

    [Fact]
    public void NormalizeDescription_RemovesDots()
    {
        Assert.Equal("NETFLIXCOM", DescriptionSimilarityCalculator.NormalizeDescription("netflix.com"));
    }

    [Fact]
    public void NormalizeDescription_ReplacesHyphensWithSpaces()
    {
        Assert.Equal("CHICK FIL A", DescriptionSimilarityCalculator.NormalizeDescription("chick-fil-a"));
    }

    [Fact]
    public void NormalizeDescription_CollapsesMultipleSpaces()
    {
        Assert.Equal("HELLO WORLD", DescriptionSimilarityCalculator.NormalizeDescription("hello   world"));
    }

    [Fact]
    public void NormalizeDescription_RemovesAsterisksAndHashes()
    {
        Assert.Equal("PAYMENT", DescriptionSimilarityCalculator.NormalizeDescription("*PAYMENT#"));
    }

    [Fact]
    public void NormalizeDescription_TrimsWhitespace()
    {
        Assert.Equal("TEST", DescriptionSimilarityCalculator.NormalizeDescription("  test  "));
    }
}
