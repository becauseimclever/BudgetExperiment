// <copyright file="BudgetCategoryServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Services;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;
using Moq;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Unit tests for BudgetCategoryService.
/// </summary>
public class BudgetCategoryServiceTests
{
    private static BudgetCategoryService CreateService(
        Mock<IBudgetCategoryRepository>? categoryRepo = null,
        Mock<IBudgetGoalRepository>? goalRepo = null,
        Mock<IUnitOfWork>? uow = null)
    {
        return new BudgetCategoryService(
            categoryRepo?.Object ?? new Mock<IBudgetCategoryRepository>().Object,
            goalRepo?.Object ?? new Mock<IBudgetGoalRepository>().Object,
            uow?.Object ?? new Mock<IUnitOfWork>().Object);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_CategoryDto()
    {
        // Arrange
        var category = BudgetCategory.Create("Groceries", CategoryType.Expense, "cart", "#4CAF50");
        var repo = new Mock<IBudgetCategoryRepository>();
        repo.Setup(r => r.GetByIdAsync(category.Id, default)).ReturnsAsync(category);
        var service = CreateService(categoryRepo: repo);

        // Act
        var result = await service.GetByIdAsync(category.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(category.Id, result.Id);
        Assert.Equal("Groceries", result.Name);
        Assert.Equal("cart", result.Icon);
        Assert.Equal("#4CAF50", result.Color);
        Assert.Equal("Expense", result.Type);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Null_When_Not_Found()
    {
        // Arrange
        var repo = new Mock<IBudgetCategoryRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((BudgetCategory?)null);
        var service = CreateService(categoryRepo: repo);

        // Act
        var result = await service.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_Returns_All_Categories()
    {
        // Arrange
        var categories = new List<BudgetCategory>
        {
            BudgetCategory.Create("Groceries", CategoryType.Expense),
            BudgetCategory.Create("Salary", CategoryType.Income),
        };
        var repo = new Mock<IBudgetCategoryRepository>();
        repo.Setup(r => r.GetAllAsync(default)).ReturnsAsync(categories);
        var service = CreateService(categoryRepo: repo);

        // Act
        var result = await service.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetActiveAsync_Returns_Only_Active_Categories()
    {
        // Arrange
        var activeCategory = BudgetCategory.Create("Groceries", CategoryType.Expense);
        var repo = new Mock<IBudgetCategoryRepository>();
        repo.Setup(r => r.GetActiveAsync(default)).ReturnsAsync(new List<BudgetCategory> { activeCategory });
        var service = CreateService(categoryRepo: repo);

        // Act
        var result = await service.GetActiveAsync();

        // Assert
        Assert.Single(result);
        Assert.True(result[0].IsActive);
    }

    [Fact]
    public async Task CreateAsync_Creates_Category()
    {
        // Arrange
        var repo = new Mock<IBudgetCategoryRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<BudgetCategory>(), default)).Returns(Task.CompletedTask);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        var service = CreateService(categoryRepo: repo, uow: uow);
        var dto = new BudgetCategoryCreateDto
        {
            Name = "Groceries",
            Type = "Expense",
            Icon = "cart",
            Color = "#4CAF50",
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.Equal("Groceries", result.Name);
        Assert.Equal("Expense", result.Type);
        Assert.Equal("cart", result.Icon);
        Assert.Equal("#4CAF50", result.Color);
        Assert.NotEqual(Guid.Empty, result.Id);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_With_Invalid_Type_Throws()
    {
        // Arrange
        var service = CreateService();
        var dto = new BudgetCategoryCreateDto { Name = "Test", Type = "InvalidType" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(dto));
        Assert.Contains("type", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_With_InitialBudget_Creates_BudgetGoal()
    {
        // Arrange
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        categoryRepo.Setup(r => r.AddAsync(It.IsAny<BudgetCategory>(), default)).Returns(Task.CompletedTask);
        var goalRepo = new Mock<IBudgetGoalRepository>();
        goalRepo.Setup(r => r.AddAsync(It.IsAny<BudgetGoal>(), default)).Returns(Task.CompletedTask);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        var service = CreateService(categoryRepo: categoryRepo, goalRepo: goalRepo, uow: uow);
        var dto = new BudgetCategoryCreateDto
        {
            Name = "Groceries",
            Type = "Expense",
            Icon = "cart",
            Color = "#4CAF50",
            InitialBudget = new MoneyDto { Amount = 500m, Currency = "USD" },
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.Equal("Groceries", result.Name);
        goalRepo.Verify(r => r.AddAsync(It.Is<BudgetGoal>(g =>
            g.TargetAmount.Amount == 500m &&
            g.TargetAmount.Currency == "USD" &&
            g.Year == DateTime.UtcNow.Year &&
            g.Month == DateTime.UtcNow.Month), default), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_With_InitialBudget_On_Income_Category_Does_Not_Create_Goal()
    {
        // Arrange
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        categoryRepo.Setup(r => r.AddAsync(It.IsAny<BudgetCategory>(), default)).Returns(Task.CompletedTask);
        var goalRepo = new Mock<IBudgetGoalRepository>();
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        var service = CreateService(categoryRepo: categoryRepo, goalRepo: goalRepo, uow: uow);
        var dto = new BudgetCategoryCreateDto
        {
            Name = "Salary",
            Type = "Income",
            InitialBudget = new MoneyDto { Amount = 5000m, Currency = "USD" },
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.Equal("Salary", result.Name);
        goalRepo.Verify(r => r.AddAsync(It.IsAny<BudgetGoal>(), default), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_Updates_Category()
    {
        // Arrange
        var category = BudgetCategory.Create("Original", CategoryType.Expense);
        var repo = new Mock<IBudgetCategoryRepository>();
        repo.Setup(r => r.GetByIdAsync(category.Id, default)).ReturnsAsync(category);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        var service = CreateService(categoryRepo: repo, uow: uow);
        var dto = new BudgetCategoryUpdateDto
        {
            Name = "Updated",
            Icon = "shopping",
            Color = "#FF0000",
            SortOrder = 5,
        };

        // Act
        var result = await service.UpdateAsync(category.Id, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated", result.Name);
        Assert.Equal("shopping", result.Icon);
        Assert.Equal("#FF0000", result.Color);
        Assert.Equal(5, result.SortOrder);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_Returns_Null_When_Not_Found()
    {
        // Arrange
        var repo = new Mock<IBudgetCategoryRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((BudgetCategory?)null);
        var uow = new Mock<IUnitOfWork>();
        var service = CreateService(categoryRepo: repo, uow: uow);

        // Act
        var result = await service.UpdateAsync(Guid.NewGuid(), new BudgetCategoryUpdateDto());

        // Assert
        Assert.Null(result);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task DeactivateAsync_Deactivates_Category()
    {
        // Arrange
        var category = BudgetCategory.Create("Groceries", CategoryType.Expense);
        var repo = new Mock<IBudgetCategoryRepository>();
        repo.Setup(r => r.GetByIdAsync(category.Id, default)).ReturnsAsync(category);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        var service = CreateService(categoryRepo: repo, uow: uow);

        // Act
        var result = await service.DeactivateAsync(category.Id);

        // Assert
        Assert.True(result);
        Assert.False(category.IsActive);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_Returns_False_When_Not_Found()
    {
        // Arrange
        var repo = new Mock<IBudgetCategoryRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((BudgetCategory?)null);
        var uow = new Mock<IUnitOfWork>();
        var service = CreateService(categoryRepo: repo, uow: uow);

        // Act
        var result = await service.DeactivateAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task ActivateAsync_Activates_Category()
    {
        // Arrange
        var category = BudgetCategory.Create("Groceries", CategoryType.Expense);
        category.Deactivate();
        var repo = new Mock<IBudgetCategoryRepository>();
        repo.Setup(r => r.GetByIdAsync(category.Id, default)).ReturnsAsync(category);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        var service = CreateService(categoryRepo: repo, uow: uow);

        // Act
        var result = await service.ActivateAsync(category.Id);

        // Assert
        Assert.True(result);
        Assert.True(category.IsActive);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Removes_Category()
    {
        // Arrange
        var category = BudgetCategory.Create("Groceries", CategoryType.Expense);
        var repo = new Mock<IBudgetCategoryRepository>();
        repo.Setup(r => r.GetByIdAsync(category.Id, default)).ReturnsAsync(category);
        repo.Setup(r => r.RemoveAsync(category, default)).Returns(Task.CompletedTask);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        var service = CreateService(categoryRepo: repo, uow: uow);

        // Act
        var result = await service.DeleteAsync(category.Id);

        // Assert
        Assert.True(result);
        repo.Verify(r => r.RemoveAsync(category, default), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Returns_False_When_Not_Found()
    {
        // Arrange
        var repo = new Mock<IBudgetCategoryRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((BudgetCategory?)null);
        var uow = new Mock<IUnitOfWork>();
        var service = CreateService(categoryRepo: repo, uow: uow);

        // Act
        var result = await service.DeleteAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }
}
