// <copyright file="DescriptionNormalizerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the DescriptionNormalizer static class.
/// </summary>
public class DescriptionNormalizerTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void Normalize_NullOrWhitespace_ReturnsEmpty(string? input, string expected)
    {
        Assert.Equal(expected, DescriptionNormalizer.Normalize(input));
    }

    [Theory]
    [InlineData("POS PURCHASE NETFLIX.COM", "NETFLIX.COM")]
    [InlineData("POS DEBIT SPOTIFY USA", "SPOTIFY USA")]
    [InlineData("POS Netflix", "NETFLIX")]
    [InlineData("PURCHASE HULU", "HULU")]
    [InlineData("CHECKCARD AMAZON.COM", "AMAZON.COM")]
    [InlineData("DEBIT CARD WALMART", "WALMART")]
    [InlineData("ACH DEBIT GEICO INSURANCE", "GEICO INSURANCE")]
    [InlineData("ACH CREDIT EMPLOYER PAYROLL", "EMPLOYER PAYROLL")]
    [InlineData("VISA DEBIT COSTCO WHOLESALE", "COSTCO WHOLESALE")]
    [InlineData("RECURRING PAYMENT ATT WIRELESS", "ATT WIRELESS")]
    [InlineData("PREAUTHORIZED DUKE ENERGY", "DUKE ENERGY")]
    [InlineData("PRE-AUTHORIZED XFINITY", "XFINITY")]
    public void Normalize_StripsBankPrefixes(string input, string expected)
    {
        Assert.Equal(expected, DescriptionNormalizer.Normalize(input));
    }

    [Theory]
    [InlineData("NETFLIX 12345678", "NETFLIX")]
    [InlineData("SPOTIFY USA 987654321", "SPOTIFY USA")]
    [InlineData("AMAZON MKTPLACE 0011223344", "AMAZON MKTPLACE")]
    public void Normalize_StripsTrailingReferenceNumbers(string input, string expected)
    {
        Assert.Equal(expected, DescriptionNormalizer.Normalize(input));
    }

    [Theory]
    [InlineData("NETFLIX 03/15/2025", "NETFLIX")]
    [InlineData("SPOTIFY 12-01-24", "SPOTIFY")]
    public void Normalize_StripsTrailingDates(string input, string expected)
    {
        Assert.Equal(expected, DescriptionNormalizer.Normalize(input));
    }

    [Theory]
    [InlineData("STORE #1234", "STORE")]
    [InlineData("WALMART SUPERCENTER #5678", "WALMART SUPERCENTER")]
    public void Normalize_StripsTrailingHashNumbers(string input, string expected)
    {
        Assert.Equal(expected, DescriptionNormalizer.Normalize(input));
    }

    [Theory]
    [InlineData("NETFLIX   .COM", "NETFLIX .COM")]
    [InlineData("  NETFLIX  ", "NETFLIX")]
    public void Normalize_CollapsesWhitespace(string input, string expected)
    {
        Assert.Equal(expected, DescriptionNormalizer.Normalize(input));
    }

    [Fact]
    public void Normalize_UppercasesResult()
    {
        Assert.Equal("NETFLIX", DescriptionNormalizer.Normalize("netflix"));
    }

    [Theory]
    [InlineData("POS PURCHASE NETFLIX.COM 12345678 03/15/2025", "NETFLIX.COM")]
    [InlineData("CHECKCARD AMAZON MKTPLACE 9876543 01-15-24", "AMAZON MKTPLACE")]
    public void Normalize_StripsPrefixAndTrailingPatterns(string input, string expected)
    {
        Assert.Equal(expected, DescriptionNormalizer.Normalize(input));
    }

    [Fact]
    public void Normalize_PreservesSimpleDescription()
    {
        Assert.Equal("NETFLIX", DescriptionNormalizer.Normalize("Netflix"));
    }

    [Fact]
    public void Normalize_PreservesDescriptionWithNoNoise()
    {
        Assert.Equal("XFINITY INTERNET", DescriptionNormalizer.Normalize("Xfinity Internet"));
    }
}
