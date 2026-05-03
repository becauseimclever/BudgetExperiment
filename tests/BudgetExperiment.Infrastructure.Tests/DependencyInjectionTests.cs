// <copyright file="DependencyInjectionTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Infrastructure.ExternalServices.AI;
using BudgetExperiment.Shared;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// Registration tests for infrastructure AI services.
/// </summary>
public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_Registers_BackendSelectingAiService_As_IAiService()
    {
        // Arrange
        var services = CreateServices();

        // Act
        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();
        var aiService = scope.ServiceProvider.GetRequiredService<IAiService>();

        // Assert
        Assert.IsType<BackendSelectingAiService>(aiService);
    }

    [Fact]
    public void AddInfrastructure_Registers_Both_Backends_For_Runtime_Selection()
    {
        // Arrange
        var services = CreateServices();

        // Act
        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();
        var ollamaService = scope.ServiceProvider.GetRequiredService<OllamaAiService>();
        var llamaCppService = scope.ServiceProvider.GetRequiredService<LlamaCppAiService>();

        // Assert
        Assert.NotNull(ollamaService);
        Assert.NotNull(llamaCppService);
    }

    [Theory]
    [InlineData(AiBackendType.Ollama, "http://localhost:11434", "http://localhost:11434/api/version")]
    [InlineData(AiBackendType.LlamaCpp, "http://localhost:8080", "http://localhost:8080/health")]
    public async Task AddInfrastructure_IAiService_Uses_Selected_Backend_Endpoint(
        AiBackendType backendType,
        string endpointUrl,
        string expectedStatusUrl)
    {
        // Arrange
        using var ollamaHandler = new RecordingHttpMessageHandler();
        using var ollamaClient = new HttpClient(ollamaHandler);
        using var llamaHandler = new RecordingHttpMessageHandler();
        using var llamaClient = new HttpClient(llamaHandler);
        using var ollamaResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        using var llamaResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK);

        ollamaHandler.ResponseFactory = (_, _) => Task.FromResult(ollamaResponse);
        llamaHandler.ResponseFactory = (_, _) => Task.FromResult(llamaResponse);

        var services = CreateServices(backendType, endpointUrl);
        services.AddScoped<OllamaAiService>(sp => new OllamaAiService(
            ollamaClient,
            sp.GetRequiredService<IAppSettingsService>(),
            NullLogger<OllamaAiService>.Instance));
        services.AddScoped<LlamaCppAiService>(sp => new LlamaCppAiService(
            llamaClient,
            sp.GetRequiredService<IAppSettingsService>(),
            NullLogger<LlamaCppAiService>.Instance));

        // Act
        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();
        var aiService = scope.ServiceProvider.GetRequiredService<IAiService>();
        var status = await aiService.GetStatusAsync(CancellationToken.None);
        var actualStatusUrl = backendType == AiBackendType.Ollama
            ? ollamaHandler.LastRequestUri?.ToString()
            : llamaHandler.LastRequestUri?.ToString();

        // Assert
        Assert.True(status.IsAvailable);
        Assert.Equal(expectedStatusUrl, actualStatusUrl);
        if (backendType == AiBackendType.Ollama)
        {
            Assert.Equal(1, ollamaHandler.RequestCount);
            Assert.Equal(0, llamaHandler.RequestCount);
        }
        else
        {
            Assert.Equal(1, llamaHandler.RequestCount);
            Assert.Equal(0, ollamaHandler.RequestCount);
        }
    }

    private static IServiceCollection CreateServices(
        AiBackendType backendType = AiBackendType.Ollama,
        string endpointUrl = "http://localhost:11434")
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:AppDb"] = "Host=localhost;Database=budgetexperiment;Username=test;Password=test",
                ["AiSettings:BackendType"] = backendType.ToString(),
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddScoped<IAppSettingsService>(_ => new FakeAppSettingsService(new AiSettingsData(
            EndpointUrl: endpointUrl,
            ModelName: "llama3.2",
            Temperature: 0.3m,
            MaxTokens: 2000,
            TimeoutSeconds: 120,
            IsEnabled: true,
            BackendType: backendType)));
        services.AddInfrastructure(configuration);

        return services;
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? ResponseFactory { get; set; }

        public Uri? LastRequestUri
        {
            get; private set;
        }

        public int RequestCount
        {
            get; private set;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            RequestCount++;

            if (ResponseFactory is null)
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            }

            return await ResponseFactory(request, cancellationToken);
        }
    }
}
