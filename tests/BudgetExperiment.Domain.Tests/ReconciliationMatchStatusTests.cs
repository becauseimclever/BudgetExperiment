// <copyright file="ReconciliationMatchStatusTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the ReconciliationMatchStatus enum.
/// </summary>
public class ReconciliationMatchStatusTests
{
    [Fact]
    public void ReconciliationMatchStatus_Has_Suggested_Value()
    {
        // Arrange & Act
        var suggested = ReconciliationMatchStatus.Suggested;

        // Assert
        Assert.Equal(0, (int)suggested);
    }

    [Fact]
    public void ReconciliationMatchStatus_Has_Accepted_Value()
    {
        // Arrange & Act
        var accepted = ReconciliationMatchStatus.Accepted;

        // Assert
        Assert.Equal(1, (int)accepted);
    }

    [Fact]
    public void ReconciliationMatchStatus_Has_Rejected_Value()
    {
        // Arrange & Act
        var rejected = ReconciliationMatchStatus.Rejected;

        // Assert
        Assert.Equal(2, (int)rejected);
    }

    [Fact]
    public void ReconciliationMatchStatus_Has_AutoMatched_Value()
    {
        // Arrange & Act
        var autoMatched = ReconciliationMatchStatus.AutoMatched;

        // Assert
        Assert.Equal(3, (int)autoMatched);
    }

    [Theory]
    [InlineData(ReconciliationMatchStatus.Suggested)]
    [InlineData(ReconciliationMatchStatus.Accepted)]
    [InlineData(ReconciliationMatchStatus.Rejected)]
    [InlineData(ReconciliationMatchStatus.AutoMatched)]
    public void ReconciliationMatchStatus_Values_Are_Defined(ReconciliationMatchStatus status)
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(ReconciliationMatchStatus), status));
    }
}
