// <copyright file="BatchRealizeItemRequest.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Request for realizing a single item in a batch.
/// </summary>
public sealed class BatchRealizeItemRequest
{
    /// <summary>Gets or sets the recurring item identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the type of recurring item (recurring-transaction or recurring-transfer).</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets the scheduled instance date.</summary>
    public DateOnly InstanceDate { get; set; }
}
