// <copyright file="LocationParserServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Location;

namespace BudgetExperiment.Application.Tests.Location;

/// <summary>
/// Unit tests for <see cref="LocationParserService"/>.
/// </summary>
public sealed class LocationParserServiceTests
{
    private readonly LocationParserService sut = new();

    [Theory]
    [InlineData("SEATTLE WA", "SEATTLE", "WA")]
    [InlineData("PORTLAND OR", "PORTLAND", "OR")]
    [InlineData("AUSTIN TX", "AUSTIN", "TX")]
    public void Parse_CityState_ReturnsLocation(string input, string expectedCity, string expectedState)
    {
        // Act
        var result = sut.ParseFromDescription(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedCity, result.City, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(expectedState, result.StateOrRegion, StringComparer.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Seattle, WA", "Seattle", "WA")]
    [InlineData("Portland, OR", "Portland", "OR")]
    public void Parse_CityCommaState_ReturnsLocation(string input, string expectedCity, string expectedState)
    {
        // Act
        var result = sut.ParseFromDescription(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedCity, result.City, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(expectedState, result.StateOrRegion, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_CityStateZip_ReturnsLocationWithPostal()
    {
        // Act
        var result = sut.ParseFromDescription("SEATTLE WA 98101");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SEATTLE", result.City, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("WA", result.StateOrRegion, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("98101", result.PostalCode);
    }

    [Theory]
    [InlineData("AMAZON.COM SEATTLE WA 12345", "SEATTLE", "WA", "12345")]
    [InlineData("CULVERS OF ANYTOWN 10/05 PURCHASE ANYTOWN MO", "ANYTOWN", "MO", null)]
    [InlineData("CHEWY.COM 10/07 PURCHASE 800-672-4399 FL", null, null, null)] // FL alone has no city — should not match city
    public void Parse_EmbeddedInDescription_ExtractsLocation(
        string input, string? expectedCity, string? expectedState, string? expectedPostal)
    {
        // Act
        var result = sut.ParseFromDescription(input);

        // Assert
        if (expectedCity is null)
        {
            // For state-only patterns at the end, we may or may not parse
            // The parser should require a city before the state to avoid false positives
            Assert.Null(result);
        }
        else
        {
            Assert.NotNull(result);
            Assert.Equal(expectedCity, result.City, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(expectedState, result!.StateOrRegion, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(expectedPostal, result.PostalCode);
        }
    }

    [Theory]
    [InlineData("NEW YORK NY", "NEW YORK", "NY")]
    [InlineData("SAN FRANCISCO CA", "SAN FRANCISCO", "CA")]
    [InlineData("LOS ANGELES CA", "LOS ANGELES", "CA")]
    [InlineData("SALT LAKE CITY UT", "SALT LAKE CITY", "UT")]
    public void Parse_MultiWordCity_HandlesCorrectly(string input, string expectedCity, string expectedState)
    {
        // Act
        var result = sut.ParseFromDescription(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedCity, result.City, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(expectedState, result.StateOrRegion, StringComparer.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("ONLINE PURCHASE")]
    [InlineData("PAYROLL DEPOSIT")]
    [InlineData("Zelle payment from John Smith")]
    [InlineData("ATM WITHDRAWAL")]
    [InlineData("")]
    public void Parse_NoLocation_ReturnsNull(string input)
    {
        // Act
        var result = sut.ParseFromDescription(input);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("WA")]
    [InlineData("TX")]
    public void Parse_StateOnly_ReturnsNull(string input)
    {
        // Act
        var result = sut.ParseFromDescription(input);

        // Assert — a bare state abbreviation without a city should not match
        Assert.Null(result);
    }

    [Fact]
    public void Parse_InvalidState_ReturnsNull()
    {
        // Act
        var result = sut.ParseFromDescription("SEATTLE ZZ");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Parse_SetsSourceToParsed()
    {
        // Act
        var result = sut.ParseFromDescription("SEATTLE WA");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(LocationSource.Parsed, result.Source);
    }

    [Theory]
    [InlineData("SEATTLE WA", "US")]
    [InlineData("PORTLAND OR", "US")]
    [InlineData("TORONTO ON", "CA")]
    public void Parse_SetsCorrectCountryCode(string input, string expectedCountry)
    {
        // Act
        var result = sut.ParseFromDescription(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedCountry, result.Country);
    }

    [Fact]
    public void ParseBatch_ReturnsResultsForEach()
    {
        // Arrange
        var descriptions = new[] { "SEATTLE WA", "ONLINE PURCHASE", "AUSTIN TX" };

        // Act
        var results = sut.ParseBatch(descriptions);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal("SEATTLE WA", results[0].OriginalText);
        Assert.NotNull(results[0].Location);
        Assert.Equal("ONLINE PURCHASE", results[1].OriginalText);
        Assert.Null(results[1].Location);
        Assert.Equal("AUSTIN TX", results[2].OriginalText);
        Assert.NotNull(results[2].Location);
    }

    [Fact]
    public void ParseBatch_ReportsConfidenceScores()
    {
        // Arrange
        var descriptions = new[] { "SEATTLE WA 98101", "ONLINE PURCHASE" };

        // Act
        var results = sut.ParseBatch(descriptions);

        // Assert — matched results should have non-zero confidence
        Assert.True(results[0].Confidence > 0m);
        Assert.Equal(0m, results[1].Confidence);
    }

    [Theory]
    [InlineData("TORONTO ON", "TORONTO", "ON", "CA")]
    [InlineData("VANCOUVER BC", "VANCOUVER", "BC", "CA")]
    [InlineData("MONTREAL QC", "MONTREAL", "QC", "CA")]
    public void Parse_CanadianProvince_ReturnsLocation(
        string input, string expectedCity, string expectedProvince, string expectedCountry)
    {
        // Act
        var result = sut.ParseFromDescription(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedCity, result.City, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(expectedProvince, result.StateOrRegion, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(expectedCountry, result.Country);
    }

    [Theory]
    [InlineData(
        "CONSUMER CELLULAR INC 09/30 PURCHASE XXX-XX91237 OR",
        null,
        null,
        null,
        "State-only at end with no city — should not match")]
    [InlineData(
        "AMAZON MKTPL*AB3CD5EF0 10/01 PURCHASE Amzn.com/bill WA",
        null,
        null,
        null,
        "WA after URL-like text — should not match")]
    [InlineData(
        "CULVERS OF ANYTOWN 10/05 PURCHASE ANYTOWN MO",
        "ANYTOWN",
        "MO",
        "US",
        "City + state near end of BoA description")]
    [InlineData(
        "VENMO* John 10/04 PMNT SENT Visa Direct NY",
        null,
        null,
        null,
        "NY after 'Visa Direct' — no city before state")]
    [InlineData(
        "Debit Card Purchase - RESTAURANT A CITY ST",
        null,
        null,
        null,
        "Placeholder 'CITY ST' — ST is not a US state")]
    [InlineData(
        "Debit Card Purchase - AMAZON MKTPL AMZN COM BIL WA",
        null,
        null,
        null,
        "WA after abbreviated merchant name — no clear city")]
    public void Parse_RealWorldSamples(
        string input,
        string? expectedCity,
        string? expectedState,
        string? expectedCountry,
        string description)
    {
        _ = description; // Used for test readability only

        // Act
        var result = sut.ParseFromDescription(input);

        // Assert
        if (expectedCity is null)
        {
            Assert.Null(result);
        }
        else
        {
            Assert.NotNull(result);
            Assert.Equal(expectedCity, result.City, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(expectedState, result!.StateOrRegion, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(expectedCountry, result.Country);
        }
    }

    [Fact]
    public void Parse_CityCommaStateZip_ReturnsLocationWithPostal()
    {
        // Act
        var result = sut.ParseFromDescription("Seattle, WA 98101");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Seattle", result.City, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("WA", result.StateOrRegion, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("98101", result.PostalCode);
    }

    [Fact]
    public void Parse_NullDescription_ReturnsNull()
    {
        // Act
        var result = sut.ParseFromDescription(null!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseBatch_EmptyInput_ReturnsEmptyList()
    {
        // Act
        var results = sut.ParseBatch(Array.Empty<string>());

        // Assert
        Assert.Empty(results);
    }

    [Theory]
    [InlineData("QT 4150 INSIDE ANYTOWN TX Date 11/14/25", "ANYTOWN", "TX", "US")]
    [InlineData("ROSAS CAFE &amp; TORTILLA F ANYTOWN TX Date", "ANYTOWN", "TX", "US")]
    [InlineData("TST* RESTAURANT NAME D ANYTOWN TX Date", "ANYTOWN", "TX", "US")]
    public void Parse_UhcuDescriptions_ExtractsLocation(
        string input, string expectedCity, string expectedState, string expectedCountry)
    {
        // Act
        var result = sut.ParseFromDescription(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedCity, result.City, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(expectedState, result.StateOrRegion, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(expectedCountry, result.Country);
    }

    [Fact]
    public void ParseBatch_SetsMatchedPatternOnSuccesses()
    {
        // Arrange
        var descriptions = new[] { "SEATTLE WA", "RANDOM TEXT" };

        // Act
        var results = sut.ParseBatch(descriptions);

        // Assert
        Assert.NotNull(results[0].MatchedPattern);
        Assert.Null(results[1].MatchedPattern);
    }
}
