// <copyright file="RecurringProjectionAccuracyTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Moq;

namespace BudgetExperiment.Application.Tests.Accuracy;

/// <summary>
/// Accuracy tests for recurring transaction projection: verifies that occurrence
/// dates are exactly correct, that no drift accumulates over long sequences, and
/// that end-date boundaries are strictly respected.
/// </summary>
[Trait("Category", "Accuracy")]
public class RecurringProjectionAccuracyTests
{
    private static readonly Guid AccountId = Guid.NewGuid();

    [Fact]
    public void MonthlyRecurring_StartingJan1_ProjectsExactDatesForSixMonths()
    {
        // Arrange
        var startDate = new DateOnly(2026, 1, 1);
        var recurring = CreateMonthlyRecurring(startDate, dayOfMonth: 1);

        // Act — project Jan through Jun
        var occurrences = recurring
            .GetOccurrencesBetween(startDate, new DateOnly(2026, 6, 30))
            .ToList();

        // Assert — exactly 6 months, each on the 1st
        var expected = new[]
        {
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 2, 1),
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 6, 1),
        };

        Assert.Equal(expected, occurrences);
    }

    [Fact]
    public void BiWeeklyRecurring_Over26Periods_NoDriftInDates()
    {
        // Arrange
        var startDate = new DateOnly(2026, 1, 5); // Monday
        var recurring = CreateBiWeeklyRecurring(startDate, DayOfWeek.Monday);

        // Act — project for 52 weeks (one full year)
        var occurrences = recurring
            .GetOccurrencesBetween(startDate, startDate.AddDays(365))
            .ToList();

        // Assert — exactly 27 occurrences (start + 26 bi-weekly steps within 365 days)
        Assert.Equal(27, occurrences.Count);

        // Assert — each consecutive pair is exactly 14 days apart (no drift)
        for (var i = 1; i < occurrences.Count; i++)
        {
            var gap = occurrences[i].DayNumber - occurrences[i - 1].DayNumber;
            Assert.Equal(14, gap);
        }
    }

    [Fact]
    public void RecurringWithEndDate_NoOccurrencesGeneratedAfterEndDate()
    {
        // Arrange
        var startDate = new DateOnly(2026, 1, 1);
        var endDate = new DateOnly(2026, 3, 31);
        var recurring = CreateMonthlyRecurring(startDate, dayOfMonth: 1, endDate: endDate);

        // Act — query beyond the end date
        var occurrences = recurring
            .GetOccurrencesBetween(startDate, new DateOnly(2026, 12, 31))
            .ToList();

        // Assert — only Jan, Feb, Mar — April is beyond end date
        Assert.Equal(3, occurrences.Count);
        Assert.All(occurrences, d => Assert.True(d <= endDate));
    }

    [Fact]
    public void RecurringWithEndDate_EndDateBoundaryIsInclusive()
    {
        // Arrange — end date is exactly the fourth occurrence
        var startDate = new DateOnly(2026, 1, 1);
        var endDate = new DateOnly(2026, 4, 1);
        var recurring = CreateMonthlyRecurring(startDate, dayOfMonth: 1, endDate: endDate);

        // Act
        var occurrences = recurring
            .GetOccurrencesBetween(startDate, new DateOnly(2026, 12, 31))
            .ToList();

        // Assert — the April 1 occurrence (= end date) must be included
        Assert.Contains(endDate, occurrences);
        Assert.Equal(4, occurrences.Count);
    }

    [Fact]
    public void OccurrenceCount_MatchesExpectedForDateRange()
    {
        // Arrange — weekly recurring for January 2026 (31 days = 5 Mondays: Jan 5,12,19,26... wait Jan has 31 days)
        // Jan 1 is Thursday 2026; first Monday is Jan 5. Mondays in Jan: 5,12,19,26 = 4 Mondays
        var startDate = new DateOnly(2026, 1, 5); // first Monday
        var recurring = CreateWeeklyRecurring(startDate, DayOfWeek.Monday);

        // Act
        var occurrences = recurring
            .GetOccurrencesBetween(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31))
            .ToList();

        // Assert — 4 Mondays in January 2026
        Assert.Equal(4, occurrences.Count);
        Assert.Equal(new DateOnly(2026, 1, 5), occurrences[0]);
        Assert.Equal(new DateOnly(2026, 1, 26), occurrences[3]);
    }

    [Fact]
    public void Occurrences_DoNotExtendBeforeStartDate()
    {
        // Arrange — recurring starts Feb 1; query from Jan 1
        var startDate = new DateOnly(2026, 2, 1);
        var recurring = CreateMonthlyRecurring(startDate, dayOfMonth: 1);

        // Act — query range includes a month before the start date
        var occurrences = recurring
            .GetOccurrencesBetween(new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30))
            .ToList();

        // Assert — no occurrence before the start date
        Assert.All(occurrences, d => Assert.True(d >= startDate));
        Assert.Equal(new DateOnly(2026, 2, 1), occurrences.First());
    }

    [Fact]
    public async Task Projector_SkippedException_InstanceDateOmittedFromProjection()
    {
        // Arrange
        var startDate = new DateOnly(2026, 1, 1);
        var skippedDate = new DateOnly(2026, 2, 1);
        var recurring = CreateMonthlyRecurring(startDate, dayOfMonth: 1);

        var recurringRepo = new Mock<IRecurringTransactionRepository>();
        var accountRepo = new Mock<IAccountRepository>();
        accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account>());

        var skippedException = RecurringTransactionException.CreateSkipped(recurring.Id, skippedDate);
        recurringRepo
            .Setup(r => r.GetExceptionsByDateRangeAsync(
                recurring.Id,
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransactionException> { skippedException });

        var projector = new RecurringInstanceProjector(recurringRepo.Object, accountRepo.Object);

        // Act — project Jan through Mar
        var result = await projector.GetInstancesByDateRangeAsync(
            new List<RecurringTransaction> { recurring },
            startDate,
            new DateOnly(2026, 3, 31));

        // Assert — Feb 1 is skipped; Jan 1 and Mar 1 are present
        Assert.DoesNotContain(skippedDate, result.Keys);
        Assert.Contains(new DateOnly(2026, 1, 1), result.Keys);
        Assert.Contains(new DateOnly(2026, 3, 1), result.Keys);
    }

    private static RecurringTransaction CreateMonthlyRecurring(
        DateOnly startDate,
        int dayOfMonth,
        DateOnly? endDate = null)
    {
        return RecurringTransaction.Create(
            AccountId,
            "Monthly Bill",
            MoneyValue.Create("USD", -100m),
            RecurrencePatternValue.CreateMonthly(1, dayOfMonth),
            startDate,
            endDate);
    }

    private static RecurringTransaction CreateBiWeeklyRecurring(DateOnly startDate, DayOfWeek dayOfWeek)
    {
        return RecurringTransaction.Create(
            AccountId,
            "Bi-weekly Payment",
            MoneyValue.Create("USD", -200m),
            RecurrencePatternValue.CreateBiWeekly(dayOfWeek),
            startDate);
    }

    private static RecurringTransaction CreateWeeklyRecurring(DateOnly startDate, DayOfWeek dayOfWeek)
    {
        return RecurringTransaction.Create(
            AccountId,
            "Weekly Payment",
            MoneyValue.Create("USD", -50m),
            RecurrencePatternValue.CreateWeekly(1, dayOfWeek),
            startDate);
    }
}
