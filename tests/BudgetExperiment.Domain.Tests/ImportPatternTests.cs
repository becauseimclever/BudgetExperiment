// <copyright file="ImportPatternTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the ImportPattern value object.
/// </summary>
public class ImportPatternTests
{
    [Fact]
    public void Create_With_Valid_Pattern_Creates_ImportPattern()
    {
        // Arrange
        var pattern = "ACME CORP PAYROLL";

        // Act
        var importPattern = ImportPattern.Create(pattern);

        // Assert
        Assert.Equal("ACME CORP PAYROLL", importPattern.Pattern);
    }

    [Fact]
    public void Create_Trims_Whitespace()
    {
        // Arrange
        var pattern = "  ACME CORP  ";

        // Act
        var importPattern = ImportPattern.Create(pattern);

        // Assert
        Assert.Equal("ACME CORP", importPattern.Pattern);
    }

    [Fact]
    public void Create_Normalizes_To_Uppercase()
    {
        // Arrange
        var pattern = "acme corp";

        // Act
        var importPattern = ImportPattern.Create(pattern);

        // Assert
        Assert.Equal("ACME CORP", importPattern.Pattern);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_With_Empty_Pattern_Throws(string? pattern)
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => ImportPattern.Create(pattern!));
        Assert.Contains("pattern", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Matches_Exact_Text_Returns_True()
    {
        // Arrange
        var importPattern = ImportPattern.Create("ACME CORP");

        // Act
        var result = importPattern.Matches("ACME CORP");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Matches_Is_Case_Insensitive()
    {
        // Arrange
        var importPattern = ImportPattern.Create("ACME CORP");

        // Act
        var result = importPattern.Matches("acme corp");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Matches_Wildcard_Prefix_Returns_True()
    {
        // Arrange - Pattern with wildcard prefix
        var importPattern = ImportPattern.Create("*ACME CORP");

        // Act
        var result = importPattern.Matches("DIRECT DEP ACME CORP");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Matches_Wildcard_Suffix_Returns_True()
    {
        // Arrange - Pattern with wildcard suffix
        var importPattern = ImportPattern.Create("ACME CORP*");

        // Act
        var result = importPattern.Matches("ACME CORP PAYROLL 01/15");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Matches_Wildcard_Both_Returns_True()
    {
        // Arrange - Pattern with wildcards on both ends
        var importPattern = ImportPattern.Create("*ACME CORP*");

        // Act
        var result = importPattern.Matches("DIRECT DEP ACME CORP PAYROLL 01/15");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Matches_Wildcard_Prefix_No_Match_Returns_False()
    {
        // Arrange
        var importPattern = ImportPattern.Create("*ACME CORP");

        // Act
        var result = importPattern.Matches("DIRECT DEP ACME");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Matches_Wildcard_Suffix_No_Match_Returns_False()
    {
        // Arrange
        var importPattern = ImportPattern.Create("ACME CORP*");

        // Act
        var result = importPattern.Matches("OTHER COMPANY PAYROLL");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Matches_Exact_Pattern_No_Match_Returns_False()
    {
        // Arrange
        var importPattern = ImportPattern.Create("ACME CORP");

        // Act
        var result = importPattern.Matches("ACME CORPORATION");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Matches_With_Null_Description_Returns_False()
    {
        // Arrange
        var importPattern = ImportPattern.Create("ACME CORP");

        // Act
        var result = importPattern.Matches(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Matches_With_Empty_Description_Returns_False()
    {
        // Arrange
        var importPattern = ImportPattern.Create("ACME CORP");

        // Act
        var result = importPattern.Matches(string.Empty);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Two_ImportPatterns_With_Same_Pattern_Are_Equal()
    {
        // Arrange
        var pattern1 = ImportPattern.Create("ACME CORP");
        var pattern2 = ImportPattern.Create("acme corp");

        // Act & Assert
        Assert.Equal(pattern1, pattern2);
    }

    [Fact]
    public void ToString_Returns_Pattern()
    {
        // Arrange
        var importPattern = ImportPattern.Create("*ACME CORP*");

        // Act
        var result = importPattern.ToString();

        // Assert
        Assert.Equal("*ACME CORP*", result);
    }
}
