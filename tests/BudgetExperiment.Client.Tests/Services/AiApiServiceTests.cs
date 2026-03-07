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
            this._handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return this._handler(request, cancellationToken);
        }
    }
}
