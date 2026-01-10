// <copyright file="RecurrencePatternTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Tests for <see cref="RecurrencePattern"/> value object.
/// </summary>
public class RecurrencePatternTests
{
    [Fact]
    public void CreateDaily_With_Valid_Interval_Creates_Pattern()
    {
        var pattern = RecurrencePattern.CreateDaily(1);

        Assert.Equal(RecurrenceFrequency.Daily, pattern.Frequency);
        Assert.Equal(1, pattern.Interval);
        Assert.Null(pattern.DayOfMonth);
        Assert.Null(pattern.DayOfWeek);
        Assert.Null(pattern.MonthOfYear);
    }

    [Fact]
    public void CreateDaily_With_Interval_Greater_Than_One_Creates_Pattern()
    {
        var pattern = RecurrencePattern.CreateDaily(3);

        Assert.Equal(RecurrenceFrequency.Daily, pattern.Frequency);
        Assert.Equal(3, pattern.Interval);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateDaily_With_Invalid_Interval_Throws(int interval)
    {
        var ex = Assert.Throws<DomainException>(() => RecurrencePattern.CreateDaily(interval));
        Assert.Contains("Interval must be at least 1", ex.Message);
    }

    [Fact]
    public void CreateWeekly_With_Valid_Parameters_Creates_Pattern()
    {
        var pattern = RecurrencePattern.CreateWeekly(1, DayOfWeek.Monday);

        Assert.Equal(RecurrenceFrequency.Weekly, pattern.Frequency);
        Assert.Equal(1, pattern.Interval);
        Assert.Equal(DayOfWeek.Monday, pattern.DayOfWeek);
        Assert.Null(pattern.DayOfMonth);
        Assert.Null(pattern.MonthOfYear);
    }

    [Fact]
    public void CreateBiWeekly_With_Valid_Parameters_Creates_Pattern()
    {
        var pattern = RecurrencePattern.CreateBiWeekly(DayOfWeek.Friday);

        Assert.Equal(RecurrenceFrequency.BiWeekly, pattern.Frequency);
        Assert.Equal(2, pattern.Interval);
        Assert.Equal(DayOfWeek.Friday, pattern.DayOfWeek);
    }

    [Fact]
    public void CreateMonthly_With_Valid_DayOfMonth_Creates_Pattern()
    {
        var pattern = RecurrencePattern.CreateMonthly(1, 15);

        Assert.Equal(RecurrenceFrequency.Monthly, pattern.Frequency);
        Assert.Equal(1, pattern.Interval);
        Assert.Equal(15, pattern.DayOfMonth);
        Assert.Null(pattern.DayOfWeek);
        Assert.Null(pattern.MonthOfYear);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    [InlineData(-1)]
    public void CreateMonthly_With_Invalid_DayOfMonth_Throws(int dayOfMonth)
    {
        var ex = Assert.Throws<DomainException>(() => RecurrencePattern.CreateMonthly(1, dayOfMonth));
        Assert.Contains("Day of month must be between 1 and 31", ex.Message);
    }

    [Fact]
    public void CreateQuarterly_With_Valid_DayOfMonth_Creates_Pattern()
    {
        var pattern = RecurrencePattern.CreateQuarterly(1);

        Assert.Equal(RecurrenceFrequency.Quarterly, pattern.Frequency);
        Assert.Equal(3, pattern.Interval);
        Assert.Equal(1, pattern.DayOfMonth);
    }

    [Fact]
    public void CreateYearly_With_Valid_Parameters_Creates_Pattern()
    {
        var pattern = RecurrencePattern.CreateYearly(15, 6);

        Assert.Equal(RecurrenceFrequency.Yearly, pattern.Frequency);
        Assert.Equal(1, pattern.Interval);
        Assert.Equal(15, pattern.DayOfMonth);
        Assert.Equal(6, pattern.MonthOfYear);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(-1)]
    public void CreateYearly_With_Invalid_MonthOfYear_Throws(int monthOfYear)
    {
        var ex = Assert.Throws<DomainException>(() => RecurrencePattern.CreateYearly(15, monthOfYear));
        Assert.Contains("Month of year must be between 1 and 12", ex.Message);
    }

    [Fact]
    public void Two_Patterns_With_Same_Values_Are_Equal()
    {
        var pattern1 = RecurrencePattern.CreateMonthly(1, 15);
        var pattern2 = RecurrencePattern.CreateMonthly(1, 15);

        Assert.Equal(pattern1, pattern2);
        Assert.True(pattern1 == pattern2);
    }

    [Fact]
    public void Two_Patterns_With_Different_Values_Are_Not_Equal()
    {
        var pattern1 = RecurrencePattern.CreateMonthly(1, 15);
        var pattern2 = RecurrencePattern.CreateMonthly(1, 20);

        Assert.NotEqual(pattern1, pattern2);
        Assert.True(pattern1 != pattern2);
    }

    [Fact]
    public void CalculateNextOccurrence_Daily_Returns_Next_Day()
    {
        var pattern = RecurrencePattern.CreateDaily(1);
        var fromDate = new DateOnly(2026, 1, 10);

        var nextDate = pattern.CalculateNextOccurrence(fromDate);

        Assert.Equal(new DateOnly(2026, 1, 11), nextDate);
    }

    [Fact]
    public void CalculateNextOccurrence_Daily_With_Interval_Returns_Correct_Date()
    {
        var pattern = RecurrencePattern.CreateDaily(3);
        var fromDate = new DateOnly(2026, 1, 10);

        var nextDate = pattern.CalculateNextOccurrence(fromDate);

        Assert.Equal(new DateOnly(2026, 1, 13), nextDate);
    }

    [Fact]
    public void CalculateNextOccurrence_Weekly_Returns_Next_Week()
    {
        var pattern = RecurrencePattern.CreateWeekly(1, DayOfWeek.Friday);
        var fromDate = new DateOnly(2026, 1, 9); // Friday

        var nextDate = pattern.CalculateNextOccurrence(fromDate);

        Assert.Equal(new DateOnly(2026, 1, 16), nextDate); // Next Friday
    }

    [Fact]
    public void CalculateNextOccurrence_BiWeekly_Returns_Two_Weeks_Later()
    {
        var pattern = RecurrencePattern.CreateBiWeekly(DayOfWeek.Friday);
        var fromDate = new DateOnly(2026, 1, 9); // Friday

        var nextDate = pattern.CalculateNextOccurrence(fromDate);

        Assert.Equal(new DateOnly(2026, 1, 23), nextDate); // 2 weeks later
    }

    [Fact]
    public void CalculateNextOccurrence_Monthly_Returns_Next_Month()
    {
        var pattern = RecurrencePattern.CreateMonthly(1, 15);
        var fromDate = new DateOnly(2026, 1, 15);

        var nextDate = pattern.CalculateNextOccurrence(fromDate);

        Assert.Equal(new DateOnly(2026, 2, 15), nextDate);
    }

    [Fact]
    public void CalculateNextOccurrence_Monthly_DayOfMonth31_In_February_Uses_Last_Day()
    {
        var pattern = RecurrencePattern.CreateMonthly(1, 31);
        var fromDate = new DateOnly(2026, 1, 31);

        var nextDate = pattern.CalculateNextOccurrence(fromDate);

        Assert.Equal(new DateOnly(2026, 2, 28), nextDate); // February has 28 days in 2026
    }

    [Fact]
    public void CalculateNextOccurrence_Monthly_DayOfMonth31_In_LeapYear_February_Uses_29()
    {
        var pattern = RecurrencePattern.CreateMonthly(1, 31);
        var fromDate = new DateOnly(2024, 1, 31); // 2024 is a leap year

        var nextDate = pattern.CalculateNextOccurrence(fromDate);

        Assert.Equal(new DateOnly(2024, 2, 29), nextDate);
    }

    [Fact]
    public void CalculateNextOccurrence_Quarterly_Returns_Three_Months_Later()
    {
        var pattern = RecurrencePattern.CreateQuarterly(1);
        var fromDate = new DateOnly(2026, 1, 1);

        var nextDate = pattern.CalculateNextOccurrence(fromDate);

        Assert.Equal(new DateOnly(2026, 4, 1), nextDate);
    }

    [Fact]
    public void CalculateNextOccurrence_Yearly_Returns_Next_Year()
    {
        var pattern = RecurrencePattern.CreateYearly(15, 6);
        var fromDate = new DateOnly(2026, 6, 15);

        var nextDate = pattern.CalculateNextOccurrence(fromDate);

        Assert.Equal(new DateOnly(2027, 6, 15), nextDate);
    }

    [Fact]
    public void CalculateNextOccurrence_Yearly_Feb29_In_NonLeapYear_Uses_Feb28()
    {
        var pattern = RecurrencePattern.CreateYearly(29, 2);
        var fromDate = new DateOnly(2024, 2, 29); // 2024 is a leap year

        var nextDate = pattern.CalculateNextOccurrence(fromDate);

        Assert.Equal(new DateOnly(2025, 2, 28), nextDate); // 2025 is not a leap year
    }

    [Fact]
    public void ToString_Returns_Readable_Description()
    {
        var pattern = RecurrencePattern.CreateMonthly(1, 15);

        var result = pattern.ToString();

        Assert.Contains("Monthly", result);
    }
}
