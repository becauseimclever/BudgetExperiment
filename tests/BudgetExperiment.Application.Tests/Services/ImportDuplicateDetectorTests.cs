// <copyright file="ImportDuplicateDetectorTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Import;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ImportDuplicateDetector"/>.
/// </summary>
public class ImportDuplicateDetectorTests
{
    private readonly ImportDuplicateDetector _detector = new();

    [Fact]
    public void FindDuplicate_WithExactMatch_ReturnsDuplicateId()
    {
        // Arrange
        var existing = CreateTransaction(new DateOnly(2026, 1, 15), -50.00m, "AMAZON PURCHASE");
        var settings = DefaultSettings();

        // Act
        var result = _detector.FindDuplicate(
            new DateOnly(2026, 1, 15),
            -50.00m,
            "AMAZON PURCHASE",
            [existing],
            settings);

        // Assert
        Assert.Equal(existing.Id, result);
    }

    [Fact]
    public void FindDuplicate_WithNoMatch_ReturnsNull()
    {
        // Arrange
        var existing = CreateTransaction(new DateOnly(2026, 1, 15), -50.00m, "AMAZON PURCHASE");
        var settings = DefaultSettings();

        // Act
        var result = _detector.FindDuplicate(
            new DateOnly(2026, 1, 15),
            -75.00m,
            "WALMART",
            [existing],
            settings);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FindDuplicate_WithDifferentAmount_ReturnsNull()
    {
        // Arrange
        var existing = CreateTransaction(new DateOnly(2026, 1, 15), -50.00m, "AMAZON PURCHASE");
        var settings = DefaultSettings();

        // Act
        var result = _detector.FindDuplicate(
            new DateOnly(2026, 1, 15),
            -50.01m,
            "AMAZON PURCHASE",
            [existing],
            settings);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FindDuplicate_WithDateOutsideLookback_ReturnsNull()
    {
        // Arrange
        var existing = CreateTransaction(new DateOnly(2026, 1, 15), -50.00m, "AMAZON PURCHASE");
        var settings = DefaultSettings();

        // Act
        var result = _detector.FindDuplicate(
            new DateOnly(2026, 1, 20), // 5 days away, lookback is 3
            -50.00m,
            "AMAZON PURCHASE",
            [existing],
            settings);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FindDuplicate_WithDateWithinLookback_ReturnsDuplicateId()
    {
        // Arrange
        var existing = CreateTransaction(new DateOnly(2026, 1, 15), -50.00m, "AMAZON PURCHASE");
        var settings = DefaultSettings();

        // Act
        var result = _detector.FindDuplicate(
            new DateOnly(2026, 1, 17), // 2 days away, within 3-day lookback
            -50.00m,
            "AMAZON PURCHASE",
            [existing],
            settings);

        // Assert
        Assert.Equal(existing.Id, result);
    }

    [Fact]
    public void FindDuplicate_WithContainsMode_MatchesSubstring()
    {
        // Arrange
        var existing = CreateTransaction(new DateOnly(2026, 1, 15), -50.00m, "AMAZON PURCHASE #12345");
        var settings = DefaultSettings(DescriptionMatchMode.Contains);

        // Act
        var result = _detector.FindDuplicate(
            new DateOnly(2026, 1, 15),
            -50.00m,
            "AMAZON PURCHASE",
            [existing],
            settings);

        // Assert
        Assert.Equal(existing.Id, result);
    }

    [Fact]
    public void FindDuplicate_WithStartsWithMode_MatchesPrefix()
    {
        // Arrange
        var existing = CreateTransaction(new DateOnly(2026, 1, 15), -50.00m, "AMAZON PURCHASE #12345");
        var settings = DefaultSettings(DescriptionMatchMode.StartsWith);

        // Act
        var result = _detector.FindDuplicate(
            new DateOnly(2026, 1, 15),
            -50.00m,
            "AMAZON PURCHASE",
            [existing],
            settings);

        // Assert
        Assert.Equal(existing.Id, result);
    }

    [Fact]
    public void FindDuplicate_WithFuzzyMode_MatchesSimilar()
    {
        // Arrange
        var existing = CreateTransaction(new DateOnly(2026, 1, 15), -50.00m, "AMAZON PURCHASE");
        var settings = DefaultSettings(DescriptionMatchMode.Fuzzy);

        // Act — slight typo, still >80% similar
        var result = _detector.FindDuplicate(
            new DateOnly(2026, 1, 15),
            -50.00m,
            "AMAZON PURCHAS",
            [existing],
            settings);

        // Assert
        Assert.Equal(existing.Id, result);
    }

    [Fact]
    public void FindDuplicate_WithFuzzyMode_RejectsVeryDifferent()
    {
        // Arrange
        var existing = CreateTransaction(new DateOnly(2026, 1, 15), -50.00m, "AMAZON PURCHASE");
        var settings = DefaultSettings(DescriptionMatchMode.Fuzzy);

        // Act — completely different description
        var result = _detector.FindDuplicate(
            new DateOnly(2026, 1, 15),
            -50.00m,
            "WALMART STORE",
            [existing],
            settings);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FindDuplicate_CaseInsensitive_MatchesRegardlessOfCase()
    {
        // Arrange
        var existing = CreateTransaction(new DateOnly(2026, 1, 15), -50.00m, "Amazon Purchase");
        var settings = DefaultSettings();

        // Act
        var result = _detector.FindDuplicate(
            new DateOnly(2026, 1, 15),
            -50.00m,
            "AMAZON PURCHASE",
            [existing],
            settings);

        // Assert
        Assert.Equal(existing.Id, result);
    }

    [Fact]
    public void FindDuplicate_WithEmptyTransactions_ReturnsNull()
    {
        // Arrange
        var settings = DefaultSettings();

        // Act
        var result = _detector.FindDuplicate(
            new DateOnly(2026, 1, 15),
            -50.00m,
            "AMAZON PURCHASE",
            [],
            settings);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FindDuplicate_ReturnsFirstMatch()
    {
        // Arrange
        var existing1 = CreateTransaction(new DateOnly(2026, 1, 15), -50.00m, "AMAZON PURCHASE");
        var existing2 = CreateTransaction(new DateOnly(2026, 1, 15), -50.00m, "AMAZON PURCHASE");
        var settings = DefaultSettings();

        // Act
        var result = _detector.FindDuplicate(
            new DateOnly(2026, 1, 15),
            -50.00m,
            "AMAZON PURCHASE",
            [existing1, existing2],
            settings);

        // Assert
        Assert.Equal(existing1.Id, result);
    }

    [Fact]
    public void CalculateSimilarity_IdenticalStrings_ReturnsOne()
    {
        var result = _detector.CalculateSimilarity("hello", "hello");
        Assert.Equal(1.0, result);
    }

    [Fact]
    public void CalculateSimilarity_CompletelyDifferent_ReturnsLow()
    {
        var result = _detector.CalculateSimilarity("abc", "xyz");
        Assert.True(result < 0.5);
    }

    [Fact]
    public void CalculateSimilarity_EmptyFirst_ReturnsZero()
    {
        var result = _detector.CalculateSimilarity(string.Empty, "hello");
        Assert.Equal(0.0, result);
    }

    [Fact]
    public void CalculateSimilarity_EmptySecond_ReturnsZero()
    {
        var result = _detector.CalculateSimilarity("hello", string.Empty);
        Assert.Equal(0.0, result);
    }

    [Fact]
    public void CalculateSimilarity_CaseInsensitive()
    {
        var result = _detector.CalculateSimilarity("Hello", "hello");
        Assert.Equal(1.0, result);
    }

    [Fact]
    public void CalculateSimilarity_SimilarStrings_ReturnsHigh()
    {
        var result = _detector.CalculateSimilarity("AMAZON PURCHASE", "AMAZON PURCHAS");
        Assert.True(result > 0.8);
    }

    private static Transaction CreateTransaction(DateOnly date, decimal amount, string description)
    {
        return Transaction.Create(
            Guid.NewGuid(),
            MoneyValue.Create("USD", amount),
            date,
            description);
    }

    private static DuplicateDetectionSettingsDto DefaultSettings(DescriptionMatchMode mode = DescriptionMatchMode.Exact)
    {
        return new DuplicateDetectionSettingsDto
        {
            Enabled = true,
            LookbackDays = 3,
            DescriptionMatch = mode,
        };
    }
}
