// <copyright file="DismissedSuggestionPatternTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the DismissedSuggestionPattern entity.
/// </summary>
public class DismissedSuggestionPatternTests
{
    private const string ValidOwnerId = "user-123";

    #region Create Tests

    [Fact]
    public void Create_With_Valid_Data_Creates_Pattern()
    {
        // Arrange
        var pattern = "netflix";

        // Act
        var dismissed = DismissedSuggestionPattern.Create(pattern, ValidOwnerId);

        // Assert
        Assert.NotEqual(Guid.Empty, dismissed.Id);
        Assert.Equal(pattern.ToUpperInvariant(), dismissed.Pattern);
        Assert.Equal(ValidOwnerId, dismissed.OwnerId);
        Assert.NotEqual(default, dismissed.DismissedAtUtc);
    }

    [Fact]
    public void Create_Normalizes_Pattern_To_Upper()
    {
        // Arrange & Act
        var dismissed = DismissedSuggestionPattern.Create("NeTfLiX", ValidOwnerId);

        // Assert
        Assert.Equal("NETFLIX", dismissed.Pattern);
    }

    [Fact]
    public void Create_Trims_Pattern()
    {
        // Arrange & Act
        var dismissed = DismissedSuggestionPattern.Create("  spotify  ", ValidOwnerId);

        // Assert
        Assert.Equal("SPOTIFY", dismissed.Pattern);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_With_Empty_Pattern_Throws(string? pattern)
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            DismissedSuggestionPattern.Create(pattern!, ValidOwnerId));
        Assert.Contains("pattern", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_With_Pattern_Too_Long_Throws()
    {
        // Arrange
        var pattern = new string('A', DismissedSuggestionPattern.MaxPatternLength + 1);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            DismissedSuggestionPattern.Create(pattern, ValidOwnerId));
        Assert.Contains("pattern", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_With_Empty_OwnerId_Throws(string? ownerId)
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            DismissedSuggestionPattern.Create("pattern", ownerId!));
        Assert.Contains("owner", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
