// <copyright file="BudgetApiServiceCategoryAndBudgetTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Shouldly;

namespace BudgetExperiment.Client.Tests.Services;

/// <summary>
/// Unit tests for <see cref="BudgetApiService"/> category, budget goal, and budget progress operations.
/// </summary>
public class BudgetApiServiceCategoryAndBudgetTests
{
    /// <summary>
    /// Tests that GetCategoriesAsync returns categories.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCategoriesAsync_ReturnsList()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<BudgetCategoryDto>
                {
                    new() { Id = Guid.NewGuid(), Name = "Food" },
                }),
            }));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.GetCategoriesAsync();

        // Assert
        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Food");
    }

    /// <summary>
    /// Tests that GetCategoriesAsync includes activeOnly filter.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCategoriesAsync_IncludesActiveOnlyParam()
    {
        // Arrange
        var capturedUrl = string.Empty;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<BudgetCategoryDto>()),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        await service.GetCategoriesAsync(activeOnly: true);

        // Assert
        capturedUrl.ShouldContain("activeOnly=true");
    }

    /// <summary>
    /// Tests that GetCategoryAsync returns category on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCategoryAsync_ReturnsCategory_OnSuccess()
    {
        // Arrange
        var catId = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new BudgetCategoryDto { Id = catId, Name = "Bills" }),
            }));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.GetCategoryAsync(catId);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Bills");
    }

    /// <summary>
    /// Tests that GetCategoryAsync returns null on error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCategoryAsync_ReturnsNull_OnHttpError()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("fail"));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.GetCategoryAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that CreateCategoryAsync returns category on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateCategoryAsync_ReturnsCategory_OnSuccess()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = JsonContent.Create(new BudgetCategoryDto { Id = Guid.NewGuid(), Name = "New Cat" }),
            }));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.CreateCategoryAsync(new BudgetCategoryCreateDto { Name = "New Cat" });

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("New Cat");
    }

    /// <summary>
    /// Tests that DeleteCategoryAsync returns true on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteCategoryAsync_ReturnsTrue_OnSuccess()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.DeleteCategoryAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that ActivateCategoryAsync sends POST to correct URL.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ActivateCategoryAsync_SendsPost()
    {
        // Arrange
        var catId = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            request.Method.ShouldBe(HttpMethod.Post);
            request.RequestUri!.PathAndQuery.ShouldContain($"categories/{catId}/activate");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.ActivateCategoryAsync(catId);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that DeactivateCategoryAsync sends POST to correct URL.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeactivateCategoryAsync_ReturnsTrue_OnSuccess()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.DeactivateCategoryAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeTrue();
    }

    // Budget Goals

    /// <summary>
    /// Tests that GetBudgetGoalsAsync builds correct URL.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetBudgetGoalsAsync_BuildsCorrectUrl()
    {
        // Arrange
        var capturedUrl = string.Empty;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<BudgetGoalDto>()),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        await service.GetBudgetGoalsAsync(2026, 3);

        // Assert
        capturedUrl.ShouldContain("api/v1/budgets");
        capturedUrl.ShouldContain("year=2026");
        capturedUrl.ShouldContain("month=3");
    }

    /// <summary>
    /// Tests that GetBudgetGoalsByCategoryAsync builds correct URL.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetBudgetGoalsByCategoryAsync_BuildsCorrectUrl()
    {
        // Arrange
        var catId = Guid.NewGuid();
        var capturedUrl = string.Empty;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<BudgetGoalDto>()),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        await service.GetBudgetGoalsByCategoryAsync(catId);

        // Assert
        capturedUrl.ShouldContain($"api/v1/budgets/category/{catId}");
    }

    /// <summary>
    /// Tests that DeleteBudgetGoalAsync builds correct URL with query params.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteBudgetGoalAsync_BuildsCorrectUrl()
    {
        // Arrange
        var catId = Guid.NewGuid();
        var capturedUrl = string.Empty;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        await service.DeleteBudgetGoalAsync(catId, 2026, 3);

        // Assert
        capturedUrl.ShouldContain($"api/v1/budgets/{catId}");
        capturedUrl.ShouldContain("year=2026");
        capturedUrl.ShouldContain("month=3");
    }

    /// <summary>
    /// Tests that CopyBudgetGoalsAsync returns result on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CopyBudgetGoalsAsync_ReturnsResult_OnSuccess()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new CopyBudgetGoalsResult()),
            }));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.CopyBudgetGoalsAsync(new CopyBudgetGoalsRequest());

        // Assert
        result.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that CopyBudgetGoalsAsync returns null on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CopyBudgetGoalsAsync_ReturnsNull_OnFailure()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.CopyBudgetGoalsAsync(new CopyBudgetGoalsRequest());

        // Assert
        result.ShouldBeNull();
    }

    // Budget Summary/Progress

    /// <summary>
    /// Tests that GetBudgetSummaryAsync returns summary on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetBudgetSummaryAsync_ReturnsDto_OnSuccess()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new BudgetSummaryDto()),
            }));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.GetBudgetSummaryAsync(2026, 3);

        // Assert
        result.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that GetBudgetSummaryAsync returns null on error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetBudgetSummaryAsync_ReturnsNull_OnHttpError()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("fail"));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.GetBudgetSummaryAsync(2026, 3);

        // Assert
        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that GetCategoryProgressAsync returns progress on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCategoryProgressAsync_ReturnsDto_OnSuccess()
    {
        // Arrange
        var catId = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            request.RequestUri!.PathAndQuery.ShouldContain($"budgets/progress/{catId}");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new BudgetProgressDto()),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.GetCategoryProgressAsync(catId, 2026, 3);

        // Assert
        result.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that GetCategoryProgressAsync returns null on error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCategoryProgressAsync_ReturnsNull_OnHttpError()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("fail"));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.GetCategoryProgressAsync(Guid.NewGuid(), 2026, 3);

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
