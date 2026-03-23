// <copyright file="AiApiServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Client.Tests.Services;

/// <summary>
/// Unit tests for the <see cref="AiApiService"/> class.
/// Tests focus on service logic (error mapping, URL building, fallback behavior)
/// rather than HTTP framework behavior.
/// </summary>
public sealed class AiApiServiceTests
{
    /// <summary>
    /// Verifies that AnalyzeAsync maps a 504 status to a timeout error message.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AnalyzeAsync_504_ThrowsTimeoutMessage()
    {
        // Arrange
        var sut = CreateService(HttpStatusCode.GatewayTimeout);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.AnalyzeAsync());
        Assert.Contains("timed out", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that AnalyzeAsync maps a 503 status to an unavailable error message.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AnalyzeAsync_503_ThrowsUnavailableMessage()
    {
        // Arrange
        var sut = CreateService(HttpStatusCode.ServiceUnavailable);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.AnalyzeAsync());
        Assert.Contains("unavailable", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that AnalyzeAsync maps an unexpected status to a generic error message.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AnalyzeAsync_UnexpectedStatus_ThrowsGenericMessage()
    {
        // Arrange
        var sut = CreateService(HttpStatusCode.BadRequest);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.AnalyzeAsync());
        Assert.Contains("400", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that AnalyzeAsync wraps HttpRequestException with a connection error message.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AnalyzeAsync_ConnectionFailure_ThrowsWithConnectionMessage()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));
        var sut = CreateService(handler);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.AnalyzeAsync());
        Assert.Contains("Failed to connect", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that GetPendingSuggestionsAsync without type filter uses base URL.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetPendingSuggestionsAsync_NoType_UsesBaseUrl()
    {
        // Arrange
        string? capturedUrl = null;
        var handler = new MockHttpMessageHandler((req, _) =>
        {
            capturedUrl = req.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<RuleSuggestionDto>()),
            });
        });
        var sut = CreateService(handler);

        // Act
        await sut.GetPendingSuggestionsAsync();

        // Assert
        Assert.Equal("/api/v1/suggestions", capturedUrl);
    }

    /// <summary>
    /// Verifies that GetPendingSuggestionsAsync with type filter adds query parameter.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetPendingSuggestionsAsync_WithType_AddsQueryParam()
    {
        // Arrange
        string? capturedUrl = null;
        var handler = new MockHttpMessageHandler((req, _) =>
        {
            capturedUrl = req.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<RuleSuggestionDto>()),
            });
        });
        var sut = CreateService(handler);

        // Act
        await sut.GetPendingSuggestionsAsync("category");

        // Assert
        Assert.Equal("/api/v1/suggestions?type=category", capturedUrl);
    }

    /// <summary>
    /// Verifies that GetModelsAsync returns empty list on HTTP failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetModelsAsync_OnHttpFailure_ReturnsEmptyList()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Network error"));
        var sut = CreateService(handler);

        // Act
        var result = await sut.GetModelsAsync();

        // Assert
        Assert.Empty(result);
    }

    /// <summary>
    /// Verifies that GetStatusAsync returns null on HTTP failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetStatusAsync_OnHttpFailure_ReturnsNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Network error"));
        var sut = CreateService(handler);

        // Act
        var result = await sut.GetStatusAsync();

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that UpdateSettingsAsync returns null on non-success status.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateSettingsAsync_NonSuccess_ReturnsNull()
    {
        // Arrange
        var sut = CreateService(HttpStatusCode.InternalServerError);

        // Act
        var result = await sut.UpdateSettingsAsync(new AiSettingsDto());

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that AnalyzeAsync returns response on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AnalyzeAsync_OnSuccess_ReturnsResponse()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new AnalysisResponseDto
                {
                    NewRuleSuggestions = 5,
                    UncategorizedTransactionsAnalyzed = 10,
                }),
            }));
        var sut = CreateService(handler);

        // Act
        var result = await sut.AnalyzeAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.NewRuleSuggestions);
    }

    /// <summary>
    /// Verifies that GetSettingsAsync returns settings on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetSettingsAsync_OnSuccess_ReturnsSettings()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((req, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new AiSettingsDto
                {
                    ModelName = "llama3",
                    IsEnabled = true,
                    TimeoutSeconds = 120,
                }),
            }));
        var sut = CreateService(handler);

        // Act
        var result = await sut.GetSettingsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("llama3", result.ModelName);
        Assert.True(result.IsEnabled);
    }

    /// <summary>
    /// Verifies that GetSettingsAsync returns null on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetSettingsAsync_OnFailure_ReturnsNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Network error"));
        var sut = CreateService(handler);

        // Act
        var result = await sut.GetSettingsAsync();

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that UpdateSettingsAsync returns updated settings on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateSettingsAsync_OnSuccess_ReturnsUpdatedSettings()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new AiSettingsDto
                {
                    ModelName = "mistral",
                    IsEnabled = true,
                }),
            }));
        var sut = CreateService(handler);

        // Act
        var result = await sut.UpdateSettingsAsync(new AiSettingsDto { ModelName = "mistral" });

        // Assert
        Assert.NotNull(result);
        Assert.Equal("mistral", result.ModelName);
    }

    /// <summary>
    /// Verifies that GenerateSuggestionsAsync calls correct endpoint and returns suggestions.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GenerateSuggestionsAsync_OnSuccess_ReturnsSuggestions()
    {
        // Arrange
        string? capturedUrl = null;
        var handler = new MockHttpMessageHandler((req, _) =>
        {
            capturedUrl = req.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<RuleSuggestionDto>
                {
                    new RuleSuggestionDto { Id = Guid.NewGuid(), Title = "Test Rule" },
                }),
            });
        });
        var sut = CreateService(handler);

        // Act
        var result = await sut.GenerateSuggestionsAsync(new GenerateSuggestionsRequest
        {
            SuggestionType = "category",
            MaxSuggestions = 5,
        });

        // Assert
        Assert.Single(result);
        Assert.Equal("Test Rule", result[0].Title);
        Assert.Equal("/api/v1/suggestions/generate", capturedUrl);
    }

    /// <summary>
    /// Verifies that GenerateSuggestionsAsync returns empty list on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GenerateSuggestionsAsync_OnFailure_ReturnsEmptyList()
    {
        // Arrange
        var sut = CreateService(HttpStatusCode.InternalServerError);

        // Act
        var result = await sut.GenerateSuggestionsAsync(new GenerateSuggestionsRequest());

        // Assert
        Assert.Empty(result);
    }

    /// <summary>
    /// Verifies that GetSuggestionAsync returns suggestion on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetSuggestionAsync_OnSuccess_ReturnsSuggestion()
    {
        // Arrange
        var id = Guid.NewGuid();
        string? capturedUrl = null;
        var handler = new MockHttpMessageHandler((req, _) =>
        {
            capturedUrl = req.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new RuleSuggestionDto { Id = id, Title = "Found" }),
            });
        });
        var sut = CreateService(handler);

        // Act
        var result = await sut.GetSuggestionAsync(id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal($"/api/v1/suggestions/{id}", capturedUrl);
    }

    /// <summary>
    /// Verifies that GetSuggestionAsync returns null on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetSuggestionAsync_OnFailure_ReturnsNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Not found"));
        var sut = CreateService(handler);

        // Act
        var result = await sut.GetSuggestionAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that AcceptSuggestionAsync returns created rule on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AcceptSuggestionAsync_OnSuccess_ReturnsRule()
    {
        // Arrange
        var id = Guid.NewGuid();
        string? capturedUrl = null;
        var handler = new MockHttpMessageHandler((req, _) =>
        {
            capturedUrl = req.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new CategorizationRuleDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Accepted Rule",
                    Pattern = "test*",
                    MatchType = "Contains",
                }),
            });
        });
        var sut = CreateService(handler);

        // Act
        var result = await sut.AcceptSuggestionAsync(id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Accepted Rule", result.Name);
        Assert.Equal($"/api/v1/suggestions/{id}/accept", capturedUrl);
    }

    /// <summary>
    /// Verifies that AcceptSuggestionAsync returns null on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AcceptSuggestionAsync_OnFailure_ReturnsNull()
    {
        // Arrange
        var sut = CreateService(HttpStatusCode.NotFound);

        // Act
        var result = await sut.AcceptSuggestionAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that DismissSuggestionAsync returns true on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DismissSuggestionAsync_OnSuccess_ReturnsTrue()
    {
        // Arrange
        var id = Guid.NewGuid();
        string? capturedUrl = null;
        var handler = new MockHttpMessageHandler((req, _) =>
        {
            capturedUrl = req.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        var sut = CreateService(handler);

        // Act
        var result = await sut.DismissSuggestionAsync(id, "Not relevant");

        // Assert
        Assert.True(result);
        Assert.Equal($"/api/v1/suggestions/{id}/dismiss", capturedUrl);
    }

    /// <summary>
    /// Verifies that DismissSuggestionAsync returns false on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DismissSuggestionAsync_OnFailure_ReturnsFalse()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection error"));
        var sut = CreateService(handler);

        // Act
        var result = await sut.DismissSuggestionAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Verifies that ProvideFeedbackAsync calls correct endpoint and returns true on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ProvideFeedbackAsync_OnSuccess_ReturnsTrue()
    {
        // Arrange
        var id = Guid.NewGuid();
        string? capturedUrl = null;
        var handler = new MockHttpMessageHandler((req, _) =>
        {
            capturedUrl = req.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        var sut = CreateService(handler);

        // Act
        var result = await sut.ProvideFeedbackAsync(id, true);

        // Assert
        Assert.True(result);
        Assert.Equal($"/api/v1/suggestions/{id}/feedback", capturedUrl);
    }

    /// <summary>
    /// Verifies that ProvideFeedbackAsync returns false on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ProvideFeedbackAsync_OnFailure_ReturnsFalse()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Server error"));
        var sut = CreateService(handler);

        // Act
        var result = await sut.ProvideFeedbackAsync(Guid.NewGuid(), false);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Verifies that GetStatusAsync returns status on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetStatusAsync_OnSuccess_ReturnsStatus()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new AiStatusDto
                {
                    IsAvailable = true,
                    IsEnabled = true,
                    CurrentModel = "llama3",
                    Endpoint = "http://localhost:11434",
                }),
            }));
        var sut = CreateService(handler);

        // Act
        var result = await sut.GetStatusAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsAvailable);
        Assert.Equal("llama3", result.CurrentModel);
    }

    /// <summary>
    /// Verifies that GetModelsAsync returns models on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetModelsAsync_OnSuccess_ReturnsModels()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<AiModelDto>
                {
                    new AiModelDto { Name = "llama3", SizeBytes = 4_000_000_000 },
                    new AiModelDto { Name = "mistral", SizeBytes = 7_000_000_000 },
                }),
            }));
        var sut = CreateService(handler);

        // Act
        var result = await sut.GetModelsAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("llama3", result[0].Name);
    }

    private static AiApiService CreateService(HttpStatusCode statusCode) =>
        CreateService(new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(string.Empty),
            })));

    private static AiApiService CreateService(MockHttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        return new AiApiService(client);
    }

    /// <summary>
    /// Mock HTTP message handler for testing.
    /// </summary>
    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handler(request, cancellationToken);
        }
    }
}
