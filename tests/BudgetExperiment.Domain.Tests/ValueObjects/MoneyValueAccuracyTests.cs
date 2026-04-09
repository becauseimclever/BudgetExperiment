// <copyright file="MoneyValueAccuracyTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Tests.ValueObjects;

/// <summary>
/// Accuracy tests verifying that <see cref="MoneyValue"/> arithmetic is exact,
/// without floating-point drift, and that rounding follows AwayFromZero semantics.
/// </summary>
[Trait("Category", "Accuracy")]
public class MoneyValueAccuracyTests
{
    [Fact]
    public void Addition_LongSequenceOfSmallAmounts_ProducesExactTotal()
    {
        // Arrange — fold 100 additions of $0.01 using the + operator
        var running = MoneyValue.Zero("USD");

        // Act
        for (var i = 0; i < 100; i++)
        {
            running = running + MoneyValue.Create("USD", 0.01m);
        }

        // Assert — decimal arithmetic must not accumulate drift
        Assert.Equal(1.00m, running.Amount);
    }

    [Fact]
    public void Addition_ZeroLeftOperand_IsIdentity()
    {
        var zero = MoneyValue.Zero("USD");
        var value = MoneyValue.Create("USD", 42.99m);

        var result = zero + value;

        Assert.Equal(42.99m, result.Amount);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public void Addition_ZeroRightOperand_IsIdentity()
    {
        var value = MoneyValue.Create("USD", 100.50m);
        var zero = MoneyValue.Zero("USD");

        var result = value + zero;

        Assert.Equal(100.50m, result.Amount);
    }

    [Fact]
    public void Subtraction_ZeroOperand_IsIdentity()
    {
        var value = MoneyValue.Create("USD", 75.25m);
        var zero = MoneyValue.Zero("USD");

        var result = value - zero;

        Assert.Equal(75.25m, result.Amount);
    }

    [Fact]
    public void Subtraction_LargerFromSmaller_ProducesExactNegativeResult()
    {
        var small = MoneyValue.Create("USD", 3.00m);
        var large = MoneyValue.Create("USD", 10.00m);

        var result = small - large;

        Assert.Equal(-7.00m, result.Amount);
    }

    [Fact]
    public void Create_PositiveMidpointAmount_RoundsAwayFromZero()
    {
        // $0.005 rounds UP to $0.01 (AwayFromZero for positive)
        var value = MoneyValue.Create("USD", 0.005m);

        Assert.Equal(0.01m, value.Amount);
    }

    [Fact]
    public void Create_NegativeMidpointAmount_RoundsAwayFromZero()
    {
        // -$0.005 rounds to -$0.01 (AwayFromZero = further from zero)
        var value = MoneyValue.Create("USD", -0.005m);

        Assert.Equal(-0.01m, value.Amount);
    }

    [Fact]
    public void Addition_KnownDecimalTriple_ProducesExactSum()
    {
        // 1.10 + 2.20 + 3.30 = 6.60 exactly with decimal (would drift with float)
        var a = MoneyValue.Create("USD", 1.10m);
        var b = MoneyValue.Create("USD", 2.20m);
        var c = MoneyValue.Create("USD", 3.30m);

        var result = a + b + c;

        Assert.Equal(6.60m, result.Amount);
    }

    [Theory]
    [InlineData(0.00, 0.00, 0.00)]
    [InlineData(0.00, 5.55, 5.55)]
    [InlineData(5.55, 0.00, 5.55)]
    public void Addition_WithZero_Identity(double leftRaw, double rightRaw, double expectedRaw)
    {
        var left = MoneyValue.Create("USD", (decimal)leftRaw);
        var right = MoneyValue.Create("USD", (decimal)rightRaw);

        var result = left + right;

        Assert.Equal((decimal)expectedRaw, result.Amount);
    }
}
