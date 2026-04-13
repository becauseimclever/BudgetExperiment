// <copyright file="AiControllerUnitTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Text.Json;

using BudgetExperiment.Api.Controllers;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Shared;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Unit tests for <see cref="AiController"/> that do not require Testcontainers.
/// </summary>
public sealed class AiControllerUnitTests
{
    [Fact]
    public async Task GetStatusAsync_Returns_BackendType_And_Endpoint_From_Settings()
    {
        // Arrange
        var aiService = new Mock<IAiService>();
        aiService
            .Setup(service => service.GetStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiServiceStatus(true, "llama3.2", null));

        var settingsService = new Mock<IAppSettingsService>();
        settingsService
            .Setup(service => service.GetAiSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiSettingsData(
                EndpointUrl: AiDefaults.DefaultLlamaCppUrl,
                ModelName: "llama3.2",
                Temperature: 0.3m,
                MaxTokens: 2000,
                TimeoutSeconds: 120,
                IsEnabled: true,
                BackendType: AiBackendType.LlamaCpp));

        var controller = CreateController(aiService.Object, settingsService.Object);

        // Act
        var result = await controller.GetStatusAsync(CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<AiStatusDto>(ok.Value);
        Assert.Equal(AiBackendType.LlamaCpp, dto.BackendType);
        Assert.Equal(AiDefaults.DefaultLlamaCppUrl, dto.Endpoint);
    }

    [Fact]
    public async Task UpdateSettingsAsync_Preserves_Default_Ollama_Backend_Contract()
    {
        // Arrange
        var aiService = new Mock<IAiService>();
        var settingsService = new Mock<IAppSettingsService>();
        settingsService
            .Setup(service => service.UpdateAiSettingsAsync(
                It.IsAny<AiSettingsData>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiSettingsData settings, CancellationToken _) => settings);

        var controller = CreateController(aiService.Object, settingsService.Object);
        var request = new AiSettingsDto
        {
            EndpointUrl = AiDefaults.DefaultOllamaUrl,
            ModelName = "llama3.2",
            Temperature = 0.3m,
            MaxTokens = 2000,
            TimeoutSeconds = 120,
            IsEnabled = true,
            BackendType = AiBackendType.Ollama,
        };

        // Act
        var result = await controller.UpdateSettingsAsync(request, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<AiSettingsDto>(ok.Value);
        Assert.Equal(AiBackendType.Ollama, dto.BackendType);
        Assert.Equal(AiDefaults.DefaultOllamaUrl, dto.EndpointUrl);
        settingsService.Verify(
            service => service.UpdateAiSettingsAsync(
                It.Is<AiSettingsData>(settings =>
                    settings.BackendType == AiBackendType.Ollama &&
                    settings.EndpointUrl == AiDefaults.DefaultOllamaUrl),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateSettingsAsync_Accepts_Legacy_OllamaEndpoint_Payload()
    {
        // Arrange
        var aiService = new Mock<IAiService>();
        var settingsService = new Mock<IAppSettingsService>();
        settingsService
            .Setup(service => service.UpdateAiSettingsAsync(
                It.IsAny<AiSettingsData>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiSettingsData settings, CancellationToken _) => settings);

        var request = JsonSerializer.Deserialize<AiSettingsDto>(
            """
            {
              "ollamaEndpoint": "http://legacy-ollama:11434",
              "modelName": "llama3.2",
              "temperature": 0.3,
              "maxTokens": 2000,
              "timeoutSeconds": 120,
              "isEnabled": true,
              "backendType": "Ollama"
            }
            """,
            CreateWebJsonOptions());
        var controller = CreateController(aiService.Object, settingsService.Object);

        // Act
        var result = await controller.UpdateSettingsAsync(request!, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<AiSettingsDto>(ok.Value);
        Assert.Equal("http://legacy-ollama:11434", dto.EndpointUrl);
        Assert.Equal(dto.EndpointUrl, dto.OllamaEndpoint);
        settingsService.Verify(
            service => service.UpdateAiSettingsAsync(
                It.Is<AiSettingsData>(settings =>
                    settings.EndpointUrl == "http://legacy-ollama:11434" &&
                    settings.BackendType == AiBackendType.Ollama),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateSettingsAsync_Uses_Backend_Default_When_Endpoint_Fields_Are_Omitted()
    {
        // Arrange
        var aiService = new Mock<IAiService>();
        var settingsService = new Mock<IAppSettingsService>();
        settingsService
            .Setup(service => service.UpdateAiSettingsAsync(
                It.IsAny<AiSettingsData>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiSettingsData settings, CancellationToken _) => settings);

        var request = JsonSerializer.Deserialize<AiSettingsDto>(
            """
            {
              "modelName": "llama3.2",
              "temperature": 0.3,
              "maxTokens": 2000,
              "timeoutSeconds": 120,
              "isEnabled": true,
              "backendType": "LlamaCpp"
            }
            """,
            CreateWebJsonOptions());
        var controller = CreateController(aiService.Object, settingsService.Object);

        // Act
        var result = await controller.UpdateSettingsAsync(request!, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<AiSettingsDto>(ok.Value);
        Assert.Equal(AiBackendDefaults.DefaultLlamaCppEndpointUrl, dto.EndpointUrl);
        Assert.Equal(AiBackendDefaults.DefaultLlamaCppEndpointUrl, dto.OllamaEndpoint);
        settingsService.Verify(
            service => service.UpdateAiSettingsAsync(
                It.Is<AiSettingsData>(settings =>
                    settings.EndpointUrl == AiBackendDefaults.DefaultLlamaCppEndpointUrl &&
                    settings.BackendType == AiBackendType.LlamaCpp),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void AiSettingsDto_Serializes_BackendType_And_Legacy_Alias_For_Api_Compatibility()
    {
        // Arrange
        var dto = new AiSettingsDto
        {
            EndpointUrl = AiDefaults.DefaultLlamaCppUrl,
            ModelName = "llama3.2",
            Temperature = 0.3m,
            MaxTokens = 2000,
            TimeoutSeconds = 120,
            IsEnabled = true,
            BackendType = AiBackendType.LlamaCpp,
        };

        // Act
        var json = JsonSerializer.Serialize(dto, CreateWebJsonOptions());
        using var document = JsonDocument.Parse(json);

        // Assert
        Assert.Equal("LlamaCpp", document.RootElement.GetProperty("backendType").GetString());
        Assert.Equal(AiDefaults.DefaultLlamaCppUrl, document.RootElement.GetProperty("endpointUrl").GetString());
        Assert.Equal(AiDefaults.DefaultLlamaCppUrl, document.RootElement.GetProperty("ollamaEndpoint").GetString());
    }

    [Fact]
    public void AiStatusDto_Serializes_BackendType_As_String()
    {
        // Arrange
        var dto = new AiStatusDto
        {
            IsAvailable = true,
            IsEnabled = true,
            CurrentModel = "llama3.2",
            Endpoint = AiDefaults.DefaultLlamaCppUrl,
            BackendType = AiBackendType.LlamaCpp,
        };

        // Act
        var json = JsonSerializer.Serialize(dto, CreateWebJsonOptions());
        using var document = JsonDocument.Parse(json);

        // Assert
        Assert.Equal("LlamaCpp", document.RootElement.GetProperty("backendType").GetString());
        Assert.Equal(AiDefaults.DefaultLlamaCppUrl, document.RootElement.GetProperty("endpoint").GetString());
    }

    private static AiController CreateController(IAiService aiService, IAppSettingsService settingsService)
    {
        return new AiController(
            aiService,
            Mock.Of<IRuleSuggestionService>(),
            settingsService,
            NullLogger<AiController>.Instance);
    }

    private static JsonSerializerOptions CreateWebJsonOptions() => new(JsonSerializerDefaults.Web);
}
