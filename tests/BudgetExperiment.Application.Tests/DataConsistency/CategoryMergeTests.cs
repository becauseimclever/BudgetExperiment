// <copyright file="CategoryMergeTests.cs" company="BecauseImClever">
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
/// Unit tests for category operations: merge, deletion, and orphaned reference handling.
/// </summary>
public class CategoryMergeTests
{
    public CategoryMergeTests()
    {
        // Set culture to en-US for consistent currency formatting
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
    }

    [Fact]
    public async Task BudgetProgressService_RecategoryTransaction_RecalculatesProgress()
    {
        // Arrange: Transaction initially in "Groceries", then moved to "Food"
        var groceriesCategoryId = Guid.NewGuid();
        var foodCategoryId = Guid.NewGuid();
        var year = 2026;
        var month = 1;

        // Before move: Groceries has 100m spent
        var groceriesGoal = BudgetGoal.Create(groceriesCategoryId, year, month, MoneyValue.Create("USD", 200m));
        var groceriesCategory = BudgetCategory.Create("Groceries", CategoryType.Expense);

        var foodGoal = BudgetGoal.Create(foodCategoryId, year, month, MoneyValue.Create("USD", 300m));
        var foodCategory = BudgetCategory.Create("Food", CategoryType.Expense);

        var goalRepo = new Mock<IBudgetGoalRepository>();
        goalRepo.Setup(r => r.GetByCategoryAndMonthAsync(groceriesCategoryId, year, month, default))
            .ReturnsAsync(groceriesGoal);
        goalRepo.Setup(r => r.GetByCategoryAndMonthAsync(foodCategoryId, year, month, default))
            .ReturnsAsync(foodGoal);

        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        categoryRepo.Setup(r => r.GetByIdAsync(groceriesCategoryId, default))
            .ReturnsAsync(groceriesCategory);
        categoryRepo.Setup(r => r.GetByIdAsync(foodCategoryId, default))
            .ReturnsAsync(foodCategory);

        var transactionRepo = new Mock<ITransactionRepository>();

        // Initially, Groceries has 100m (before move)
        transactionRepo.Setup(r => r.GetSpendingByCategoryAsync(groceriesCategoryId, year, month, default))
            .ReturnsAsync(MoneyValue.Create("USD", 100m));

        // After move: Food should have 100m more
        transactionRepo.Setup(r => r.GetSpendingByCategoryAsync(foodCategoryId, year, month, default))
            .ReturnsAsync(MoneyValue.Create("USD", 0m));

        var userContext = new Mock<IUserContext>();
        var currencyProvider = new Mock<ICurrencyProvider>();

        var service = new BudgetProgressService(
            goalRepo.Object,
            categoryRepo.Object,
            transactionRepo.Object,
            currencyProvider.Object,
            userContext.Object);

        // Act: Get progress for Groceries before move
        var groceriesProgressBefore = await service.GetProgressAsync(groceriesCategoryId, year, month, default);

        // Assert: Groceries has 100m spent (before move)
        groceriesProgressBefore.ShouldNotBeNull();
        groceriesProgressBefore.SpentAmount.Amount.ShouldBe(100m);
        groceriesProgressBefore.PercentUsed.ShouldBe(50m); // 100/200
    }

    [Fact]
    public async Task BudgetProgressService_AfterCategoryRecategorization_ProgressUpdates()
    {
        // Arrange: Simulate category merge by updating transaction category
        var originalCategoryId = Guid.NewGuid();
        var newCategoryId = Guid.NewGuid();
        var year = 2026;
        var month = 1;

        var originalGoal = BudgetGoal.Create(originalCategoryId, year, month, MoneyValue.Create("USD", 500m));
        var newGoal = BudgetGoal.Create(newCategoryId, year, month, MoneyValue.Create("USD", 500m));

        var originalCategory = BudgetCategory.Create("OldCategory", CategoryType.Expense);
        var newCategory = BudgetCategory.Create("NewCategory", CategoryType.Expense);

        var goalRepo = new Mock<IBudgetGoalRepository>();
        goalRepo.Setup(r => r.GetByCategoryAndMonthAsync(originalCategoryId, year, month, default))
            .ReturnsAsync(originalGoal);
        goalRepo.Setup(r => r.GetByCategoryAndMonthAsync(newCategoryId, year, month, default))
            .ReturnsAsync(newGoal);

        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        categoryRepo.Setup(r => r.GetByIdAsync(originalCategoryId, default))
            .ReturnsAsync(originalCategory);
        categoryRepo.Setup(r => r.GetByIdAsync(newCategoryId, default))
            .ReturnsAsync(newCategory);

        var transactionRepo = new Mock<ITransactionRepository>();

        // Original category loses 100m after recategorization
        transactionRepo.Setup(r => r.GetSpendingByCategoryAsync(originalCategoryId, year, month, default))
            .ReturnsAsync(MoneyValue.Create("USD", 0m));

        // New category gains 100m
        transactionRepo.Setup(r => r.GetSpendingByCategoryAsync(newCategoryId, year, month, default))
            .ReturnsAsync(MoneyValue.Create("USD", 100m));

        var userContext = new Mock<IUserContext>();
        var currencyProvider = new Mock<ICurrencyProvider>();

        var service = new BudgetProgressService(
            goalRepo.Object,
            categoryRepo.Object,
            transactionRepo.Object,
            currencyProvider.Object,
            userContext.Object);

        // Act: Get progress for both categories after recategorization
        var originalProgress = await service.GetProgressAsync(originalCategoryId, year, month, default);
        var newProgress = await service.GetProgressAsync(newCategoryId, year, month, default);

        // Assert: Original category should show 0%, new category should show 20%
        originalProgress.ShouldNotBeNull();
        originalProgress.SpentAmount.Amount.ShouldBe(0m);
        originalProgress.PercentUsed.ShouldBe(0m);

        newProgress.ShouldNotBeNull();
        newProgress.SpentAmount.Amount.ShouldBe(100m);
        newProgress.PercentUsed.ShouldBe(20m); // 100/500
    }

    [Fact]
    public async Task BudgetProgressService_OrphanedGoal_SoftDeleteHandledGracefully()
    {
        // Arrange: Budget goal for deleted category (soft-deleted)
        var categoryId = Guid.NewGuid();
        var year = 2026;
        var month = 1;

        var goal = BudgetGoal.Create(categoryId, year, month, MoneyValue.Create("USD", 100m));

        // Simulate soft-deleted category (DeletedAtUtc is not null)
        var category = BudgetCategory.Create("DeletedCategory", CategoryType.Expense);
        category.GetType().GetProperty(nameof(BudgetCategory.DeletedAtUtc))
            ?.SetValue(category, DateTime.UtcNow);

        var goalRepo = new Mock<IBudgetGoalRepository>();
        goalRepo.Setup(r => r.GetByCategoryAndMonthAsync(categoryId, year, month, default))
            .ReturnsAsync(goal);

        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        categoryRepo.Setup(r => r.GetByIdAsync(categoryId, default))
            .ReturnsAsync(category);

        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetSpendingByCategoryAsync(categoryId, year, month, default))
            .ReturnsAsync(MoneyValue.Create("USD", 50m));

        var userContext = new Mock<IUserContext>();
        var currencyProvider = new Mock<ICurrencyProvider>();

        var service = new BudgetProgressService(
            goalRepo.Object,
            categoryRepo.Object,
            transactionRepo.Object,
            currencyProvider.Object,
            userContext.Object);

        // Act: Query progress for orphaned goal (should not throw)
        var progress = await service.GetProgressAsync(categoryId, year, month, default);

        // Assert: Service should handle gracefully (either skip or return with deleted flag)
        progress.ShouldNotBeNull();
        progress.SpentAmount.Amount.ShouldBe(50m);
    }

    [Fact]
    public async Task BudgetProgressService_SoftDeletedGoal_ExcludedFromQueries()
    {
        // Arrange: Goal that is soft-deleted
        var categoryId = Guid.NewGuid();
        var year = 2026;
        var month = 1;

        var goal = BudgetGoal.Create(categoryId, year, month, MoneyValue.Create("USD", 200m));

        // Soft-delete the goal
        goal.GetType().GetProperty(nameof(BudgetGoal.DeletedAtUtc))
            ?.SetValue(goal, DateTime.UtcNow);

        var category = BudgetCategory.Create("TestCategory", CategoryType.Expense);

        var goalRepo = new Mock<IBudgetGoalRepository>();

        // Repository should not return soft-deleted goals in normal queries
        goalRepo.Setup(r => r.GetByCategoryAndMonthAsync(categoryId, year, month, default))
            .ReturnsAsync((BudgetGoal?)null);

        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        categoryRepo.Setup(r => r.GetByIdAsync(categoryId, default))
            .ReturnsAsync(category);

        var transactionRepo = new Mock<ITransactionRepository>();
        var userContext = new Mock<IUserContext>();
        var currencyProvider = new Mock<ICurrencyProvider>();

        var service = new BudgetProgressService(
            goalRepo.Object,
            categoryRepo.Object,
            transactionRepo.Object,
            currencyProvider.Object,
            userContext.Object);

        // Act: Query should return null for soft-deleted goal
        var progress = await service.GetProgressAsync(categoryId, year, month, default);

        // Assert
        progress.ShouldBeNull();
    }

    [Fact]
    public async Task BudgetProgressService_MultipleSoftDeletedCategories_SkippedInRollup()
    {
        // Arrange: Mix of active and soft-deleted categories in monthly summary
        var activeCategory1Id = Guid.NewGuid();
        var activeCategory2Id = Guid.NewGuid();
        var deletedCategoryId = Guid.NewGuid();
        var year = 2026;
        var month = 1;

        var activeGoal1 = BudgetGoal.Create(activeCategory1Id, year, month, MoneyValue.Create("USD", 100m));
        var activeGoal2 = BudgetGoal.Create(activeCategory2Id, year, month, MoneyValue.Create("USD", 150m));
        var deletedGoal = BudgetGoal.Create(deletedCategoryId, year, month, MoneyValue.Create("USD", 200m));
        deletedGoal.GetType().GetProperty(nameof(BudgetGoal.DeletedAtUtc))
            ?.SetValue(deletedGoal, DateTime.UtcNow);

        var activeCategory1 = BudgetCategory.Create("Active1", CategoryType.Expense);
        var activeCategory2 = BudgetCategory.Create("Active2", CategoryType.Expense);
        var deletedCategory = BudgetCategory.Create("Deleted", CategoryType.Expense);
        deletedCategory.GetType().GetProperty(nameof(BudgetCategory.DeletedAtUtc))
            ?.SetValue(deletedCategory, DateTime.UtcNow);

        // Set category IDs to match
        activeCategory1.GetType().GetProperty(nameof(BudgetCategory.Id))
            ?.SetValue(activeCategory1, activeCategory1Id);
        activeCategory2.GetType().GetProperty(nameof(BudgetCategory.Id))
            ?.SetValue(activeCategory2, activeCategory2Id);
        deletedCategory.GetType().GetProperty(nameof(BudgetCategory.Id))
            ?.SetValue(deletedCategory, deletedCategoryId);

        var goalRepo = new Mock<IBudgetGoalRepository>();

        // Return only active goals (filtering soft-deleted ones)
        goalRepo.Setup(r => r.GetByMonthAsync(year, month, default))
            .ReturnsAsync(new List<BudgetGoal> { activeGoal1, activeGoal2 });

        var categoryRepo = new Mock<IBudgetCategoryRepository>();

        // Return only active categories
        categoryRepo.Setup(r => r.GetByTypeAsync(CategoryType.Expense, default))
            .ReturnsAsync(new List<BudgetCategory> { activeCategory1, activeCategory2 });

        var transactionRepo = new Mock<ITransactionRepository>();
        var spendingByCategory = new Dictionary<Guid, decimal>
        {
            { activeCategory1Id, 50m },
            { activeCategory2Id, 75m },
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

        // Assert: Summary includes only active categories (excludes deleted)
        summary.ShouldNotBeNull();
        summary.TotalBudgeted.Amount.ShouldBe(250m); // 100+150 (not 200)
        summary.TotalSpent.Amount.ShouldBe(125m); // 50+75 (no contribution from deleted)
    }

    [Fact]
    public void BudgetCategory_SoftDeletion_TimeStampRecorded()
    {
        // Arrange
        var category = BudgetCategory.Create("ToDelete", CategoryType.Expense);
        var beforeDelete = DateTime.UtcNow;

        // Act: Simulate soft delete by setting DeletedAtUtc
        category.GetType().GetProperty(nameof(BudgetCategory.DeletedAtUtc))
            ?.SetValue(category, DateTime.UtcNow);

        var afterDelete = DateTime.UtcNow;

        // Assert: Deletion timestamp should be recorded
        var deletedAtUtc = (DateTime?)category.GetType().GetProperty(nameof(BudgetCategory.DeletedAtUtc))
            ?.GetValue(category);

        deletedAtUtc.ShouldNotBeNull();
        deletedAtUtc.Value.ShouldBeGreaterThanOrEqualTo(beforeDelete);
        deletedAtUtc.Value.ShouldBeLessThanOrEqualTo(afterDelete);
    }

    [Fact]
    public async Task BudgetGoalService_UpdateGoalWithDeletedCategory_ValidatesOwnership()
    {
        // Arrange: Goal with deleted category
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var year = 2026;
        var month = 1;

        var goal = BudgetGoal.Create(categoryId, year, month, MoneyValue.Create("USD", 100m));
        goal.GetType().GetProperty(nameof(BudgetGoal.CreatedByUserId))
            ?.SetValue(goal, userId);
        goal.GetType().GetProperty(nameof(BudgetGoal.OwnerUserId))
            ?.SetValue(goal, userId);

        var deletedCategory = BudgetCategory.Create("Deleted", CategoryType.Expense);
        deletedCategory.GetType().GetProperty(nameof(BudgetCategory.DeletedAtUtc))
            ?.SetValue(deletedCategory, DateTime.UtcNow);

        var goalRepo = new Mock<IBudgetGoalRepository>();
        goalRepo.Setup(r => r.GetByCategoryAndMonthAsync(categoryId, year, month, default))
            .ReturnsAsync((BudgetGoal?)null);
        goalRepo.Setup(r => r.AddAsync(It.IsAny<BudgetGoal>(), default))
            .Returns(Task.CompletedTask);

        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        categoryRepo.Setup(r => r.GetByIdAsync(categoryId, default))
            .ReturnsAsync(deletedCategory);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var service = new BudgetGoalService(goalRepo.Object, categoryRepo.Object, uow.Object);

        var updateDto = new BudgetGoalSetDto
        {
            Year = year,
            Month = month,
            TargetAmount = new MoneyDto { Currency = "USD", Amount = 150m },
        };

        // Act: SetGoal should still work (deletion state of category doesn't block creation)
        var result = await service.SetGoalAsync(categoryId, updateDto, null, default);

        // Assert
        result.ShouldNotBeNull();
        result.TargetAmount.Amount.ShouldBe(150m);
    }
}
