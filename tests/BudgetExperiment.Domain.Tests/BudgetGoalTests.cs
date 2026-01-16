// <copyright file="BudgetGoalTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the BudgetGoal entity.
/// </summary>
public class BudgetGoalTests
{
    [Fact]
    public void Create_With_Valid_Data_Creates_Goal()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var year = 2026;
        var month = 1;
        var targetAmount = MoneyValue.Create("USD", 500m);

        // Act
        var goal = BudgetGoal.Create(categoryId, year, month, targetAmount);

        // Assert
        Assert.NotEqual(Guid.Empty, goal.Id);
        Assert.Equal(categoryId, goal.CategoryId);
        Assert.Equal(year, goal.Year);
        Assert.Equal(month, goal.Month);
        Assert.Equal(targetAmount, goal.TargetAmount);
        Assert.NotEqual(default, goal.CreatedAtUtc);
        Assert.NotEqual(default, goal.UpdatedAtUtc);
    }

    [Fact]
    public void Create_With_Empty_CategoryId_Throws()
    {
        // Arrange
        var targetAmount = MoneyValue.Create("USD", 500m);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => BudgetGoal.Create(Guid.Empty, 2026, 1, targetAmount));
        Assert.Contains("category", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(13)]
    [InlineData(99)]
    public void Create_With_Invalid_Month_Throws(int month)
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var targetAmount = MoneyValue.Create("USD", 500m);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => BudgetGoal.Create(categoryId, 2026, month, targetAmount));
        Assert.Contains("month", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1899)]
    public void Create_With_Invalid_Year_Throws(int year)
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var targetAmount = MoneyValue.Create("USD", 500m);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => BudgetGoal.Create(categoryId, year, 1, targetAmount));
        Assert.Contains("year", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(12)]
    public void Create_Accepts_Valid_Months(int month)
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var targetAmount = MoneyValue.Create("USD", 500m);

        // Act
        var goal = BudgetGoal.Create(categoryId, 2026, month, targetAmount);

        // Assert
        Assert.Equal(month, goal.Month);
    }

    [Fact]
    public void Create_With_Negative_Target_Throws()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var targetAmount = MoneyValue.Create("USD", -100m);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => BudgetGoal.Create(categoryId, 2026, 1, targetAmount));
        Assert.Contains("target", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_With_Zero_Target_Is_Allowed()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var targetAmount = MoneyValue.Create("USD", 0m);

        // Act
        var goal = BudgetGoal.Create(categoryId, 2026, 1, targetAmount);

        // Assert
        Assert.Equal(0m, goal.TargetAmount.Amount);
    }

    [Fact]
    public void UpdateTarget_Changes_Amount_And_UpdatedAtUtc()
    {
        // Arrange
        var goal = BudgetGoal.Create(Guid.NewGuid(), 2026, 1, MoneyValue.Create("USD", 500m));
        var originalUpdatedAt = goal.UpdatedAtUtc;
        var newTarget = MoneyValue.Create("USD", 750m);

        // Act
        goal.UpdateTarget(newTarget);

        // Assert
        Assert.Equal(newTarget, goal.TargetAmount);
        Assert.True(goal.UpdatedAtUtc >= originalUpdatedAt);
    }

    [Fact]
    public void UpdateTarget_With_Negative_Amount_Throws()
    {
        // Arrange
        var goal = BudgetGoal.Create(Guid.NewGuid(), 2026, 1, MoneyValue.Create("USD", 500m));
        var newTarget = MoneyValue.Create("USD", -100m);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => goal.UpdateTarget(newTarget));
        Assert.Contains("target", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UpdateTarget_To_Zero_Is_Allowed()
    {
        // Arrange
        var goal = BudgetGoal.Create(Guid.NewGuid(), 2026, 1, MoneyValue.Create("USD", 500m));
        var newTarget = MoneyValue.Create("USD", 0m);

        // Act
        goal.UpdateTarget(newTarget);

        // Assert
        Assert.Equal(0m, goal.TargetAmount.Amount);
    }
}
