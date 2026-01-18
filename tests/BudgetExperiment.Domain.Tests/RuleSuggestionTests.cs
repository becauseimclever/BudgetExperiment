// <copyright file="RuleSuggestionTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the RuleSuggestion entity.
/// </summary>
public class RuleSuggestionTests
{
    private static readonly Guid ValidCategoryId = Guid.NewGuid();
    private static readonly Guid ValidRuleId = Guid.NewGuid();

    #region CreateNewRuleSuggestion Tests

    [Fact]
    public void CreateNewRuleSuggestion_With_Valid_Data_Creates_Suggestion()
    {
        // Arrange
        var title = "Add AMAZON rule";
        var description = "Create rule to categorize Amazon purchases";
        var reasoning = "Found 15 transactions matching AMAZON pattern";
        var confidence = 0.85m;
        var pattern = "AMAZON";
        var matchType = RuleMatchType.Contains;
        var affectedCount = 15;
        var samples = new[] { "AMAZON.COM*123", "AMAZON PRIME", "AMZN MKTP" };

        // Act
        var suggestion = RuleSuggestion.CreateNewRuleSuggestion(
            title,
            description,
            reasoning,
            confidence,
            pattern,
            matchType,
            ValidCategoryId,
            affectedCount,
            samples);

        // Assert
        Assert.NotEqual(Guid.Empty, suggestion.Id);
        Assert.Equal(SuggestionType.NewRule, suggestion.Type);
        Assert.Equal(SuggestionStatus.Pending, suggestion.Status);
        Assert.Equal(title, suggestion.Title);
        Assert.Equal(description, suggestion.Description);
        Assert.Equal(reasoning, suggestion.Reasoning);
        Assert.Equal(confidence, suggestion.Confidence);
        Assert.Equal(pattern, suggestion.SuggestedPattern);
        Assert.Equal(matchType, suggestion.SuggestedMatchType);
        Assert.Equal(ValidCategoryId, suggestion.SuggestedCategoryId);
        Assert.Equal(affectedCount, suggestion.AffectedTransactionCount);
        Assert.Equal(samples, suggestion.SampleDescriptions);
        Assert.NotEqual(default, suggestion.CreatedAtUtc);
        Assert.Null(suggestion.ReviewedAtUtc);
        Assert.Null(suggestion.TargetRuleId);
        Assert.Null(suggestion.OptimizedPattern);
        Assert.Empty(suggestion.ConflictingRuleIds);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateNewRuleSuggestion_With_Empty_Title_Throws(string? title)
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            RuleSuggestion.CreateNewRuleSuggestion(
                title!,
                "Description",
                "Reasoning",
                0.8m,
                "PATTERN",
                RuleMatchType.Contains,
                ValidCategoryId,
                10,
                Array.Empty<string>()));
        Assert.Contains("title", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateNewRuleSuggestion_With_Title_Too_Long_Throws()
    {
        // Arrange
        var title = new string('A', 201);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            RuleSuggestion.CreateNewRuleSuggestion(
                title,
                "Description",
                "Reasoning",
                0.8m,
                "PATTERN",
                RuleMatchType.Contains,
                ValidCategoryId,
                10,
                Array.Empty<string>()));
        Assert.Contains("200", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateNewRuleSuggestion_With_Empty_Pattern_Throws(string? pattern)
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            RuleSuggestion.CreateNewRuleSuggestion(
                "Title",
                "Description",
                "Reasoning",
                0.8m,
                pattern!,
                RuleMatchType.Contains,
                ValidCategoryId,
                10,
                Array.Empty<string>()));
        Assert.Contains("pattern", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateNewRuleSuggestion_With_Empty_CategoryId_Throws()
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            RuleSuggestion.CreateNewRuleSuggestion(
                "Title",
                "Description",
                "Reasoning",
                0.8m,
                "PATTERN",
                RuleMatchType.Contains,
                Guid.Empty,
                10,
                Array.Empty<string>()));
        Assert.Contains("category", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(-1)]
    [InlineData(2)]
    public void CreateNewRuleSuggestion_With_Invalid_Confidence_Throws(decimal confidence)
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            RuleSuggestion.CreateNewRuleSuggestion(
                "Title",
                "Description",
                "Reasoning",
                confidence,
                "PATTERN",
                RuleMatchType.Contains,
                ValidCategoryId,
                10,
                Array.Empty<string>()));
        Assert.Contains("confidence", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.5)]
    [InlineData(1)]
    public void CreateNewRuleSuggestion_With_Valid_Confidence_Succeeds(decimal confidence)
    {
        // Act
        var suggestion = RuleSuggestion.CreateNewRuleSuggestion(
            "Title",
            "Description",
            "Reasoning",
            confidence,
            "PATTERN",
            RuleMatchType.Contains,
            ValidCategoryId,
            10,
            Array.Empty<string>());

        // Assert
        Assert.Equal(confidence, suggestion.Confidence);
    }

    [Fact]
    public void CreateNewRuleSuggestion_With_Negative_AffectedCount_Throws()
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            RuleSuggestion.CreateNewRuleSuggestion(
                "Title",
                "Description",
                "Reasoning",
                0.8m,
                "PATTERN",
                RuleMatchType.Contains,
                ValidCategoryId,
                -1,
                Array.Empty<string>()));
        Assert.Contains("affected", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateNewRuleSuggestion_Trims_Title()
    {
        // Act
        var suggestion = RuleSuggestion.CreateNewRuleSuggestion(
            "  Trimmed Title  ",
            "Description",
            "Reasoning",
            0.8m,
            "PATTERN",
            RuleMatchType.Contains,
            ValidCategoryId,
            10,
            Array.Empty<string>());

        // Assert
        Assert.Equal("Trimmed Title", suggestion.Title);
    }

    #endregion

    #region CreateOptimizationSuggestion Tests

    [Fact]
    public void CreateOptimizationSuggestion_With_Valid_Data_Creates_Suggestion()
    {
        // Arrange
        var title = "Simplify AMAZON rules";
        var description = "Consolidate 3 Amazon rules into one";
        var reasoning = "Rules AMAZON.COM, AMAZON PRIME, AMZN can use pattern AMAZON";
        var confidence = 0.9m;
        var optimizedPattern = "AMAZON";

        // Act
        var suggestion = RuleSuggestion.CreateOptimizationSuggestion(
            title,
            description,
            reasoning,
            confidence,
            ValidRuleId,
            optimizedPattern);

        // Assert
        Assert.NotEqual(Guid.Empty, suggestion.Id);
        Assert.Equal(SuggestionType.PatternOptimization, suggestion.Type);
        Assert.Equal(SuggestionStatus.Pending, suggestion.Status);
        Assert.Equal(title, suggestion.Title);
        Assert.Equal(description, suggestion.Description);
        Assert.Equal(reasoning, suggestion.Reasoning);
        Assert.Equal(confidence, suggestion.Confidence);
        Assert.Equal(ValidRuleId, suggestion.TargetRuleId);
        Assert.Equal(optimizedPattern, suggestion.OptimizedPattern);
        Assert.Null(suggestion.SuggestedPattern);
        Assert.Null(suggestion.SuggestedCategoryId);
        Assert.Empty(suggestion.ConflictingRuleIds);
    }

    [Fact]
    public void CreateOptimizationSuggestion_With_Empty_TargetRuleId_Throws()
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            RuleSuggestion.CreateOptimizationSuggestion(
                "Title",
                "Description",
                "Reasoning",
                0.8m,
                Guid.Empty,
                "PATTERN"));
        Assert.Contains("target", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateOptimizationSuggestion_With_Empty_OptimizedPattern_Throws(string? pattern)
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            RuleSuggestion.CreateOptimizationSuggestion(
                "Title",
                "Description",
                "Reasoning",
                0.8m,
                ValidRuleId,
                pattern!));
        Assert.Contains("pattern", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region CreateConflictSuggestion Tests

    [Fact]
    public void CreateConflictSuggestion_With_Valid_Data_Creates_Suggestion()
    {
        // Arrange
        var title = "Conflicting rules detected";
        var description = "Two rules match the same transactions";
        var reasoning = "Rules 'Groceries' and 'Shopping' both match WALMART";
        var conflictingIds = new[] { Guid.NewGuid(), Guid.NewGuid() };

        // Act
        var suggestion = RuleSuggestion.CreateConflictSuggestion(
            title,
            description,
            reasoning,
            conflictingIds);

        // Assert
        Assert.NotEqual(Guid.Empty, suggestion.Id);
        Assert.Equal(SuggestionType.RuleConflict, suggestion.Type);
        Assert.Equal(SuggestionStatus.Pending, suggestion.Status);
        Assert.Equal(title, suggestion.Title);
        Assert.Equal(description, suggestion.Description);
        Assert.Equal(reasoning, suggestion.Reasoning);
        Assert.Equal(0m, suggestion.Confidence); // Conflicts don't have confidence
        Assert.Equal(conflictingIds, suggestion.ConflictingRuleIds);
        Assert.Null(suggestion.TargetRuleId);
        Assert.Null(suggestion.SuggestedPattern);
    }

    [Fact]
    public void CreateConflictSuggestion_With_Empty_ConflictingIds_Throws()
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            RuleSuggestion.CreateConflictSuggestion(
                "Title",
                "Description",
                "Reasoning",
                Array.Empty<Guid>()));
        Assert.Contains("conflict", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateConflictSuggestion_With_Single_ConflictingId_Throws()
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            RuleSuggestion.CreateConflictSuggestion(
                "Title",
                "Description",
                "Reasoning",
                new[] { Guid.NewGuid() }));
        Assert.Contains("two", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region CreateUnusedRuleSuggestion Tests

    [Fact]
    public void CreateUnusedRuleSuggestion_With_Valid_Data_Creates_Suggestion()
    {
        // Arrange
        var title = "Unused rule detected";
        var description = "Rule has not matched any transactions";
        var reasoning = "Rule 'Old Store' has 0 matches in the last 6 months";

        // Act
        var suggestion = RuleSuggestion.CreateUnusedRuleSuggestion(
            title,
            description,
            reasoning,
            ValidRuleId);

        // Assert
        Assert.NotEqual(Guid.Empty, suggestion.Id);
        Assert.Equal(SuggestionType.UnusedRule, suggestion.Type);
        Assert.Equal(SuggestionStatus.Pending, suggestion.Status);
        Assert.Equal(ValidRuleId, suggestion.TargetRuleId);
        Assert.Equal(0, suggestion.AffectedTransactionCount);
    }

    [Fact]
    public void CreateUnusedRuleSuggestion_With_Empty_TargetRuleId_Throws()
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            RuleSuggestion.CreateUnusedRuleSuggestion(
                "Title",
                "Description",
                "Reasoning",
                Guid.Empty));
        Assert.Contains("target", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region CreateConsolidationSuggestion Tests

    [Fact]
    public void CreateConsolidationSuggestion_With_Valid_Data_Creates_Suggestion()
    {
        // Arrange
        var title = "Consolidate Amazon rules";
        var description = "Merge 3 similar rules into one";
        var reasoning = "Rules share similar patterns and same category";
        var confidence = 0.85m;
        var ruleIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var consolidatedPattern = "AMAZON|AMZN";

        // Act
        var suggestion = RuleSuggestion.CreateConsolidationSuggestion(
            title,
            description,
            reasoning,
            confidence,
            ruleIds,
            consolidatedPattern);

        // Assert
        Assert.NotEqual(Guid.Empty, suggestion.Id);
        Assert.Equal(SuggestionType.RuleConsolidation, suggestion.Type);
        Assert.Equal(SuggestionStatus.Pending, suggestion.Status);
        Assert.Equal(ruleIds, suggestion.ConflictingRuleIds); // Reuses field for rules to consolidate
        Assert.Equal(consolidatedPattern, suggestion.OptimizedPattern);
    }

    [Fact]
    public void CreateConsolidationSuggestion_With_Less_Than_Two_Rules_Throws()
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            RuleSuggestion.CreateConsolidationSuggestion(
                "Title",
                "Description",
                "Reasoning",
                0.8m,
                new[] { Guid.NewGuid() },
                "PATTERN"));
        Assert.Contains("two", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Accept Tests

    [Fact]
    public void Accept_Changes_Status_To_Accepted()
    {
        // Arrange
        var suggestion = RuleSuggestion.CreateNewRuleSuggestion(
            "Title",
            "Description",
            "Reasoning",
            0.8m,
            "PATTERN",
            RuleMatchType.Contains,
            ValidCategoryId,
            10,
            Array.Empty<string>());

        // Act
        suggestion.Accept();

        // Assert
        Assert.Equal(SuggestionStatus.Accepted, suggestion.Status);
        Assert.NotNull(suggestion.ReviewedAtUtc);
    }

    [Fact]
    public void Accept_When_Already_Accepted_Throws()
    {
        // Arrange
        var suggestion = RuleSuggestion.CreateNewRuleSuggestion(
            "Title",
            "Description",
            "Reasoning",
            0.8m,
            "PATTERN",
            RuleMatchType.Contains,
            ValidCategoryId,
            10,
            Array.Empty<string>());
        suggestion.Accept();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => suggestion.Accept());
        Assert.Contains("pending", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Accept_When_Dismissed_Throws()
    {
        // Arrange
        var suggestion = RuleSuggestion.CreateNewRuleSuggestion(
            "Title",
            "Description",
            "Reasoning",
            0.8m,
            "PATTERN",
            RuleMatchType.Contains,
            ValidCategoryId,
            10,
            Array.Empty<string>());
        suggestion.Dismiss();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => suggestion.Accept());
        Assert.Contains("pending", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Dismiss Tests

    [Fact]
    public void Dismiss_Changes_Status_To_Dismissed()
    {
        // Arrange
        var suggestion = RuleSuggestion.CreateNewRuleSuggestion(
            "Title",
            "Description",
            "Reasoning",
            0.8m,
            "PATTERN",
            RuleMatchType.Contains,
            ValidCategoryId,
            10,
            Array.Empty<string>());

        // Act
        suggestion.Dismiss();

        // Assert
        Assert.Equal(SuggestionStatus.Dismissed, suggestion.Status);
        Assert.NotNull(suggestion.ReviewedAtUtc);
        Assert.Null(suggestion.DismissalReason);
    }

    [Fact]
    public void Dismiss_With_Reason_Stores_Reason()
    {
        // Arrange
        var suggestion = RuleSuggestion.CreateNewRuleSuggestion(
            "Title",
            "Description",
            "Reasoning",
            0.8m,
            "PATTERN",
            RuleMatchType.Contains,
            ValidCategoryId,
            10,
            Array.Empty<string>());
        var reason = "Not applicable to my use case";

        // Act
        suggestion.Dismiss(reason);

        // Assert
        Assert.Equal(SuggestionStatus.Dismissed, suggestion.Status);
        Assert.Equal(reason, suggestion.DismissalReason);
    }

    [Fact]
    public void Dismiss_When_Already_Dismissed_Throws()
    {
        // Arrange
        var suggestion = RuleSuggestion.CreateNewRuleSuggestion(
            "Title",
            "Description",
            "Reasoning",
            0.8m,
            "PATTERN",
            RuleMatchType.Contains,
            ValidCategoryId,
            10,
            Array.Empty<string>());
        suggestion.Dismiss();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => suggestion.Dismiss());
        Assert.Contains("pending", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Dismiss_When_Accepted_Throws()
    {
        // Arrange
        var suggestion = RuleSuggestion.CreateNewRuleSuggestion(
            "Title",
            "Description",
            "Reasoning",
            0.8m,
            "PATTERN",
            RuleMatchType.Contains,
            ValidCategoryId,
            10,
            Array.Empty<string>());
        suggestion.Accept();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => suggestion.Dismiss());
        Assert.Contains("pending", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region ProvideFeedback Tests

    [Fact]
    public void ProvideFeedback_Positive_Sets_Flag()
    {
        // Arrange
        var suggestion = RuleSuggestion.CreateNewRuleSuggestion(
            "Title",
            "Description",
            "Reasoning",
            0.8m,
            "PATTERN",
            RuleMatchType.Contains,
            ValidCategoryId,
            10,
            Array.Empty<string>());
        suggestion.Accept();

        // Act
        suggestion.ProvideFeedback(positive: true);

        // Assert
        Assert.True(suggestion.UserFeedbackPositive);
    }

    [Fact]
    public void ProvideFeedback_Negative_Sets_Flag()
    {
        // Arrange
        var suggestion = RuleSuggestion.CreateNewRuleSuggestion(
            "Title",
            "Description",
            "Reasoning",
            0.8m,
            "PATTERN",
            RuleMatchType.Contains,
            ValidCategoryId,
            10,
            Array.Empty<string>());
        suggestion.Accept();

        // Act
        suggestion.ProvideFeedback(positive: false);

        // Assert
        Assert.False(suggestion.UserFeedbackPositive);
    }

    [Fact]
    public void ProvideFeedback_When_Pending_Throws()
    {
        // Arrange
        var suggestion = RuleSuggestion.CreateNewRuleSuggestion(
            "Title",
            "Description",
            "Reasoning",
            0.8m,
            "PATTERN",
            RuleMatchType.Contains,
            ValidCategoryId,
            10,
            Array.Empty<string>());

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => suggestion.ProvideFeedback(true));
        Assert.Contains("reviewed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProvideFeedback_Can_Be_Changed()
    {
        // Arrange
        var suggestion = RuleSuggestion.CreateNewRuleSuggestion(
            "Title",
            "Description",
            "Reasoning",
            0.8m,
            "PATTERN",
            RuleMatchType.Contains,
            ValidCategoryId,
            10,
            Array.Empty<string>());
        suggestion.Accept();
        suggestion.ProvideFeedback(positive: true);

        // Act
        suggestion.ProvideFeedback(positive: false);

        // Assert
        Assert.False(suggestion.UserFeedbackPositive);
    }

    #endregion
}
