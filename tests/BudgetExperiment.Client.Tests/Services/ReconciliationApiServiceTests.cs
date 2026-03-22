// <copyright file="ReconciliationApiServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Shouldly;

namespace BudgetExperiment.Client.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ReconciliationApiService"/>.
/// </summary>
public class ReconciliationApiServiceTests
{
    /// <summary>
    /// Tests that GetStatusAsync builds the correct URL with year and month parameters.
    /// </summary>
    /// <param name="year">The year to query.</param>
    /// <param name="month">The month to query.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Theory]
    [InlineData(2026, 1)]
    [InlineData(2026, 12)]
    [InlineData(2025, 6)]
    public async Task GetStatusAsync_BuildsCorrectUrl_WithYearAndMonthParams(int year, int month)
    {
        // Arrange
        string? capturedUrl = null;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new ReconciliationStatusDto
                {
                    Year = year,
                    Month = month,
                    TotalExpectedInstances = 0,
                    MatchedCount = 0,
                    PendingCount = 0,
                    MissingCount = 0,
                    Instances = [],
                }),
            };
            return Task.FromResult(response);
        });

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost/"),
        };
        var service = new ReconciliationApiService(httpClient);

        // Act
        await service.GetStatusAsync(year, month);

        // Assert
        capturedUrl.ShouldNotBeNull();
        capturedUrl.ShouldBe($"/api/v1/reconciliation/status?year={year}&month={month}");
    }

    /// <summary>
    /// Tests that GetStatusAsync includes accountId in URL when provided.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetStatusAsync_IncludesAccountId_WhenProvided()
    {
        // Arrange
        var accountId = Guid.Parse("12345678-1234-1234-1234-123456789012");
        string? capturedUrl = null;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new ReconciliationStatusDto
                {
                    Year = 2026,
                    Month = 1,
                    TotalExpectedInstances = 0,
                    MatchedCount = 0,
                    PendingCount = 0,
                    MissingCount = 0,
                    Instances = [],
                }),
            };
            return Task.FromResult(response);
        });

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost/"),
        };
        var service = new ReconciliationApiService(httpClient);

        // Act
        await service.GetStatusAsync(2026, 1, accountId);

        // Assert
        capturedUrl.ShouldNotBeNull();
        capturedUrl.ShouldBe($"/api/v1/reconciliation/status?year=2026&month=1&accountId={accountId}");
    }

    /// <summary>
    /// Tests that GetStatusAsync returns null on HTTP error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetStatusAsync_ReturnsNull_WhenHttpRequestFails()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost/"),
        };
        var service = new ReconciliationApiService(httpClient);

        // Act
        var result = await service.GetStatusAsync(2026, 1);

        // Assert
        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that UnlinkMatchAsync calls the correct DELETE endpoint.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UnlinkMatchAsync_CallsCorrectEndpoint()
    {
        // Arrange
        var matchId = Guid.Parse("12345678-1234-1234-1234-123456789012");
        string? capturedUrl = null;
        string? capturedMethod = null;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            capturedMethod = request.Method.Method;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost/"),
        };
        var service = new ReconciliationApiService(httpClient);

        // Act
        var result = await service.UnlinkMatchAsync(matchId);

        // Assert
        result.ShouldBeTrue();
        capturedMethod.ShouldBe("DELETE");
        capturedUrl.ShouldBe($"/api/v1/reconciliation/matches/{matchId}");
    }

    /// <summary>
    /// Tests that UnlinkMatchAsync returns false on HTTP error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UnlinkMatchAsync_ReturnsFalse_WhenRequestFails()
    {
        // Arrange
        var matchId = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost/"),
        };
        var service = new ReconciliationApiService(httpClient);

        // Act
        var result = await service.UnlinkMatchAsync(matchId);

        // Assert
        result.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that GetLinkableInstancesAsync calls the correct endpoint with transactionId.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetLinkableInstancesAsync_CallsCorrectEndpoint()
    {
        // Arrange
        var transactionId = Guid.Parse("12345678-1234-1234-1234-123456789012");
        string? capturedUrl = null;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<LinkableInstanceDto>
                {
                    new LinkableInstanceDto
                    {
                        RecurringTransactionId = Guid.NewGuid(),
                        Description = "Test Recurring",
                        ExpectedAmount = new MoneyDto { Amount = 100, Currency = "USD" },
                        InstanceDate = new DateOnly(2026, 1, 15),
                        IsAlreadyMatched = false,
                    },
                }),
            };
            return Task.FromResult(response);
        });

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost/"),
        };
        var service = new ReconciliationApiService(httpClient);

        // Act
        var result = await service.GetLinkableInstancesAsync(transactionId);

        // Assert
        capturedUrl.ShouldBe($"/api/v1/reconciliation/linkable-instances?transactionId={transactionId}");
        result.Count.ShouldBe(1);
        result[0].Description.ShouldBe("Test Recurring");
    }

    /// <summary>
    /// Tests that GetLinkableInstancesAsync returns empty list on HTTP error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetLinkableInstancesAsync_ReturnsEmptyList_WhenRequestFails()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost/"),
        };
        var service = new ReconciliationApiService(httpClient);

        // Act
        var result = await service.GetLinkableInstancesAsync(transactionId);

        // Assert
        result.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that GetPendingMatchesAsync calls the correct endpoint without accountId.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetPendingMatchesAsync_NoAccount_UsesBaseUrl()
    {
        // Arrange
        string? capturedUrl = null;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<ReconciliationMatchDto>()),
            };
            return Task.FromResult(response);
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new ReconciliationApiService(httpClient);

        // Act
        await service.GetPendingMatchesAsync();

        // Assert
        capturedUrl.ShouldBe("/api/v1/reconciliation/pending");
    }

    /// <summary>
    /// Tests that GetPendingMatchesAsync includes accountId when provided.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetPendingMatchesAsync_WithAccount_AddsQueryParam()
    {
        // Arrange
        var accountId = Guid.Parse("12345678-1234-1234-1234-123456789012");
        string? capturedUrl = null;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<ReconciliationMatchDto>()),
            };
            return Task.FromResult(response);
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new ReconciliationApiService(httpClient);

        // Act
        await service.GetPendingMatchesAsync(accountId);

        // Assert
        capturedUrl.ShouldBe($"/api/v1/reconciliation/pending?accountId={accountId}");
    }

    /// <summary>
    /// Tests that GetPendingMatchesAsync returns empty list on HTTP failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetPendingMatchesAsync_OnFailure_ReturnsEmptyList()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new ReconciliationApiService(httpClient);

        // Act
        var result = await service.GetPendingMatchesAsync();

        // Assert
        result.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that FindMatchesAsync calls the correct POST endpoint.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task FindMatchesAsync_CallsCorrectEndpoint()
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
                Content = JsonContent.Create(new FindMatchesResult
                {
                    MatchesByTransaction = new Dictionary<Guid, IReadOnlyList<ReconciliationMatchDto>>(),
                    TotalMatchesFound = 0,
                    HighConfidenceCount = 0,
                }),
            };
            return Task.FromResult(response);
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new ReconciliationApiService(httpClient);

        // Act
        var result = await service.FindMatchesAsync(new FindMatchesRequest
        {
            TransactionIds = [Guid.NewGuid()],
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 1, 31),
        });

        // Assert
        capturedMethod.ShouldBe("POST");
        capturedUrl.ShouldBe("/api/v1/reconciliation/find-matches");
        result.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that FindMatchesAsync returns null on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task FindMatchesAsync_OnFailure_ReturnsNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new ReconciliationApiService(httpClient);

        // Act
        var result = await service.FindMatchesAsync(new FindMatchesRequest
        {
            TransactionIds = [],
            StartDate = default,
            EndDate = default,
        });

        // Assert
        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that AcceptMatchAsync calls the correct POST endpoint.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AcceptMatchAsync_OnSuccess_ReturnsTrue()
    {
        // Arrange
        var matchId = Guid.Parse("12345678-1234-1234-1234-123456789012");
        string? capturedUrl = null;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new ReconciliationApiService(httpClient);

        // Act
        var result = await service.AcceptMatchAsync(matchId);

        // Assert
        result.ShouldBeTrue();
        capturedUrl.ShouldBe($"/api/v1/reconciliation/{matchId}/accept");
    }

    /// <summary>
    /// Tests that AcceptMatchAsync returns false on HTTP failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AcceptMatchAsync_OnFailure_ReturnsFalse()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new ReconciliationApiService(httpClient);

        // Act
        var result = await service.AcceptMatchAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that RejectMatchAsync calls the correct POST endpoint.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RejectMatchAsync_OnSuccess_ReturnsTrue()
    {
        // Arrange
        var matchId = Guid.Parse("12345678-1234-1234-1234-123456789012");
        string? capturedUrl = null;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new ReconciliationApiService(httpClient);

        // Act
        var result = await service.RejectMatchAsync(matchId);

        // Assert
        result.ShouldBeTrue();
        capturedUrl.ShouldBe($"/api/v1/reconciliation/{matchId}/reject");
    }

    /// <summary>
    /// Tests that RejectMatchAsync returns false on HTTP failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RejectMatchAsync_OnFailure_ReturnsFalse()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new ReconciliationApiService(httpClient);

        // Act
        var result = await service.RejectMatchAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that BulkAcceptMatchesAsync returns accepted count on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task BulkAcceptMatchesAsync_OnSuccess_ReturnsCount()
    {
        // Arrange
        string? capturedUrl = null;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new BulkMatchActionResult
                {
                    AcceptedCount = 3,
                    FailedCount = 0,
                }),
            };
            return Task.FromResult(response);
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new ReconciliationApiService(httpClient);

        // Act
        var result = await service.BulkAcceptMatchesAsync([Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()]);

        // Assert
        result.ShouldBe(3);
        capturedUrl.ShouldBe("/api/v1/reconciliation/bulk-accept");
    }

    /// <summary>
    /// Tests that BulkAcceptMatchesAsync returns zero on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task BulkAcceptMatchesAsync_OnFailure_ReturnsZero()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new ReconciliationApiService(httpClient);

        // Act
        var result = await service.BulkAcceptMatchesAsync([Guid.NewGuid()]);

        // Assert
        result.ShouldBe(0);
    }

    /// <summary>
    /// Tests that CreateManualMatchAsync returns match on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateManualMatchAsync_OnSuccess_ReturnsMatch()
    {
        // Arrange
        var matchId = Guid.NewGuid();
        string? capturedUrl = null;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new ReconciliationMatchDto
                {
                    Id = matchId,
                    ImportedTransactionId = Guid.NewGuid(),
                    RecurringTransactionId = Guid.NewGuid(),
                    RecurringInstanceDate = new DateOnly(2026, 1, 15),
                    ConfidenceScore = 1.0m,
                    ConfidenceLevel = "High",
                    Status = "Accepted",
                    Source = "Manual",
                }),
            };
            return Task.FromResult(response);
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new ReconciliationApiService(httpClient);

        // Act
        var result = await service.CreateManualMatchAsync(new ManualMatchRequest
        {
            TransactionId = Guid.NewGuid(),
            RecurringTransactionId = Guid.NewGuid(),
            InstanceDate = new DateOnly(2026, 1, 15),
        });

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(matchId);
        capturedUrl.ShouldBe("/api/v1/reconciliation/manual-match");
    }

    /// <summary>
    /// Tests that CreateManualMatchAsync returns null on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateManualMatchAsync_OnFailure_ReturnsNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new ReconciliationApiService(httpClient);

        // Act
        var result = await service.CreateManualMatchAsync(new ManualMatchRequest
        {
            TransactionId = Guid.NewGuid(),
            RecurringTransactionId = Guid.NewGuid(),
            InstanceDate = new DateOnly(2026, 1, 15),
        });

        // Assert
        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that GetTolerancesAsync returns tolerances on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetTolerancesAsync_OnSuccess_ReturnsTolerance()
    {
        // Arrange
        string? capturedUrl = null;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new MatchingTolerancesDto
                {
                    DateToleranceDays = 3,
                    AmountTolerancePercent = 0.05m,
                    AmountToleranceAbsolute = 1.0m,
                    DescriptionSimilarityThreshold = 0.7m,
                    AutoMatchThreshold = 0.9m,
                }),
            };
            return Task.FromResult(response);
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new ReconciliationApiService(httpClient);

        // Act
        var result = await service.GetTolerancesAsync();

        // Assert
        result.ShouldNotBeNull();
        result.DateToleranceDays.ShouldBe(3);
        capturedUrl.ShouldBe("/api/v1/reconciliation/tolerances");
    }

    /// <summary>
    /// Tests that GetTolerancesAsync returns null on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetTolerancesAsync_OnFailure_ReturnsNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new ReconciliationApiService(httpClient);

        // Act
        var result = await service.GetTolerancesAsync();

        // Assert
        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that UpdateTolerancesAsync returns true on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateTolerancesAsync_OnSuccess_ReturnsTrue()
    {
        // Arrange
        string? capturedUrl = null;
        string? capturedMethod = null;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            capturedMethod = request.Method.Method;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new ReconciliationApiService(httpClient);

        // Act
        var result = await service.UpdateTolerancesAsync(new MatchingTolerancesDto
        {
            DateToleranceDays = 5,
            AmountTolerancePercent = 0.1m,
            AmountToleranceAbsolute = 2.0m,
            DescriptionSimilarityThreshold = 0.8m,
            AutoMatchThreshold = 0.95m,
        });

        // Assert
        result.ShouldBeTrue();
        capturedMethod.ShouldBe("PUT");
        capturedUrl.ShouldBe("/api/v1/reconciliation/tolerances");
    }

    /// <summary>
    /// Tests that UpdateTolerancesAsync returns false on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateTolerancesAsync_OnFailure_ReturnsFalse()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new ReconciliationApiService(httpClient);

        // Act
        var result = await service.UpdateTolerancesAsync(new MatchingTolerancesDto());

        // Assert
        result.ShouldBeFalse();
    }

    /// <summary>
    /// Simple mock HTTP message handler for testing.
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
