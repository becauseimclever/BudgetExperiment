// <copyright file="RecurringTransferInstanceProjector.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Recurring;

/// <summary>
/// Projects recurring transfer instances for date ranges.
/// </summary>
public sealed class RecurringTransferInstanceProjector : IRecurringTransferInstanceProjector
{
    private readonly IRecurringTransferRepository _recurringTransferRepository;
    private readonly IAccountRepository _accountRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringTransferInstanceProjector"/> class.
    /// </summary>
    /// <param name="recurringTransferRepository">The recurring transfer repository.</param>
    /// <param name="accountRepository">The account repository.</param>
    public RecurringTransferInstanceProjector(
        IRecurringTransferRepository recurringTransferRepository,
        IAccountRepository accountRepository)
    {
        _recurringTransferRepository = recurringTransferRepository;
        _accountRepository = accountRepository;
    }

    /// <inheritdoc/>
    public async Task<Dictionary<DateOnly, List<RecurringTransferInstanceInfo>>> GetInstancesByDateRangeAsync(
        IReadOnlyList<RecurringTransfer> recurringTransfers,
        DateOnly fromDate,
        DateOnly toDate,
        Guid? accountId = null,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<DateOnly, List<RecurringTransferInstanceInfo>>();
        var accounts = await _accountRepository.GetAllAsync(cancellationToken);
        var accountMap = accounts.ToDictionary(a => a.Id, a => a.Name);

        foreach (var transfer in recurringTransfers.Where(r => r.IsActive))
        {
            var occurrences = transfer.GetOccurrencesBetween(fromDate, toDate);
            var exceptions = await _recurringTransferRepository.GetExceptionsByDateRangeAsync(
                transfer.Id,
                fromDate,
                toDate,
                cancellationToken);
            var exceptionMap = exceptions.ToDictionary(e => e.OriginalDate);

            foreach (var date in occurrences)
            {
                exceptionMap.TryGetValue(date, out var exception);

                if (exception?.ExceptionType == ExceptionType.Skipped)
                {
                    continue;
                }

                var effectiveAmount = exception?.ModifiedAmount ?? transfer.Amount;
                var effectiveDescription = exception?.ModifiedDescription ?? transfer.Description;
                var isModified = exception?.ExceptionType == ExceptionType.Modified;

                // Add source account entry (outgoing - negative amount)
                if (!accountId.HasValue || accountId.Value == transfer.SourceAccountId)
                {
                    var sourceInstance = new RecurringTransferInstanceInfo(
                        RecurringTransferId: transfer.Id,
                        InstanceDate: date,
                        AccountId: transfer.SourceAccountId,
                        AccountName: accountMap.GetValueOrDefault(transfer.SourceAccountId, string.Empty),
                        Description: $"Transfer to {accountMap.GetValueOrDefault(transfer.DestinationAccountId, string.Empty)}: {effectiveDescription}",
                        Amount: MoneyValue.Create(effectiveAmount.Currency, -effectiveAmount.Amount),
                        IsModified: isModified,
                        IsSkipped: false,
                        TransferDirection: "Source");

                    if (!result.TryGetValue(date, out var sourceList))
                    {
                        sourceList = new List<RecurringTransferInstanceInfo>();
                        result[date] = sourceList;
                    }

                    sourceList.Add(sourceInstance);
                }

                // Add destination account entry (incoming - positive amount)
                if (!accountId.HasValue || accountId.Value == transfer.DestinationAccountId)
                {
                    var destInstance = new RecurringTransferInstanceInfo(
                        RecurringTransferId: transfer.Id,
                        InstanceDate: date,
                        AccountId: transfer.DestinationAccountId,
                        AccountName: accountMap.GetValueOrDefault(transfer.DestinationAccountId, string.Empty),
                        Description: $"Transfer from {accountMap.GetValueOrDefault(transfer.SourceAccountId, string.Empty)}: {effectiveDescription}",
                        Amount: effectiveAmount,
                        IsModified: isModified,
                        IsSkipped: false,
                        TransferDirection: "Destination");

                    if (!result.TryGetValue(date, out var destList))
                    {
                        destList = new List<RecurringTransferInstanceInfo>();
                        result[date] = destList;
                    }

                    destList.Add(destInstance);
                }
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<List<RecurringTransferInstanceInfo>> GetInstancesForDateAsync(
        IReadOnlyList<RecurringTransfer> recurringTransfers,
        DateOnly date,
        Guid? accountId = null,
        CancellationToken cancellationToken = default)
    {
        var result = new List<RecurringTransferInstanceInfo>();
        var accounts = await _accountRepository.GetAllAsync(cancellationToken);
        var accountMap = accounts.ToDictionary(a => a.Id, a => a.Name);

        foreach (var transfer in recurringTransfers.Where(r => r.IsActive))
        {
            var occurrences = transfer.GetOccurrencesBetween(date, date);
            if (!occurrences.Contains(date))
            {
                continue;
            }

            var exception = await _recurringTransferRepository.GetExceptionAsync(transfer.Id, date, cancellationToken);
            var effectiveAmount = exception?.ModifiedAmount ?? transfer.Amount;
            var effectiveDescription = exception?.ModifiedDescription ?? transfer.Description;
            var isModified = exception?.ExceptionType == ExceptionType.Modified;
            var isSkipped = exception?.ExceptionType == ExceptionType.Skipped;

            // Add source account entry (outgoing - negative amount)
            if (!accountId.HasValue || accountId.Value == transfer.SourceAccountId)
            {
                result.Add(new RecurringTransferInstanceInfo(
                    RecurringTransferId: transfer.Id,
                    InstanceDate: date,
                    AccountId: transfer.SourceAccountId,
                    AccountName: accountMap.GetValueOrDefault(transfer.SourceAccountId, string.Empty),
                    Description: $"Transfer to {accountMap.GetValueOrDefault(transfer.DestinationAccountId, string.Empty)}: {effectiveDescription}",
                    Amount: MoneyValue.Create(effectiveAmount.Currency, -effectiveAmount.Amount),
                    IsModified: isModified,
                    IsSkipped: isSkipped,
                    TransferDirection: "Source"));
            }

            // Add destination account entry (incoming - positive amount)
            if (!accountId.HasValue || accountId.Value == transfer.DestinationAccountId)
            {
                result.Add(new RecurringTransferInstanceInfo(
                    RecurringTransferId: transfer.Id,
                    InstanceDate: date,
                    AccountId: transfer.DestinationAccountId,
                    AccountName: accountMap.GetValueOrDefault(transfer.DestinationAccountId, string.Empty),
                    Description: $"Transfer from {accountMap.GetValueOrDefault(transfer.SourceAccountId, string.Empty)}: {effectiveDescription}",
                    Amount: effectiveAmount,
                    IsModified: isModified,
                    IsSkipped: isSkipped,
                    TransferDirection: "Destination"));
            }
        }

        return result;
    }
}
