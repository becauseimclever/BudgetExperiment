// <copyright file="DebitCreditIndicatorSettingsTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the DebitCreditIndicatorSettings value object.
/// </summary>
public class DebitCreditIndicatorSettingsTests
{
    [Fact]
    public void Create_With_Valid_Data_Creates_Settings()
    {
        // Arrange
        var debitIndicators = new List<string> { "Debit", "DR" };
        var creditIndicators = new List<string> { "Credit", "CR" };

        // Act
        var settings = DebitCreditIndicatorSettings.Create(0, debitIndicators, creditIndicators);

        // Assert
        Assert.Equal(0, settings.IndicatorColumnIndex);
        Assert.Equal(2, settings.DebitIndicators.Count);
        Assert.Equal(2, settings.CreditIndicators.Count);
        Assert.False(settings.CaseSensitive);
        Assert.True(settings.IsEnabled);
    }

    [Fact]
    public void Create_With_Negative_ColumnIndex_Throws_DomainException()
    {
        // Arrange
        var debitIndicators = new List<string> { "Debit" };
        var creditIndicators = new List<string> { "Credit" };

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            DebitCreditIndicatorSettings.Create(-1, debitIndicators, creditIndicators));
        Assert.Contains("non-negative", ex.Message);
    }

    [Fact]
    public void Create_With_Empty_DebitIndicators_Throws_DomainException()
    {
        // Arrange
        var debitIndicators = new List<string>();
        var creditIndicators = new List<string> { "Credit" };

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            DebitCreditIndicatorSettings.Create(0, debitIndicators, creditIndicators));
        Assert.Contains("debit indicator", ex.Message);
    }

    [Fact]
    public void Create_With_Null_DebitIndicators_Throws_DomainException()
    {
        // Arrange
        var creditIndicators = new List<string> { "Credit" };

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            DebitCreditIndicatorSettings.Create(0, null!, creditIndicators));
        Assert.Contains("debit indicator", ex.Message);
    }

    [Fact]
    public void Create_With_Empty_CreditIndicators_Throws_DomainException()
    {
        // Arrange
        var debitIndicators = new List<string> { "Debit" };
        var creditIndicators = new List<string>();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            DebitCreditIndicatorSettings.Create(0, debitIndicators, creditIndicators));
        Assert.Contains("credit indicator", ex.Message);
    }

    [Fact]
    public void Create_With_Null_CreditIndicators_Throws_DomainException()
    {
        // Arrange
        var debitIndicators = new List<string> { "Debit" };

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            DebitCreditIndicatorSettings.Create(0, debitIndicators, null!));
        Assert.Contains("credit indicator", ex.Message);
    }

    [Fact]
    public void Create_With_Overlapping_Indicators_Throws_DomainException()
    {
        // Arrange
        var debitIndicators = new List<string> { "Debit", "Transaction" };
        var creditIndicators = new List<string> { "Credit", "Transaction" };

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            DebitCreditIndicatorSettings.Create(0, debitIndicators, creditIndicators));
        Assert.Contains("overlap", ex.Message);
    }

    [Fact]
    public void Create_With_Overlapping_Indicators_Case_Insensitive_Throws_DomainException()
    {
        // Arrange
        var debitIndicators = new List<string> { "DEBIT" };
        var creditIndicators = new List<string> { "debit" };

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            DebitCreditIndicatorSettings.Create(0, debitIndicators, creditIndicators, caseSensitive: false));
        Assert.Contains("overlap", ex.Message);
    }

    [Fact]
    public void Create_With_Same_Value_Different_Case_When_CaseSensitive_Does_Not_Throw()
    {
        // Arrange
        var debitIndicators = new List<string> { "DEBIT" };
        var creditIndicators = new List<string> { "debit" };

        // Act
        var settings = DebitCreditIndicatorSettings.Create(0, debitIndicators, creditIndicators, caseSensitive: true);

        // Assert
        Assert.True(settings.CaseSensitive);
        Assert.True(settings.IsEnabled);
    }

    [Fact]
    public void Create_Trims_Indicator_Values()
    {
        // Arrange
        var debitIndicators = new List<string> { "  Debit  " };
        var creditIndicators = new List<string> { "  Credit  " };

        // Act
        var settings = DebitCreditIndicatorSettings.Create(0, debitIndicators, creditIndicators);

        // Assert
        Assert.Equal("Debit", settings.DebitIndicators[0]);
        Assert.Equal("Credit", settings.CreditIndicators[0]);
    }

    [Fact]
    public void Disabled_Returns_Settings_With_IsEnabled_False()
    {
        // Arrange & Act
        var settings = DebitCreditIndicatorSettings.Disabled;

        // Assert
        Assert.False(settings.IsEnabled);
        Assert.Equal(-1, settings.IndicatorColumnIndex);
        Assert.Empty(settings.DebitIndicators);
        Assert.Empty(settings.CreditIndicators);
    }

    [Fact]
    public void GetSignMultiplier_Returns_Negative_For_DebitIndicator()
    {
        // Arrange
        var settings = DebitCreditIndicatorSettings.Create(
            0,
            new List<string> { "Debit", "DR" },
            new List<string> { "Credit", "CR" });

        // Act
        var result = settings.GetSignMultiplier("DR");

        // Assert
        Assert.Equal(-1, result);
    }

    [Fact]
    public void GetSignMultiplier_Returns_Positive_For_CreditIndicator()
    {
        // Arrange
        var settings = DebitCreditIndicatorSettings.Create(
            0,
            new List<string> { "Debit", "DR" },
            new List<string> { "Credit", "CR" });

        // Act
        var result = settings.GetSignMultiplier("CR");

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public void GetSignMultiplier_Returns_Null_For_Unknown_Indicator()
    {
        // Arrange
        var settings = DebitCreditIndicatorSettings.Create(
            0,
            new List<string> { "Debit" },
            new List<string> { "Credit" });

        // Act
        var result = settings.GetSignMultiplier("Unknown");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetSignMultiplier_Returns_Null_For_Empty_Value()
    {
        // Arrange
        var settings = DebitCreditIndicatorSettings.Create(
            0,
            new List<string> { "Debit" },
            new List<string> { "Credit" });

        // Act
        var result = settings.GetSignMultiplier(string.Empty);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetSignMultiplier_Returns_Null_When_Disabled()
    {
        // Arrange
        var settings = DebitCreditIndicatorSettings.Disabled;

        // Act
        var result = settings.GetSignMultiplier("Debit");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetSignMultiplier_Is_Case_Insensitive_By_Default()
    {
        // Arrange
        var settings = DebitCreditIndicatorSettings.Create(
            0,
            new List<string> { "Debit" },
            new List<string> { "Credit" });

        // Act
        var resultDebit = settings.GetSignMultiplier("DEBIT");
        var resultCredit = settings.GetSignMultiplier("credit");

        // Assert
        Assert.Equal(-1, resultDebit);
        Assert.Equal(1, resultCredit);
    }

    [Fact]
    public void GetSignMultiplier_Is_Case_Sensitive_When_Configured()
    {
        // Arrange
        var settings = DebitCreditIndicatorSettings.Create(
            0,
            new List<string> { "Debit" },
            new List<string> { "Credit" },
            caseSensitive: true);

        // Act
        var resultDebit = settings.GetSignMultiplier("DEBIT");
        var resultCredit = settings.GetSignMultiplier("Credit");

        // Assert
        Assert.Null(resultDebit); // Should not match due to case
        Assert.Equal(1, resultCredit); // Should match
    }

    [Fact]
    public void GetSignMultiplier_Trims_Input_Value()
    {
        // Arrange
        var settings = DebitCreditIndicatorSettings.Create(
            0,
            new List<string> { "Debit" },
            new List<string> { "Credit" });

        // Act
        var result = settings.GetSignMultiplier("  Debit  ");

        // Assert
        Assert.Equal(-1, result);
    }
}
