// <copyright file="ReconciliationMapperTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Reconciliation;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain.Reconciliation;

namespace BudgetExperiment.Application.Tests.Reconciliation;

/// <summary>
/// Unit tests for ReconciliationMapper.
/// </summary>
public class ReconciliationMapperTests
{
    [Fact]
    public void ToDto_ReconciliationMatch_MapsAllProperties()
    {
        // Arrange
        var importedTxId = Guid.NewGuid();
        var recurringTxId = Guid.NewGuid();
        var instanceDate = new DateOnly(2026, 4, 21);
        var match = ReconciliationMatch.Create(
            importedTxId,
            recurringTxId,
            instanceDate,
            confidenceScore: 0.9m,
            amountVariance: 2.50m,
            dateOffsetDays: -1,
            ownerUserId: null);

        // Act
        var dto = ReconciliationMapper.ToDto(match);

        // Assert
        Assert.Equal(match.Id, dto.Id);
        Assert.Equal(importedTxId, dto.ImportedTransactionId);
        Assert.Equal(recurringTxId, dto.RecurringTransactionId);
        Assert.Equal(instanceDate, dto.RecurringInstanceDate);
        Assert.Equal(0.9m, dto.ConfidenceScore);
        Assert.Equal("High", dto.ConfidenceLevel);
        Assert.Equal("Suggested", dto.Status);
        Assert.Equal("Auto", dto.Source);
        Assert.Equal(2.50m, dto.AmountVariance);
        Assert.Equal(-1, dto.DateOffsetDays);
        Assert.NotEqual(default, dto.CreatedAtUtc);
        Assert.Null(dto.ResolvedAtUtc);
        Assert.Null(dto.ImportedTransaction);
        Assert.Null(dto.RecurringTransactionDescription);
        Assert.Null(dto.ExpectedAmount);
    }

    [Fact]
    public void ToDto_ReconciliationMatch_WithOptionalFields_MapsEnrichedData()
    {
        // Arrange
        var match = ReconciliationMatch.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 4, 21),
            confidenceScore: 0.7m,
            amountVariance: 0m,
            dateOffsetDays: 0,
            ownerUserId: null);

        var accountId = Guid.NewGuid();
        var transaction = TransactionFactory.Create(
            accountId,
            MoneyValue.Create("USD", 100.00m),
            new DateOnly(2026, 4, 21),
            "Test Transaction",
            null);

        var expectedAmount = MoneyValue.Create("USD", 100.00m);

        // Act
        var dto = ReconciliationMapper.ToDto(
            match,
            transaction,
            "Monthly Subscription",
            expectedAmount);

        // Assert
        Assert.NotNull(dto.ImportedTransaction);
        Assert.Equal("Monthly Subscription", dto.RecurringTransactionDescription);
        Assert.NotNull(dto.ExpectedAmount);
        Assert.Equal(100.00m, dto.ExpectedAmount!.Amount);
    }

    [Fact]
    public void ToDto_MatchingTolerancesValue_MapsAllProperties()
    {
        // Arrange
        var tolerances = MatchingTolerancesValue.Create(
            dateToleranceDays: 5,
            amountTolerancePercent: 0.15m,
            amountToleranceAbsolute: 25.00m,
            descriptionSimilarityThreshold: 0.7m,
            autoMatchThreshold: 0.9m);

        // Act
        var dto = ReconciliationMapper.ToDto(tolerances);

        // Assert
        Assert.Equal(5, dto.DateToleranceDays);
        Assert.Equal(0.15m, dto.AmountTolerancePercent);
        Assert.Equal(25.00m, dto.AmountToleranceAbsolute);
        Assert.Equal(0.7m, dto.DescriptionSimilarityThreshold);
        Assert.Equal(0.9m, dto.AutoMatchThreshold);
    }

    [Fact]
    public void ToDomain_MatchingTolerancesDto_CreatesValidValueObject()
    {
        // Arrange
        var dto = new MatchingTolerancesDto
        {
            DateToleranceDays = 7,
            AmountTolerancePercent = 0.10m,
            AmountToleranceAbsolute = 10.00m,
            DescriptionSimilarityThreshold = 0.6m,
            AutoMatchThreshold = 0.85m,
        };

        // Act
        var domain = ReconciliationMapper.ToDomain(dto);

        // Assert
        Assert.Equal(7, domain.DateToleranceDays);
        Assert.Equal(0.10m, domain.AmountTolerancePercent);
        Assert.Equal(10.00m, domain.AmountToleranceAbsolute);
        Assert.Equal(0.6m, domain.DescriptionSimilarityThreshold);
        Assert.Equal(0.85m, domain.AutoMatchThreshold);
    }

    [Fact]
    public void ToDto_MatchingTolerancesValue_RoundTrip_PreservesValues()
    {
        // Arrange
        var original = MatchingTolerancesValue.Create(3, 0.05m, 5.00m, 0.8m, 0.95m);

        // Act
        var dto = ReconciliationMapper.ToDto(original);
        var roundTrip = ReconciliationMapper.ToDomain(dto);

        // Assert
        Assert.Equal(original.DateToleranceDays, roundTrip.DateToleranceDays);
        Assert.Equal(original.AmountTolerancePercent, roundTrip.AmountTolerancePercent);
        Assert.Equal(original.AmountToleranceAbsolute, roundTrip.AmountToleranceAbsolute);
        Assert.Equal(original.DescriptionSimilarityThreshold, roundTrip.DescriptionSimilarityThreshold);
        Assert.Equal(original.AutoMatchThreshold, roundTrip.AutoMatchThreshold);
    }
}
