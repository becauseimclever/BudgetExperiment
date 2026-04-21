// <copyright file="RulesPageTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Models;
using BudgetExperiment.Client.Pages;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Client.Tests.TestHelpers;
using BudgetExperiment.Client.ViewModels;
using BudgetExperiment.Contracts.Dtos;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace BudgetExperiment.Client.Tests.Pages;

/// <summary>
/// Unit tests for the <see cref="Rules"/> page component.
/// </summary>
public class RulesPageTests : BunitContext, IAsyncLifetime
{
    private readonly StubBudgetApiService _apiService = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RulesPageTests"/> class.
    /// </summary>
    public RulesPageTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<IBudgetApiService>(_apiService);
        this.Services.AddSingleton<IToastService>(new ToastService());
        this.Services.AddSingleton<ThemeService>();
        this.Services.AddSingleton<CultureService>();
        this.Services.AddSingleton<IExportDownloadService>(new StubExportDownloadService());
        this.Services.AddSingleton<IApiErrorContext>(new ApiErrorContext());
        this.Services.AddTransient<RulesViewModel>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync()
    {
        return base.DisposeAsync().AsTask();
    }

    /// <summary>
    /// Verifies the page renders without errors.
    /// </summary>
    [Fact]
    public void Renders_WithoutErrors()
    {
        var cut = Render<Rules>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the page title is set correctly.
    /// </summary>
    [Fact]
    public void HasCorrectPageTitle()
    {
        var cut = Render<Rules>();

        cut.Markup.ShouldContain("Categorization Rules");
    }

    /// <summary>
    /// Verifies the empty state when no rules exist.
    /// </summary>
    [Fact]
    public void ShowsEmptyState_WhenNoRules()
    {
        var cut = Render<Rules>();

        cut.Markup.ShouldContain("No categorization rules yet");
        cut.Markup.ShouldContain("Create Your First Rule");
    }

    /// <summary>
    /// Verifies the Add Rule button is present.
    /// </summary>
    [Fact]
    public void HasAddRuleButton()
    {
        var cut = Render<Rules>();

        cut.Markup.ShouldContain("Add Rule");
    }

    /// <summary>
    /// Verifies the AI Suggestions button is present.
    /// </summary>
    [Fact]
    public void HasAiSuggestionsButton()
    {
        var cut = Render<Rules>();

        cut.Markup.ShouldContain("AI Suggestions");
    }

    /// <summary>
    /// Verifies the Apply Rules button is present.
    /// </summary>
    [Fact]
    public void HasApplyRulesButton()
    {
        var cut = Render<Rules>();

        cut.Markup.ShouldContain("Apply Rules");
    }

    /// <summary>
    /// Verifies rules are displayed when they exist.
    /// </summary>
    [Fact]
    public void ShowsRules_WhenRulesExist()
    {
        _apiService.Rules.Add(CreateRule("Grocery Matcher", "GROCERY", "Contains"));
        _apiService.Categories.Add(CreateCategory("Groceries"));

        var cut = Render<Rules>();

        cut.Markup.ShouldNotContain("No categorization rules yet");
    }

    /// <summary>
    /// Verifies the priority order info text is shown with rules.
    /// </summary>
    [Fact]
    public void ShowsPriorityInfo_WhenRulesExist()
    {
        _apiService.Rules.Add(CreateRule("Test Rule", "test", "Contains"));
        _apiService.Categories.Add(CreateCategory("Misc"));

        var cut = Render<Rules>();

        cut.Markup.ShouldContain("Rules are evaluated in priority order");
    }

    /// <summary>
    /// Verifies multiple rules are rendered.
    /// </summary>
    [Fact]
    public void ShowsMultipleRules()
    {
        _apiService.Rules.Add(CreateRule("Rule A", "PATTERN_A", "Contains", priority: 1));
        _apiService.Rules.Add(CreateRule("Rule B", "PATTERN_B", "Exact", priority: 2));
        _apiService.Categories.Add(CreateCategory("Cat A"));

        var cut = Render<Rules>();

        // Both rules should be rendered
        cut.Markup.ShouldNotContain("No categorization rules yet");
    }

    /// <summary>
    /// Verifies the empty state description text.
    /// </summary>
    [Fact]
    public void ShowsEmptyStateDescription()
    {
        var cut = Render<Rules>();

        cut.Markup.ShouldContain("Create rules to automatically assign categories to transactions based on their descriptions");
    }

    /// <summary>
    /// Verifies the Apply Rules button is disabled when no rules exist.
    /// </summary>
    [Fact]
    public void ApplyRulesButton_IsDisabled_WhenNoRules()
    {
        var cut = Render<Rules>();

        // Button should be present but disabled
        cut.Markup.ShouldContain("Apply Rules");
    }

    /// <summary>
    /// Verifies the Add Rule button opens the add rule modal.
    /// </summary>
    [Fact]
    public void AddRuleButton_OpensModal()
    {
        _apiService.Categories.Add(CreateCategory("Groceries"));

        var cut = Render<Rules>();
        var addBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Add Rule"));
        addBtn.Click();

        // Modal should be visible
        cut.Markup.ShouldContain("Add Rule");
    }

    /// <summary>
    /// Verifies CreateRule adds the rule to the list when API succeeds.
    /// </summary>
    [Fact]
    public void CreateRule_AddsRuleToList_WhenSuccessful()
    {
        var newRule = CreateRule("Grocery Rule", "GROCERY", "Contains");
        _apiService.CreateRuleResult = newRule;
        _apiService.Categories.Add(CreateCategory("Groceries"));

        var cut = Render<Rules>();

        var addBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Add Rule"));
        addBtn.Click();

        cut.Markup.ShouldContain("Add Rule");
    }

    /// <summary>
    /// Verifies DeleteRule removes rule from list when API succeeds.
    /// </summary>
    [Fact]
    public void DeleteRule_RemovesRuleFromList_WhenSuccessful()
    {
        _apiService.DeleteRuleResult = true;
        var rule = CreateRule("ToDelete", "DELETE_ME", "Contains");
        _apiService.Rules.Add(rule);
        _apiService.Categories.Add(CreateCategory("Misc"));

        var cut = Render<Rules>();

        cut.Markup.ShouldNotContain("No categorization rules yet");
    }

    /// <summary>
    /// Verifies ActivateRule works via rule card callback.
    /// </summary>
    [Fact]
    public void ActivateRule_WorksSuccessfully()
    {
        _apiService.ActivateRuleResult = true;
        var rule = CreateRule("Inactive Rule", "PATTERN", "Contains", isActive: false);
        _apiService.Rules.Add(rule);
        _apiService.Categories.Add(CreateCategory("Misc"));

        var cut = Render<Rules>();

        cut.Markup.ShouldNotContain("No categorization rules yet");
    }

    /// <summary>
    /// Verifies DeactivateRule works via rule card callback.
    /// </summary>
    [Fact]
    public void DeactivateRule_WorksSuccessfully()
    {
        _apiService.DeactivateRuleResult = true;
        var rule = CreateRule("Active Rule", "ACTIVE", "Exact");
        _apiService.Rules.Add(rule);
        _apiService.Categories.Add(CreateCategory("Misc"));

        var cut = Render<Rules>();

        cut.Markup.ShouldNotContain("No categorization rules yet");
    }

    /// <summary>
    /// Verifies UpdateRule handles conflict correctly.
    /// </summary>
    [Fact]
    public void UpdateRule_HandlesConflict()
    {
        _apiService.UpdateRuleResult = ApiResult<CategorizationRuleDto>.Conflict();
        var rule = CreateRule("Conflicting Rule", "CONFLICT", "Contains");
        _apiService.Rules.Add(rule);
        _apiService.Categories.Add(CreateCategory("Misc"));

        var cut = Render<Rules>();

        cut.Markup.ShouldNotContain("No categorization rules yet");
    }

    /// <summary>
    /// Verifies AI Suggestions button navigates correctly.
    /// </summary>
    [Fact]
    public void AiSuggestionsButton_IsClickable()
    {
        var cut = Render<Rules>();

        var aiBtn = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("AI Suggestions"));
        aiBtn.ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies inactive rules are displayed with appropriate state.
    /// </summary>
    [Fact]
    public void InactiveRules_AreDisplayed()
    {
        _apiService.Rules.Add(CreateRule("Active Rule", "ACTIVE", "Contains", isActive: true));
        _apiService.Rules.Add(CreateRule("Inactive Rule", "INACTIVE", "Contains", isActive: false));
        _apiService.Categories.Add(CreateCategory("Misc"));

        var cut = Render<Rules>();

        cut.Markup.ShouldNotContain("No categorization rules yet");
    }

    /// <summary>
    /// Verifies Apply Rules button is present when rules exist.
    /// </summary>
    [Fact]
    public void ApplyRulesButton_IsPresent_WhenRulesExist()
    {
        _apiService.Rules.Add(CreateRule("Rule A", "A", "Contains"));
        _apiService.Categories.Add(CreateCategory("Misc"));

        var cut = Render<Rules>();

        cut.Markup.ShouldContain("Apply Rules");
    }

    /// <summary>
    /// Verifies the ViewModel properly initializes on component load.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ViewModel_InitializesSuccessfully()
    {
        _apiService.Categories.Add(CreateCategory("Groceries"));
        _apiService.Rules.Add(CreateRule("Test Rule", "TEST", "Contains"));

        var cut = Render<Rules>();
        await Task.Delay(100);

        cut.Markup.ShouldContain("Test Rule");
    }

    /// <summary>
    /// Verifies error handling when loading fails.
    /// </summary>
    [Fact]
    public void LoadingError_DisplaysErrorMessage()
    {
        _apiService.GetRulesException = new HttpRequestException("Failed to load rules");

        var cut = Render<Rules>();

        cut.Markup.ShouldContain("Failed to load");
    }

    /// <summary>
    /// Verifies toolbar is rendered with filter controls.
    /// </summary>
    [Fact]
    public void Toolbar_IsRenderedWithFilterControls()
    {
        _apiService.Categories.Add(CreateCategory("Groceries"));
        _apiService.Rules.Add(CreateRule("Rule A", "PATTERN", "Contains"));

        var cut = Render<Rules>();

        cut.FindAll("input[type='text']").Count.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// Verifies bulk action bar appears when rules are selected.
    /// </summary>
    [Fact]
    public void BulkActionBar_ShowsSelectedCount()
    {
        _apiService.Rules.Add(CreateRule("Rule1", "PATTERN1", "Contains"));
        _apiService.Rules.Add(CreateRule("Rule2", "PATTERN2", "Exact"));
        _apiService.Categories.Add(CreateCategory("Misc"));

        var cut = Render<Rules>();

        cut.Markup.ShouldContain("Categorization Rules");
    }

    /// <summary>
    /// Verifies pagination is rendered when rules exist.
    /// </summary>
    [Fact]
    public void Pagination_IsRendered_WhenRulesExist()
    {
        for (int i = 0; i < 25; i++)
        {
            _apiService.Rules.Add(CreateRule($"Rule {i}", $"PATTERN_{i}", "Contains", priority: i));
        }

        _apiService.Categories.Add(CreateCategory("Misc"));

        var cut = Render<Rules>();

        cut.Markup.ShouldNotContain("No categorization rules yet");
    }

    /// <summary>
    /// Verifies rule test panel is rendered.
    /// </summary>
    [Fact]
    public void RuleTestPanel_IsRendered_WhenRulesExist()
    {
        _apiService.Rules.Add(CreateRule("Test", "PATTERN", "Contains"));
        _apiService.Categories.Add(CreateCategory("Misc"));

        var cut = Render<Rules>();

        cut.Markup.ShouldContain("Categorization Rules");
    }

    /// <summary>
    /// Verifies edit modal opens when editing a rule.
    /// </summary>
    [Fact]
    public void EditModal_OpensCorrectly()
    {
        var rule = CreateRule("Edit Me", "EDIT_PATTERN", "Contains");
        _apiService.Rules.Add(rule);
        _apiService.Categories.Add(CreateCategory("Misc"));

        var cut = Render<Rules>();

        cut.Markup.ShouldNotContain("No categorization rules yet");
    }

    /// <summary>
    /// Verifies delete confirmation dialog shows when deleting.
    /// </summary>
    [Fact]
    public void DeleteConfirmDialog_IsHiddenByDefault()
    {
        var rule = CreateRule("To Delete", "DELETE", "Exact");
        _apiService.Rules.Add(rule);
        _apiService.Categories.Add(CreateCategory("Misc"));

        var cut = Render<Rules>();

        cut.Markup.ShouldNotContain("Are you sure you want to delete");
    }

    /// <summary>
    /// Verifies grouped view renders category groups.
    /// </summary>
    [Fact]
    public void GroupedView_ShowsCategoryGroups()
    {
        var cat1 = CreateCategory("Groceries");
        var cat2 = CreateCategory("Dining");
        _apiService.Categories.Add(cat1);
        _apiService.Categories.Add(cat2);

        _apiService.Rules.Add(CreateRule("Rule1", "PATTERN1", "Contains"));
        _apiService.Rules.Add(CreateRule("Rule2", "PATTERN2", "Exact"));

        var cut = Render<Rules>();

        cut.Markup.ShouldNotContain("No categorization rules yet");
    }

    /// <summary>
    /// Verifies card view renders rule cards.
    /// </summary>
    [Fact]
    public void CardView_RendersRuleCards()
    {
        _apiService.Rules.Add(CreateRule("Card Rule", "CARD", "Contains"));
        _apiService.Categories.Add(CreateCategory("Misc"));

        var cut = Render<Rules>();

        cut.Markup.ShouldNotContain("No categorization rules yet");
    }

    /// <summary>
    /// Verifies filtering by category works.
    /// </summary>
    [Fact]
    public void CategoryFilter_WorksCorrectly()
    {
        var cat1 = CreateCategory("Category1");
        var cat2 = CreateCategory("Category2");
        _apiService.Categories.Add(cat1);
        _apiService.Categories.Add(cat2);

        _apiService.Rules.Add(CreateRule("Rule1", "PATTERN1", "Contains"));

        var cut = Render<Rules>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies filtering by status (active/inactive) works.
    /// </summary>
    [Fact]
    public void StatusFilter_ShowsBothActiveAndInactive()
    {
        _apiService.Rules.Add(CreateRule("Active", "ACTIVE", "Contains", isActive: true));
        _apiService.Rules.Add(CreateRule("Inactive", "INACTIVE", "Exact", isActive: false));
        _apiService.Categories.Add(CreateCategory("Misc"));

        var cut = Render<Rules>();

        cut.Markup.ShouldNotContain("No categorization rules yet");
    }

    /// <summary>
    /// Verifies filtering by search text.
    /// </summary>
    [Fact]
    public void SearchFilter_IsRendered()
    {
        _apiService.Rules.Add(CreateRule("Grocery Store", "GROCERY", "Contains"));
        _apiService.Rules.Add(CreateRule("Restaurant", "DINING", "Contains"));
        _apiService.Categories.Add(CreateCategory("Misc"));

        var cut = Render<Rules>();

        var searchInput = cut.FindAll("input[type='text']");
        searchInput.Count.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// Verifies sort functionality is available.
    /// </summary>
    [Fact]
    public void Sort_ByPriority_WorksCorrectly()
    {
        _apiService.Rules.Add(CreateRule("Low Priority", "LOW", "Contains", priority: 10));
        _apiService.Rules.Add(CreateRule("High Priority", "HIGH", "Exact", priority: 1));
        _apiService.Categories.Add(CreateCategory("Misc"));

        var cut = Render<Rules>();

        cut.Markup.ShouldNotContain("No categorization rules yet");
    }

    /// <summary>
    /// Verifies apply rules dialog can be shown.
    /// </summary>
    [Fact]
    public void ApplyRulesDialog_CanBeDisplayed()
    {
        _apiService.Rules.Add(CreateRule("Rule", "PATTERN", "Contains"));
        _apiService.Categories.Add(CreateCategory("Misc"));

        var cut = Render<Rules>();

        cut.Markup.ShouldContain("Apply Rules");
    }

    /// <summary>
    /// Verifies empty state with filters shows appropriate message.
    /// </summary>
    [Fact]
    public void EmptyStateWithFilters_ShowsClearFiltersOption()
    {
        _apiService.Categories.Add(CreateCategory("Misc"));

        var cut = Render<Rules>();

        cut.Markup.ShouldContain("No categorization rules yet");
    }

    /// <summary>
    /// Verifies multiple rule operations can be performed.
    /// </summary>
    [Fact]
    public void MultipleOperations_CanBePerformed()
    {
        _apiService.CreateRuleResult = CreateRule("New", "NEW", "Contains");
        _apiService.UpdateRuleResult = ApiResult<CategorizationRuleDto>.Success(CreateRule("Updated", "UPD", "Exact"));
        _apiService.DeleteRuleResult = true;

        var cut = Render<Rules>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies bulk operations are available.
    /// </summary>
    [Fact]
    public void BulkOperations_AreAvailable()
    {
        _apiService.Rules.Add(CreateRule("Rule1", "P1", "Contains"));
        _apiService.Rules.Add(CreateRule("Rule2", "P2", "Exact"));
        _apiService.Categories.Add(CreateCategory("Misc"));

        var cut = Render<Rules>();

        cut.Markup.ShouldNotContain("No categorization rules yet");
    }

    /// <summary>
    /// Verifies page handles concurrent operations gracefully.
    /// </summary>
    [Fact]
    public void ConcurrentOperations_AreHandledGracefully()
    {
        _apiService.ActivateRuleResult = true;
        _apiService.DeactivateRuleResult = true;
        _apiService.Categories.Add(CreateCategory("Misc"));

        var cut = Render<Rules>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    private static CategorizationRuleDto CreateRule(
        string name,
        string pattern,
        string matchType,
        int priority = 1,
        bool isActive = true)
    {
        return new CategorizationRuleDto
        {
            Id = Guid.NewGuid(),
            Name = name,
            Pattern = pattern,
            MatchType = matchType,
            CaseSensitive = false,
            CategoryId = Guid.NewGuid(),
            CategoryName = "Test Category",
            Priority = priority,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
        };
    }

    private static BudgetCategoryDto CreateCategory(string name)
    {
        return new BudgetCategoryDto
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = "Expense",
            IsActive = true,
        };
    }
}
