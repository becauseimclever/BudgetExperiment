// <copyright file="CategoryDiscoveryPromptBuilderTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Tests.Categorization;

/// <summary>
/// Unit tests for <see cref="CategoryDiscoveryPromptBuilder"/>.
/// </summary>
public class CategoryDiscoveryPromptBuilderTests
{
    [Fact]
    public void Build_Returns_Prompt_With_CategoryDiscovery_SystemPrompt()
    {
        // Arrange
        var groups = new List<DescriptionGroup>
        {
            new("HOME DEPOT", 5, 25m, 350m),
        };

        // Act
        var prompt = CategoryDiscoveryPromptBuilder.Build(groups, ["Groceries"]);

        // Assert
        Assert.Equal(AiPrompts.CategoryDiscoverySystemPrompt, prompt.SystemPrompt);
    }

    [Fact]
    public void Build_Includes_ExistingCategories_In_UserPrompt()
    {
        // Arrange
        var groups = new List<DescriptionGroup>
        {
            new("HOME DEPOT", 5, 25m, 350m),
        };
        var existingCategories = new List<string> { "Groceries", "Dining", "Utilities" };

        // Act
        var prompt = CategoryDiscoveryPromptBuilder.Build(groups, existingCategories);

        // Assert
        Assert.Contains("- Groceries", prompt.UserPrompt);
        Assert.Contains("- Dining", prompt.UserPrompt);
        Assert.Contains("- Utilities", prompt.UserPrompt);
    }

    [Fact]
    public void Build_Includes_Descriptions_In_UserPrompt()
    {
        // Arrange
        var groups = new List<DescriptionGroup>
        {
            new("HOME DEPOT", 5, 25m, 350m),
            new("LOWES", 3, 15m, 200m),
        };

        // Act
        var prompt = CategoryDiscoveryPromptBuilder.Build(groups, ["Groceries"]);

        // Assert
        Assert.Contains("HOME DEPOT", prompt.UserPrompt);
        Assert.Contains("LOWES", prompt.UserPrompt);
        Assert.Contains("5 transactions", prompt.UserPrompt);
        Assert.Contains("3 transactions", prompt.UserPrompt);
    }

    [Fact]
    public void Build_Includes_DismissedCategories_When_Provided()
    {
        // Arrange
        var groups = new List<DescriptionGroup>
        {
            new("HOME DEPOT", 5, 25m, 350m),
        };
        var dismissed = new List<string> { "Home Improvement", "Hardware" };

        // Act
        var prompt = CategoryDiscoveryPromptBuilder.Build(groups, ["Groceries"], dismissed);

        // Assert
        Assert.Contains("PREVIOUSLY DISMISSED", prompt.UserPrompt);
        Assert.Contains("- Home Improvement", prompt.UserPrompt);
        Assert.Contains("- Hardware", prompt.UserPrompt);
    }

    [Fact]
    public void Build_Omits_DismissedSection_When_Null()
    {
        // Arrange
        var groups = new List<DescriptionGroup>
        {
            new("HOME DEPOT", 5, 25m, 350m),
        };

        // Act
        var prompt = CategoryDiscoveryPromptBuilder.Build(groups, ["Groceries"], null);

        // Assert
        Assert.DoesNotContain("PREVIOUSLY DISMISSED", prompt.UserPrompt);
    }

    [Fact]
    public void Build_Omits_DismissedSection_When_Empty()
    {
        // Arrange
        var groups = new List<DescriptionGroup>
        {
            new("HOME DEPOT", 5, 25m, 350m),
        };

        // Act
        var prompt = CategoryDiscoveryPromptBuilder.Build(groups, ["Groceries"], []);

        // Assert
        Assert.DoesNotContain("PREVIOUSLY DISMISSED", prompt.UserPrompt);
    }

    [Fact]
    public void Build_Uses_Default_Temperature_And_MaxTokens()
    {
        // Arrange
        var groups = new List<DescriptionGroup>
        {
            new("HOME DEPOT", 5, 25m, 350m),
        };

        // Act
        var prompt = CategoryDiscoveryPromptBuilder.Build(groups, ["Groceries"]);

        // Assert
        Assert.Equal(0.3m, prompt.Temperature);
        Assert.Equal(2000, prompt.MaxTokens);
    }

    [Fact]
    public void Build_Includes_FewShot_Examples()
    {
        // Arrange
        var groups = new List<DescriptionGroup>
        {
            new("HOME DEPOT", 5, 25m, 350m),
        };

        // Act
        var prompt = CategoryDiscoveryPromptBuilder.Build(groups, ["Groceries"]);

        // Assert
        Assert.Contains("FEW-SHOT EXAMPLES", prompt.UserPrompt);
        Assert.Contains("Home Improvement", prompt.UserPrompt);
        Assert.Contains("Pet Care", prompt.UserPrompt);
    }

    [Fact]
    public void Build_Includes_Amount_Ranges_In_Descriptions()
    {
        // Arrange
        var groups = new List<DescriptionGroup>
        {
            new("HOME DEPOT", 5, 25.00m, 350.00m),
        };

        // Act
        var prompt = CategoryDiscoveryPromptBuilder.Build(groups, []);

        // Assert
        Assert.Contains("$25.00", prompt.UserPrompt);
        Assert.Contains("$350.00", prompt.UserPrompt);
    }
}
