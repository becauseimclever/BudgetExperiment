// <copyright file="CapitalOneCsvParser.cs" company="Fortinbra">
// Copyright (c) 2025 Fortinbra (becauseimclever.com). All rights reserved.
// </copyright>

using System.Globalization;

using BudgetExperiment.Application.CsvImport.Models;
using BudgetExperiment.Domain;

using CsvHelper;
using CsvHelper.Configuration;

namespace BudgetExperiment.Application.CsvImport.Parsers;

/// <summary>
/// CSV parser for Capital One transaction exports.
/// </summary>
public sealed class CapitalOneCsvParser : IBankCsvParser
{
    /// <inheritdoc />
    public BankType BankType => BankType.CapitalOne;

    /// <inheritdoc />
    public async Task<IReadOnlyList<ParsedTransaction>> ParseAsync(Stream csvStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(csvStream);

        var transactions = new List<ParsedTransaction>();

        using var reader = new StreamReader(csvStream);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
        };

        using var csv = new CsvReader(reader, config);

        // Read header
        await csv.ReadAsync().ConfigureAwait(false);
        csv.ReadHeader();

        // Read transaction records
        while (await csv.ReadAsync().ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // Capital One CSV Format:
                // Account Number,Transaction Description,Transaction Date,Transaction Type,Transaction Amount,Balance
                var accountNumber = csv.GetField<string>("Account Number")?.Trim();
                var description = csv.GetField<string>("Transaction Description")?.Trim();
                var dateString = csv.GetField<string>("Transaction Date")?.Trim();
                var transactionTypeString = csv.GetField<string>("Transaction Type")?.Trim();
                var amountString = csv.GetField<string>("Transaction Amount")?.Trim();

                // Skip rows with empty or zero amounts
                if (string.IsNullOrWhiteSpace(amountString))
                {
                    continue;
                }

                // Parse amount
                if (!decimal.TryParse(amountString, NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var amount))
                {
                    throw new DomainException($"Invalid amount format: '{amountString}'.");
                }

                // Skip zero amounts (like prenotes)
                if (amount == 0)
                {
                    continue;
                }

                // Parse date (Capital One uses MM/dd/yy format with 2-digit year)
                if (!DateOnly.TryParseExact(dateString, "MM/dd/yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    throw new DomainException($"Invalid date format: '{dateString}'. Expected MM/dd/yy format.");
                }

                // Determine transaction type from Transaction Type column
                var transactionType = transactionTypeString?.ToUpperInvariant() switch
                {
                    "DEBIT" => TransactionType.Expense,
                    "CREDIT" => TransactionType.Income,
                    _ => throw new DomainException($"Unrecognized transaction type: '{transactionTypeString}'. Expected 'Debit' or 'Credit'."),
                };

                transactions.Add(new ParsedTransaction(
                    Date: date,
                    Description: description ?? string.Empty,
                    Amount: amount,
                    TransactionType: transactionType,
                    Category: null));
            }
            catch (DomainException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new DomainException($"Error parsing CSV row: {ex.Message}");
            }
        }

        return transactions;
    }
}
