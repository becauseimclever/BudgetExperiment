// <copyright file="MatchingTolerancesTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the MatchingTolerances value object.
/// </summary>
public class MatchingTolerancesTests
{
    [Fact]
    public void Default_Returns_Valid_Configuration()
    {
        // Act
        var tolerances = MatchingTolerances.Default;

        // Assert
        Assert.Equal(7, tolerances.DateToleranceDays);
        Assert.Equal(0.10m, tolerances.AmountTolerancePercent);
        Assert.Equal(10.00m, tolerances.AmountToleranceAbsolute);
        Assert.Equal(0.6m, tolerances.DescriptionSimilarityThreshold);
        Assert.Equal(0.85m, tolerances.AutoMatchThreshold);
    }

    [Fact]
    public void Create_With_Valid_Values_Creates_Instance()
    {
        // Act
        var tolerances = MatchingTolerances.Create(
            dateToleranceDays: 5,
            amountTolerancePercent: 0.15m,
            amountToleranceAbsolute: 20.00m,
            descriptionSimilarityThreshold: 0.7m,
            autoMatchThreshold: 0.90m);

        // Assert
        Assert.Equal(5, tolerances.DateToleranceDays);
        Assert.Equal(0.15m, tolerances.AmountTolerancePercent);
        Assert.Equal(20.00m, tolerances.AmountToleranceAbsolute);
        Assert.Equal(0.7m, tolerances.DescriptionSimilarityThreshold);
        Assert.Equal(0.90m, tolerances.AutoMatchThreshold);
    }

    [Fact]
    public void Create_With_Negative_DateToleranceDays_Throws()
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            MatchingTolerances.Create(
                dateToleranceDays: -1,
                amountTolerancePercent: 0.10m,
                amountToleranceAbsolute: 10.00m,
                descriptionSimilarityThreshold: 0.6m,
                autoMatchThreshold: 0.85m));

        Assert.Contains("date tolerance", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_With_Negative_AmountTolerancePercent_Throws()
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            MatchingTolerances.Create(
                dateToleranceDays: 7,
                amountTolerancePercent: -0.10m,
                amountToleranceAbsolute: 10.00m,
                descriptionSimilarityThreshold: 0.6m,
                autoMatchThreshold: 0.85m));

        Assert.Contains("amount tolerance percent", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_With_AmountTolerancePercent_Over_One_Throws()
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            MatchingTolerances.Create(
                dateToleranceDays: 7,
                amountTolerancePercent: 1.5m,
                amountToleranceAbsolute: 10.00m,
                descriptionSimilarityThreshold: 0.6m,
                autoMatchThreshold: 0.85m));

        Assert.Contains("amount tolerance percent", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_With_Negative_AmountToleranceAbsolute_Throws()
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            MatchingTolerances.Create(
                dateToleranceDays: 7,
                amountTolerancePercent: 0.10m,
                amountToleranceAbsolute: -5.00m,
                descriptionSimilarityThreshold: 0.6m,
                autoMatchThreshold: 0.85m));

        Assert.Contains("amount tolerance absolute", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void Create_With_Invalid_DescriptionSimilarityThreshold_Throws(decimal threshold)
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            MatchingTolerances.Create(
                dateToleranceDays: 7,
                amountTolerancePercent: 0.10m,
                amountToleranceAbsolute: 10.00m,
                descriptionSimilarityThreshold: threshold,
                autoMatchThreshold: 0.85m));

        Assert.Contains("description similarity", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void Create_With_Invalid_AutoMatchThreshold_Throws(decimal threshold)
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            MatchingTolerances.Create(
                dateToleranceDays: 7,
                amountTolerancePercent: 0.10m,
                amountToleranceAbsolute: 10.00m,
                descriptionSimilarityThreshold: 0.6m,
                autoMatchThreshold: threshold));

        Assert.Contains("auto match threshold", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_With_Zero_DateToleranceDays_Is_Valid()
    {
        // Act
        var tolerances = MatchingTolerances.Create(
            dateToleranceDays: 0,
            amountTolerancePercent: 0.10m,
            amountToleranceAbsolute: 10.00m,
            descriptionSimilarityThreshold: 0.6m,
            autoMatchThreshold: 0.85m);

        // Assert - exact date match only
        Assert.Equal(0, tolerances.DateToleranceDays);
    }

    [Fact]
    public void Create_With_Zero_AmountTolerancePercent_Is_Valid()
    {
        // Act
        var tolerances = MatchingTolerances.Create(
            dateToleranceDays: 7,
            amountTolerancePercent: 0m,
            amountToleranceAbsolute: 10.00m,
            descriptionSimilarityThreshold: 0.6m,
            autoMatchThreshold: 0.85m);

        // Assert - exact amount match only (by percent)
        Assert.Equal(0m, tolerances.AmountTolerancePercent);
    }

    [Fact]
    public void Create_With_Zero_AmountToleranceAbsolute_Is_Valid()
    {
        // Act
        var tolerances = MatchingTolerances.Create(
            dateToleranceDays: 7,
            amountTolerancePercent: 0.10m,
            amountToleranceAbsolute: 0m,
            descriptionSimilarityThreshold: 0.6m,
            autoMatchThreshold: 0.85m);

        // Assert - exact amount match only (by absolute)
        Assert.Equal(0m, tolerances.AmountToleranceAbsolute);
    }

    [Fact]
    public void Two_Instances_With_Same_Values_Are_Equal()
    {
        // Arrange
        var tolerances1 = MatchingTolerances.Create(5, 0.15m, 20.00m, 0.7m, 0.90m);
        var tolerances2 = MatchingTolerances.Create(5, 0.15m, 20.00m, 0.7m, 0.90m);

        // Assert
        Assert.Equal(tolerances1, tolerances2);
        Assert.Equal(tolerances1.GetHashCode(), tolerances2.GetHashCode());
    }

    [Fact]
    public void Two_Instances_With_Different_Values_Are_Not_Equal()
    {
        // Arrange
        var tolerances1 = MatchingTolerances.Create(5, 0.15m, 20.00m, 0.7m, 0.90m);
        var tolerances2 = MatchingTolerances.Create(7, 0.15m, 20.00m, 0.7m, 0.90m);

        // Assert
        Assert.NotEqual(tolerances1, tolerances2);
    }
}
