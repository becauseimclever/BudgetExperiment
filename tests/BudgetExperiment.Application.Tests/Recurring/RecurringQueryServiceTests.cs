// <copyright file="RecurringQueryServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Moq;

namespace BudgetExperiment.Application.Tests.Recurring;

/// <summary>
/// Unit tests for <see cref="RecurringQueryService"/>.
/// Verifies that the service correctly bridges realized transaction data with the projector's
/// <c>excludeDates</c> parameter to enforce INV-7 (no double-counting of recurring instances).
/// </summary>
public class RecurringQueryServiceTests
{
    private readonly Mock<ITransactionRepository> _transactionRepo;
    private readonly Mock<IRecurringInstanceProjector> _projector;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringQueryServiceTests"/> class.
    /// </summary>
    public RecurringQueryServiceTests()
    {
        _transactionRepo = new Mock<ITransactionRepository>();
        _projector = new Mock<IRecurringInstanceProjector>();
    }

    /// <summary>
    /// Passing null for the projector must throw <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void RecurringQueryService_NullProjector_ThrowsArgumentNull()
    {
        // Arrange
        var transactionRepo = _transactionRepo.Object;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new RecurringQueryService(transactionRepo, null!));
    }

    /// <summary>
    /// Passing null for the transaction repository must throw <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void RecurringQueryService_NullRepository_ThrowsArgumentNull()
    {
        // Arrange
        var projector = _projector.Object;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new RecurringQueryService(null!, projector));
    }

    /// <summary>
    /// When the repository returns realized transactions for the given recurring IDs,
    /// the projector must be called with a non-empty <c>excludeDates</c> set
    /// containing exactly those transaction dates.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task RecurringQueryService_WithRealizations_ExcludesThemFromProjection()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var fromDate = new DateOnly(2030, 1, 7);
        var toDate = new DateOnly(2030, 3, 25);

        var recurring = CreateWeeklyRecurring(accountId, fromDate);

        var realizedDates = new[]
        {
            new DateOnly(2030, 1, 7),
            new DateOnly(2030, 1, 14),
            new DateOnly(2030, 1, 21),
        };

        var realizedTransactions = realizedDates
            .Select(d => Transaction.CreateFromRecurring(
                accountId,
                MoneyValue.Create("USD", -50m),
                d,
                "Weekly Payment",
                recurring.Id,
                d))
            .ToList<Transaction>();

        _transactionRepo
            .Setup(r => r.GetByDateRangeAsync(fromDate, toDate, null, default))
            .ReturnsAsync(realizedTransactions);

        _projector
            .Setup(p => p.GetInstancesByDateRangeAsync(
                It.IsAny<IReadOnlyList<RecurringTransaction>>(),
                fromDate,
                toDate,
                It.IsAny<ISet<DateOnly>>(),
                default))
            .ReturnsAsync(new Dictionary<DateOnly, List<RecurringInstanceInfoValue>>());

        var service = new RecurringQueryService(_transactionRepo.Object, _projector.Object);

        // Act
        await service.GetProjectedInstancesAsync(
            new List<RecurringTransaction> { recurring },
            fromDate,
            toDate);

        // Assert — projector invoked with a set containing all 3 realized dates
        _projector.Verify(
            p => p.GetInstancesByDateRangeAsync(
                It.IsAny<IReadOnlyList<RecurringTransaction>>(),
                fromDate,
                toDate,
                It.Is<ISet<DateOnly>>(s =>
                    s.Count == 3 &&
                    s.Contains(new DateOnly(2030, 1, 7)) &&
                    s.Contains(new DateOnly(2030, 1, 14)) &&
                    s.Contains(new DateOnly(2030, 1, 21))),
                default),
            Times.Once);
    }

    /// <summary>
    /// When no realized transactions exist in the date range, the projector must be called
    /// with an empty <c>excludeDates</c> set (no exclusions applied).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task RecurringQueryService_NoRealizations_ReturnsAllProjections()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var fromDate = new DateOnly(2030, 1, 7);
        var toDate = new DateOnly(2030, 3, 25);
        var recurring = CreateWeeklyRecurring(accountId, fromDate);

        _transactionRepo
            .Setup(r => r.GetByDateRangeAsync(fromDate, toDate, null, default))
            .ReturnsAsync(new List<Transaction>());

        _projector
            .Setup(p => p.GetInstancesByDateRangeAsync(
                It.IsAny<IReadOnlyList<RecurringTransaction>>(),
                fromDate,
                toDate,
                It.IsAny<ISet<DateOnly>>(),
                default))
            .ReturnsAsync(new Dictionary<DateOnly, List<RecurringInstanceInfoValue>>());

        var service = new RecurringQueryService(_transactionRepo.Object, _projector.Object);

        // Act
        await service.GetProjectedInstancesAsync(
            new List<RecurringTransaction> { recurring },
            fromDate,
            toDate);

        // Assert — projector called with an empty exclude set (nothing to exclude)
        _projector.Verify(
            p => p.GetInstancesByDateRangeAsync(
                It.IsAny<IReadOnlyList<RecurringTransaction>>(),
                fromDate,
                toDate,
                It.Is<ISet<DateOnly>>(s => s.Count == 0),
                default),
            Times.Once);
    }

    /// <summary>
    /// When <c>accountId</c> is null, the transaction repository must be queried without an account
    /// filter so all accounts are considered when collecting realized dates.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task RecurringQueryService_NullAccountId_FetchesAllAccounts()
    {
        // Arrange
        var fromDate = new DateOnly(2030, 1, 7);
        var toDate = new DateOnly(2030, 3, 25);
        var recurring = CreateWeeklyRecurring(Guid.NewGuid(), fromDate);

        _transactionRepo
            .Setup(r => r.GetByDateRangeAsync(fromDate, toDate, null, default))
            .ReturnsAsync(new List<Transaction>());

        _projector
            .Setup(p => p.GetInstancesByDateRangeAsync(
                It.IsAny<IReadOnlyList<RecurringTransaction>>(),
                fromDate,
                toDate,
                It.IsAny<ISet<DateOnly>>(),
                default))
            .ReturnsAsync(new Dictionary<DateOnly, List<RecurringInstanceInfoValue>>());

        var service = new RecurringQueryService(_transactionRepo.Object, _projector.Object);

        // Act — no accountId supplied
        await service.GetProjectedInstancesAsync(
            new List<RecurringTransaction> { recurring },
            fromDate,
            toDate,
            accountId: null);

        // Assert — repository queried with null accountId (no account filter applied)
        _transactionRepo.Verify(
            r => r.GetByDateRangeAsync(fromDate, toDate, null, default),
            Times.Once);
    }

    // ===== Helpers =====
    private static RecurringTransaction CreateWeeklyRecurring(Guid accountId, DateOnly startDate)
    {
        return RecurringTransaction.Create(
            accountId,
            "Weekly Payment",
            MoneyValue.Create("USD", -50m),
            RecurrencePatternValue.CreateWeekly(1, DayOfWeek.Monday),
            startDate);
    }
}
