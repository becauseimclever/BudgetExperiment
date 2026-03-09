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
            this._scopeService);
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

        /// <inheritdoc/>
        public IReadOnlyList<ToastItem> Toasts { get; } = [];

        /// <inheritdoc/>
        public void ShowSuccess(string message, string? title = null)
        {
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
