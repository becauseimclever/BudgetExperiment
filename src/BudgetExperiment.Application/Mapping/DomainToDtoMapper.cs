// <copyright file="DomainToDtoMapper.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
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
            IsTransfer = transaction.IsTransfer,
            TransferId = transaction.TransferId,
            TransferDirection = transaction.TransferDirection?.ToString(),
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

    /// <summary>
    /// Maps a <see cref="RecurringTransaction"/> to a <see cref="RecurringTransactionDto"/>.
    /// </summary>
    /// <param name="recurring">The recurring transaction entity.</param>
    /// <param name="accountName">The account name.</param>
    /// <returns>The mapped DTO.</returns>
    public static RecurringTransactionDto ToDto(RecurringTransaction recurring, string accountName = "")
    {
        return new RecurringTransactionDto
        {
            Id = recurring.Id,
            AccountId = recurring.AccountId,
            AccountName = accountName,
            Description = recurring.Description,
            Amount = ToDto(recurring.Amount),
            Frequency = recurring.RecurrencePattern.Frequency.ToString(),
            Interval = recurring.RecurrencePattern.Interval,
            DayOfMonth = recurring.RecurrencePattern.DayOfMonth,
            DayOfWeek = recurring.RecurrencePattern.DayOfWeek?.ToString(),
            MonthOfYear = recurring.RecurrencePattern.MonthOfYear,
            StartDate = recurring.StartDate,
            EndDate = recurring.EndDate,
            NextOccurrence = recurring.NextOccurrence,
            IsActive = recurring.IsActive,
            CreatedAtUtc = recurring.CreatedAtUtc,
            UpdatedAtUtc = recurring.UpdatedAtUtc,
        };
    }

    /// <summary>
    /// Maps a recurring instance to a <see cref="RecurringInstanceDto"/>.
    /// </summary>
    /// <param name="recurring">The recurring transaction entity.</param>
    /// <param name="scheduledDate">The scheduled date of the instance.</param>
    /// <param name="exception">Optional exception for this instance.</param>
    /// <param name="generatedTransactionId">Optional ID of generated transaction.</param>
    /// <returns>The mapped DTO.</returns>
    public static RecurringInstanceDto ToInstanceDto(
        RecurringTransaction recurring,
        DateOnly scheduledDate,
        RecurringTransactionException? exception = null,
        Guid? generatedTransactionId = null)
    {
        var isSkipped = exception?.ExceptionType == ExceptionType.Skipped;
        var isModified = exception?.ExceptionType == ExceptionType.Modified;

        return new RecurringInstanceDto
        {
            RecurringTransactionId = recurring.Id,
            ScheduledDate = scheduledDate,
            EffectiveDate = exception?.GetEffectiveDate() ?? scheduledDate,
            Amount = isModified && exception?.ModifiedAmount != null
                ? ToDto(exception.ModifiedAmount)
                : ToDto(recurring.Amount),
            Description = isModified && exception?.ModifiedDescription != null
                ? exception.ModifiedDescription
                : recurring.Description,
            IsModified = isModified,
            IsSkipped = isSkipped,
            IsGenerated = generatedTransactionId.HasValue,
            GeneratedTransactionId = generatedTransactionId,
        };
    }
}
