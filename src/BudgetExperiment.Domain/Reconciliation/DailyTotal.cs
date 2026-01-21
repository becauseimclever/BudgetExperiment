// <copyright file="DailyTotal.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Reconciliation;

/// <summary>
/// Represents the total transaction amount for a single day.
/// Used for calendar summary views.
/// </summary>
/// <param name="Date">The date.</param>
/// <param name="Total">The total amount for the day.</param>
/// <param name="TransactionCount">Number of transactions on that day.</param>
public sealed record DailyTotal(DateOnly Date, MoneyValue Total, int TransactionCount);
