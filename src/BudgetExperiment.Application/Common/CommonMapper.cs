// <copyright file="CommonMapper.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Common;

/// <summary>
/// Mappers for common value objects to DTOs.
/// </summary>
public static class CommonMapper
{
    /// <summary>
    /// Maps a <see cref="MoneyValue"/> to a <see cref="MoneyDto"/>.
    /// </summary>
    /// <param name="money">The money value object.</param>
    /// <returns>The mapped DTO.</returns>
    public static MoneyDto ToDto(MoneyValue money)
    {
        return new MoneyDto
        {
            Currency = money.Currency,
            Amount = money.Amount,
        };
    }
}
