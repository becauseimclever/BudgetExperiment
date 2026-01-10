// <copyright file="RecurrenceFrequencyTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Tests for <see cref="RecurrenceFrequency"/> enum.
/// </summary>
public class RecurrenceFrequencyTests
{
    [Theory]
    [InlineData(RecurrenceFrequency.Daily, 0)]
    [InlineData(RecurrenceFrequency.Weekly, 1)]
    [InlineData(RecurrenceFrequency.BiWeekly, 2)]
    [InlineData(RecurrenceFrequency.Monthly, 3)]
    [InlineData(RecurrenceFrequency.Quarterly, 4)]
    [InlineData(RecurrenceFrequency.Yearly, 5)]
    public void RecurrenceFrequency_Has_Expected_Values(RecurrenceFrequency frequency, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)frequency);
    }

    [Fact]
    public void RecurrenceFrequency_Has_Six_Values()
    {
        var values = Enum.GetValues<RecurrenceFrequency>();
        Assert.Equal(6, values.Length);
    }
}
