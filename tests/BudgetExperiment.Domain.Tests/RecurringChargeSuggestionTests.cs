// <copyright file="RecurringChargeSuggestionTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the RecurringChargeSuggestion entity.
/// </summary>
public class RecurringChargeSuggestionTests
{
    private static readonly Guid TestAccountId = Guid.NewGuid();
    private static readonly Guid TestUserId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_CreatesPendingSuggestion()
    {
        // Arrange
        var pattern = CreateTestPattern();

        // Act
        var suggestion = RecurringChargeSuggestion.Create(
            TestAccountId, pattern, BudgetScope.Shared, TestUserId);

        // Assert
        Assert.NotEqual(Guid.Empty, suggestion.Id);
        Assert.Equal(TestAccountId, suggestion.AccountId);
        Assert.Equal("NETFLIX.COM", suggestion.NormalizedDescription);
        Assert.Equal("Netflix.com", suggestion.SampleDescription);
        Assert.Equal(-15.99m, suggestion.AverageAmount.Amount);
        Assert.Equal(RecurrenceFrequency.Monthly, suggestion.DetectedFrequency);
        Assert.Equal(1, suggestion.DetectedInterval);
        Assert.Equal(0.85m, suggestion.Confidence);
        Assert.Equal(6, suggestion.MatchingTransactionCount);
        Assert.Equal(SuggestionStatus.Pending, suggestion.Status);
        Assert.Null(suggestion.AcceptedRecurringTransactionId);
        Assert.Equal(BudgetScope.Shared, suggestion.Scope);
        Assert.Equal(TestUserId, suggestion.CreatedByUserId);
        Assert.NotEqual(default, suggestion.CreatedAtUtc);
        Assert.NotEqual(default, suggestion.UpdatedAtUtc);
    }

    [Fact]
    public void Create_WithEmptyAccountId_Throws()
    {
        var pattern = CreateTestPattern();

        var ex = Assert.Throws<DomainException>(() =>
            RecurringChargeSuggestion.Create(Guid.Empty, pattern, BudgetScope.Shared, TestUserId));

        Assert.Equal("Account ID is required.", ex.Message);
    }

    [Fact]
    public void Create_WithNullPattern_Throws()
    {
        var ex = Assert.Throws<DomainException>(() =>
            RecurringChargeSuggestion.Create(TestAccountId, null!, BudgetScope.Shared, TestUserId));

        Assert.Equal("Detected pattern is required.", ex.Message);
    }

    [Fact]
    public void Create_WithEmptyCreatedByUserId_Throws()
    {
        var pattern = CreateTestPattern();

        var ex = Assert.Throws<DomainException>(() =>
            RecurringChargeSuggestion.Create(TestAccountId, pattern, BudgetScope.Shared, Guid.Empty));

        Assert.Equal("Created by user ID is required.", ex.Message);
    }

    [Fact]
    public void Create_WithInvalidConfidence_Throws()
    {
        var pattern = CreateTestPattern(confidence: 1.5m);

        var ex = Assert.Throws<DomainException>(() =>
            RecurringChargeSuggestion.Create(TestAccountId, pattern, BudgetScope.Shared, TestUserId));

        Assert.Equal("Confidence must be between 0.0 and 1.0.", ex.Message);
    }

    [Fact]
    public void Accept_PendingSuggestion_SetsAccepted()
    {
        // Arrange
        var suggestion = CreateTestSuggestion();
        var recurringTransactionId = Guid.NewGuid();

        // Act
        suggestion.Accept(recurringTransactionId);

        // Assert
        Assert.Equal(SuggestionStatus.Accepted, suggestion.Status);
        Assert.Equal(recurringTransactionId, suggestion.AcceptedRecurringTransactionId);
    }

    [Fact]
    public void Accept_WithEmptyRecurringTransactionId_Throws()
    {
        var suggestion = CreateTestSuggestion();

        var ex = Assert.Throws<DomainException>(() => suggestion.Accept(Guid.Empty));
        Assert.Equal("Recurring transaction ID is required.", ex.Message);
    }

    [Fact]
    public void Accept_AlreadyAccepted_Throws()
    {
        var suggestion = CreateTestSuggestion();
        suggestion.Accept(Guid.NewGuid());

        var ex = Assert.Throws<DomainException>(() => suggestion.Accept(Guid.NewGuid()));
        Assert.Equal("Only pending suggestions can be accepted.", ex.Message);
    }

    [Fact]
    public void Accept_DismissedSuggestion_Throws()
    {
        var suggestion = CreateTestSuggestion();
        suggestion.Dismiss();

        var ex = Assert.Throws<DomainException>(() => suggestion.Accept(Guid.NewGuid()));
        Assert.Equal("Only pending suggestions can be accepted.", ex.Message);
    }

    [Fact]
    public void Dismiss_PendingSuggestion_SetsDismissed()
    {
        // Arrange
        var suggestion = CreateTestSuggestion();

        // Act
        suggestion.Dismiss();

        // Assert
        Assert.Equal(SuggestionStatus.Dismissed, suggestion.Status);
    }

    [Fact]
    public void Dismiss_AlreadyDismissed_Throws()
    {
        var suggestion = CreateTestSuggestion();
        suggestion.Dismiss();

        var ex = Assert.Throws<DomainException>(() => suggestion.Dismiss());
        Assert.Equal("Only pending suggestions can be dismissed.", ex.Message);
    }

    [Fact]
    public void Restore_DismissedSuggestion_SetsPending()
    {
        // Arrange
        var suggestion = CreateTestSuggestion();
        suggestion.Dismiss();

        // Act
        suggestion.Restore();

        // Assert
        Assert.Equal(SuggestionStatus.Pending, suggestion.Status);
    }

    [Fact]
    public void Restore_PendingSuggestion_Throws()
    {
        var suggestion = CreateTestSuggestion();

        var ex = Assert.Throws<DomainException>(() => suggestion.Restore());
        Assert.Equal("Only dismissed suggestions can be restored.", ex.Message);
    }

    [Fact]
    public void UpdateFromDetection_PendingSuggestion_UpdatesFields()
    {
        // Arrange
        var suggestion = CreateTestSuggestion();
        var updatedPattern = new DetectedPattern(
            "NETFLIX.COM",
            "Netflix.com",
            MoneyValue.Create("USD", -16.99m),
            RecurrenceFrequency.Monthly,
            1,
            0.92m,
            [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()],
            new DateOnly(2025, 1, 15),
            new DateOnly(2025, 7, 15),
            null);

        // Act
        suggestion.UpdateFromDetection(updatedPattern);

        // Assert
        Assert.Equal(-16.99m, suggestion.AverageAmount.Amount);
        Assert.Equal(0.92m, suggestion.Confidence);
        Assert.Equal(7, suggestion.MatchingTransactionCount);
    }

    [Fact]
    public void UpdateFromDetection_AcceptedSuggestion_Throws()
    {
        var suggestion = CreateTestSuggestion();
        suggestion.Accept(Guid.NewGuid());

        var ex = Assert.Throws<DomainException>(() =>
            suggestion.UpdateFromDetection(CreateTestPattern()));

        Assert.Equal("Accepted suggestions cannot be updated by detection.", ex.Message);
    }

    [Fact]
    public void UpdateFromDetection_DismissedSuggestion_Updates()
    {
        // Dismissed suggestions can be updated — they may be restored later
        var suggestion = CreateTestSuggestion();
        suggestion.Dismiss();
        var updatedPattern = CreateTestPattern(confidence: 0.90m);

        suggestion.UpdateFromDetection(updatedPattern);

        Assert.Equal(0.90m, suggestion.Confidence);
    }

    private static DetectedPattern CreateTestPattern(decimal confidence = 0.85m)
    {
        return new DetectedPattern(
            "NETFLIX.COM",
            "Netflix.com",
            MoneyValue.Create("USD", -15.99m),
            RecurrenceFrequency.Monthly,
            1,
            confidence,
            [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()],
            new DateOnly(2025, 1, 15),
            new DateOnly(2025, 6, 15),
            null);
    }

    private static RecurringChargeSuggestion CreateTestSuggestion()
    {
        return RecurringChargeSuggestion.Create(
            TestAccountId,
            CreateTestPattern(),
            BudgetScope.Shared,
            TestUserId);
    }
}
