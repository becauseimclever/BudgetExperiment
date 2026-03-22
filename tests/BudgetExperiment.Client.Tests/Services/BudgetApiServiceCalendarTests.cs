// <copyright file="BudgetApiServiceCalendarTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Shouldly;

namespace BudgetExperiment.Client.Tests.Services;

/// <summary>
/// Unit tests for <see cref="BudgetApiService"/> calendar operations.
/// </summary>
public class BudgetApiServiceCalendarTests
{
    /// <summary>
    /// Tests that GetCalendarGridAsync builds correct URL.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCalendarGridAsync_BuildsCorrectUrl()
    {
        // Arrange
        var capturedUrl = string.Empty;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new CalendarGridDto { Year = 2026, Month = 3 }),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        await service.GetCalendarGridAsync(2026, 3);

        // Assert
        capturedUrl.ShouldContain("api/v1/calendar/grid");
        capturedUrl.ShouldContain("year=2026");
        capturedUrl.ShouldContain("month=3");
    }

    /// <summary>
    /// Tests that GetCalendarGridAsync includes accountId when provided.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCalendarGridAsync_IncludesAccountId()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var capturedUrl = string.Empty;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new CalendarGridDto { Year = 2026, Month = 3 }),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        await service.GetCalendarGridAsync(2026, 3, accountId);

        // Assert
        capturedUrl.ShouldContain($"accountId={accountId}");
    }

    /// <summary>
    /// Tests that GetCalendarGridAsync returns default on null response.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCalendarGridAsync_ReturnsDefault_OnNullResponse()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create<CalendarGridDto?>(null),
            }));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.GetCalendarGridAsync(2026, 3);

        // Assert
        result.Year.ShouldBe(2026);
        result.Month.ShouldBe(3);
    }

    /// <summary>
    /// Tests that GetDayDetailAsync builds correct URL.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetDayDetailAsync_BuildsCorrectUrl()
    {
        // Arrange
        var capturedUrl = string.Empty;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new DayDetailDto { Date = new DateOnly(2026, 3, 15) }),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        await service.GetDayDetailAsync(new DateOnly(2026, 3, 15));

        // Assert
        capturedUrl.ShouldContain("api/v1/calendar/day/2026-03-15");
    }

    /// <summary>
    /// Tests that GetDayDetailAsync includes accountId when provided.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetDayDetailAsync_IncludesAccountId()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var capturedUrl = string.Empty;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new DayDetailDto { Date = new DateOnly(2026, 3, 15) }),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        await service.GetDayDetailAsync(new DateOnly(2026, 3, 15), accountId);

        // Assert
        capturedUrl.ShouldContain($"accountId={accountId}");
    }

    /// <summary>
    /// Tests that GetAccountTransactionListAsync builds correct URL.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetAccountTransactionListAsync_BuildsCorrectUrl()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var capturedUrl = string.Empty;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new TransactionListDto
                {
                    AccountId = accountId,
                    StartDate = new DateOnly(2026, 1, 1),
                    EndDate = new DateOnly(2026, 1, 31),
                }),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        await service.GetAccountTransactionListAsync(accountId, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        // Assert
        capturedUrl.ShouldContain($"api/v1/calendar/accounts/{accountId}/transactions");
        capturedUrl.ShouldContain("includeRecurring=True");
    }

    /// <summary>
    /// Tests that GetCalendarSummaryAsync builds correct URL.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCalendarSummaryAsync_BuildsCorrectUrl()
    {
        // Arrange
        var capturedUrl = string.Empty;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<DailyTotalDto>()),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        await service.GetCalendarSummaryAsync(2026, 3);

        // Assert
        capturedUrl.ShouldContain("api/v1/calendar/summary");
        capturedUrl.ShouldContain("year=2026");
        capturedUrl.ShouldContain("month=3");
    }

    /// <summary>
    /// Tests that GetCalendarSummaryAsync includes accountId.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCalendarSummaryAsync_IncludesAccountId()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var capturedUrl = string.Empty;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<DailyTotalDto>()),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        await service.GetCalendarSummaryAsync(2026, 3, accountId);

        // Assert
        capturedUrl.ShouldContain($"accountId={accountId}");
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
