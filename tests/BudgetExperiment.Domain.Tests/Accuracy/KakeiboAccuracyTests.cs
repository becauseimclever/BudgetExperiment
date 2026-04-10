// <copyright file="KakeiboAccuracyTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Tests.Accuracy;

/// <summary>
/// Accuracy tests verifying Kakeibo bucket assignment, override precedence,
/// and total-reconciliation invariants.
/// </summary>
[Trait("Category", "Accuracy")]
public class KakeiboAccuracyTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly DateOnly Jan1 = new(2026, 1, 1);

    [Fact]
    public void GetEffectiveKakeiboCategory_NoOverrideNoCategory_ReturnsWants()
    {
        var transaction = TransactionFactory.Create(AccountId, MoneyValue.Create("USD", -10m), Jan1, "Unknown");

        var bucket = transaction.GetEffectiveKakeiboCategory();

        Assert.Equal(KakeiboCategory.Wants, bucket);
    }

    [Fact]
    public void GetEffectiveKakeiboCategory_CategoryHasKakeibo_ReturnsCategoryBucket()
    {
        var category = BudgetCategory.Create("Groceries", CategoryType.Expense);
        category.SetKakeiboCategory(KakeiboCategory.Essentials);

        var transaction = TransactionFactory.Create(AccountId, MoneyValue.Create("USD", -50m), Jan1, "Groceries");
        SetCategory(transaction, category);

        var bucket = transaction.GetEffectiveKakeiboCategory();

        Assert.Equal(KakeiboCategory.Essentials, bucket);
    }

    [Fact]
    public void GetEffectiveKakeiboCategory_OverrideSet_IgnoresCategoryRouting()
    {
        var category = BudgetCategory.Create("Groceries", CategoryType.Expense);
        category.SetKakeiboCategory(KakeiboCategory.Essentials);

        var transaction = TransactionFactory.Create(AccountId, MoneyValue.Create("USD", -50m), Jan1, "Emergency groceries");
        SetCategory(transaction, category);
        transaction.SetKakeiboOverride(KakeiboCategory.Unexpected);

        var bucket = transaction.GetEffectiveKakeiboCategory();

        Assert.Equal(KakeiboCategory.Unexpected, bucket);
    }

    [Fact]
    public void GetEffectiveKakeiboCategory_OverrideWithNoCategory_ReturnsOverride()
    {
        var transaction = TransactionFactory.Create(AccountId, MoneyValue.Create("USD", -25m), Jan1, "Medical");
        transaction.SetKakeiboOverride(KakeiboCategory.Unexpected);

        var bucket = transaction.GetEffectiveKakeiboCategory();

        Assert.Equal(KakeiboCategory.Unexpected, bucket);
    }

    [Fact]
    public void GetEffectiveKakeiboCategory_ClearOverride_FallsBackToCategory()
    {
        var category = BudgetCategory.Create("Books", CategoryType.Expense);
        category.SetKakeiboCategory(KakeiboCategory.Culture);

        var transaction = TransactionFactory.Create(AccountId, MoneyValue.Create("USD", -15m), Jan1, "Novel");
        SetCategory(transaction, category);
        transaction.SetKakeiboOverride(KakeiboCategory.Wants);

        // Clear the override
        transaction.SetKakeiboOverride(null);

        var bucket = transaction.GetEffectiveKakeiboCategory();

        Assert.Equal(KakeiboCategory.Culture, bucket);
    }

    [Fact]
    public void KakeiboTotals_FourBuckets_EachSumIsExact()
    {
        // Arrange — one transaction per bucket
        var essentialsTx = CreateTransactionWithBucket(-100m, KakeiboCategory.Essentials);
        var wantsTx = CreateTransactionWithBucket(-60m, KakeiboCategory.Wants);
        var cultureTx = CreateTransactionWithBucket(-30m, KakeiboCategory.Culture);
        var unexpectedTx = CreateTransactionWithBucket(-10m, KakeiboCategory.Unexpected);

        var transactions = new[] { essentialsTx, wantsTx, cultureTx, unexpectedTx };

        // Act — group by effective bucket and sum
        var totals = transactions
            .GroupBy(t => t.GetEffectiveKakeiboCategory())
            .ToDictionary(g => g.Key, g => g.Sum(t => Math.Abs(t.Amount.Amount)));

        // Assert
        Assert.Equal(100m, totals[KakeiboCategory.Essentials]);
        Assert.Equal(60m, totals[KakeiboCategory.Wants]);
        Assert.Equal(30m, totals[KakeiboCategory.Culture]);
        Assert.Equal(10m, totals[KakeiboCategory.Unexpected]);
    }

    [Fact]
    public void KakeiboTotals_SumOfAllBuckets_EqualsTotalSpending()
    {
        // Arrange — multiple transactions spread across buckets
        var transactions = new[]
        {
            CreateTransactionWithBucket(-100m, KakeiboCategory.Essentials),
            CreateTransactionWithBucket(-50m, KakeiboCategory.Essentials),
            CreateTransactionWithBucket(-75m, KakeiboCategory.Wants),
            CreateTransactionWithBucket(-20m, KakeiboCategory.Culture),
            CreateTransactionWithBucket(-5m, KakeiboCategory.Unexpected),
        };

        // Act
        var grandTotal = transactions.Sum(t => Math.Abs(t.Amount.Amount));
        var bucketSum = transactions
            .GroupBy(t => t.GetEffectiveKakeiboCategory())
            .Sum(g => g.Sum(t => Math.Abs(t.Amount.Amount)));

        // Assert — bucket totals must reconcile with grand total
        Assert.Equal(grandTotal, bucketSum);
        Assert.Equal(250m, grandTotal);
    }

    [Fact]
    public void SetKakeiboCategory_OnExpenseCategory_SetsCorrectly()
    {
        var category = BudgetCategory.Create("Gas", CategoryType.Expense);

        category.SetKakeiboCategory(KakeiboCategory.Essentials);

        Assert.Equal(KakeiboCategory.Essentials, category.KakeiboCategory);
    }

    [Fact]
    public void SetKakeiboCategory_OnIncomeCategory_ThrowsDomainException()
    {
        var category = BudgetCategory.Create("Salary", CategoryType.Income);

        var ex = Assert.Throws<DomainException>(() =>
            category.SetKakeiboCategory(KakeiboCategory.Essentials));

        Assert.Contains("Expense", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void KakeiboTotals_MixedTransactions_WeeklySubsetSumsAreContainedInMonthlyTotal()
    {
        // Arrange — transactions across two weeks in January 2026
        var week1 = new[]
        {
            CreateTransactionWithOverrideAndDate(-30m, KakeiboCategory.Essentials, new DateOnly(2026, 1, 3)),
            CreateTransactionWithOverrideAndDate(-20m, KakeiboCategory.Wants, new DateOnly(2026, 1, 4)),
        };
        var week2 = new[]
        {
            CreateTransactionWithOverrideAndDate(-40m, KakeiboCategory.Essentials, new DateOnly(2026, 1, 10)),
            CreateTransactionWithOverrideAndDate(-10m, KakeiboCategory.Culture, new DateOnly(2026, 1, 11)),
        };

        var allTransactions = week1.Concat(week2).ToArray();

        // Act
        var week1Total = week1.Sum(t => Math.Abs(t.Amount.Amount));
        var week2Total = week2.Sum(t => Math.Abs(t.Amount.Amount));
        var monthlyTotal = allTransactions.Sum(t => Math.Abs(t.Amount.Amount));

        // Assert — weekly totals sum to monthly total
        Assert.Equal(monthlyTotal, week1Total + week2Total);
        Assert.Equal(100m, monthlyTotal);
    }

    private static Transaction CreateTransactionWithBucket(decimal amount, KakeiboCategory bucket)
    {
        var tx = TransactionFactory.Create(AccountId, MoneyValue.Create("USD", amount), Jan1, $"Tx {bucket}");
        tx.SetKakeiboOverride(bucket);
        return tx;
    }

    private static Transaction CreateTransactionWithOverrideAndDate(decimal amount, KakeiboCategory bucket, DateOnly date)
    {
        var tx = TransactionFactory.Create(AccountId, MoneyValue.Create("USD", amount), date, $"Tx {bucket}");
        tx.SetKakeiboOverride(bucket);
        return tx;
    }

    private static void SetCategory(Transaction transaction, BudgetCategory category)
    {
        typeof(Transaction)
            .GetProperty(nameof(Transaction.Category))!
            .SetValue(transaction, category);
    }
}
