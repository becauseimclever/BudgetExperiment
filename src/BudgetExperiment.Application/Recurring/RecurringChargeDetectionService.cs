// <copyright file="RecurringChargeDetectionService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using BudgetExperiment.Domain.Accounts;
using BudgetExperiment.Domain.Identity;
using BudgetExperiment.Domain.Recurring;
using BudgetExperiment.Domain.Repositories;

namespace BudgetExperiment.Application.Recurring;

/// <summary>
/// Orchestrates recurring charge detection, suggestion management, and acceptance workflows.
/// </summary>
public sealed class RecurringChargeDetectionService : IRecurringChargeDetectionService
{
    private readonly ITransactionQueryRepository _transactionRepository;
    private readonly IRecurringChargeSuggestionRepository _suggestionRepository;
    private readonly IRecurringTransactionRepository _recurringTransactionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserContext _userContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringChargeDetectionService"/> class.
    /// </summary>
    /// <param name="transactionRepository">The transaction repository.</param>
    /// <param name="suggestionRepository">The recurring charge suggestion repository.</param>
    /// <param name="recurringTransactionRepository">The recurring transaction repository.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <param name="userContext">The current user context.</param>
    public RecurringChargeDetectionService(
        ITransactionQueryRepository transactionRepository,
        IRecurringChargeSuggestionRepository suggestionRepository,
        IRecurringTransactionRepository recurringTransactionRepository,
        IUnitOfWork unitOfWork,
        IUserContext userContext)
    {
        _transactionRepository = transactionRepository;
        _suggestionRepository = suggestionRepository;
        _recurringTransactionRepository = recurringTransactionRepository;
        _unitOfWork = unitOfWork;
        _userContext = userContext;
    }

    /// <inheritdoc />
    public async Task<int> DetectAsync(Guid? accountId = null, CancellationToken cancellationToken = default)
    {
        var userId = this.GetRequiredUserId();
        var options = new RecurrenceDetectionOptions();

        var endDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var startDate = endDate.AddMonths(-options.AnalysisWindowMonths);

        var transactions = await _transactionRepository.GetByDateRangeAsync(
            startDate,
            endDate,
            accountId,
            cancellationToken);

        var snapshots = transactions
            .Select(t => ToSnapshot(t))
            .ToList();

        var patterns = RecurrenceDetector.Detect(snapshots, options, endDate);
        var newOrUpdatedCount = 0;

        foreach (var pattern in patterns)
        {
            var existing = await _suggestionRepository.GetByNormalizedDescriptionAndAccountAsync(
                pattern.NormalizedDescription,
                GetPatternAccountId(transactions, pattern, accountId),
                cancellationToken);

            if (existing is not null)
            {
                existing.UpdateFromDetection(pattern);
            }
            else
            {
                var patternAccountId = GetPatternAccountId(transactions, pattern, accountId);

                var suggestion = RecurringChargeSuggestion.Create(
                    patternAccountId,
                    pattern,
                    userId);

                await _suggestionRepository.AddAsync(suggestion, cancellationToken);
            }

            newOrUpdatedCount++;
        }

        if (newOrUpdatedCount > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return newOrUpdatedCount;
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<RecurringChargeSuggestion> Items, long TotalCount)> GetSuggestionsAsync(
        Guid? accountId = null,
        SuggestionStatus? status = null,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        var items = await _suggestionRepository.GetByStatusAsync(
            accountId,
            status,
            skip,
            take,
            cancellationToken);

        var totalCount = await _suggestionRepository.CountByStatusAsync(
            accountId,
            status,
            cancellationToken);

        return (items, totalCount);
    }

    /// <inheritdoc />
    public async Task<RecurringChargeSuggestion?> GetSuggestionByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _suggestionRepository.GetByIdAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AcceptRecurringChargeSuggestionResult> AcceptAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var suggestion = await _suggestionRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new DomainException($"Suggestion with ID '{id}' not found.", DomainExceptionType.NotFound);

        if (suggestion.Status != SuggestionStatus.Pending)
        {
            throw new DomainException($"Suggestion is in '{suggestion.Status}' status and cannot be accepted.");
        }

        var recurrencePattern = BuildRecurrencePattern(suggestion);

        var recurringTransaction = RecurringTransaction.Create(
            suggestion.AccountId,
            suggestion.SampleDescription,
            suggestion.AverageAmount,
            recurrencePattern,
            suggestion.FirstOccurrence,
            categoryId: suggestion.CategoryId);

        var importPattern = ImportPatternValue.Create($"*{suggestion.NormalizedDescription}*");
        recurringTransaction.AddImportPattern(importPattern);

        await _recurringTransactionRepository.AddAsync(recurringTransaction, cancellationToken);

        var linkedCount = await this.LinkMatchingTransactionsAsync(
            suggestion,
            recurringTransaction.Id,
            cancellationToken);

        suggestion.Accept(recurringTransaction.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AcceptRecurringChargeSuggestionResult(recurringTransaction.Id, linkedCount);
    }

    /// <inheritdoc />
    public async Task DismissAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var suggestion = await _suggestionRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new DomainException($"Suggestion with ID '{id}' not found.", DomainExceptionType.NotFound);

        suggestion.Dismiss();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static RecurrencePatternValue BuildRecurrencePattern(RecurringChargeSuggestion suggestion)
    {
        return suggestion.DetectedFrequency switch
        {
            RecurrenceFrequency.Weekly => RecurrencePatternValue.CreateWeekly(
                suggestion.DetectedInterval,
                suggestion.LastOccurrence.DayOfWeek),
            RecurrenceFrequency.BiWeekly => RecurrencePatternValue.CreateBiWeekly(
                suggestion.LastOccurrence.DayOfWeek),
            RecurrenceFrequency.Monthly => RecurrencePatternValue.CreateMonthly(
                suggestion.DetectedInterval,
                suggestion.LastOccurrence.Day),
            RecurrenceFrequency.Quarterly => RecurrencePatternValue.CreateQuarterly(
                suggestion.LastOccurrence.Day),
            RecurrenceFrequency.Yearly => RecurrencePatternValue.CreateYearly(
                suggestion.LastOccurrence.Day,
                suggestion.LastOccurrence.Month),
            _ => RecurrencePatternValue.CreateMonthly(1, suggestion.LastOccurrence.Day),
        };
    }

    private static Guid GetPatternAccountId(
        IReadOnlyList<Transaction> transactions,
        DetectedPattern pattern,
        Guid? fallbackAccountId)
    {
        if (pattern.MatchingTransactionIds.Count > 0)
        {
            return transactions.First(t => t.Id == pattern.MatchingTransactionIds[0]).AccountId;
        }

        return fallbackAccountId ?? Guid.Empty;
    }

    private static TransactionSnapshot ToSnapshot(Transaction t)
    {
        return new TransactionSnapshot(
            t.Id,
            t.AccountId,
            t.Description,
            t.Amount.Amount,
            t.Amount.Currency,
            t.Date,
            t.CategoryId,
            t.RecurringTransactionId);
    }

    private async Task<int> LinkMatchingTransactionsAsync(
        RecurringChargeSuggestion suggestion,
        Guid recurringTransactionId,
        CancellationToken cancellationToken)
    {
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var startDate = endDate.AddMonths(-12);

        var transactions = await _transactionRepository.GetByDateRangeAsync(
            startDate,
            endDate,
            suggestion.AccountId,
            cancellationToken);

        var linkedCount = 0;
        foreach (var transaction in transactions)
        {
            if (transaction.RecurringTransactionId.HasValue)
            {
                continue;
            }

            var normalized = DescriptionNormalizer.Normalize(transaction.Description);
            if (string.Equals(normalized, suggestion.NormalizedDescription, StringComparison.Ordinal))
            {
                transaction.LinkToRecurringInstance(recurringTransactionId, transaction.Date);
                linkedCount++;
            }
        }

        return linkedCount;
    }

    private Guid GetRequiredUserId()
    {
        return _userContext.UserIdAsGuid
            ?? throw new DomainException("User context is not available.");
    }
}
