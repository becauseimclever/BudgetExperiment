// <copyright file="DependencyInjectionTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Infrastructure.ExternalServices.AI;
using BudgetExperiment.Shared;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

    private static IServiceCollection CreateServices()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:AppDb"] = "Host=localhost;Database=budgetexperiment;Username=test;Password=test",
                ["AiSettings:BackendType"] = nameof(AiBackendType.LlamaCpp),
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddScoped<IAppSettingsService>(_ => new FakeAppSettingsService(new AiSettingsData(
            EndpointUrl: "http://localhost:11434",
            ModelName: "llama3.2",
            Temperature: 0.3m,
            MaxTokens: 2000,
            TimeoutSeconds: 120,
            IsEnabled: true,
            BackendType: AiBackendType.Ollama)));
        services.AddInfrastructure(configuration);

        return services;
    }
}
