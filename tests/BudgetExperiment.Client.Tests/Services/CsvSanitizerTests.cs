// <copyright file="CsvSanitizerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Services;

namespace BudgetExperiment.Client.Tests.Services;

/// <summary>
/// Unit tests for <see cref="CsvSanitizer"/> CSV injection prevention.
/// </summary>
public class CsvSanitizerTests
{
    [Fact]
    public void SanitizeForDisplay_Formula_Equals_Prefixed()
    {
        // Arrange & Act
        var result = CsvSanitizer.SanitizeForDisplay("=SUM(A1:A2)");

        // Assert
        Assert.Equal("'=SUM(A1:A2)", result);
    }

    [Fact]
    public void SanitizeForDisplay_At_Sign_Prefixed()
    {
        // Arrange & Act
        var result = CsvSanitizer.SanitizeForDisplay("@SUM(A1)");

        // Assert
        Assert.Equal("'@SUM(A1)", result);
    }

    [Fact]
    public void SanitizeForDisplay_Plus_Sign_Prefixed()
    {
        // Arrange & Act
        var result = CsvSanitizer.SanitizeForDisplay("+100");

        // Assert
        Assert.Equal("'+100", result);
    }

    [Fact]
    public void SanitizeForDisplay_Minus_Sign_Prefixed()
    {
        // Arrange & Act
        var result = CsvSanitizer.SanitizeForDisplay("-45.99");

        // Assert
        Assert.Equal("'-45.99", result);
    }

    [Fact]
    public void SanitizeForDisplay_Tab_Prefixed()
    {
        // Arrange & Act
        var result = CsvSanitizer.SanitizeForDisplay("\tsome data");

        // Assert
        Assert.Equal("'\tsome data", result);
    }

    [Fact]
    public void SanitizeForDisplay_CarriageReturn_Prefixed()
    {
        // Arrange & Act
        var result = CsvSanitizer.SanitizeForDisplay("\rsome data");

        // Assert
        Assert.Equal("'\rsome data", result);
    }

    [Fact]
    public void SanitizeForDisplay_Null_Returns_Null()
    {
        // Arrange & Act
        var result = CsvSanitizer.SanitizeForDisplay(null!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void SanitizeForDisplay_Empty_Returns_Empty()
    {
        // Arrange & Act
        var result = CsvSanitizer.SanitizeForDisplay(string.Empty);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData("Hello World")]
    [InlineData("WALMART")]
    [InlineData("01/15/2026")]
    [InlineData("$45.99")]
    [InlineData("(45.99)")]
    [InlineData("Normal text")]
    [InlineData("123.45")]
    public void SanitizeForDisplay_NonTrigger_Returns_Unchanged(string value)
    {
        // Arrange & Act
        var result = CsvSanitizer.SanitizeForDisplay(value);

        // Assert
        Assert.Equal(value, result);
    }

    [Fact]
    public void SanitizeForDisplay_Equals_Cmd_Prefixed()
    {
        // Arrange — more dangerous payload
        var result = CsvSanitizer.SanitizeForDisplay("=cmd|'/C calc'!A0");

        // Assert
        Assert.Equal("'=cmd|'/C calc'!A0", result);
    }

    [Fact]
    public void SanitizeForDisplay_At_Formula_Excel_Prefixed()
    {
        // Arrange & Act
        var result = CsvSanitizer.SanitizeForDisplay("@SUM(A1:A10)");

        // Assert
        Assert.Equal("'@SUM(A1:A10)", result);
    }

    [Fact]
    public void SanitizeForDisplay_NegativeAmount_With_Decimal_Prefixed()
    {
        // Arrange & Act — negative amounts ARE sanitized for display safety
        var result = CsvSanitizer.SanitizeForDisplay("-42.50");

        // Assert
        Assert.Equal("'-42.50", result);
    }

    [Fact]
    public void UnsanitizeForParsing_Removes_Prefix_From_Sanitized_Formula()
    {
        // Arrange
        var sanitized = CsvSanitizer.SanitizeForDisplay("=SUM(A1)");

        // Act
        var result = CsvSanitizer.UnsanitizeForParsing(sanitized);

        // Assert
        Assert.Equal("=SUM(A1)", result);
    }

    [Fact]
    public void UnsanitizeForParsing_Removes_Prefix_From_Sanitized_NegativeAmount()
    {
        // Arrange
        var sanitized = CsvSanitizer.SanitizeForDisplay("-45.99");

        // Act
        var result = CsvSanitizer.UnsanitizeForParsing(sanitized);

        // Assert
        Assert.Equal("-45.99", result);
    }

    [Fact]
    public void UnsanitizeForParsing_Removes_Prefix_From_Sanitized_PlusAmount()
    {
        // Arrange
        var sanitized = CsvSanitizer.SanitizeForDisplay("+29.99");

        // Act
        var result = CsvSanitizer.UnsanitizeForParsing(sanitized);

        // Assert
        Assert.Equal("+29.99", result);
    }

    [Fact]
    public void UnsanitizeForParsing_NonSanitized_Returns_Unchanged()
    {
        // Arrange & Act
        var result = CsvSanitizer.UnsanitizeForParsing("WALMART");

        // Assert
        Assert.Equal("WALMART", result);
    }

    [Fact]
    public void UnsanitizeForParsing_Null_Returns_Null()
    {
        // Arrange & Act
        var result = CsvSanitizer.UnsanitizeForParsing(null!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void UnsanitizeForParsing_Empty_Returns_Empty()
    {
        // Arrange & Act
        var result = CsvSanitizer.UnsanitizeForParsing(string.Empty);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void UnsanitizeForParsing_Apostrophe_Not_Followed_By_Trigger_Returns_Unchanged()
    {
        // Arrange — value genuinely starts with apostrophe but not a trigger char
        var result = CsvSanitizer.UnsanitizeForParsing("'hello");

        // Assert
        Assert.Equal("'hello", result);
    }

    [Theory]
    [InlineData("=", "'=")]
    [InlineData("@", "'@")]
    [InlineData("+", "'+")]
    [InlineData("-", "'-")]
    public void SanitizeForDisplay_SingleTriggerChar_Prefixed(string input, string expected)
    {
        // Arrange & Act
        var result = CsvSanitizer.SanitizeForDisplay(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void SanitizeForDisplay_Roundtrip_Preserves_Original_Value()
    {
        // Arrange
        var original = "-45.99";

        // Act — sanitize then unsanitize
        var sanitized = CsvSanitizer.SanitizeForDisplay(original);
        var restored = CsvSanitizer.UnsanitizeForParsing(sanitized);

        // Assert
        Assert.Equal(original, restored);
    }

    [Fact]
    public async Task ParseAsync_Sanitizes_Formula_Injection_Cells()
    {
        // Arrange — CSV with formula-trigger cells
        var csv = "Date,Description,Amount\n01/15/2026,=SUM(A1:A2),45.99\n01/16/2026,@VLOOKUP,29.99";
        var parser = new CsvParserService();
        using var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv));

        // Act
        var result = await parser.ParseAsync(stream, "test.csv");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("'=SUM(A1:A2)", result.Rows[0][1]);
        Assert.Equal("'@VLOOKUP", result.Rows[1][1]);
    }

    [Fact]
    public async Task ParseAsync_Sanitizes_NegativeAmounts_In_Parsed_Output()
    {
        // Arrange — negative amounts start with '-' trigger
        var csv = "Date,Description,Amount\n01/15/2026,WALMART,-45.99\n01/16/2026,Refund,+29.99";
        var parser = new CsvParserService();
        using var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv));

        // Act
        var result = await parser.ParseAsync(stream, "test.csv");

        // Assert — amounts are sanitized for display safety
        Assert.True(result.Success);
        Assert.Equal("'-45.99", result.Rows[0][2]);
        Assert.Equal("'+29.99", result.Rows[1][2]);
    }

    [Fact]
    public async Task ParseAsync_NonTrigger_Values_Not_Sanitized()
    {
        // Arrange — normal values should pass through unchanged
        var csv = "Date,Description,Amount\n01/15/2026,WALMART,45.99";
        var parser = new CsvParserService();
        using var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv));

        // Act
        var result = await parser.ParseAsync(stream, "test.csv");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("01/15/2026", result.Rows[0][0]);
        Assert.Equal("WALMART", result.Rows[0][1]);
        Assert.Equal("45.99", result.Rows[0][2]);
    }
}
