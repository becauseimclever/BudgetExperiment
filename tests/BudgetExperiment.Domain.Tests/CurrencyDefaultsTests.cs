// <copyright file="CurrencyDefaultsTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Regression guard tests for <see cref="CurrencyDefaults"/> values.
/// Ensures currency default strings are never accidentally changed.
/// </summary>
public sealed class CurrencyDefaultsTests
{
    /// <summary>
    /// DefaultCurrency must equal "USD".
    /// </summary>
    [Fact]
    public void DefaultCurrency_EqualsExpectedValue()
    {
        Assert.Equal("USD", CurrencyDefaults.DefaultCurrency);
    }
}
