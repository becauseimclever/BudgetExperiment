// <copyright file="DomainToDtoMapper.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Mapping;

/// <summary>
/// Static mappers for domain to DTO conversion.
/// </summary>
public static class DomainToDtoMapper
{
    /// <summary>
    /// Maps an <see cref="Account"/> to an <see cref="AccountDto"/>.
    /// </summary>
    /// <param name="account">The account entity.</param>
    /// <returns>The mapped DTO.</returns>
    public static AccountDto ToDto(Account account)
    {
        return new AccountDto
        {
            Id = account.Id,
            Name = account.Name,
            Type = account.Type.ToString(),
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt,
            Transactions = account.Transactions.Select(ToDto).ToList(),
        };
    }

    /// <summary>
    /// Maps a <see cref="Transaction"/> to a <see cref="TransactionDto"/>.
    /// </summary>
    /// <param name="transaction">The transaction entity.</param>
    /// <returns>The mapped DTO.</returns>
    public static TransactionDto ToDto(Transaction transaction)
    {
        return new TransactionDto
        {
            Id = transaction.Id,
            AccountId = transaction.AccountId,
            Amount = ToDto(transaction.Amount),
            Date = transaction.Date,
            Description = transaction.Description,
            Category = transaction.Category,
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt,
        };
    }

    /// <summary>
    /// Maps a <see cref="DailyTotal"/> to a <see cref="DailyTotalDto"/>.
    /// </summary>
    /// <param name="dailyTotal">The daily total record.</param>
    /// <returns>The mapped DTO.</returns>
    public static DailyTotalDto ToDto(DailyTotal dailyTotal)
    {
        return new DailyTotalDto
        {
            Date = dailyTotal.Date,
            Total = ToDto(dailyTotal.Total),
            TransactionCount = dailyTotal.TransactionCount,
        };
    }

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
