// <copyright file="LearnedMerchantMappingTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the LearnedMerchantMapping entity.
/// </summary>
public class LearnedMerchantMappingTests
{
    private const string ValidOwnerId = "user-123";
    private static readonly Guid ValidCategoryId = Guid.NewGuid();

    #region Create Tests

    [Fact]
    public void Create_With_Valid_Data_Creates_Mapping()
    {
        // Arrange
        var merchantPattern = "netflix";

        // Act
        var mapping = LearnedMerchantMapping.Create(
            merchantPattern,
            ValidCategoryId,
            ValidOwnerId);

        // Assert
        Assert.NotEqual(Guid.Empty, mapping.Id);
        Assert.Equal(merchantPattern.ToUpperInvariant(), mapping.MerchantPattern);
        Assert.Equal(ValidCategoryId, mapping.CategoryId);
        Assert.Equal(ValidOwnerId, mapping.OwnerId);
        Assert.Equal(1, mapping.LearnCount);
        Assert.NotEqual(default, mapping.CreatedAtUtc);
        Assert.NotEqual(default, mapping.UpdatedAtUtc);
    }

    [Fact]
    public void Create_Normalizes_Pattern_To_Upper()
    {
        // Arrange & Act
        var mapping = LearnedMerchantMapping.Create(
            "NeTfLiX",
            ValidCategoryId,
            ValidOwnerId);

        // Assert
        Assert.Equal("NETFLIX", mapping.MerchantPattern);
    }

    [Fact]
    public void Create_Trims_Pattern()
    {
        // Arrange & Act
        var mapping = LearnedMerchantMapping.Create(
            "  spotify  ",
            ValidCategoryId,
            ValidOwnerId);

        // Assert
        Assert.Equal("SPOTIFY", mapping.MerchantPattern);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_With_Empty_Pattern_Throws(string? pattern)
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            LearnedMerchantMapping.Create(
                pattern!,
                ValidCategoryId,
                ValidOwnerId));
        Assert.Contains("pattern", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_With_Pattern_Too_Long_Throws()
    {
        // Arrange
        var pattern = new string('A', LearnedMerchantMapping.MaxPatternLength + 1);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            LearnedMerchantMapping.Create(
                pattern,
                ValidCategoryId,
                ValidOwnerId));
        Assert.Contains("pattern", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_With_Empty_CategoryId_Throws()
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            LearnedMerchantMapping.Create(
                "pattern",
                Guid.Empty,
                ValidOwnerId));
        Assert.Contains("category", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_With_Empty_OwnerId_Throws(string? ownerId)
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            LearnedMerchantMapping.Create(
                "pattern",
                ValidCategoryId,
                ownerId!));
        Assert.Contains("owner", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region IncrementLearnCount Tests

    [Fact]
    public void IncrementLearnCount_Increases_Count()
    {
        // Arrange
        var mapping = LearnedMerchantMapping.Create("netflix", ValidCategoryId, ValidOwnerId);
        var originalCount = mapping.LearnCount;

        // Act
        mapping.IncrementLearnCount();

        // Assert
        Assert.Equal(originalCount + 1, mapping.LearnCount);
    }

    [Fact]
    public void IncrementLearnCount_Updates_Timestamp()
    {
        // Arrange
        var mapping = LearnedMerchantMapping.Create("netflix", ValidCategoryId, ValidOwnerId);
        var originalUpdatedAt = mapping.UpdatedAtUtc;

        // Act
        Thread.Sleep(10); // Ensure time difference
        mapping.IncrementLearnCount();

        // Assert
        Assert.True(mapping.UpdatedAtUtc >= originalUpdatedAt);
    }

    #endregion

    #region UpdateCategory Tests

    [Fact]
    public void UpdateCategory_Changes_CategoryId()
    {
        // Arrange
        var mapping = LearnedMerchantMapping.Create("netflix", ValidCategoryId, ValidOwnerId);
        var newCategoryId = Guid.NewGuid();

        // Act
        mapping.UpdateCategory(newCategoryId);

        // Assert
        Assert.Equal(newCategoryId, mapping.CategoryId);
        Assert.Equal(1, mapping.LearnCount); // Reset to 1
    }

    [Fact]
    public void UpdateCategory_With_Empty_CategoryId_Throws()
    {
        // Arrange
        var mapping = LearnedMerchantMapping.Create("netflix", ValidCategoryId, ValidOwnerId);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => mapping.UpdateCategory(Guid.Empty));
        Assert.Contains("category", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
