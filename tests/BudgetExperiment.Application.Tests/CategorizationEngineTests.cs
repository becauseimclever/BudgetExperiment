// <copyright file="CategorizationEngineTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Services;
using BudgetExperiment.Domain;
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

    #region FindMatchingCategoryAsync Tests

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

    #endregion

    #region ApplyRulesAsync Tests

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

    #endregion

    #region TestPatternAsync Tests

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

    #endregion

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

        var unitOfWork = new Mock<IUnitOfWork>();

        var engine = new CategorizationEngine(ruleRepo.Object, transactionRepo.Object, unitOfWork.Object);
        return (engine, ruleRepo, transactionRepo);
    }
}
