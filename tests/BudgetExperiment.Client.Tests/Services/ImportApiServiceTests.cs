// <copyright file="ImportApiServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Client.Models;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Client.Tests.Services;

/// <summary>
/// Unit tests for the <see cref="ImportApiService"/> class.
/// Tests focus on service logic (ETag handling, conflict detection, URL construction)
/// rather than HTTP framework behavior.
/// </summary>
public sealed class ImportApiServiceTests
{
    /// <summary>
    /// Verifies that UpdateMappingAsync adds ETag header when version is provided.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateMappingAsync_WithVersion_AddsIfMatchHeader()
    {
        // Arrange
        string? capturedIfMatch = null;
        var handler = new MockHttpMessageHandler((req, _) =>
        {
            capturedIfMatch = req.Headers.IfMatch.FirstOrDefault()?.Tag;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new ImportMappingDto()),
            });
        });
        var sut = CreateService(handler);

        // Act
        await sut.UpdateMappingAsync(Guid.NewGuid(), new UpdateImportMappingRequest(), "v2");

        // Assert
        Assert.Equal("\"v2\"", capturedIfMatch);
    }

    /// <summary>
    /// Verifies that UpdateMappingAsync does not add ETag header when version is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateMappingAsync_WithoutVersion_NoIfMatchHeader()
    {
        // Arrange
        bool hasIfMatch = true;
        var handler = new MockHttpMessageHandler((req, _) =>
        {
            hasIfMatch = req.Headers.IfMatch.Any();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new ImportMappingDto()),
            });
        });
        var sut = CreateService(handler);

        // Act
        await sut.UpdateMappingAsync(Guid.NewGuid(), new UpdateImportMappingRequest());

        // Assert
        Assert.False(hasIfMatch);
    }

    /// <summary>
    /// Verifies that UpdateMappingAsync returns conflict result on 409 status.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateMappingAsync_On409_ReturnsConflict()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict)));
        var sut = CreateService(handler);

        // Act
        var result = await sut.UpdateMappingAsync(Guid.NewGuid(), new UpdateImportMappingRequest(), "stale-etag");

        // Assert
        Assert.True(result.IsConflict);
        Assert.False(result.IsSuccess);
    }

    /// <summary>
    /// Verifies that UpdateMappingAsync returns success result on 200 status.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateMappingAsync_OnSuccess_ReturnsSuccessResult()
    {
        // Arrange
        var mappingId = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new ImportMappingDto { Id = mappingId }),
            }));
        var sut = CreateService(handler);

        // Act
        var result = await sut.UpdateMappingAsync(mappingId, new UpdateImportMappingRequest());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsConflict);
        Assert.NotNull(result.Data);
    }

    /// <summary>
    /// Verifies that UpdateMappingAsync returns failure on non-success status.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateMappingAsync_OnNonSuccessStatus_ReturnsFailure()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));
        var sut = CreateService(handler);

        // Act
        var result = await sut.UpdateMappingAsync(Guid.NewGuid(), new UpdateImportMappingRequest());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.False(result.IsConflict);
    }

    /// <summary>
    /// Verifies that SuggestMappingAsync returns null for 204 NoContent.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SuggestMappingAsync_OnNoContent_ReturnsNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)));
        var sut = CreateService(handler);

        // Act
        var result = await sut.SuggestMappingAsync(["Date", "Amount", "Description"]);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that DeleteMappingAsync returns true on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteMappingAsync_OnSuccess_ReturnsTrue()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var sut = CreateService(handler);

        // Act
        var result = await sut.DeleteMappingAsync(Guid.NewGuid());

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Verifies that DeleteMappingAsync returns false on non-success status.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteMappingAsync_OnNotFound_ReturnsFalse()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        var sut = CreateService(handler);

        // Act
        var result = await sut.DeleteMappingAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Verifies that GetMappingsAsync propagates HttpRequestException (not caught).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetMappingsAsync_OnHttpFailure_PropagatesException()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Network error"));
        var sut = CreateService(handler);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => sut.GetMappingsAsync());
    }

    private static ImportApiService CreateService(MockHttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        return new ImportApiService(client);
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
