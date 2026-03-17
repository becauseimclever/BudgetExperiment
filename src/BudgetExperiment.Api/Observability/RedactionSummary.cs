// <copyright file="RedactionSummary.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Api.Observability;

/// <summary>
/// Summary of PII redaction applied to the debug bundle.
/// </summary>
public sealed record RedactionSummary
{
    /// <summary>
    /// Gets the total number of fields that were redacted.
    /// </summary>
    public required int TotalFieldsRedacted { get; init; }

    /// <summary>
    /// Gets the categories of data that were redacted.
    /// </summary>
    public required IReadOnlyList<string> CategoriesRedacted { get; init; }
}
