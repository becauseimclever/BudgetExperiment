// <copyright file="AiModelDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for AI model information.
/// </summary>
public sealed class AiModelDto
{
    /// <summary>
    /// Gets or sets the model name/identifier.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the model was last modified.
    /// </summary>
    public DateTime ModifiedAt
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the model size in bytes.
    /// </summary>
    public long SizeBytes
    {
        get; set;
    }
}
