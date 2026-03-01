// <copyright file="ReorderRulesRequest.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Request for reordering rule priorities.
/// </summary>
public sealed class ReorderRulesRequest
{
    /// <summary>
    /// Gets or sets the ordered list of rule IDs. The index becomes the new priority.
    /// </summary>
    public IReadOnlyList<Guid> RuleIds { get; set; } = Array.Empty<Guid>();
}
