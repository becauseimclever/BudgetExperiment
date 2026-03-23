// <copyright file="CategorizationEngineTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

using Microsoft.Extensions.Caching.Memory;

using Moq;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Unit tests for the CategorizationEngine service.
/// </summary>
public class CategorizationEngineTests
{
    private static readonly Guid GroceryCategoryId = Guid.NewGuid();
    private static readonly Guid TransportCategoryId = Guid.NewGuid();
    private static readonly Guid EntertainmentCategoryId = Guid.NewGuid();

    [Fact]
    public async Task FindMatchingCategoryAsync_Returns_CategoryId_When_Rule_Matches()
    {
        // Arrange
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create("Walmart", RuleMatchType.Contains, "WALMART", GroceryCategoryId, priority: 1),
        };
        var (engine, _, _) = CreateEngine(rules);

        // Act
        var result = await engine.FindMatchingCategoryAsync("WALMART STORE #123");

        // Assert
        Assert.Equal(GroceryCategoryId, result);
    }

    [Fact]
    public async Task FindMatchingCategoryAsync_Returns_Null_When_No_Rule_Matches()
    {
        // Arrange
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create("Walmart", RuleMatchType.Contains, "WALMART", GroceryCategoryId, priority: 1),
        };
        var (engine, _, _) = CreateEngine(rules);

        // Act
        var result = await engine.FindMatchingCategoryAsync("TARGET STORE #456");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task FindMatchingCategoryAsync_Returns_First_Matching_Rule_By_Priority()
    {
        // Arrange - lower priority number = higher priority (evaluated first)
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create("General Store", RuleMatchType.Contains, "STORE", GroceryCategoryId, priority: 10),
            CategorizationRule.Create("Walmart Specific", RuleMatchType.Contains, "WALMART", TransportCategoryId, priority: 1),
        };
        var (engine, _, _) = CreateEngine(rules);

        // Act - "WALMART STORE" matches both, but "Walmart Specific" has lower priority number
        var result = await engine.FindMatchingCategoryAsync("WALMART STORE #123");

        // Assert
        Assert.Equal(TransportCategoryId, result);
    }

    [Fact]
    public async Task FindMatchingCategoryAsync_Returns_Null_For_Empty_Description()
    {
        // Arrange
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create("Walmart", RuleMatchType.Contains, "WALMART", GroceryCategoryId, priority: 1),
        };
        var (engine, _, _) = CreateEngine(rules);

        // Act
        var result = await engine.FindMatchingCategoryAsync(string.Empty);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task FindMatchingCategoryAsync_Returns_Null_When_No_Rules_Exist()
    {
        // Arrange
        var (engine, _, _) = CreateEngine(new List<CategorizationRule>());

        // Act
        var result = await engine.FindMatchingCategoryAsync("WALMART STORE");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ApplyRulesAsync_Categorizes_Uncategorized_Transactions()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var transactions = new List<Transaction>
        {
            Transaction.Create(accountId, MoneyValue.Create("USD", -50m), new DateOnly(2026, 1, 15), "WALMART STORE #123"),
            Transaction.Create(accountId, MoneyValue.Create("USD", -25m), new DateOnly(2026, 1, 16), "UBER TRIP"),
        };
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create("Walmart", RuleMatchType.Contains, "WALMART", GroceryCategoryId, priority: 1),
            CategorizationRule.Create("Uber", RuleMatchType.Contains, "UBER", TransportCategoryId, priority: 2),
        };
        var (engine, _, _) = CreateEngine(rules, transactions);

        // Act
        var result = await engine.ApplyRulesAsync(transactions.Select(t => t.Id));

        // Assert
        Assert.Equal(2, result.TotalProcessed);
        Assert.Equal(2, result.Categorized);
        Assert.Equal(0, result.Skipped);
        Assert.Equal(0, result.Errors);
        Assert.Equal(GroceryCategoryId, transactions[0].CategoryId);
        Assert.Equal(TransportCategoryId, transactions[1].CategoryId);
    }

    [Fact]
    public async Task ApplyRulesAsync_Skips_Already_Categorized_When_OverwriteExisting_False()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var transaction = Transaction.Create(accountId, MoneyValue.Create("USD", -50m), new DateOnly(2026, 1, 15), "WALMART STORE", EntertainmentCategoryId);
        var transactions = new List<Transaction> { transaction };
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create("Walmart", RuleMatchType.Contains, "WALMART", GroceryCategoryId, priority: 1),
        };
        var (engine, _, _) = CreateEngine(rules, transactions);

        // Act
        var result = await engine.ApplyRulesAsync(new[] { transaction.Id }, overwriteExisting: false);

        // Assert
        Assert.Equal(1, result.TotalProcessed);
        Assert.Equal(0, result.Categorized);
        Assert.Equal(1, result.Skipped);
        Assert.Equal(EntertainmentCategoryId, transaction.CategoryId); // Unchanged
    }

    [Fact]
    public async Task ApplyRulesAsync_Overwrites_When_OverwriteExisting_True()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var transaction = Transaction.Create(accountId, MoneyValue.Create("USD", -50m), new DateOnly(2026, 1, 15), "WALMART STORE", EntertainmentCategoryId);
        var transactions = new List<Transaction> { transaction };
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create("Walmart", RuleMatchType.Contains, "WALMART", GroceryCategoryId, priority: 1),
        };
        var (engine, _, _) = CreateEngine(rules, transactions);

        // Act
        var result = await engine.ApplyRulesAsync(new[] { transaction.Id }, overwriteExisting: true);

        // Assert
        Assert.Equal(1, result.TotalProcessed);
        Assert.Equal(1, result.Categorized);
        Assert.Equal(0, result.Skipped);
        Assert.Equal(GroceryCategoryId, transaction.CategoryId); // Changed
    }

    [Fact]
    public async Task ApplyRulesAsync_Skips_When_No_Rule_Matches()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var transaction = Transaction.Create(accountId, MoneyValue.Create("USD", -50m), new DateOnly(2026, 1, 15), "RANDOM STORE");
        var transactions = new List<Transaction> { transaction };
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create("Walmart", RuleMatchType.Contains, "WALMART", GroceryCategoryId, priority: 1),
        };
        var (engine, _, _) = CreateEngine(rules, transactions);

        // Act
        var result = await engine.ApplyRulesAsync(new[] { transaction.Id });

        // Assert
        Assert.Equal(1, result.TotalProcessed);
        Assert.Equal(0, result.Categorized);
        Assert.Equal(1, result.Skipped);
        Assert.Null(transaction.CategoryId);
    }

    [Fact]
    public async Task ApplyRulesAsync_With_Null_TransactionIds_Processes_All_Uncategorized()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var uncategorized1 = Transaction.Create(accountId, MoneyValue.Create("USD", -50m), new DateOnly(2026, 1, 15), "WALMART STORE");
        var uncategorized2 = Transaction.Create(accountId, MoneyValue.Create("USD", -25m), new DateOnly(2026, 1, 16), "UBER TRIP");
        var categorized = Transaction.Create(accountId, MoneyValue.Create("USD", -30m), new DateOnly(2026, 1, 17), "NETFLIX", EntertainmentCategoryId);
        var allTransactions = new List<Transaction> { uncategorized1, uncategorized2, categorized };
        var uncategorizedTransactions = new List<Transaction> { uncategorized1, uncategorized2 };

        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create("Walmart", RuleMatchType.Contains, "WALMART", GroceryCategoryId, priority: 1),
            CategorizationRule.Create("Uber", RuleMatchType.Contains, "UBER", TransportCategoryId, priority: 2),
        };
        var (engine, _, transactionRepo) = CreateEngine(rules, allTransactions);

        // Setup mock to return uncategorized transactions when null is passed
        transactionRepo.Setup(r => r.GetUncategorizedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(uncategorizedTransactions);

        // Act
        var result = await engine.ApplyRulesAsync(null);

        // Assert
        Assert.Equal(2, result.TotalProcessed);
        Assert.Equal(2, result.Categorized);
    }

    [Fact]
    public async Task TestPatternAsync_Returns_Matching_Descriptions()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var transactions = new List<Transaction>
        {
            Transaction.Create(accountId, MoneyValue.Create("USD", -50m), new DateOnly(2026, 1, 15), "WALMART STORE #123"),
            Transaction.Create(accountId, MoneyValue.Create("USD", -25m), new DateOnly(2026, 1, 16), "WALMART GROCERY"),
            Transaction.Create(accountId, MoneyValue.Create("USD", -30m), new DateOnly(2026, 1, 17), "TARGET STORE"),
        };
        var (engine, _, transactionRepo) = CreateEngine(new List<CategorizationRule>(), transactions);

        transactionRepo.Setup(r => r.GetAllDescriptionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions.Select(t => t.Description).ToList());

        // Act
        var result = await engine.TestPatternAsync(RuleMatchType.Contains, "WALMART", caseSensitive: false, limit: 10);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("WALMART STORE #123", result);
        Assert.Contains("WALMART GROCERY", result);
    }

    [Fact]
    public async Task TestPatternAsync_Respects_Limit()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var transactions = new List<Transaction>
        {
            Transaction.Create(accountId, MoneyValue.Create("USD", -50m), new DateOnly(2026, 1, 15), "WALMART 1"),
            Transaction.Create(accountId, MoneyValue.Create("USD", -25m), new DateOnly(2026, 1, 16), "WALMART 2"),
            Transaction.Create(accountId, MoneyValue.Create("USD", -30m), new DateOnly(2026, 1, 17), "WALMART 3"),
        };
        var (engine, _, transactionRepo) = CreateEngine(new List<CategorizationRule>(), transactions);

        transactionRepo.Setup(r => r.GetAllDescriptionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions.Select(t => t.Description).ToList());

        // Act
        var result = await engine.TestPatternAsync(RuleMatchType.Contains, "WALMART", caseSensitive: false, limit: 2);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task TestPatternAsync_Returns_Empty_When_No_Matches()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var transactions = new List<Transaction>
        {
            Transaction.Create(accountId, MoneyValue.Create("USD", -50m), new DateOnly(2026, 1, 15), "TARGET STORE"),
        };
        var (engine, _, transactionRepo) = CreateEngine(new List<CategorizationRule>(), transactions);

        transactionRepo.Setup(r => r.GetAllDescriptionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions.Select(t => t.Description).ToList());

        // Act
        var result = await engine.TestPatternAsync(RuleMatchType.Contains, "WALMART", caseSensitive: false, limit: 10);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task TestPatternAsync_Respects_CaseSensitivity()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var transactions = new List<Transaction>
        {
            Transaction.Create(accountId, MoneyValue.Create("USD", -50m), new DateOnly(2026, 1, 15), "WALMART STORE"),
            Transaction.Create(accountId, MoneyValue.Create("USD", -25m), new DateOnly(2026, 1, 16), "walmart store"),
        };
        var (engine, _, transactionRepo) = CreateEngine(new List<CategorizationRule>(), transactions);

        transactionRepo.Setup(r => r.GetAllDescriptionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions.Select(t => t.Description).ToList());

        // Act
        var result = await engine.TestPatternAsync(RuleMatchType.Contains, "WALMART", caseSensitive: true, limit: 10);

        // Assert
        Assert.Single(result);
        Assert.Equal("WALMART STORE", result[0]);
    }

    [Fact]
    public async Task GetBatchSuggestionsAsync_Returns_Empty_When_No_TransactionIds()
    {
        // Arrange
        var (engine, _, _) = CreateEngine(new List<CategorizationRule>());

        // Act
        var result = await engine.GetBatchSuggestionsAsync([]);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetBatchSuggestionsAsync_Returns_Empty_When_No_Rules_Exist()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var transaction = Transaction.Create(accountId, MoneyValue.Create("USD", -25m), new DateOnly(2026, 1, 15), "WALMART STORE");
        var (engine, _, _) = CreateEngine(new List<CategorizationRule>(), new List<Transaction> { transaction });

        // Act
        var result = await engine.GetBatchSuggestionsAsync([transaction.Id]);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetBatchSuggestionsAsync_Returns_Suggestion_When_Rule_Matches()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var transaction = Transaction.Create(accountId, MoneyValue.Create("USD", -25m), new DateOnly(2026, 1, 15), "WALMART STORE #123");
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create("Grocery Rule", RuleMatchType.Contains, "WALMART", GroceryCategoryId, priority: 1),
        };
        var (engine, _, _) = CreateEngine(rules, new List<Transaction> { transaction });

        // Act
        var result = await engine.GetBatchSuggestionsAsync([transaction.Id]);

        // Assert
        Assert.Single(result);
        Assert.True(result.ContainsKey(transaction.Id));
        Assert.Equal(GroceryCategoryId, result[transaction.Id].CategoryId);
        Assert.Equal(transaction.Id, result[transaction.Id].TransactionId);
    }

    [Fact]
    public async Task GetBatchSuggestionsAsync_Skips_Already_Categorized_Transactions()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var transaction = Transaction.Create(accountId, MoneyValue.Create("USD", -25m), new DateOnly(2026, 1, 15), "WALMART STORE");
        transaction.UpdateCategory(Guid.NewGuid()); // Already categorized
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create("Grocery Rule", RuleMatchType.Contains, "WALMART", GroceryCategoryId, priority: 1),
        };
        var (engine, _, _) = CreateEngine(rules, new List<Transaction> { transaction });

        // Act
        var result = await engine.GetBatchSuggestionsAsync([transaction.Id]);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetBatchSuggestionsAsync_Skips_Transactions_Without_Matching_Rules()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var transaction = Transaction.Create(accountId, MoneyValue.Create("USD", -25m), new DateOnly(2026, 1, 15), "TARGET STORE");
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create("Walmart", RuleMatchType.Contains, "WALMART", GroceryCategoryId, priority: 1),
        };
        var (engine, _, _) = CreateEngine(rules, new List<Transaction> { transaction });

        // Act
        var result = await engine.GetBatchSuggestionsAsync([transaction.Id]);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetBatchSuggestionsAsync_Returns_Multiple_Suggestions_For_Multiple_Transactions()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var txn1 = Transaction.Create(accountId, MoneyValue.Create("USD", -25m), new DateOnly(2026, 1, 15), "WALMART STORE");
        var txn2 = Transaction.Create(accountId, MoneyValue.Create("USD", -15m), new DateOnly(2026, 1, 16), "UBER TRIP");
        var txn3 = Transaction.Create(accountId, MoneyValue.Create("USD", -50m), new DateOnly(2026, 1, 17), "RANDOM STORE");
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create("Grocery", RuleMatchType.Contains, "WALMART", GroceryCategoryId, priority: 1),
            CategorizationRule.Create("Transport", RuleMatchType.Contains, "UBER", TransportCategoryId, priority: 2),
        };
        var transactions = new List<Transaction> { txn1, txn2, txn3 };
        var (engine, _, _) = CreateEngine(rules, transactions);

        // Act
        var result = await engine.GetBatchSuggestionsAsync([txn1.Id, txn2.Id, txn3.Id]);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(GroceryCategoryId, result[txn1.Id].CategoryId);
        Assert.Equal(TransportCategoryId, result[txn2.Id].CategoryId);
        Assert.False(result.ContainsKey(txn3.Id));
    }

    [Fact]
    public async Task GetBatchSuggestionsAsync_Skips_Unknown_TransactionIds()
    {
        // Arrange
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create("Grocery", RuleMatchType.Contains, "WALMART", GroceryCategoryId, priority: 1),
        };
        var (engine, _, _) = CreateEngine(rules);

        // Act
        var result = await engine.GetBatchSuggestionsAsync([Guid.NewGuid()]);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ApplyRulesAsync_Uses_GetByIdsAsync_Instead_Of_N_Plus_1()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var transactions = new List<Transaction>
        {
            Transaction.Create(accountId, MoneyValue.Create("USD", -50m), new DateOnly(2026, 1, 15), "WALMART STORE"),
            Transaction.Create(accountId, MoneyValue.Create("USD", -25m), new DateOnly(2026, 1, 16), "UBER TRIP"),
        };
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create("Walmart", RuleMatchType.Contains, "WALMART", GroceryCategoryId, priority: 1),
        };
        var (engine, _, transactionRepo) = CreateEngine(rules, transactions);

        // Act
        await engine.ApplyRulesAsync(transactions.Select(t => t.Id));

        // Assert - GetByIdsAsync called once (batch), GetByIdAsync never called
        transactionRepo.Verify(r => r.GetByIdsAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
        transactionRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetBatchSuggestionsAsync_Uses_GetByIdsAsync_Instead_Of_N_Plus_1()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var txn1 = Transaction.Create(accountId, MoneyValue.Create("USD", -25m), new DateOnly(2026, 1, 15), "WALMART STORE");
        var txn2 = Transaction.Create(accountId, MoneyValue.Create("USD", -15m), new DateOnly(2026, 1, 16), "UBER TRIP");
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create("Grocery", RuleMatchType.Contains, "WALMART", GroceryCategoryId, priority: 1),
        };
        var transactions = new List<Transaction> { txn1, txn2 };
        var (engine, _, transactionRepo) = CreateEngine(rules, transactions);

        // Act
        await engine.GetBatchSuggestionsAsync([txn1.Id, txn2.Id]);

        // Assert - GetByIdsAsync called once (batch), GetByIdAsync never called
        transactionRepo.Verify(r => r.GetByIdsAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
        transactionRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FindMatchingCategoryAsync_Uses_Cached_Rules_On_Second_Call()
    {
        // Arrange
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create("Walmart", RuleMatchType.Contains, "WALMART", GroceryCategoryId, priority: 1),
        };
        var (engine, ruleRepo, _) = CreateEngine(rules);

        // Act - call twice
        await engine.FindMatchingCategoryAsync("WALMART STORE #1");
        await engine.FindMatchingCategoryAsync("WALMART STORE #2");

        // Assert - repository only queried once (second call hits cache)
        ruleRepo.Verify(r => r.GetActiveByPriorityAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvalidateRuleCache_Forces_Fresh_Load_On_Next_Call()
    {
        // Arrange
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create("Walmart", RuleMatchType.Contains, "WALMART", GroceryCategoryId, priority: 1),
        };
        var (engine, ruleRepo, _) = CreateEngine(rules);

        // Act - first call populates cache
        await engine.FindMatchingCategoryAsync("WALMART STORE");
        engine.InvalidateRuleCache();
        await engine.FindMatchingCategoryAsync("WALMART STORE");

        // Assert - repository queried twice (cache invalidated between calls)
        ruleRepo.Verify(r => r.GetActiveByPriorityAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ApplyRulesAsync_Evaluates_StringRules_Before_RegexRules()
    {
        // Arrange: regex rule has HIGHER priority (lower number) but string rules should be tried first within each group
        // Both rules match "WALMART STORE", but the regex rule has lower priority number and would normally win.
        // With partitioned evaluation, string rules are checked before regex, but we still respect priority within each group.
        // This test verifies that when a string rule matches, the regex rule is never evaluated.
        var accountId = Guid.NewGuid();
        var transaction = Transaction.Create(accountId, MoneyValue.Create("USD", -50m), new DateOnly(2026, 1, 15), "WALMART STORE");
        var containsRule = CategorizationRule.Create("Walmart Contains", RuleMatchType.Contains, "WALMART", GroceryCategoryId, priority: 10);
        var regexRule = CategorizationRule.Create("Walmart Regex", RuleMatchType.Regex, "WALMART.*STORE", TransportCategoryId, priority: 5);

        var rules = new List<CategorizationRule> { regexRule, containsRule }; // regex first by priority
        var (engine, _, _) = CreateEngine(rules, new List<Transaction> { transaction });

        // Act
        var result = await engine.ApplyRulesAsync(new[] { transaction.Id });

        // Assert - string rule matched first even though regex has higher priority
        Assert.Equal(1, result.Categorized);
        Assert.Equal(GroceryCategoryId, transaction.CategoryId);
    }

    [Fact]
    public async Task ApplyRulesAsync_MultipleCalls_UsesCachedRules()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create("Walmart", RuleMatchType.Contains, "WALMART", categoryId, priority: 1),
        };
        var accountId = Guid.NewGuid();
        var t1 = Transaction.Create(accountId, MoneyValue.Create("USD", -10m), new DateOnly(2026, 1, 1), "WALMART #1");
        var t2 = Transaction.Create(accountId, MoneyValue.Create("USD", -20m), new DateOnly(2026, 1, 2), "WALMART #2");
        var (engine, ruleRepo, _) = CreateEngine(rules, new List<Transaction> { t1, t2 });

        // Act — apply rules twice on the same engine instance
        await engine.ApplyRulesAsync(new[] { t1.Id });
        await engine.ApplyRulesAsync(new[] { t2.Id });

        // Assert — rule repository queried only once; second call used cached rules
        ruleRepo.Verify(r => r.GetActiveByPriorityAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyRulesAsync_StringRuleMatchesAllTransactions_RegexRuleNeverApplied()
    {
        // Arrange — 500 transactions all matching a Contains rule; regex rule has higher priority
        // but string rules are always evaluated before regex rules
        var categoryId = Guid.NewGuid();
        var containsRule = CategorizationRule.Create(
            "Walmart Contains", RuleMatchType.Contains, "WALMART", categoryId, priority: 10);
        var regexRule = CategorizationRule.Create(
            "Walmart Regex", RuleMatchType.Regex, "WALMART.*", Guid.NewGuid(), priority: 5);

        var accountId = Guid.NewGuid();
        var transactions = Enumerable.Range(0, 500)
            .Select(i => Transaction.Create(
                accountId,
                MoneyValue.Create("USD", -(i + 1)),
                new DateOnly(2026, 1, 15),
                $"WALMART STORE #{i}"))
            .ToList();

        var (engine, _, _) = CreateEngine([containsRule, regexRule], transactions);

        // Act
        var result = await engine.ApplyRulesAsync(transactions.Select(t => t.Id));

        // Assert — all 500 categorized by the string Contains rule, not the regex rule
        Assert.Equal(500, result.Categorized);
        Assert.All(transactions, t => Assert.Equal(categoryId, t.CategoryId));
    }

    private static (CategorizationEngine Engine, Mock<ICategorizationRuleRepository> RuleRepo, Mock<ITransactionRepository> TransactionRepo)
        CreateEngine(List<CategorizationRule> rules, List<Transaction>? transactions = null)
    {
        var ruleRepo = new Mock<ICategorizationRuleRepository>();
        ruleRepo.Setup(r => r.GetActiveByPriorityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules.Where(r => r.IsActive).OrderBy(r => r.Priority).ToList());

        var transactionRepo = new Mock<ITransactionRepository>();
        transactions ??= new List<Transaction>();

        foreach (var transaction in transactions)
        {
            transactionRepo.Setup(r => r.GetByIdAsync(transaction.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(transaction);
        }

        // Setup batch loading for GetByIdsAsync
        transactionRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Guid> ids, CancellationToken _) =>
                transactions.Where(t => ids.Contains(t.Id)).ToList());

        var unitOfWork = new Mock<IUnitOfWork>();
        var cache = new MemoryCache(new MemoryCacheOptions());

        var engine = new CategorizationEngine(ruleRepo.Object, transactionRepo.Object, unitOfWork.Object, cache);
        return (engine, ruleRepo, transactionRepo);
    }
}
