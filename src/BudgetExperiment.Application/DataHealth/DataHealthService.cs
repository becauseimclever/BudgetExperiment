// <copyright file="DataHealthService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Accounts;
using BudgetExperiment.Application.FeatureFlags;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain.Accounts;
using BudgetExperiment.Domain.Common;
using BudgetExperiment.Domain.DataHealth;
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
    private const int NearDuplicateMaxLevenshteinDistance = 2;
    private const string OptimizedAnalysisFeatureFlag = "feature-data-health-optimized-analysis";

    private readonly ITransactionRepository _transactions;
    private readonly IAccountRepository _accounts;
    private readonly IDismissedOutlierRepository _dismissedOutliers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFeatureFlagService _featureFlagService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataHealthService"/> class.
    /// </summary>
    /// <param name="transactions">Transaction repository.</param>
    /// <param name="accounts">Account repository.</param>
    /// <param name="dismissedOutliers">Dismissed outlier repository.</param>
    /// <param name="unitOfWork">Unit of work.</param>
    /// <param name="featureFlagService">Feature flag service.</param>
    public DataHealthService(
        ITransactionRepository transactions,
        IAccountRepository accounts,
        IDismissedOutlierRepository dismissedOutliers,
        IUnitOfWork unitOfWork,
        IFeatureFlagService featureFlagService)
    {
        _transactions = transactions;
        _accounts = accounts;
        _dismissedOutliers = dismissedOutliers;
        _unitOfWork = unitOfWork;
        _featureFlagService = featureFlagService;
    }

    /// <inheritdoc />
    public async Task<DataHealthReportDto> AnalyzeAsync(Guid? accountId, CancellationToken ct)
    {
        if (!await _featureFlagService.IsEnabledAsync(OptimizedAnalysisFeatureFlag, ct))
        {
            // Sequential: DbContext is scoped per request and cannot handle concurrent operations.
            var duplicates = await FindDuplicatesAsync(accountId, ct);
            var outliers = await FindOutliersAsync(accountId, ct);
            var dateGaps = await FindDateGapsAsync(accountId, minGapDays: 14, ct);
            var uncategorized = await GetUncategorizedSummaryAsync(ct);

            return new DataHealthReportDto
            {
                Duplicates = duplicates,
                Outliers = outliers,
                DateGaps = dateGaps,
                Uncategorized = uncategorized,
            };
        }

        var duplicateProjections = await _transactions.GetTransactionProjectionsForDuplicateDetectionAsync(ct);
        var filteredDuplicateProjections = FilterByAccount(duplicateProjections, accountId);
        var duplicatesOptimized = BuildDuplicateClusters(filteredDuplicateProjections);

        var dismissedIds = await _dismissedOutliers.GetDismissedTransactionIdsAsync(ct);
        var dismissedSet = new HashSet<Guid>(dismissedIds);
        var outlierProjections = await _transactions.GetTransactionAmountsForOutlierAnalysisAsync(ct);
        var filteredOutlierProjections = await FilterOutlierProjectionsAsync(accountId, duplicateProjections, outlierProjections, ct);
        var outliersOptimized = BuildOutliers(filteredOutlierProjections, dismissedSet);

        var accounts = await _accounts.GetAllAsync(ct);
        var accountNames = accounts.ToDictionary(a => a.Id, a => a.Name);
        var dateGapProjections = await _transactions.GetTransactionDatesForGapAnalysisAsync(ct);
        var filteredDateGapProjections = FilterByAccount(dateGapProjections, accountId);
        var dateGapsOptimized = BuildDateGaps(filteredDateGapProjections, accountNames, minGapDays: 14);

        var uncategorizedOptimized = await GetUncategorizedSummaryAsync(ct);

        return new DataHealthReportDto
        {
            Duplicates = duplicatesOptimized,
            Outliers = outliersOptimized,
            DateGaps = dateGapsOptimized,
            Uncategorized = uncategorizedOptimized,
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DuplicateClusterDto>> FindDuplicatesAsync(Guid? accountId, CancellationToken ct)
    {
        var projections = await _transactions.GetTransactionProjectionsForDuplicateDetectionAsync(ct);
        return BuildDuplicateClusters(FilterByAccount(projections, accountId));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AmountOutlierDto>> FindOutliersAsync(Guid? accountId, CancellationToken ct)
    {
        var dismissedIds = await _dismissedOutliers.GetDismissedTransactionIdsAsync(ct);
        var dismissedSet = new HashSet<Guid>(dismissedIds);
        var duplicateProjections = await _transactions.GetTransactionProjectionsForDuplicateDetectionAsync(ct);
        var outlierProjections = await _transactions.GetTransactionAmountsForOutlierAnalysisAsync(ct);
        var filteredOutlierProjections = await FilterOutlierProjectionsAsync(accountId, duplicateProjections, outlierProjections, ct);
        return BuildOutliers(filteredOutlierProjections, dismissedSet);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DateGapDto>> FindDateGapsAsync(Guid? accountId, int minGapDays, CancellationToken ct)
    {
        var dateProjections = await _transactions.GetTransactionDatesForGapAnalysisAsync(ct);
        var accounts = await _accounts.GetAllAsync(ct);
        var accountNames = accounts.ToDictionary(a => a.Id, a => a.Name);
        return BuildDateGaps(FilterByAccount(dateProjections, accountId), accountNames, minGapDays);
    }

    /// <inheritdoc />
    public async Task<UncategorizedSummaryDto> GetUncategorizedSummaryAsync(CancellationToken ct)
    {
        var uncategorized = await _transactions.GetUncategorizedAsync(cancellationToken: ct);
        var accounts = await _accounts.GetAllAsync(ct);
        var accountNames = accounts.ToDictionary(a => a.Id, a => a.Name);
        return BuildUncategorizedSummary(uncategorized, accountNames);
    }

    /// <inheritdoc />
    public async Task MergeDuplicatesAsync(Guid primaryTransactionId, IReadOnlyList<Guid> duplicateIds, CancellationToken ct)
    {
        var primary = await _transactions.GetByIdAsync(primaryTransactionId, ct);
        if (primary is null)
        {
            throw new DomainException($"Transaction {primaryTransactionId} not found.", DomainExceptionType.NotFound);
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

        // Near-duplicate groups: same AccountId + Amount, date window ±3 days, Levenshtein distance <= 2
        var remaining = transactions.Where(t => !processed.Contains(t.Id)).ToList();
        var nearDuplicateCandidates = remaining
            .GroupBy(t => (t.AccountId, t.Amount.Amount, t.Amount.Currency))
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

    private static IReadOnlyList<DuplicateClusterDto> BuildDuplicateClusters(
        IReadOnlyList<DuplicateDetectionProjection> transactions)
    {
        var result = new List<DuplicateClusterDto>();
        var processed = new HashSet<Guid>();

        var exactGroups = transactions
            .GroupBy(t => (
                t.AccountId,
                t.Date,
                t.Amount,
                NormalizeDesc(t.Description)))
            .Where(g => g.Count() >= 2)
            .ToList();

        foreach (var group in exactGroups)
        {
            var groupKey = $"{group.Key.Date:yyyy-MM-dd}|{group.Key.Amount} {CurrencyDefaults.DefaultCurrency}|{group.Key.Item4}";
            var dtos = group.Select(ToTransactionDto).ToList();
            result.Add(new DuplicateClusterDto { GroupKey = groupKey, Transactions = dtos });
            foreach (var transaction in group)
            {
                processed.Add(transaction.Id);
            }
        }

        var remaining = transactions.Where(t => !processed.Contains(t.Id)).ToList();
        var nearDuplicateCandidates = remaining
            .GroupBy(t => (t.AccountId, t.Amount))
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
        List<Transaction> candidates,
        Func<string, string, bool>? isSimilar = null)
    {
        var clusters = new List<DuplicateClusterDto>();
        var used = new HashSet<Guid>();
        var ordered = candidates.OrderBy(t => t.Date).ToList();
        var similarityPredicate = isSimilar ?? IsNearDuplicateDescription;

        for (var i = 0; i < ordered.Count; i++)
        {
            var current = ordered[i];
            if (used.Contains(current.Id))
            {
                continue;
            }

            var cluster = new List<Transaction> { current };

            for (var j = i + 1; j < ordered.Count; j++)
            {
                var candidate = ordered[j];
                var dayDiff = candidate.Date.DayNumber - current.Date.DayNumber;
                if (dayDiff > 3)
                {
                    break;
                }

                if (used.Contains(candidate.Id))
                {
                    continue;
                }

                if (similarityPredicate(current.Description, candidate.Description))
                {
                    cluster.Add(candidate);
                    used.Add(candidate.Id);
                }
            }

            if (cluster.Count >= 2)
            {
                used.Add(current.Id);
                var groupKey = $"{current.Date:yyyy-MM-dd}|{current.Amount.Amount} {current.Amount.Currency}|near-duplicate";
                clusters.Add(new DuplicateClusterDto
                {
                    GroupKey = groupKey,
                    Transactions = cluster.Select(t => AccountMapper.ToDto(t)).ToList(),
                });
            }
        }

        return clusters;
    }

    private static List<DuplicateClusterDto> FindNearDuplicateClusters(
        List<DuplicateDetectionProjection> candidates,
        Func<string, string, bool>? isSimilar = null)
    {
        var clusters = new List<DuplicateClusterDto>();
        var used = new HashSet<Guid>();
        var ordered = candidates.OrderBy(t => t.Date).ToList();
        var similarityPredicate = isSimilar ?? IsNearDuplicateDescription;

        for (var i = 0; i < ordered.Count; i++)
        {
            var current = ordered[i];
            if (used.Contains(current.Id))
            {
                continue;
            }

            var cluster = new List<DuplicateDetectionProjection> { current };

            for (var j = i + 1; j < ordered.Count; j++)
            {
                var candidate = ordered[j];
                var dayDiff = candidate.Date.DayNumber - current.Date.DayNumber;
                if (dayDiff > 3)
                {
                    break;
                }

                if (used.Contains(candidate.Id))
                {
                    continue;
                }

                if (similarityPredicate(current.Description, candidate.Description))
                {
                    cluster.Add(candidate);
                    used.Add(candidate.Id);
                }
            }

            if (cluster.Count >= 2)
            {
                used.Add(current.Id);
                var groupKey = $"{current.Date:yyyy-MM-dd}|{current.Amount} {CurrencyDefaults.DefaultCurrency}|near-duplicate";
                clusters.Add(new DuplicateClusterDto
                {
                    GroupKey = groupKey,
                    Transactions = cluster.Select(ToTransactionDto).ToList(),
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

    private static IReadOnlyList<AmountOutlierDto> BuildOutliers(
        IReadOnlyList<OutlierProjection> transactions,
        HashSet<Guid> dismissedIds)
    {
        var result = new List<AmountOutlierDto>();

        var groups = transactions
            .GroupBy(t => NormalizeDesc(t.Description))
            .Where(g => g.Count() >= OutlierMinGroupSize)
            .ToList();

        foreach (var group in groups)
        {
            var amounts = group.Select(t => Math.Abs(t.Amount)).ToList();
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

                var absAmount = Math.Abs(tx.Amount);
                var deviation = Math.Abs(absAmount - mean) / stddev;

                if (deviation > OutlierSigmaThreshold)
                {
                    result.Add(new AmountOutlierDto
                    {
                        Transaction = ToTransactionDto(tx),
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

    private static bool IsNearDuplicateDescription(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        var normalizedLeft = DescriptionSimilarityCalculator.NormalizeDescription(left);
        var normalizedRight = DescriptionSimilarityCalculator.NormalizeDescription(right);
        if (normalizedLeft.Length == 0 || normalizedRight.Length == 0)
        {
            return false;
        }

        var distance = CalculateLevenshteinDistance(normalizedLeft, normalizedRight);
        return distance <= NearDuplicateMaxLevenshteinDistance;
    }

    private static int CalculateLevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source))
        {
            return string.IsNullOrEmpty(target) ? 0 : target.Length;
        }

        if (string.IsNullOrEmpty(target))
        {
            return source.Length;
        }

        var sourceLength = source.Length;
        var targetLength = target.Length;
        var distance = new int[sourceLength + 1, targetLength + 1];

        for (var i = 0; i <= sourceLength; i++)
        {
            distance[i, 0] = i;
        }

        for (var j = 0; j <= targetLength; j++)
        {
            distance[0, j] = j;
        }

        for (var i = 1; i <= sourceLength; i++)
        {
            for (var j = 1; j <= targetLength; j++)
            {
                var cost = target[j - 1] == source[i - 1] ? 0 : 1;
                distance[i, j] = Math.Min(
                    Math.Min(
                        distance[i - 1, j] + 1,
                        distance[i, j - 1] + 1),
                    distance[i - 1, j - 1] + cost);
            }
        }

        return distance[sourceLength, targetLength];
    }

    private static string NormalizeDesc(string description) =>
        DescriptionSimilarityCalculator.NormalizeDescription(description);

    private static IReadOnlyList<DateGapDto> BuildDateGaps(
        IReadOnlyList<Transaction> transactions,
        Dictionary<Guid, string> accountNames,
        int minGapDays)
    {
        var result = new List<DateGapDto>();
        var byAccount = transactions.GroupBy(t => t.AccountId);

        foreach (var group in byAccount)
        {
            var ordered = group.OrderBy(t => t.Date).ToList();
            if (ordered.Count < 2)
            {
                continue;
            }

            var historyDays = (ordered[^1].Date.ToDateTime(TimeOnly.MinValue) - ordered[0].Date.ToDateTime(TimeOnly.MinValue)).TotalDays;
            if (historyDays < 30)
            {
                continue;
            }

            var accountName = accountNames.TryGetValue(group.Key, out var name) ? name : string.Empty;

            for (var i = 0; i < ordered.Count - 1; i++)
            {
                var gapDays = (ordered[i + 1].Date.ToDateTime(TimeOnly.MinValue) - ordered[i].Date.ToDateTime(TimeOnly.MinValue)).TotalDays;
                if (gapDays > minGapDays)
                {
                    result.Add(new DateGapDto
                    {
                        AccountId = group.Key,
                        AccountName = accountName,
                        GapStart = ordered[i].Date.AddDays(1),
                        GapEnd = ordered[i + 1].Date.AddDays(-1),
                        DurationDays = (int)gapDays,
                    });
                }
            }
        }

        return result.OrderByDescending(g => g.DurationDays).ToList();
    }

    private static IReadOnlyList<DateGapDto> BuildDateGaps(
        IReadOnlyList<DateGapProjection> transactions,
        Dictionary<Guid, string> accountNames,
        int minGapDays)
    {
        var result = new List<DateGapDto>();
        var byAccount = transactions.GroupBy(t => t.AccountId);

        foreach (var group in byAccount)
        {
            var ordered = group.OrderBy(t => t.Date).ToList();
            if (ordered.Count < 2)
            {
                continue;
            }

            var historyDays = (ordered[^1].Date.ToDateTime(TimeOnly.MinValue) - ordered[0].Date.ToDateTime(TimeOnly.MinValue)).TotalDays;
            if (historyDays < 30)
            {
                continue;
            }

            var accountName = accountNames.TryGetValue(group.Key, out var name) ? name : string.Empty;

            for (var i = 0; i < ordered.Count - 1; i++)
            {
                var gapDays = (ordered[i + 1].Date.ToDateTime(TimeOnly.MinValue) - ordered[i].Date.ToDateTime(TimeOnly.MinValue)).TotalDays;
                if (gapDays > minGapDays)
                {
                    result.Add(new DateGapDto
                    {
                        AccountId = group.Key,
                        AccountName = accountName,
                        GapStart = ordered[i].Date.AddDays(1),
                        GapEnd = ordered[i + 1].Date.AddDays(-1),
                        DurationDays = (int)gapDays,
                    });
                }
            }
        }

        return result.OrderByDescending(g => g.DurationDays).ToList();
    }

    private static UncategorizedSummaryDto BuildUncategorizedSummary(
        IReadOnlyList<Transaction> transactions,
        Dictionary<Guid, string> accountNames)
    {
        var byAccount = transactions
            .GroupBy(t => t.AccountId)
            .Select(g => new AccountUncategorizedSummaryDto
            {
                AccountId = g.Key,
                AccountName = accountNames.TryGetValue(g.Key, out var name) ? name : string.Empty,
                Count = g.Count(),
                Amount = g.Sum(t => Math.Abs(t.Amount.Amount)),
            })
            .ToList();

        return new UncategorizedSummaryDto
        {
            ByAccount = byAccount,
            TotalCount = byAccount.Sum(a => a.Count),
            TotalAmount = byAccount.Sum(a => a.Amount),
        };
    }

    private static IReadOnlyList<DuplicateDetectionProjection> FilterByAccount(
        IReadOnlyList<DuplicateDetectionProjection> projections,
        Guid? accountId)
    {
        return accountId.HasValue
            ? projections.Where(p => p.AccountId == accountId.Value).ToList()
            : projections;
    }

    private static IReadOnlyList<DateGapProjection> FilterByAccount(
        IReadOnlyList<DateGapProjection> projections,
        Guid? accountId)
    {
        return accountId.HasValue
            ? projections.Where(p => p.AccountId == accountId.Value).ToList()
            : projections;
    }

    private static TransactionDto ToTransactionDto(DuplicateDetectionProjection projection)
    {
        return new TransactionDto
        {
            Id = projection.Id,
            AccountId = projection.AccountId,
            Date = projection.Date,
            Description = projection.Description,
            Amount = new MoneyDto
            {
                Amount = projection.Amount,
                Currency = CurrencyDefaults.DefaultCurrency,
            },
        };
    }

    private static TransactionDto ToTransactionDto(OutlierProjection projection)
    {
        return new TransactionDto
        {
            Id = projection.Id,
            Description = projection.Description,
            Amount = new MoneyDto
            {
                Amount = projection.Amount,
                Currency = CurrencyDefaults.DefaultCurrency,
            },
        };
    }

    private async Task<IReadOnlyList<OutlierProjection>> FilterOutlierProjectionsAsync(
        Guid? accountId,
        IReadOnlyList<DuplicateDetectionProjection> duplicateProjections,
        IReadOnlyList<OutlierProjection> outlierProjections,
        CancellationToken ct)
    {
        if (!accountId.HasValue)
        {
            return outlierProjections;
        }

        var accountTransactionIds = duplicateProjections
            .Where(p => p.AccountId == accountId.Value)
            .Select(p => p.Id)
            .ToHashSet();

        if (accountTransactionIds.Count > 0)
        {
            return outlierProjections
                .Where(p => accountTransactionIds.Contains(p.Id))
                .ToList();
        }

        var accountTransactions = await _transactions.GetByDateRangeAsync(
            DateOnly.MinValue,
            DateOnly.MaxValue,
            accountId.Value,
            ct);

        var accountIdsFromRange = accountTransactions
            .Select(t => t.Id)
            .ToHashSet();

        return outlierProjections
            .Where(p => accountIdsFromRange.Contains(p.Id))
            .ToList();
    }
}
