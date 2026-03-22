// <copyright file="ApplyRulesResponse.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Response from bulk applying categorization rules.
/// </summary>
public sealed class ApplyRulesResponse
{
    /// <summary>
    /// Gets or sets the total number of transactions processed.
    /// </summary>
    public int TotalProcessed
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the number of transactions categorized.
    /// </summary>
    public int Categorized
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the number of transactions skipped.
    /// </summary>
    public int Skipped
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the number of errors encountered.
    /// </summary>
    public int Errors
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the error messages, if any.
    /// </summary>
    public IReadOnlyList<string> ErrorMessages { get; set; } = Array.Empty<string>();
}
