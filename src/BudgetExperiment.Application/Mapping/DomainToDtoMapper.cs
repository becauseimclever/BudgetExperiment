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
            Amount = ToDto(transaction.Amount),
            Date = transaction.Date,
            Description = transaction.Description,
            CategoryId = transaction.CategoryId,
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
                ? ToDto(exception.ModifiedAmount)
                : ToDto(recurring.Amount),
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

    /// <summary>
    /// Maps a <see cref="PaycheckAllocation"/> to a <see cref="PaycheckAllocationDto"/>.
    /// </summary>
    /// <param name="allocation">The allocation.</param>
    /// <returns>The mapped DTO.</returns>
    public static PaycheckAllocationDto ToDto(PaycheckAllocation allocation)
    {
        return new PaycheckAllocationDto
        {
            Description = allocation.Bill.Description,
            BillAmount = ToDto(allocation.Bill.Amount),
            BillFrequency = allocation.Bill.Frequency.ToString(),
            AmountPerPaycheck = ToDto(allocation.AmountPerPaycheck),
            AnnualAmount = ToDto(allocation.AnnualAmount),
            RecurringTransactionId = allocation.Bill.SourceRecurringTransactionId,
        };
    }

    /// <summary>
    /// Maps a <see cref="PaycheckAllocationWarning"/> to a <see cref="PaycheckAllocationWarningDto"/>.
    /// </summary>
    /// <param name="warning">The warning.</param>
    /// <returns>The mapped DTO.</returns>
    public static PaycheckAllocationWarningDto ToDto(PaycheckAllocationWarning warning)
    {
        return new PaycheckAllocationWarningDto
        {
            Type = warning.Type.ToString(),
            Message = warning.Message,
            Amount = warning.Amount is not null ? ToDto(warning.Amount) : null,
        };
    }

    /// <summary>
    /// Maps a <see cref="PaycheckAllocationSummary"/> to a <see cref="PaycheckAllocationSummaryDto"/>.
    /// </summary>
    /// <param name="summary">The summary.</param>
    /// <returns>The mapped DTO.</returns>
    public static PaycheckAllocationSummaryDto ToDto(PaycheckAllocationSummary summary)
    {
        return new PaycheckAllocationSummaryDto
        {
            Allocations = summary.Allocations.Select(ToDto).ToList(),
            TotalPerPaycheck = ToDto(summary.TotalPerPaycheck),
            PaycheckAmount = summary.PaycheckAmount is not null ? ToDto(summary.PaycheckAmount) : null,
            RemainingPerPaycheck = ToDto(summary.RemainingPerPaycheck),
            Shortfall = ToDto(summary.Shortfall),
            TotalAnnualBills = ToDto(summary.TotalAnnualBills),
            TotalAnnualIncome = summary.TotalAnnualIncome is not null ? ToDto(summary.TotalAnnualIncome) : null,
            Warnings = summary.Warnings.Select(ToDto).ToList(),
            HasWarnings = summary.HasWarnings,
            CannotReconcile = summary.CannotReconcile,
            PaycheckFrequency = summary.PaycheckFrequency.ToString(),
        };
    }

    /// <summary>
    /// Maps a <see cref="BudgetCategory"/> to a <see cref="BudgetCategoryDto"/>.
    /// </summary>
    /// <param name="category">The budget category entity.</param>
    /// <returns>The mapped DTO.</returns>
    public static BudgetCategoryDto ToDto(BudgetCategory category)
    {
        return new BudgetCategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Type = category.Type.ToString(),
            Icon = category.Icon,
            Color = category.Color,
            SortOrder = category.SortOrder,
            IsActive = category.IsActive,
        };
    }

    /// <summary>
    /// Maps a <see cref="BudgetGoal"/> to a <see cref="BudgetGoalDto"/>.
    /// </summary>
    /// <param name="goal">The budget goal entity.</param>
    /// <returns>The mapped DTO.</returns>
    public static BudgetGoalDto ToDto(BudgetGoal goal)
    {
        return new BudgetGoalDto
        {
            Id = goal.Id,
            CategoryId = goal.CategoryId,
            Year = goal.Year,
            Month = goal.Month,
            TargetAmount = ToDto(goal.TargetAmount),
        };
    }

    /// <summary>
    /// Maps a <see cref="BudgetProgress"/> to a <see cref="BudgetProgressDto"/>.
    /// </summary>
    /// <param name="progress">The budget progress value object.</param>
    /// <returns>The mapped DTO.</returns>
    public static BudgetProgressDto ToDto(BudgetProgress progress)
    {
        return new BudgetProgressDto
        {
            CategoryId = progress.CategoryId,
            CategoryName = progress.CategoryName,
            CategoryIcon = progress.CategoryIcon,
            CategoryColor = progress.CategoryColor,
            TargetAmount = ToDto(progress.TargetAmount),
            SpentAmount = ToDto(progress.SpentAmount),
            RemainingAmount = ToDto(progress.RemainingAmount),
            PercentUsed = progress.PercentUsed,
            Status = progress.Status.ToString(),
            TransactionCount = progress.TransactionCount,
        };
    }

    /// <summary>
    /// Maps a <see cref="CategorizationRule"/> to a <see cref="CategorizationRuleDto"/>.
    /// </summary>
    /// <param name="rule">The categorization rule entity.</param>
    /// <returns>The mapped DTO.</returns>
    public static CategorizationRuleDto ToDto(CategorizationRule rule)
    {
        return new CategorizationRuleDto
        {
            Id = rule.Id,
            Name = rule.Name,
            Pattern = rule.Pattern,
            MatchType = rule.MatchType.ToString(),
            CaseSensitive = rule.CaseSensitive,
            CategoryId = rule.CategoryId,
            CategoryName = rule.Category?.Name,
            Priority = rule.Priority,
            IsActive = rule.IsActive,
            CreatedAt = rule.CreatedAtUtc,
            UpdatedAt = rule.UpdatedAtUtc,
        };
    }
}
