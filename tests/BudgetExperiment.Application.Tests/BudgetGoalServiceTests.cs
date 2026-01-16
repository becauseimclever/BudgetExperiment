// <copyright file="BudgetGoalServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Services;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;
using Moq;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Unit tests for BudgetGoalService.
/// </summary>
public class BudgetGoalServiceTests
{
    [Fact]
    public async Task GetByIdAsync_Returns_GoalDto()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var goal = BudgetGoal.Create(categoryId, 2026, 1, MoneyValue.Create("USD", 500m));
        var repo = new Mock<IBudgetGoalRepository>();
        repo.Setup(r => r.GetByIdAsync(goal.Id, default)).ReturnsAsync(goal);
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        var uow = new Mock<IUnitOfWork>();
        var service = new BudgetGoalService(repo.Object, categoryRepo.Object, uow.Object);

        // Act
        var result = await service.GetByIdAsync(goal.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(goal.Id, result.Id);
        Assert.Equal(categoryId, result.CategoryId);
        Assert.Equal(2026, result.Year);
        Assert.Equal(1, result.Month);
        Assert.Equal(500m, result.TargetAmount.Amount);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Null_When_Not_Found()
    {
        // Arrange
        var repo = new Mock<IBudgetGoalRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((BudgetGoal?)null);
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        var uow = new Mock<IUnitOfWork>();
        var service = new BudgetGoalService(repo.Object, categoryRepo.Object, uow.Object);

        // Act
        var result = await service.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByMonthAsync_Returns_Goals_For_Month()
    {
        // Arrange
        var goals = new List<BudgetGoal>
        {
            BudgetGoal.Create(Guid.NewGuid(), 2026, 1, MoneyValue.Create("USD", 500m)),
            BudgetGoal.Create(Guid.NewGuid(), 2026, 1, MoneyValue.Create("USD", 300m)),
        };
        var repo = new Mock<IBudgetGoalRepository>();
        repo.Setup(r => r.GetByMonthAsync(2026, 1, default)).ReturnsAsync(goals);
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        var uow = new Mock<IUnitOfWork>();
        var service = new BudgetGoalService(repo.Object, categoryRepo.Object, uow.Object);

        // Act
        var result = await service.GetByMonthAsync(2026, 1);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task SetGoalAsync_Creates_New_Goal()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = BudgetCategory.Create("Groceries", CategoryType.Expense);
        var repo = new Mock<IBudgetGoalRepository>();
        repo.Setup(r => r.GetByCategoryAndMonthAsync(categoryId, 2026, 1, default)).ReturnsAsync((BudgetGoal?)null);
        repo.Setup(r => r.AddAsync(It.IsAny<BudgetGoal>(), default)).Returns(Task.CompletedTask);
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        categoryRepo.Setup(r => r.GetByIdAsync(categoryId, default)).ReturnsAsync(category);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        var service = new BudgetGoalService(repo.Object, categoryRepo.Object, uow.Object);
        var dto = new BudgetGoalSetDto
        {
            Year = 2026,
            Month = 1,
            TargetAmount = new MoneyDto { Amount = 500m, Currency = "USD" },
        };

        // Act
        var result = await service.SetGoalAsync(categoryId, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(categoryId, result.CategoryId);
        Assert.Equal(500m, result.TargetAmount.Amount);
        repo.Verify(r => r.AddAsync(It.IsAny<BudgetGoal>(), default), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task SetGoalAsync_Updates_Existing_Goal()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingGoal = BudgetGoal.Create(categoryId, 2026, 1, MoneyValue.Create("USD", 300m));
        var category = BudgetCategory.Create("Groceries", CategoryType.Expense);
        var repo = new Mock<IBudgetGoalRepository>();
        repo.Setup(r => r.GetByCategoryAndMonthAsync(categoryId, 2026, 1, default)).ReturnsAsync(existingGoal);
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        categoryRepo.Setup(r => r.GetByIdAsync(categoryId, default)).ReturnsAsync(category);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        var service = new BudgetGoalService(repo.Object, categoryRepo.Object, uow.Object);
        var dto = new BudgetGoalSetDto
        {
            Year = 2026,
            Month = 1,
            TargetAmount = new MoneyDto { Amount = 500m, Currency = "USD" },
        };

        // Act
        var result = await service.SetGoalAsync(categoryId, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(500m, result.TargetAmount.Amount);
        repo.Verify(r => r.AddAsync(It.IsAny<BudgetGoal>(), default), Times.Never);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task SetGoalAsync_Returns_Null_When_Category_Not_Found()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var repo = new Mock<IBudgetGoalRepository>();
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        categoryRepo.Setup(r => r.GetByIdAsync(categoryId, default)).ReturnsAsync((BudgetCategory?)null);
        var uow = new Mock<IUnitOfWork>();
        var service = new BudgetGoalService(repo.Object, categoryRepo.Object, uow.Object);
        var dto = new BudgetGoalSetDto
        {
            Year = 2026,
            Month = 1,
            TargetAmount = new MoneyDto { Amount = 500m, Currency = "USD" },
        };

        // Act
        var result = await service.SetGoalAsync(categoryId, dto);

        // Assert
        Assert.Null(result);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task DeleteGoalAsync_Removes_Goal()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var goal = BudgetGoal.Create(categoryId, 2026, 1, MoneyValue.Create("USD", 500m));
        var repo = new Mock<IBudgetGoalRepository>();
        repo.Setup(r => r.GetByCategoryAndMonthAsync(categoryId, 2026, 1, default)).ReturnsAsync(goal);
        repo.Setup(r => r.RemoveAsync(goal, default)).Returns(Task.CompletedTask);
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        var service = new BudgetGoalService(repo.Object, categoryRepo.Object, uow.Object);

        // Act
        var result = await service.DeleteGoalAsync(categoryId, 2026, 1);

        // Assert
        Assert.True(result);
        repo.Verify(r => r.RemoveAsync(goal, default), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task DeleteGoalAsync_Returns_False_When_Not_Found()
    {
        // Arrange
        var repo = new Mock<IBudgetGoalRepository>();
        repo.Setup(r => r.GetByCategoryAndMonthAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), default))
            .ReturnsAsync((BudgetGoal?)null);
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        var uow = new Mock<IUnitOfWork>();
        var service = new BudgetGoalService(repo.Object, categoryRepo.Object, uow.Object);

        // Act
        var result = await service.DeleteGoalAsync(Guid.NewGuid(), 2026, 1);

        // Assert
        Assert.False(result);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task GetByCategoryAsync_Returns_Goals_For_Category()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var goals = new List<BudgetGoal>
        {
            BudgetGoal.Create(categoryId, 2026, 1, MoneyValue.Create("USD", 500m)),
            BudgetGoal.Create(categoryId, 2026, 2, MoneyValue.Create("USD", 500m)),
        };
        var repo = new Mock<IBudgetGoalRepository>();
        repo.Setup(r => r.GetByCategoryAsync(categoryId, default)).ReturnsAsync(goals);
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        var uow = new Mock<IUnitOfWork>();
        var service = new BudgetGoalService(repo.Object, categoryRepo.Object, uow.Object);

        // Act
        var result = await service.GetByCategoryAsync(categoryId);

        // Assert
        Assert.Equal(2, result.Count);
    }
}
