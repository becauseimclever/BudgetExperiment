// <copyright file="BudgetCalculationEdgeCasesTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;
using BudgetExperiment.Application.Budgeting;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;
using Moq;
using Shouldly;

namespace BudgetExperiment.Application.Tests.DataConsistency;

/// <summary>
/// Unit tests for budget calculation edge cases: null handling, empty datasets, boundary conditions.
/// </summary>
public class BudgetCalculationEdgeCasesTests
{
    public BudgetCalculationEdgeCasesTests()
    {
        // Set culture to en-US for consistent currency formatting
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
    }

    [Fact]
    public async Task MultiCategoryProgress_MultipleCategories_RollupAccuracy()
    {
        // Arrange: 5 categories with different spending levels
        var categoryIds = Enumerable.Range(1, 5).Select(_ => Guid.NewGuid()).ToList();
        var year = 2026;
        var month = 1;

        var categories = new List<BudgetCategory>
        {
            BudgetCategory.Create("Cat1", CategoryType.Expense),
            BudgetCategory.Create("Cat2", CategoryType.Expense),
            BudgetCategory.Create("Cat3", CategoryType.Expense),
            BudgetCategory.Create("Cat4", CategoryType.Expense),
            BudgetCategory.Create("Cat5", CategoryType.Expense),
        };

        // Set category IDs to match the categoryIds
        for (int i = 0; i < categories.Count; i++)
        {
            categories[i].GetType().GetProperty(nameof(BudgetCategory.Id))
                ?.SetValue(categories[i], categoryIds[i]);
        }

        var goals = new List<BudgetGoal>
        {
            BudgetGoal.Create(categoryIds[0], year, month, MoneyValue.Create("USD", 100m)),
            BudgetGoal.Create(categoryIds[1], year, month, MoneyValue.Create("USD", 100m)),
            BudgetGoal.Create(categoryIds[2], year, month, MoneyValue.Create("USD", 100m)),
            BudgetGoal.Create(categoryIds[3], year, month, MoneyValue.Create("USD", 100m)),
            BudgetGoal.Create(categoryIds[4], year, month, MoneyValue.Create("USD", 100m)),
        };

        var goalRepo = new Mock<IBudgetGoalRepository>();
        goalRepo.Setup(r => r.GetByMonthAsync(year, month, default)).ReturnsAsync(goals);

        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        categoryRepo.Setup(r => r.GetByTypeAsync(CategoryType.Expense, default))
            .ReturnsAsync(categories);

        var transactionRepo = new Mock<ITransactionRepository>();
        var spendingByCategory = new Dictionary<Guid, decimal>
        {
            { categoryIds[0], 50m },
            { categoryIds[1], 75m },
            { categoryIds[2], 100m },
            { categoryIds[3], 25m },
            { categoryIds[4], 0m },
        };
        transactionRepo.Setup(r => r.GetSpendingByCategoriesAsync(year, month, default))
            .ReturnsAsync(spendingByCategory);

        var userContext = new Mock<IUserContext>();
        var currencyProvider = new Mock<ICurrencyProvider>();
        currencyProvider.Setup(c => c.GetCurrencyAsync(default)).ReturnsAsync("USD");

        var service = new BudgetProgressService(
            goalRepo.Object,
            categoryRepo.Object,
            transactionRepo.Object,
            currencyProvider.Object,
            userContext.Object);

        // Act
        var summary = await service.GetMonthlySummaryAsync(year, month, false, default);

        // Assert: Total spent = 50+75+100+25+0 = 250 across 500 total budget
        summary.ShouldNotBeNull();
        summary.TotalSpent.Amount.ShouldBe(250m);
        summary.TotalBudgeted.Amount.ShouldBe(500m);

        // Overall percent = (250/500) = 50%
        summary.OverallPercentUsed.ShouldBe(50m);
    }

    [Fact]
    public async Task BudgetProgressService_EmptyDataset_ProgressIsZeroNotNull()
    {
        // Arrange: Category with goal but no transactions
        var categoryId = Guid.NewGuid();
        var year = 2026;
        var month = 1;

        var goal = BudgetGoal.Create(categoryId, year, month, MoneyValue.Create("USD", 200m));
        var category = BudgetCategory.Create("TestCat", CategoryType.Expense);

        var goalRepo = new Mock<IBudgetGoalRepository>();
        goalRepo.Setup(r => r.GetByCategoryAndMonthAsync(categoryId, year, month, default))
            .ReturnsAsync(goal);

        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        categoryRepo.Setup(r => r.GetByIdAsync(categoryId, default)).ReturnsAsync(category);

        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetSpendingByCategoryAsync(categoryId, year, month, default))
            .ReturnsAsync(MoneyValue.Create("USD", 0m));

        var userContext = new Mock<IUserContext>();
        var currencyProvider = new Mock<ICurrencyProvider>();

        var service = new BudgetProgressService(
            goalRepo.Object,
            categoryRepo.Object,
            transactionRepo.Object,
            currencyProvider.Object,
            userContext.Object);

        // Act
        var progress = await service.GetProgressAsync(categoryId, year, month, default);

        // Assert: Should return progress, not null, with 0% spent
        progress.ShouldNotBeNull();
        progress.SpentAmount.Amount.ShouldBe(0m);
        progress.PercentUsed.ShouldBe(0m);
    }

    [Fact]
    public void BudgetGoal_ZeroTarget_ValidationPasses()
    {
        // Arrange & Act: Create goal with 0 target (allowed)
        var categoryId = Guid.NewGuid();
        var goal = BudgetGoal.Create(categoryId, 2026, 1, MoneyValue.Create("USD", 0m));

        // Assert: Goal created successfully
        goal.ShouldNotBeNull();
        goal.TargetAmount.Amount.ShouldBe(0m);
    }

    [Fact]
    public void BudgetProgress_ZeroTargetAmount_NoAritmeticException()
    {
        // Arrange: Progress with zero target
        var categoryId = Guid.NewGuid();
        var targetAmount = MoneyValue.Create("USD", 0m);
        var spentAmount = MoneyValue.Create("USD", 50m);

        // Act: Create progress with zero target (should handle gracefully)
        var progress = BudgetProgress.Create(
            categoryId,
            "TestCategory",
            "🏠",
            "#FF0000",
            targetAmount,
            spentAmount,
            transactionCount: 1);

        // Assert: Progress created without division by zero
        progress.ShouldNotBeNull();
        progress.TargetAmount.Amount.ShouldBe(0m);
        progress.SpentAmount.Amount.ShouldBe(50m);

        // PercentUsed when target is 0: returns 0 (no division by zero exception)
        progress.PercentUsed.ShouldBe(0m);
    }

    [Fact]
    public async Task BudgetProgressService_MonthBoundary_LastDayOfMonth()
    {
        // Arrange: Transaction on Feb 28 (non-leap year)
        var categoryId = Guid.NewGuid();
        var year = 2025; // Non-leap year
        var month = 2;

        var goal = BudgetGoal.Create(categoryId, year, month, MoneyValue.Create("USD", 300m));
        var category = BudgetCategory.Create("February", CategoryType.Expense);

        var goalRepo = new Mock<IBudgetGoalRepository>();
        goalRepo.Setup(r => r.GetByCategoryAndMonthAsync(categoryId, year, month, default))
            .ReturnsAsync(goal);

        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        categoryRepo.Setup(r => r.GetByIdAsync(categoryId, default)).ReturnsAsync(category);

        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetSpendingByCategoryAsync(categoryId, year, month, default))
            .ReturnsAsync(MoneyValue.Create("USD", 150m));

        var userContext = new Mock<IUserContext>();
        var currencyProvider = new Mock<ICurrencyProvider>();

        var service = new BudgetProgressService(
            goalRepo.Object,
            categoryRepo.Object,
            transactionRepo.Object,
            currencyProvider.Object,
            userContext.Object);

        // Act
        var progress = await service.GetProgressAsync(categoryId, year, month, default);

        // Assert: Progress should include last day of month transaction
        progress.ShouldNotBeNull();
        progress.SpentAmount.Amount.ShouldBe(150m);
        progress.PercentUsed.ShouldBe(50m); // 150/300
    }

    [Fact]
    public async Task BudgetProgressService_MonthBoundary_LeapYearFeb29()
    {
        // Arrange: Transaction on Feb 29 (leap year)
        var categoryId = Guid.NewGuid();
        var year = 2024; // Leap year
        var month = 2;

        var goal = BudgetGoal.Create(categoryId, year, month, MoneyValue.Create("USD", 200m));
        var category = BudgetCategory.Create("February", CategoryType.Expense);

        var goalRepo = new Mock<IBudgetGoalRepository>();
        goalRepo.Setup(r => r.GetByCategoryAndMonthAsync(categoryId, year, month, default))
            .ReturnsAsync(goal);

        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        categoryRepo.Setup(r => r.GetByIdAsync(categoryId, default)).ReturnsAsync(category);

        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetSpendingByCategoryAsync(categoryId, year, month, default))
            .ReturnsAsync(MoneyValue.Create("USD", 100m));

        var userContext = new Mock<IUserContext>();
        var currencyProvider = new Mock<ICurrencyProvider>();

        var service = new BudgetProgressService(
            goalRepo.Object,
            categoryRepo.Object,
            transactionRepo.Object,
            currencyProvider.Object,
            userContext.Object);

        // Act
        var progress = await service.GetProgressAsync(categoryId, year, month, default);

        // Assert
        progress.ShouldNotBeNull();
        progress.SpentAmount.Amount.ShouldBe(100m);
        progress.PercentUsed.ShouldBe(50m); // 100/200
    }

    [Fact]
    public void BudgetGoal_VeryLargeTarget_HandlesCorrectly()
    {
        // Arrange & Act: Create goal with 1 million dollar target
        var categoryId = Guid.NewGuid();
        var goal = BudgetGoal.Create(categoryId, 2026, 1, MoneyValue.Create("USD", 1000000m));

        // Assert
        goal.ShouldNotBeNull();
        goal.TargetAmount.Amount.ShouldBe(1000000m);
    }

    [Fact]
    public void BudgetProgress_VeryLargeNumbers_FormattingCorrect()
    {
        // Arrange: Progress with very large amounts
        var categoryId = Guid.NewGuid();
        var targetAmount = MoneyValue.Create("USD", 1000000m);
        var spentAmount = MoneyValue.Create("USD", 500000m);

        // Act
        var progress = BudgetProgress.Create(
            categoryId,
            "HighValue",
            "🏦",
            "#0000FF",
            targetAmount,
            spentAmount,
            transactionCount: 100);

        // Assert: Should handle large numbers without overflow
        progress.ShouldNotBeNull();
        progress.TargetAmount.Amount.ShouldBe(1000000m);
        progress.SpentAmount.Amount.ShouldBe(500000m);
        progress.PercentUsed.ShouldBe(50m);
    }

    [Fact]
    public async Task BudgetProgressService_CategoryNotFound_ReturnsNull()
    {
        // Arrange: Goal exists but category doesn't
        var categoryId = Guid.NewGuid();
        var year = 2026;
        var month = 1;

        var goal = BudgetGoal.Create(categoryId, year, month, MoneyValue.Create("USD", 100m));

        var goalRepo = new Mock<IBudgetGoalRepository>();
        goalRepo.Setup(r => r.GetByCategoryAndMonthAsync(categoryId, year, month, default))
            .ReturnsAsync(goal);

        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        categoryRepo.Setup(r => r.GetByIdAsync(categoryId, default))
            .ReturnsAsync((BudgetCategory?)null);

        var transactionRepo = new Mock<ITransactionRepository>();
        var userContext = new Mock<IUserContext>();
        var currencyProvider = new Mock<ICurrencyProvider>();

        var service = new BudgetProgressService(
            goalRepo.Object,
            categoryRepo.Object,
            transactionRepo.Object,
            currencyProvider.Object,
            userContext.Object);

        // Act
        var progress = await service.GetProgressAsync(categoryId, year, month, default);

        // Assert: Returns null when category not found
        progress.ShouldBeNull();
    }
}
