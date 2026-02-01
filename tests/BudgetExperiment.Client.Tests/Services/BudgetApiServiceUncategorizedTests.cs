// <copyright file="BudgetApiServiceUncategorizedTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Shouldly;

namespace BudgetExperiment.Client.Tests.Services;

/// <summary>
/// Unit tests for <see cref="BudgetApiService"/> uncategorized transaction methods.
/// </summary>
public class BudgetApiServiceUncategorizedTests
{
    /// <summary>
    /// Tests that GetUncategorizedTransactionsAsync builds the correct URL with filter params.
    /// </summary>
    [Fact]
    public async Task GetUncategorizedTransactionsAsync_BuildsCorrectUrl_WithDefaultFilter()
    {
        // Arrange
        string? capturedUrl = null;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new UncategorizedTransactionPageDto
                {
                    Items = [],
                    TotalCount = 0,
                    Page = 1,
                    PageSize = 50,
                }),
            };
            return Task.FromResult(response);
        });

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost/"),
        };
        var service = new BudgetApiService(httpClient);
        var filter = new UncategorizedTransactionFilterDto();

        // Act
        await service.GetUncategorizedTransactionsAsync(filter);

        // Assert
        capturedUrl.ShouldNotBeNull();
        capturedUrl.ShouldContain("/api/v1/transactions/uncategorized");
        capturedUrl.ShouldContain("page=1");
        capturedUrl.ShouldContain("pageSize=50");
        capturedUrl.ShouldContain("sortBy=Date");
        capturedUrl.ShouldContain("sortDescending=True");
    }

    /// <summary>
    /// Tests that GetUncategorizedTransactionsAsync includes all filter parameters.
    /// </summary>
    [Fact]
    public async Task GetUncategorizedTransactionsAsync_IncludesAllFilterParams()
    {
        // Arrange
        var accountId = Guid.Parse("12345678-1234-1234-1234-123456789012");
        string? capturedUrl = null;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new UncategorizedTransactionPageDto()),
            };
            return Task.FromResult(response);
        });

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost/"),
        };
        var service = new BudgetApiService(httpClient);
        var filter = new UncategorizedTransactionFilterDto
        {
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 1, 31),
            MinAmount = 10.0m,
            MaxAmount = 100.0m,
            DescriptionContains = "grocery",
            AccountId = accountId,
            SortBy = "Amount",
            SortDescending = false,
            Page = 2,
            PageSize = 25,
        };

        // Act
        await service.GetUncategorizedTransactionsAsync(filter);

        // Assert
        capturedUrl.ShouldNotBeNull();
        capturedUrl.ShouldContain("startDate=2026-01-01");
        capturedUrl.ShouldContain("endDate=2026-01-31");
        capturedUrl.ShouldContain("minAmount=10.0");
        capturedUrl.ShouldContain("maxAmount=100.0");
        capturedUrl.ShouldContain("descriptionContains=grocery");
        capturedUrl.ShouldContain($"accountId={accountId}");
        capturedUrl.ShouldContain("sortBy=Amount");
        capturedUrl.ShouldContain("sortDescending=False");
        capturedUrl.ShouldContain("page=2");
        capturedUrl.ShouldContain("pageSize=25");
    }

    /// <summary>
    /// Tests that GetUncategorizedTransactionsAsync returns empty page on HTTP error.
    /// </summary>
    [Fact]
    public async Task GetUncategorizedTransactionsAsync_ReturnsEmptyPage_OnHttpError()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost/"),
        };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.GetUncategorizedTransactionsAsync(new UncategorizedTransactionFilterDto());

        // Assert
        result.ShouldNotBeNull();
        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
    }

    /// <summary>
    /// Tests that BulkCategorizeTransactionsAsync calls the correct POST endpoint.
    /// </summary>
    [Fact]
    public async Task BulkCategorizeTransactionsAsync_CallsCorrectEndpoint()
    {
        // Arrange
        string? capturedUrl = null;
        string? capturedMethod = null;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            capturedMethod = request.Method.Method;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new BulkCategorizeResponse
                {
                    TotalRequested = 3,
                    SuccessCount = 3,
                    FailedCount = 0,
                    Errors = [],
                }),
            };
            return Task.FromResult(response);
        });

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost/"),
        };
        var service = new BudgetApiService(httpClient);
        var request = new BulkCategorizeRequest
        {
            TransactionIds = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()],
            CategoryId = Guid.NewGuid(),
        };

        // Act
        await service.BulkCategorizeTransactionsAsync(request);

        // Assert
        capturedUrl.ShouldBe("/api/v1/transactions/bulk-categorize");
        capturedMethod.ShouldBe("POST");
    }

    /// <summary>
    /// Tests that BulkCategorizeTransactionsAsync returns error response on HTTP failure.
    /// </summary>
    [Fact]
    public async Task BulkCategorizeTransactionsAsync_ReturnsErrorResponse_OnHttpFailure()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost/"),
        };
        var service = new BudgetApiService(httpClient);
        var request = new BulkCategorizeRequest
        {
            TransactionIds = [Guid.NewGuid()],
            CategoryId = Guid.NewGuid(),
        };

        // Act
        var result = await service.BulkCategorizeTransactionsAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.SuccessCount.ShouldBe(0);
        result.FailedCount.ShouldBe(1);
        result.Errors.ShouldNotBeEmpty();
        result.Errors[0].ShouldContain("BadRequest");
    }

    /// <summary>
    /// Tests that BulkCategorizeTransactionsAsync returns success response.
    /// </summary>
    [Fact]
    public async Task BulkCategorizeTransactionsAsync_ReturnsSuccessResponse()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new BulkCategorizeResponse
                {
                    TotalRequested = 5,
                    SuccessCount = 4,
                    FailedCount = 1,
                    Errors = ["Transaction not found"],
                }),
            };
            return Task.FromResult(response);
        });

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost/"),
        };
        var service = new BudgetApiService(httpClient);
        var request = new BulkCategorizeRequest
        {
            TransactionIds = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()],
            CategoryId = Guid.NewGuid(),
        };

        // Act
        var result = await service.BulkCategorizeTransactionsAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.TotalRequested.ShouldBe(5);
        result.SuccessCount.ShouldBe(4);
        result.FailedCount.ShouldBe(1);
        result.Errors.Count.ShouldBe(1);
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
