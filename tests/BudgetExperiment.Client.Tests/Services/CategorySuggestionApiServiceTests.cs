// <copyright file="CategorySuggestionApiServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Shouldly;

namespace BudgetExperiment.Client.Tests.Services;

/// <summary>
/// Comprehensive unit tests for <see cref="CategorySuggestionApiService"/>.
/// </summary>
public class CategorySuggestionApiServiceTests
{
    // AnalyzeAsync

    /// <summary>
    /// Tests that AnalyzeAsync sends POST and returns suggestions on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AnalyzeAsync_ReturnsSuggestions_OnSuccess()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            request.Method.ShouldBe(HttpMethod.Post);
            request.RequestUri!.PathAndQuery.ShouldBe("/api/v1/categorysuggestions/analyze");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<CategorySuggestionDto>
                {
                    CreateSuggestionDto(),
                }),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new CategorySuggestionApiService(httpClient);

        // Act
        var result = await service.AnalyzeAsync();

        // Assert
        result.Count.ShouldBe(1);
    }

    /// <summary>
    /// Tests that AnalyzeAsync returns empty list on server error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AnalyzeAsync_ReturnsEmpty_OnServerError()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new CategorySuggestionApiService(httpClient);

        // Act
        var result = await service.AnalyzeAsync();

        // Assert
        result.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that AnalyzeAsync returns empty list on HTTP exception.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AnalyzeAsync_ReturnsEmpty_OnHttpException()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("fail"));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new CategorySuggestionApiService(httpClient);

        // Act
        var result = await service.AnalyzeAsync();

        // Assert
        result.ShouldBeEmpty();
    }

    // GetPendingAsync

    /// <summary>
    /// Tests that GetPendingAsync returns pending suggestions.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetPendingAsync_ReturnsSuggestions_OnSuccess()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            request.RequestUri!.PathAndQuery.ShouldBe("/api/v1/categorysuggestions");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<CategorySuggestionDto>
                {
                    CreateSuggestionDto(),
                    CreateSuggestionDto(),
                }),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new CategorySuggestionApiService(httpClient);

        // Act
        var result = await service.GetPendingAsync();

        // Assert
        result.Count.ShouldBe(2);
    }

    /// <summary>
    /// Tests that GetPendingAsync returns empty list on HTTP exception.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetPendingAsync_ReturnsEmpty_OnHttpException()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("fail"));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new CategorySuggestionApiService(httpClient);

        // Act
        var result = await service.GetPendingAsync();

        // Assert
        result.ShouldBeEmpty();
    }

    // GetDismissedAsync

    /// <summary>
    /// Tests that GetDismissedAsync builds correct URL with skip and take params.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetDismissedAsync_BuildsCorrectUrl()
    {
        // Arrange
        var capturedUrl = string.Empty;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUrl = request.RequestUri?.PathAndQuery ?? string.Empty;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<CategorySuggestionDto>()),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new CategorySuggestionApiService(httpClient);

        // Act
        await service.GetDismissedAsync(10, 50);

        // Assert
        capturedUrl.ShouldContain("dismissed");
        capturedUrl.ShouldContain("skip=10");
        capturedUrl.ShouldContain("take=50");
    }

    /// <summary>
    /// Tests that GetDismissedAsync returns empty list on HTTP exception.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetDismissedAsync_ReturnsEmpty_OnHttpException()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("fail"));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new CategorySuggestionApiService(httpClient);

        // Act
        var result = await service.GetDismissedAsync();

        // Assert
        result.ShouldBeEmpty();
    }

    // GetByIdAsync

    /// <summary>
    /// Tests that GetByIdAsync returns suggestion on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetByIdAsync_ReturnsSuggestion_OnSuccess()
    {
        // Arrange
        var id = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            request.RequestUri!.PathAndQuery.ShouldBe($"/api/v1/categorysuggestions/{id}");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(CreateSuggestionDto(id)),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new CategorySuggestionApiService(httpClient);

        // Act
        var result = await service.GetByIdAsync(id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(id);
    }

    /// <summary>
    /// Tests that GetByIdAsync returns null on HTTP exception.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetByIdAsync_ReturnsNull_OnHttpException()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("fail"));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new CategorySuggestionApiService(httpClient);

        // Act
        var result = await service.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    // AcceptAsync

    /// <summary>
    /// Tests that AcceptAsync returns success result on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AcceptAsync_ReturnsSuccessResult_OnSuccess()
    {
        // Arrange
        var id = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            request.Method.ShouldBe(HttpMethod.Post);
            request.RequestUri!.PathAndQuery.ShouldBe($"/api/v1/categorysuggestions/{id}/accept");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new AcceptCategorySuggestionResultDto
                {
                    SuggestionId = id,
                    Success = true,
                }),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new CategorySuggestionApiService(httpClient);

        // Act
        var result = await service.AcceptAsync(id);

        // Assert
        result.Success.ShouldBeTrue();
        result.SuggestionId.ShouldBe(id);
    }

    /// <summary>
    /// Tests that AcceptAsync returns failed result on server error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AcceptAsync_ReturnsFailedResult_OnServerError()
    {
        // Arrange
        var id = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new CategorySuggestionApiService(httpClient);

        // Act
        var result = await service.AcceptAsync(id);

        // Assert
        result.Success.ShouldBeFalse();
        result.SuggestionId.ShouldBe(id);
    }

    /// <summary>
    /// Tests that AcceptAsync returns failed result on HTTP exception.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AcceptAsync_ReturnsFailedResult_OnHttpException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("fail"));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new CategorySuggestionApiService(httpClient);

        // Act
        var result = await service.AcceptAsync(id);

        // Assert
        result.Success.ShouldBeFalse();
    }

    // DismissAsync

    /// <summary>
    /// Tests that DismissAsync returns true on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DismissAsync_ReturnsTrue_OnSuccess()
    {
        // Arrange
        var id = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            request.Method.ShouldBe(HttpMethod.Post);
            request.RequestUri!.PathAndQuery.ShouldBe($"/api/v1/categorysuggestions/{id}/dismiss");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new CategorySuggestionApiService(httpClient);

        // Act
        var result = await service.DismissAsync(id);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that DismissAsync returns false on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DismissAsync_ReturnsFalse_OnFailure()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new CategorySuggestionApiService(httpClient);

        // Act
        var result = await service.DismissAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that DismissAsync returns false on HTTP exception.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DismissAsync_ReturnsFalse_OnHttpException()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("fail"));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new CategorySuggestionApiService(httpClient);

        // Act
        var result = await service.DismissAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeFalse();
    }

    // RestoreAsync

    /// <summary>
    /// Tests that RestoreAsync returns suggestion on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RestoreAsync_ReturnsSuggestion_OnSuccess()
    {
        // Arrange
        var id = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            request.Method.ShouldBe(HttpMethod.Post);
            request.RequestUri!.PathAndQuery.ShouldBe($"/api/v1/categorysuggestions/{id}/restore");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(CreateSuggestionDto(id)),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new CategorySuggestionApiService(httpClient);

        // Act
        var result = await service.RestoreAsync(id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(id);
    }

    /// <summary>
    /// Tests that RestoreAsync returns null on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RestoreAsync_ReturnsNull_OnFailure()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new CategorySuggestionApiService(httpClient);

        // Act
        var result = await service.RestoreAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that RestoreAsync returns null on HTTP exception.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RestoreAsync_ReturnsNull_OnHttpException()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("fail"));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new CategorySuggestionApiService(httpClient);

        // Act
        var result = await service.RestoreAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    // BulkAcceptAsync

    /// <summary>
    /// Tests that BulkAcceptAsync sends POST with correct body.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task BulkAcceptAsync_ReturnsResults_OnSuccess()
    {
        // Arrange
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            request.Method.ShouldBe(HttpMethod.Post);
            request.RequestUri!.PathAndQuery.ShouldBe("/api/v1/categorysuggestions/bulk-accept");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<AcceptCategorySuggestionResultDto>
                {
                    new() { SuggestionId = ids[0], Success = true },
                    new() { SuggestionId = ids[1], Success = true },
                }),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new CategorySuggestionApiService(httpClient);

        // Act
        var result = await service.BulkAcceptAsync(ids);

        // Assert
        result.Count.ShouldBe(2);
    }

    /// <summary>
    /// Tests that BulkAcceptAsync returns empty list on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task BulkAcceptAsync_ReturnsEmpty_OnFailure()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new CategorySuggestionApiService(httpClient);

        // Act
        var result = await service.BulkAcceptAsync(new[] { Guid.NewGuid() });

        // Assert
        result.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that BulkAcceptAsync returns empty list on HTTP exception.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task BulkAcceptAsync_ReturnsEmpty_OnHttpException()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("fail"));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new CategorySuggestionApiService(httpClient);

        // Act
        var result = await service.BulkAcceptAsync(new[] { Guid.NewGuid() });

        // Assert
        result.ShouldBeEmpty();
    }

    // PreviewRulesAsync

    /// <summary>
    /// Tests that PreviewRulesAsync returns rules on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task PreviewRulesAsync_ReturnsRules_OnSuccess()
    {
        // Arrange
        var id = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            request.RequestUri!.PathAndQuery.ShouldBe($"/api/v1/categorysuggestions/{id}/preview-rules");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<SuggestedCategoryRuleDto>
                {
                    new() { Pattern = "TEST", MatchType = "Contains", MatchingTransactionCount = 5, SampleDescriptions = new List<string> { "Sample" } },
                }),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new CategorySuggestionApiService(httpClient);

        // Act
        var result = await service.PreviewRulesAsync(id);

        // Assert
        result.Count.ShouldBe(1);
    }

    /// <summary>
    /// Tests that PreviewRulesAsync returns empty list on HTTP exception.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task PreviewRulesAsync_ReturnsEmpty_OnHttpException()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("fail"));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new CategorySuggestionApiService(httpClient);

        // Act
        var result = await service.PreviewRulesAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeEmpty();
    }

    // CreateRulesAsync

    /// <summary>
    /// Tests that CreateRulesAsync returns success result on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateRulesAsync_ReturnsSuccessResult_OnSuccess()
    {
        // Arrange
        var id = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            request.Method.ShouldBe(HttpMethod.Post);
            request.RequestUri!.PathAndQuery.ShouldBe($"/api/v1/categorysuggestions/{id}/create-rules");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new CreateRulesFromSuggestionResult
                {
                    Success = true,
                }),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new CategorySuggestionApiService(httpClient);

        // Act
        var result = await service.CreateRulesAsync(id, new CreateRulesFromSuggestionRequest { CategoryId = Guid.NewGuid() });

        // Assert
        result.Success.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that CreateRulesAsync returns failed result on server error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateRulesAsync_ReturnsFailedResult_OnServerError()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new CategorySuggestionApiService(httpClient);

        // Act
        var result = await service.CreateRulesAsync(Guid.NewGuid(), new CreateRulesFromSuggestionRequest { CategoryId = Guid.NewGuid() });

        // Assert
        result.Success.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that CreateRulesAsync returns failed result on HTTP exception.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateRulesAsync_ReturnsFailedResult_OnHttpException()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("fail"));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new CategorySuggestionApiService(httpClient);

        // Act
        var result = await service.CreateRulesAsync(Guid.NewGuid(), new CreateRulesFromSuggestionRequest { CategoryId = Guid.NewGuid() });

        // Assert
        result.Success.ShouldBeFalse();
    }

    private static CategorySuggestionDto CreateSuggestionDto(Guid? id = null)
    {
        return new CategorySuggestionDto
        {
            Id = id ?? Guid.NewGuid(),
            SuggestedName = "Test",
            SuggestedType = "Expense",
            Confidence = 0.9m,
            MerchantPatterns = new List<string> { "TEST" },
            MatchingTransactionCount = 5,
            Status = "Pending",
            Source = "PatternMatch",
            CreatedAtUtc = DateTime.UtcNow,
        };
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
