// <copyright file="KakeiboReportServiceAccuracyTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Reports;
using BudgetExperiment.Infrastructure.Persistence.Repositories;

using Microsoft.Extensions.Logging.Abstractions;

namespace BudgetExperiment.Infrastructure.Tests.Accuracy;

/// <summary>
/// Integration accuracy tests for <see cref="KakeiboReportService"/> backed by a real
/// PostgreSQL Testcontainer. These tests prove the financial invariants required by
/// CAT-8 (Kakeibo Allocation Accuracy) and INV-8 (Kakeibo Category Assignment Completeness).
///
/// Every expense transaction maps to exactly one bucket — no orphans, no double-counting.
/// Weekly sub-totals sum exactly to the monthly total (no decimal drift).
/// </summary>
[Collection("PostgreSqlDb")]
[Trait("Category", "Accuracy")]
public sealed class KakeiboReportServiceAccuracyTests
{
    private readonly PostgreSqlFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="KakeiboReportServiceAccuracyTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared PostgreSQL Testcontainer fixture.</param>
    public KakeiboReportServiceAccuracyTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AccuracyTest_EveryExpenseInRangeTracksToExactlyOneBucket_NoneOrphaned()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        var account = Account.Create("Kakeibo Accuracy Account", AccountType.Checking);
        var (categories, transactions) = BuildMixedExpenseTransactions(account);

        var categoryRepo = new BudgetCategoryRepository(context, FakeUserContext.CreateDefault());
        foreach (var cat in categories)
        {
            await categoryRepo.AddAsync(cat);
        }

        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        var from = new DateOnly(2040, 1, 1);
        var to = new DateOnly(2040, 1, 31);

        await using var readContext = _fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(readContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var service = new KakeiboReportService(transactionRepo);

        // Act
        var summary = await service.GetKakeiboSummaryAsync(from, to, null);

        // Assert — sum of all four monthly bucket totals == sum of all expense amounts (no orphans)
        var totalFromBuckets = summary.MonthlyTotals.Values.Sum();
        var totalExpenseAmount = transactions.Sum(t => Math.Abs(t.Amount.Amount));

        Assert.Equal(totalExpenseAmount, totalFromBuckets);

        // Every expense is accounted for in exactly one bucket (no double-counting)
        // — proved by the sum equality above given that BuildBucketTotals adds each tx once
        Assert.True(totalFromBuckets > 0m, "At least one expense must exist for this test to be meaningful.");
    }

    [Fact]
    public async Task AccuracyTest_KakeiboOverride_PrecedesCategory()
    {
        // Arrange — category says Essentials; override says Wants
        await using var context = _fixture.CreateContext();

        var account = Account.Create("Override Accuracy Account", AccountType.Checking);
        var essentialsCategory = BudgetCategory.Create("Rent Override Test", CategoryType.Expense);
        essentialsCategory.SetKakeiboCategory(KakeiboCategory.Essentials);

        var categoryRepo = new BudgetCategoryRepository(context, FakeUserContext.CreateDefault());
        await categoryRepo.AddAsync(essentialsCategory);

        var tx = account.AddTransaction(
            MoneyValue.Create("USD", -200m),
            new DateOnly(2041, 2, 15),
            "Rent with override",
            essentialsCategory.Id);
        tx.SetKakeiboOverride(KakeiboCategory.Wants);

        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        await using var readContext = _fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(readContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var service = new KakeiboReportService(transactionRepo);

        // Act
        var summary = await service.GetKakeiboSummaryAsync(
            new DateOnly(2041, 2, 1),
            new DateOnly(2041, 2, 28),
            null);

        // Assert — override wins: Wants gets 200, Essentials gets 0
        Assert.Equal(200m, summary.MonthlyTotals[KakeiboCategory.Wants]);
        Assert.Equal(0m, summary.MonthlyTotals[KakeiboCategory.Essentials]);
    }

    [Fact]
    public async Task AccuracyTest_WeeklyTotals_SumToMonthlyTotals()
    {
        // Arrange — transactions spread across four ISO weeks in March 2042
        await using var context = _fixture.CreateContext();

        var account = Account.Create("Weekly Sum Accuracy Account", AccountType.Checking);
        var essentials = BudgetCategory.Create("Weekly-Essentials", CategoryType.Expense);
        essentials.SetKakeiboCategory(KakeiboCategory.Essentials);
        var wants = BudgetCategory.Create("Weekly-Wants", CategoryType.Expense);
        wants.SetKakeiboCategory(KakeiboCategory.Wants);

        var categoryRepo = new BudgetCategoryRepository(context, FakeUserContext.CreateDefault());
        await categoryRepo.AddAsync(essentials);
        await categoryRepo.AddAsync(wants);

        // Create 8 transactions across 4 ISO weeks in March 2042
        var dates = new[]
        {
            new DateOnly(2042, 3, 3),   // Week 1 (Mon)
            new DateOnly(2042, 3, 5),   // Week 1
            new DateOnly(2042, 3, 10),  // Week 2
            new DateOnly(2042, 3, 12),  // Week 2
            new DateOnly(2042, 3, 17),  // Week 3
            new DateOnly(2042, 3, 19),  // Week 3
            new DateOnly(2042, 3, 24),  // Week 4
            new DateOnly(2042, 3, 26),  // Week 4
        };

        for (var i = 0; i < dates.Length; i++)
        {
            var cat = i % 2 == 0 ? essentials : wants;
            account.AddTransaction(MoneyValue.Create("USD", -(50m + (i * 7m))), dates[i], $"TX-{i}", cat.Id);
        }

        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        await using var readContext = _fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(readContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var service = new KakeiboReportService(transactionRepo);

        // Act
        var summary = await service.GetKakeiboSummaryAsync(
            new DateOnly(2042, 3, 1),
            new DateOnly(2042, 3, 31),
            null);

        // Assert — for each bucket: sum of weekly totals == monthly total (no decimal drift)
        foreach (var bucket in new[] { KakeiboCategory.Essentials, KakeiboCategory.Wants, KakeiboCategory.Culture, KakeiboCategory.Unexpected })
        {
            var weeklySum = summary.WeeklyTotals.Sum(w => w.BucketTotals[bucket]);
            Assert.Equal(
                summary.MonthlyTotals[bucket],
                weeklySum);
        }
    }

    [Fact]
    public async Task AccuracyTest_IncomeAndTransferExcluded_FromAllBuckets()
    {
        // Arrange — mix of expense, income, and transfer transactions
        await using var context = _fixture.CreateContext();

        var account = Account.Create("Exclusion Accuracy Account", AccountType.Checking);
        var expenseCategory = BudgetCategory.Create("Exclusion-Expense", CategoryType.Expense);
        expenseCategory.SetKakeiboCategory(KakeiboCategory.Culture);
        var incomeCategory = BudgetCategory.Create("Exclusion-Income", CategoryType.Income);

        var categoryRepo = new BudgetCategoryRepository(context, FakeUserContext.CreateDefault());
        await categoryRepo.AddAsync(expenseCategory);
        await categoryRepo.AddAsync(incomeCategory);

        // Two expense transactions (should appear in Culture bucket)
        account.AddTransaction(MoneyValue.Create("USD", -30m), new DateOnly(2043, 4, 5), "Book", expenseCategory.Id);
        account.AddTransaction(MoneyValue.Create("USD", -20m), new DateOnly(2043, 4, 10), "Museum", expenseCategory.Id);

        // One income transaction (should be excluded)
        account.AddTransaction(MoneyValue.Create("USD", 3000m), new DateOnly(2043, 4, 1), "Salary", incomeCategory.Id);

        // One transfer transaction (no category → Category?.Type != Expense → excluded)
        var transferTx = Transaction.CreateTransfer(
            account.Id,
            MoneyValue.Create("USD", -500m),
            new DateOnly(2043, 4, 15),
            "Transfer Out",
            Guid.NewGuid(),
            TransferDirection.Source);

        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        await accountRepo.AddAsync(account);
        context.Transactions.Add(transferTx);
        await context.SaveChangesAsync();

        await using var readContext = _fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(readContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var service = new KakeiboReportService(transactionRepo);

        // Act
        var summary = await service.GetKakeiboSummaryAsync(
            new DateOnly(2043, 4, 1),
            new DateOnly(2043, 4, 30),
            null);

        // Assert — only the two expense transactions appear; total is exactly 50
        var totalBuckets = summary.MonthlyTotals.Values.Sum();
        Assert.Equal(50m, totalBuckets);
        Assert.Equal(50m, summary.MonthlyTotals[KakeiboCategory.Culture]);
        Assert.Equal(0m, summary.MonthlyTotals[KakeiboCategory.Essentials]);
        Assert.Equal(0m, summary.MonthlyTotals[KakeiboCategory.Wants]);
        Assert.Equal(0m, summary.MonthlyTotals[KakeiboCategory.Unexpected]);
    }

    // ===== Helpers =====

    /// <summary>
    /// Builds 12 expense transactions across all four Kakeibo buckets in January 2040.
    /// Returns categories (for seeding) and the created transactions (for assertion).
    /// </summary>
    private static (List<BudgetCategory> Categories, List<Transaction> Transactions) BuildMixedExpenseTransactions(Account account)
    {
        var essentials = BudgetCategory.Create("Acc-Essentials", CategoryType.Expense);
        essentials.SetKakeiboCategory(KakeiboCategory.Essentials);
        var wants = BudgetCategory.Create("Acc-Wants", CategoryType.Expense);
        wants.SetKakeiboCategory(KakeiboCategory.Wants);
        var culture = BudgetCategory.Create("Acc-Culture", CategoryType.Expense);
        culture.SetKakeiboCategory(KakeiboCategory.Culture);
        var unexpected = BudgetCategory.Create("Acc-Unexpected", CategoryType.Expense);
        unexpected.SetKakeiboCategory(KakeiboCategory.Unexpected);

        var categories = new List<BudgetCategory> { essentials, wants, culture, unexpected };
        var transactions = new List<Transaction>();

        // 3 essentials, 3 wants, 3 culture, 3 unexpected
        var buckets = new[] { essentials, wants, culture, unexpected };
        var amounts = new[] { 100m, 50m, 30m };
        var baseDays = new[] { 3, 8, 15, 22 };

        for (var bi = 0; bi < buckets.Length; bi++)
        {
            for (var ai = 0; ai < amounts.Length; ai++)
            {
                var date = new DateOnly(2040, 1, baseDays[bi] + ai);
                var tx = account.AddTransaction(
                    MoneyValue.Create("USD", -amounts[ai]),
                    date,
                    $"Acc-{buckets[bi].Name}-{ai}",
                    buckets[bi].Id);
                transactions.Add(tx);
            }
        }

        return (categories, transactions);
    }
}
