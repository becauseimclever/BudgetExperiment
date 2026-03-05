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
        return ToDto(account, null);
    }

    /// <summary>
    /// Maps an <see cref="Account"/> to an <see cref="AccountDto"/> with a concurrency version.
    /// </summary>
    /// <param name="account">The account entity.</param>
    /// <param name="version">The concurrency token value.</param>
    /// <returns>The mapped DTO.</returns>
    public static AccountDto ToDto(Account account, string? version)
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
            Version = version,
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
        return ToTransactionDto(transaction, null);
    }

    /// <summary>
    /// Maps a <see cref="Transaction"/> to a <see cref="TransactionDto"/> with a concurrency version.
    /// </summary>
    /// <param name="transaction">The transaction entity.</param>
    /// <param name="version">The concurrency token value.</param>
    /// <returns>The mapped DTO.</returns>
    public static TransactionDto ToTransactionDto(Transaction transaction, string? version)
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
            Location = ToLocationDto(transaction.Location),
            Version = version,
        };
    }

    /// <summary>
    /// Maps a <see cref="TransactionLocationValue"/> to a <see cref="TransactionLocationDto"/>.
    /// </summary>
    /// <param name="location">The location value object (may be null).</param>
    /// <returns>The mapped DTO, or null.</returns>
    public static TransactionLocationDto? ToLocationDto(TransactionLocationValue? location)
    {
        if (location is null)
        {
            return null;
        }

        return new TransactionLocationDto
        {
            Latitude = location.Coordinates?.Latitude,
            Longitude = location.Coordinates?.Longitude,
            City = location.City,
            StateOrRegion = location.StateOrRegion,
            Country = location.Country,
            PostalCode = location.PostalCode,
            Source = location.Source.ToString(),
        };
    }

    /// <summary>
    /// Maps a <see cref="DailyTotalValue"/> to a <see cref="DailyTotalDto"/>.
    /// </summary>
    /// <param name="dailyTotal">The daily total record.</param>
    /// <returns>The mapped DTO.</returns>
    public static DailyTotalDto ToDto(DailyTotalValue dailyTotal)
    {
        return new DailyTotalDto
        {
            Date = dailyTotal.Date,
            Total = CommonMapper.ToDto(dailyTotal.Total),
            TransactionCount = dailyTotal.TransactionCount,
        };
    }
}
