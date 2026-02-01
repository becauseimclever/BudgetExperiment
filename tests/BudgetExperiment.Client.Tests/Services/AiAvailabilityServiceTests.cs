// <copyright file="AiAvailabilityServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Shouldly;

namespace BudgetExperiment.Client.Tests.Services;

/// <summary>
/// Unit tests for <see cref="AiAvailabilityService"/>.
/// </summary>
public class AiAvailabilityServiceTests
{
    /// <summary>
    /// Tests that State returns Disabled when AI feature flag is off.
    /// </summary>
    [Fact]
    public async Task RefreshAsync_WhenAiDisabled_StateIsDisabled()
    {
        // Arrange
        var aiApiService = new FakeAiApiService
        {
            StatusToReturn = new AiStatusDto
            {
                IsEnabled = false,
                IsAvailable = false,
                Endpoint = "http://localhost:11434",
            },
        };

        var service = new AiAvailabilityService(aiApiService);

        // Act
        await service.RefreshAsync();

        // Assert
        service.State.ShouldBe(AiAvailabilityState.Disabled);
        service.IsEnabled.ShouldBeFalse();
        service.IsAvailable.ShouldBeFalse();
        service.IsFullyOperational.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that State returns Unavailable when AI is enabled but Ollama is not connected.
    /// </summary>
    [Fact]
    public async Task RefreshAsync_WhenAiEnabledButNotConnected_StateIsUnavailable()
    {
        // Arrange
        var aiApiService = new FakeAiApiService
        {
            StatusToReturn = new AiStatusDto
            {
                IsEnabled = true,
                IsAvailable = false,
                Endpoint = "http://localhost:11434",
                ErrorMessage = "Connection refused",
            },
        };

        var service = new AiAvailabilityService(aiApiService);

        // Act
        await service.RefreshAsync();

        // Assert
        service.State.ShouldBe(AiAvailabilityState.Unavailable);
        service.IsEnabled.ShouldBeTrue();
        service.IsAvailable.ShouldBeFalse();
        service.IsFullyOperational.ShouldBeFalse();
        service.ErrorMessage.ShouldBe("Connection refused");
    }

    /// <summary>
    /// Tests that State returns Available when AI is enabled and connected.
    /// </summary>
    [Fact]
    public async Task RefreshAsync_WhenAiEnabledAndConnected_StateIsAvailable()
    {
        // Arrange
        var aiApiService = new FakeAiApiService
        {
            StatusToReturn = new AiStatusDto
            {
                IsEnabled = true,
                IsAvailable = true,
                Endpoint = "http://localhost:11434",
                CurrentModel = "llama3.2",
            },
        };

        var service = new AiAvailabilityService(aiApiService);

        // Act
        await service.RefreshAsync();

        // Assert
        service.State.ShouldBe(AiAvailabilityState.Available);
        service.IsEnabled.ShouldBeTrue();
        service.IsAvailable.ShouldBeTrue();
        service.IsFullyOperational.ShouldBeTrue();
        service.ErrorMessage.ShouldBeNull();
    }

    /// <summary>
    /// Tests that State defaults to Disabled when API returns null.
    /// </summary>
    [Fact]
    public async Task RefreshAsync_WhenApiReturnsNull_StateIsDisabled()
    {
        // Arrange
        var aiApiService = new FakeAiApiService
        {
            StatusToReturn = null,
        };

        var service = new AiAvailabilityService(aiApiService);

        // Act
        await service.RefreshAsync();

        // Assert
        service.State.ShouldBe(AiAvailabilityState.Disabled);
        service.IsEnabled.ShouldBeFalse();
        service.IsAvailable.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that State returns Unavailable when API throws exception (graceful handling).
    /// </summary>
    [Fact]
    public async Task RefreshAsync_WhenApiThrows_StateIsUnavailable()
    {
        // Arrange
        var aiApiService = new FakeAiApiService
        {
            ExceptionToThrow = new HttpRequestException("Network error"),
        };

        var service = new AiAvailabilityService(aiApiService);

        // Act
        await service.RefreshAsync();

        // Assert
        // When API fails completely, we assume AI might be enabled but unreachable
        // This allows showing the warning state rather than hiding completely
        service.State.ShouldBe(AiAvailabilityState.Unavailable);
        service.ErrorMessage.ShouldNotBeNull();
        service.ErrorMessage.ShouldContain("Network error");
    }

    /// <summary>
    /// Tests that StatusChanged event is raised when status changes.
    /// </summary>
    [Fact]
    public async Task RefreshAsync_WhenStatusChanges_RaisesStatusChangedEvent()
    {
        // Arrange
        var aiApiService = new FakeAiApiService
        {
            StatusToReturn = new AiStatusDto
            {
                IsEnabled = true,
                IsAvailable = true,
                Endpoint = "http://localhost:11434",
            },
        };

        var service = new AiAvailabilityService(aiApiService);
        var eventRaised = false;
        service.StatusChanged += () => eventRaised = true;

        // Act
        await service.RefreshAsync();

        // Assert
        eventRaised.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that cached status is returned without API call within cache duration.
    /// </summary>
    [Fact]
    public async Task State_ReturnsCachedValue_WithinCacheDuration()
    {
        // Arrange
        var aiApiService = new FakeAiApiService
        {
            StatusToReturn = new AiStatusDto
            {
                IsEnabled = true,
                IsAvailable = true,
                Endpoint = "http://localhost:11434",
            },
        };

        var service = new AiAvailabilityService(aiApiService);

        // Act - First call
        await service.RefreshAsync();

        // Change what the API would return
        aiApiService.StatusToReturn = new AiStatusDto
        {
            IsEnabled = false,
            IsAvailable = false,
            Endpoint = "http://localhost:11434",
        };

        // Assert - State should still be cached from first call
        service.State.ShouldBe(AiAvailabilityState.Available);

        // Verify API was only called once
        aiApiService.GetStatusCallCount.ShouldBe(1);
    }

    /// <summary>
    /// Tests that initial state before any refresh is Disabled.
    /// </summary>
    [Fact]
    public void State_BeforeRefresh_IsDisabled()
    {
        // Arrange
        var aiApiService = new FakeAiApiService();
        var service = new AiAvailabilityService(aiApiService);

        // Assert
        service.State.ShouldBe(AiAvailabilityState.Disabled);
        service.IsEnabled.ShouldBeFalse();
        service.IsAvailable.ShouldBeFalse();
        service.IsFullyOperational.ShouldBeFalse();
    }

    /// <summary>
    /// Fake implementation of IAiApiService for testing.
    /// </summary>
    private sealed class FakeAiApiService : IAiApiService
    {
        public AiStatusDto? StatusToReturn { get; set; }

        public Exception? ExceptionToThrow { get; set; }

        public int GetStatusCallCount { get; private set; }

        public Task<AiStatusDto?> GetStatusAsync()
        {
            this.GetStatusCallCount++;

            if (this.ExceptionToThrow != null)
            {
                throw this.ExceptionToThrow;
            }

            return Task.FromResult(this.StatusToReturn);
        }

        public Task<IReadOnlyList<AiModelDto>> GetModelsAsync() =>
            Task.FromResult<IReadOnlyList<AiModelDto>>(Array.Empty<AiModelDto>());

        public Task<AiSettingsDto?> GetSettingsAsync() =>
            Task.FromResult<AiSettingsDto?>(null);

        public Task<AiSettingsDto?> UpdateSettingsAsync(AiSettingsDto settings) =>
            Task.FromResult<AiSettingsDto?>(settings);

        public Task<AnalysisResponseDto?> AnalyzeAsync() =>
            Task.FromResult<AnalysisResponseDto?>(null);

        public Task<IReadOnlyList<RuleSuggestionDto>> GenerateSuggestionsAsync(GenerateSuggestionsRequest request) =>
            Task.FromResult<IReadOnlyList<RuleSuggestionDto>>(Array.Empty<RuleSuggestionDto>());

        public Task<IReadOnlyList<RuleSuggestionDto>> GetPendingSuggestionsAsync(string? type = null) =>
            Task.FromResult<IReadOnlyList<RuleSuggestionDto>>(Array.Empty<RuleSuggestionDto>());

        public Task<RuleSuggestionDto?> GetSuggestionAsync(Guid id) =>
            Task.FromResult<RuleSuggestionDto?>(null);

        public Task<CategorizationRuleDto?> AcceptSuggestionAsync(Guid id) =>
            Task.FromResult<CategorizationRuleDto?>(null);

        public Task<bool> DismissSuggestionAsync(Guid id, string? reason = null) =>
            Task.FromResult(true);

        public Task<bool> ProvideFeedbackAsync(Guid id, bool isPositive) =>
            Task.FromResult(true);
    }
}
