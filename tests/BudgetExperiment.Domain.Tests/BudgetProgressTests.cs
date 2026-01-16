// <copyright file="BudgetProgressTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the BudgetProgress value object.
/// </summary>
public class BudgetProgressTests
{
    [Fact]
    public void Create_With_Valid_Data_Creates_Progress()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var categoryName = "Groceries";
        var targetAmount = MoneyValue.Create("USD", 500m);
        var spentAmount = MoneyValue.Create("USD", 300m);

        // Act
        var progress = BudgetProgress.Create(
            categoryId,
            categoryName,
            "cart",
            "#4CAF50",
            targetAmount,
            spentAmount,
            5);

        // Assert
        Assert.Equal(categoryId, progress.CategoryId);
        Assert.Equal(categoryName, progress.CategoryName);
        Assert.Equal("cart", progress.CategoryIcon);
        Assert.Equal("#4CAF50", progress.CategoryColor);
        Assert.Equal(targetAmount, progress.TargetAmount);
        Assert.Equal(spentAmount, progress.SpentAmount);
        Assert.Equal(5, progress.TransactionCount);
    }

    [Fact]
    public void RemainingAmount_Is_Target_Minus_Spent()
    {
        // Arrange
        var targetAmount = MoneyValue.Create("USD", 500m);
        var spentAmount = MoneyValue.Create("USD", 300m);

        // Act
        var progress = BudgetProgress.Create(
            Guid.NewGuid(),
            "Groceries",
            null,
            null,
            targetAmount,
            spentAmount,
            0);

        // Assert
        Assert.Equal(200m, progress.RemainingAmount.Amount);
    }

    [Fact]
    public void RemainingAmount_Can_Be_Negative_When_OverBudget()
    {
        // Arrange
        var targetAmount = MoneyValue.Create("USD", 500m);
        var spentAmount = MoneyValue.Create("USD", 600m);

        // Act
        var progress = BudgetProgress.Create(
            Guid.NewGuid(),
            "Groceries",
            null,
            null,
            targetAmount,
            spentAmount,
            0);

        // Assert
        Assert.Equal(-100m, progress.RemainingAmount.Amount);
    }

    [Fact]
    public void PercentUsed_Is_Calculated_Correctly()
    {
        // Arrange
        var targetAmount = MoneyValue.Create("USD", 500m);
        var spentAmount = MoneyValue.Create("USD", 250m);

        // Act
        var progress = BudgetProgress.Create(
            Guid.NewGuid(),
            "Groceries",
            null,
            null,
            targetAmount,
            spentAmount,
            0);

        // Assert
        Assert.Equal(50m, progress.PercentUsed);
    }

    [Fact]
    public void PercentUsed_Can_Exceed_100_When_OverBudget()
    {
        // Arrange
        var targetAmount = MoneyValue.Create("USD", 200m);
        var spentAmount = MoneyValue.Create("USD", 246m);

        // Act
        var progress = BudgetProgress.Create(
            Guid.NewGuid(),
            "Entertainment",
            null,
            null,
            targetAmount,
            spentAmount,
            0);

        // Assert
        Assert.Equal(123m, progress.PercentUsed);
    }

    [Fact]
    public void PercentUsed_Is_Zero_When_No_Spending()
    {
        // Arrange
        var targetAmount = MoneyValue.Create("USD", 500m);
        var spentAmount = MoneyValue.Create("USD", 0m);

        // Act
        var progress = BudgetProgress.Create(
            Guid.NewGuid(),
            "Groceries",
            null,
            null,
            targetAmount,
            spentAmount,
            0);

        // Assert
        Assert.Equal(0m, progress.PercentUsed);
    }

    [Fact]
    public void PercentUsed_Is_Zero_When_Target_Is_Zero()
    {
        // Arrange
        var targetAmount = MoneyValue.Create("USD", 0m);
        var spentAmount = MoneyValue.Create("USD", 100m);

        // Act
        var progress = BudgetProgress.Create(
            Guid.NewGuid(),
            "Groceries",
            null,
            null,
            targetAmount,
            spentAmount,
            0);

        // Assert - avoid division by zero
        Assert.Equal(0m, progress.PercentUsed);
    }

    [Fact]
    public void Status_Is_OnTrack_When_Under_80_Percent()
    {
        // Arrange
        var targetAmount = MoneyValue.Create("USD", 500m);
        var spentAmount = MoneyValue.Create("USD", 395m); // 79%

        // Act
        var progress = BudgetProgress.Create(
            Guid.NewGuid(),
            "Groceries",
            null,
            null,
            targetAmount,
            spentAmount,
            0);

        // Assert
        Assert.Equal(BudgetStatus.OnTrack, progress.Status);
    }

    [Fact]
    public void Status_Is_Warning_When_At_80_Percent()
    {
        // Arrange
        var targetAmount = MoneyValue.Create("USD", 500m);
        var spentAmount = MoneyValue.Create("USD", 400m); // 80%

        // Act
        var progress = BudgetProgress.Create(
            Guid.NewGuid(),
            "Groceries",
            null,
            null,
            targetAmount,
            spentAmount,
            0);

        // Assert
        Assert.Equal(BudgetStatus.Warning, progress.Status);
    }

    [Fact]
    public void Status_Is_Warning_When_At_99_Percent()
    {
        // Arrange
        var targetAmount = MoneyValue.Create("USD", 500m);
        var spentAmount = MoneyValue.Create("USD", 495m); // 99%

        // Act
        var progress = BudgetProgress.Create(
            Guid.NewGuid(),
            "Groceries",
            null,
            null,
            targetAmount,
            spentAmount,
            0);

        // Assert
        Assert.Equal(BudgetStatus.Warning, progress.Status);
    }

    [Fact]
    public void Status_Is_OverBudget_When_At_100_Percent()
    {
        // Arrange
        var targetAmount = MoneyValue.Create("USD", 500m);
        var spentAmount = MoneyValue.Create("USD", 500m); // 100%

        // Act
        var progress = BudgetProgress.Create(
            Guid.NewGuid(),
            "Groceries",
            null,
            null,
            targetAmount,
            spentAmount,
            0);

        // Assert
        Assert.Equal(BudgetStatus.OverBudget, progress.Status);
    }

    [Fact]
    public void Status_Is_OverBudget_When_Over_100_Percent()
    {
        // Arrange
        var targetAmount = MoneyValue.Create("USD", 500m);
        var spentAmount = MoneyValue.Create("USD", 600m); // 120%

        // Act
        var progress = BudgetProgress.Create(
            Guid.NewGuid(),
            "Groceries",
            null,
            null,
            targetAmount,
            spentAmount,
            0);

        // Assert
        Assert.Equal(BudgetStatus.OverBudget, progress.Status);
    }

    [Fact]
    public void CreateWithNoBudget_Returns_NoBudgetSet_Status()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var spentAmount = MoneyValue.Create("USD", 300m);

        // Act
        var progress = BudgetProgress.CreateWithNoBudget(
            categoryId,
            "Groceries",
            "cart",
            "#4CAF50",
            spentAmount,
            5);

        // Assert
        Assert.Equal(BudgetStatus.NoBudgetSet, progress.Status);
        Assert.Equal(MoneyValue.Zero("USD"), progress.TargetAmount);
        Assert.Equal(spentAmount, progress.SpentAmount);
        Assert.Equal(0m, progress.PercentUsed);
    }
}
