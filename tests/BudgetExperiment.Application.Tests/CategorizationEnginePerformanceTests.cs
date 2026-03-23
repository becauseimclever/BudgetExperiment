// <copyright file="CategorizationEnginePerformanceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Diagnostics;

using BudgetExperiment.Domain;

using Microsoft.Extensions.Caching.Memory;

using Moq;

using Shouldly;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Performance regression tests for the <see cref="CategorizationEngine"/> to verify
/// that rule application scales efficiently with large datasets.
/// </summary>
public class CategorizationEnginePerformanceTests
{
    private static readonly string[] _sampleMerchants =
    [
        "WALMART", "TARGET", "AMAZON", "COSTCO", "KROGER",
        "WALGREENS", "CVS", "HOME DEPOT", "LOWES", "BEST BUY",
        "STARBUCKS", "MCDONALDS", "SUBWAY", "CHICK-FIL-A", "WENDYS",
        "SHELL", "EXXON", "BP", "CHEVRON", "MARATHON",
        "NETFLIX", "SPOTIFY", "HULU", "DISNEY", "APPLE",
    ];

    /// <summary>
    /// Verifies that applying 100 rules against 1000 transactions completes within
    /// a reasonable threshold (5 seconds), validating the performance optimizations
    /// including batch loading, rule caching, and string-first evaluation.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    [Trait("Category", "Performance")]
    public async Task ApplyRulesAsync_100Rules_1000Transactions_CompletesWithinThreshold()
    {
        // Arrange
        const int ruleCount = 100;
        const int transactionCount = 1000;
        const int thresholdMs = 5000;

        var categoryIds = Enumerable.Range(0, 20).Select(_ => Guid.NewGuid()).ToArray();
        var rules = CreateRules(ruleCount, categoryIds);
        var accountId = Guid.NewGuid();
        var transactions = CreateTransactions(transactionCount, accountId);

        var (engine, _, _) = CreateEngine(rules, transactions);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await engine.ApplyRulesAsync(transactions.Select(t => t.Id));
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(
            thresholdMs,
            $"Applying {ruleCount} rules to {transactionCount} transactions took {stopwatch.ElapsedMilliseconds}ms (threshold: {thresholdMs}ms)");

        result.TotalProcessed.ShouldBe(transactionCount);
        result.Errors.ShouldBe(0);
        (result.Categorized + result.Skipped).ShouldBe(transactionCount);
    }

    private static List<CategorizationRule> CreateRules(int count, Guid[] categoryIds)
    {
        var rules = new List<CategorizationRule>(count);

        for (var i = 0; i < count; i++)
        {
            var categoryId = categoryIds[i % categoryIds.Length];
            var merchant = _sampleMerchants[i % _sampleMerchants.Length];

            // 80% string rules, 20% regex rules
            if (i % 5 == 0)
            {
                rules.Add(CategorizationRule.Create(
                    $"Regex Rule {i}",
                    RuleMatchType.Regex,
                    $"{merchant}.*#{i}",
                    categoryId,
                    priority: i + 1));
            }
            else
            {
                var matchType = (i % 4) switch
                {
                    0 => RuleMatchType.Contains,
                    1 => RuleMatchType.StartsWith,
                    2 => RuleMatchType.EndsWith,
                    _ => RuleMatchType.Exact,
                };

                rules.Add(CategorizationRule.Create(
                    $"String Rule {i}",
                    matchType,
                    $"{merchant} #{i}",
                    categoryId,
                    priority: i + 1));
            }
        }

        return rules;
    }

    private static List<Transaction> CreateTransactions(int count, Guid accountId)
    {
        return Enumerable.Range(0, count)
            .Select(i => Transaction.Create(
                accountId,
                MoneyValue.Create("USD", -(i + 1)),
                new DateOnly(2026, 1, 1).AddDays(i % 365),
                $"{_sampleMerchants[i % _sampleMerchants.Length]} STORE #{i} PURCHASE"))
            .ToList();
    }

    private static (CategorizationEngine Engine, Mock<ICategorizationRuleRepository> RuleRepo, Mock<ITransactionRepository> TransactionRepo)
        CreateEngine(List<CategorizationRule> rules, List<Transaction> transactions)
    {
        var ruleRepo = new Mock<ICategorizationRuleRepository>();
        ruleRepo.Setup(r => r.GetActiveByPriorityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules.Where(r => r.IsActive).OrderBy(r => r.Priority).ToList());

        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Guid> ids, CancellationToken _) =>
                transactions.Where(t => ids.Contains(t.Id)).ToList());

        var unitOfWork = new Mock<IUnitOfWork>();
        var cache = new MemoryCache(new MemoryCacheOptions());

        var engine = new CategorizationEngine(ruleRepo.Object, transactionRepo.Object, unitOfWork.Object, cache);
        return (engine, ruleRepo, transactionRepo);
    }
}
