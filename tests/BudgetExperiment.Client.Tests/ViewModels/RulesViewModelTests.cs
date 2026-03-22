// <copyright file="RulesViewModelTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Models;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Client.Tests.TestHelpers;
using BudgetExperiment.Client.ViewModels;
using BudgetExperiment.Contracts.Dtos;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Shouldly;

namespace BudgetExperiment.Client.Tests.ViewModels;

/// <summary>
/// Unit tests for <see cref="RulesViewModel"/>.
/// </summary>
public sealed class RulesViewModelTests : IDisposable
{
    private readonly StubBudgetApiService _apiService = new();
    private readonly StubToastService _toastService = new();
    private readonly ScopeService _scopeService;
    private readonly StubNavigationManager _navigationManager = new();
    private readonly StubApiErrorContext _apiErrorContext = new();
    private readonly RulesViewModel _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="RulesViewModelTests"/> class.
    /// </summary>
    public RulesViewModelTests()
    {
        this._scopeService = new ScopeService(new StubJSRuntime());
        this._sut = new RulesViewModel(
            this._apiService,
            this._toastService,
            this._navigationManager,
            this._scopeService,
            this._apiErrorContext,
            new StubJSRuntime());
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this._sut.Dispose();
    }

    // --- Initialization ---

    /// <summary>
    /// Verifies that InitializeAsync loads rules from the API.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_LoadsRules()
    {
        this._apiService.Rules.Add(CreateRule("Grocery Rule", "GROCERY", "Contains"));

        await this._sut.InitializeAsync();

        this._sut.Rules.Count.ShouldBe(1);
        this._sut.Rules[0].Name.ShouldBe("Grocery Rule");
    }

    /// <summary>
    /// Verifies that InitializeAsync loads categories from the API.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_LoadsCategories()
    {
        this._apiService.Categories.Add(CreateCategory("Groceries"));

        await this._sut.InitializeAsync();

        this._sut.Categories.Count.ShouldBe(1);
        this._sut.Categories[0].Name.ShouldBe("Groceries");
    }

    /// <summary>
    /// Verifies that InitializeAsync sets IsLoading to false after loading.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_SetsIsLoadingToFalse()
    {
        await this._sut.InitializeAsync();

        this._sut.IsLoading.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that InitializeAsync handles API failure gracefully.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_SetsErrorMessage_WhenApiFails()
    {
        this._apiService.GetRulesException = new HttpRequestException("Server error");

        await this._sut.InitializeAsync();

        this._sut.ErrorMessage.ShouldNotBeNull();
        this._sut.ErrorMessage!.ShouldContain("Failed to load data");
        this._sut.IsLoading.ShouldBeFalse();
    }

    // --- LoadDataAsync ---

    /// <summary>
    /// Verifies that LoadDataAsync clears the error message before loading.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task LoadDataAsync_ClearsErrorMessage()
    {
        this._apiService.GetRulesException = new HttpRequestException("fail");
        await this._sut.InitializeAsync();
        this._sut.ErrorMessage.ShouldNotBeNull();

        this._apiService.GetRulesException = null;
        await this._sut.LoadDataAsync();

        this._sut.ErrorMessage.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that LoadDataAsync sets IsLoading to false on completion.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task LoadDataAsync_SetsIsLoadingFalseAfterLoad()
    {
        await this._sut.LoadDataAsync();

        this._sut.IsLoading.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that LoadDataAsync sets IsLoading to false even on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task LoadDataAsync_SetsIsLoadingFalse_WhenApiFails()
    {
        this._apiService.GetRulesException = new HttpRequestException("fail");

        await this._sut.LoadDataAsync();

        this._sut.IsLoading.ShouldBeFalse();
    }

    // --- RetryLoadAsync ---

    /// <summary>
    /// Verifies that RetryLoadAsync reloads data successfully.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RetryLoadAsync_ReloadsData()
    {
        this._apiService.GetRulesException = new HttpRequestException("fail");
        await this._sut.InitializeAsync();
        this._sut.Rules.Count.ShouldBe(0);

        this._apiService.GetRulesException = null;
        this._apiService.Rules.Add(CreateRule("Rule1", "PATTERN", "Contains"));
        await this._sut.RetryLoadAsync();

        this._sut.Rules.Count.ShouldBe(1);
    }

    /// <summary>
    /// Verifies that RetryLoadAsync sets IsRetrying to false after completion.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RetryLoadAsync_SetsIsRetryingFalseAfterCompletion()
    {
        await this._sut.RetryLoadAsync();

        this._sut.IsRetrying.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that RetryLoadAsync invokes OnStateChanged.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RetryLoadAsync_NotifiesStateChanged()
    {
        var stateChangedCount = 0;
        this._sut.OnStateChanged = () => stateChangedCount++;

        await this._sut.RetryLoadAsync();

        stateChangedCount.ShouldBeGreaterThan(0);
    }

    // --- DismissError ---

    /// <summary>
    /// Verifies that DismissError clears the error message.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DismissError_ClearsErrorMessage()
    {
        this._apiService.GetRulesException = new HttpRequestException("fail");
        await this._sut.InitializeAsync();
        this._sut.ErrorMessage.ShouldNotBeNull();

        this._sut.DismissError();

        this._sut.ErrorMessage.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that DismissError notifies state change.
    /// </summary>
    [Fact]
    public void DismissError_NotifiesStateChanged()
    {
        var called = false;
        this._sut.OnStateChanged = () => called = true;

        this._sut.DismissError();

        called.ShouldBeTrue();
    }

    // --- NavigateToAiSuggestions ---

    /// <summary>
    /// Verifies that NavigateToAiSuggestions navigates to the correct URL.
    /// </summary>
    [Fact]
    public void NavigateToAiSuggestions_NavigatesToCorrectUrl()
    {
        this._sut.NavigateToAiSuggestions();

        this._navigationManager.LastNavigatedUri.ShouldBe("/ai/suggestions");
    }

    // --- ShowAddRule / HideAddRule ---

    /// <summary>
    /// Verifies that ShowAddRule opens the add form.
    /// </summary>
    [Fact]
    public void ShowAddRule_SetsShowAddFormTrue()
    {
        this._sut.ShowAddRule();

        this._sut.ShowAddForm.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that ShowAddRule resets newRule with default MatchType.
    /// </summary>
    [Fact]
    public void ShowAddRule_ResetsNewRule()
    {
        this._sut.ShowAddRule();

        this._sut.NewRule.ShouldNotBeNull();
        this._sut.NewRule.MatchType.ShouldBe("Contains");
    }

    /// <summary>
    /// Verifies that ShowAddRule clears previous test result.
    /// </summary>
    [Fact]
    public void ShowAddRule_ClearsTestResult()
    {
        this._sut.ShowAddRule();

        this._sut.TestResult.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that ShowAddRule notifies state change.
    /// </summary>
    [Fact]
    public void ShowAddRule_NotifiesStateChanged()
    {
        var called = false;
        this._sut.OnStateChanged = () => called = true;

        this._sut.ShowAddRule();

        called.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that HideAddRule closes the add form.
    /// </summary>
    [Fact]
    public void HideAddRule_SetsShowAddFormFalse()
    {
        this._sut.ShowAddRule();
        this._sut.HideAddRule();

        this._sut.ShowAddForm.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that HideAddRule clears test result.
    /// </summary>
    [Fact]
    public void HideAddRule_ClearsTestResult()
    {
        this._sut.HideAddRule();

        this._sut.TestResult.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that HideAddRule notifies state change.
    /// </summary>
    [Fact]
    public void HideAddRule_NotifiesStateChanged()
    {
        var called = false;
        this._sut.OnStateChanged = () => called = true;

        this._sut.HideAddRule();

        called.ShouldBeTrue();
    }

    // --- CreateRuleAsync ---

    /// <summary>
    /// Verifies that CreateRuleAsync adds the rule to the list on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateRuleAsync_AddsRuleToList_WhenSuccessful()
    {
        var newRule = CreateRule("Grocery Rule", "GROCERY", "Contains");
        this._apiService.CreateRuleResult = newRule;
        this._sut.ShowAddRule();

        await this._sut.CreateRuleAsync();

        this._sut.Rules.Count.ShouldBe(1);
        this._sut.Rules[0].Name.ShouldBe("Grocery Rule");
    }

    /// <summary>
    /// Verifies that CreateRuleAsync hides the add form on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateRuleAsync_HidesAddForm_WhenSuccessful()
    {
        this._apiService.CreateRuleResult = CreateRule("Rule", "PATTERN", "Contains");
        this._sut.ShowAddRule();

        await this._sut.CreateRuleAsync();

        this._sut.ShowAddForm.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that CreateRuleAsync sets error message when API returns null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateRuleAsync_SetsErrorMessage_WhenApiReturnsNull()
    {
        this._apiService.CreateRuleResult = null;

        await this._sut.CreateRuleAsync();

        this._sut.ErrorMessage.ShouldBe("Failed to create rule.");
    }

    /// <summary>
    /// Verifies that CreateRuleAsync sets error message when API throws.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateRuleAsync_SetsErrorMessage_WhenApiThrows()
    {
        this._apiService.CreateRuleException = new HttpRequestException("Network error");

        await this._sut.CreateRuleAsync();

        this._sut.ErrorMessage!.ShouldContain("Failed to create rule");
        this._sut.ErrorMessage!.ShouldContain("Network error");
    }

    /// <summary>
    /// Verifies that CreateRuleAsync sets IsSubmitting to false after completion.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateRuleAsync_SetsIsSubmittingFalse_AfterCompletion()
    {
        this._apiService.CreateRuleResult = CreateRule("Rule", "PATTERN", "Contains");

        await this._sut.CreateRuleAsync();

        this._sut.IsSubmitting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that CreateRuleAsync sets IsSubmitting to false on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateRuleAsync_SetsIsSubmittingFalse_WhenApiThrows()
    {
        this._apiService.CreateRuleException = new HttpRequestException("fail");

        await this._sut.CreateRuleAsync();

        this._sut.IsSubmitting.ShouldBeFalse();
    }

    // --- ShowEditRule / HideEditRule ---

    /// <summary>
    /// Verifies that ShowEditRule opens the edit form with correct data.
    /// </summary>
    [Fact]
    public void ShowEditRule_OpensEditForm()
    {
        var rule = CreateRule("Test Rule", "PATTERN", "Exact");

        this._sut.ShowEditRule(rule);

        this._sut.ShowEditForm.ShouldBeTrue();
        this._sut.EditingRuleId.ShouldBe(rule.Id);
        this._sut.EditRule.Name.ShouldBe("Test Rule");
        this._sut.EditRule.Pattern.ShouldBe("PATTERN");
        this._sut.EditRule.MatchType.ShouldBe("Exact");
    }

    /// <summary>
    /// Verifies that ShowEditRule stores the version for concurrency.
    /// </summary>
    [Fact]
    public void ShowEditRule_StoresVersion()
    {
        var rule = CreateRule("Test", "PATTERN", "Contains");
        rule.Version = "v123";

        this._sut.ShowEditRule(rule);

        this._sut.EditingVersion.ShouldBe("v123");
    }

    /// <summary>
    /// Verifies that ShowEditRule clears test result.
    /// </summary>
    [Fact]
    public void ShowEditRule_ClearsTestResult()
    {
        var rule = CreateRule("Test", "PATTERN", "Contains");

        this._sut.ShowEditRule(rule);

        this._sut.TestResult.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that ShowEditRule notifies state change.
    /// </summary>
    [Fact]
    public void ShowEditRule_NotifiesStateChanged()
    {
        var called = false;
        this._sut.OnStateChanged = () => called = true;
        var rule = CreateRule("Test", "PATTERN", "Contains");

        this._sut.ShowEditRule(rule);

        called.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that HideEditRule closes the edit form and clears EditingRuleId.
    /// </summary>
    [Fact]
    public void HideEditRule_ClosesEditForm()
    {
        var rule = CreateRule("Test", "PATTERN", "Contains");
        this._sut.ShowEditRule(rule);

        this._sut.HideEditRule();

        this._sut.ShowEditForm.ShouldBeFalse();
        this._sut.EditingRuleId.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that HideEditRule clears test result.
    /// </summary>
    [Fact]
    public void HideEditRule_ClearsTestResult()
    {
        this._sut.HideEditRule();

        this._sut.TestResult.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that HideEditRule notifies state change.
    /// </summary>
    [Fact]
    public void HideEditRule_NotifiesStateChanged()
    {
        var called = false;
        this._sut.OnStateChanged = () => called = true;

        this._sut.HideEditRule();

        called.ShouldBeTrue();
    }

    // --- UpdateRuleAsync ---

    /// <summary>
    /// Verifies that UpdateRuleAsync updates the rule in the list on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateRuleAsync_UpdatesRuleInList_WhenSuccessful()
    {
        var originalRule = CreateRule("Original", "ORIG", "Contains");
        await this.SetupWithRuleAsync(originalRule);

        var updatedRule = CreateRule("Updated", "UPDATED", "Exact");
        updatedRule.Id = originalRule.Id;
        this._apiService.UpdateRuleResult = ApiResult<CategorizationRuleDto>.Success(updatedRule);

        this._sut.ShowEditRule(originalRule);
        await this._sut.UpdateRuleAsync();

        this._sut.Rules[0].Name.ShouldBe("Updated");
    }

    /// <summary>
    /// Verifies that UpdateRuleAsync hides the edit form on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateRuleAsync_HidesEditForm_WhenSuccessful()
    {
        var rule = CreateRule("Rule", "PATTERN", "Contains");
        await this.SetupWithRuleAsync(rule);

        var updated = CreateRule("Updated", "PATTERN", "Contains");
        updated.Id = rule.Id;
        this._apiService.UpdateRuleResult = ApiResult<CategorizationRuleDto>.Success(updated);

        this._sut.ShowEditRule(rule);
        await this._sut.UpdateRuleAsync();

        this._sut.ShowEditForm.ShouldBeFalse();
        this._sut.EditingRuleId.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that UpdateRuleAsync handles conflict response.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateRuleAsync_HandlesConflict()
    {
        var rule = CreateRule("Rule", "PATTERN", "Contains");
        await this.SetupWithRuleAsync(rule);
        this._apiService.UpdateRuleResult = ApiResult<CategorizationRuleDto>.Conflict();

        this._sut.ShowEditRule(rule);
        await this._sut.UpdateRuleAsync();

        this._toastService.WarningShown.ShouldBeTrue();
        this._sut.ShowEditForm.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that UpdateRuleAsync sets error message when API returns failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateRuleAsync_SetsErrorMessage_WhenApiReturnsFailure()
    {
        var rule = CreateRule("Rule", "PATTERN", "Contains");
        await this.SetupWithRuleAsync(rule);
        this._apiService.UpdateRuleResult = ApiResult<CategorizationRuleDto>.Failure();

        this._sut.ShowEditRule(rule);
        await this._sut.UpdateRuleAsync();

        this._sut.ErrorMessage.ShouldBe("Failed to update rule.");
    }

    /// <summary>
    /// Verifies that UpdateRuleAsync sets error message when API throws.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateRuleAsync_SetsErrorMessage_WhenApiThrows()
    {
        var rule = CreateRule("Rule", "PATTERN", "Contains");
        await this.SetupWithRuleAsync(rule);
        this._apiService.UpdateRuleException = new HttpRequestException("Network error");

        this._sut.ShowEditRule(rule);
        await this._sut.UpdateRuleAsync();

        this._sut.ErrorMessage!.ShouldContain("Failed to update rule");
        this._sut.ErrorMessage!.ShouldContain("Network error");
    }

    /// <summary>
    /// Verifies that UpdateRuleAsync sets IsSubmitting to false after completion.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateRuleAsync_SetsIsSubmittingFalse_AfterCompletion()
    {
        var rule = CreateRule("Rule", "PATTERN", "Contains");
        await this.SetupWithRuleAsync(rule);
        var updated = CreateRule("Updated", "PATTERN", "Contains");
        updated.Id = rule.Id;
        this._apiService.UpdateRuleResult = ApiResult<CategorizationRuleDto>.Success(updated);

        this._sut.ShowEditRule(rule);
        await this._sut.UpdateRuleAsync();

        this._sut.IsSubmitting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that UpdateRuleAsync returns early when no rule is being edited.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateRuleAsync_ReturnsEarly_WhenNoEditingRuleId()
    {
        await this._sut.UpdateRuleAsync();

        this._sut.IsSubmitting.ShouldBeFalse();
        this._sut.ErrorMessage.ShouldBeNull();
    }

    // --- ConfirmDeleteRule / CancelDelete ---

    /// <summary>
    /// Verifies that ConfirmDeleteRule shows the confirmation dialog.
    /// </summary>
    [Fact]
    public void ConfirmDeleteRule_ShowsConfirmDialog()
    {
        var rule = CreateRule("ToDelete", "PATTERN", "Contains");

        this._sut.ConfirmDeleteRule(rule);

        this._sut.ShowDeleteConfirm.ShouldBeTrue();
        this._sut.DeletingRule.ShouldBe(rule);
    }

    /// <summary>
    /// Verifies that ConfirmDeleteRule notifies state change.
    /// </summary>
    [Fact]
    public void ConfirmDeleteRule_NotifiesStateChanged()
    {
        var called = false;
        this._sut.OnStateChanged = () => called = true;
        var rule = CreateRule("Test", "PATTERN", "Contains");

        this._sut.ConfirmDeleteRule(rule);

        called.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that CancelDelete hides the confirmation dialog.
    /// </summary>
    [Fact]
    public void CancelDelete_HidesConfirmDialog()
    {
        var rule = CreateRule("ToDelete", "PATTERN", "Contains");
        this._sut.ConfirmDeleteRule(rule);

        this._sut.CancelDelete();

        this._sut.ShowDeleteConfirm.ShouldBeFalse();
        this._sut.DeletingRule.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that CancelDelete notifies state change.
    /// </summary>
    [Fact]
    public void CancelDelete_NotifiesStateChanged()
    {
        var called = false;
        this._sut.OnStateChanged = () => called = true;

        this._sut.CancelDelete();

        called.ShouldBeTrue();
    }

    // --- DeleteRuleAsync ---

    /// <summary>
    /// Verifies that DeleteRuleAsync removes the rule from the list on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteRuleAsync_RemovesRule_WhenSuccessful()
    {
        var rule = CreateRule("ToDelete", "PATTERN", "Contains");
        await this.SetupWithRuleAsync(rule);
        this._apiService.DeleteRuleResult = true;
        this._sut.ConfirmDeleteRule(rule);

        await this._sut.DeleteRuleAsync();

        this._sut.Rules.Count.ShouldBe(0);
        this._sut.ShowDeleteConfirm.ShouldBeFalse();
        this._sut.DeletingRule.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that DeleteRuleAsync sets error message when API returns false.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteRuleAsync_SetsErrorMessage_WhenApiFails()
    {
        var rule = CreateRule("Rule", "PATTERN", "Contains");
        await this.SetupWithRuleAsync(rule);
        this._apiService.DeleteRuleResult = false;
        this._sut.ConfirmDeleteRule(rule);

        await this._sut.DeleteRuleAsync();

        this._sut.ErrorMessage.ShouldBe("Failed to delete rule.");
    }

    /// <summary>
    /// Verifies that DeleteRuleAsync sets error message when API throws.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteRuleAsync_SetsErrorMessage_WhenApiThrows()
    {
        var rule = CreateRule("Rule", "PATTERN", "Contains");
        await this.SetupWithRuleAsync(rule);
        this._apiService.DeleteRuleException = new HttpRequestException("Server error");
        this._sut.ConfirmDeleteRule(rule);

        await this._sut.DeleteRuleAsync();

        this._sut.ErrorMessage!.ShouldContain("Failed to delete rule");
        this._sut.ErrorMessage!.ShouldContain("Server error");
    }

    /// <summary>
    /// Verifies that DeleteRuleAsync sets IsDeleting to false after completion.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteRuleAsync_SetsIsDeletingFalse_AfterCompletion()
    {
        var rule = CreateRule("Rule", "PATTERN", "Contains");
        this._sut.ConfirmDeleteRule(rule);
        this._apiService.DeleteRuleResult = true;

        await this._sut.DeleteRuleAsync();

        this._sut.IsDeleting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that DeleteRuleAsync returns early when no rule is being deleted.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteRuleAsync_ReturnsEarly_WhenNoDeletingRule()
    {
        await this._sut.DeleteRuleAsync();

        this._sut.IsDeleting.ShouldBeFalse();
        this._sut.ErrorMessage.ShouldBeNull();
    }

    // --- ActivateRuleAsync ---

    /// <summary>
    /// Verifies that ActivateRuleAsync sets IsActive to true on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ActivateRuleAsync_SetsIsActiveTrue_WhenSuccessful()
    {
        var rule = CreateRule("Inactive Rule", "PATTERN", "Contains", isActive: false);
        await this.SetupWithRuleAsync(rule);
        this._apiService.ActivateRuleResult = true;

        await this._sut.ActivateRuleAsync(rule);

        this._sut.Rules[0].IsActive.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that ActivateRuleAsync does not change state when API returns false.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ActivateRuleAsync_DoesNotChange_WhenApiFails()
    {
        var rule = CreateRule("Inactive Rule", "PATTERN", "Contains", isActive: false);
        await this.SetupWithRuleAsync(rule);
        this._apiService.ActivateRuleResult = false;

        await this._sut.ActivateRuleAsync(rule);

        this._sut.Rules[0].IsActive.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that ActivateRuleAsync sets error message when API throws.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ActivateRuleAsync_SetsErrorMessage_WhenApiThrows()
    {
        var rule = CreateRule("Rule", "PATTERN", "Contains");
        this._apiService.ActivateRuleException = new HttpRequestException("Server error");

        await this._sut.ActivateRuleAsync(rule);

        this._sut.ErrorMessage!.ShouldContain("Failed to activate rule");
    }

    // --- DeactivateRuleAsync ---

    /// <summary>
    /// Verifies that DeactivateRuleAsync sets IsActive to false on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeactivateRuleAsync_SetsIsActiveFalse_WhenSuccessful()
    {
        var rule = CreateRule("Active Rule", "PATTERN", "Contains", isActive: true);
        await this.SetupWithRuleAsync(rule);
        this._apiService.DeactivateRuleResult = true;

        await this._sut.DeactivateRuleAsync(rule);

        this._sut.Rules[0].IsActive.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that DeactivateRuleAsync does not change state when API returns false.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeactivateRuleAsync_DoesNotChange_WhenApiFails()
    {
        var rule = CreateRule("Active Rule", "PATTERN", "Contains", isActive: true);
        await this.SetupWithRuleAsync(rule);
        this._apiService.DeactivateRuleResult = false;

        await this._sut.DeactivateRuleAsync(rule);

        this._sut.Rules[0].IsActive.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that DeactivateRuleAsync sets error message when API throws.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeactivateRuleAsync_SetsErrorMessage_WhenApiThrows()
    {
        var rule = CreateRule("Rule", "PATTERN", "Contains");
        this._apiService.DeactivateRuleException = new HttpRequestException("Server error");

        await this._sut.DeactivateRuleAsync(rule);

        this._sut.ErrorMessage!.ShouldContain("Failed to deactivate rule");
    }

    // --- TestPatternAsync ---

    /// <summary>
    /// Verifies that TestPatternAsync sets the test result on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TestPatternAsync_SetsTestResult_WhenSuccessful()
    {
        var response = new TestPatternResponse
        {
            MatchingDescriptions = new[] { "GROCERY STORE" },
            MatchCount = 1,
        };
        this._apiService.TestPatternResult = response;

        await this._sut.TestPatternAsync(new TestPatternRequest { Pattern = "GROCERY" });

        this._sut.TestResult.ShouldNotBeNull();
        this._sut.TestResult!.MatchCount.ShouldBe(1);
    }

    /// <summary>
    /// Verifies that TestPatternAsync sets IsTesting to false after completion.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TestPatternAsync_SetsIsTestingFalse_AfterCompletion()
    {
        this._apiService.TestPatternResult = new TestPatternResponse();

        await this._sut.TestPatternAsync(new TestPatternRequest { Pattern = "TEST" });

        this._sut.IsTesting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that TestPatternAsync clears previous TestResult before testing.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TestPatternAsync_ClearsPreviousResult()
    {
        this._apiService.TestPatternResult = new TestPatternResponse { MatchCount = 5 };
        await this._sut.TestPatternAsync(new TestPatternRequest { Pattern = "FIRST" });
        this._sut.TestResult.ShouldNotBeNull();

        this._apiService.TestPatternResult = new TestPatternResponse { MatchCount = 3 };
        await this._sut.TestPatternAsync(new TestPatternRequest { Pattern = "SECOND" });

        this._sut.TestResult!.MatchCount.ShouldBe(3);
    }

    /// <summary>
    /// Verifies that TestPatternAsync sets error message when API throws.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TestPatternAsync_SetsErrorMessage_WhenApiThrows()
    {
        this._apiService.TestPatternException = new HttpRequestException("Server error");

        await this._sut.TestPatternAsync(new TestPatternRequest { Pattern = "TEST" });

        this._sut.ErrorMessage!.ShouldContain("Failed to test pattern");
    }

    /// <summary>
    /// Verifies that TestPatternAsync sets IsTesting to false on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TestPatternAsync_SetsIsTestingFalse_WhenApiThrows()
    {
        this._apiService.TestPatternException = new HttpRequestException("fail");

        await this._sut.TestPatternAsync(new TestPatternRequest { Pattern = "TEST" });

        this._sut.IsTesting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that TestPatternAsync notifies state change.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TestPatternAsync_NotifiesStateChanged()
    {
        var stateChangedCount = 0;
        this._sut.OnStateChanged = () => stateChangedCount++;
        this._apiService.TestPatternResult = new TestPatternResponse();

        await this._sut.TestPatternAsync(new TestPatternRequest { Pattern = "TEST" });

        stateChangedCount.ShouldBeGreaterThan(0);
    }

    // --- ShowApplyRules / HideApplyRules ---

    /// <summary>
    /// Verifies that ShowApplyRules sets the dialog visible.
    /// </summary>
    [Fact]
    public void ShowApplyRules_SetsDialogVisible()
    {
        this._sut.ShowApplyRules();

        this._sut.ShowApplyRulesDialog.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that ShowApplyRules notifies state change.
    /// </summary>
    [Fact]
    public void ShowApplyRules_NotifiesStateChanged()
    {
        var called = false;
        this._sut.OnStateChanged = () => called = true;

        this._sut.ShowApplyRules();

        called.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that HideApplyRules hides the dialog.
    /// </summary>
    [Fact]
    public void HideApplyRules_HidesDialog()
    {
        this._sut.ShowApplyRules();
        this._sut.HideApplyRules();

        this._sut.ShowApplyRulesDialog.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that HideApplyRules notifies state change.
    /// </summary>
    [Fact]
    public void HideApplyRules_NotifiesStateChanged()
    {
        var called = false;
        this._sut.OnStateChanged = () => called = true;

        this._sut.HideApplyRules();

        called.ShouldBeTrue();
    }

    // --- OnRulesApplied ---

    /// <summary>
    /// Verifies that OnRulesApplied does not throw.
    /// </summary>
    [Fact]
    public void OnRulesApplied_DoesNotThrow()
    {
        var response = new ApplyRulesResponse { TotalProcessed = 10, Categorized = 5 };

        Should.NotThrow(() => this._sut.OnRulesApplied(response));
    }

    // --- CreateRuleFromTest ---

    /// <summary>
    /// Verifies that CreateRuleFromTest populates the new rule form.
    /// </summary>
    [Fact]
    public void CreateRuleFromTest_PopulatesNewRuleForm()
    {
        this._sut.CreateRuleFromTest(("GROCERY", "Contains", false));

        this._sut.NewRule.Pattern.ShouldBe("GROCERY");
        this._sut.NewRule.MatchType.ShouldBe("Contains");
        this._sut.NewRule.CaseSensitive.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that CreateRuleFromTest opens the add form.
    /// </summary>
    [Fact]
    public void CreateRuleFromTest_OpensAddForm()
    {
        this._sut.CreateRuleFromTest(("PATTERN", "Exact", true));

        this._sut.ShowAddForm.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that CreateRuleFromTest clears test result.
    /// </summary>
    [Fact]
    public void CreateRuleFromTest_ClearsTestResult()
    {
        this._sut.CreateRuleFromTest(("PATTERN", "Contains", false));

        this._sut.TestResult.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that CreateRuleFromTest notifies state change.
    /// </summary>
    [Fact]
    public void CreateRuleFromTest_NotifiesStateChanged()
    {
        var called = false;
        this._sut.OnStateChanged = () => called = true;

        this._sut.CreateRuleFromTest(("PATTERN", "Contains", false));

        called.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that CreateRuleFromTest handles case-sensitive flag.
    /// </summary>
    [Fact]
    public void CreateRuleFromTest_HandlesCaseSensitiveFlag()
    {
        this._sut.CreateRuleFromTest(("PATTERN", "Regex", true));

        this._sut.NewRule.CaseSensitive.ShouldBeTrue();
        this._sut.NewRule.MatchType.ShouldBe("Regex");
    }

    // --- Scope Change ---

    /// <summary>
    /// Verifies that scope change triggers data reload.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ScopeChanged_ReloadsData()
    {
        await this._sut.InitializeAsync();
        this._sut.Rules.Count.ShouldBe(0);

        this._apiService.Rules.Add(CreateRule("New Rule", "PATTERN", "Contains"));
        await this._scopeService.SetScopeAsync(BudgetExperiment.Shared.Budgeting.BudgetScope.Personal);

        // Allow async event handler to complete
        await Task.Delay(50);

        this._sut.Rules.Count.ShouldBe(1);
    }

    // --- Dispose ---

    /// <summary>
    /// Verifies that Dispose unsubscribes from scope change events.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Dispose_UnsubscribesFromScopeChanged()
    {
        await this._sut.InitializeAsync();
        this._sut.Dispose();

        // After dispose, changing scope should not trigger a reload
        this._apiService.Rules.Add(CreateRule("New Rule", "PATTERN", "Contains"));
        await this._scopeService.SetScopeAsync(BudgetExperiment.Shared.Budgeting.BudgetScope.Personal);
        await Task.Delay(50);

        // Rules list should still be empty (from initial load) since dispose unsubscribed
        this._sut.Rules.Count.ShouldBe(0);
    }

    // --- OnStateChanged callback ---

    /// <summary>
    /// Verifies that OnStateChanged is invoked when state-mutating methods are called.
    /// </summary>
    [Fact]
    public void OnStateChanged_InvokedOnDismissError()
    {
        var called = false;
        this._sut.OnStateChanged = () => called = true;

        this._sut.DismissError();

        called.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that OnStateChanged is invoked during ShowAddRule.
    /// </summary>
    [Fact]
    public void OnStateChanged_InvokedOnShowAddRule()
    {
        var called = false;
        this._sut.OnStateChanged = () => called = true;

        this._sut.ShowAddRule();

        called.ShouldBeTrue();
    }

    // --- Pagination ---

    /// <summary>
    /// Verifies that InitializeAsync sets pagination properties from the paged response.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_SetsPaginationProperties()
    {
        this._apiService.Rules.Add(CreateRule("Rule1", "P1", "Contains"));
        this._apiService.Rules.Add(CreateRule("Rule2", "P2", "Contains"));

        await this._sut.InitializeAsync();

        this._sut.CurrentPage.ShouldBe(1);
        this._sut.PageSize.ShouldBe(25);
        this._sut.TotalCount.ShouldBe(2);
    }

    /// <summary>
    /// Verifies ChangePageAsync updates CurrentPage and reloads data.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ChangePageAsync_UpdatesCurrentPage()
    {
        await this._sut.InitializeAsync();

        await this._sut.ChangePageAsync(3);

        this._sut.CurrentPage.ShouldBe(3);
    }

    /// <summary>
    /// Verifies ChangePageAsync invokes OnStateChanged.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ChangePageAsync_NotifiesStateChanged()
    {
        await this._sut.InitializeAsync();
        var called = false;
        this._sut.OnStateChanged = () => called = true;

        await this._sut.ChangePageAsync(2);

        called.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies ChangePageSizeAsync updates PageSize and resets to page 1.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ChangePageSizeAsync_UpdatesPageSizeAndResetsPage()
    {
        await this._sut.InitializeAsync();
        await this._sut.ChangePageAsync(3);
        this._sut.CurrentPage.ShouldBe(3);

        await this._sut.ChangePageSizeAsync(50);

        this._sut.PageSize.ShouldBe(50);
        this._sut.CurrentPage.ShouldBe(1);
    }

    /// <summary>
    /// Verifies ChangePageSizeAsync invokes OnStateChanged.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ChangePageSizeAsync_NotifiesStateChanged()
    {
        await this._sut.InitializeAsync();
        var called = false;
        this._sut.OnStateChanged = () => called = true;

        await this._sut.ChangePageSizeAsync(10);

        called.ShouldBeTrue();
    }

    // --- Filter Methods ---

    /// <summary>
    /// Verifies that SetSearchAsync updates SearchText.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SetSearchAsync_UpdatesSearchText()
    {
        await this._sut.SetSearchAsync("grocery");

        this._sut.SearchText.ShouldBe("grocery");
    }

    /// <summary>
    /// Verifies that SetSearchAsync resets page to 1 after debounce.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SetSearchAsync_ResetsPageToOne()
    {
        await this._sut.InitializeAsync();
        await this._sut.ChangePageAsync(3);
        this._sut.CurrentPage.ShouldBe(3);

        await this._sut.SetSearchAsync("test");

        this._sut.CurrentPage.ShouldBe(1);
    }

    /// <summary>
    /// Verifies that SetSearchAsync notifies state changed.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SetSearchAsync_NotifiesStateChanged()
    {
        var called = false;
        this._sut.OnStateChanged = () => called = true;

        await this._sut.SetSearchAsync("test");

        called.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that SetCategoryFilterAsync updates SelectedCategoryId.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SetCategoryFilterAsync_UpdatesCategoryId()
    {
        var categoryId = Guid.NewGuid();

        await this._sut.SetCategoryFilterAsync(categoryId);

        this._sut.SelectedCategoryId.ShouldBe(categoryId);
    }

    /// <summary>
    /// Verifies that SetCategoryFilterAsync resets page to 1.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SetCategoryFilterAsync_ResetsPageToOne()
    {
        await this._sut.InitializeAsync();
        await this._sut.ChangePageAsync(3);

        await this._sut.SetCategoryFilterAsync(Guid.NewGuid());

        this._sut.CurrentPage.ShouldBe(1);
    }

    /// <summary>
    /// Verifies that SetCategoryFilterAsync notifies state changed.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SetCategoryFilterAsync_NotifiesStateChanged()
    {
        var called = false;
        this._sut.OnStateChanged = () => called = true;

        await this._sut.SetCategoryFilterAsync(Guid.NewGuid());

        called.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that SetStatusFilterAsync updates SelectedStatus.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SetStatusFilterAsync_UpdatesSelectedStatus()
    {
        await this._sut.SetStatusFilterAsync("Active");

        this._sut.SelectedStatus.ShouldBe("Active");
    }

    /// <summary>
    /// Verifies that SetStatusFilterAsync resets page to 1.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SetStatusFilterAsync_ResetsPageToOne()
    {
        await this._sut.InitializeAsync();
        await this._sut.ChangePageAsync(3);

        await this._sut.SetStatusFilterAsync("Active");

        this._sut.CurrentPage.ShouldBe(1);
    }

    /// <summary>
    /// Verifies that SetStatusFilterAsync notifies state changed.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SetStatusFilterAsync_NotifiesStateChanged()
    {
        var called = false;
        this._sut.OnStateChanged = () => called = true;

        await this._sut.SetStatusFilterAsync("Inactive");

        called.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that ClearFiltersAsync resets all filter state.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ClearFiltersAsync_ResetsAllFilters()
    {
        await this._sut.SetCategoryFilterAsync(Guid.NewGuid());
        await this._sut.SetStatusFilterAsync("Active");

        // Directly set search to avoid debounce issues in test
        await this._sut.SetSearchAsync("test");

        await this._sut.ClearFiltersAsync();

        this._sut.SearchText.ShouldBeNull();
        this._sut.SelectedCategoryId.ShouldBeNull();
        this._sut.SelectedStatus.ShouldBeNull();
        this._sut.CurrentPage.ShouldBe(1);
    }

    /// <summary>
    /// Verifies that ClearFiltersAsync notifies state changed.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ClearFiltersAsync_NotifiesStateChanged()
    {
        var called = false;
        this._sut.OnStateChanged = () => called = true;

        await this._sut.ClearFiltersAsync();

        called.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that HasActiveFilters is false when no filters are set.
    /// </summary>
    [Fact]
    public void HasActiveFilters_FalseWhenNoFilters()
    {
        this._sut.HasActiveFilters.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that HasActiveFilters is true when search text is set.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HasActiveFilters_TrueWhenSearchSet()
    {
        await this._sut.SetSearchAsync("test");

        this._sut.HasActiveFilters.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that HasActiveFilters is true when category is set.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HasActiveFilters_TrueWhenCategorySet()
    {
        await this._sut.SetCategoryFilterAsync(Guid.NewGuid());

        this._sut.HasActiveFilters.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that HasActiveFilters is true when status is set.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HasActiveFilters_TrueWhenStatusSet()
    {
        await this._sut.SetStatusFilterAsync("Active");

        this._sut.HasActiveFilters.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that ActiveFilterCount returns the correct count.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ActiveFilterCount_ReturnsCorrectCount()
    {
        this._sut.ActiveFilterCount.ShouldBe(0);

        await this._sut.SetCategoryFilterAsync(Guid.NewGuid());
        this._sut.ActiveFilterCount.ShouldBe(1);

        await this._sut.SetStatusFilterAsync("Active");
        this._sut.ActiveFilterCount.ShouldBe(2);

        await this._sut.SetSearchAsync("test");
        this._sut.ActiveFilterCount.ShouldBe(3);
    }

    // --- Group by Category ---

    // --- Sort ---

    /// <summary>
    /// Verifies that SortBy and SortDirection default to null.
    /// </summary>
    [Fact]
    public void Sort_DefaultsToNull()
    {
        this._sut.SortBy.ShouldBeNull();
        this._sut.SortDirection.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that ToggleSortAsync sets SortBy and defaults to ascending for a new field.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ToggleSortAsync_SetsSortField_WhenNewField()
    {
        await this._sut.ToggleSortAsync("name");

        this._sut.SortBy.ShouldBe("name");
        this._sut.SortDirection.ShouldBe("asc");
    }

    /// <summary>
    /// Verifies that ToggleSortAsync toggles direction when sorting by the same field.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ToggleSortAsync_TogglesDirection_WhenSameField()
    {
        await this._sut.ToggleSortAsync("name");
        this._sut.SortDirection.ShouldBe("asc");

        await this._sut.ToggleSortAsync("name");
        this._sut.SortDirection.ShouldBe("desc");

        await this._sut.ToggleSortAsync("name");
        this._sut.SortDirection.ShouldBe("asc");
    }

    /// <summary>
    /// Verifies that ToggleSortAsync resets direction when switching to a different field.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ToggleSortAsync_ResetsDirection_WhenDifferentField()
    {
        await this._sut.ToggleSortAsync("name");
        await this._sut.ToggleSortAsync("name");
        this._sut.SortDirection.ShouldBe("desc");

        await this._sut.ToggleSortAsync("priority");
        this._sut.SortBy.ShouldBe("priority");
        this._sut.SortDirection.ShouldBe("asc");
    }

    /// <summary>
    /// Verifies that ToggleSortAsync resets page to 1.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ToggleSortAsync_ResetsPageToOne()
    {
        await this._sut.InitializeAsync();
        await this._sut.ChangePageAsync(3);
        this._sut.CurrentPage.ShouldBe(3);

        await this._sut.ToggleSortAsync("name");

        this._sut.CurrentPage.ShouldBe(1);
    }

    /// <summary>
    /// Verifies that ToggleSortAsync notifies state changed.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ToggleSortAsync_NotifiesStateChanged()
    {
        var called = false;
        this._sut.OnStateChanged = () => called = true;

        await this._sut.ToggleSortAsync("name");

        called.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that ClearFiltersAsync also resets sort state.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ClearFiltersAsync_ResetsSort()
    {
        await this._sut.ToggleSortAsync("name");
        this._sut.SortBy.ShouldNotBeNull();

        await this._sut.ClearFiltersAsync();

        this._sut.SortBy.ShouldBeNull();
        this._sut.SortDirection.ShouldBeNull();
    }

    // --- Group by Category (continued) ---

    /// <summary>
    /// Verifies that IsGroupedByCategory is false by default.
    /// </summary>
    [Fact]
    public void IsGroupedByCategory_DefaultsFalse()
    {
        this._sut.IsGroupedByCategory.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that ToggleGroupByCategory toggles the grouping state.
    /// </summary>
    [Fact]
    public void ToggleGroupByCategory_TogglesState()
    {
        this._sut.ToggleGroupByCategory();
        this._sut.IsGroupedByCategory.ShouldBeTrue();

        this._sut.ToggleGroupByCategory();
        this._sut.IsGroupedByCategory.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that ToggleGroupByCategory notifies state changed.
    /// </summary>
    [Fact]
    public void ToggleGroupByCategory_NotifiesStateChanged()
    {
        var called = false;
        this._sut.OnStateChanged = () => called = true;

        this._sut.ToggleGroupByCategory();

        called.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that GroupedRules groups rules by CategoryName.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GroupedRules_GroupsByCategoryName()
    {
        var groceryId = Guid.NewGuid();
        var utilityId = Guid.NewGuid();
        this._apiService.Rules.Add(CreateRuleWithCategory("Grocery Rule", "GROCERY", "Contains", groceryId, "Groceries"));
        this._apiService.Rules.Add(CreateRuleWithCategory("Utility Rule", "ELECTRIC", "Contains", utilityId, "Utilities"));
        this._apiService.Rules.Add(CreateRuleWithCategory("Grocery Rule 2", "FOOD", "Contains", groceryId, "Groceries"));

        await this._sut.InitializeAsync();
        this._sut.ToggleGroupByCategory();

        var groups = this._sut.GroupedRules;
        groups.Count.ShouldBe(2);
        groups.ShouldContainKey("Groceries");
        groups.ShouldContainKey("Utilities");
        groups["Groceries"].Count.ShouldBe(2);
        groups["Utilities"].Count.ShouldBe(1);
    }

    /// <summary>
    /// Verifies that GroupedRules sorts rules within each group by priority.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GroupedRules_SortsWithinGroupByPriority()
    {
        var catId = Guid.NewGuid();
        this._apiService.Rules.Add(CreateRuleWithCategory("Low Priority", "LOW", "Contains", catId, "Groceries", priority: 10));
        this._apiService.Rules.Add(CreateRuleWithCategory("High Priority", "HIGH", "Contains", catId, "Groceries", priority: 1));

        await this._sut.InitializeAsync();
        this._sut.ToggleGroupByCategory();

        var group = this._sut.GroupedRules["Groceries"];
        group[0].Name.ShouldBe("High Priority");
        group[1].Name.ShouldBe("Low Priority");
    }

    /// <summary>
    /// Verifies that GroupedRules returns empty when not grouped.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GroupedRules_ReturnsEmpty_WhenNotGrouped()
    {
        this._apiService.Rules.Add(CreateRule("Rule1", "PATTERN", "Contains"));
        await this._sut.InitializeAsync();

        this._sut.GroupedRules.Count.ShouldBe(0);
    }

    /// <summary>
    /// Verifies that GroupedRules handles rules with null CategoryName.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GroupedRules_HandlesNullCategoryName()
    {
        this._apiService.Rules.Add(CreateRuleWithCategory("Rule1", "PATTERN", "Contains", Guid.NewGuid(), null));
        await this._sut.InitializeAsync();
        this._sut.ToggleGroupByCategory();

        var groups = this._sut.GroupedRules;
        groups.Count.ShouldBe(1);
        groups.ShouldContainKey("Unknown");
    }

    /// <summary>
    /// Verifies that collapsed category state is tracked.
    /// </summary>
    [Fact]
    public void ToggleCategoryCollapse_TracksCollapsedState()
    {
        this._sut.ToggleCategoryCollapse("Groceries");
        this._sut.IsCategoryCollapsed("Groceries").ShouldBeTrue();

        this._sut.ToggleCategoryCollapse("Groceries");
        this._sut.IsCategoryCollapsed("Groceries").ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that categories are expanded by default.
    /// </summary>
    [Fact]
    public void IsCategoryCollapsed_DefaultsFalse()
    {
        this._sut.IsCategoryCollapsed("AnyCategory").ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that ToggleCategoryCollapse notifies state changed.
    /// </summary>
    [Fact]
    public void ToggleCategoryCollapse_NotifiesStateChanged()
    {
        var called = false;
        this._sut.OnStateChanged = () => called = true;

        this._sut.ToggleCategoryCollapse("Groceries");

        called.ShouldBeTrue();
    }

    // --- Selection ---

    /// <summary>
    /// Verifies that no rules are selected by default.
    /// </summary>
    [Fact]
    public void Selection_DefaultsToEmpty()
    {
        this._sut.SelectedCount.ShouldBe(0);
        this._sut.HasSelection.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that ToggleRuleSelection adds a rule to selection.
    /// </summary>
    [Fact]
    public void ToggleRuleSelection_AddsRule()
    {
        var id = Guid.NewGuid();
        this._sut.ToggleRuleSelection(id);

        this._sut.SelectedCount.ShouldBe(1);
        this._sut.IsRuleSelected(id).ShouldBeTrue();
        this._sut.HasSelection.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that ToggleRuleSelection removes a previously selected rule.
    /// </summary>
    [Fact]
    public void ToggleRuleSelection_RemovesWhenAlreadySelected()
    {
        var id = Guid.NewGuid();
        this._sut.ToggleRuleSelection(id);
        this._sut.ToggleRuleSelection(id);

        this._sut.SelectedCount.ShouldBe(0);
        this._sut.IsRuleSelected(id).ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that ToggleRuleSelection notifies state changed.
    /// </summary>
    [Fact]
    public void ToggleRuleSelection_NotifiesStateChanged()
    {
        var called = false;
        this._sut.OnStateChanged = () => called = true;

        this._sut.ToggleRuleSelection(Guid.NewGuid());

        called.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that SelectAllOnPage selects all rules on the current page.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SelectAllOnPage_SelectsAllCurrentRules()
    {
        var rule1 = CreateRule("R1", "p1", "Contains");
        var rule2 = CreateRule("R2", "p2", "Contains");
        this._apiService.Rules.Add(rule1);
        this._apiService.Rules.Add(rule2);
        await this._sut.InitializeAsync();

        this._sut.SelectAllOnPage();

        this._sut.SelectedCount.ShouldBe(2);
        this._sut.IsRuleSelected(rule1.Id).ShouldBeTrue();
        this._sut.IsRuleSelected(rule2.Id).ShouldBeTrue();
        this._sut.AreAllOnPageSelected.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that DeselectAllOnPage removes all current page rules from selection.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeselectAllOnPage_RemovesCurrentPageFromSelection()
    {
        var rule1 = CreateRule("R1", "p1", "Contains");
        this._apiService.Rules.Add(rule1);
        await this._sut.InitializeAsync();

        this._sut.SelectAllOnPage();
        this._sut.DeselectAllOnPage();

        this._sut.SelectedCount.ShouldBe(0);
        this._sut.AreAllOnPageSelected.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that ToggleSelectAllOnPage selects all when not all selected.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ToggleSelectAllOnPage_SelectsAll_WhenNotAllSelected()
    {
        var rule1 = CreateRule("R1", "p1", "Contains");
        var rule2 = CreateRule("R2", "p2", "Contains");
        this._apiService.Rules.Add(rule1);
        this._apiService.Rules.Add(rule2);
        await this._sut.InitializeAsync();

        this._sut.ToggleRuleSelection(rule1.Id);
        this._sut.ToggleSelectAllOnPage();

        this._sut.AreAllOnPageSelected.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that ToggleSelectAllOnPage deselects all when all are selected.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ToggleSelectAllOnPage_DeselectsAll_WhenAllSelected()
    {
        var rule1 = CreateRule("R1", "p1", "Contains");
        this._apiService.Rules.Add(rule1);
        await this._sut.InitializeAsync();

        this._sut.SelectAllOnPage();
        this._sut.ToggleSelectAllOnPage();

        this._sut.AreAllOnPageSelected.ShouldBeFalse();
        this._sut.SelectedCount.ShouldBe(0);
    }

    /// <summary>
    /// Verifies that ClearSelection removes all selections.
    /// </summary>
    [Fact]
    public void ClearSelection_RemovesAll()
    {
        this._sut.ToggleRuleSelection(Guid.NewGuid());
        this._sut.ToggleRuleSelection(Guid.NewGuid());
        this._sut.ClearSelection();

        this._sut.SelectedCount.ShouldBe(0);
        this._sut.HasSelection.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that AreAllOnPageSelected returns false when no rules are loaded.
    /// </summary>
    [Fact]
    public void AreAllOnPageSelected_ReturnsFalse_WhenNoRules()
    {
        this._sut.AreAllOnPageSelected.ShouldBeFalse();
    }

    // --- Bulk Delete ---

    /// <summary>
    /// Verifies that ConfirmBulkDelete shows the confirmation dialog.
    /// </summary>
    [Fact]
    public void ConfirmBulkDelete_ShowsDialog()
    {
        this._sut.ConfirmBulkDelete();

        this._sut.ShowBulkDeleteConfirm.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that CancelBulkDelete hides the confirmation dialog.
    /// </summary>
    [Fact]
    public void CancelBulkDelete_HidesDialog()
    {
        this._sut.ConfirmBulkDelete();
        this._sut.CancelBulkDelete();

        this._sut.ShowBulkDeleteConfirm.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that BulkDeleteAsync does nothing when no rules are selected.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task BulkDeleteAsync_DoesNothing_WhenNoSelection()
    {
        await this._sut.BulkDeleteAsync();

        this._sut.ErrorMessage.ShouldBeNull();
        this._sut.IsBulkOperating.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that BulkDeleteAsync calls API and clears selection on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task BulkDeleteAsync_ClearsSelection_OnSuccess()
    {
        var rule = CreateRule("R1", "p1", "Contains");
        this._apiService.Rules.Add(rule);
        this._apiService.BulkDeleteResult = new BulkRuleActionResponse { AffectedCount = 1 };
        await this._sut.InitializeAsync();

        this._sut.ToggleRuleSelection(rule.Id);
        await this._sut.BulkDeleteAsync();

        this._sut.SelectedCount.ShouldBe(0);
        this._sut.IsBulkOperating.ShouldBeFalse();
        this._sut.ShowBulkDeleteConfirm.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that BulkDeleteAsync shows toast on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task BulkDeleteAsync_ShowsToast_OnSuccess()
    {
        var rule = CreateRule("R1", "p1", "Contains");
        this._apiService.Rules.Add(rule);
        this._apiService.BulkDeleteResult = new BulkRuleActionResponse { AffectedCount = 1 };
        await this._sut.InitializeAsync();

        this._sut.ToggleRuleSelection(rule.Id);
        await this._sut.BulkDeleteAsync();

        this._toastService.LastSuccessMessage.ShouldBe("Deleted 1 rule(s).");
    }

    /// <summary>
    /// Verifies that BulkDeleteAsync sets error when API returns null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task BulkDeleteAsync_SetsError_WhenApiFails()
    {
        var rule = CreateRule("R1", "p1", "Contains");
        this._apiService.Rules.Add(rule);
        this._apiService.BulkDeleteResult = null;
        await this._sut.InitializeAsync();

        this._sut.ToggleRuleSelection(rule.Id);
        await this._sut.BulkDeleteAsync();

        this._sut.ErrorMessage.ShouldBe("Failed to bulk delete rules.");
    }

    /// <summary>
    /// Verifies that BulkDeleteAsync sets error on exception.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task BulkDeleteAsync_SetsError_OnException()
    {
        var rule = CreateRule("R1", "p1", "Contains");
        this._apiService.Rules.Add(rule);
        this._apiService.BulkDeleteException = new InvalidOperationException("Network error");
        await this._sut.InitializeAsync();

        this._sut.ToggleRuleSelection(rule.Id);
        await this._sut.BulkDeleteAsync();

        this._sut.ErrorMessage.ShouldNotBeNull();
        this._sut.ErrorMessage.ShouldContain("Network error");
    }

    // --- Bulk Activate ---

    /// <summary>
    /// Verifies that BulkActivateAsync clears selection and shows toast on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task BulkActivateAsync_ClearsSelection_OnSuccess()
    {
        var rule = CreateRule("R1", "p1", "Contains", isActive: false);
        this._apiService.Rules.Add(rule);
        this._apiService.BulkActivateResult = new BulkRuleActionResponse { AffectedCount = 1 };
        await this._sut.InitializeAsync();

        this._sut.ToggleRuleSelection(rule.Id);
        await this._sut.BulkActivateAsync();

        this._sut.SelectedCount.ShouldBe(0);
        this._sut.IsBulkOperating.ShouldBeFalse();
        this._toastService.LastSuccessMessage.ShouldBe("Activated 1 rule(s).");
    }

    /// <summary>
    /// Verifies that BulkActivateAsync sets error when API returns null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task BulkActivateAsync_SetsError_WhenApiFails()
    {
        var rule = CreateRule("R1", "p1", "Contains", isActive: false);
        this._apiService.Rules.Add(rule);
        this._apiService.BulkActivateResult = null;
        await this._sut.InitializeAsync();

        this._sut.ToggleRuleSelection(rule.Id);
        await this._sut.BulkActivateAsync();

        this._sut.ErrorMessage.ShouldBe("Failed to bulk activate rules.");
    }

    /// <summary>
    /// Verifies that BulkActivateAsync does nothing when no selection.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task BulkActivateAsync_DoesNothing_WhenNoSelection()
    {
        await this._sut.BulkActivateAsync();

        this._sut.ErrorMessage.ShouldBeNull();
    }

    // --- Bulk Deactivate ---

    /// <summary>
    /// Verifies that BulkDeactivateAsync clears selection and shows toast on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task BulkDeactivateAsync_ClearsSelection_OnSuccess()
    {
        var rule = CreateRule("R1", "p1", "Contains");
        this._apiService.Rules.Add(rule);
        this._apiService.BulkDeactivateResult = new BulkRuleActionResponse { AffectedCount = 1 };
        await this._sut.InitializeAsync();

        this._sut.ToggleRuleSelection(rule.Id);
        await this._sut.BulkDeactivateAsync();

        this._sut.SelectedCount.ShouldBe(0);
        this._sut.IsBulkOperating.ShouldBeFalse();
        this._toastService.LastSuccessMessage.ShouldBe("Deactivated 1 rule(s).");
    }

    /// <summary>
    /// Verifies that BulkDeactivateAsync sets error when API returns null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task BulkDeactivateAsync_SetsError_WhenApiFails()
    {
        var rule = CreateRule("R1", "p1", "Contains");
        this._apiService.Rules.Add(rule);
        this._apiService.BulkDeactivateResult = null;
        await this._sut.InitializeAsync();

        this._sut.ToggleRuleSelection(rule.Id);
        await this._sut.BulkDeactivateAsync();

        this._sut.ErrorMessage.ShouldBe("Failed to bulk deactivate rules.");
    }

    /// <summary>
    /// Verifies that BulkDeactivateAsync does nothing when no selection.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task BulkDeactivateAsync_DoesNothing_WhenNoSelection()
    {
        await this._sut.BulkDeactivateAsync();

        this._sut.ErrorMessage.ShouldBeNull();
    }

    // --- View Mode ---

    /// <summary>
    /// Verifies the default view mode is Table.
    /// </summary>
    [Fact]
    public void ViewMode_DefaultsToTable()
    {
        this._sut.ViewMode.ShouldBe(RulesViewMode.Table);
    }

    /// <summary>
    /// Verifies SetViewModeAsync changes the view mode and notifies state change.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SetViewModeAsync_ChangesViewMode()
    {
        var stateChangedCount = 0;
        this._sut.OnStateChanged = () => stateChangedCount++;

        await this._sut.SetViewModeAsync(RulesViewMode.Card);

        this._sut.ViewMode.ShouldBe(RulesViewMode.Card);
        stateChangedCount.ShouldBe(1);
    }

    /// <summary>
    /// Verifies SetViewModeAsync does nothing when mode is the same.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SetViewModeAsync_SameMode_DoesNotNotify()
    {
        var stateChangedCount = 0;
        this._sut.OnStateChanged = () => stateChangedCount++;

        await this._sut.SetViewModeAsync(RulesViewMode.Table);

        stateChangedCount.ShouldBe(0);
    }

    // --- Active Rule Count ---

    /// <summary>
    /// Verifies ActiveRuleCount returns the number of active rules on the current page.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ActiveRuleCount_ReturnsCountOfActiveRules()
    {
        this._apiService.Rules.Add(CreateRule("R1", "pat1", "Contains", isActive: true));
        this._apiService.Rules.Add(CreateRule("R2", "pat2", "Contains", isActive: false));
        this._apiService.Rules.Add(CreateRule("R3", "pat3", "Contains", isActive: true));

        await this._sut.InitializeAsync();

        this._sut.ActiveRuleCount.ShouldBe(2);
    }

    /// <summary>
    /// Verifies FilteredCount returns the number of rules on the current page.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task FilteredCount_ReturnsPageRuleCount()
    {
        this._apiService.Rules.Add(CreateRule("R1", "pat1", "Contains"));
        this._apiService.Rules.Add(CreateRule("R2", "pat2", "Contains"));

        await this._sut.InitializeAsync();

        this._sut.FilteredCount.ShouldBe(2);
    }

    // --- Preference Persistence ---

    /// <summary>
    /// Verifies that InitializeAsync loads view mode preference from localStorage.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_LoadsViewModePreference()
    {
        var jsRuntime = new ConfigurableJSRuntime();
        jsRuntime.SetStorageItem("budget-experiment-rules-view-mode", "Card");
        var sut = new RulesViewModel(
            this._apiService,
            this._toastService,
            this._navigationManager,
            this._scopeService,
            this._apiErrorContext,
            jsRuntime);

        await sut.InitializeAsync();

        sut.ViewMode.ShouldBe(RulesViewMode.Card);
        sut.Dispose();
    }

    /// <summary>
    /// Verifies that InitializeAsync loads page size preference from localStorage.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_LoadsPageSizePreference()
    {
        var jsRuntime = new ConfigurableJSRuntime();
        jsRuntime.SetStorageItem("budget-experiment-rules-page-size", "50");
        var sut = new RulesViewModel(
            this._apiService,
            this._toastService,
            this._navigationManager,
            this._scopeService,
            this._apiErrorContext,
            jsRuntime);

        await sut.InitializeAsync();

        sut.PageSize.ShouldBe(50);
        sut.Dispose();
    }

    /// <summary>
    /// Verifies that an invalid page size from localStorage is ignored.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_InvalidPageSize_UsesDefault()
    {
        var jsRuntime = new ConfigurableJSRuntime();
        jsRuntime.SetStorageItem("budget-experiment-rules-page-size", "999");
        var sut = new RulesViewModel(
            this._apiService,
            this._toastService,
            this._navigationManager,
            this._scopeService,
            this._apiErrorContext,
            jsRuntime);

        await sut.InitializeAsync();

        sut.PageSize.ShouldBe(25);
        sut.Dispose();
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

    private static CategorizationRuleDto CreateRuleWithCategory(
        string name,
        string pattern,
        string matchType,
        Guid categoryId,
        string? categoryName,
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
            CategoryId = categoryId,
            CategoryName = categoryName,
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

    private async Task SetupWithRuleAsync(CategorizationRuleDto rule)
    {
        this._apiService.Rules.Add(rule);
        await this._sut.InitializeAsync();
    }

    /// <summary>
    /// Stub toast service for ViewModel testing.
    /// </summary>
    private sealed class StubToastService : IToastService
    {
        /// <inheritdoc/>
        public event Action? OnChange;

        /// <summary>
        /// Gets a value indicating whether a warning was shown.
        /// </summary>
        public bool WarningShown { get; private set; }

        /// <summary>
        /// Gets the last success message shown.
        /// </summary>
        public string? LastSuccessMessage { get; private set; }

        /// <inheritdoc/>
        public IReadOnlyList<ToastItem> Toasts { get; } = [];

        /// <inheritdoc/>
        public void ShowSuccess(string message, string? title = null)
        {
            this.LastSuccessMessage = message;
        }

        /// <inheritdoc/>
        public void ShowError(string message, string? title = null)
        {
        }

        /// <inheritdoc/>
        public void ShowInfo(string message, string? title = null)
        {
        }

        /// <inheritdoc/>
        public void ShowWarning(string message, string? title = null)
        {
            this.WarningShown = true;
            this.OnChange?.Invoke();
        }

        /// <inheritdoc/>
        public void Remove(Guid id)
        {
        }
    }

    /// <summary>
    /// Minimal stub for IJSRuntime to satisfy ScopeService constructor.
    /// </summary>
    private sealed class StubJSRuntime : IJSRuntime
    {
        /// <inheritdoc/>
        /// <returns>A default value.</returns>
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => default;

        /// <inheritdoc/>
        /// <returns>A default value.</returns>
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => default;
    }

    /// <summary>
    /// Configurable IJSRuntime that can return stored localStorage values.
    /// </summary>
    private sealed class ConfigurableJSRuntime : IJSRuntime
    {
        private readonly Dictionary<string, string> _storage = new(StringComparer.Ordinal);

        public void SetStorageItem(string key, string value) => this._storage[key] = value;

        /// <inheritdoc/>
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            if (identifier == "localStorage.getItem" && args?.Length > 0 && args[0] is string key && this._storage.TryGetValue(key, out var val))
            {
                return new ValueTask<TValue>((TValue)(object)val);
            }

            return default;
        }

        /// <inheritdoc/>
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            return this.InvokeAsync<TValue>(identifier, args);
        }
    }

    /// <summary>
    /// Stub NavigationManager for testing navigation calls.
    /// </summary>
    private sealed class StubNavigationManager : NavigationManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StubNavigationManager"/> class.
        /// </summary>
        public StubNavigationManager()
        {
            this.Initialize("https://localhost/", "https://localhost/");
        }

        /// <summary>
        /// Gets the last URI that was navigated to.
        /// </summary>
        public string? LastNavigatedUri { get; private set; }

        /// <inheritdoc/>
        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            this.LastNavigatedUri = uri;
        }
    }
}
