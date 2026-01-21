// <copyright file="MerchantKnowledgeBaseTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Categorization;

namespace BudgetExperiment.Application.Tests.Categorization;

/// <summary>
/// Unit tests for the MerchantKnowledgeBase static class.
/// </summary>
public class MerchantKnowledgeBaseTests
{
    #region TryGetMapping Tests

    [Theory]
    [InlineData("NETFLIX.COM", "Entertainment", "movie")]
    [InlineData("netflix", "Entertainment", "movie")]
    [InlineData("NETFLIX MONTHLY", "Entertainment", "movie")]
    [InlineData("SPOTIFY PREMIUM", "Entertainment", "music")]
    [InlineData("MCDONALD'S", "Dining", "restaurant")]
    [InlineData("AMAZON.COM*123ABC", "Shopping", "shopping-cart")]
    [InlineData("WALMART SUPERCENTER", "Shopping", "shopping-cart")]
    [InlineData("SHELL GAS STATION", "Gas", "fuel")]
    [InlineData("KROGER #1234", "Groceries", "grocery")]
    public void TryGetMapping_With_Known_Merchant_Returns_Mapping(string description, string expectedCategory, string expectedIcon)
    {
        // Act
        var result = MerchantKnowledgeBase.TryGetMapping(description, out var mapping);

        // Assert
        Assert.True(result);
        Assert.NotNull(mapping);
        Assert.Equal(expectedCategory, mapping.Value.Category);
        Assert.Equal(expectedIcon, mapping.Value.Icon);
    }

    [Theory]
    [InlineData("XYZ RANDOM STORE")]
    [InlineData("UNKNOWN MERCHANT")]
    [InlineData("LOCAL BUSINESS")]
    [InlineData("")]
    public void TryGetMapping_With_Unknown_Merchant_Returns_False(string description)
    {
        // Act
        var result = MerchantKnowledgeBase.TryGetMapping(description, out var mapping);

        // Assert
        Assert.False(result);
        Assert.Null(mapping);
    }

    [Fact]
    public void TryGetMapping_Is_Case_Insensitive()
    {
        // Arrange & Act
        var resultLower = MerchantKnowledgeBase.TryGetMapping("netflix", out var mappingLower);
        var resultUpper = MerchantKnowledgeBase.TryGetMapping("NETFLIX", out var mappingUpper);
        var resultMixed = MerchantKnowledgeBase.TryGetMapping("NeTfLiX", out var mappingMixed);

        // Assert
        Assert.True(resultLower);
        Assert.True(resultUpper);
        Assert.True(resultMixed);
        Assert.Equal(mappingLower, mappingUpper);
        Assert.Equal(mappingLower, mappingMixed);
    }

    #endregion

    #region GetAllMappings Tests

    [Fact]
    public void GetAllMappings_Returns_All_Mappings()
    {
        // Act
        var mappings = MerchantKnowledgeBase.GetAllMappings();

        // Assert
        Assert.NotEmpty(mappings);
        Assert.True(mappings.Count >= 60, "Should have at least 60 default mappings");
    }

    [Fact]
    public void GetAllMappings_Contains_Expected_Categories()
    {
        // Act
        var mappings = MerchantKnowledgeBase.GetAllMappings();
        var categories = mappings.Values.Select(m => m.Category).Distinct().ToList();

        // Assert
        Assert.Contains("Entertainment", categories);
        Assert.Contains("Dining", categories);
        Assert.Contains("Shopping", categories);
        Assert.Contains("Groceries", categories);
        Assert.Contains("Gas", categories);
        Assert.Contains("Transportation", categories);
        Assert.Contains("Utilities", categories);
        Assert.Contains("Subscriptions", categories);
        Assert.Contains("Healthcare", categories);
        Assert.Contains("Travel", categories);
    }

    #endregion

    #region GetCategoryType Tests

    [Theory]
    [InlineData("Entertainment", CategoryType.Expense)]
    [InlineData("Dining", CategoryType.Expense)]
    [InlineData("Shopping", CategoryType.Expense)]
    [InlineData("Groceries", CategoryType.Expense)]
    [InlineData("Gas", CategoryType.Expense)]
    [InlineData("Paycheck", CategoryType.Income)]
    [InlineData("Transfer", CategoryType.Transfer)]
    public void GetCategoryType_Returns_Correct_Type(string categoryName, CategoryType expectedType)
    {
        // Act
        var result = MerchantKnowledgeBase.GetCategoryType(categoryName);

        // Assert
        Assert.Equal(expectedType, result);
    }

    [Fact]
    public void GetCategoryType_Defaults_To_Expense_For_Unknown()
    {
        // Act
        var result = MerchantKnowledgeBase.GetCategoryType("Unknown Category");

        // Assert
        Assert.Equal(CategoryType.Expense, result);
    }

    #endregion
}
