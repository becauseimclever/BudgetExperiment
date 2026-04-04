// <copyright file="DailyBalanceDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Components.Charts.Models;

/// <summary>
/// Lightweight DTO representing the account balance on a specific date, used as input for chart calculations.
/// </summary>
/// <param name="Date">The date of the balance reading.</param>
/// <param name="Balance">The account balance on this date.</param>
public sealed record DailyBalanceDto(DateOnly Date, decimal Balance);
