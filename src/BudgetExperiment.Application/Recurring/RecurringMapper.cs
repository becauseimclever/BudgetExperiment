// <copyright file="RecurringMapper.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Recurring;

/// <summary>
/// Mappers for recurring transaction and transfer entities to DTOs.
/// </summary>
public static class RecurringMapper
{
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
            Amount = CommonMapper.ToDto(recurring.Amount),
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
                ? CommonMapper.ToDto(exception.ModifiedAmount)
                : CommonMapper.ToDto(recurring.Amount),
            Description = isModified && exception?.ModifiedDescription != null
                ? exception.ModifiedDescription
                : recurring.Description,
            IsModified = isModified,
            IsSkipped = isSkipped,
            IsGenerated = generatedTransactionId.HasValue,
            GeneratedTransactionId = generatedTransactionId,
            CategoryId = recurring.CategoryId,
            CategoryName = recurring.Category?.Name,
        };
    }

    /// <summary>
    /// Maps a <see cref="RecurringTransfer"/> to a <see cref="RecurringTransferDto"/>.
    /// </summary>
    /// <param name="recurring">The recurring transfer entity.</param>
    /// <param name="sourceAccountName">The source account name.</param>
    /// <param name="destAccountName">The destination account name.</param>
    /// <returns>The mapped DTO.</returns>
    public static RecurringTransferDto ToDto(RecurringTransfer recurring, string sourceAccountName = "", string destAccountName = "")
    {
        return new RecurringTransferDto
        {
            Id = recurring.Id,
            SourceAccountId = recurring.SourceAccountId,
            SourceAccountName = sourceAccountName,
            DestinationAccountId = recurring.DestinationAccountId,
            DestinationAccountName = destAccountName,
            Description = recurring.Description,
            Amount = CommonMapper.ToDto(recurring.Amount),
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
    /// Maps a recurring transfer instance to a <see cref="RecurringTransferInstanceDto"/>.
    /// </summary>
    /// <param name="recurring">The recurring transfer entity.</param>
    /// <param name="scheduledDate">The scheduled date of the instance.</param>
    /// <param name="sourceAccountName">The source account name.</param>
    /// <param name="destAccountName">The destination account name.</param>
    /// <param name="exception">Optional exception for this instance.</param>
    /// <param name="sourceTransactionId">Optional ID of generated source transaction.</param>
    /// <param name="destTransactionId">Optional ID of generated destination transaction.</param>
    /// <returns>The mapped DTO.</returns>
    public static RecurringTransferInstanceDto ToTransferInstanceDto(
        RecurringTransfer recurring,
        DateOnly scheduledDate,
        string sourceAccountName,
        string destAccountName,
        RecurringTransferException? exception = null,
        Guid? sourceTransactionId = null,
        Guid? destTransactionId = null)
    {
        var isSkipped = exception?.ExceptionType == ExceptionType.Skipped;
        var isModified = exception?.ExceptionType == ExceptionType.Modified;

        return new RecurringTransferInstanceDto
        {
            RecurringTransferId = recurring.Id,
            ScheduledDate = scheduledDate,
            EffectiveDate = exception?.GetEffectiveDate() ?? scheduledDate,
            Amount = isModified && exception?.ModifiedAmount != null
                ? CommonMapper.ToDto(exception.ModifiedAmount)
                : CommonMapper.ToDto(recurring.Amount),
            Description = isModified && exception?.ModifiedDescription != null
                ? exception.ModifiedDescription
                : recurring.Description,
            SourceAccountId = recurring.SourceAccountId,
            SourceAccountName = sourceAccountName,
            DestinationAccountId = recurring.DestinationAccountId,
            DestinationAccountName = destAccountName,
            IsModified = isModified,
            IsSkipped = isSkipped,
            IsGenerated = sourceTransactionId.HasValue || destTransactionId.HasValue,
            SourceTransactionId = sourceTransactionId,
            DestinationTransactionId = destTransactionId,
        };
    }
}
