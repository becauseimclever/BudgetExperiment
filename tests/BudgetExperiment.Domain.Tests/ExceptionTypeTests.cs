// <copyright file="ExceptionTypeTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Tests for <see cref="ExceptionType"/> enum.
/// </summary>
public class ExceptionTypeTests
{
    [Theory]
    [InlineData(ExceptionType.Modified, 0)]
    [InlineData(ExceptionType.Skipped, 1)]
    public void ExceptionType_Has_Expected_Values(ExceptionType exceptionType, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)exceptionType);
    }

    [Fact]
    public void ExceptionType_Has_Two_Values()
    {
        var values = Enum.GetValues<ExceptionType>();
        Assert.Equal(2, values.Length);
    }
}
