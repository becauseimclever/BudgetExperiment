// <copyright file="BulkRuleActionRequest.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Request body for bulk rule operations (delete, activate, deactivate).
/// </summary>
public sealed class BulkRuleActionRequest
{
    /// <summary>Gets or sets the IDs of the rules to act upon.</summary>
    public IReadOnlyList<Guid> Ids { get; set; } = [];
}
