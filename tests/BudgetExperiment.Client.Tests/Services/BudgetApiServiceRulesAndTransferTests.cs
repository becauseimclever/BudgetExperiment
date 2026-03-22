// <copyright file="BudgetApiServiceRulesAndTransferTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Shouldly;

namespace BudgetExperiment.Client.Tests.Services;

/// <summary>
/// Unit tests for <see cref="BudgetApiService"/> categorization rules, transfers, and import pattern operations.
/// </summary>
public class BudgetApiServiceRulesAndTransferTests
{
    // Categorization Rules

    /// <summary>
    /// Tests that GetCategorizationRulesAsync returns list.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCategorizationRulesAsync_ReturnsList()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<CategorizationRuleDto>
                {
                    new() { Id = Guid.NewGuid(), Name = "Groceries" },
                }),
            }));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.GetCategorizationRulesAsync();

        // Assert
        result.Count.ShouldBe(1);
    }

    /// <summary>
    /// Tests that GetCategorizationRulesAsync includes activeOnly filter.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCategorizationRulesAsync_IncludesActiveOnly()
    {
        // Arrange
        var capturedUrl = string.Empty;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<CategorizationRuleDto>()),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        await service.GetCategorizationRulesAsync(activeOnly: true);

        // Assert
        capturedUrl.ShouldContain("activeOnly=true");
    }

    /// <summary>
    /// Tests that GetCategorizationRuleAsync returns null on error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCategorizationRuleAsync_ReturnsNull_OnHttpError()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("fail"));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.GetCategorizationRuleAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that CreateCategorizationRuleAsync returns rule on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateCategorizationRuleAsync_ReturnsRule_OnSuccess()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = JsonContent.Create(new CategorizationRuleDto { Id = Guid.NewGuid() }),
            }));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.CreateCategorizationRuleAsync(new CategorizationRuleCreateDto());

        // Assert
        result.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that CreateCategorizationRuleAsync returns null on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateCategorizationRuleAsync_ReturnsNull_OnFailure()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.CreateCategorizationRuleAsync(new CategorizationRuleCreateDto());

        // Assert
        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that DeleteCategorizationRuleAsync returns true on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteCategorizationRuleAsync_ReturnsTrue_OnSuccess()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.DeleteCategorizationRuleAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that ActivateCategorizationRuleAsync sends POST.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ActivateCategorizationRuleAsync_ReturnsTrue_OnSuccess()
    {
        // Arrange
        var ruleId = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            request.RequestUri!.PathAndQuery.ShouldContain($"categorizationrules/{ruleId}/activate");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.ActivateCategorizationRuleAsync(ruleId);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that DeactivateCategorizationRuleAsync returns true on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeactivateCategorizationRuleAsync_ReturnsTrue_OnSuccess()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.DeactivateCategorizationRuleAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that TestPatternAsync returns result on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TestPatternAsync_ReturnsResult_OnSuccess()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new TestPatternResponse()),
            }));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.TestPatternAsync(new TestPatternRequest());

        // Assert
        result.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that TestPatternAsync returns null on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TestPatternAsync_ReturnsNull_OnFailure()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.TestPatternAsync(new TestPatternRequest());

        // Assert
        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that ApplyCategorizationRulesAsync returns response on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ApplyCategorizationRulesAsync_ReturnsResult_OnSuccess()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new ApplyRulesResponse()),
            }));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.ApplyCategorizationRulesAsync(new ApplyRulesRequest());

        // Assert
        result.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that ReorderCategorizationRulesAsync sends PUT to correct URL.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ReorderCategorizationRulesAsync_SendsPut()
    {
        // Arrange
        var capturedUrl = string.Empty;
        HttpMethod? capturedMethod = null;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            capturedMethod = request.Method;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.ReorderCategorizationRulesAsync(new List<Guid> { Guid.NewGuid() });

        // Assert
        capturedUrl.ShouldContain("categorizationrules/reorder");
        capturedMethod.ShouldBe(HttpMethod.Put);
        result.ShouldBeTrue();
    }

    // Transfers

    /// <summary>
    /// Tests that CreateTransferAsync returns response on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateTransferAsync_ReturnsResponse_OnSuccess()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            request.Method.ShouldBe(HttpMethod.Post);
            request.RequestUri!.PathAndQuery.ShouldBe("/api/v1/transfers");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = JsonContent.Create(new TransferResponse()),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.CreateTransferAsync(new CreateTransferRequest());

        // Assert
        result.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that GetTransferAsync returns null on error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetTransferAsync_ReturnsNull_OnHttpError()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("fail"));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.GetTransferAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that GetTransfersAsync builds correct URL with all params.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetTransfersAsync_BuildsCorrectUrl_WithAllParams()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var capturedUrl = string.Empty;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new TransferListPageResponse { Items = new List<TransferListItemResponse>() }),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        await service.GetTransfersAsync(accountId, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), 2, 10);

        // Assert
        capturedUrl.ShouldContain("api/v1/transfers");
        capturedUrl.ShouldContain($"accountId={accountId}");
        capturedUrl.ShouldContain("from=2026-01-01");
        capturedUrl.ShouldContain("to=2026-01-31");
        capturedUrl.ShouldContain("page=2");
        capturedUrl.ShouldContain("pageSize=10");
    }

    /// <summary>
    /// Tests that UpdateTransferAsync returns response on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateTransferAsync_ReturnsResponse_OnSuccess()
    {
        // Arrange
        var transferId = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            request.Method.ShouldBe(HttpMethod.Put);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new TransferResponse()),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.UpdateTransferAsync(transferId, new UpdateTransferRequest());

        // Assert
        result.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that DeleteTransferAsync returns true on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteTransferAsync_ReturnsTrue_OnSuccess()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.DeleteTransferAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeTrue();
    }

    // Import Patterns

    /// <summary>
    /// Tests that GetImportPatternsAsync builds correct URL.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetImportPatternsAsync_BuildsCorrectUrl()
    {
        // Arrange
        var rtId = Guid.NewGuid();
        var capturedUrl = string.Empty;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new ImportPatternsDto()),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        await service.GetImportPatternsAsync(rtId);

        // Assert
        capturedUrl.ShouldContain($"recurring-transactions/{rtId}/import-patterns");
    }

    /// <summary>
    /// Tests that GetImportPatternsAsync returns null on error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetImportPatternsAsync_ReturnsNull_OnHttpError()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("fail"));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.GetImportPatternsAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that UpdateImportPatternsAsync returns result on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateImportPatternsAsync_ReturnsResult_OnSuccess()
    {
        // Arrange
        var rtId = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            request.Method.ShouldBe(HttpMethod.Put);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new ImportPatternsDto()),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.UpdateImportPatternsAsync(rtId, new ImportPatternsDto());

        // Assert
        result.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that UpdateImportPatternsAsync returns null on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateImportPatternsAsync_ReturnsNull_OnFailure()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.UpdateImportPatternsAsync(Guid.NewGuid(), new ImportPatternsDto());

        // Assert
        result.ShouldBeNull();
    }

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
