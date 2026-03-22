// <copyright file="RecurrencePatternFactoryTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Recurring;
using BudgetExperiment.Domain;

using Shouldly;

using Xunit;

namespace BudgetExperiment.Application.Tests.Services;

/// <summary>
/// Unit tests for <see cref="RecurrencePatternFactory"/>.
/// </summary>
public class RecurrencePatternFactoryTests
{
    [Fact]
    public void Create_WithDailyFrequency_ReturnsDailyPattern()
    {
        var pattern = RecurrencePatternFactory.Create("Daily", 1, null, null, null);

        pattern.Frequency.ShouldBe(RecurrenceFrequency.Daily);
        pattern.Interval.ShouldBe(1);
    }

    [Fact]
    public void Create_WithWeeklyFrequency_ReturnsWeeklyPattern()
    {
        var pattern = RecurrencePatternFactory.Create("Weekly", 2, null, "Monday", null);

        pattern.Frequency.ShouldBe(RecurrenceFrequency.Weekly);
        pattern.Interval.ShouldBe(2);
        pattern.DayOfWeek.ShouldBe(DayOfWeek.Monday);
    }

    [Fact]
    public void Create_WithMonthlyFrequency_ReturnsMonthlyPattern()
    {
        var pattern = RecurrencePatternFactory.Create("Monthly", 1, 15, null, null);

        pattern.Frequency.ShouldBe(RecurrenceFrequency.Monthly);
        pattern.DayOfMonth.ShouldBe(15);
    }

    [Fact]
    public void Create_WithInvalidFrequency_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() =>
            RecurrencePatternFactory.Create("InvalidFreq", 1, null, null, null));
    }

    [Fact]
    public void ParseDayOfWeek_WithValidDay_ReturnsParsedDay()
    {
        RecurrencePatternFactory.ParseDayOfWeek("Friday").ShouldBe(DayOfWeek.Friday);
    }

    [Fact]
    public void ParseDayOfWeek_WithNull_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() =>
            RecurrencePatternFactory.ParseDayOfWeek(null));
    }

    [Fact]
    public void ParseDayOfWeek_WithInvalidDay_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() =>
            RecurrencePatternFactory.ParseDayOfWeek("NotADay"));
    }
}
