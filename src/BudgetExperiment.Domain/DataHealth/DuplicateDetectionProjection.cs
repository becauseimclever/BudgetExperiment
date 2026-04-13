// <copyright file="DuplicateDetectionProjection.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.DataHealth;

/// <summary>
/// Projection for duplicate detection analysis.
/// </summary>
/// <param name="Id">The transaction identifier.</param>
/// <param name="AccountId">The account identifier.</param>
/// <param name="Date">The transaction date.</param>
/// <param name="Amount">The transaction amount.</param>
/// <param name="Description">The transaction description.</param>
public sealed record DuplicateDetectionProjection(
    Guid Id,
    Guid AccountId,
    DateOnly Date,
    decimal Amount,
    string Description);
