// <copyright file="TransactionCategoryUpdateDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for quick category assignment on a transaction.
/// </summary>
public sealed class TransactionCategoryUpdateDto
{
    /// <summary>Gets or sets the category ID to assign, or null to clear the category.</summary>
    public Guid? CategoryId
    {
        get; set;
    }
}
