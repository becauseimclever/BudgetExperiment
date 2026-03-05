// <copyright file="BudgetApiServiceETagTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Client.Models;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Shouldly;

namespace BudgetExperiment.Client.Tests.Services;

/// <summary>
/// Unit tests for <see cref="BudgetApiService"/> ETag/If-Match concurrency behavior.
/// </summary>
public class BudgetApiServiceETagTests
{
    /// <summary>
    /// Tests that UpdateAccountAsync sends If-Match header when version is provided.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateAccountAsync_WithVersion_SendsIfMatchHeader()
    {
        // Arrange
        string? ifMatchValue = null;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            ifMatchValue = request.Headers.IfMatch.FirstOrDefault()?.Tag;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new AccountDto { Id = Guid.NewGuid(), Name = "Test", Version = "999" }),
            };
            return Task.FromResult(response);
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        await service.UpdateAccountAsync(Guid.NewGuid(), new AccountUpdateDto { Name = "Test" }, "12345");

        // Assert
        ifMatchValue.ShouldBe("\"12345\"");
    }

    /// <summary>
    /// Tests that UpdateAccountAsync does not send If-Match header when version is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateAccountAsync_WithoutVersion_DoesNotSendIfMatchHeader()
    {
        // Arrange
        bool hasIfMatch = false;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            hasIfMatch = request.Headers.IfMatch.Any();
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new AccountDto { Id = Guid.NewGuid(), Name = "Test" }),
            };
            return Task.FromResult(response);
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        await service.UpdateAccountAsync(Guid.NewGuid(), new AccountUpdateDto { Name = "Test" });

        // Assert
        hasIfMatch.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that UpdateAccountAsync returns conflict result when server responds with 409.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateAccountAsync_Returns409_ReturnsConflictResult()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.UpdateAccountAsync(Guid.NewGuid(), new AccountUpdateDto { Name = "Test" }, "stale");

        // Assert
        result.IsConflict.ShouldBeTrue();
        result.IsSuccess.ShouldBeFalse();
        result.Data.ShouldBeNull();
    }

    /// <summary>
    /// Tests that UpdateAccountAsync returns success result with updated data.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateAccountAsync_Returns200_ReturnsSuccessResult()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new AccountDto { Id = expectedId, Name = "Updated", Version = "999" }),
            };
            return Task.FromResult(response);
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.UpdateAccountAsync(expectedId, new AccountUpdateDto { Name = "Updated" }, "123");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.IsConflict.ShouldBeFalse();
        result.Data.ShouldNotBeNull();
        result.Data.Name.ShouldBe("Updated");
        result.Data.Version.ShouldBe("999");
    }

    /// <summary>
    /// Tests that UpdateAccountAsync returns failure for non-success, non-conflict status codes.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateAccountAsync_Returns400_ReturnsFailureResult()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.UpdateAccountAsync(Guid.NewGuid(), new AccountUpdateDto { Name = "Test" });

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.IsConflict.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that UpdateTransactionAsync sends If-Match header when version is provided.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateTransactionAsync_WithVersion_SendsIfMatchHeader()
    {
        // Arrange
        string? ifMatchValue = null;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            ifMatchValue = request.Headers.IfMatch.FirstOrDefault()?.Tag;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new TransactionDto()),
            };
            return Task.FromResult(response);
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        await service.UpdateTransactionAsync(Guid.NewGuid(), new TransactionUpdateDto(), "ver1");

        // Assert
        ifMatchValue.ShouldBe("\"ver1\"");
    }

    /// <summary>
    /// Tests that UpdateTransactionAsync returns conflict on 409.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateTransactionAsync_Returns409_ReturnsConflict()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.UpdateTransactionAsync(Guid.NewGuid(), new TransactionUpdateDto(), "stale");

        // Assert
        result.IsConflict.ShouldBeTrue();
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
