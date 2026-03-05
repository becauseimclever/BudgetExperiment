// <copyright file="ImportTransactionCreator.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Import;

/// <summary>
/// Creates domain transactions from import data, tracking categorization sources
/// and location enrichment.
/// </summary>
public sealed class ImportTransactionCreator : IImportTransactionCreator
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICurrencyProvider _currencyProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImportTransactionCreator"/> class.
    /// </summary>
    /// <param name="transactionRepository">Transaction repository.</param>
    /// <param name="currencyProvider">Currency provider.</param>
    public ImportTransactionCreator(
        ITransactionRepository transactionRepository,
        ICurrencyProvider currencyProvider)
    {
        this._transactionRepository = transactionRepository;
        this._currencyProvider = currencyProvider;
    }

    /// <inheritdoc />
    public async Task<ImportTransactionResult> CreateTransactionsAsync(
        Account account,
        Guid batchId,
        IReadOnlyList<ImportTransactionData> transactions,
        CancellationToken cancellationToken = default)
    {
        var createdIds = new List<Guid>();
        int autoCategorized = 0;
        int csvCategorized = 0;
        int uncategorized = 0;
        int skipped = 0;
        int locationEnriched = 0;
        var currency = await this._currencyProvider.GetCurrencyAsync(cancellationToken);

        foreach (var txData in transactions)
        {
            try
            {
                var transaction = CreateAndConfigureTransaction(account, batchId, txData, currency);

                if (transaction.Location != null)
                {
                    locationEnriched++;
                }

                await this._transactionRepository.AddAsync(transaction, cancellationToken);
                createdIds.Add(transaction.Id);
                TrackCategorizationSource(txData.CategorySource, ref autoCategorized, ref csvCategorized, ref uncategorized);
            }
            catch (DomainException)
            {
                skipped++;
            }
        }

        return new ImportTransactionResult(createdIds, autoCategorized, csvCategorized, uncategorized, skipped, locationEnriched);
    }

    private static Transaction CreateAndConfigureTransaction(
        Account account,
        Guid batchId,
        ImportTransactionData txData,
        string currency)
    {
        var amount = MoneyValue.Create(currency, txData.Amount);
        var transaction = account.AddTransaction(amount, txData.Date, txData.Description, txData.CategoryId);
        transaction.SetImportBatch(batchId, txData.Reference);

        if (!string.IsNullOrEmpty(txData.LocationCity) || !string.IsNullOrEmpty(txData.LocationStateOrRegion))
        {
            var locationSource = Enum.TryParse<LocationSource>(txData.LocationSource, true, out var src)
                ? src
                : LocationSource.Parsed;

            var location = TransactionLocationValue.Create(
                city: txData.LocationCity,
                stateOrRegion: txData.LocationStateOrRegion,
                country: txData.LocationCountry,
                postalCode: txData.LocationPostalCode,
                coordinates: null,
                source: locationSource);

            transaction.SetLocation(location);
        }

        return transaction;
    }

    private static void TrackCategorizationSource(
        CategorySource source,
        ref int autoCategorized,
        ref int csvCategorized,
        ref int uncategorized)
    {
        switch (source)
        {
            case CategorySource.AutoRule:
                autoCategorized++;
                break;
            case CategorySource.CsvColumn:
                csvCategorized++;
                break;
            case CategorySource.None:
                uncategorized++;
                break;
        }
    }
}
