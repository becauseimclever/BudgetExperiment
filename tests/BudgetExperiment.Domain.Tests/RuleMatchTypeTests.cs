// <copyright file="RuleMatchTypeTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the RuleMatchType enum.
/// </summary>
public class RuleMatchTypeTests
{
    [Fact]
    public void RuleMatchType_Has_Exact_Value()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(RuleMatchType), RuleMatchType.Exact));
    }

    [Fact]
    public void RuleMatchType_Has_Contains_Value()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(RuleMatchType), RuleMatchType.Contains));
    }

    [Fact]
    public void RuleMatchType_Has_StartsWith_Value()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(RuleMatchType), RuleMatchType.StartsWith));
    }

    [Fact]
    public void RuleMatchType_Has_EndsWith_Value()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(RuleMatchType), RuleMatchType.EndsWith));
    }

    [Fact]
    public void RuleMatchType_Has_Regex_Value()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(RuleMatchType), RuleMatchType.Regex));
    }

    [Fact]
    public void RuleMatchType_Has_Five_Values()
    {
        // Assert
        Assert.Equal(5, Enum.GetValues(typeof(RuleMatchType)).Length);
    }
}
