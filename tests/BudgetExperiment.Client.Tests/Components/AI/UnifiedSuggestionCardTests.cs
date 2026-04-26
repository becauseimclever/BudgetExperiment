// <copyright file="UnifiedSuggestionCardTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.AI;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace BudgetExperiment.Client.Tests.Components.AI;

/// <summary>
/// Unit tests for the UnifiedSuggestionCard component.
/// </summary>
public class UnifiedSuggestionCardTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnifiedSuggestionCardTests"/> class.
    /// </summary>
    public UnifiedSuggestionCardTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<ThemeService>();
        this.Services.AddSingleton<CultureService>();
        this.Services.AddSingleton<IFeatureFlagClientService>(new TestHelpers.StubFeatureFlagClientService());
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies that a rule suggestion card displays the title and description.
    /// </summary>
    [Fact]
    public void RuleSuggestionCard_DisplaysTitle_AndDescription()
    {
        // Arrange
        var suggestionId = Guid.NewGuid();
        var ruleSuggestion = new RuleSuggestionDto
        {
            Id = suggestionId,
            Title = "Auto-categorize Coffee",
            Description = "Pattern: *STARBUCKS*",
            Confidence = 0.85m,
            AffectedTransactionCount = 5,
            SuggestedPattern = "*STARBUCKS*",
            SuggestedMatchType = "Contains",
            SuggestedCategoryName = "Beverages",
            SuggestedKakeiboCategory = "Wants",
            Reasoning = "Multiple STARBUCKS transactions categorized as Beverages",
            SampleDescriptions = new List<string> { "STARBUCKS #12345", "STARBUCKS COFFEE" },
            UserFeedbackPositive = null,
        };

        // Act
        var cut = Render<UnifiedSuggestionCard>(p => p
            .Add(x => x.Item, (object)ruleSuggestion));

        // Assert
        cut.Markup.ShouldContain("Auto-categorize Coffee");
        cut.Markup.ShouldContain("Pattern: *STARBUCKS*");
    }

    /// <summary>
    /// Verifies that a rule suggestion card shows "High Confidence" badge when confidence is greater than 0.8.
    /// </summary>
    [Fact]
    public void RuleSuggestionCard_HighConfidence_ShowsHighBadge()
    {
        // Arrange
        var ruleSuggestion = new RuleSuggestionDto
        {
            Id = Guid.NewGuid(),
            Title = "Test Rule",
            Description = "Test description",
            Confidence = 0.85m,
            AffectedTransactionCount = 3,
            SuggestedPattern = "*TEST*",
            SampleDescriptions = [],
        };

        // Act
        var cut = Render<UnifiedSuggestionCard>(p => p
            .Add(x => x.Item, (object)ruleSuggestion));

        // Assert
        cut.Markup.ShouldContain("High Confidence");
        cut.Markup.ShouldContain("high"); // CSS class
    }

    /// <summary>
    /// Verifies that a rule suggestion card shows "Medium Confidence" badge when 0.5 is less than confidence is less than or equal to 0.8.
    /// </summary>
    [Fact]
    public void RuleSuggestionCard_MediumConfidence_ShowsMediumBadge()
    {
        // Arrange
        var ruleSuggestion = new RuleSuggestionDto
        {
            Id = Guid.NewGuid(),
            Title = "Test Rule",
            Description = "Test description",
            Confidence = 0.65m,
            AffectedTransactionCount = 2,
            SuggestedPattern = "*TEST*",
            SampleDescriptions = [],
        };

        // Act
        var cut = Render<UnifiedSuggestionCard>(p => p
            .Add(x => x.Item, (object)ruleSuggestion));

        // Assert
        cut.Markup.ShouldContain("Medium Confidence");
        cut.Markup.ShouldContain("medium");
    }

    /// <summary>
    /// Verifies that a rule suggestion card shows "Low Confidence" badge when confidence is less than or equal to 0.5.
    /// </summary>
    [Fact]
    public void RuleSuggestionCard_LowConfidence_ShowsLowBadge()
    {
        // Arrange
        var ruleSuggestion = new RuleSuggestionDto
        {
            Id = Guid.NewGuid(),
            Title = "Test Rule",
            Description = "Test description",
            Confidence = 0.4m,
            AffectedTransactionCount = 1,
            SuggestedPattern = "*TEST*",
            SampleDescriptions = [],
        };

        // Act
        var cut = Render<UnifiedSuggestionCard>(p => p
            .Add(x => x.Item, (object)ruleSuggestion));

        // Assert
        cut.Markup.ShouldContain("Low Confidence");
        cut.Markup.ShouldContain("low");
    }

    /// <summary>
    /// Verifies that Accept button invokes OnAccept callback with correct suggestion ID.
    /// </summary>
    [Fact]
    public void RuleSuggestionCard_AcceptButton_InvokesOnAcceptWithCorrectId()
    {
        // Arrange
        var suggestionId = Guid.NewGuid();
        var ruleSuggestion = new RuleSuggestionDto
        {
            Id = suggestionId,
            Title = "Test Rule",
            Description = "Test description",
            Confidence = 0.75m,
            AffectedTransactionCount = 3,
            SuggestedPattern = "*TEST*",
            SampleDescriptions = [],
        };

        var acceptedId = Guid.Empty;

        // Act
        var cut = Render<UnifiedSuggestionCard>(p => p
            .Add(x => x.Item, (object)ruleSuggestion)
            .Add(x => x.OnAccept, EventCallback.Factory.Create<Guid>(this, (Guid id) => { acceptedId = id; })));

        var acceptButton = cut.FindAll(".btn-success").FirstOrDefault();
        acceptButton?.Click();

        // Assert
        acceptedId.ShouldBe(suggestionId);
    }

    /// <summary>
    /// Verifies that Dismiss button invokes OnDismiss callback.
    /// </summary>
    [Fact]
    public void RuleSuggestionCard_DismissButton_InvokesOnDismiss()
    {
        // Arrange
        var suggestionId = Guid.NewGuid();
        var ruleSuggestion = new RuleSuggestionDto
        {
            Id = suggestionId,
            Title = "Test Rule",
            Description = "Test description",
            Confidence = 0.75m,
            AffectedTransactionCount = 3,
            SuggestedPattern = "*TEST*",
            SampleDescriptions = [],
        };

        var dismissedId = Guid.Empty;

        // Act
        var cut = Render<UnifiedSuggestionCard>(p => p
            .Add(x => x.Item, (object)ruleSuggestion)
            .Add(x => x.OnDismiss, EventCallback.Factory.Create<Guid>(this, (Guid id) => { dismissedId = id; })));

        var dismissButton = cut.FindAll(".btn-secondary").FirstOrDefault();
        dismissButton?.Click();

        // Assert
        dismissedId.ShouldBe(suggestionId);
    }

    /// <summary>
    /// Verifies that Details toggle expands the details section.
    /// </summary>
    [Fact]
    public void RuleSuggestionCard_DetailsToggle_ExpandsDetailsSection()
    {
        // Arrange
        var ruleSuggestion = new RuleSuggestionDto
        {
            Id = Guid.NewGuid(),
            Title = "Test Rule",
            Description = "Test description",
            Confidence = 0.75m,
            AffectedTransactionCount = 3,
            SuggestedPattern = "*TEST*",
            SuggestedCategoryName = "Test Category",
            Reasoning = "Test reasoning",
            SampleDescriptions = ["Sample 1", "Sample 2"],
        };

        // Act
        var cut = Render<UnifiedSuggestionCard>(p => p
            .Add(x => x.Item, (object)ruleSuggestion));

        // Details should be hidden initially
        var detailsSection = cut.FindAll(".card-details");
        detailsSection.Count.ShouldBe(0);

        // Click Details toggle
        var detailsToggle = cut.Find(".details-toggle");
        detailsToggle.Click();

        // Assert
        var expandedDetails = cut.FindAll(".card-details");
        expandedDetails.Count.ShouldBeGreaterThan(0);
        cut.Markup.ShouldContain("Test reasoning");
    }

    /// <summary>
    /// Verifies that Details toggle collapses the details section after expansion.
    /// </summary>
    [Fact]
    public void RuleSuggestionCard_DetailsToggle_CollapsesAfterExpand()
    {
        // Arrange
        var ruleSuggestion = new RuleSuggestionDto
        {
            Id = Guid.NewGuid(),
            Title = "Test Rule",
            Description = "Test description",
            Confidence = 0.75m,
            AffectedTransactionCount = 3,
            SuggestedPattern = "*TEST*",
            SuggestedCategoryName = "Test Category",
            Reasoning = "Test reasoning",
            SampleDescriptions = ["Sample 1"],
        };

        // Act
        var cut = Render<UnifiedSuggestionCard>(p => p
            .Add(x => x.Item, (object)ruleSuggestion));

        // Click Details toggle to expand
        var detailsToggle = cut.Find(".details-toggle");
        detailsToggle.Click();

        // Details should be visible
        cut.Markup.ShouldContain("Test reasoning");

        // Click Details toggle again to collapse
        detailsToggle.Click();

        // Assert - details content should not be visible
        var detailsSection = cut.FindAll(".card-details");
        detailsSection.Count.ShouldBe(0);
    }

    /// <summary>
    /// Verifies that category suggestion card displays correctly.
    /// </summary>
    [Fact]
    public void CategorySuggestionCard_DisplaysCorrectly()
    {
        // Arrange
        var categorySuggestion = new CategorySuggestionDto
        {
            Id = Guid.NewGuid(),
            SuggestedName = "Coffee Shops",
            MatchingTransactionCount = 8,
            Confidence = 0.9m,
            Source = "AiDiscovered",
            MerchantPatterns = ["STARBUCKS", "DUNKIN", "LOCAL COFFEE"],
            SuggestedType = "Dining",
            Reasoning = "Found consistent merchant patterns for coffee purchases",
            Status = "Pending",
            CreatedAtUtc = DateTime.UtcNow,
        };

        // Act
        var cut = Render<UnifiedSuggestionCard>(p => p
            .Add(x => x.Item, (object)categorySuggestion));

        // Assert
        cut.Markup.ShouldContain("Coffee Shops");
        cut.Markup.ShouldContain("AI");
        cut.Markup.ShouldContain("8 matching transactions");
    }

    /// <summary>
    /// Verifies that feedback buttons for rule suggestions can be toggled.
    /// </summary>
    [Fact]
    public void RuleSuggestionCard_FeedbackButtons_CanTogglePositive()
    {
        // Arrange
        var suggestionId = Guid.NewGuid();
        var ruleSuggestion = new RuleSuggestionDto
        {
            Id = suggestionId,
            Title = "Test Rule",
            Description = "Test description",
            Confidence = 0.75m,
            AffectedTransactionCount = 3,
            SuggestedPattern = "*TEST*",
            Reasoning = "Test reasoning",
            SampleDescriptions = [],
            UserFeedbackPositive = null,
        };

        var feedbackReceived = false;

        // Act
        var cut = Render<UnifiedSuggestionCard>(p => p
            .Add(x => x.Item, (object)ruleSuggestion)
            .Add(x => x.OnFeedback, EventCallback.Factory.Create<(Guid, bool)>(this, feedback => { feedbackReceived = true; })));

        // Expand details to see feedback buttons
        var detailsToggle = cut.Find(".details-toggle");
        detailsToggle.Click();

        // Find thumbs-up button
        var feedbackBtns = cut.FindAll(".feedback-btn");
        feedbackBtns[0].Click();

        // Assert
        feedbackReceived.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that category suggestion shows merchant patterns in details.
    /// </summary>
    [Fact]
    public void CategorySuggestionCard_ShowsMerchantPatterns_InDetails()
    {
        // Arrange
        var categorySuggestion = new CategorySuggestionDto
        {
            Id = Guid.NewGuid(),
            SuggestedName = "Coffee Shops",
            MatchingTransactionCount = 5,
            Confidence = 0.85m,
            Source = "AiDiscovered",
            MerchantPatterns = ["STARBUCKS", "DUNKIN", "LOCAL COFFEE"],
            SuggestedType = "Dining",
            Status = "Pending",
            CreatedAtUtc = DateTime.UtcNow,
        };

        // Act
        var cut = Render<UnifiedSuggestionCard>(p => p
            .Add(x => x.Item, (object)categorySuggestion));

        // Expand details
        var detailsToggle = cut.Find(".details-toggle");
        detailsToggle.Click();

        // Assert
        cut.Markup.ShouldContain("STARBUCKS");
        cut.Markup.ShouldContain("DUNKIN");
    }

    /// <summary>
    /// Verifies that high-confidence rule suggestions have the high-confidence border.
    /// </summary>
    [Fact]
    public void RuleSuggestionCard_HighConfidence_ShowsBorder()
    {
        // Arrange
        var ruleSuggestion = new RuleSuggestionDto
        {
            Id = Guid.NewGuid(),
            Title = "Test Rule",
            Description = "Test description",
            Confidence = 0.95m,
            AffectedTransactionCount = 10,
            SuggestedPattern = "*TEST*",
            SampleDescriptions = [],
        };

        // Act
        var cut = Render<UnifiedSuggestionCard>(p => p
            .Add(x => x.Item, (object)ruleSuggestion));

        // Assert
        var card = cut.Find(".suggestion-card");
        card.ClassList.ShouldContain("high-confidence");
    }

    /// <summary>
    /// Verifies that the impact summary displays transaction count correctly.
    /// </summary>
    [Fact]
    public void RuleSuggestionCard_ImpactSummary_DisplaysTransactionCount()
    {
        // Arrange
        var ruleSuggestion = new RuleSuggestionDto
        {
            Id = Guid.NewGuid(),
            Title = "Test Rule",
            Description = "Test description",
            Confidence = 0.75m,
            AffectedTransactionCount = 7,
            SuggestedPattern = "*TEST*",
            SampleDescriptions = [],
        };

        // Act
        var cut = Render<UnifiedSuggestionCard>(p => p
            .Add(x => x.Item, (object)ruleSuggestion));

        // Assert
        cut.Markup.ShouldContain("7 transaction(s)");
    }
}
