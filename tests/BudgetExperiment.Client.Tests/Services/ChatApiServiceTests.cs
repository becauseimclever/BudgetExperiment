// <copyright file="ChatApiServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Client.Tests.Services;

/// <summary>
/// Unit tests for the <see cref="ChatApiService"/> class.
/// Tests focus on URL construction, request packaging, and fallback behavior
/// rather than HTTP framework behavior.
/// </summary>
public sealed class ChatApiServiceTests
{
    /// <summary>
    /// Verifies that GetMessagesAsync includes the limit query parameter.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetMessagesAsync_IncludesLimitQueryParam()
    {
        // Arrange
        string? capturedUrl = null;
        var sessionId = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((req, _) =>
        {
            capturedUrl = req.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<ChatMessageDto>()),
            });
        });
        var sut = CreateService(handler);

        // Act
        await sut.GetMessagesAsync(sessionId, 25);

        // Assert
        Assert.Contains("limit=25", capturedUrl, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that GetMessagesAsync defaults limit to 50.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetMessagesAsync_DefaultLimit_Is50()
    {
        // Arrange
        string? capturedUrl = null;
        var handler = new MockHttpMessageHandler((req, _) =>
        {
            capturedUrl = req.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<ChatMessageDto>()),
            });
        });
        var sut = CreateService(handler);

        // Act
        await sut.GetMessagesAsync(Guid.NewGuid());

        // Assert
        Assert.Contains("limit=50", capturedUrl, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that GetMessagesAsync returns empty list on HTTP failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetMessagesAsync_OnHttpFailure_ReturnsEmptyList()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Network error"));
        var sut = CreateService(handler);

        // Act
        var result = await sut.GetMessagesAsync(Guid.NewGuid());

        // Assert
        Assert.Empty(result);
    }

    /// <summary>
    /// Verifies that SendMessageAsync includes content in the request body.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SendMessageAsync_IncludesContentInBody()
    {
        // Arrange
        string? capturedBody = null;
        var handler = new MockHttpMessageHandler(async (req, _) =>
        {
            capturedBody = await req.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new SendMessageResponse()),
            };
        });
        var sut = CreateService(handler);

        // Act
        await sut.SendMessageAsync(Guid.NewGuid(), "Hello AI");

        // Assert
        Assert.NotNull(capturedBody);
        Assert.Contains("Hello AI", capturedBody, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that CancelActionAsync returns false on HTTP failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CancelActionAsync_OnHttpFailure_ReturnsFalse()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Network error"));
        var sut = CreateService(handler);

        // Act
        var result = await sut.CancelActionAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Verifies that CloseSessionAsync returns true on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CloseSessionAsync_OnSuccess_ReturnsTrue()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var sut = CreateService(handler);

        // Act
        var result = await sut.CloseSessionAsync(Guid.NewGuid());

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Verifies that CloseSessionAsync returns false on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CloseSessionAsync_OnFailure_ReturnsFalse()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));
        var sut = CreateService(handler);

        // Act
        var result = await sut.CloseSessionAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Verifies that GetOrCreateSessionAsync returns null on HTTP failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetOrCreateSessionAsync_OnHttpFailure_ReturnsNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Network error"));
        var sut = CreateService(handler);

        // Act
        var result = await sut.GetOrCreateSessionAsync();

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that GetOrCreateSessionAsync calls the correct endpoint.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetOrCreateSessionAsync_CallsCorrectEndpoint()
    {
        // Arrange
        string? capturedUrl = null;
        var handler = new MockHttpMessageHandler((req, _) =>
        {
            capturedUrl = req.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new ChatSessionDto { Id = Guid.NewGuid() }),
            });
        });
        var sut = CreateService(handler);

        // Act
        var result = await sut.GetOrCreateSessionAsync();

        // Assert
        Assert.Contains("api/v1/chat/sessions", capturedUrl, StringComparison.Ordinal);
        Assert.NotNull(result);
    }

    /// <summary>
    /// Verifies that GetSessionAsync returns null on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetSessionAsync_OnHttpFailure_ReturnsNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Network error"));
        var sut = CreateService(handler);

        // Act
        var result = await sut.GetSessionAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that GetSessionAsync calls the correct endpoint with session ID.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetSessionAsync_IncludesSessionIdInUrl()
    {
        // Arrange
        string? capturedUrl = null;
        var sessionId = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((req, _) =>
        {
            capturedUrl = req.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new ChatSessionDto { Id = sessionId }),
            });
        });
        var sut = CreateService(handler);

        // Act
        await sut.GetSessionAsync(sessionId);

        // Assert
        Assert.Contains(sessionId.ToString(), capturedUrl, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that SendMessageAsync returns null on HTTP failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SendMessageAsync_OnHttpFailure_ReturnsNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Network error"));
        var sut = CreateService(handler);

        // Act
        var result = await sut.SendMessageAsync(Guid.NewGuid(), "Hello");

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that SendMessageAsync calls the correct endpoint.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SendMessageAsync_CallsCorrectEndpoint()
    {
        // Arrange
        string? capturedUrl = null;
        var sessionId = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((req, _) =>
        {
            capturedUrl = req.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new SendMessageResponse()),
            });
        });
        var sut = CreateService(handler);

        // Act
        await sut.SendMessageAsync(sessionId, "Test");

        // Assert
        Assert.Contains($"{sessionId}/messages", capturedUrl, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that ConfirmActionAsync returns response on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ConfirmActionAsync_OnSuccess_ReturnsResponse()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new ConfirmActionResponse()),
            }));
        var sut = CreateService(handler);

        // Act
        var result = await sut.ConfirmActionAsync(Guid.NewGuid());

        // Assert
        Assert.NotNull(result);
    }

    /// <summary>
    /// Verifies that ConfirmActionAsync returns null on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ConfirmActionAsync_OnHttpFailure_ReturnsNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Network error"));
        var sut = CreateService(handler);

        // Act
        var result = await sut.ConfirmActionAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that CancelActionAsync returns true on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CancelActionAsync_OnSuccess_ReturnsTrue()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var sut = CreateService(handler);

        // Act
        var result = await sut.CancelActionAsync(Guid.NewGuid());

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Verifies that GetMessagesAsync includes session ID in the URL.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetMessagesAsync_IncludesSessionIdInUrl()
    {
        // Arrange
        string? capturedUrl = null;
        var sessionId = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((req, _) =>
        {
            capturedUrl = req.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<ChatMessageDto>()),
            });
        });
        var sut = CreateService(handler);

        // Act
        await sut.GetMessagesAsync(sessionId);

        // Assert
        Assert.Contains(sessionId.ToString(), capturedUrl, StringComparison.Ordinal);
    }

    private static ChatApiService CreateService(MockHttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        return new ChatApiService(client);
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
