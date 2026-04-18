// <copyright file="ConsolidationSuggestionsViewModelTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Tests.TestHelpers;
using BudgetExperiment.Client.ViewModels;

using Shouldly;

namespace BudgetExperiment.Client.Tests.ViewModels;

/// <summary>
/// Unit tests for <see cref="ConsolidationSuggestionsViewModel"/>.
/// </summary>
public sealed class ConsolidationSuggestionsViewModelTests
{
    private readonly StubBudgetApiService _apiService = new();
    private readonly ConsolidationSuggestionsViewModel _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsolidationSuggestionsViewModelTests"/> class.
    /// </summary>
    public ConsolidationSuggestionsViewModelTests()
    {
        _sut = new ConsolidationSuggestionsViewModel(_apiService);
    }

    [Fact]
    public async Task InitializeAsync_SetsErrorMessage_WhenApiRequestFails()
    {
        _apiService.GetConsolidationSuggestionsException = new HttpRequestException("Server error");

        await _sut.InitializeAsync();

        _sut.ErrorMessage.ShouldBe("Failed to load suggestions: Server error");
        _sut.IsLoading.ShouldBeFalse();
    }

    [Fact]
    public async Task InitializeAsync_PropagatesCancellation()
    {
        _apiService.GetConsolidationSuggestionsException = new OperationCanceledException();

        await Should.ThrowAsync<OperationCanceledException>(() => _sut.InitializeAsync());
        _sut.IsLoading.ShouldBeFalse();
    }

    [Fact]
    public async Task RunAnalysisAsync_SetsErrorMessage_WhenStateIsInvalid()
    {
        _apiService.AnalyzeConsolidationException = new InvalidOperationException("Bad response");

        await _sut.RunAnalysisAsync();

        _sut.ErrorMessage.ShouldBe("Analysis failed: Bad response");
        _sut.IsAnalyzing.ShouldBeFalse();
    }

    [Fact]
    public async Task AcceptAsync_SetsErrorMessage_WhenApiRequestFails()
    {
        _apiService.AcceptConsolidationSuggestionException = new HttpRequestException("No route to host");

        await _sut.AcceptAsync(Guid.NewGuid());

        _sut.ErrorMessage.ShouldBe("Failed to accept suggestion: No route to host");
    }

    [Fact]
    public async Task DismissAsync_SetsErrorMessage_WhenApiRequestFails()
    {
        _apiService.DismissConsolidationSuggestionException = new HttpRequestException("Timeout");

        await _sut.DismissAsync(Guid.NewGuid());

        _sut.ErrorMessage.ShouldBe("Failed to dismiss suggestion: Timeout");
    }

    [Fact]
    public async Task UndoAsync_SetsErrorMessage_WhenApiRequestFails()
    {
        _apiService.UndoConsolidationException = new InvalidOperationException("Unexpected response");

        await _sut.UndoAsync(Guid.NewGuid());

        _sut.ErrorMessage.ShouldBe("Failed to undo consolidation: Unexpected response");
    }
}
