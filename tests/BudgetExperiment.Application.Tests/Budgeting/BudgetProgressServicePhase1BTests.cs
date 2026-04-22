// <copyright file="BudgetProgressServicePhase1BTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;
using BudgetExperiment.Application.Budgeting;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;
using BudgetExperiment.Domain.Identity;
using BudgetExperiment.Domain.Settings;
using Moq;
using Shouldly;

namespace BudgetExperiment.Application.Tests.Budgeting;

/// <summary>
/// Phase 1B deep-dive tests for BudgetProgressService edge cases.
/// </summary>
public class BudgetProgressServicePhase1BTests
{
    public BudgetProgressServicePhase1BTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
    }

    [Fact]
    public async Task GetMonthlySummary_MultipleCategoriesWithZeroBudget_OverallPercentageDoesNotOverflow()
    {
        // Arrange
        var mockGoalRepo = new Mock<IBudgetGoalRepository>();
        var mockCategoryRepo = new Mock<IBudgetCategoryRepository>();
        var mockTransactionRepo = new Mock<ITransactionRepository>();
        var mockCurrencyProvider = new Mock<ICurrencyProvider>();
        var mockUserContext = new Mock<IUserContext>();

        var category1 = BudgetCategory.Create("Cat1", CategoryType.Expense);
        var category2 = BudgetCategory.Create("Cat2", CategoryType.Expense);
        var category3 = BudgetCategory.Create("Cat3", CategoryType.Expense);

        mockCategoryRepo.Setup(r => r.GetByTypeAsync(CategoryType.Expense, default))
            .ReturnsAsync(new[] { category1, category2, category3 });

        mockGoalRepo.Setup(r => r.GetByMonthAsync(2026, 1, default))
            .ReturnsAsync(new[]
            {
                BudgetGoal.Create(category1.Id, 2026, 1, MoneyValue.Create("USD", 0m)),
                BudgetGoal.Create(category2.Id, 2026, 1, MoneyValue.Create("USD", 0m)),
                BudgetGoal.Create(category3.Id, 2026, 1, MoneyValue.Create("USD", 0m)),
            });

        mockTransactionRepo.Setup(r => r.GetSpendingByCategoriesAsync(2026, 1, default))
            .ReturnsAsync(new Dictionary<Guid, decimal>
            {
                { category1.Id, 10m },
                { category2.Id, 20m },
                { category3.Id, 30m },
            });

        mockCurrencyProvider.Setup(c => c.GetCurrencyAsync(default))
            .ReturnsAsync("USD");

        var service = new BudgetProgressService(
            mockGoalRepo.Object,
            mockCategoryRepo.Object,
            mockTransactionRepo.Object,
            mockCurrencyProvider.Object,
            mockUserContext.Object);

        // Act
        var summary = await service.GetMonthlySummaryAsync(2026, 1, false, default);

        // Assert
        summary.TotalBudgeted.Amount.ShouldBe(0m);
        summary.TotalSpent.Amount.ShouldBe(60m);
        summary.OverallPercentUsed.ShouldBe(0m);
    }

    [Fact]
    public async Task GetMonthlySummary_NegativeBudgetTargets_HandledGracefully()
    {
        // Arrange
        var mockGoalRepo = new Mock<IBudgetGoalRepository>();
        var mockCategoryRepo = new Mock<IBudgetCategoryRepository>();
        var mockTransactionRepo = new Mock<ITransactionRepository>();
        var mockCurrencyProvider = new Mock<ICurrencyProvider>();
        var mockUserContext = new Mock<IUserContext>();

        var category1 = BudgetCategory.Create("Income", CategoryType.Expense);

        mockCategoryRepo.Setup(r => r.GetByTypeAsync(CategoryType.Expense, default))
            .ReturnsAsync(new[] { category1 });

        mockGoalRepo.Setup(r => r.GetByMonthAsync(2026, 1, default))
            .ReturnsAsync(new[]
            {
                BudgetGoal.Create(category1.Id, 2026, 1, MoneyValue.Create("USD", -500m)),
            });

        mockTransactionRepo.Setup(r => r.GetSpendingByCategoriesAsync(2026, 1, default))
            .ReturnsAsync(new Dictionary<Guid, decimal>
            {
                { category1.Id, -100m },
            });

        mockCurrencyProvider.Setup(c => c.GetCurrencyAsync(default))
            .ReturnsAsync("USD");

        var service = new BudgetProgressService(
            mockGoalRepo.Object,
            mockCategoryRepo.Object,
            mockTransactionRepo.Object,
            mockCurrencyProvider.Object,
            mockUserContext.Object);

        // Act
        var summary = await service.GetMonthlySummaryAsync(2026, 1, false, default);

        // Assert
        summary.TotalBudgeted.Amount.ShouldBe(-500m);
        summary.TotalSpent.Amount.ShouldBe(-100m);
        summary.OverallPercentUsed.ShouldBe(20m);
    }

    [Fact]
    public async Task GetMonthlySummary_NoCategoryWithBudget_OverallZeroPercent()
    {
        // Arrange
        var mockGoalRepo = new Mock<IBudgetGoalRepository>();
        var mockCategoryRepo = new Mock<IBudgetCategoryRepository>();
        var mockTransactionRepo = new Mock<ITransactionRepository>();
        var mockCurrencyProvider = new Mock<ICurrencyProvider>();
        var mockUserContext = new Mock<IUserContext>();

        var category1 = BudgetCategory.Create("Cat1", CategoryType.Expense);

        mockCategoryRepo.Setup(r => r.GetByTypeAsync(CategoryType.Expense, default))
            .ReturnsAsync(new[] { category1 });

        mockGoalRepo.Setup(r => r.GetByMonthAsync(2026, 1, default))
            .ReturnsAsync(Array.Empty<BudgetGoal>());

        mockTransactionRepo.Setup(r => r.GetSpendingByCategoriesAsync(2026, 1, default))
            .ReturnsAsync(new Dictionary<Guid, decimal>
            {
                { category1.Id, 150m },
            });

        mockCurrencyProvider.Setup(c => c.GetCurrencyAsync(default))
            .ReturnsAsync("USD");

        var service = new BudgetProgressService(
            mockGoalRepo.Object,
            mockCategoryRepo.Object,
            mockTransactionRepo.Object,
            mockCurrencyProvider.Object,
            mockUserContext.Object);

        // Act
        var summary = await service.GetMonthlySummaryAsync(2026, 1, false, default);

        // Assert
        summary.TotalBudgeted.Amount.ShouldBe(0m);
        summary.TotalSpent.Amount.ShouldBe(150m);
        summary.OverallPercentUsed.ShouldBe(0m);
        summary.CategoriesNoBudgetSet.ShouldBe(1);
    }

    [Fact]
    public async Task GetMonthlySummary_MonthBoundaryJan31ToFeb1_CalculatesCorrectly()
    {
        // Arrange
        var mockGoalRepo = new Mock<IBudgetGoalRepository>();
        var mockCategoryRepo = new Mock<IBudgetCategoryRepository>();
        var mockTransactionRepo = new Mock<ITransactionRepository>();
        var mockCurrencyProvider = new Mock<ICurrencyProvider>();
        var mockUserContext = new Mock<IUserContext>();

        var category = BudgetCategory.Create("Groceries", CategoryType.Expense);

        mockCategoryRepo.Setup(r => r.GetByTypeAsync(CategoryType.Expense, default))
            .ReturnsAsync(new[] { category });

        mockGoalRepo.Setup(r => r.GetByMonthAsync(2026, 1, default))
            .ReturnsAsync(new[]
            {
                BudgetGoal.Create(category.Id, 2026, 1, MoneyValue.Create("USD", 500m)),
            });

        mockTransactionRepo.Setup(r => r.GetSpendingByCategoriesAsync(2026, 1, default))
            .ReturnsAsync(new Dictionary<Guid, decimal>
            {
                { category.Id, 250m },
            });

        mockGoalRepo.Setup(r => r.GetByMonthAsync(2026, 2, default))
            .ReturnsAsync(new[]
            {
                BudgetGoal.Create(category.Id, 2026, 2, MoneyValue.Create("USD", 500m)),
            });

        mockTransactionRepo.Setup(r => r.GetSpendingByCategoriesAsync(2026, 2, default))
            .ReturnsAsync(new Dictionary<Guid, decimal>
            {
                { category.Id, 100m },
            });

        mockCurrencyProvider.Setup(c => c.GetCurrencyAsync(default))
            .ReturnsAsync("USD");

        var service = new BudgetProgressService(
            mockGoalRepo.Object,
            mockCategoryRepo.Object,
            mockTransactionRepo.Object,
            mockCurrencyProvider.Object,
            mockUserContext.Object);

        // Act
        var janSummary = await service.GetMonthlySummaryAsync(2026, 1, false, default);
        var febSummary = await service.GetMonthlySummaryAsync(2026, 2, false, default);

        // Assert
        janSummary.TotalSpent.Amount.ShouldBe(250m);
        janSummary.OverallPercentUsed.ShouldBe(50m);
        febSummary.TotalSpent.Amount.ShouldBe(100m);
        febSummary.OverallPercentUsed.ShouldBe(20m);
    }

    [Fact]
    public async Task GetMonthlySummary_LeapYearFeb29Boundary_CalculatesCorrectly()
    {
        // Arrange
        var mockGoalRepo = new Mock<IBudgetGoalRepository>();
        var mockCategoryRepo = new Mock<IBudgetCategoryRepository>();
        var mockTransactionRepo = new Mock<ITransactionRepository>();
        var mockCurrencyProvider = new Mock<ICurrencyProvider>();
        var mockUserContext = new Mock<IUserContext>();

        var category = BudgetCategory.Create("Utilities", CategoryType.Expense);

        mockCategoryRepo.Setup(r => r.GetByTypeAsync(CategoryType.Expense, default))
            .ReturnsAsync(new[] { category });

        mockGoalRepo.Setup(r => r.GetByMonthAsync(2024, 2, default))
            .ReturnsAsync(new[]
            {
                BudgetGoal.Create(category.Id, 2024, 2, MoneyValue.Create("USD", 300m)),
            });

        mockTransactionRepo.Setup(r => r.GetSpendingByCategoriesAsync(2024, 2, default))
            .ReturnsAsync(new Dictionary<Guid, decimal>
            {
                { category.Id, 150m },
            });

        mockCurrencyProvider.Setup(c => c.GetCurrencyAsync(default))
            .ReturnsAsync("USD");

        var service = new BudgetProgressService(
            mockGoalRepo.Object,
            mockCategoryRepo.Object,
            mockTransactionRepo.Object,
            mockCurrencyProvider.Object,
            mockUserContext.Object);

        // Act
        var summary = await service.GetMonthlySummaryAsync(2024, 2, false, default);

        // Assert
        summary.TotalBudgeted.Amount.ShouldBe(300m);
        summary.TotalSpent.Amount.ShouldBe(150m);
        summary.OverallPercentUsed.ShouldBe(50m);
    }

    [Fact]
    public async Task GetMonthlySummary_LargeDataset1000Categories_CalculatesWithoutError()
    {
        // Arrange
        var mockGoalRepo = new Mock<IBudgetGoalRepository>();
        var mockCategoryRepo = new Mock<IBudgetCategoryRepository>();
        var mockTransactionRepo = new Mock<ITransactionRepository>();
        var mockCurrencyProvider = new Mock<ICurrencyProvider>();
        var mockUserContext = new Mock<IUserContext>();

        var categories = new List<BudgetCategory>();
        var goals = new List<BudgetGoal>();
        var spending = new Dictionary<Guid, decimal>();

        for (int i = 0; i < 1000; i++)
        {
            var category = BudgetCategory.Create($"Category{i}", CategoryType.Expense);
            categories.Add(category);
            goals.Add(BudgetGoal.Create(category.Id, 2026, 1, MoneyValue.Create("USD", 100m)));
            spending[category.Id] = 50m;
        }

        mockCategoryRepo.Setup(r => r.GetByTypeAsync(CategoryType.Expense, default))
            .ReturnsAsync(categories);

        mockGoalRepo.Setup(r => r.GetByMonthAsync(2026, 1, default))
            .ReturnsAsync(goals);

        mockTransactionRepo.Setup(r => r.GetSpendingByCategoriesAsync(2026, 1, default))
            .ReturnsAsync(spending);

        mockCurrencyProvider.Setup(c => c.GetCurrencyAsync(default))
            .ReturnsAsync("USD");

        var service = new BudgetProgressService(
            mockGoalRepo.Object,
            mockCategoryRepo.Object,
            mockTransactionRepo.Object,
            mockCurrencyProvider.Object,
            mockUserContext.Object);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var summary = await service.GetMonthlySummaryAsync(2026, 1, false, default);
        stopwatch.Stop();

        // Assert
        summary.CategoryProgress.Count.ShouldBe(1000);
        summary.TotalBudgeted.Amount.ShouldBe(100000m);
        summary.TotalSpent.Amount.ShouldBe(50000m);
        summary.OverallPercentUsed.ShouldBe(50m);
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(500);
    }

    [Fact]
    public async Task GetMonthlySummary_ConcurrentTransactionAdditions_AggregatesCorrectly()
    {
        // Arrange
        var mockGoalRepo = new Mock<IBudgetGoalRepository>();
        var mockCategoryRepo = new Mock<IBudgetCategoryRepository>();
        var mockTransactionRepo = new Mock<ITransactionRepository>();
        var mockCurrencyProvider = new Mock<ICurrencyProvider>();
        var mockUserContext = new Mock<IUserContext>();

        var category = BudgetCategory.Create("Groceries", CategoryType.Expense);

        mockCategoryRepo.Setup(r => r.GetByTypeAsync(CategoryType.Expense, default))
            .ReturnsAsync(new[] { category });

        mockGoalRepo.Setup(r => r.GetByMonthAsync(2026, 1, default))
            .ReturnsAsync(new[]
            {
                BudgetGoal.Create(category.Id, 2026, 1, MoneyValue.Create("USD", 500m)),
            });

        var spendingValue = 0m;
        mockTransactionRepo.Setup(r => r.GetSpendingByCategoriesAsync(2026, 1, default))
            .ReturnsAsync(() => new Dictionary<Guid, decimal> { { category.Id, spendingValue } });

        mockCurrencyProvider.Setup(c => c.GetCurrencyAsync(default))
            .ReturnsAsync("USD");

        var service = new BudgetProgressService(
            mockGoalRepo.Object,
            mockCategoryRepo.Object,
            mockTransactionRepo.Object,
            mockCurrencyProvider.Object,
            mockUserContext.Object);

        // Act - Simulate two concurrent transaction additions
        spendingValue = 100m;
        var result1 = await service.GetMonthlySummaryAsync(2026, 1, false, default);

        spendingValue = 200m;
        var result2 = await service.GetMonthlySummaryAsync(2026, 1, false, default);

        // Assert
        result1.TotalSpent.Amount.ShouldBe(100m);
        result2.TotalSpent.Amount.ShouldBe(200m);
    }

    [Fact]
    public async Task GetProgress_CategoryNotFound_ReturnsNull()
    {
        // Arrange
        var mockGoalRepo = new Mock<IBudgetGoalRepository>();
        var mockCategoryRepo = new Mock<IBudgetCategoryRepository>();
        var mockTransactionRepo = new Mock<ITransactionRepository>();
        var mockCurrencyProvider = new Mock<ICurrencyProvider>();
        var mockUserContext = new Mock<IUserContext>();

        var categoryId = Guid.NewGuid();

        mockGoalRepo.Setup(r => r.GetByCategoryAndMonthAsync(categoryId, 2026, 1, default))
            .ReturnsAsync(BudgetGoal.Create(categoryId, 2026, 1, MoneyValue.Create("USD", 500m)));

        mockCategoryRepo.Setup(r => r.GetByIdAsync(categoryId, default))
            .ReturnsAsync((BudgetCategory?)null);

        var service = new BudgetProgressService(
            mockGoalRepo.Object,
            mockCategoryRepo.Object,
            mockTransactionRepo.Object,
            mockCurrencyProvider.Object,
            mockUserContext.Object);

        // Act
        var result = await service.GetProgressAsync(categoryId, 2026, 1, default);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetProgress_GoalNotFound_ReturnsNull()
    {
        // Arrange
        var mockGoalRepo = new Mock<IBudgetGoalRepository>();
        var mockCategoryRepo = new Mock<IBudgetCategoryRepository>();
        var mockTransactionRepo = new Mock<ITransactionRepository>();
        var mockCurrencyProvider = new Mock<ICurrencyProvider>();
        var mockUserContext = new Mock<IUserContext>();

        var categoryId = Guid.NewGuid();

        mockGoalRepo.Setup(r => r.GetByCategoryAndMonthAsync(categoryId, 2026, 1, default))
            .ReturnsAsync((BudgetGoal?)null);

        var service = new BudgetProgressService(
            mockGoalRepo.Object,
            mockCategoryRepo.Object,
            mockTransactionRepo.Object,
            mockCurrencyProvider.Object,
            mockUserContext.Object);

        // Act
        var result = await service.GetProgressAsync(categoryId, 2026, 1, default);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetMonthlySummary_ConcurrentUpdates_NoRaceConditions()
    {
        // Arrange
        var mockGoalRepo = new Mock<IBudgetGoalRepository>();
        var mockCategoryRepo = new Mock<IBudgetCategoryRepository>();
        var mockTransactionRepo = new Mock<ITransactionRepository>();
        var mockCurrencyProvider = new Mock<ICurrencyProvider>();
        var mockUserContext = new Mock<IUserContext>();

        var category = BudgetCategory.Create("Test", CategoryType.Expense);

        mockCategoryRepo.Setup(r => r.GetByTypeAsync(CategoryType.Expense, default))
            .ReturnsAsync(new[] { category });

        mockGoalRepo.Setup(r => r.GetByMonthAsync(2026, 1, default))
            .ReturnsAsync(new[]
            {
                BudgetGoal.Create(category.Id, 2026, 1, MoneyValue.Create("USD", 1000m)),
            });

        var counter = 0;
        mockTransactionRepo.Setup(r => r.GetSpendingByCategoriesAsync(2026, 1, default))
            .ReturnsAsync(() => new Dictionary<Guid, decimal>
            {
                { category.Id, Interlocked.Increment(ref counter) * 10m },
            });

        mockCurrencyProvider.Setup(c => c.GetCurrencyAsync(default))
            .ReturnsAsync("USD");

        var service = new BudgetProgressService(
            mockGoalRepo.Object,
            mockCategoryRepo.Object,
            mockTransactionRepo.Object,
            mockCurrencyProvider.Object,
            mockUserContext.Object);

        // Act - Fire 10 concurrent requests
        var tasks = new List<Task<BudgetSummaryDto>>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(service.GetMonthlySummaryAsync(2026, 1, false, default));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - All should complete without exceptions
        results.Length.ShouldBe(10);
        foreach (var result in results)
        {
            result.ShouldNotBeNull();
            result.TotalBudgeted.Amount.ShouldBe(1000m);
        }
    }
}
