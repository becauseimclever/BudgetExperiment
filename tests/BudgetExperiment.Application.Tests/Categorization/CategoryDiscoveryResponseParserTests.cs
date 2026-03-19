// <copyright file="CategoryDiscoveryResponseParserTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Tests.Categorization;

/// <summary>
/// Unit tests for <see cref="CategoryDiscoveryResponseParser"/>.
/// </summary>
public class CategoryDiscoveryResponseParserTests
{
    [Fact]
    public void Parse_ValidJson_ReturnsDiscoveredCategories()
    {
        // Arrange
        var json = """
            [
              {
                "categoryName": "Home Improvement",
                "icon": "🔨",
                "color": "#8B4513",
                "confidence": 0.85,
                "reasoning": "Hardware stores suggest home improvement spending.",
                "matchedDescriptions": ["HOME DEPOT", "LOWES"]
              }
            ]
            """;

        // Act
        var result = CategoryDiscoveryResponseParser.Parse(json);

        // Assert
        Assert.Single(result);
        Assert.Equal("Home Improvement", result[0].CategoryName);
        Assert.Equal("🔨", result[0].Icon);
        Assert.Equal("#8B4513", result[0].Color);
        Assert.Equal(0.85m, result[0].Confidence);
        Assert.Equal("Hardware stores suggest home improvement spending.", result[0].Reasoning);
        Assert.Equal(new[] { "HOME DEPOT", "LOWES" }, result[0].MatchedDescriptions);
    }

    [Fact]
    public void Parse_MultipleCategories_ReturnsAll()
    {
        // Arrange
        var json = """
            [
              {
                "categoryName": "Home Improvement",
                "icon": "🔨",
                "color": "#8B4513",
                "confidence": 0.85,
                "reasoning": "Hardware stores cluster.",
                "matchedDescriptions": ["HOME DEPOT", "LOWES"]
              },
              {
                "categoryName": "Pet Care",
                "icon": "🐾",
                "color": "#4CAF50",
                "confidence": 0.80,
                "reasoning": "Pet stores and vet visits.",
                "matchedDescriptions": ["PETCO", "PETSMART"]
              }
            ]
            """;

        // Act
        var result = CategoryDiscoveryResponseParser.Parse(json);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Home Improvement", result[0].CategoryName);
        Assert.Equal("Pet Care", result[1].CategoryName);
    }

    [Fact]
    public void Parse_JsonInMarkdownCodeBlock_ExtractsCorrectly()
    {
        // Arrange
        var response = """
            Here are my suggestions:

            ```json
            [
              {
                "categoryName": "Home Improvement",
                "icon": "🔨",
                "color": "#8B4513",
                "confidence": 0.85,
                "reasoning": "Hardware stores cluster.",
                "matchedDescriptions": ["HOME DEPOT", "LOWES"]
              }
            ]
            ```

            Hope this helps!
            """;

        // Act
        var result = CategoryDiscoveryResponseParser.Parse(response);

        // Assert
        Assert.Single(result);
        Assert.Equal("Home Improvement", result[0].CategoryName);
    }

    [Fact]
    public void Parse_JsonInGenericCodeBlock_ExtractsCorrectly()
    {
        // Arrange
        var response = """
            ```
            [
              {
                "categoryName": "Pet Care",
                "icon": "🐾",
                "color": "#4CAF50",
                "confidence": 0.80,
                "reasoning": "Pet stores.",
                "matchedDescriptions": ["PETCO"]
              }
            ]
            ```
            """;

        // Act
        var result = CategoryDiscoveryResponseParser.Parse(response);

        // Assert
        Assert.Single(result);
        Assert.Equal("Pet Care", result[0].CategoryName);
    }

    [Fact]
    public void Parse_EmptyString_ReturnsEmpty()
    {
        // Act
        var result = CategoryDiscoveryResponseParser.Parse(string.Empty);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Parse_NullContent_ReturnsEmpty()
    {
        // Act
        var result = CategoryDiscoveryResponseParser.Parse(null!);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Parse_InvalidJson_ReturnsEmpty()
    {
        // Act
        var result = CategoryDiscoveryResponseParser.Parse("not valid json at all");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Parse_EmptyArray_ReturnsEmpty()
    {
        // Act
        var result = CategoryDiscoveryResponseParser.Parse("[]");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Parse_FiltersOut_ItemsWithMissingCategoryName()
    {
        // Arrange
        var json = """
            [
              {
                "categoryName": "",
                "confidence": 0.85,
                "reasoning": "Some reason.",
                "matchedDescriptions": ["HOME DEPOT"]
              },
              {
                "categoryName": "Valid Category",
                "confidence": 0.80,
                "reasoning": "Good reason.",
                "matchedDescriptions": ["PETCO"]
              }
            ]
            """;

        // Act
        var result = CategoryDiscoveryResponseParser.Parse(json);

        // Assert
        Assert.Single(result);
        Assert.Equal("Valid Category", result[0].CategoryName);
    }

    [Fact]
    public void Parse_FiltersOut_ItemsWithNoMatchedDescriptions()
    {
        // Arrange
        var json = """
            [
              {
                "categoryName": "Empty Category",
                "confidence": 0.85,
                "reasoning": "Some reason.",
                "matchedDescriptions": []
              }
            ]
            """;

        // Act
        var result = CategoryDiscoveryResponseParser.Parse(json);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Parse_FiltersOut_ItemsWithInvalidConfidence()
    {
        // Arrange
        var json = """
            [
              {
                "categoryName": "Too Confident",
                "confidence": 1.5,
                "reasoning": "Some reason.",
                "matchedDescriptions": ["X"]
              }
            ]
            """;

        // Act
        var result = CategoryDiscoveryResponseParser.Parse(json);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Parse_FiltersOut_ItemsWithMissingReasoning()
    {
        // Arrange
        var json = """
            [
              {
                "categoryName": "No Reason",
                "confidence": 0.85,
                "reasoning": "",
                "matchedDescriptions": ["X"]
              }
            ]
            """;

        // Act
        var result = CategoryDiscoveryResponseParser.Parse(json);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Parse_TrimsWhitespace_FromCategoryNameAndReasoning()
    {
        // Arrange
        var json = """
            [
              {
                "categoryName": "  Home Improvement  ",
                "icon": "  🔨  ",
                "color": "  #8B4513  ",
                "confidence": 0.85,
                "reasoning": "  Hardware stores cluster.  ",
                "matchedDescriptions": ["HOME DEPOT"]
              }
            ]
            """;

        // Act
        var result = CategoryDiscoveryResponseParser.Parse(json);

        // Assert
        Assert.Single(result);
        Assert.Equal("Home Improvement", result[0].CategoryName);
        Assert.Equal("🔨", result[0].Icon);
        Assert.Equal("#8B4513", result[0].Color);
        Assert.Equal("Hardware stores cluster.", result[0].Reasoning);
    }

    [Fact]
    public void Parse_HandlesNullIconAndColor()
    {
        // Arrange
        var json = """
            [
              {
                "categoryName": "Home Improvement",
                "confidence": 0.85,
                "reasoning": "Hardware stores.",
                "matchedDescriptions": ["HOME DEPOT"]
              }
            ]
            """;

        // Act
        var result = CategoryDiscoveryResponseParser.Parse(json);

        // Assert
        Assert.Single(result);
        Assert.Null(result[0].Icon);
        Assert.Null(result[0].Color);
    }

    [Fact]
    public void Parse_JsonWithSurroundingText_ExtractsArray()
    {
        // Arrange
        var response = """
            Based on my analysis, here are the categories:
            [
              {
                "categoryName": "Home Improvement",
                "confidence": 0.85,
                "reasoning": "Hardware stores.",
                "matchedDescriptions": ["HOME DEPOT"]
              }
            ]
            That is my suggestion.
            """;

        // Act
        var result = CategoryDiscoveryResponseParser.Parse(response);

        // Assert
        Assert.Single(result);
        Assert.Equal("Home Improvement", result[0].CategoryName);
    }

    [Fact]
    public void Parse_CaseInsensitivePropertyNames()
    {
        // Arrange
        var json = """
            [
              {
                "CategoryName": "Home Improvement",
                "Confidence": 0.85,
                "Reasoning": "Hardware stores.",
                "MatchedDescriptions": ["HOME DEPOT"]
              }
            ]
            """;

        // Act
        var result = CategoryDiscoveryResponseParser.Parse(json);

        // Assert
        Assert.Single(result);
        Assert.Equal("Home Improvement", result[0].CategoryName);
    }
}
