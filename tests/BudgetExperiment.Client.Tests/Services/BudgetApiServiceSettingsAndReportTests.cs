// <copyright file="BudgetApiServiceSettingsAndReportTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Shouldly;

namespace BudgetExperiment.Client.Tests.Services;

/// <summary>
/// Unit tests for <see cref="BudgetApiService"/> settings, reports, and custom report operations.
/// </summary>
public class BudgetApiServiceSettingsAndReportTests
{
    // Settings

    /// <summary>
    /// Tests that GetSettingsAsync returns settings on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetSettingsAsync_ReturnsDto_OnSuccess()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new AppSettingsDto()),
            }));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.GetSettingsAsync();

        // Assert
        result.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that GetSettingsAsync returns null on error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetSettingsAsync_ReturnsNull_OnHttpError()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("fail"));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.GetSettingsAsync();

        // Assert
        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that UpdateSettingsAsync returns settings on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateSettingsAsync_ReturnsDto_OnSuccess()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            request.Method.ShouldBe(HttpMethod.Put);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new AppSettingsDto()),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.UpdateSettingsAsync(new AppSettingsUpdateDto());

        // Assert
        result.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that UpdateSettingsAsync returns null on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateSettingsAsync_ReturnsNull_OnFailure()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.UpdateSettingsAsync(new AppSettingsUpdateDto());

        // Assert
        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that DeleteAllLocationDataAsync returns result on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteAllLocationDataAsync_ReturnsResult_OnSuccess()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            request.Method.ShouldBe(HttpMethod.Delete);
            request.RequestUri!.PathAndQuery.ShouldBe("/api/v1/settings/location-data");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new LocationDataClearedDto()),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.DeleteAllLocationDataAsync();

        // Assert
        result.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that DeleteAllLocationDataAsync returns null on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteAllLocationDataAsync_ReturnsNull_OnFailure()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.DeleteAllLocationDataAsync();

        // Assert
        result.ShouldBeNull();
    }

    // User Settings

    /// <summary>
    /// Tests that GetUserSettingsAsync returns settings.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetUserSettingsAsync_ReturnsDto_OnSuccess()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new UserSettingsDto()),
            }));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.GetUserSettingsAsync();

        // Assert
        result.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that GetUserSettingsAsync returns null on error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetUserSettingsAsync_ReturnsNull_OnHttpError()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("fail"));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.GetUserSettingsAsync();

        // Assert
        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that UpdateUserSettingsAsync returns settings on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateUserSettingsAsync_ReturnsDto_OnSuccess()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new UserSettingsDto()),
            }));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.UpdateUserSettingsAsync(new UserSettingsUpdateDto());

        // Assert
        result.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that CompleteOnboardingAsync sends POST and returns settings.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CompleteOnboardingAsync_ReturnsDto_OnSuccess()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            request.Method.ShouldBe(HttpMethod.Post);
            request.RequestUri!.PathAndQuery.ShouldContain("user/settings/complete-onboarding");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new UserSettingsDto()),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.CompleteOnboardingAsync();

        // Assert
        result.ShouldNotBeNull();
    }

    // Paycheck Allocation

    /// <summary>
    /// Tests that GetPaycheckAllocationAsync builds correct URL with all params.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetPaycheckAllocationAsync_BuildsCorrectUrl()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var capturedUrl = string.Empty;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new PaycheckAllocationSummaryDto()),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        await service.GetPaycheckAllocationAsync("biweekly", 2500m, accountId);

        // Assert
        capturedUrl.ShouldContain("api/v1/allocations/paycheck");
        capturedUrl.ShouldContain("frequency=biweekly");
        capturedUrl.ShouldContain("amount=2500");
        capturedUrl.ShouldContain($"accountId={accountId}");
    }

    /// <summary>
    /// Tests that GetPaycheckAllocationAsync returns null on error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetPaycheckAllocationAsync_ReturnsNull_OnHttpError()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("fail"));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.GetPaycheckAllocationAsync("monthly");

        // Assert
        result.ShouldBeNull();
    }

    // Reports

    /// <summary>
    /// Tests that GetMonthlyCategoryReportAsync returns report on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetMonthlyCategoryReportAsync_ReturnsDto_OnSuccess()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            request.RequestUri!.PathAndQuery.ShouldContain("reports/categories/monthly");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new MonthlyCategoryReportDto()),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.GetMonthlyCategoryReportAsync(2026, 3);

        // Assert
        result.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that GetMonthlyCategoryReportAsync returns null on error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetMonthlyCategoryReportAsync_ReturnsNull_OnHttpError()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("fail"));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.GetMonthlyCategoryReportAsync(2026, 3);

        // Assert
        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that GetCategoryReportByRangeAsync builds URL with optional accountId.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCategoryReportByRangeAsync_BuildsCorrectUrl()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var capturedUrl = string.Empty;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new DateRangeCategoryReportDto()),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        await service.GetCategoryReportByRangeAsync(new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31), accountId);

        // Assert
        capturedUrl.ShouldContain("reports/categories/range");
        capturedUrl.ShouldContain("startDate=2026-01-01");
        capturedUrl.ShouldContain("endDate=2026-03-31");
        capturedUrl.ShouldContain($"accountId={accountId}");
    }

    /// <summary>
    /// Tests that GetCategoryReportByRangeAsync returns null on error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCategoryReportByRangeAsync_ReturnsNull_OnHttpError()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("fail"));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.GetCategoryReportByRangeAsync(new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31));

        // Assert
        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that GetSpendingTrendsAsync builds URL with all optional params.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetSpendingTrendsAsync_BuildsCorrectUrl_WithAllParams()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var capturedUrl = string.Empty;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new SpendingTrendsReportDto()),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        await service.GetSpendingTrendsAsync(12, 2026, 3, categoryId);

        // Assert
        capturedUrl.ShouldContain("reports/trends");
        capturedUrl.ShouldContain("months=12");
        capturedUrl.ShouldContain("endYear=2026");
        capturedUrl.ShouldContain("endMonth=3");
        capturedUrl.ShouldContain($"categoryId={categoryId}");
    }

    /// <summary>
    /// Tests that GetDaySummaryAsync builds correct URL.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetDaySummaryAsync_BuildsCorrectUrl()
    {
        // Arrange
        var capturedUrl = string.Empty;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new DaySummaryDto()),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        await service.GetDaySummaryAsync(new DateOnly(2026, 3, 15));

        // Assert
        capturedUrl.ShouldContain("reports/day-summary/2026-03-15");
    }

    /// <summary>
    /// Tests that GetDaySummaryAsync includes accountId when provided.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetDaySummaryAsync_IncludesAccountId()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var capturedUrl = string.Empty;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new DaySummaryDto()),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        await service.GetDaySummaryAsync(new DateOnly(2026, 3, 15), accountId);

        // Assert
        capturedUrl.ShouldContain($"accountId={accountId}");
    }

    /// <summary>
    /// Tests that GetSpendingByLocationAsync returns result on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetSpendingByLocationAsync_ReturnsDto_OnSuccess()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new LocationSpendingReportDto()),
            }));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.GetSpendingByLocationAsync(new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31));

        // Assert
        result.ShouldNotBeNull();
    }

    // Custom Reports

    /// <summary>
    /// Tests that GetCustomReportLayoutsAsync returns list on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCustomReportLayoutsAsync_ReturnsList()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<CustomReportLayoutDto> { new() { Id = Guid.NewGuid() } }),
            }));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.GetCustomReportLayoutsAsync();

        // Assert
        result.Count.ShouldBe(1);
    }

    /// <summary>
    /// Tests that GetCustomReportLayoutsAsync returns empty on error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCustomReportLayoutsAsync_ReturnsEmpty_OnHttpError()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("fail"));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.GetCustomReportLayoutsAsync();

        // Assert
        result.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that GetCustomReportLayoutAsync returns layout on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCustomReportLayoutAsync_ReturnsDto_OnSuccess()
    {
        // Arrange
        var layoutId = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new CustomReportLayoutDto { Id = layoutId }),
            }));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.GetCustomReportLayoutAsync(layoutId);

        // Assert
        result.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that GetCustomReportLayoutAsync returns null on error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCustomReportLayoutAsync_ReturnsNull_OnHttpError()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("fail"));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.GetCustomReportLayoutAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that CreateCustomReportLayoutAsync returns layout on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateCustomReportLayoutAsync_ReturnsDto_OnSuccess()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = JsonContent.Create(new CustomReportLayoutDto { Id = Guid.NewGuid() }),
            }));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.CreateCustomReportLayoutAsync(new CustomReportLayoutCreateDto());

        // Assert
        result.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that CreateCustomReportLayoutAsync returns null on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateCustomReportLayoutAsync_ReturnsNull_OnFailure()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.CreateCustomReportLayoutAsync(new CustomReportLayoutCreateDto());

        // Assert
        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that DeleteCustomReportLayoutAsync returns true on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteCustomReportLayoutAsync_ReturnsTrue_OnSuccess()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.DeleteCustomReportLayoutAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that DeleteCustomReportLayoutAsync returns false on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteCustomReportLayoutAsync_ReturnsFalse_OnFailure()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.DeleteCustomReportLayoutAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that DeleteCustomReportLayoutAsync returns false on HTTP error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteCustomReportLayoutAsync_ReturnsFalse_OnHttpError()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("fail"));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.DeleteCustomReportLayoutAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeFalse();
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
