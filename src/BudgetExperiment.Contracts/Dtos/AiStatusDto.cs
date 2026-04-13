// <copyright file="AiStatusDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Text.Json.Serialization;

using BudgetExperiment.Shared;

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Response DTO for AI service status.
/// </summary>
public sealed class AiStatusDto
{
    /// <summary>
    /// Gets or sets a value indicating whether the AI service is available.
    /// </summary>
    public bool IsAvailable
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether AI features are enabled.
    /// </summary>
    public bool IsEnabled
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the currently configured model name.
    /// </summary>
    public string? CurrentModel
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the AI backend endpoint URL.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the active AI backend type.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<AiBackendType>))]
    public AiBackendType BackendType { get; set; } = AiBackendType.Ollama;

    /// <summary>
    /// Gets or sets any error message if the service is unavailable.
    /// </summary>
    public string? ErrorMessage
    {
        get; set;
    }
}
