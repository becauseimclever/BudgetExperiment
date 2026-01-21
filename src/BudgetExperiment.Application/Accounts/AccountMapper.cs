// <copyright file="AccountMapper.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Accounts;

/// <summary>
/// Mappers for account-related domain entities to DTOs.
/// </summary>
public static class AccountMapper
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
            InitialBalance = account.InitialBalance.Amount,
            InitialBalanceCurrency = account.InitialBalance.Currency,
            InitialBalanceDate = account.InitialBalanceDate,
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt,
            Scope = account.Scope.ToString(),
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
            Amount = CommonMapper.ToDto(transaction.Amount),
            Date = transaction.Date,
            Description = transaction.Description,
            CategoryId = transaction.CategoryId,
            CategoryName = transaction.Category?.Name,
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt,
            IsTransfer = transaction.IsTransfer,
            TransferId = transaction.TransferId,
            TransferDirection = transaction.TransferDirection?.ToString(),
            RecurringTransactionId = transaction.RecurringTransactionId,
            RecurringInstanceDate = transaction.RecurringInstanceDate,
            RecurringTransferId = transaction.RecurringTransferId,
            RecurringTransferInstanceDate = transaction.RecurringTransferInstanceDate,
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
            Total = CommonMapper.ToDto(dailyTotal.Total),
            TransactionCount = dailyTotal.TransactionCount,
        };
    }
}
