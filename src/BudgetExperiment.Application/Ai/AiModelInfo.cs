// <copyright file="AiModelInfo.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Ai;

/// <summary>
/// Information about an available AI model.
/// </summary>
/// <param name="Name">The model name/identifier.</param>
/// <param name="ModifiedAt">When the model was last modified.</param>
/// <param name="SizeBytes">The model size in bytes.</param>
public sealed record AiModelInfo(
    string Name,
    DateTime ModifiedAt,
    long SizeBytes);
