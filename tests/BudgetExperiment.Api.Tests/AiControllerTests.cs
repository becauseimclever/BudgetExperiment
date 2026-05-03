// <copyright file="AiControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain.Settings;
using BudgetExperiment.Shared;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Moq;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for the AI API endpoints.
/// </summary>
[Collection("ApiDb")]
public sealed class AiControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory.</param>
    public AiControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.ResetDatabase();
        _client = factory.CreateApiClient();
    }

    /// <summary>
    /// GET /api/v1/ai/status returns 200 OK with status information.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetStatus_Returns_200_WithStatusInfo()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/ai/status");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var status = await response.Content.ReadFromJsonAsync<AiStatusDto>();
        Assert.NotNull(status);
        Assert.Equal(AiDefaults.DefaultOllamaUrl, status.Endpoint);
        Assert.Equal(AiBackendType.Ollama, status.BackendType);
    }

    /// <summary>
    /// GET /api/v1/ai/models returns 200 OK with model list.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetModels_Returns_200_WithModelList()
    {
        // Arrange
        var expectedModels = new List<AiModelInfo>
        {
            new("stub-model-1", new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc), 1234),
            new("stub-model-2", new DateTime(2026, 2, 3, 4, 5, 6, DateTimeKind.Utc), 5678),
        };

        using var factory = CreateFactoryForModels(AiBackendType.Ollama, expectedModels);
        using var client = CreateAuthenticatedClient(factory);

        // Act
        var response = await client.GetAsync("/api/v1/ai/models");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var models = await response.Content.ReadFromJsonAsync<List<AiModelDto>>();
        Assert.NotNull(models);
        Assert.Equal(2, models.Count);

        Assert.Equal("stub-model-1", models[0].Name);
        Assert.Equal(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc), models[0].ModifiedAt);
        Assert.Equal(1234, models[0].SizeBytes);

        Assert.Equal("stub-model-2", models[1].Name);
        Assert.Equal(new DateTime(2026, 2, 3, 4, 5, 6, DateTimeKind.Utc), models[1].ModifiedAt);
        Assert.Equal(5678, models[1].SizeBytes);
    }

    [Theory]
    [InlineData(AiBackendType.Ollama, "ollama-model")]
    [InlineData(AiBackendType.LlamaCpp, "llama-model")]
    public async Task GetModels_Returns_200_WithMocked_Models_For_Selected_Backend(
        AiBackendType backendType,
        string expectedModelName)
    {
        // Arrange
        using var factory = CreateFactoryForModels(
            backendType,
            [
                new AiModelInfo(expectedModelName, DateTime.UnixEpoch, 0),
            ]);
        using var client = CreateAuthenticatedClient(factory);

        // Act
        var response = await client.GetAsync("/api/v1/ai/models");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var models = await response.Content.ReadFromJsonAsync<List<AiModelDto>>();
        Assert.NotNull(models);
        Assert.Single(models);
        Assert.Equal(expectedModelName, models[0].Name);
    }

    /// <summary>
    /// GET /api/v1/ai/settings returns 200 OK with settings.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetSettings_Returns_200_WithSettings()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/ai/settings");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var settings = await response.Content.ReadFromJsonAsync<AiSettingsDto>();
        Assert.NotNull(settings);
        Assert.Equal(AiDefaults.DefaultOllamaUrl, settings.EndpointUrl);
        Assert.NotNull(settings.ModelName);
        Assert.Equal(AiBackendType.Ollama, settings.BackendType);
    }

    /// <summary>
    /// PUT /api/v1/ai/settings returns 200 OK with updated settings.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateSettings_Returns_200_WithSettings()
    {
        // Arrange
        var request = new AiSettingsDto
        {
            EndpointUrl = AiDefaults.DefaultLlamaCppUrl,
            ModelName = "llama3.2",
            Temperature = 0.5m,
            MaxTokens = 3000,
            TimeoutSeconds = 180,
            IsEnabled = true,
            BackendType = AiBackendType.LlamaCpp,
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/v1/ai/settings", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var settings = await response.Content.ReadFromJsonAsync<AiSettingsDto>();
        Assert.NotNull(settings);
        Assert.Equal(0.5m, settings.Temperature);
        Assert.Equal(AiBackendType.LlamaCpp, settings.BackendType);
        Assert.Equal(AiDefaults.DefaultLlamaCppUrl, settings.EndpointUrl);

        var readBack = await _client.GetFromJsonAsync<AiSettingsDto>("/api/v1/ai/settings");
        Assert.NotNull(readBack);
        Assert.Equal(AiBackendType.LlamaCpp, readBack.BackendType);
        Assert.Equal(AiDefaults.DefaultLlamaCppUrl, readBack.EndpointUrl);
    }

    /// <summary>
    /// POST /api/v1/ai/analyze returns 200 with analysis response.
    /// </summary>
    /// <remarks>
    /// When AI is unavailable, the service methods return empty results
    /// which results in a valid response with zero counts.
    /// </remarks>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Analyze_Returns_200_WithAnalysisResponse()
    {
        // Arrange
        var expectedAnalysis = new RuleSuggestionAnalysis
        {
            UncategorizedTransactionsAnalyzed = 11,
            RulesAnalyzed = 7,
            AnalysisDuration = TimeSpan.FromSeconds(2.5),
        };

        using var factory = CreateFactoryForAnalyze(
            aiStatus: new AiServiceStatus(true, "stub-model", null),
            analysisResult: expectedAnalysis);
        using var client = CreateAuthenticatedClient(factory);

        // Act
        var response = await client.PostAsync("/api/v1/ai/analyze", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AnalysisResponseDto>();
        Assert.NotNull(result);
        Assert.Equal(0, result.NewRuleSuggestions);
        Assert.Equal(0, result.OptimizationSuggestions);
        Assert.Equal(0, result.ConflictSuggestions);
        Assert.Equal(11, result.UncategorizedTransactionsAnalyzed);
        Assert.Equal(7, result.RulesAnalyzed);
        Assert.Equal(2.5d, result.AnalysisDurationSeconds);
    }

    /// <summary>
    /// POST /api/v1/ai/analyze returns 503 when AI service is unavailable.
    /// </summary>
    /// <remarks>
    /// This tests that the endpoint is configured to return 503 for unavailable AI scenarios.
    /// Timeout behavior is covered by a dedicated 504 test.
    /// </remarks>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Analyze_Returns_503_When_Ai_Is_Unavailable()
    {
        // Arrange
        using var factory = CreateFactoryForAnalyze(
            aiStatus: new AiServiceStatus(false, "stub-model", "AI offline"),
            analysisResult: new RuleSuggestionAnalysis());
        using var client = CreateAuthenticatedClient(factory);

        // Act
        var response = await client.PostAsync("/api/v1/ai/analyze", null);

        // Assert
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.NotNull(response.Content.Headers.ContentType);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        Assert.Equal("about:blank", root.GetProperty("type").GetString());
        Assert.Equal("Service Unavailable", root.GetProperty("title").GetString());
        Assert.Equal(503, root.GetProperty("status").GetInt32());
        Assert.Equal("AI service is not available. AI offline", root.GetProperty("detail").GetString());
        Assert.Equal("/api/v1/ai/analyze", root.GetProperty("instance").GetString());
        Assert.True(root.TryGetProperty("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
    }

    [Fact]
    public async Task Analyze_Returns_504_When_Analysis_Times_Out()
    {
        // Arrange
        using var factory = CreateFactoryForAnalyze(
            aiStatus: new AiServiceStatus(true, "stub-model", null),
            analysisException: new OperationCanceledException("timed out"));
        using var client = CreateAuthenticatedClient(factory);

        // Act
        var response = await client.PostAsync("/api/v1/ai/analyze", null);

        // Assert
        Assert.Equal(HttpStatusCode.GatewayTimeout, response.StatusCode);
        Assert.NotNull(response.Content.Headers.ContentType);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        Assert.Equal("about:blank", root.GetProperty("type").GetString());
        Assert.Equal("Gateway Timeout", root.GetProperty("title").GetString());
        Assert.Equal(504, root.GetProperty("status").GetInt32());
        Assert.Equal("AI analysis timed out. The AI service took too long to respond.", root.GetProperty("detail").GetString());
        Assert.Equal("/api/v1/ai/analyze", root.GetProperty("instance").GetString());
        Assert.True(root.TryGetProperty("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
    }

    [Fact]
    public async Task Analyze_Returns_503_ProblemDetails_When_Analysis_Cannot_Reach_Ai_Service()
    {
        // Arrange
        using var factory = CreateFactoryForAnalyze(
            aiStatus: new AiServiceStatus(true, "stub-model", null),
            analysisException: new HttpRequestException("connection refused"));
        using var client = CreateAuthenticatedClient(factory);

        // Act
        var response = await client.PostAsync("/api/v1/ai/analyze", null);

        // Assert
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.NotNull(response.Content.Headers.ContentType);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        Assert.Equal("about:blank", root.GetProperty("type").GetString());
        Assert.Equal("Service Unavailable", root.GetProperty("title").GetString());
        Assert.Equal(503, root.GetProperty("status").GetInt32());
        Assert.Equal("Failed to communicate with AI service. connection refused", root.GetProperty("detail").GetString());
        Assert.Equal("/api/v1/ai/analyze", root.GetProperty("instance").GetString());
        Assert.True(root.TryGetProperty("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
    }

    [Fact]
    public async Task Analyze_Returns_500_ProblemDetails_With_TraceId_When_Unexpected_Error_Occurs()
    {
        // Arrange
        using var factory = CreateFactoryForAnalyze(
            aiStatus: new AiServiceStatus(true, "stub-model", null),
            analysisException: new InvalidOperationException("analysis crashed unexpectedly"));
        using var client = CreateAuthenticatedClient(factory);

        // Act
        var response = await client.PostAsync("/api/v1/ai/analyze", null);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotNull(response.Content.Headers.ContentType);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        Assert.Equal("about:blank", root.GetProperty("type").GetString());
        Assert.Equal("Internal Server Error", root.GetProperty("title").GetString());
        Assert.Equal(500, root.GetProperty("status").GetInt32());
        Assert.Equal("analysis crashed unexpectedly", root.GetProperty("detail").GetString());
        Assert.Equal("/api/v1/ai/analyze", root.GetProperty("instance").GetString());
        Assert.True(root.TryGetProperty("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
    }

    private WebApplicationFactory<Program> CreateFactoryForModels(AiBackendType backendType, IReadOnlyList<AiModelInfo> models)
    {
        var modelName = models[0].Name;

        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                ReplaceSingletonService<IAppSettingsService>(services, new StubAppSettingsService(new AiSettingsData(
                    EndpointUrl: backendType == AiBackendType.LlamaCpp ? AiDefaults.DefaultLlamaCppUrl : AiDefaults.DefaultOllamaUrl,
                    ModelName: modelName,
                    Temperature: 0.3m,
                    MaxTokens: 2000,
                    TimeoutSeconds: 120,
                    IsEnabled: true,
                    BackendType: backendType)));
                ReplaceSingletonService<IAiService>(services, new StubAiService(
                    status: new AiServiceStatus(true, modelName, null),
                    models: models));
            });
        });
    }

    private WebApplicationFactory<Program> CreateFactoryForAnalyze(
        AiServiceStatus aiStatus,
        RuleSuggestionAnalysis? analysisResult = null,
        Exception? analysisException = null)
    {
        var aiServiceMock = new Mock<IAiService>(MockBehavior.Strict);
        aiServiceMock
            .Setup(service => service.GetStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(aiStatus);
        aiServiceMock
            .Setup(service => service.GetAvailableModelsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AiModelInfo>());
        aiServiceMock
            .Setup(service => service.CompleteAsync(It.IsAny<AiPrompt>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponse(true, "ok", null, 0, TimeSpan.Zero));

        var suggestionServiceMock = new Mock<IRuleSuggestionService>(MockBehavior.Strict);
        if (analysisException == null)
        {
            suggestionServiceMock
                .Setup(service => service.AnalyzeAllAsync(It.IsAny<IProgress<AnalysisProgress>?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(analysisResult ?? new RuleSuggestionAnalysis());
        }
        else
        {
            suggestionServiceMock
                .Setup(service => service.AnalyzeAllAsync(It.IsAny<IProgress<AnalysisProgress>?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(analysisException);
        }

        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                ReplaceSingletonService<IAppSettingsService>(services, new StubAppSettingsService(new AiSettingsData(
                    EndpointUrl: AiDefaults.DefaultOllamaUrl,
                    ModelName: "stub-model",
                    Temperature: 0.3m,
                    MaxTokens: 2000,
                    TimeoutSeconds: 120,
                    IsEnabled: true,
                    BackendType: AiBackendType.Ollama)));
                ReplaceSingletonService<IAiService>(services, aiServiceMock.Object);
                ReplaceSingletonService<IRuleSuggestionService>(services, suggestionServiceMock.Object);
            });
        });
    }

    private void ReplaceSingletonService<TService>(IServiceCollection services, TService implementation)
        where TService : class
    {
        var descriptors = services
            .Where(descriptor => descriptor.ServiceType == typeof(TService))
            .ToList();

        foreach (var descriptor in descriptors)
        {
            services.Remove(descriptor);
        }

        services.AddSingleton(implementation);
    }

    private HttpClient CreateAuthenticatedClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("TestAuto", "authenticated");
        return client;
    }

    private sealed class StubAiService : IAiService
    {
        private readonly AiServiceStatus _status;
        private readonly IReadOnlyList<AiModelInfo> _models;

        public StubAiService(
            AiServiceStatus status,
            IReadOnlyList<AiModelInfo> models)
        {
            _status = status;
            _models = models;
        }

        public Task<AiServiceStatus> GetStatusAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_status);
        }

        public Task<IReadOnlyList<AiModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_models);
        }

        public Task<AiResponse> CompleteAsync(AiPrompt prompt, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AiResponse(true, "ok", null, 0, TimeSpan.Zero));
        }
    }

    private sealed class StubAppSettingsService : IAppSettingsService
    {
        private AiSettingsData _settings;

        public StubAppSettingsService(AiSettingsData settings)
        {
            _settings = settings;
        }

        public Task<AppSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AppSettingsDto());
        }

        public Task<AppSettingsDto> UpdateSettingsAsync(AppSettingsUpdateDto dto, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AppSettingsDto());
        }

        public Task<AiSettingsData> GetAiSettingsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_settings);
        }

        public Task<AiSettingsData> UpdateAiSettingsAsync(AiSettingsData settings, CancellationToken cancellationToken = default)
        {
            _settings = settings;
            return Task.FromResult(_settings);
        }
    }
}
