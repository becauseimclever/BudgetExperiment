// <copyright file="AiPrompt.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Ai;

/// <summary>
/// A prompt to send to the AI.
/// </summary>
/// <param name="SystemPrompt">The system prompt for context.</param>
/// <param name="UserPrompt">The user's prompt/question.</param>
/// <param name="Temperature">Temperature for randomness (0.0 to 1.0).</param>
/// <param name="MaxTokens">Maximum tokens in response.</param>
public sealed record AiPrompt(
    string SystemPrompt,
    string UserPrompt,
    decimal Temperature = 0.3m,
    int MaxTokens = 2000);
