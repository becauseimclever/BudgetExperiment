// <copyright file="AnalysisResponseDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Response DTO for comprehensive AI analysis.
/// </summary>
public sealed class AnalysisResponseDto
{
    /// <summary>
    /// Gets or sets the number of new rule suggestions generated.
    /// </summary>
    public int NewRuleSuggestions
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the number of optimization suggestions generated.
    /// </summary>
    public int OptimizationSuggestions
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the number of conflict suggestions generated.
    /// </summary>
    public int ConflictSuggestions
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the number of uncategorized transactions analyzed.
    /// </summary>
    public int UncategorizedTransactionsAnalyzed
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the number of rules analyzed.
    /// </summary>
    public int RulesAnalyzed
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the analysis duration in seconds.
    /// </summary>
    public double AnalysisDurationSeconds
    {
        get; set;
    }
}
