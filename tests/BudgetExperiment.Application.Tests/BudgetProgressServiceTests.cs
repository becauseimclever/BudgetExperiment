// <copyright file="BudgetProgressServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>


using BudgetExperiment.Domain;
using Moq;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Unit tests for BudgetProgressService.
/// </summary>
public class BudgetProgressServiceTests
{
    [Fact]
    public async Task GetProgressAsync_Returns_Progress_For_Category()
    {
        // Arrange
        var category = BudgetCategory.Create("Groceries", CategoryType.Expense);
        var categoryId = category.Id;
        var goal = BudgetGoal.Create(categoryId, 2026, 1, MoneyValue.Create("USD", 500m));
        var goalRepo = new Mock<IBudgetGoalRepository>();
        goalRepo.Setup(r => r.GetByCategoryAndMonthAsync(categoryId, 2026, 1, default)).ReturnsAsync(goal);
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        categoryRepo.Setup(r => r.GetByIdAsync(categoryId, default)).ReturnsAsync(category);
        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetSpendingByCategoryAsync(categoryId, 2026, 1, default))
            .ReturnsAsync(MoneyValue.Create("USD", 250m));
        var service = new BudgetProgressService(goalRepo.Object, categoryRepo.Object, transactionRepo.Object);

        // Act
        var result = await service.GetProgressAsync(categoryId, 2026, 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(categoryId, result.CategoryId);
        Assert.Equal(500m, result.TargetAmount.Amount);
        Assert.Equal(250m, result.SpentAmount.Amount);
        Assert.Equal(250m, result.RemainingAmount.Amount);
        Assert.Equal(50m, result.PercentUsed);
        Assert.Equal("OnTrack", result.Status);
    }

    [Fact]
    public async Task GetProgressAsync_Returns_Null_When_No_Goal()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var goalRepo = new Mock<IBudgetGoalRepository>();
        goalRepo.Setup(r => r.GetByCategoryAndMonthAsync(categoryId, 2026, 1, default)).ReturnsAsync((BudgetGoal?)null);
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        var transactionRepo = new Mock<ITransactionRepository>();
        var service = new BudgetProgressService(goalRepo.Object, categoryRepo.Object, transactionRepo.Object);

        // Act
        var result = await service.GetProgressAsync(categoryId, 2026, 1);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetProgressAsync_Returns_Warning_Status_When_Over_80_Percent()
    {
        // Arrange
        var category = BudgetCategory.Create("Groceries", CategoryType.Expense);
        var categoryId = category.Id;
        var goal = BudgetGoal.Create(categoryId, 2026, 1, MoneyValue.Create("USD", 100m));
        var goalRepo = new Mock<IBudgetGoalRepository>();
        goalRepo.Setup(r => r.GetByCategoryAndMonthAsync(categoryId, 2026, 1, default)).ReturnsAsync(goal);
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        categoryRepo.Setup(r => r.GetByIdAsync(categoryId, default)).ReturnsAsync(category);
        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetSpendingByCategoryAsync(categoryId, 2026, 1, default))
            .ReturnsAsync(MoneyValue.Create("USD", 85m));
        var service = new BudgetProgressService(goalRepo.Object, categoryRepo.Object, transactionRepo.Object);

        // Act
        var result = await service.GetProgressAsync(categoryId, 2026, 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Warning", result.Status);
        Assert.Equal(85m, result.PercentUsed);
    }

    [Fact]
    public async Task GetProgressAsync_Returns_OverBudget_Status_When_Over_100_Percent()
    {
        // Arrange
        var category = BudgetCategory.Create("Groceries", CategoryType.Expense);
        var categoryId = category.Id;
        var goal = BudgetGoal.Create(categoryId, 2026, 1, MoneyValue.Create("USD", 100m));
        var goalRepo = new Mock<IBudgetGoalRepository>();
        goalRepo.Setup(r => r.GetByCategoryAndMonthAsync(categoryId, 2026, 1, default)).ReturnsAsync(goal);
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        categoryRepo.Setup(r => r.GetByIdAsync(categoryId, default)).ReturnsAsync(category);
        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetSpendingByCategoryAsync(categoryId, 2026, 1, default))
            .ReturnsAsync(MoneyValue.Create("USD", 120m));
        var service = new BudgetProgressService(goalRepo.Object, categoryRepo.Object, transactionRepo.Object);

        // Act
        var result = await service.GetProgressAsync(categoryId, 2026, 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("OverBudget", result.Status);
        Assert.Equal(-20m, result.RemainingAmount.Amount);
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_Returns_All_Category_Progress()
    {
        // Arrange
        var category1 = BudgetCategory.Create("Groceries", CategoryType.Expense);
        var category2 = BudgetCategory.Create("Entertainment", CategoryType.Expense);
        var category1Id = category1.Id;
        var category2Id = category2.Id;
        var allExpenseCategories = new List<BudgetCategory> { category1, category2 };
        var goals = new List<BudgetGoal>
        {
            BudgetGoal.Create(category1Id, 2026, 1, MoneyValue.Create("USD", 500m)),
            BudgetGoal.Create(category2Id, 2026, 1, MoneyValue.Create("USD", 300m)),
        };
        var goalRepo = new Mock<IBudgetGoalRepository>();
        goalRepo.Setup(r => r.GetByMonthAsync(2026, 1, default)).ReturnsAsync(goals);
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        categoryRepo.Setup(r => r.GetByTypeAsync(CategoryType.Expense, default)).ReturnsAsync(allExpenseCategories);
        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetSpendingByCategoryAsync(category1Id, 2026, 1, default))
            .ReturnsAsync(MoneyValue.Create("USD", 250m));
        transactionRepo.Setup(r => r.GetSpendingByCategoryAsync(category2Id, 2026, 1, default))
            .ReturnsAsync(MoneyValue.Create("USD", 150m));
        var service = new BudgetProgressService(goalRepo.Object, categoryRepo.Object, transactionRepo.Object);

        // Act
        var result = await service.GetMonthlySummaryAsync(2026, 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.CategoryProgress.Count);
        Assert.Equal(800m, result.TotalBudgeted.Amount);
        Assert.Equal(400m, result.TotalSpent.Amount);
        Assert.Equal(400m, result.TotalRemaining.Amount);
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_Returns_Empty_Summary_When_No_Goals_And_No_Categories()
    {
        // Arrange
        var goalRepo = new Mock<IBudgetGoalRepository>();
        goalRepo.Setup(r => r.GetByMonthAsync(2026, 1, default)).ReturnsAsync(new List<BudgetGoal>());
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        categoryRepo.Setup(r => r.GetByTypeAsync(CategoryType.Expense, default)).ReturnsAsync(new List<BudgetCategory>());
        var transactionRepo = new Mock<ITransactionRepository>();
        var service = new BudgetProgressService(goalRepo.Object, categoryRepo.Object, transactionRepo.Object);

        // Act
        var result = await service.GetMonthlySummaryAsync(2026, 1);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.CategoryProgress);
        Assert.Equal(0m, result.TotalBudgeted.Amount);
        Assert.Equal(0m, result.TotalSpent.Amount);
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_Includes_Expense_Categories_Without_Goals()
    {
        // Arrange
        var categoryWithGoal = BudgetCategory.Create("Groceries", CategoryType.Expense);
        var categoryWithoutGoal = BudgetCategory.Create("Entertainment", CategoryType.Expense);
        var allExpenseCategories = new List<BudgetCategory> { categoryWithGoal, categoryWithoutGoal };
        var goals = new List<BudgetGoal>
        {
            BudgetGoal.Create(categoryWithGoal.Id, 2026, 1, MoneyValue.Create("USD", 500m)),
        };

        var goalRepo = new Mock<IBudgetGoalRepository>();
        goalRepo.Setup(r => r.GetByMonthAsync(2026, 1, default)).ReturnsAsync(goals);

        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        categoryRepo.Setup(r => r.GetByIdAsync(categoryWithGoal.Id, default)).ReturnsAsync(categoryWithGoal);
        categoryRepo.Setup(r => r.GetByTypeAsync(CategoryType.Expense, default)).ReturnsAsync(allExpenseCategories);

        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetSpendingByCategoryAsync(categoryWithGoal.Id, 2026, 1, default))
            .ReturnsAsync(MoneyValue.Create("USD", 250m));
        transactionRepo.Setup(r => r.GetSpendingByCategoryAsync(categoryWithoutGoal.Id, 2026, 1, default))
            .ReturnsAsync(MoneyValue.Create("USD", 0m));

        var service = new BudgetProgressService(goalRepo.Object, categoryRepo.Object, transactionRepo.Object);

        // Act
        var result = await service.GetMonthlySummaryAsync(2026, 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.CategoryProgress.Count);

        var progressWithGoal = result.CategoryProgress.First(p => p.CategoryId == categoryWithGoal.Id);
        Assert.Equal("OnTrack", progressWithGoal.Status);
        Assert.Equal(500m, progressWithGoal.TargetAmount.Amount);
        Assert.Equal(250m, progressWithGoal.SpentAmount.Amount);

        var progressWithoutGoal = result.CategoryProgress.First(p => p.CategoryId == categoryWithoutGoal.Id);
        Assert.Equal("NoBudgetSet", progressWithoutGoal.Status);
        Assert.Equal(0m, progressWithoutGoal.TargetAmount.Amount);
        Assert.Equal(0m, progressWithoutGoal.SpentAmount.Amount);
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_Returns_CategoriesNoBudgetSet_Count()
    {
        // Arrange
        var categoryWithGoal = BudgetCategory.Create("Groceries", CategoryType.Expense);
        var categoryWithoutGoal1 = BudgetCategory.Create("Entertainment", CategoryType.Expense);
        var categoryWithoutGoal2 = BudgetCategory.Create("Clothing", CategoryType.Expense);
        var allExpenseCategories = new List<BudgetCategory> { categoryWithGoal, categoryWithoutGoal1, categoryWithoutGoal2 };
        var goals = new List<BudgetGoal>
        {
            BudgetGoal.Create(categoryWithGoal.Id, 2026, 1, MoneyValue.Create("USD", 500m)),
        };

        var goalRepo = new Mock<IBudgetGoalRepository>();
        goalRepo.Setup(r => r.GetByMonthAsync(2026, 1, default)).ReturnsAsync(goals);

        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        categoryRepo.Setup(r => r.GetByIdAsync(categoryWithGoal.Id, default)).ReturnsAsync(categoryWithGoal);
        categoryRepo.Setup(r => r.GetByTypeAsync(CategoryType.Expense, default)).ReturnsAsync(allExpenseCategories);

        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetSpendingByCategoryAsync(It.IsAny<Guid>(), 2026, 1, default))
            .ReturnsAsync(MoneyValue.Create("USD", 100m));

        var service = new BudgetProgressService(goalRepo.Object, categoryRepo.Object, transactionRepo.Object);

        // Act
        var result = await service.GetMonthlySummaryAsync(2026, 1);

        // Assert
        Assert.Equal(1, result.CategoriesOnTrack);
        Assert.Equal(2, result.CategoriesNoBudgetSet);
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_Excludes_Income_Categories()
    {
        // Arrange
        var expenseCategory = BudgetCategory.Create("Groceries", CategoryType.Expense);
        var incomeCategory = BudgetCategory.Create("Salary", CategoryType.Income);

        // GetByTypeAsync(Expense) should only return expense categories
        var expenseCategories = new List<BudgetCategory> { expenseCategory };

        var goalRepo = new Mock<IBudgetGoalRepository>();
        goalRepo.Setup(r => r.GetByMonthAsync(2026, 1, default)).ReturnsAsync(new List<BudgetGoal>());

        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        categoryRepo.Setup(r => r.GetByTypeAsync(CategoryType.Expense, default)).ReturnsAsync(expenseCategories);

        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetSpendingByCategoryAsync(expenseCategory.Id, 2026, 1, default))
            .ReturnsAsync(MoneyValue.Create("USD", 0m));

        var service = new BudgetProgressService(goalRepo.Object, categoryRepo.Object, transactionRepo.Object);

        // Act
        var result = await service.GetMonthlySummaryAsync(2026, 1);

        // Assert
        Assert.Single(result.CategoryProgress);
        Assert.Equal("Groceries", result.CategoryProgress.First().CategoryName);
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_Only_Includes_Active_Expense_Categories()
    {
        // Arrange
        var activeCategory = BudgetCategory.Create("Groceries", CategoryType.Expense);
        var inactiveCategory = BudgetCategory.Create("Old Category", CategoryType.Expense);
        inactiveCategory.Deactivate();

        // GetByTypeAsync should only return active categories based on repository implementation
        // but we mock it to return only active ones to test the service behavior
        var activeExpenseCategories = new List<BudgetCategory> { activeCategory };

        var goalRepo = new Mock<IBudgetGoalRepository>();
        goalRepo.Setup(r => r.GetByMonthAsync(2026, 1, default)).ReturnsAsync(new List<BudgetGoal>());

        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        categoryRepo.Setup(r => r.GetByTypeAsync(CategoryType.Expense, default)).ReturnsAsync(activeExpenseCategories);

        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetSpendingByCategoryAsync(activeCategory.Id, 2026, 1, default))
            .ReturnsAsync(MoneyValue.Create("USD", 0m));

        var service = new BudgetProgressService(goalRepo.Object, categoryRepo.Object, transactionRepo.Object);

        // Act
        var result = await service.GetMonthlySummaryAsync(2026, 1);

        // Assert
        Assert.Single(result.CategoryProgress);
        Assert.Equal("Groceries", result.CategoryProgress.First().CategoryName);
    }
}
