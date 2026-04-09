// <copyright file="LinkableInstanceFinder.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Reconciliation;

/// <summary>
/// Finds linkable recurring instances for a given transaction by projecting
/// nearby recurring instances and calculating match confidence.
/// </summary>
public sealed class LinkableInstanceFinder : ILinkableInstanceFinder
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IRecurringTransactionRepository _recurringRepository;
    private readonly IRecurringInstanceProjector _instanceProjector;
    private readonly ITransactionMatcher _transactionMatcher;
    private readonly IReconciliationMatchRepository _matchRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="LinkableInstanceFinder"/> class.
    /// </summary>
    /// <param name="transactionRepository">The transaction repository.</param>
    /// <param name="recurringRepository">The recurring transaction repository.</param>
    /// <param name="instanceProjector">The recurring instance projector.</param>
    /// <param name="transactionMatcher">The transaction matcher.</param>
    /// <param name="matchRepository">The match repository.</param>
    public LinkableInstanceFinder(
        ITransactionRepository transactionRepository,
        IRecurringTransactionRepository recurringRepository,
        IRecurringInstanceProjector instanceProjector,
        ITransactionMatcher transactionMatcher,
        IReconciliationMatchRepository matchRepository)
    {
        _transactionRepository = transactionRepository;
        _recurringRepository = recurringRepository;
        _instanceProjector = instanceProjector;
        _transactionMatcher = transactionMatcher;
        _matchRepository = matchRepository;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LinkableInstanceDto>> GetLinkableInstancesAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        var transaction = await _transactionRepository.GetByIdAsync(transactionId, cancellationToken);
        if (transaction is null)
        {
            return [];
        }

        var allInstances = await this.GetNearbyInstancesAsync(transaction.Date, cancellationToken);

        var result = new List<LinkableInstanceDto>();
        foreach (var instance in allInstances)
        {
            if (instance.IsSkipped)
            {
                continue;
            }

            var dto = await this.BuildLinkableInstanceDtoAsync(transaction, instance, cancellationToken);
            result.Add(dto);
        }

        return result
            .OrderBy(i => i.InstanceDate)
            .ThenByDescending(i => i.SuggestedConfidence ?? 0)
            .ToList();
    }

    private async Task<List<RecurringInstanceInfoValue>> GetNearbyInstancesAsync(
        DateOnly transactionDate,
        CancellationToken cancellationToken)
    {
        var startDate = transactionDate.AddDays(-30);
        var endDate = transactionDate.AddDays(30);

        var recurringTransactions = await _recurringRepository.GetActiveAsync(cancellationToken);
        var instancesByDate = await _instanceProjector.GetInstancesByDateRangeAsync(
            recurringTransactions, startDate, endDate, excludeDates: null, cancellationToken);

        return instancesByDate.Values.SelectMany(list => list).ToList();
    }

    private async Task<LinkableInstanceDto> BuildLinkableInstanceDtoAsync(
        Transaction transaction,
        RecurringInstanceInfoValue instance,
        CancellationToken cancellationToken)
    {
        var isAlreadyMatched = await _matchRepository.IsInstanceMatchedAsync(
            instance.RecurringTransactionId, instance.InstanceDate, cancellationToken);

        decimal? suggestedConfidence = null;
        if (!isAlreadyMatched)
        {
            var matchResults = _transactionMatcher.FindMatches(
                transaction, [instance], MatchingTolerancesValue.Default);
            suggestedConfidence = matchResults.FirstOrDefault()?.ConfidenceScore;
        }

        return new LinkableInstanceDto
        {
            RecurringTransactionId = instance.RecurringTransactionId,
            Description = instance.Description,
            ExpectedAmount = CommonMapper.ToDto(instance.Amount),
            InstanceDate = instance.InstanceDate,
            IsAlreadyMatched = isAlreadyMatched,
            SuggestedConfidence = suggestedConfidence,
        };
    }
}
