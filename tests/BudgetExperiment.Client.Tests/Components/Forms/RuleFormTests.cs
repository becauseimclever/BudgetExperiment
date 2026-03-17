// <copyright file="RuleFormTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Forms;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Components.Forms;

/// <summary>
/// Unit tests for the <see cref="RuleForm"/> component.
/// </summary>
public sealed class RuleFormTests : BunitContext, IAsyncLifetime
{
    private readonly Guid _testCategoryId = Guid.NewGuid();

    /// <summary>
    /// Initializes a new instance of the <see cref="RuleFormTests"/> class.
    /// </summary>
    public RuleFormTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ThemeService>();
        Services.AddSingleton<CultureService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies that the form renders all expected fields.
    /// </summary>
    [Fact]
    public void Render_ShowsAllFormFields()
    {
        // Act
        var cut = RenderRuleForm();

        // Assert
        Assert.NotNull(cut.Find("#ruleName"));
        Assert.NotNull(cut.Find("#rulePattern"));
        Assert.NotNull(cut.Find("#matchType"));
        Assert.NotNull(cut.Find("#caseSensitive"));
        Assert.NotNull(cut.Find("#categoryId"));
    }

    /// <summary>
    /// Verifies that submitting without a category selected does not invoke OnSubmit.
    /// </summary>
    [Fact]
    public void Submit_WithoutCategory_DoesNotInvokeOnSubmit()
    {
        // Arrange
        var submitCalled = false;
        var cut = RenderRuleForm(onSubmit: () => submitCalled = true);

        cut.Find("#ruleName").Input("Test Rule");
        cut.Find("#rulePattern").Input("WALMART");

        // Leave category unselected (default Guid.Empty)

        // Act
        cut.Find("form").Submit();

        // Assert
        Assert.False(submitCalled);
    }

    /// <summary>
    /// Verifies that submitting with a valid category invokes OnSubmit.
    /// </summary>
    [Fact]
    public void Submit_WithValidCategory_InvokesOnSubmit()
    {
        // Arrange
        var submitCalled = false;
        var cut = RenderRuleForm(onSubmit: () => submitCalled = true);

        cut.Find("#ruleName").Input("Test Rule");
        cut.Find("#rulePattern").Input("WALMART");
        cut.Find("#categoryId").Change(_testCategoryId.ToString());

        // Act
        cut.Find("form").Submit();

        // Assert
        Assert.True(submitCalled);
    }

    /// <summary>
    /// Verifies that the match type dropdown has all five options.
    /// </summary>
    [Fact]
    public void Render_MatchTypeDropdown_HasFiveOptions()
    {
        // Act
        var cut = RenderRuleForm();

        // Assert
        var options = cut.Find("#matchType").QuerySelectorAll("option");
        Assert.Equal(5, options.Length);
        Assert.Equal("Contains", options[0].GetAttribute("value"));
        Assert.Equal("Exact", options[1].GetAttribute("value"));
        Assert.Equal("StartsWith", options[2].GetAttribute("value"));
        Assert.Equal("EndsWith", options[3].GetAttribute("value"));
        Assert.Equal("Regex", options[4].GetAttribute("value"));
    }

    /// <summary>
    /// Verifies that the Test Pattern button is shown when pattern is non-empty and OnTestPattern has delegate.
    /// </summary>
    [Fact]
    public void Render_WithPatternAndTestDelegate_ShowsTestButton()
    {
        // Arrange
        var model = new CategorizationRuleCreateDto { Pattern = "WAL" };

        // Act
        var cut = Render<RuleForm>(p => p
            .Add(x => x.Model, model)
            .Add(x => x.Categories, CreateTestCategories())
            .Add(x => x.OnTestPattern, (TestPatternRequest _) => { }));

        // Assert
        Assert.Contains("Test Pattern", cut.Markup);
    }

    /// <summary>
    /// Verifies that the Test Pattern button is hidden when pattern is empty.
    /// </summary>
    [Fact]
    public void Render_WithEmptyPattern_HidesTestButton()
    {
        // Arrange
        var model = new CategorizationRuleCreateDto { Pattern = string.Empty };

        // Act
        var cut = Render<RuleForm>(p => p
            .Add(x => x.Model, model)
            .Add(x => x.Categories, CreateTestCategories())
            .Add(x => x.OnTestPattern, (TestPatternRequest _) => { }));

        // Assert
        Assert.DoesNotContain("Test Pattern", cut.Markup);
    }

    /// <summary>
    /// Verifies that SelectedCategoryId converts string to Guid correctly on the model.
    /// </summary>
    [Fact]
    public void CategorySelect_SettingValue_UpdatesModelCategoryId()
    {
        // Arrange
        var model = new CategorizationRuleCreateDto();
        var cut = Render<RuleForm>(p => p
            .Add(x => x.Model, model)
            .Add(x => x.Categories, CreateTestCategories()));

        // Act
        cut.Find("#categoryId").Change(_testCategoryId.ToString());

        // Assert
        Assert.Equal(_testCategoryId, model.CategoryId);
    }

    /// <summary>
    /// Verifies that clicking cancel invokes OnCancel.
    /// </summary>
    [Fact]
    public void ClickCancel_InvokesOnCancel()
    {
        // Arrange
        var cancelCalled = false;
        var cut = Render<RuleForm>(p => p
            .Add(x => x.Model, new CategorizationRuleCreateDto())
            .Add(x => x.Categories, CreateTestCategories())
            .Add(x => x.OnCancel, () => cancelCalled = true));

        // Act
        var cancelBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Cancel"));
        cancelBtn.Click();

        // Assert
        Assert.True(cancelCalled);
    }

    /// <summary>
    /// Verifies that the default submit button text is "Create Rule".
    /// </summary>
    [Fact]
    public void Render_DefaultSubmitButtonText_IsCreateRule()
    {
        // Act
        var cut = RenderRuleForm();

        // Assert
        Assert.Contains("Create Rule", cut.Markup);
    }

    /// <summary>
    /// Verifies that test results are displayed when TestResult is provided.
    /// </summary>
    [Fact]
    public void Render_WithTestResult_ShowsMatchCount()
    {
        // Arrange
        var model = new CategorizationRuleCreateDto { Pattern = "WAL" };
        var testResult = new TestPatternResponse
        {
            MatchCount = 3,
            MatchingDescriptions = new List<string> { "WALMART STORE", "WALGREENS" },
        };

        // Act
        var cut = Render<RuleForm>(p => p
            .Add(x => x.Model, model)
            .Add(x => x.Categories, CreateTestCategories())
            .Add(x => x.OnTestPattern, (TestPatternRequest _) => { })
            .Add(x => x.TestResult, testResult));

        // Assert
        Assert.Contains("3", cut.Markup);
        Assert.Contains("matching transaction(s)", cut.Markup);
    }

    private IRenderedComponent<RuleForm> RenderRuleForm(
        Action? onSubmit = null)
    {
        return Render<RuleForm>(p => p
            .Add(x => x.Model, new CategorizationRuleCreateDto())
            .Add(x => x.Categories, CreateTestCategories())
            .Add(x => x.OnSubmit, onSubmit ?? (() => { })));
    }

    private IReadOnlyList<BudgetCategoryDto> CreateTestCategories()
    {
        return new List<BudgetCategoryDto>
        {
            new() { Id = _testCategoryId, Name = "Groceries", Type = "Expense", IsActive = true },
        };
    }
}
