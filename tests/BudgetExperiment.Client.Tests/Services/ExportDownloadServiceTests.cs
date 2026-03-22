// <copyright file="ExportDownloadServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Headers;

using BudgetExperiment.Client.Services;

using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Tests.Services;

/// <summary>
/// Unit tests for the <see cref="ExportDownloadService"/> class.
/// Tests focus on result construction and error handling
/// rather than JS interop or HTTP framework behavior.
/// </summary>
public sealed class ExportDownloadServiceTests : IAsyncDisposable
{
    /// <summary>
    /// Verifies that DownloadAsync returns failure result on non-success HTTP status.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DownloadAsync_NonSuccessStatus_ReturnsFailure()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                ReasonPhrase = "Not Found",
            }));
        var sut = CreateService(handler);

        // Act
        var result = await sut.DownloadAsync("api/v1/export");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("404", result.ErrorMessage!, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that DownloadAsync returns failure with reason phrase in error message.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DownloadAsync_IncludesReasonPhraseInError()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                ReasonPhrase = "Forbidden",
            }));
        var sut = CreateService(handler);

        // Act
        var result = await sut.DownloadAsync("api/v1/export");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Forbidden", result.ErrorMessage!, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that DownloadAsync returns failure on HTTP request exception.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DownloadAsync_OnHttpException_ReturnsFailure()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));
        var sut = CreateService(handler);

        // Act
        var result = await sut.DownloadAsync("api/v1/export");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Connection refused", result.ErrorMessage!, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that DownloadAsync returns failure on cancellation.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DownloadAsync_OnCancellation_ReturnsFailure()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new OperationCanceledException("Cancelled"));
        var sut = CreateService(handler);

        // Act
        var result = await sut.DownloadAsync("api/v1/export");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("canceled", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that ExportDownloadResult.Ok creates a success result.
    /// </summary>
    [Fact]
    public void ExportDownloadResult_Ok_CreatesSuccessResult()
    {
        var result = ExportDownloadResult.Ok();

        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
    }

    /// <summary>
    /// Verifies that ExportDownloadResult.Fail creates a failure result with message.
    /// </summary>
    [Fact]
    public void ExportDownloadResult_Fail_CreatesFailureWithMessage()
    {
        var result = ExportDownloadResult.Fail("Something broke");

        Assert.False(result.Success);
        Assert.Equal("Something broke", result.ErrorMessage);
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    private static ExportDownloadService CreateService(MockHttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        return new ExportDownloadService(client, new StubJSRuntime());
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

    /// <summary>
    /// Stub JavaScript runtime that returns defaults.
    /// </summary>
    private sealed class StubJSRuntime : IJSRuntime
    {
        /// <inheritdoc/>
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return new ValueTask<TValue>(default(TValue)!);
        }

        /// <inheritdoc/>
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            return new ValueTask<TValue>(default(TValue)!);
        }
    }
}
