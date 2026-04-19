// <copyright file="ReconciliationMatchTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the ReconciliationMatch entity.
/// </summary>
public class ReconciliationMatchTests
{
    private readonly Guid _importedTransactionId = Guid.NewGuid();
    private readonly Guid _recurringTransactionId = Guid.NewGuid();
    private readonly DateOnly _instanceDate = new(2026, 1, 15);
    private readonly Guid _ownerUserId = Guid.NewGuid();

    [Fact]
    public void Create_With_Valid_Data_Creates_Match()
    {
        // Arrange
        var confidenceScore = 0.85m;
        var amountVariance = -5.50m;
        var dateOffsetDays = 2;

        // Act
        var match = ReconciliationMatch.Create(
            _importedTransactionId,
            _recurringTransactionId,
            _instanceDate,
            confidenceScore,
            amountVariance,
            dateOffsetDays,
            _ownerUserId);

        // Assert
        Assert.NotEqual(Guid.Empty, match.Id);
        Assert.Equal(_importedTransactionId, match.ImportedTransactionId);
        Assert.Equal(_recurringTransactionId, match.RecurringTransactionId);
        Assert.Equal(_instanceDate, match.RecurringInstanceDate);
        Assert.Equal(confidenceScore, match.ConfidenceScore);
        Assert.Equal(amountVariance, match.AmountVariance);
        Assert.Equal(dateOffsetDays, match.DateOffsetDays);
        Assert.Equal(_ownerUserId, match.OwnerUserId);
        Assert.NotEqual(default, match.CreatedAtUtc);
        Assert.Null(match.ResolvedAtUtc);
    }

    [Theory]
    [InlineData(0.85, MatchConfidenceLevel.High)]
    [InlineData(0.90, MatchConfidenceLevel.High)]
    [InlineData(1.00, MatchConfidenceLevel.High)]
    public void Create_With_High_Confidence_Sets_High_Level(decimal score, MatchConfidenceLevel expected)
    {
        // Act
        var match = ReconciliationMatch.Create(
            _importedTransactionId,
            _recurringTransactionId,
            _instanceDate,
            score,
            amountVariance: 0m,
            dateOffsetDays: 0,
            ownerUserId: null);

        // Assert
        Assert.Equal(expected, match.ConfidenceLevel);
    }

    [Theory]
    [InlineData(0.60, MatchConfidenceLevel.Medium)]
    [InlineData(0.70, MatchConfidenceLevel.Medium)]
    [InlineData(0.84, MatchConfidenceLevel.Medium)]
    public void Create_With_Medium_Confidence_Sets_Medium_Level(decimal score, MatchConfidenceLevel expected)
    {
        // Act
        var match = ReconciliationMatch.Create(
            _importedTransactionId,
            _recurringTransactionId,
            _instanceDate,
            score,
            amountVariance: 0m,
            dateOffsetDays: 0,
            ownerUserId: null);

        // Assert
        Assert.Equal(expected, match.ConfidenceLevel);
    }

    [Theory]
    [InlineData(0.00, MatchConfidenceLevel.Low)]
    [InlineData(0.30, MatchConfidenceLevel.Low)]
    [InlineData(0.59, MatchConfidenceLevel.Low)]
    public void Create_With_Low_Confidence_Sets_Low_Level(decimal score, MatchConfidenceLevel expected)
    {
        // Act
        var match = ReconciliationMatch.Create(
            _importedTransactionId,
            _recurringTransactionId,
            _instanceDate,
            score,
            amountVariance: 0m,
            dateOffsetDays: 0,
            ownerUserId: null);

        // Assert
        Assert.Equal(expected, match.ConfidenceLevel);
    }

    [Fact]
    public void Create_Sets_Initial_Status_To_Suggested()
    {
        // Act
        var match = ReconciliationMatch.Create(
            _importedTransactionId,
            _recurringTransactionId,
            _instanceDate,
            confidenceScore: 0.75m,
            amountVariance: 0m,
            dateOffsetDays: 0,
            ownerUserId: null);

        // Assert
        Assert.Equal(ReconciliationMatchStatus.Suggested, match.Status);
    }

    [Fact]
    public void Create_With_Empty_ImportedTransactionId_Throws()
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            ReconciliationMatch.Create(
                Guid.Empty,
                _recurringTransactionId,
                _instanceDate,
                confidenceScore: 0.75m,
                amountVariance: 0m,
                dateOffsetDays: 0,
                ownerUserId: null));

        Assert.Contains("imported transaction", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_With_Empty_RecurringTransactionId_Throws()
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            ReconciliationMatch.Create(
                _importedTransactionId,
                Guid.Empty,
                _instanceDate,
                confidenceScore: 0.75m,
                amountVariance: 0m,
                dateOffsetDays: 0,
                ownerUserId: null));

        Assert.Contains("recurring transaction", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void Create_With_Invalid_ConfidenceScore_Throws(decimal score)
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            ReconciliationMatch.Create(
                _importedTransactionId,
                _recurringTransactionId,
                _instanceDate,
                score,
                amountVariance: 0m,
                dateOffsetDays: 0,
                ownerUserId: null));

        Assert.Contains("confidence score", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Accept_Sets_Status_To_Accepted()
    {
        // Arrange
        var match = ReconciliationMatch.Create(
            _importedTransactionId,
            _recurringTransactionId,
            _instanceDate,
            confidenceScore: 0.75m,
            amountVariance: 0m,
            dateOffsetDays: 0,
            ownerUserId: null);

        // Act
        match.Accept();

        // Assert
        Assert.Equal(ReconciliationMatchStatus.Accepted, match.Status);
        Assert.NotNull(match.ResolvedAtUtc);
    }

    [Fact]
    public void Reject_Sets_Status_To_Rejected()
    {
        // Arrange
        var match = ReconciliationMatch.Create(
            _importedTransactionId,
            _recurringTransactionId,
            _instanceDate,
            confidenceScore: 0.75m,
            amountVariance: 0m,
            dateOffsetDays: 0,
            ownerUserId: null);

        // Act
        match.Reject();

        // Assert
        Assert.Equal(ReconciliationMatchStatus.Rejected, match.Status);
        Assert.NotNull(match.ResolvedAtUtc);
    }

    [Fact]
    public void AutoMatch_Sets_Status_To_AutoMatched()
    {
        // Arrange
        var match = ReconciliationMatch.Create(
            _importedTransactionId,
            _recurringTransactionId,
            _instanceDate,
            confidenceScore: 0.90m,
            amountVariance: 0m,
            dateOffsetDays: 0,
            ownerUserId: null);

        // Act
        match.AutoMatch();

        // Assert
        Assert.Equal(ReconciliationMatchStatus.AutoMatched, match.Status);
        Assert.NotNull(match.ResolvedAtUtc);
    }

    [Theory]
    [InlineData(ReconciliationMatchStatus.Accepted)]
    [InlineData(ReconciliationMatchStatus.Rejected)]
    [InlineData(ReconciliationMatchStatus.AutoMatched)]
    public void Accept_When_Already_Resolved_Throws(ReconciliationMatchStatus initialStatus)
    {
        // Arrange
        var match = ReconciliationMatch.Create(
            _importedTransactionId,
            _recurringTransactionId,
            _instanceDate,
            confidenceScore: 0.75m,
            amountVariance: 0m,
            dateOffsetDays: 0,
            ownerUserId: null);

        // Set to resolved state
        switch (initialStatus)
        {
            case ReconciliationMatchStatus.Accepted:
                match.Accept();
                break;
            case ReconciliationMatchStatus.Rejected:
                match.Reject();
                break;
            case ReconciliationMatchStatus.AutoMatched:
                match.AutoMatch();
                break;
        }

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => match.Accept());
        Assert.Contains("already resolved", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(10.00)] // paid less than expected
    [InlineData(-10.00)] // paid more than expected
    public void AmountVariance_Can_Be_Signed(double varianceDouble)
    {
        var amountVariance = (decimal)varianceDouble;

        // Act
        var match = ReconciliationMatch.Create(
            _importedTransactionId,
            _recurringTransactionId,
            _instanceDate,
            confidenceScore: 0.75m,
            amountVariance,
            dateOffsetDays: 0,
            ownerUserId: null);

        // Assert
        Assert.Equal(amountVariance, match.AmountVariance);
    }

    [Theory]
    [InlineData(3)] // transaction occurred after scheduled date
    [InlineData(-2)] // transaction occurred before scheduled date
    public void DateOffsetDays_Can_Be_Signed(int dateOffsetDays)
    {
        // Act
        var match = ReconciliationMatch.Create(
            _importedTransactionId,
            _recurringTransactionId,
            _instanceDate,
            confidenceScore: 0.75m,
            amountVariance: 0m,
            dateOffsetDays,
            ownerUserId: null);

        // Assert
        Assert.Equal(dateOffsetDays, match.DateOffsetDays);
    }

    [Fact]
    public void Create_Sets_Source_To_Auto()
    {
        // Act
        var match = ReconciliationMatch.Create(
            _importedTransactionId,
            _recurringTransactionId,
            _instanceDate,
            confidenceScore: 0.75m,
            amountVariance: 0m,
            dateOffsetDays: 0,
            ownerUserId: null);

        // Assert
        Assert.Equal(MatchSource.Auto, match.Source);
    }

    [Fact]
    public void CreateManualLink_Sets_Source_To_Manual()
    {
        // Act
        var match = ReconciliationMatch.CreateManualLink(
            _importedTransactionId,
            _recurringTransactionId,
            _instanceDate,
            amountVariance: 5.00m,
            dateOffsetDays: 2,
            _ownerUserId);

        // Assert
        Assert.Equal(MatchSource.Manual, match.Source);
    }

    [Fact]
    public void CreateManualLink_Sets_Status_To_Accepted()
    {
        // Act
        var match = ReconciliationMatch.CreateManualLink(
            _importedTransactionId,
            _recurringTransactionId,
            _instanceDate,
            amountVariance: 0m,
            dateOffsetDays: 0,
            ownerUserId: null);

        // Assert
        Assert.Equal(ReconciliationMatchStatus.Accepted, match.Status);
        Assert.NotNull(match.ResolvedAtUtc);
    }

    [Fact]
    public void CreateManualLink_Sets_ConfidenceScore_To_One()
    {
        // Act
        var match = ReconciliationMatch.CreateManualLink(
            _importedTransactionId,
            _recurringTransactionId,
            _instanceDate,
            amountVariance: 0m,
            dateOffsetDays: 0,
            ownerUserId: null);

        // Assert
        Assert.Equal(1.0m, match.ConfidenceScore);
        Assert.Equal(MatchConfidenceLevel.High, match.ConfidenceLevel);
    }

    [Fact]
    public void CreateManualLink_With_Empty_ImportedTransactionId_Throws()
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            ReconciliationMatch.CreateManualLink(
                Guid.Empty,
                _recurringTransactionId,
                _instanceDate,
                amountVariance: 0m,
                dateOffsetDays: 0,
                ownerUserId: null));

        Assert.Contains("imported transaction", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateManualLink_With_Empty_RecurringTransactionId_Throws()
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            ReconciliationMatch.CreateManualLink(
                _importedTransactionId,
                Guid.Empty,
                _instanceDate,
                amountVariance: 0m,
                dateOffsetDays: 0,
                ownerUserId: null));

        Assert.Contains("recurring transaction", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Unlink_Sets_Status_To_Rejected()
    {
        // Arrange - Create an accepted match
        var match = ReconciliationMatch.CreateManualLink(
            _importedTransactionId,
            _recurringTransactionId,
            _instanceDate,
            amountVariance: 0m,
            dateOffsetDays: 0,
            ownerUserId: null);

        // Act
        match.Unlink();

        // Assert
        Assert.Equal(ReconciliationMatchStatus.Rejected, match.Status);
    }

    [Fact]
    public void Unlink_On_AutoMatched_Match_Sets_Status_To_Rejected()
    {
        // Arrange
        var match = ReconciliationMatch.Create(
            _importedTransactionId,
            _recurringTransactionId,
            _instanceDate,
            confidenceScore: 0.90m,
            amountVariance: 0m,
            dateOffsetDays: 0,
            ownerUserId: null);
        match.AutoMatch();

        // Act
        match.Unlink();

        // Assert
        Assert.Equal(ReconciliationMatchStatus.Rejected, match.Status);
    }

    [Fact]
    public void Unlink_On_Suggested_Match_Throws()
    {
        // Arrange - Create a suggested (not yet resolved) match
        var match = ReconciliationMatch.Create(
            _importedTransactionId,
            _recurringTransactionId,
            _instanceDate,
            confidenceScore: 0.75m,
            amountVariance: 0m,
            dateOffsetDays: 0,
            ownerUserId: null);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => match.Unlink());
        Assert.Contains("not linked", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Unlink_On_Already_Rejected_Match_Throws()
    {
        // Arrange
        var match = ReconciliationMatch.Create(
            _importedTransactionId,
            _recurringTransactionId,
            _instanceDate,
            confidenceScore: 0.75m,
            amountVariance: 0m,
            dateOffsetDays: 0,
            ownerUserId: null);
        match.Reject();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => match.Unlink());
        Assert.Contains("not linked", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
