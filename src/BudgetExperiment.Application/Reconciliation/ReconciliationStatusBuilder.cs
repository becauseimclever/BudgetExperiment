// <copyright file="ReconciliationStatusBuilder.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Constants;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Reconciliation;

/// <summary>
/// Builds reconciliation status reports by comparing projected recurring instances
/// against existing matches for a given period.
/// </summary>
public sealed class ReconciliationStatusBuilder : IReconciliationStatusBuilder
{
    private readonly IReconciliationMatchRepository _matchRepository;
    private readonly IRecurringTransactionRepository _recurringRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IRecurringInstanceProjector _instanceProjector;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReconciliationStatusBuilder"/> class.
    /// </summary>
    /// <param name="matchRepository">The reconciliation match repository.</param>
    /// <param name="recurringRepository">The recurring transaction repository.</param>
    /// <param name="transactionRepository">The transaction repository.</param>
    /// <param name="instanceProjector">The recurring instance projector.</param>
    public ReconciliationStatusBuilder(
        IReconciliationMatchRepository matchRepository,
        IRecurringTransactionRepository recurringRepository,
        ITransactionRepository transactionRepository,
        IRecurringInstanceProjector instanceProjector)
    {
        _matchRepository = matchRepository;
        _recurringRepository = recurringRepository;
        _transactionRepository = transactionRepository;
        _instanceProjector = instanceProjector;
    }

    /// <inheritdoc />
    public async Task<ReconciliationStatusDto> GetReconciliationStatusAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var recurringTransactions = await _recurringRepository.GetActiveAsync(cancellationToken);
        var instancesByDate = await _instanceProjector.GetInstancesByDateRangeAsync(
            recurringTransactions,
            startDate,
            endDate,
            excludeDates: null,
            cancellationToken);

        // Get all matches for this period
        var periodMatches = await _matchRepository.GetByPeriodAsync(year, month, cancellationToken);

        var matchLookup = periodMatches
            .GroupBy(m => (m.RecurringTransactionId, m.RecurringInstanceDate))
            .ToDictionary(g => g.Key, g => g.ToList());

        var instances = new List<RecurringInstanceStatusDto>();
        var matchedCount = 0;
        var pendingCount = 0;
        var missingCount = 0;

        foreach (var (_, instancesForDate) in instancesByDate)
        {
            foreach (var instance in instancesForDate)
            {
                if (instance.IsSkipped)
                {
                    continue;
                }

                var statusDto = await this.BuildInstanceStatusAsync(
                    instance, matchLookup, cancellationToken);

                switch (statusDto.Status)
                {
                    case ReconciliationStatus.Matched:
                        matchedCount++;
                        break;
                    case ReconciliationStatus.Pending:
                        pendingCount++;
                        break;
                    default:
                        missingCount++;
                        break;
                }

                instances.Add(statusDto);
            }
        }

        return new ReconciliationStatusDto
        {
            Year = year,
            Month = month,
            TotalExpectedInstances = instances.Count,
            MatchedCount = matchedCount,
            PendingCount = pendingCount,
            MissingCount = missingCount,
            Instances = instances,
        };
    }

    private async Task<RecurringInstanceStatusDto> BuildInstanceStatusAsync(
        RecurringInstanceInfoValue instance,
        Dictionary<(Guid RecurringTransactionId, DateOnly RecurringInstanceDate), List<ReconciliationMatch>> matchLookup,
        CancellationToken cancellationToken)
    {
        var key = (instance.RecurringTransactionId, instance.InstanceDate);
        var matchesForInstance = matchLookup.GetValueOrDefault(key, []);

        var acceptedMatch = matchesForInstance.FirstOrDefault(
            m => m.Status is ReconciliationMatchStatus.Accepted or ReconciliationMatchStatus.AutoMatched);
        var pendingMatch = matchesForInstance.FirstOrDefault(
            m => m.Status == ReconciliationMatchStatus.Suggested);

        string status;
        Guid? matchedTransactionId = null;
        MoneyDto? actualAmount = null;
        decimal? amountVariance = null;
        Guid? matchId = null;
        string? matchSource = null;

        if (acceptedMatch != null)
        {
            status = ReconciliationStatus.Matched;
            matchedTransactionId = acceptedMatch.ImportedTransactionId;
            amountVariance = acceptedMatch.AmountVariance;
            matchId = acceptedMatch.Id;
            matchSource = acceptedMatch.Source.ToString();

            // Get actual amount from matched transaction
            var transaction = await _transactionRepository.GetByIdAsync(
                acceptedMatch.ImportedTransactionId,
                cancellationToken);
            if (transaction != null)
            {
                actualAmount = CommonMapper.ToDto(transaction.Amount);
            }
        }
        else if (pendingMatch != null)
        {
            status = ReconciliationStatus.Pending;
            matchId = pendingMatch.Id;
            matchSource = pendingMatch.Source.ToString();
        }
        else
        {
            status = ReconciliationStatus.Missing;
        }

        return new RecurringInstanceStatusDto
        {
            RecurringTransactionId = instance.RecurringTransactionId,
            Description = instance.Description,
            InstanceDate = instance.InstanceDate,
            ExpectedAmount = CommonMapper.ToDto(instance.Amount),
            Status = status,
            MatchedTransactionId = matchedTransactionId,
            ActualAmount = actualAmount,
            AmountVariance = amountVariance,
            MatchId = matchId,
            MatchSource = matchSource,
        };
    }
}
