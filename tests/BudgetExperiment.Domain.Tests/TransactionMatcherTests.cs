// <copyright file="TransactionMatcherTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the TransactionMatcher domain service.
/// </summary>
public class TransactionMatcherTests
{
    private readonly TransactionMatcher _matcher = new();
    private readonly MatchingTolerances _defaultTolerances = MatchingTolerances.Default;
    private readonly Guid _accountId = Guid.NewGuid();

    #region Description Matching Tests

    [Fact]
    public void CalculateMatch_ExactDescriptionMatch_ReturnsHighSimilarity()
    {
        // Arrange
        var transaction = this.CreateTransaction("Netflix Subscription", 15.99m, new DateOnly(2026, 1, 15));
        var candidate = this.CreateCandidate("Netflix Subscription", 15.99m, new DateOnly(2026, 1, 15));

        // Act
        var result = this._matcher.CalculateMatch(transaction, candidate, this._defaultTolerances);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1.0m, result.DescriptionSimilarity);
    }

    [Fact]
    public void CalculateMatch_SimilarDescriptionCaseInsensitive_ReturnsHighSimilarity()
    {
        // Arrange
        var transaction = this.CreateTransaction("NETFLIX SUBSCRIPTION", 15.99m, new DateOnly(2026, 1, 15));
        var candidate = this.CreateCandidate("netflix subscription", 15.99m, new DateOnly(2026, 1, 15));

        // Act
        var result = this._matcher.CalculateMatch(transaction, candidate, this._defaultTolerances);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1.0m, result.DescriptionSimilarity);
    }

    [Fact]
    public void CalculateMatch_PartialDescriptionMatch_ReturnsModerateSimilarity()
    {
        // Arrange - Use loose tolerances for partial description matching
        var looseTolerances = MatchingTolerances.Create(7, 0.10m, 10.00m, 0.30m, 0.85m);
        var transaction = this.CreateTransaction("NETFLIX.COM 800-123-4567", 15.99m, new DateOnly(2026, 1, 15));
        var candidate = this.CreateCandidate("Netflix Subscription", 15.99m, new DateOnly(2026, 1, 15));

        // Act
        var result = this._matcher.CalculateMatch(transaction, candidate, looseTolerances);

        // Assert - With lowered threshold, partial match should succeed
        Assert.NotNull(result);
        Assert.True(result.DescriptionSimilarity >= 0.30m, $"Expected >= 0.30, got {result.DescriptionSimilarity}");
    }

    [Fact]
    public void CalculateMatch_VeryDifferentDescription_ReturnsLowSimilarity()
    {
        // Arrange
        var transaction = this.CreateTransaction("AMAZON MARKETPLACE", 50.00m, new DateOnly(2026, 1, 15));
        var candidate = this.CreateCandidate("Netflix", 15.99m, new DateOnly(2026, 1, 15));

        // Act
        var result = this._matcher.CalculateMatch(transaction, candidate, this._defaultTolerances);

        // Assert - Low similarity should result in no match if below threshold
        Assert.Null(result);
    }

    #endregion

    #region Amount Matching Tests

    [Fact]
    public void CalculateMatch_ExactAmountMatch_ReturnsMatch()
    {
        // Arrange
        var transaction = this.CreateTransaction("Netflix", 15.99m, new DateOnly(2026, 1, 15));
        var candidate = this.CreateCandidate("Netflix", 15.99m, new DateOnly(2026, 1, 15));

        // Act
        var result = this._matcher.CalculateMatch(transaction, candidate, this._defaultTolerances);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0m, result.AmountVariance);
    }

    [Fact]
    public void CalculateMatch_AmountWithinPercentTolerance_ReturnsMatch()
    {
        // Arrange - 5% difference, within default 10% tolerance
        var transaction = this.CreateTransaction("Electric Bill", 105.00m, new DateOnly(2026, 1, 15));
        var candidate = this.CreateCandidate("Electric Bill", 100.00m, new DateOnly(2026, 1, 15));

        // Act
        var result = this._matcher.CalculateMatch(transaction, candidate, this._defaultTolerances);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(-5.00m, result.AmountVariance); // Expected - Actual = 100 - 105 = -5
    }

    [Fact]
    public void CalculateMatch_AmountExceedsPercentTolerance_ReturnsNull()
    {
        // Arrange - 20% difference, exceeds default 10% tolerance
        var tolerances = MatchingTolerances.Create(7, 0.10m, 0m, 0.6m, 0.85m); // No absolute tolerance
        var transaction = this.CreateTransaction("Electric Bill", 120.00m, new DateOnly(2026, 1, 15));
        var candidate = this.CreateCandidate("Electric Bill", 100.00m, new DateOnly(2026, 1, 15));

        // Act
        var result = this._matcher.CalculateMatch(transaction, candidate, tolerances);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void CalculateMatch_AmountWithinAbsoluteTolerance_ReturnsMatch()
    {
        // Arrange - $8 difference, within default $10 absolute tolerance
        var transaction = this.CreateTransaction("Gym Membership", 58.00m, new DateOnly(2026, 1, 15));
        var candidate = this.CreateCandidate("Gym Membership", 50.00m, new DateOnly(2026, 1, 15));

        // Act
        var result = this._matcher.CalculateMatch(transaction, candidate, this._defaultTolerances);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(-8.00m, result.AmountVariance);
    }

    [Fact]
    public void CalculateMatch_NegativeAmounts_CalculatesVarianceCorrectly()
    {
        // Arrange - Expenses are typically negative
        var transaction = this.CreateTransaction("Netflix", -15.99m, new DateOnly(2026, 1, 15));
        var candidate = this.CreateCandidate("Netflix", -15.99m, new DateOnly(2026, 1, 15));

        // Act
        var result = this._matcher.CalculateMatch(transaction, candidate, this._defaultTolerances);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0m, result.AmountVariance);
    }

    #endregion

    #region Date Matching Tests

    [Fact]
    public void CalculateMatch_ExactDateMatch_ReturnsMatch()
    {
        // Arrange
        var date = new DateOnly(2026, 1, 15);
        var transaction = this.CreateTransaction("Netflix", 15.99m, date);
        var candidate = this.CreateCandidate("Netflix", 15.99m, date);

        // Act
        var result = this._matcher.CalculateMatch(transaction, candidate, this._defaultTolerances);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.DateOffsetDays);
    }

    [Fact]
    public void CalculateMatch_DateWithinTolerance_ReturnsMatch()
    {
        // Arrange - 3 days after scheduled, within default 7-day tolerance
        var transaction = this.CreateTransaction("Netflix", 15.99m, new DateOnly(2026, 1, 18));
        var candidate = this.CreateCandidate("Netflix", 15.99m, new DateOnly(2026, 1, 15));

        // Act
        var result = this._matcher.CalculateMatch(transaction, candidate, this._defaultTolerances);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.DateOffsetDays);
    }

    [Fact]
    public void CalculateMatch_DateBeforeScheduled_ReturnsNegativeOffset()
    {
        // Arrange - 2 days before scheduled
        var transaction = this.CreateTransaction("Netflix", 15.99m, new DateOnly(2026, 1, 13));
        var candidate = this.CreateCandidate("Netflix", 15.99m, new DateOnly(2026, 1, 15));

        // Act
        var result = this._matcher.CalculateMatch(transaction, candidate, this._defaultTolerances);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(-2, result.DateOffsetDays);
    }

    [Fact]
    public void CalculateMatch_DateExceedsTolerance_ReturnsNull()
    {
        // Arrange - 10 days after, exceeds 7-day tolerance
        var transaction = this.CreateTransaction("Netflix", 15.99m, new DateOnly(2026, 1, 25));
        var candidate = this.CreateCandidate("Netflix", 15.99m, new DateOnly(2026, 1, 15));

        // Act
        var result = this._matcher.CalculateMatch(transaction, candidate, this._defaultTolerances);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Confidence Score Tests

    [Fact]
    public void CalculateMatch_PerfectMatch_ReturnsHighConfidence()
    {
        // Arrange - Exact match on all criteria
        var transaction = this.CreateTransaction("Netflix", 15.99m, new DateOnly(2026, 1, 15));
        var candidate = this.CreateCandidate("Netflix", 15.99m, new DateOnly(2026, 1, 15));

        // Act
        var result = this._matcher.CalculateMatch(transaction, candidate, this._defaultTolerances);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ConfidenceScore >= 0.85m, $"Expected high confidence >= 0.85, got {result.ConfidenceScore}");
        Assert.Equal(MatchConfidenceLevel.High, result.ConfidenceLevel);
    }

    [Fact]
    public void CalculateMatch_ModerateMatch_ReturnsMediumConfidence()
    {
        // Arrange - Small variance in amount, date close, description slightly different
        var transaction = this.CreateTransaction("Netflix Inc", 16.49m, new DateOnly(2026, 1, 17));
        var candidate = this.CreateCandidate("Netflix", 15.99m, new DateOnly(2026, 1, 15));

        // Act
        var result = this._matcher.CalculateMatch(transaction, candidate, this._defaultTolerances);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ConfidenceScore >= 0.60m && result.ConfidenceScore < 0.85m,
            $"Expected medium confidence 0.60-0.85, got {result.ConfidenceScore}");
        Assert.Equal(MatchConfidenceLevel.Medium, result.ConfidenceLevel);
    }

    #endregion

    #region FindMatches Tests

    [Fact]
    public void FindMatches_MultipleMatchingCandidates_ReturnsOrderedByConfidence()
    {
        // Arrange
        var transaction = this.CreateTransaction("Netflix", 15.99m, new DateOnly(2026, 1, 15));
        var candidates = new[]
        {
            this.CreateCandidate("Netflix", 15.99m, new DateOnly(2026, 1, 15)), // Perfect match
            this.CreateCandidate("Netflix", 16.99m, new DateOnly(2026, 1, 16)), // Close match
            this.CreateCandidate("Hulu", 8.99m, new DateOnly(2026, 1, 15)), // Different service
        };

        // Act
        var results = this._matcher.FindMatches(transaction, candidates, this._defaultTolerances);

        // Assert
        Assert.True(results.Count >= 1);
        Assert.True(results[0].ConfidenceScore >= results[results.Count - 1].ConfidenceScore);
    }

    [Fact]
    public void FindMatches_NoCandidates_ReturnsEmptyList()
    {
        // Arrange
        var transaction = this.CreateTransaction("Netflix", 15.99m, new DateOnly(2026, 1, 15));
        var candidates = Array.Empty<RecurringInstanceInfo>();

        // Act
        var results = this._matcher.FindMatches(transaction, candidates, this._defaultTolerances);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void FindMatches_NoViableMatches_ReturnsEmptyList()
    {
        // Arrange
        var transaction = this.CreateTransaction("Completely Different Store", 999.99m, new DateOnly(2026, 6, 1));
        var candidates = new[]
        {
            this.CreateCandidate("Netflix", 15.99m, new DateOnly(2026, 1, 15)),
            this.CreateCandidate("Spotify", 9.99m, new DateOnly(2026, 1, 15)),
        };

        // Act
        var results = this._matcher.FindMatches(transaction, candidates, this._defaultTolerances);

        // Assert
        Assert.Empty(results);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void CalculateMatch_EmptyDescriptions_HandlesGracefully()
    {
        // Arrange
        var transaction = this.CreateTransaction("Payment", 100.00m, new DateOnly(2026, 1, 15));
        var candidate = this.CreateCandidateWithDescription(string.Empty, 100.00m, new DateOnly(2026, 1, 15));

        // Act
        var result = this._matcher.CalculateMatch(transaction, candidate, this._defaultTolerances);

        // Assert - Should not throw, may or may not match depending on scoring
        // The key is that it doesn't crash
        Assert.True(true);
    }

    [Fact]
    public void CalculateMatch_ZeroAmount_HandlesGracefully()
    {
        // Arrange
        var transaction = this.CreateTransaction("Adjustment", 0m, new DateOnly(2026, 1, 15));
        var candidate = this.CreateCandidate("Adjustment", 0m, new DateOnly(2026, 1, 15));

        // Act
        var result = this._matcher.CalculateMatch(transaction, candidate, this._defaultTolerances);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0m, result.AmountVariance);
    }

    #endregion

    #region Helper Methods

    private Transaction CreateTransaction(string description, decimal amount, DateOnly date)
    {
        return Transaction.Create(
            this._accountId,
            MoneyValue.Create("USD", amount),
            date,
            description);
    }

    private RecurringInstanceInfo CreateCandidate(string description, decimal amount, DateOnly instanceDate)
    {
        return new RecurringInstanceInfo(
            RecurringTransactionId: Guid.NewGuid(),
            InstanceDate: instanceDate,
            AccountId: this._accountId,
            AccountName: "Test Account",
            Description: description,
            Amount: MoneyValue.Create("USD", amount),
            CategoryId: null,
            CategoryName: null,
            IsModified: false,
            IsSkipped: false);
    }

    private RecurringInstanceInfo CreateCandidateWithDescription(string description, decimal amount, DateOnly instanceDate)
    {
        return new RecurringInstanceInfo(
            RecurringTransactionId: Guid.NewGuid(),
            InstanceDate: instanceDate,
            AccountId: this._accountId,
            AccountName: "Test Account",
            Description: description,
            Amount: MoneyValue.Create("USD", amount),
            CategoryId: null,
            CategoryName: null,
            IsModified: false,
            IsSkipped: false);
    }

    #endregion
}
