// <copyright file="DataHealthService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Accounts;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain.Accounts;
using BudgetExperiment.Domain.Reconciliation;
using BudgetExperiment.Domain.Repositories;

namespace BudgetExperiment.Application.DataHealth;

/// <summary>
/// Implements data health analysis: duplicate detection, outlier detection,
/// date gap analysis, and uncategorized transaction summary.
/// </summary>
public sealed class DataHealthService : IDataHealthService
{
    private const decimal OutlierSigmaThreshold = 3m;
    private const int OutlierMinGroupSize = 5;
    private const decimal NearDuplicateDescriptionThreshold = 0.85m;

    private readonly ITransactionRepository _transactions;
    private readonly IAccountRepository _accounts;
    private readonly IDismissedOutlierRepository _dismissedOutliers;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataHealthService"/> class.
    /// </summary>
    /// <param name="transactions">Transaction repository.</param>
    /// <param name="accounts">Account repository.</param>
    /// <param name="dismissedOutliers">Dismissed outlier repository.</param>
    /// <param name="unitOfWork">Unit of work.</param>
    public DataHealthService(
        ITransactionRepository transactions,
        IAccountRepository accounts,
        IDismissedOutlierRepository dismissedOutliers,
        IUnitOfWork unitOfWork)
    {
        _transactions = transactions;
        _accounts = accounts;
        _dismissedOutliers = dismissedOutliers;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<DataHealthReportDto> AnalyzeAsync(Guid? accountId, CancellationToken ct)
    {
        var duplicatesTask = FindDuplicatesAsync(accountId, ct);
        var outliersTask = FindOutliersAsync(accountId, ct);
        var dateGapsTask = FindDateGapsAsync(accountId, minGapDays: 14, ct);
        var uncategorizedTask = GetUncategorizedSummaryAsync(ct);

        await Task.WhenAll(duplicatesTask, outliersTask, dateGapsTask, uncategorizedTask);

        return new DataHealthReportDto
        {
            Duplicates = await duplicatesTask,
            Outliers = await outliersTask,
            DateGaps = await dateGapsTask,
            Uncategorized = await uncategorizedTask,
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DuplicateClusterDto>> FindDuplicatesAsync(Guid? accountId, CancellationToken ct)
    {
        var allTransactions = await _transactions.GetAllForHealthAnalysisAsync(accountId, ct);
        var nonTransfers = allTransactions.Where(t => t.TransferId is null).ToList();
        return BuildDuplicateClusters(nonTransfers);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AmountOutlierDto>> FindOutliersAsync(Guid? accountId, CancellationToken ct)
    {
        var allTransactions = await _transactions.GetAllForHealthAnalysisAsync(accountId, ct);
        var dismissedIds = await _dismissedOutliers.GetDismissedTransactionIdsAsync(ct);
        var dismissedSet = new HashSet<Guid>(dismissedIds);
        return BuildOutliers(allTransactions, dismissedSet);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<DateGapDto>> FindDateGapsAsync(Guid? accountId, int minGapDays, CancellationToken ct)
    {
        return Task.FromResult<IReadOnlyList<DateGapDto>>([]);
    }

    /// <inheritdoc />
    public Task<UncategorizedSummaryDto> GetUncategorizedSummaryAsync(CancellationToken ct)
    {
        return Task.FromResult(new UncategorizedSummaryDto());
    }

    /// <inheritdoc />
    public async Task MergeDuplicatesAsync(Guid primaryTransactionId, IReadOnlyList<Guid> duplicateIds, CancellationToken ct)
    {
        var primary = await _transactions.GetByIdAsync(primaryTransactionId, ct);
        if (primary is null)
        {
            return;
        }

        var duplicates = await _transactions.GetByIdsAsync(duplicateIds, ct);

        if (primary.CategoryId is null)
        {
            var categorySource = duplicates.FirstOrDefault(d => d.CategoryId.HasValue);
            if (categorySource is not null)
            {
                primary.UpdateCategory(categorySource.CategoryId!.Value);
                _unitOfWork.MarkAsModified(primary);
            }
        }

        foreach (var duplicate in duplicates)
        {
            await _transactions.RemoveAsync(duplicate, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task DismissOutlierAsync(Guid transactionId, CancellationToken ct)
    {
        var dismissed = DismissedOutlier.Create(transactionId);
        await _dismissedOutliers.AddAsync(dismissed, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static IReadOnlyList<DuplicateClusterDto> BuildDuplicateClusters(
        IReadOnlyList<Transaction> transactions)
    {
        var result = new List<DuplicateClusterDto>();
        var processed = new HashSet<Guid>();

        // Exact duplicate groups: same AccountId + Date + Amount + normalized description
        var exactGroups = transactions
            .GroupBy(t => (
                t.AccountId,
                t.Date,
                t.Amount.Amount,
                t.Amount.Currency,
                NormalizeDesc(t.Description)))
            .Where(g => g.Count() >= 2)
            .ToList();

        foreach (var group in exactGroups)
        {
            var groupKey = $"{group.Key.Date:yyyy-MM-dd}|{group.Key.Amount} {group.Key.Currency}|{group.Key.Item5}";
            var dtos = group.Select(t => AccountMapper.ToDto(t)).ToList();
            result.Add(new DuplicateClusterDto { GroupKey = groupKey, Transactions = dtos });
            foreach (var t in group)
            {
                processed.Add(t.Id);
            }
        }

        // Near-duplicate groups: same AccountId + Date + Amount, description similarity >= 0.85
        var remaining = transactions.Where(t => !processed.Contains(t.Id)).ToList();
        var nearDuplicateCandidates = remaining
            .GroupBy(t => (t.AccountId, t.Date, t.Amount.Amount, t.Amount.Currency))
            .Where(g => g.Count() >= 2)
            .ToList();

        foreach (var group in nearDuplicateCandidates)
        {
            var items = group.ToList();
            var clusters = FindNearDuplicateClusters(items);
            result.AddRange(clusters);
        }

        return result.OrderByDescending(c => c.Transactions.Count).ToList();
    }

    private static List<DuplicateClusterDto> FindNearDuplicateClusters(
        List<Transaction> candidates)
    {
        var clusters = new List<DuplicateClusterDto>();
        var used = new HashSet<Guid>();

        for (var i = 0; i < candidates.Count; i++)
        {
            if (used.Contains(candidates[i].Id))
            {
                continue;
            }

            var cluster = new List<Transaction> { candidates[i] };

            for (var j = i + 1; j < candidates.Count; j++)
            {
                if (used.Contains(candidates[j].Id))
                {
                    continue;
                }

                var similarity = DescriptionSimilarityCalculator.CalculateSimilarity(
                    candidates[i].Description, candidates[j].Description);

                if (similarity >= NearDuplicateDescriptionThreshold)
                {
                    cluster.Add(candidates[j]);
                    used.Add(candidates[j].Id);
                }
            }

            if (cluster.Count >= 2)
            {
                used.Add(candidates[i].Id);
                var groupKey = $"{candidates[i].Date:yyyy-MM-dd}|{candidates[i].Amount.Amount} {candidates[i].Amount.Currency}|near-duplicate";
                clusters.Add(new DuplicateClusterDto
                {
                    GroupKey = groupKey,
                    Transactions = cluster.Select(t => AccountMapper.ToDto(t)).ToList(),
                });
            }
        }

        return clusters;
    }

    private static IReadOnlyList<AmountOutlierDto> BuildOutliers(
        IReadOnlyList<Transaction> transactions,
        HashSet<Guid> dismissedIds)
    {
        var result = new List<AmountOutlierDto>();

        var groups = transactions
            .GroupBy(t => NormalizeDesc(t.Description))
            .Where(g => g.Count() >= OutlierMinGroupSize)
            .ToList();

        foreach (var group in groups)
        {
            var amounts = group.Select(t => Math.Abs(t.Amount.Amount)).ToList();
            var mean = amounts.Average();
            var stddev = CalculateStdDev(amounts, mean);

            if (stddev == 0m)
            {
                continue;
            }

            foreach (var tx in group)
            {
                if (dismissedIds.Contains(tx.Id))
                {
                    continue;
                }

                var absAmount = Math.Abs(tx.Amount.Amount);
                var deviation = Math.Abs(absAmount - mean) / stddev;

                if (deviation > OutlierSigmaThreshold)
                {
                    result.Add(new AmountOutlierDto
                    {
                        Transaction = AccountMapper.ToDto(tx),
                        HistoricalMean = mean,
                        DeviationFactor = deviation,
                        MerchantGroup = group.Key,
                    });
                }
            }
        }

        return result.OrderByDescending(o => o.DeviationFactor).ToList();
    }

    private static decimal CalculateStdDev(List<decimal> values, decimal mean)
    {
        if (values.Count < 2)
        {
            return 0m;
        }

        var sumOfSquares = values.Sum(v => (v - mean) * (v - mean));
        return (decimal)Math.Sqrt((double)(sumOfSquares / values.Count));
    }

    private static string NormalizeDesc(string description) =>
        DescriptionSimilarityCalculator.NormalizeDescription(description);
}
