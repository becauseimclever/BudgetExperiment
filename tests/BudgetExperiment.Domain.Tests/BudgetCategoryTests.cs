// <copyright file="BudgetCategoryTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the BudgetCategory entity.
/// </summary>
public class BudgetCategoryTests
{
    [Fact]
    public void Create_With_Valid_Data_Creates_Category()
    {
        // Arrange
        var name = "Groceries";
        var type = CategoryType.Expense;

        // Act
        var category = BudgetCategory.Create(name, type);

        // Assert
        Assert.NotEqual(Guid.Empty, category.Id);
        Assert.Equal(name, category.Name);
        Assert.Equal(type, category.Type);
        Assert.Null(category.Icon);
        Assert.Null(category.Color);
        Assert.Equal(0, category.SortOrder);
        Assert.True(category.IsActive);
        Assert.NotEqual(default, category.CreatedAtUtc);
        Assert.NotEqual(default, category.UpdatedAtUtc);
    }

    [Fact]
    public void Create_With_Icon_And_Color_Sets_Properties()
    {
        // Arrange
        var name = "Transportation";
        var type = CategoryType.Expense;
        var icon = "car";
        var color = "#FF9800";

        // Act
        var category = BudgetCategory.Create(name, type, icon, color);

        // Assert
        Assert.Equal(icon, category.Icon);
        Assert.Equal(color, category.Color);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_With_Empty_Name_Throws(string? name)
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => BudgetCategory.Create(name!, CategoryType.Expense));
        Assert.Contains("name", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_Trims_Name()
    {
        // Arrange
        var name = "  Groceries  ";

        // Act
        var category = BudgetCategory.Create(name, CategoryType.Expense);

        // Assert
        Assert.Equal("Groceries", category.Name);
    }

    [Fact]
    public void Create_With_Name_Too_Long_Throws()
    {
        // Arrange
        var name = new string('A', 101);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => BudgetCategory.Create(name, CategoryType.Expense));
        Assert.Contains("100", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Update_Changes_Properties_And_UpdatedAtUtc()
    {
        // Arrange
        var category = BudgetCategory.Create("Original", CategoryType.Expense);
        var originalUpdatedAt = category.UpdatedAtUtc;

        // Act
        category.Update("New Name", "shopping", "#4CAF50", 5);

        // Assert
        Assert.Equal("New Name", category.Name);
        Assert.Equal("shopping", category.Icon);
        Assert.Equal("#4CAF50", category.Color);
        Assert.Equal(5, category.SortOrder);
        Assert.True(category.UpdatedAtUtc >= originalUpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_With_Empty_Name_Throws(string? name)
    {
        // Arrange
        var category = BudgetCategory.Create("Original", CategoryType.Expense);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => category.Update(name!, "icon", "#000000", 0));
        Assert.Contains("name", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Update_Trims_Name()
    {
        // Arrange
        var category = BudgetCategory.Create("Original", CategoryType.Expense);

        // Act
        category.Update("  New Name  ", null, null, 0);

        // Assert
        Assert.Equal("New Name", category.Name);
    }

    [Fact]
    public void Deactivate_Sets_IsActive_False()
    {
        // Arrange
        var category = BudgetCategory.Create("Groceries", CategoryType.Expense);
        Assert.True(category.IsActive);

        // Act
        category.Deactivate();

        // Assert
        Assert.False(category.IsActive);
    }

    [Fact]
    public void Activate_Sets_IsActive_True()
    {
        // Arrange
        var category = BudgetCategory.Create("Groceries", CategoryType.Expense);
        category.Deactivate();
        Assert.False(category.IsActive);

        // Act
        category.Activate();

        // Assert
        Assert.True(category.IsActive);
    }

    [Fact]
    public void Deactivate_Updates_UpdatedAtUtc()
    {
        // Arrange
        var category = BudgetCategory.Create("Groceries", CategoryType.Expense);
        var originalUpdatedAt = category.UpdatedAtUtc;

        // Act
        category.Deactivate();

        // Assert
        Assert.True(category.UpdatedAtUtc >= originalUpdatedAt);
    }

    [Fact]
    public void Activate_Updates_UpdatedAtUtc()
    {
        // Arrange
        var category = BudgetCategory.Create("Groceries", CategoryType.Expense);
        category.Deactivate();
        var originalUpdatedAt = category.UpdatedAtUtc;

        // Act
        category.Activate();

        // Assert
        Assert.True(category.UpdatedAtUtc >= originalUpdatedAt);
    }

    [Theory]
    [InlineData(CategoryType.Expense)]
    [InlineData(CategoryType.Income)]
    [InlineData(CategoryType.Transfer)]
    public void Create_Accepts_All_Category_Types(CategoryType type)
    {
        // Act
        var category = BudgetCategory.Create("Test", type);

        // Assert
        Assert.Equal(type, category.Type);
    }

    [Fact]
    public void Update_With_Negative_SortOrder_Is_Allowed()
    {
        // Arrange
        var category = BudgetCategory.Create("Test", CategoryType.Expense);

        // Act - negative sort order might be used for pinned items
        category.Update("Test", null, null, -1);

        // Assert
        Assert.Equal(-1, category.SortOrder);
    }
}
