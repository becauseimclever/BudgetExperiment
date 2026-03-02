// <copyright file="AmountParseModeTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the AmountParseMode enum.
/// </summary>
public class AmountParseModeTests
{
    [Theory]
    [InlineData(AmountParseMode.NegativeIsExpense, 0)]
    [InlineData(AmountParseMode.PositiveIsExpense, 1)]
    [InlineData(AmountParseMode.SeparateColumns, 2)]
    [InlineData(AmountParseMode.AbsoluteExpense, 3)]
    [InlineData(AmountParseMode.AbsoluteIncome, 4)]
    [InlineData(AmountParseMode.IndicatorColumn, 5)]
    public void AmountParseMode_Has_Expected_Values(AmountParseMode mode, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)mode);
    }
}
