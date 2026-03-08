// <copyright file="BudgetApiServiceTransactionTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Shouldly;

namespace BudgetExperiment.Client.Tests.Services;

/// <summary>
/// Unit tests for <see cref="BudgetApiService"/> transaction operations.
/// </summary>
public class BudgetApiServiceTransactionTests
{
    /// <summary>
    /// Tests that GetTransactionsAsync builds correct URL with date range.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetTransactionsAsync_BuildsCorrectUrl_WithDateRange()
    {
        // Arrange
        var capturedUrl = string.Empty;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<TransactionDto>()),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        await service.GetTransactionsAsync(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        // Assert
        capturedUrl.ShouldNotBeNull();
        capturedUrl.ShouldContain("startDate=2026-01-01");
        capturedUrl.ShouldContain("endDate=2026-01-31");
    }

    /// <summary>
    /// Tests that GetTransactionsAsync includes accountId when provided.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetTransactionsAsync_IncludesAccountId_WhenProvided()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var capturedUrl = string.Empty;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<TransactionDto>()),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        await service.GetTransactionsAsync(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), accountId);

        // Assert
        capturedUrl.ShouldContain($"accountId={accountId}");
    }

    /// <summary>
    /// Tests that GetTransactionsAsync returns transactions on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetTransactionsAsync_ReturnsTransactions_OnSuccess()
    {
        // Arrange
        var transactions = new List<TransactionDto>
        {
            new() { Id = Guid.NewGuid(), Description = "Grocery" },
            new() { Id = Guid.NewGuid(), Description = "Gas" },
        };
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(transactions),
            }));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.GetTransactionsAsync(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        // Assert
        result.Count.ShouldBe(2);
    }

    /// <summary>
    /// Tests that GetTransactionAsync returns transaction on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetTransactionAsync_ReturnsTransaction_OnSuccess()
    {
        // Arrange
        var txId = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new TransactionDto { Id = txId, Description = "Test" }),
            }));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.GetTransactionAsync(txId);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(txId);
    }

    /// <summary>
    /// Tests that GetTransactionAsync returns null on HTTP error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetTransactionAsync_ReturnsNull_OnHttpError()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("fail"));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.GetTransactionAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that CreateTransactionAsync returns transaction on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateTransactionAsync_ReturnsTransaction_OnSuccess()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            request.Method.ShouldBe(HttpMethod.Post);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = JsonContent.Create(new TransactionDto { Id = expectedId, Description = "New" }),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.CreateTransactionAsync(new TransactionCreateDto { Description = "New" });

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(expectedId);
    }

    /// <summary>
    /// Tests that CreateTransactionAsync returns null on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateTransactionAsync_ReturnsNull_OnFailure()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.CreateTransactionAsync(new TransactionCreateDto());

        // Assert
        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that DeleteTransactionAsync returns true on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteTransactionAsync_ReturnsTrue_OnSuccess()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.DeleteTransactionAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that DeleteTransactionAsync returns false on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteTransactionAsync_ReturnsFalse_OnFailure()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.DeleteTransactionAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that UpdateTransactionLocationAsync sends PATCH request.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateTransactionLocationAsync_SendsPatchRequest()
    {
        // Arrange
        var txId = Guid.NewGuid();
        HttpMethod? capturedMethod = null;
        var capturedUrl = string.Empty;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedMethod = request.Method;
            capturedUrl = request.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new TransactionDto { Id = txId }),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        await service.UpdateTransactionLocationAsync(txId, new TransactionLocationUpdateDto());

        // Assert
        capturedMethod.ShouldBe(HttpMethod.Patch);
        capturedUrl.ShouldContain($"/api/v1/transactions/{txId}/location");
    }

    /// <summary>
    /// Tests that ClearTransactionLocationAsync sends DELETE request.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ClearTransactionLocationAsync_ReturnsTrue_OnSuccess()
    {
        // Arrange
        var txId = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            request.Method.ShouldBe(HttpMethod.Delete);
            request.RequestUri!.PathAndQuery.ShouldContain($"transactions/{txId}/location");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.ClearTransactionLocationAsync(txId);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that ClearTransactionLocationAsync returns false on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ClearTransactionLocationAsync_ReturnsFalse_OnFailure()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.ClearTransactionLocationAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that ReverseGeocodeAsync returns result on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ReverseGeocodeAsync_ReturnsResult_OnSuccess()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            request.Method.ShouldBe(HttpMethod.Post);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new ReverseGeocodeResponseDto { FormattedAddress = "123 Main St" }),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.ReverseGeocodeAsync(40.7128m, -74.0060m);

        // Assert
        result.ShouldNotBeNull();
        result.FormattedAddress.ShouldBe("123 Main St");
    }

    /// <summary>
    /// Tests that ReverseGeocodeAsync returns null on NoContent.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ReverseGeocodeAsync_ReturnsNull_OnNoContent()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.ReverseGeocodeAsync(0, 0);

        // Assert
        result.ShouldBeNull();
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
