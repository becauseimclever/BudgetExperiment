// <copyright file="CategorySuggestionTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the CategorySuggestion entity.
/// </summary>
public class CategorySuggestionTests
{
    private const string ValidOwnerId = "user-123";

    #region Create Tests

    [Fact]
    public void Create_With_Valid_Data_Creates_Suggestion()
    {
        // Arrange
        var suggestedName = "Entertainment";
        var suggestedType = CategoryType.Expense;
        var merchantPatterns = new[] { "netflix", "spotify", "hulu" };
        var matchingCount = 25;
        var confidence = 0.85m;

        // Act
        var suggestion = CategorySuggestion.Create(
            suggestedName,
            suggestedType,
            merchantPatterns,
            matchingCount,
            confidence,
            ValidOwnerId,
            "movie",
            "#FF5733");

        // Assert
        Assert.NotEqual(Guid.Empty, suggestion.Id);
        Assert.Equal(suggestedName, suggestion.SuggestedName);
        Assert.Equal(suggestedType, suggestion.SuggestedType);
        Assert.Equal("movie", suggestion.SuggestedIcon);
        Assert.Equal("#FF5733", suggestion.SuggestedColor);
        Assert.Equal(confidence, suggestion.Confidence);
        Assert.Equal(merchantPatterns, suggestion.MerchantPatterns);
        Assert.Equal(matchingCount, suggestion.MatchingTransactionCount);
        Assert.Equal(SuggestionStatus.Pending, suggestion.Status);
        Assert.Equal(ValidOwnerId, suggestion.OwnerId);
        Assert.NotEqual(default, suggestion.CreatedAtUtc);
    }

    [Fact]
    public void Create_With_Minimal_Data_Creates_Suggestion()
    {
        // Arrange & Act
        var suggestion = CategorySuggestion.Create(
            "Groceries",
            CategoryType.Expense,
            new[] { "kroger" },
            5,
            0.75m,
            ValidOwnerId);

        // Assert
        Assert.NotEqual(Guid.Empty, suggestion.Id);
        Assert.Equal("Groceries", suggestion.SuggestedName);
        Assert.Null(suggestion.SuggestedIcon);
        Assert.Null(suggestion.SuggestedColor);
        Assert.Equal(SuggestionStatus.Pending, suggestion.Status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_With_Empty_Name_Throws(string? name)
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            CategorySuggestion.Create(
                name!,
                CategoryType.Expense,
                new[] { "pattern" },
                1,
                0.5m,
                ValidOwnerId));
        Assert.Contains("name", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_With_Name_Too_Long_Throws()
    {
        // Arrange
        var name = new string('A', CategorySuggestion.MaxNameLength + 1);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            CategorySuggestion.Create(
                name,
                CategoryType.Expense,
                new[] { "pattern" },
                1,
                0.5m,
                ValidOwnerId));
        Assert.Contains("name", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void Create_With_Invalid_Confidence_Throws(decimal confidence)
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            CategorySuggestion.Create(
                "Test",
                CategoryType.Expense,
                new[] { "pattern" },
                1,
                confidence,
                ValidOwnerId));
        Assert.Contains("confidence", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_With_Empty_Merchant_Patterns_Throws()
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            CategorySuggestion.Create(
                "Test",
                CategoryType.Expense,
                Array.Empty<string>(),
                1,
                0.5m,
                ValidOwnerId));
        Assert.Contains("pattern", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_With_Negative_Transaction_Count_Throws()
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            CategorySuggestion.Create(
                "Test",
                CategoryType.Expense,
                new[] { "pattern" },
                -1,
                0.5m,
                ValidOwnerId));
        Assert.Contains("transaction", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_With_Empty_OwnerId_Throws(string? ownerId)
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            CategorySuggestion.Create(
                "Test",
                CategoryType.Expense,
                new[] { "pattern" },
                1,
                0.5m,
                ownerId!));
        Assert.Contains("owner", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Accept Tests

    [Fact]
    public void Accept_Sets_Status_To_Accepted()
    {
        // Arrange
        var suggestion = CreateValidSuggestion();

        // Act
        suggestion.Accept();

        // Assert
        Assert.Equal(SuggestionStatus.Accepted, suggestion.Status);
    }

    [Fact]
    public void Accept_When_Already_Accepted_Throws()
    {
        // Arrange
        var suggestion = CreateValidSuggestion();
        suggestion.Accept();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => suggestion.Accept());
        Assert.Contains("pending", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Accept_When_Dismissed_Throws()
    {
        // Arrange
        var suggestion = CreateValidSuggestion();
        suggestion.Dismiss();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => suggestion.Accept());
        Assert.Contains("pending", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Dismiss Tests

    [Fact]
    public void Dismiss_Sets_Status_To_Dismissed()
    {
        // Arrange
        var suggestion = CreateValidSuggestion();

        // Act
        suggestion.Dismiss();

        // Assert
        Assert.Equal(SuggestionStatus.Dismissed, suggestion.Status);
    }

    [Fact]
    public void Dismiss_When_Already_Dismissed_Throws()
    {
        // Arrange
        var suggestion = CreateValidSuggestion();
        suggestion.Dismiss();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => suggestion.Dismiss());
        Assert.Contains("pending", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Dismiss_When_Accepted_Throws()
    {
        // Arrange
        var suggestion = CreateValidSuggestion();
        suggestion.Accept();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => suggestion.Dismiss());
        Assert.Contains("pending", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    private static CategorySuggestion CreateValidSuggestion()
    {
        return CategorySuggestion.Create(
            "Entertainment",
            CategoryType.Expense,
            new[] { "netflix", "spotify" },
            10,
            0.85m,
            ValidOwnerId,
            "movie",
            "#FF5733");
    }
}
