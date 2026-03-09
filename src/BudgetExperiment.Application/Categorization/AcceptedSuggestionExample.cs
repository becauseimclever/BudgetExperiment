// <copyright file="AcceptedSuggestionExample.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Represents a previously accepted suggestion pattern and its assigned category.
/// </summary>
/// <param name="Pattern">The matched pattern (e.g., "STARBUCKS").</param>
/// <param name="CategoryName">The category the pattern was assigned to (e.g., "Dining").</param>
public sealed record AcceptedSuggestionExample(string Pattern, string CategoryName);
