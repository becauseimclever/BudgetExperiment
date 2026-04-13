// <copyright file="OutlierProjection.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.DataHealth;

/// <summary>
/// Projection for outlier analysis.
/// </summary>
/// <param name="Id">The transaction identifier.</param>
/// <param name="Description">The transaction description.</param>
/// <param name="Amount">The transaction amount.</param>
public sealed record OutlierProjection(Guid Id, string Description, decimal Amount);
