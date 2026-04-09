// <copyright file="RecurringProjectionAccuracyTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Infrastructure.Persistence.Repositories;

using Microsoft.Extensions.Logging.Abstractions;

namespace BudgetExperiment.Infrastructure.Tests.Accuracy;

/// <summary>
/// Integration accuracy tests for recurring projection and realization, backed by a real PostgreSQL
/// Testcontainer. These tests prove INV-7 (Recurring Projection No-Double-Count):
/// the sum of projected instances plus realized transactions equals the expected occurrence count,
/// with no double-counting regardless of how many instances have been realized.
/// </summary>
[Collection("PostgreSqlDb")]
[Trait("Category", "Accuracy")]
public sealed class RecurringProjectionAccuracyTests
{
    private const int TotalOccurrences = 12;

    // 12 Mondays starting Jan 7 2030 through Mar 25 2030.
    private static readonly DateOnly RangeFrom = new(2030, 1, 7);
    private static readonly DateOnly RangeTo = new(2030, 3, 25);

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

    private readonly PostgreSqlFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringProjectionAccuracyTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared PostgreSQL Testcontainer fixture.</param>
    public RecurringProjectionAccuracyTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Prove INV-7: with 5 of 12 occurrences realized, projected count is 7 and
    /// projected + realized equals the total expected occurrence count of 12.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task RecurringProjection_ProjectedPlusRealized_EqualsExpectedOccurrences()
    {
        const int realizedCount = 5;
        const int expectedProjectedCount = TotalOccurrences - realizedCount;

        // Arrange — account and recurring transaction
        await using var context = _fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        var recurringRepo = new RecurringTransactionRepository(context, FakeUserContext.CreateDefault());
        var transactionRepo = new TransactionRepository(context, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);

        var account = Account.Create("Proj-Accuracy-Partial", AccountType.Checking);
        await accountRepo.AddAsync(account);

        var recurring = RecurringTransaction.Create(
            account.Id,
            "Weekly Groceries",
            MoneyValue.Create("USD", -75m),
            RecurrencePatternValue.CreateWeekly(1, DayOfWeek.Monday),
            RangeFrom);
        await recurringRepo.AddAsync(recurring);
        await context.SaveChangesAsync();

        // Realize the first 5 occurrences
        for (var i = 0; i < realizedCount; i++)
        {
            var instanceDate = AllMondays[i];
            var tx = Transaction.CreateFromRecurring(
                account.Id,
                MoneyValue.Create("USD", -75m),
                instanceDate,
                "Weekly Groceries",
                recurring.Id,
                instanceDate);
            await transactionRepo.AddAsync(tx);
        }

        await context.SaveChangesAsync();

        // Act — build service with real repositories and query projected instances
        await using var readContext = _fixture.CreateSharedContext(context);
        var readRecurringRepo = new RecurringTransactionRepository(readContext, FakeUserContext.CreateDefault());
        var readAccountRepo = new AccountRepository(readContext, FakeUserContext.CreateDefault());
        var readTransactionRepo = new TransactionRepository(readContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);

        var projector = new RecurringInstanceProjector(readRecurringRepo, readAccountRepo);
        var service = new RecurringQueryService(readTransactionRepo, projector);

        var loadedRecurring = await readRecurringRepo.GetByIdAsync(recurring.Id);
        Assert.NotNull(loadedRecurring);

        var projected = await service.GetProjectedInstancesAsync(
            new List<RecurringTransaction> { loadedRecurring },
            RangeFrom,
            RangeTo);

        var actualProjectedCount = projected.Values.Sum(list => list.Count);

        // Verify realized count independently
        var realizedTransactions = await readTransactionRepo.GetByDateRangeAsync(RangeFrom, RangeTo, account.Id);
        var actualRealizedCount = realizedTransactions.Count(t => t.RecurringTransactionId == recurring.Id);

        // Assert — projected (7) + realized (5) = total (12); INV-7 holds
        Assert.Equal(expectedProjectedCount, actualProjectedCount);
        Assert.Equal(realizedCount, actualRealizedCount);
        Assert.Equal(TotalOccurrences, actualProjectedCount + actualRealizedCount);
    }

    /// <summary>
    /// When no occurrences have been realized, all 12 must appear as projected instances.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task RecurringProjection_NoRealizations_ProjectsAll()
    {
        // Arrange — account and recurring transaction, no realized transactions
        await using var context = _fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        var recurringRepo = new RecurringTransactionRepository(context, FakeUserContext.CreateDefault());

        var account = Account.Create("Proj-Accuracy-None", AccountType.Checking);
        await accountRepo.AddAsync(account);

        var recurring = RecurringTransaction.Create(
            account.Id,
            "Weekly Groceries (none realized)",
            MoneyValue.Create("USD", -75m),
            RecurrencePatternValue.CreateWeekly(1, DayOfWeek.Monday),
            RangeFrom);
        await recurringRepo.AddAsync(recurring);
        await context.SaveChangesAsync();

        // Act
        await using var readContext = _fixture.CreateSharedContext(context);
        var readRecurringRepo = new RecurringTransactionRepository(readContext, FakeUserContext.CreateDefault());
        var readAccountRepo = new AccountRepository(readContext, FakeUserContext.CreateDefault());
        var readTransactionRepo = new TransactionRepository(readContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);

        var projector = new RecurringInstanceProjector(readRecurringRepo, readAccountRepo);
        var service = new RecurringQueryService(readTransactionRepo, projector);

        var loadedRecurring = await readRecurringRepo.GetByIdAsync(recurring.Id);
        Assert.NotNull(loadedRecurring);

        var projected = await service.GetProjectedInstancesAsync(
            new List<RecurringTransaction> { loadedRecurring },
            RangeFrom,
            RangeTo);

        var actualProjectedCount = projected.Values.Sum(list => list.Count);

        // Assert — all 12 occurrences are projected since nothing is realized
        Assert.Equal(TotalOccurrences, actualProjectedCount);
    }

    /// <summary>
    /// When all 12 occurrences have been realized, projected output must be empty (zero projected).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task RecurringProjection_AllRealized_ProjectsNone()
    {
        // Arrange — account, recurring transaction, all 12 realized
        await using var context = _fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        var recurringRepo = new RecurringTransactionRepository(context, FakeUserContext.CreateDefault());
        var transactionRepo = new TransactionRepository(context, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);

        var account = Account.Create("Proj-Accuracy-All", AccountType.Checking);
        await accountRepo.AddAsync(account);

        var recurring = RecurringTransaction.Create(
            account.Id,
            "Weekly Groceries (all realized)",
            MoneyValue.Create("USD", -75m),
            RecurrencePatternValue.CreateWeekly(1, DayOfWeek.Monday),
            RangeFrom);
        await recurringRepo.AddAsync(recurring);
        await context.SaveChangesAsync();

        // Realize all 12 occurrences
        foreach (var instanceDate in AllMondays)
        {
            var tx = Transaction.CreateFromRecurring(
                account.Id,
                MoneyValue.Create("USD", -75m),
                instanceDate,
                "Weekly Groceries (all realized)",
                recurring.Id,
                instanceDate);
            await transactionRepo.AddAsync(tx);
        }

        await context.SaveChangesAsync();

        // Act
        await using var readContext = _fixture.CreateSharedContext(context);
        var readRecurringRepo = new RecurringTransactionRepository(readContext, FakeUserContext.CreateDefault());
        var readAccountRepo = new AccountRepository(readContext, FakeUserContext.CreateDefault());
        var readTransactionRepo = new TransactionRepository(readContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);

        var projector = new RecurringInstanceProjector(readRecurringRepo, readAccountRepo);
        var service = new RecurringQueryService(readTransactionRepo, projector);

        var loadedRecurring = await readRecurringRepo.GetByIdAsync(recurring.Id);
        Assert.NotNull(loadedRecurring);

        var projected = await service.GetProjectedInstancesAsync(
            new List<RecurringTransaction> { loadedRecurring },
            RangeFrom,
            RangeTo);

        var actualProjectedCount = projected.Values.Sum(list => list.Count);

        // Assert — zero projected when all occurrences are already realized
        Assert.Equal(0, actualProjectedCount);
    }
}
