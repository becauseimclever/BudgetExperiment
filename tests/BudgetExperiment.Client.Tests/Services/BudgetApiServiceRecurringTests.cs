// <copyright file="BudgetApiServiceRecurringTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Shouldly;

namespace BudgetExperiment.Client.Tests.Services;

/// <summary>
/// Unit tests for <see cref="BudgetApiService"/> recurring transaction and transfer operations.
/// </summary>
public class BudgetApiServiceRecurringTests
{
    /// <summary>
    /// Tests that GetRecurringTransactionsAsync returns list on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetRecurringTransactionsAsync_ReturnsList_OnSuccess()
    {
        // Arrange
        var items = new List<RecurringTransactionDto> { new() { Id = Guid.NewGuid(), Description = "Rent" } };
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(items) }));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.GetRecurringTransactionsAsync();

        // Assert
        result.Count.ShouldBe(1);
        result[0].Description.ShouldBe("Rent");
    }

    /// <summary>
    /// Tests that GetRecurringTransactionAsync returns null on error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetRecurringTransactionAsync_ReturnsNull_OnHttpError()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("fail"));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.GetRecurringTransactionAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that CreateRecurringTransactionAsync returns transaction on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateRecurringTransactionAsync_ReturnsDto_OnSuccess()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = JsonContent.Create(new RecurringTransactionDto { Id = expectedId }),
            }));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.CreateRecurringTransactionAsync(new RecurringTransactionCreateDto());

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(expectedId);
    }

    /// <summary>
    /// Tests that CreateRecurringTransactionAsync returns null on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateRecurringTransactionAsync_ReturnsNull_OnFailure()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.CreateRecurringTransactionAsync(new RecurringTransactionCreateDto());

        // Assert
        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that DeleteRecurringTransactionAsync returns true on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteRecurringTransactionAsync_ReturnsTrue_OnSuccess()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.DeleteRecurringTransactionAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that PauseRecurringTransactionAsync returns updated DTO on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task PauseRecurringTransactionAsync_ReturnsDto_OnSuccess()
    {
        // Arrange
        var id = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            request.Method.ShouldBe(HttpMethod.Post);
            request.RequestUri!.PathAndQuery.ShouldContain($"recurring-transactions/{id}/pause");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new RecurringTransactionDto { Id = id }),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.PauseRecurringTransactionAsync(id);

        // Assert
        result.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that PauseRecurringTransactionAsync returns null on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task PauseRecurringTransactionAsync_ReturnsNull_OnFailure()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.PauseRecurringTransactionAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that ResumeRecurringTransactionAsync returns DTO on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ResumeRecurringTransactionAsync_ReturnsDto_OnSuccess()
    {
        // Arrange
        var id = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            request.RequestUri!.PathAndQuery.ShouldContain($"recurring-transactions/{id}/resume");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new RecurringTransactionDto { Id = id }),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.ResumeRecurringTransactionAsync(id);

        // Assert
        result.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that SkipNextRecurringAsync returns DTO on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SkipNextRecurringAsync_ReturnsDto_OnSuccess()
    {
        // Arrange
        var id = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            request.RequestUri!.PathAndQuery.ShouldContain($"recurring-transactions/{id}/skip");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new RecurringTransactionDto { Id = id }),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.SkipNextRecurringAsync(id);

        // Assert
        result.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that GetProjectedRecurringAsync builds correct URL.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetProjectedRecurringAsync_BuildsCorrectUrl()
    {
        // Arrange
        var capturedUrl = string.Empty;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<RecurringInstanceDto>()),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        await service.GetProjectedRecurringAsync(new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31));

        // Assert
        capturedUrl.ShouldContain("api/v1/recurring-transactions/projected");
        capturedUrl.ShouldContain("from=2026-01-01");
        capturedUrl.ShouldContain("to=2026-03-31");
    }

    /// <summary>
    /// Tests that GetProjectedRecurringAsync includes accountId.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetProjectedRecurringAsync_IncludesAccountId()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var capturedUrl = string.Empty;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<RecurringInstanceDto>()),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        await service.GetProjectedRecurringAsync(new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31), accountId);

        // Assert
        capturedUrl.ShouldContain($"accountId={accountId}");
    }

    /// <summary>
    /// Tests that SkipRecurringInstanceAsync sends DELETE with correct URL.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SkipRecurringInstanceAsync_SendsDelete()
    {
        // Arrange
        var id = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            request.Method.ShouldBe(HttpMethod.Delete);
            request.RequestUri!.PathAndQuery.ShouldContain($"recurring-transactions/{id}/instances/2026-03-15");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.SkipRecurringInstanceAsync(id, new DateOnly(2026, 3, 15));

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that RealizeRecurringTransactionAsync returns transaction on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RealizeRecurringTransactionAsync_ReturnsTransaction_OnSuccess()
    {
        // Arrange
        var rtId = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            request.RequestUri!.PathAndQuery.ShouldContain($"recurring-transactions/{rtId}/realize");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new TransactionDto { Id = Guid.NewGuid() }),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.RealizeRecurringTransactionAsync(rtId, new RealizeRecurringTransactionRequest());

        // Assert
        result.ShouldNotBeNull();
    }

    // Recurring Transfers

    /// <summary>
    /// Tests that GetRecurringTransfersAsync returns list on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetRecurringTransfersAsync_ReturnsList()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<RecurringTransferDto> { new() { Id = Guid.NewGuid() } }),
            }));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.GetRecurringTransfersAsync();

        // Assert
        result.Count.ShouldBe(1);
    }

    /// <summary>
    /// Tests that GetRecurringTransfersAsync includes accountId filter.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetRecurringTransfersAsync_IncludesAccountId()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var capturedUrl = string.Empty;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<RecurringTransferDto>()),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        await service.GetRecurringTransfersAsync(accountId);

        // Assert
        capturedUrl.ShouldContain($"accountId={accountId}");
    }

    /// <summary>
    /// Tests that GetRecurringTransferAsync returns null on error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetRecurringTransferAsync_ReturnsNull_OnHttpError()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("fail"));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.GetRecurringTransferAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that CreateRecurringTransferAsync returns DTO on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateRecurringTransferAsync_ReturnsDto_OnSuccess()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = JsonContent.Create(new RecurringTransferDto { Id = Guid.NewGuid() }),
            }));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.CreateRecurringTransferAsync(new RecurringTransferCreateDto());

        // Assert
        result.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that DeleteRecurringTransferAsync returns true on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteRecurringTransferAsync_ReturnsTrue_OnSuccess()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.DeleteRecurringTransferAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that PauseRecurringTransferAsync returns DTO on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task PauseRecurringTransferAsync_ReturnsDto_OnSuccess()
    {
        // Arrange
        var id = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            request.RequestUri!.PathAndQuery.ShouldContain($"recurring-transfers/{id}/pause");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new RecurringTransferDto { Id = id }),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.PauseRecurringTransferAsync(id);

        // Assert
        result.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that ResumeRecurringTransferAsync returns DTO on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ResumeRecurringTransferAsync_ReturnsDto_OnSuccess()
    {
        // Arrange
        var id = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new RecurringTransferDto { Id = id }),
            }));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.ResumeRecurringTransferAsync(id);

        // Assert
        result.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that SkipNextRecurringTransferAsync returns DTO on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SkipNextRecurringTransferAsync_ReturnsDto_OnSuccess()
    {
        // Arrange
        var id = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new RecurringTransferDto { Id = id }),
            }));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.SkipNextRecurringTransferAsync(id);

        // Assert
        result.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that RealizeRecurringTransferAsync returns response on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RealizeRecurringTransferAsync_ReturnsDto_OnSuccess()
    {
        // Arrange
        var id = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new TransferResponse()),
            }));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.RealizeRecurringTransferAsync(id, new RealizeRecurringTransferRequest());

        // Assert
        result.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that GetPastDueItemsAsync builds correct URL without accountId.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetPastDueItemsAsync_BuildsUrl_WithoutAccountId()
    {
        // Arrange
        var capturedUrl = string.Empty;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new PastDueSummaryDto()),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        await service.GetPastDueItemsAsync();

        // Assert
        capturedUrl.ShouldBe("/api/v1/recurring/past-due");
    }

    /// <summary>
    /// Tests that GetPastDueItemsAsync builds correct URL with accountId.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetPastDueItemsAsync_BuildsUrl_WithAccountId()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var capturedUrl = string.Empty;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new PastDueSummaryDto()),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        await service.GetPastDueItemsAsync(accountId);

        // Assert
        capturedUrl.ShouldContain($"accountId={accountId}");
    }

    /// <summary>
    /// Tests that RealizeBatchAsync returns result on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RealizeBatchAsync_ReturnsResult_OnSuccess()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new BatchRealizeResultDto()),
            }));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.RealizeBatchAsync(new BatchRealizeRequest());

        // Assert
        result.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that RealizeBatchAsync returns null on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RealizeBatchAsync_ReturnsNull_OnFailure()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.RealizeBatchAsync(new BatchRealizeRequest());

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
