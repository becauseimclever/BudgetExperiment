// <copyright file="MoneyRoundingTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;
using BudgetExperiment.Domain.Common;
using Shouldly;

namespace BudgetExperiment.Application.Tests.DataConsistency;

/// <summary>
/// Unit tests for money value rounding and numeric precision in calculations.
/// </summary>
public class MoneyRoundingTests
{
    public MoneyRoundingTests()
    {
        // Set culture to en-US for consistent currency formatting
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
    }

    [Fact]
    public void MoneyValue_RoundsTo2Decimals_USD()
    {
        // Arrange & Act
        var money = MoneyValue.Create("USD", 10.125m);

        // Assert: Should round to 10.13 (round half away from zero)
        money.Amount.ShouldBe(10.13m);
    }

    [Fact]
    public void MoneyValue_Addition_Precision_NoAccumulation()
    {
        // Arrange: Three values that sum to 0.03
        var value1 = MoneyValue.Create("USD", 0.01m);
        var value2 = MoneyValue.Create("USD", 0.01m);
        var value3 = MoneyValue.Create("USD", 0.01m);

        // Act: Add them together
        var sum = value1 + value2 + value3;

        // Assert: Sum should be exactly 0.03, no rounding errors
        sum.Amount.ShouldBe(0.03m);
        sum.Currency.ShouldBe("USD");
    }

    [Fact]
    public void MoneyValue_Subtraction_Precision_Exact()
    {
        // Arrange: Values that test precision on subtraction
        var minuend = MoneyValue.Create("USD", 100.00m);
        var subtrahend = MoneyValue.Create("USD", 33.33m);

        // Act
        var difference = minuend - subtrahend;

        // Assert
        difference.Amount.ShouldBe(66.67m);
    }

    [Fact]
    public void MoneyValue_ComplexCalculation_Preserves2Decimals()
    {
        // Arrange: Mix of additions and subtractions
        var initial = MoneyValue.Create("USD", 100.00m);
        var payment1 = MoneyValue.Create("USD", 25.50m);
        var payment2 = MoneyValue.Create("USD", 30.25m);
        var refund = MoneyValue.Create("USD", 10.00m);

        // Act: initial - payment1 - payment2 + refund
        var result = ((initial - payment1) - payment2) + refund;

        // Assert: 100 - 25.50 - 30.25 + 10.00 = 54.25
        result.Amount.ShouldBe(54.25m);
    }

    [Fact]
    public void MoneyValue_LargeNumbers_NoOverflow()
    {
        // Arrange: Very large transaction amount
        var largeMoney = MoneyValue.Create("USD", 1000000.00m);

        // Act & Assert: Should handle million-dollar amounts
        largeMoney.Amount.ShouldBe(1000000.00m);
        largeMoney.Currency.ShouldBe("USD");
    }

    [Fact]
    public void MoneyValue_NegativeValues_Allowed()
    {
        // Arrange & Act
        var negative = MoneyValue.Create("USD", -50.75m);

        // Assert: Negative amounts allowed (for corrections/refunds)
        negative.Amount.ShouldBe(-50.75m);
    }

    [Fact]
    public void MoneyValue_ZeroValue_Valid()
    {
        // Arrange & Act
        var zero = MoneyValue.Zero("USD");

        // Assert
        zero.Amount.ShouldBe(0m);
        zero.Currency.ShouldBe("USD");
    }

    [Fact]
    public void MoneyValue_DifferentCurrencies_ThrowException()
    {
        // Arrange
        var usdValue = MoneyValue.Create("USD", 100m);
        var eurValue = MoneyValue.Create("EUR", 100m);

        // Act & Assert: Cannot add different currencies
        var ex = Should.Throw<DomainException>(() => usdValue + eurValue);
        ex.Message.ShouldContain("different currencies", Case.Insensitive);
    }

    [Fact]
    public void MoneyValue_CurrencyNormalization_Uppercase()
    {
        // Arrange & Act
        var money = MoneyValue.Create("usd", 50m);

        // Assert: Currency should be uppercase
        money.Currency.ShouldBe("USD");
    }

    [Fact]
    public void MoneyValue_RoundingAwayFromZero_Positive()
    {
        // Arrange & Act: 10.125 should round to 10.13 (away from zero)
        var money = MoneyValue.Create("USD", 10.125m);

        // Assert
        money.Amount.ShouldBe(10.13m);
    }

    [Fact]
    public void MoneyValue_RoundingAwayFromZero_Negative()
    {
        // Arrange & Act: -10.125 should round to -10.13 (away from zero)
        var money = MoneyValue.Create("USD", -10.125m);

        // Assert
        money.Amount.ShouldBe(-10.13m);
    }

    [Fact]
    public void MoneyValue_SumOfManySmallTransactions_NoPrecisionLoss()
    {
        // Arrange: Sum 100 transactions of 0.01 each
        var amount = MoneyValue.Create("USD", 0.01m);
        var sum = amount;

        // Act: Add 99 more times
        for (int i = 0; i < 99; i++)
        {
            sum = sum + amount;
        }

        // Assert: Total should be exactly 1.00, no drift
        sum.Amount.ShouldBe(1.00m);
    }

    [Fact]
    public void MoneyValue_Subtraction_DifferentCurrencies_ThrowException()
    {
        // Arrange
        var usdValue = MoneyValue.Create("USD", 100m);
        var gbpValue = MoneyValue.Create("GBP", 50m);

        // Act & Assert
        var ex = Should.Throw<DomainException>(() => usdValue - gbpValue);
        ex.Message.ShouldContain("different currencies", Case.Insensitive);
    }
}
