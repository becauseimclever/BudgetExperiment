// <copyright file="BulkRuleActionResponse.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Response for bulk rule operations.
/// </summary>
public sealed class BulkRuleActionResponse
{
    /// <summary>Gets or sets the number of rules affected.</summary>
    public int AffectedCount { get; set; }
}
