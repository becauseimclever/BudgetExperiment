// <copyright file="ImportPreviewEnricher.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;
using BudgetExperiment.Domain.Settings;

namespace BudgetExperiment.Application.Import;

/// <summary>
/// Enriches import preview rows with recurring transaction matches and location data.
/// </summary>
public sealed class ImportPreviewEnricher : IImportPreviewEnricher
{
    private readonly IRecurringTransactionRepository _recurringRepository;
    private readonly IRecurringInstanceProjector _instanceProjector;
    private readonly ITransactionMatcher _transactionMatcher;
    private readonly ILocationParserService _locationParser;
    private readonly IAppSettingsRepository _settingsRepository;
    private readonly ICurrencyProvider _currencyProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImportPreviewEnricher"/> class.
    /// </summary>
    /// <param name="recurringRepository">Recurring transaction repository.</param>
    /// <param name="instanceProjector">Recurring instance projector.</param>
    /// <param name="transactionMatcher">Transaction matcher.</param>
    /// <param name="locationParser">Location parser service.</param>
    /// <param name="settingsRepository">App settings repository.</param>
    /// <param name="currencyProvider">The currency provider.</param>
    public ImportPreviewEnricher(
        IRecurringTransactionRepository recurringRepository,
        IRecurringInstanceProjector instanceProjector,
        ITransactionMatcher transactionMatcher,
        ILocationParserService locationParser,
        IAppSettingsRepository settingsRepository,
        ICurrencyProvider currencyProvider)
    {
        _recurringRepository = recurringRepository;
        _instanceProjector = instanceProjector;
        _transactionMatcher = transactionMatcher;
        _locationParser = locationParser;
        _settingsRepository = settingsRepository;
        _currencyProvider = currencyProvider;
    }

    /// <inheritdoc />
    public async Task<List<ImportPreviewRow>> EnrichWithRecurringMatchesAsync(
        List<ImportPreviewRow> rows,
        CancellationToken cancellationToken = default)
    {
        var validRowsWithDates = rows
            .Where(r => r.Date.HasValue && r.Amount.HasValue)
            .ToList();

        if (validRowsWithDates.Count == 0)
        {
            return rows;
        }

        var dates = validRowsWithDates.Select(r => r.Date!.Value).ToList();
        var minDate = dates.Min().AddDays(-7);
        var maxDate = dates.Max().AddDays(7);

        var recurringTransactions = await _recurringRepository.GetActiveAsync(cancellationToken);
        if (recurringTransactions.Count == 0)
        {
            return rows;
        }

        var instancesByDate = await _instanceProjector.GetInstancesByDateRangeAsync(
            recurringTransactions, minDate, maxDate, excludeDates: null, cancellationToken);

        var allCandidates = instancesByDate.Values.SelectMany(list => list).ToList();
        if (allCandidates.Count == 0)
        {
            return rows;
        }

        var tolerances = MatchingTolerancesValue.Default;
        var currency = await _currencyProvider.GetCurrencyAsync(cancellationToken);

        return this.MatchRowsToCandidates(rows, allCandidates, tolerances, currency);
    }

    /// <inheritdoc />
    public async Task<List<ImportPreviewRow>> EnrichWithLocationDataAsync(
        List<ImportPreviewRow> rows,
        CancellationToken cancellationToken = default)
    {
        var settings = await _settingsRepository.GetAsync(cancellationToken);
        if (!settings.EnableLocationData)
        {
            return rows;
        }

        var descriptions = rows.Select(r => r.Description).ToList();
        var parseResults = _locationParser.ParseBatch(descriptions);

        var enrichedRows = new List<ImportPreviewRow>();
        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var parseResult = parseResults[i];

            if (parseResult.Location != null)
            {
                var locationPreview = new ImportLocationPreview
                {
                    City = parseResult.Location.City,
                    StateOrRegion = parseResult.Location.StateOrRegion,
                    Country = parseResult.Location.Country,
                    PostalCode = parseResult.Location.PostalCode,
                    Confidence = parseResult.Confidence,
                    IsAccepted = true,
                };

                enrichedRows.Add(row with { ParsedLocation = locationPreview });
            }
            else
            {
                enrichedRows.Add(row);
            }
        }

        return enrichedRows;
    }

    private static ImportRecurringMatchPreview BuildMatchPreview(
        TransactionMatchResultValue match,
        IReadOnlyList<RecurringInstanceInfoValue> candidates,
        MatchingTolerancesValue tolerances)
    {
        var source = candidates.First(c => c.RecurringTransactionId == match.RecurringTransactionId);
        return new ImportRecurringMatchPreview
        {
            RecurringTransactionId = match.RecurringTransactionId,
            RecurringDescription = source.Description,
            InstanceDate = match.InstanceDate,
            ExpectedAmount = source.Amount.Amount,
            ConfidenceScore = match.ConfidenceScore,
            ConfidenceLevel = match.ConfidenceLevel.ToString(),
            WouldAutoMatch = match.ConfidenceScore >= tolerances.AutoMatchThreshold,
        };
    }

    private static Transaction CreatePreviewTransaction(string description, decimal amount, DateOnly date, string currency)
    {
        return Transaction.Create(
            Guid.NewGuid(),
            MoneyValue.Create(currency, amount),
            date,
            description);
    }

    private List<ImportPreviewRow> MatchRowsToCandidates(
        List<ImportPreviewRow> rows,
        IReadOnlyList<RecurringInstanceInfoValue> allCandidates,
        MatchingTolerancesValue tolerances,
        string currency)
    {
        var enrichedRows = new List<ImportPreviewRow>();
        foreach (var row in rows)
        {
            if (!row.Date.HasValue || !row.Amount.HasValue)
            {
                enrichedRows.Add(row);
                continue;
            }

            var bestMatch = this.FindBestMatch(
                row.Description, row.Amount.Value, row.Date.Value, allCandidates, tolerances, currency);

            if (bestMatch != null)
            {
                enrichedRows.Add(row with { RecurringMatch = BuildMatchPreview(bestMatch, allCandidates, tolerances) });
            }
            else
            {
                enrichedRows.Add(row);
            }
        }

        return enrichedRows;
    }

    private TransactionMatchResultValue? FindBestMatch(
        string description,
        decimal amount,
        DateOnly date,
        IReadOnlyList<RecurringInstanceInfoValue> candidates,
        MatchingTolerancesValue tolerances,
        string currency)
    {
        TransactionMatchResultValue? best = null;

        foreach (var candidate in candidates)
        {
            var matchResult = _transactionMatcher.CalculateMatch(
                CreatePreviewTransaction(description, amount, date, currency),
                candidate,
                tolerances);

            if (matchResult != null && (best == null || matchResult.ConfidenceScore > best.ConfidenceScore))
            {
                best = matchResult;
            }
        }

        return best;
    }
}
