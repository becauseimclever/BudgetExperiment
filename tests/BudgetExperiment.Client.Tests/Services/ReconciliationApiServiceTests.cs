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
    /// Simple mock HTTP message handler for testing.
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
