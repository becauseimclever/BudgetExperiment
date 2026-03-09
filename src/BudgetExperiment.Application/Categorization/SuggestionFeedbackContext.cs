// <copyright file="SuggestionFeedbackContext.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Contains historical feedback data for enriching AI suggestion prompts.
/// </summary>
/// <param name="DismissedPatterns">Patterns from previously dismissed suggestions that should not be re-suggested.</param>
/// <param name="AcceptedExamples">Examples of previously accepted suggestions to guide the AI.</param>
public sealed record SuggestionFeedbackContext(
    IReadOnlyList<string> DismissedPatterns,
    IReadOnlyList<AcceptedSuggestionExample> AcceptedExamples);
