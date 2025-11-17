// <copyright file="CsvImportService.cs" company="Fortinbra">
// Copyright (c) 2025 Fortinbra (becauseimclever.com). All rights reserved.
// </copyright>

using BudgetExperiment.Application.CsvImport.Models;
using BudgetExperiment.Application.CsvImport.Parsers;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.CsvImport;

/// <summary>
/// Service for importing bank transactions from CSV files.
/// </summary>
public sealed class CsvImportService : ICsvImportService
{
    private readonly IAdhocTransactionReadRepository _readRepository;
    private readonly IAdhocTransactionWriteRepository _writeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly Dictionary<BankType, IBankCsvParser> _parsers;

    /// <summary>
    /// Initializes a new instance of the <see cref="CsvImportService"/> class.
    /// </summary>
    /// <param name="readRepository">Read repository.</param>
    /// <param name="writeRepository">Write repository.</param>
    /// <param name="unitOfWork">Unit of work.</param>
    /// <param name="parsers">Collection of bank CSV parsers.</param>
    public CsvImportService(
        IAdhocTransactionReadRepository readRepository,
        IAdhocTransactionWriteRepository writeRepository,
        IUnitOfWork unitOfWork,
        IEnumerable<IBankCsvParser> parsers)
    {
        this._readRepository = readRepository ?? throw new ArgumentNullException(nameof(readRepository));
        this._writeRepository = writeRepository ?? throw new ArgumentNullException(nameof(writeRepository));
        this._unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        this._parsers = parsers?.ToDictionary(p => p.BankType, p => p) ?? throw new ArgumentNullException(nameof(parsers));
    }

    /// <inheritdoc />
    public async Task<CsvImportResult> ImportAsync(Stream csvStream, BankType bankType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(csvStream);

        if (!this._parsers.TryGetValue(bankType, out var parser))
        {
            throw new ArgumentException($"No parser registered for bank type: {bankType}", nameof(bankType));
        }

        var errors = new List<CsvImportError>();
        var successCount = 0;
        var failedCount = 0;

        try
        {
            // Parse CSV
            var parsedTransactions = await parser.ParseAsync(csvStream, cancellationToken).ConfigureAwait(false);

            // Process each transaction
            for (int i = 0; i < parsedTransactions.Count; i++)
            {
                var rowNumber = i + 2; // +1 for 1-based indexing, +1 for header row
                var parsed = parsedTransactions[i];

                try
                {
                    // Create domain entity based on transaction type
                    var money = MoneyValue.Create("USD", Math.Abs(parsed.Amount));

                    AdhocTransaction transaction;
                    if (parsed.TransactionType == TransactionType.Income)
                    {
                        transaction = AdhocTransaction.CreateIncome(parsed.Description, money, parsed.Date, parsed.Category);
                    }
                    else
                    {
                        transaction = AdhocTransaction.CreateExpense(parsed.Description, money, parsed.Date, parsed.Category);
                    }

                    await this._writeRepository.AddAsync(transaction, cancellationToken).ConfigureAwait(false);
                    successCount++;
                }
                catch (DomainException ex)
                {
                    failedCount++;
                    errors.Add(new CsvImportError(rowNumber, "Transaction", ex.Message));
                }
                catch (Exception ex)
                {
                    failedCount++;
                    errors.Add(new CsvImportError(rowNumber, "Unknown", $"Unexpected error: {ex.Message}"));
                }
            }

            // Save all changes if any transactions were added
            if (successCount > 0)
            {
                await this._unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            return new CsvImportResult(
                TotalRows: parsedTransactions.Count,
                SuccessfulImports: successCount,
                FailedImports: failedCount,
                DuplicatesSkipped: 0, // Phase 1: No duplicate detection
                Errors: errors);
        }
        catch (DomainException ex)
        {
            // Parser-level error (e.g., invalid CSV format)
            throw new DomainException($"Failed to parse CSV: {ex.Message}");
        }
    }
}
