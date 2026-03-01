// <copyright file="AiResponse.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Ai;

/// <summary>
/// The response from the AI.
/// </summary>
/// <param name="Success">Whether the request succeeded.</param>
/// <param name="Content">The generated content.</param>
/// <param name="ErrorMessage">Error message if failed.</param>
/// <param name="TokensUsed">Number of tokens used.</param>
/// <param name="Duration">Time taken for the request.</param>
public sealed record AiResponse(
    bool Success,
    string Content,
    string? ErrorMessage,
    int TokensUsed,
    TimeSpan Duration);
