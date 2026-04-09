// <copyright file="RecurringInstanceProjectorExcludeDatesTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Moq;

namespace BudgetExperiment.Application.Tests.Recurring;

/// <summary>
/// Unit tests for the <c>excludeDates</c> parameter introduced in
/// <see cref="IRecurringInstanceProjector.GetInstancesByDateRangeAsync"/> (Feature 147).
/// Verifies that already-realized dates are suppressed from projected results.
/// </summary>
public class RecurringInstanceProjectorExcludeDatesTests
{
    // Weekly recurring every Monday; 12 Mondays from Jan 7 to Mar 25 2030.
    private static readonly DateOnly FromDate = new(2030, 1, 7);
    private static readonly DateOnly ToDate = new(2030, 3, 25);

    private static readonly DateOnly[] AllMondays = new[]
    {
        new DateOnly(2030, 1, 7),
        new DateOnly(2030, 1, 14),
        new DateOnly(2030, 1, 21),
        new DateOnly(2030, 1, 28),
        new DateOnly(2030, 2, 4),
        new DateOnly(2030, 2, 11),
        new DateOnly(2030, 2, 18),
        new DateOnly(2030, 2, 25),
        new DateOnly(2030, 3, 4),
        new DateOnly(2030, 3, 11),
        new DateOnly(2030, 3, 18),
        new DateOnly(2030, 3, 25),
    };

    private readonly Mock<IRecurringTransactionRepository> _recurringRepo;
    private readonly Mock<IAccountRepository> _accountRepo;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringInstanceProjectorExcludeDatesTests"/> class.
    /// </summary>
    public RecurringInstanceProjectorExcludeDatesTests()
    {
        _recurringRepo = new Mock<IRecurringTransactionRepository>();
        _accountRepo = new Mock<IAccountRepository>();

        _accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account>());
    }

    /// <summary>
    /// When two realized dates are in the exclude set, those dates must be absent from the projected result.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExcludeDates_SkipsExcludedOccurrences()
    {
        // Arrange
        var recurring = CreateWeeklyRecurring();
        SetupNoExceptions(recurring.Id);
        var projector = CreateProjector();

        var excludeDates = new HashSet<DateOnly>
        {
            new DateOnly(2030, 1, 7),   // Week 1
            new DateOnly(2030, 2, 4),   // Week 5
        };

        // Act
        var result = await projector.GetInstancesByDateRangeAsync(
            new List<RecurringTransaction> { recurring },
            FromDate,
            ToDate,
            excludeDates: excludeDates);

        // Assert — excluded dates absent, remaining 10 weeks present
        Assert.DoesNotContain(new DateOnly(2030, 1, 7), result.Keys);
        Assert.DoesNotContain(new DateOnly(2030, 2, 4), result.Keys);
        Assert.Equal(10, result.Count);
    }

    /// <summary>
    /// Passing an empty <see cref="ISet{T}"/> must produce the same output as passing null:
    /// all occurrences are returned.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExcludeDates_EmptySet_ReturnsAll()
    {
        // Arrange
        var recurring = CreateWeeklyRecurring();
        SetupNoExceptions(recurring.Id);
        var projector = CreateProjector();

        // Act
        var result = await projector.GetInstancesByDateRangeAsync(
            new List<RecurringTransaction> { recurring },
            FromDate,
            ToDate,
            excludeDates: new HashSet<DateOnly>());

        // Assert — all 12 weeks projected
        Assert.Equal(12, result.Count);
        Assert.All(AllMondays, d => Assert.True(result.ContainsKey(d)));
    }

    /// <summary>
    /// Passing <c>null</c> for <c>excludeDates</c> must be backward-compatible:
    /// all occurrences are returned (no exclusion applied).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExcludeDates_NullParameter_ReturnsAll()
    {
        // Arrange
        var recurring = CreateWeeklyRecurring();
        SetupNoExceptions(recurring.Id);
        var projector = CreateProjector();

        // Act
        var result = await projector.GetInstancesByDateRangeAsync(
            new List<RecurringTransaction> { recurring },
            FromDate,
            ToDate,
            excludeDates: null);

        // Assert — all 12 weeks projected
        Assert.Equal(12, result.Count);
        Assert.All(AllMondays, d => Assert.True(result.ContainsKey(d)));
    }

    /// <summary>
    /// When every occurrence date is included in the exclude set, the result must be empty.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExcludeDates_AllOccurrences_ReturnsEmpty()
    {
        // Arrange
        var recurring = CreateWeeklyRecurring();
        SetupNoExceptions(recurring.Id);
        var projector = CreateProjector();

        var allDates = new HashSet<DateOnly>(AllMondays);

        // Act
        var result = await projector.GetInstancesByDateRangeAsync(
            new List<RecurringTransaction> { recurring },
            FromDate,
            ToDate,
            excludeDates: allDates);

        // Assert — nothing projected when every date is excluded
        Assert.Empty(result);
    }

    // ===== Helpers =====
    private static RecurringTransaction CreateWeeklyRecurring()
    {
        return RecurringTransaction.Create(
            Guid.NewGuid(),
            "Weekly Payment",
            MoneyValue.Create("USD", -50m),
            RecurrencePatternValue.CreateWeekly(1, DayOfWeek.Monday),
            FromDate);
    }

    private void SetupNoExceptions(Guid recurringId)
    {
        _recurringRepo
            .Setup(r => r.GetExceptionsByDateRangeAsync(
                recurringId,
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransactionException>());
    }

    private RecurringInstanceProjector CreateProjector()
    {
        return new RecurringInstanceProjector(_recurringRepo.Object, _accountRepo.Object);
    }
}
