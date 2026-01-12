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
    public int SuccessCount { get; set; }

    /// <summary>Gets or sets the number of items that failed.</summary>
    public int FailureCount { get; set; }

    /// <summary>Gets or sets the list of failures with details.</summary>
    public IReadOnlyList<BatchRealizeFailure> Failures { get; set; } = [];
}

/// <summary>
/// Details about a failed batch realize item.
/// </summary>
public sealed class BatchRealizeFailure
{
    /// <summary>Gets or sets the recurring item identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the type of recurring item.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets the instance date.</summary>
    public DateOnly InstanceDate { get; set; }

    /// <summary>Gets or sets the error message.</summary>
    public string Error { get; set; } = string.Empty;
}
