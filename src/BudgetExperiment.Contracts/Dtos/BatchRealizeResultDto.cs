// <copyright file="BatchRealizeResultDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Result DTO for batch realize operation.
/// </summary>
public sealed class BatchRealizeResultDto
{
    /// <summary>Gets or sets the number of items successfully realized.</summary>
    public int SuccessCount
    {
        get; set;
    }

    /// <summary>Gets or sets the number of items that failed.</summary>
    public int FailureCount
    {
        get; set;
    }

    /// <summary>Gets or sets the list of failures with details.</summary>
    public IReadOnlyList<BatchRealizeFailure> Failures { get; set; } = [];
}
