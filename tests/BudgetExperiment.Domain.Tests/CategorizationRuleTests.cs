// <copyright file="CategorizationRuleTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the CategorizationRule entity.
/// </summary>
public class CategorizationRuleTests
{
    private static readonly Guid ValidCategoryId = Guid.NewGuid();

    #region Create Tests

    [Fact]
    public void Create_With_Valid_Data_Creates_Rule()
    {
        // Arrange
        var name = "Grocery Stores";
        var pattern = "WALMART";

        // Act
        var rule = CategorizationRule.Create(name, RuleMatchType.Contains, pattern, ValidCategoryId);

        // Assert
        Assert.NotEqual(Guid.Empty, rule.Id);
        Assert.Equal(name, rule.Name);
        Assert.Equal(RuleMatchType.Contains, rule.MatchType);
        Assert.Equal(pattern, rule.Pattern);
        Assert.Equal(ValidCategoryId, rule.CategoryId);
        Assert.Equal(100, rule.Priority);
        Assert.True(rule.IsActive);
        Assert.False(rule.CaseSensitive);
        Assert.NotEqual(default, rule.CreatedAtUtc);
        Assert.NotEqual(default, rule.UpdatedAtUtc);
    }

    [Fact]
    public void Create_With_Custom_Priority_Sets_Priority()
    {
        // Arrange & Act
        var rule = CategorizationRule.Create("Test", RuleMatchType.Contains, "TEST", ValidCategoryId, priority: 5);

        // Assert
        Assert.Equal(5, rule.Priority);
    }

    [Fact]
    public void Create_With_CaseSensitive_Sets_CaseSensitive()
    {
        // Arrange & Act
        var rule = CategorizationRule.Create("Test", RuleMatchType.Contains, "Test", ValidCategoryId, caseSensitive: true);

        // Assert
        Assert.True(rule.CaseSensitive);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_With_Empty_Name_Throws(string? name)
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            CategorizationRule.Create(name!, RuleMatchType.Contains, "TEST", ValidCategoryId));
        Assert.Contains("name", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_Trims_Name()
    {
        // Arrange & Act
        var rule = CategorizationRule.Create("  Grocery Stores  ", RuleMatchType.Contains, "TEST", ValidCategoryId);

        // Assert
        Assert.Equal("Grocery Stores", rule.Name);
    }

    [Fact]
    public void Create_With_Name_Too_Long_Throws()
    {
        // Arrange
        var name = new string('A', 201);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            CategorizationRule.Create(name, RuleMatchType.Contains, "TEST", ValidCategoryId));
        Assert.Contains("200", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_With_Empty_Pattern_Throws(string? pattern)
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            CategorizationRule.Create("Test", RuleMatchType.Contains, pattern!, ValidCategoryId));
        Assert.Contains("pattern", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_Trims_Pattern()
    {
        // Arrange & Act
        var rule = CategorizationRule.Create("Test", RuleMatchType.Contains, "  WALMART  ", ValidCategoryId);

        // Assert
        Assert.Equal("WALMART", rule.Pattern);
    }

    [Fact]
    public void Create_With_Pattern_Too_Long_Throws()
    {
        // Arrange
        var pattern = new string('A', 501);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            CategorizationRule.Create("Test", RuleMatchType.Contains, pattern, ValidCategoryId));
        Assert.Contains("500", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_With_Empty_CategoryId_Throws()
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            CategorizationRule.Create("Test", RuleMatchType.Contains, "TEST", Guid.Empty));
        Assert.Contains("category", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_With_Invalid_Regex_Pattern_Throws()
    {
        // Arrange
        var invalidRegex = "[invalid(";

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            CategorizationRule.Create("Test", RuleMatchType.Regex, invalidRegex, ValidCategoryId));
        Assert.Contains("regex", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_With_Valid_Regex_Pattern_Succeeds()
    {
        // Arrange
        var validRegex = @"WALMART\s+\d+";

        // Act
        var rule = CategorizationRule.Create("Test", RuleMatchType.Regex, validRegex, ValidCategoryId);

        // Assert
        Assert.Equal(validRegex, rule.Pattern);
    }

    [Fact]
    public void Create_With_Negative_Priority_Throws()
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            CategorizationRule.Create("Test", RuleMatchType.Contains, "TEST", ValidCategoryId, priority: -1));
        Assert.Contains("priority", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_Changes_Properties_And_UpdatedAtUtc()
    {
        // Arrange
        var rule = CategorizationRule.Create("Original", RuleMatchType.Contains, "OLD", ValidCategoryId);
        var originalUpdatedAt = rule.UpdatedAtUtc;
        var newCategoryId = Guid.NewGuid();

        // Act
        rule.Update("New Name", RuleMatchType.StartsWith, "NEW", newCategoryId, caseSensitive: true);

        // Assert
        Assert.Equal("New Name", rule.Name);
        Assert.Equal(RuleMatchType.StartsWith, rule.MatchType);
        Assert.Equal("NEW", rule.Pattern);
        Assert.Equal(newCategoryId, rule.CategoryId);
        Assert.True(rule.CaseSensitive);
        Assert.True(rule.UpdatedAtUtc >= originalUpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_With_Empty_Name_Throws(string? name)
    {
        // Arrange
        var rule = CategorizationRule.Create("Original", RuleMatchType.Contains, "TEST", ValidCategoryId);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            rule.Update(name!, RuleMatchType.Contains, "TEST", ValidCategoryId, false));
        Assert.Contains("name", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_With_Empty_Pattern_Throws(string? pattern)
    {
        // Arrange
        var rule = CategorizationRule.Create("Original", RuleMatchType.Contains, "TEST", ValidCategoryId);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            rule.Update("Test", RuleMatchType.Contains, pattern!, ValidCategoryId, false));
        Assert.Contains("pattern", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Update_Trims_Name_And_Pattern()
    {
        // Arrange
        var rule = CategorizationRule.Create("Original", RuleMatchType.Contains, "TEST", ValidCategoryId);

        // Act
        rule.Update("  New Name  ", RuleMatchType.Contains, "  NEW PATTERN  ", ValidCategoryId, false);

        // Assert
        Assert.Equal("New Name", rule.Name);
        Assert.Equal("NEW PATTERN", rule.Pattern);
    }

    [Fact]
    public void Update_With_Invalid_Regex_Throws()
    {
        // Arrange
        var rule = CategorizationRule.Create("Original", RuleMatchType.Contains, "TEST", ValidCategoryId);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            rule.Update("Test", RuleMatchType.Regex, "[invalid(", ValidCategoryId, false));
        Assert.Contains("regex", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region SetPriority Tests

    [Fact]
    public void SetPriority_Changes_Priority_And_UpdatedAtUtc()
    {
        // Arrange
        var rule = CategorizationRule.Create("Test", RuleMatchType.Contains, "TEST", ValidCategoryId, priority: 100);
        var originalUpdatedAt = rule.UpdatedAtUtc;

        // Act
        rule.SetPriority(10);

        // Assert
        Assert.Equal(10, rule.Priority);
        Assert.True(rule.UpdatedAtUtc >= originalUpdatedAt);
    }

    [Fact]
    public void SetPriority_With_Negative_Value_Throws()
    {
        // Arrange
        var rule = CategorizationRule.Create("Test", RuleMatchType.Contains, "TEST", ValidCategoryId);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => rule.SetPriority(-1));
        Assert.Contains("priority", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SetPriority_With_Zero_Is_Allowed()
    {
        // Arrange
        var rule = CategorizationRule.Create("Test", RuleMatchType.Contains, "TEST", ValidCategoryId);

        // Act
        rule.SetPriority(0);

        // Assert
        Assert.Equal(0, rule.Priority);
    }

    #endregion

    #region Activate/Deactivate Tests

    [Fact]
    public void Deactivate_Sets_IsActive_False()
    {
        // Arrange
        var rule = CategorizationRule.Create("Test", RuleMatchType.Contains, "TEST", ValidCategoryId);
        Assert.True(rule.IsActive);

        // Act
        rule.Deactivate();

        // Assert
        Assert.False(rule.IsActive);
    }

    [Fact]
    public void Activate_Sets_IsActive_True()
    {
        // Arrange
        var rule = CategorizationRule.Create("Test", RuleMatchType.Contains, "TEST", ValidCategoryId);
        rule.Deactivate();
        Assert.False(rule.IsActive);

        // Act
        rule.Activate();

        // Assert
        Assert.True(rule.IsActive);
    }

    [Fact]
    public void Deactivate_Updates_UpdatedAtUtc()
    {
        // Arrange
        var rule = CategorizationRule.Create("Test", RuleMatchType.Contains, "TEST", ValidCategoryId);
        var originalUpdatedAt = rule.UpdatedAtUtc;

        // Act
        rule.Deactivate();

        // Assert
        Assert.True(rule.UpdatedAtUtc >= originalUpdatedAt);
    }

    [Fact]
    public void Activate_Updates_UpdatedAtUtc()
    {
        // Arrange
        var rule = CategorizationRule.Create("Test", RuleMatchType.Contains, "TEST", ValidCategoryId);
        rule.Deactivate();
        var originalUpdatedAt = rule.UpdatedAtUtc;

        // Act
        rule.Activate();

        // Assert
        Assert.True(rule.UpdatedAtUtc >= originalUpdatedAt);
    }

    #endregion

    #region Matches Tests - Exact

    [Fact]
    public void Matches_Exact_Returns_True_When_Description_Equals_Pattern()
    {
        // Arrange
        var rule = CategorizationRule.Create("Test", RuleMatchType.Exact, "WALMART STORE #123", ValidCategoryId);

        // Act & Assert
        Assert.True(rule.Matches("WALMART STORE #123"));
    }

    [Fact]
    public void Matches_Exact_Returns_False_When_Description_Not_Equals_Pattern()
    {
        // Arrange
        var rule = CategorizationRule.Create("Test", RuleMatchType.Exact, "WALMART STORE #123", ValidCategoryId);

        // Act & Assert
        Assert.False(rule.Matches("WALMART STORE #456"));
    }

    [Fact]
    public void Matches_Exact_CaseInsensitive_By_Default()
    {
        // Arrange
        var rule = CategorizationRule.Create("Test", RuleMatchType.Exact, "WALMART", ValidCategoryId);

        // Act & Assert
        Assert.True(rule.Matches("walmart"));
        Assert.True(rule.Matches("Walmart"));
    }

    [Fact]
    public void Matches_Exact_CaseSensitive_Returns_False_When_Case_Differs()
    {
        // Arrange
        var rule = CategorizationRule.Create("Test", RuleMatchType.Exact, "WALMART", ValidCategoryId, caseSensitive: true);

        // Act & Assert
        Assert.False(rule.Matches("walmart"));
        Assert.True(rule.Matches("WALMART"));
    }

    #endregion

    #region Matches Tests - Contains

    [Fact]
    public void Matches_Contains_Returns_True_When_Description_Contains_Pattern()
    {
        // Arrange
        var rule = CategorizationRule.Create("Test", RuleMatchType.Contains, "WALMART", ValidCategoryId);

        // Act & Assert
        Assert.True(rule.Matches("WALMART STORE #123"));
        Assert.True(rule.Matches("CHECK CARD WALMART GROCERY"));
        Assert.True(rule.Matches("AT WALMART TODAY"));
    }

    [Fact]
    public void Matches_Contains_Returns_False_When_Description_Does_Not_Contain_Pattern()
    {
        // Arrange
        var rule = CategorizationRule.Create("Test", RuleMatchType.Contains, "WALMART", ValidCategoryId);

        // Act & Assert
        Assert.False(rule.Matches("TARGET STORE #123"));
    }

    [Fact]
    public void Matches_Contains_CaseInsensitive_By_Default()
    {
        // Arrange
        var rule = CategorizationRule.Create("Test", RuleMatchType.Contains, "WALMART", ValidCategoryId);

        // Act & Assert
        Assert.True(rule.Matches("Purchase at walmart store"));
    }

    [Fact]
    public void Matches_Contains_CaseSensitive_Returns_False_When_Case_Differs()
    {
        // Arrange
        var rule = CategorizationRule.Create("Test", RuleMatchType.Contains, "WALMART", ValidCategoryId, caseSensitive: true);

        // Act & Assert
        Assert.False(rule.Matches("Purchase at walmart store"));
        Assert.True(rule.Matches("Purchase at WALMART store"));
    }

    #endregion

    #region Matches Tests - StartsWith

    [Fact]
    public void Matches_StartsWith_Returns_True_When_Description_Starts_With_Pattern()
    {
        // Arrange
        var rule = CategorizationRule.Create("Test", RuleMatchType.StartsWith, "WALMART", ValidCategoryId);

        // Act & Assert
        Assert.True(rule.Matches("WALMART STORE #123"));
        Assert.True(rule.Matches("WALMART"));
    }

    [Fact]
    public void Matches_StartsWith_Returns_False_When_Description_Does_Not_Start_With_Pattern()
    {
        // Arrange
        var rule = CategorizationRule.Create("Test", RuleMatchType.StartsWith, "WALMART", ValidCategoryId);

        // Act & Assert
        Assert.False(rule.Matches("CHECK CARD WALMART GROCERY"));
        Assert.False(rule.Matches("AT WALMART TODAY"));
    }

    [Fact]
    public void Matches_StartsWith_CaseInsensitive_By_Default()
    {
        // Arrange
        var rule = CategorizationRule.Create("Test", RuleMatchType.StartsWith, "WALMART", ValidCategoryId);

        // Act & Assert
        Assert.True(rule.Matches("walmart store"));
    }

    [Fact]
    public void Matches_StartsWith_CaseSensitive_Returns_False_When_Case_Differs()
    {
        // Arrange
        var rule = CategorizationRule.Create("Test", RuleMatchType.StartsWith, "WALMART", ValidCategoryId, caseSensitive: true);

        // Act & Assert
        Assert.False(rule.Matches("walmart store"));
        Assert.True(rule.Matches("WALMART store"));
    }

    #endregion

    #region Matches Tests - EndsWith

    [Fact]
    public void Matches_EndsWith_Returns_True_When_Description_Ends_With_Pattern()
    {
        // Arrange
        var rule = CategorizationRule.Create("Test", RuleMatchType.EndsWith, "GROCERY", ValidCategoryId);

        // Act & Assert
        Assert.True(rule.Matches("WALMART GROCERY"));
        Assert.True(rule.Matches("GROCERY"));
    }

    [Fact]
    public void Matches_EndsWith_Returns_False_When_Description_Does_Not_End_With_Pattern()
    {
        // Arrange
        var rule = CategorizationRule.Create("Test", RuleMatchType.EndsWith, "GROCERY", ValidCategoryId);

        // Act & Assert
        Assert.False(rule.Matches("GROCERY STORE"));
        Assert.False(rule.Matches("WALMART GROCERY #123"));
    }

    [Fact]
    public void Matches_EndsWith_CaseInsensitive_By_Default()
    {
        // Arrange
        var rule = CategorizationRule.Create("Test", RuleMatchType.EndsWith, "GROCERY", ValidCategoryId);

        // Act & Assert
        Assert.True(rule.Matches("Walmart grocery"));
    }

    [Fact]
    public void Matches_EndsWith_CaseSensitive_Returns_False_When_Case_Differs()
    {
        // Arrange
        var rule = CategorizationRule.Create("Test", RuleMatchType.EndsWith, "GROCERY", ValidCategoryId, caseSensitive: true);

        // Act & Assert
        Assert.False(rule.Matches("Walmart grocery"));
        Assert.True(rule.Matches("Walmart GROCERY"));
    }

    #endregion

    #region Matches Tests - Regex

    [Fact]
    public void Matches_Regex_Returns_True_When_Description_Matches_Pattern()
    {
        // Arrange
        var rule = CategorizationRule.Create("Test", RuleMatchType.Regex, @"WALMART\s+STORE\s+#\d+", ValidCategoryId);

        // Act & Assert
        Assert.True(rule.Matches("WALMART STORE #123"));
        Assert.True(rule.Matches("WALMART  STORE  #456"));
    }

    [Fact]
    public void Matches_Regex_Returns_False_When_Description_Does_Not_Match_Pattern()
    {
        // Arrange
        var rule = CategorizationRule.Create("Test", RuleMatchType.Regex, @"WALMART\s+STORE\s+#\d+", ValidCategoryId);

        // Act & Assert
        Assert.False(rule.Matches("WALMART GROCERY"));
        Assert.False(rule.Matches("TARGET STORE #123"));
    }

    [Fact]
    public void Matches_Regex_CaseInsensitive_By_Default()
    {
        // Arrange
        var rule = CategorizationRule.Create("Test", RuleMatchType.Regex, @"walmart\s+store", ValidCategoryId);

        // Act & Assert
        Assert.True(rule.Matches("WALMART STORE"));
        Assert.True(rule.Matches("Walmart Store"));
    }

    [Fact]
    public void Matches_Regex_CaseSensitive_Returns_False_When_Case_Differs()
    {
        // Arrange
        var rule = CategorizationRule.Create("Test", RuleMatchType.Regex, @"WALMART\s+STORE", ValidCategoryId, caseSensitive: true);

        // Act & Assert
        Assert.False(rule.Matches("walmart store"));
        Assert.True(rule.Matches("WALMART STORE"));
    }

    [Fact]
    public void Matches_Regex_Partial_Match_Returns_True()
    {
        // Arrange - regex should match anywhere in the string by default
        var rule = CategorizationRule.Create("Test", RuleMatchType.Regex, @"\d{4}", ValidCategoryId);

        // Act & Assert
        Assert.True(rule.Matches("CHECK 1234 DEPOSIT"));
    }

    #endregion

    #region Matches Tests - Edge Cases

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Matches_Returns_False_For_Null_Or_Empty_Description(string? description)
    {
        // Arrange
        var rule = CategorizationRule.Create("Test", RuleMatchType.Contains, "TEST", ValidCategoryId);

        // Act & Assert
        Assert.False(rule.Matches(description!));
    }

    [Fact]
    public void Matches_Handles_Whitespace_Only_Description()
    {
        // Arrange
        var rule = CategorizationRule.Create("Test", RuleMatchType.Contains, "TEST", ValidCategoryId);

        // Act & Assert
        Assert.False(rule.Matches("   "));
    }

    [Fact]
    public void Matches_Handles_Special_Characters_In_Pattern()
    {
        // Arrange - Contains should treat pattern literally, not as regex
        var rule = CategorizationRule.Create("Test", RuleMatchType.Contains, "STORE #123", ValidCategoryId);

        // Act & Assert
        Assert.True(rule.Matches("WALMART STORE #123 PURCHASE"));
    }

    [Fact]
    public void Matches_Handles_Special_Regex_Characters_In_Non_Regex_Patterns()
    {
        // Arrange - non-regex patterns with special chars should be treated literally
        var rule = CategorizationRule.Create("Test", RuleMatchType.Contains, "TEST (ABC)", ValidCategoryId);

        // Act & Assert
        Assert.True(rule.Matches("SAMPLE TEST (ABC) DATA"));
        Assert.False(rule.Matches("SAMPLE TEST ABC DATA"));
    }

    #endregion
}
