// <copyright file="BatchRealizeFailure.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Details about a failed batch realize item.
/// </summary>
public sealed class BatchRealizeFailure
{
    /// <summary>Gets or sets the recurring item identifier.</summary>
    public Guid Id
    {
        get; set;
    }

    /// <summary>Gets or sets the type of recurring item.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets the instance date.</summary>
    public DateOnly InstanceDate
    {
        get; set;
    }

    /// <summary>Gets or sets the error message.</summary>
    public string Error { get; set; } = string.Empty;
}
