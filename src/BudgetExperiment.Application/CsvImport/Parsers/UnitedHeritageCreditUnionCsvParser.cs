// <copyright file="UnitedHeritageCreditUnionCsvParser.cs" company="Fortinbra">
// Copyright (c) 2025 Fortinbra (becauseimclever.com). All rights reserved.
// </copyright>

using System.Globalization;

using BudgetExperiment.Application.CsvImport.Models;
using BudgetExperiment.Domain;

using CsvHelper;
using CsvHelper.Configuration;

namespace BudgetExperiment.Application.CsvImport.Parsers;

/// <summary>
/// CSV parser for United Heritage Credit Union transaction exports.
/// </summary>
public sealed class UnitedHeritageCreditUnionCsvParser : IBankCsvParser
{
    /// <inheritdoc />
    public BankType BankType => BankType.UnitedHeritageCreditUnion;

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
                // UHCU CSV Format:
                // Account Number,Post Date,Check,Description,Debit,Credit,Status,Balance
                var accountNumber = csv.GetField<string>("Account Number")?.Trim();
                var dateString = csv.GetField<string>("Post Date")?.Trim();
                var checkNumber = csv.GetField<string>("Check")?.Trim();
                var description = csv.GetField<string>("Description")?.Trim();
                var debitString = csv.GetField<string>("Debit")?.Trim();
                var creditString = csv.GetField<string>("Credit")?.Trim();
                var status = csv.GetField<string>("Status")?.Trim();
                var balanceString = csv.GetField<string>("Balance")?.Trim();

                // Skip rows with both empty debit and credit
                if (string.IsNullOrWhiteSpace(debitString) && string.IsNullOrWhiteSpace(creditString))
                {
                    continue;
                }

                // Parse date (UHCU uses M/d/yyyy format)
                if (!DateOnly.TryParseExact(dateString, "M/d/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    throw new DomainException($"Invalid date format: '{dateString}'. Expected M/d/yyyy format.");
                }

                // Determine amount and transaction type based on Debit/Credit columns
                decimal amount;
                TransactionType transactionType;

                if (!string.IsNullOrWhiteSpace(creditString))
                {
                    // Credit = Income (positive amount)
                    if (!decimal.TryParse(creditString, NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out amount))
                    {
                        throw new DomainException($"Invalid credit amount format: '{creditString}'.");
                    }

                    transactionType = TransactionType.Income;
                }
                else
                {
                    // Debit = Expense (negative amount)
                    if (!decimal.TryParse(debitString, NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out amount))
                    {
                        throw new DomainException($"Invalid debit amount format: '{debitString}'.");
                    }

                    amount = -amount; // Make negative for expenses
                    transactionType = TransactionType.Expense;
                }

                // Skip zero amounts
                if (amount == 0)
                {
                    continue;
                }

                // Include check number in description if present
                var fullDescription = string.IsNullOrWhiteSpace(checkNumber)
                    ? description ?? string.Empty
                    : $"Check {checkNumber} - {description}";

                transactions.Add(new ParsedTransaction(
                    Date: date,
                    Description: fullDescription,
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

