// <copyright file="DateGapProjection.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.DataHealth;

/// <summary>
/// Projection for date gap analysis.
/// </summary>
/// <param name="AccountId">The account identifier.</param>
/// <param name="Date">The transaction date.</param>
public sealed record DateGapProjection(Guid AccountId, DateOnly Date);
