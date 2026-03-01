// <copyright file="BatchRealizeRequest.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Request DTO for batch realizing multiple past-due items.
/// </summary>
public sealed class BatchRealizeRequest
{
    /// <summary>Gets or sets the list of items to realize.</summary>
    public IReadOnlyList<BatchRealizeItemRequest> Items { get; set; } = [];
}
