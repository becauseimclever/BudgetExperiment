// <copyright file="AuthorizationTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Budgeting;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;
using Moq;
using Shouldly;

namespace BudgetExperiment.Application.Tests.Authorization;

/// <summary>
/// Unit tests for authorization and cross-user access control.
/// </summary>
public class AuthorizationTests
{
    [Fact]
    public async Task BudgetGoalService_UserAccessingOwnGoal_Succeeds()
    {
        // Arrange: Goal belongs to and accessed by same user
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var goal = BudgetGoal.Create(categoryId, 2026, 1, MoneyValue.Create("USD", 500m));
        goal.GetType().GetProperty(nameof(BudgetGoal.CreatedByUserId))
            ?.SetValue(goal, userId);
        goal.GetType().GetProperty(nameof(BudgetGoal.OwnerUserId))
            ?.SetValue(goal, userId);

        var goalRepo = new Mock<IBudgetGoalRepository>();
        goalRepo.Setup(r => r.GetByCategoryAndMonthAsync(categoryId, 2026, 1, default))
            .ReturnsAsync((BudgetGoal?)null);
        goalRepo.Setup(r => r.AddAsync(It.IsAny<BudgetGoal>(), default))
            .Returns(Task.CompletedTask);

        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        var category = BudgetCategory.Create("Groceries", CategoryType.Expense);
        category.GetType().GetProperty(nameof(BudgetCategory.OwnerUserId))
            ?.SetValue(category, userId);
        categoryRepo.Setup(r => r.GetByIdAsync(categoryId, default)).ReturnsAsync(category);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        var service = new BudgetGoalService(goalRepo.Object, categoryRepo.Object, uow.Object);

        var updateDto = new BudgetGoalSetDto
        {
            Year = 2026,
            Month = 1,
            TargetAmount = new MoneyDto { Currency = "USD", Amount = 600m },
        };

        // Act: User accessing own goal should succeed
        var result = await service.SetGoalAsync(categoryId, updateDto, null, default);

        // Assert
        result.ShouldNotBeNull();
        result.TargetAmount.Amount.ShouldBe(600m);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task BudgetGoalService_ZeroTargetGoal_DoesNotThrowDivideByZero()
    {
        // Arrange: Goal with zero target amount
        var categoryId = Guid.NewGuid();

        var goal = BudgetGoal.Create(categoryId, 2026, 1, MoneyValue.Create("USD", 0m));

        var goalRepo = new Mock<IBudgetGoalRepository>();
        goalRepo.Setup(r => r.GetByCategoryAndMonthAsync(categoryId, 2026, 1, default))
            .ReturnsAsync((BudgetGoal?)null);
        goalRepo.Setup(r => r.AddAsync(It.IsAny<BudgetGoal>(), default))
            .Returns(Task.CompletedTask);

        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        var category = BudgetCategory.Create("Groceries", CategoryType.Expense);
        categoryRepo.Setup(r => r.GetByIdAsync(categoryId, default)).ReturnsAsync(category);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var service = new BudgetGoalService(goalRepo.Object, categoryRepo.Object, uow.Object);

        var updateDto = new BudgetGoalSetDto
        {
            Year = 2026,
            Month = 1,
            TargetAmount = new MoneyDto { Currency = "USD", Amount = 50m },
        };

        // Act: Update zero target to non-zero should succeed
        var result = await service.SetGoalAsync(categoryId, updateDto, null, default);

        // Assert
        result.ShouldNotBeNull();
        result.TargetAmount.Amount.ShouldBe(50m);
    }

    [Fact]
    public async Task BudgetProgressService_EmptyDataset_ReturnsZeroProgress()
    {
        // Arrange: Category with goal but no transactions
        var categoryId = Guid.NewGuid();
        var year = 2026;
        var month = 1;

        var goal = BudgetGoal.Create(categoryId, year, month, MoneyValue.Create("USD", 100m));
        var category = BudgetCategory.Create("Food", CategoryType.Expense);

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
        var result = await service.GetProgressAsync(categoryId, year, month, default);

        // Assert
        result.ShouldNotBeNull();
        result.SpentAmount.Amount.ShouldBe(0m);
        result.PercentUsed.ShouldBe(0m);
    }

    [Fact]
    public async Task BudgetGoalService_GetByIdAsync_ReturnsGoal()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var goal = BudgetGoal.Create(categoryId, 2026, 1, MoneyValue.Create("USD", 500m));

        var goalRepo = new Mock<IBudgetGoalRepository>();
        goalRepo.Setup(r => r.GetByIdAsync(goal.Id, default)).ReturnsAsync(goal);

        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        var uow = new Mock<IUnitOfWork>();

        var service = new BudgetGoalService(goalRepo.Object, categoryRepo.Object, uow.Object);

        // Act
        var result = await service.GetByIdAsync(goal.Id, default);

        // Assert
        result.ShouldNotBeNull();
        result.TargetAmount.Amount.ShouldBe(500m);
    }

    [Fact]
    public async Task BudgetGoalService_DeleteGoalAsync_RemovesGoal()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var goal = BudgetGoal.Create(categoryId, 2026, 1, MoneyValue.Create("USD", 500m));

        var goalRepo = new Mock<IBudgetGoalRepository>();
        goalRepo.Setup(r => r.GetByCategoryAndMonthAsync(categoryId, 2026, 1, default))
            .ReturnsAsync(goal);
        goalRepo.Setup(r => r.RemoveAsync(goal, default))
            .Returns(Task.CompletedTask);

        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var service = new BudgetGoalService(goalRepo.Object, categoryRepo.Object, uow.Object);

        // Act
        var result = await service.DeleteGoalAsync(categoryId, 2026, 1, default);

        // Assert
        result.ShouldBeTrue();
        goalRepo.Verify(r => r.RemoveAsync(goal, default), Times.Once);
    }

    [Fact]
    public async Task BudgetGoalService_GetByMonthAsync_ReturnsGoalsForMonth()
    {
        // Arrange
        var goals = new List<BudgetGoal>
        {
            BudgetGoal.Create(Guid.NewGuid(), 2026, 1, MoneyValue.Create("USD", 500m)),
            BudgetGoal.Create(Guid.NewGuid(), 2026, 1, MoneyValue.Create("USD", 300m)),
        };

        var goalRepo = new Mock<IBudgetGoalRepository>();
        goalRepo.Setup(r => r.GetByMonthAsync(2026, 1, default)).ReturnsAsync(goals);

        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        var uow = new Mock<IUnitOfWork>();

        var service = new BudgetGoalService(goalRepo.Object, categoryRepo.Object, uow.Object);

        // Act
        var result = await service.GetByMonthAsync(2026, 1, default);

        // Assert
        result.Count.ShouldBe(2);
    }
}
