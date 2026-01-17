// <copyright file="BudgetScopeTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Tests for <see cref="BudgetScope"/> enum.
/// </summary>
public class BudgetScopeTests
{
    [Theory]
    [InlineData(BudgetScope.Shared, 0)]
    [InlineData(BudgetScope.Personal, 1)]
    public void BudgetScope_Has_Expected_Values(BudgetScope scope, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)scope);
    }

    [Fact]
    public void BudgetScope_Has_Two_Values()
    {
        var values = Enum.GetValues<BudgetScope>();
        Assert.Equal(2, values.Length);
    }

    [Fact]
    public void BudgetScope_Default_Value_Is_Shared()
    {
        // Default enum value should be Shared (0)
        var defaultScope = default(BudgetScope);
        Assert.Equal(BudgetScope.Shared, defaultScope);
    }
}
