// <copyright file="CategorySuggestionApiServiceClearPatternsTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Client.Services;

namespace BudgetExperiment.Client.Tests.Services;

/// <summary>
/// Tests for <see cref="CategorySuggestionApiService.ClearDismissedPatternsAsync"/>.
/// </summary>
public class CategorySuggestionApiServiceClearPatternsTests
{
    /// <summary>
    /// Verifies that ClearDismissedPatternsAsync sends a DELETE request to the correct URL.
    /// </summary>
    [Fact]
    public async Task ClearDismissedPatternsAsync_SendsDeleteToCorrectUrl()
    {
        // Arrange
        string? capturedUrl = null;
        HttpMethod? capturedMethod = null;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            capturedMethod = request.Method;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { clearedCount = 5 }),
            };
            return Task.FromResult(response);
        });

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost/"),
        };
        var service = new CategorySuggestionApiService(httpClient);

        // Act
        var result = await service.ClearDismissedPatternsAsync();

        // Assert
        Assert.Equal("/api/v1/categorysuggestions/dismissed-patterns", capturedUrl);
        Assert.Equal(HttpMethod.Delete, capturedMethod);
        Assert.Equal(5, result);
    }

    /// <summary>
    /// Verifies that ClearDismissedPatternsAsync returns zero when server returns non-success.
    /// </summary>
    [Fact]
    public async Task ClearDismissedPatternsAsync_ReturnsZero_WhenServerReturnsError()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            return Task.FromResult(response);
        });

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost/"),
        };
        var service = new CategorySuggestionApiService(httpClient);

        // Act
        var result = await service.ClearDismissedPatternsAsync();

        // Assert
        Assert.Equal(0, result);
    }

    /// <summary>
    /// Verifies that ClearDismissedPatternsAsync returns zero on HTTP exception.
    /// </summary>
    [Fact]
    public async Task ClearDismissedPatternsAsync_ReturnsZero_OnHttpException()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
        {
            throw new HttpRequestException("connection refused");
        });

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost/"),
        };
        var service = new CategorySuggestionApiService(httpClient);

        // Act
        var result = await service.ClearDismissedPatternsAsync();

        // Assert
        Assert.Equal(0, result);
    }

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
