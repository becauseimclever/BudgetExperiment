// <copyright file="CsvImportService.cs" company="Fortinbra">
// Copyright (c) 2025 Fortinbra (becauseimclever.com). All rights reserved.
// </copyright>

using BudgetExperiment.Application.CsvImport.Models;
using BudgetExperiment.Application.CsvImport.Parsers;
using BudgetExperiment.Domain;
using Microsoft.Extensions.Options;

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
    private readonly CsvImportDeduplicationOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="CsvImportService"/> class.
    /// </summary>
    /// <param name="readRepository">Read repository.</param>
    /// <param name="writeRepository">Write repository.</param>
    /// <param name="unitOfWork">Unit of work.</param>
    /// <param name="parsers">Collection of bank CSV parsers.</param>
    /// <param name="options">Optional deduplication configuration (uses defaults if not provided).</param>
    public CsvImportService(
        IAdhocTransactionReadRepository readRepository,
        IAdhocTransactionWriteRepository writeRepository,
        IUnitOfWork unitOfWork,
        IEnumerable<IBankCsvParser> parsers,
        IOptions<CsvImportDeduplicationOptions>? options = null)
    {
        this._readRepository = readRepository ?? throw new ArgumentNullException(nameof(readRepository));
        this._writeRepository = writeRepository ?? throw new ArgumentNullException(nameof(writeRepository));
        this._unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        this._parsers = parsers?.ToDictionary(p => p.BankType, p => p) ?? throw new ArgumentNullException(nameof(parsers));
        this._options = options?.Value ?? new CsvImportDeduplicationOptions();
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
            var duplicates = new List<DuplicateTransaction>();
        var successCount = 0;
        var failedCount = 0;
    var duplicatesSkipped = 0;

        try
        {
            // Parse CSV
            var parsedTransactions = await parser.ParseAsync(csvStream, cancellationToken).ConfigureAwait(false);

            // Process each transaction
            for (int i = 0; i < parsedTransactions.Count; i++)
            {
                var rowNumber = i + 2; // +1 for 1-based indexing, +1 for header row
                var parsed = parsedTransactions[i];
                    // Check for duplicates
                    var potentialDuplicates = await this._readRepository.FindDuplicatesAsync(
                        parsed.Date,
                        parsed.Description,
                        Math.Abs(parsed.Amount),
                        parsed.TransactionType,
                        cancellationToken).ConfigureAwait(false);

                    if (potentialDuplicates.Count > 0)
                    {
                        // Skip this transaction as it's a duplicate
                        duplicatesSkipped++;
                        duplicates.Add(new DuplicateTransaction(
                            rowNumber,
                            parsed.Date,
                            parsed.Description,
                            Math.Abs(parsed.Amount),
                            potentialDuplicates[0].Id));
                        continue;
                    }

                    // Phase 5: Advanced deduplication (fuzzy matching)
                    // Search within date proximity (±N days) and same amount/type, then fuzzy match description
                    var window = this._options.EffectiveDateWindowDays;
                    var startDate = parsed.Date.AddDays(-window);
                    var endDate = parsed.Date.AddDays(window);
                    var nearby = await this._readRepository.GetByDateRangeAsync(startDate, endDate, cancellationToken).ConfigureAwait(false);

                    if (nearby.Count > 0)
                    {
                        var targetNormalized = NormalizeDescription(parsed.Description);
                        var targetTokens = ExtractKeywords(parsed.Description);
                        var amountAbs = Math.Abs(parsed.Amount);
                        var candidates = nearby.Where(t =>
                            t.TransactionType == parsed.TransactionType &&
                            Math.Abs(t.Money.Amount) == amountAbs).ToList();

                        Guid? fuzzyMatchId = null;
                        foreach (var candidate in candidates)
                        {
                            var candNormalized = NormalizeDescription(candidate.Description);
                            var candTokens = ExtractKeywords(candidate.Description);

                            // Try Levenshtein on normalized strings
                            var dist = LevenshteinDistance(targetNormalized, candNormalized);
                            if (dist <= this._options.EffectiveMaxLevenshtein)
                            {
                                fuzzyMatchId = candidate.Id;
                                break;
                            }

                            // Try keyword-based matching (Jaccard similarity)
                            var jaccardScore = JaccardSimilarity(targetTokens, candTokens);
                            if (jaccardScore >= this._options.EffectiveMinJaccard)
                            {
                                fuzzyMatchId = candidate.Id;
                                break;
                            }
                        }

                        if (fuzzyMatchId.HasValue)
                        {
                            duplicatesSkipped++;
                            duplicates.Add(new DuplicateTransaction(
                                rowNumber,
                                parsed.Date,
                                parsed.Description,
                                amountAbs,
                                fuzzyMatchId.Value));
                            continue;
                        }
                    }


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
                DuplicatesSkipped: duplicatesSkipped,
                Errors: errors,
                Duplicates: duplicates);
        }
        catch (DomainException ex)
        {
            // Parser-level error (e.g., invalid CSV format)
            throw new DomainException($"Failed to parse CSV: {ex.Message}");
        }
    }

    private static string NormalizeDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return string.Empty;
        }

        // Remove common bank metadata patterns before normalization
        var cleaned = RemoveBankMetadata(description);

        // Uppercase, trim, remove most punctuation, collapse whitespace
        var chars = cleaned.Trim().ToUpperInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : (char.IsWhiteSpace(ch) ? ' ' : '\0'))
            .Where(ch => ch != '\0')
            .ToArray();

        var normalized = new string(chars);
        while (normalized.Contains("  ", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("  ", " ", StringComparison.Ordinal);
        }

        return normalized.Trim();
    }

    private static string RemoveBankMetadata(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return string.Empty;
        }

        var cleaned = description;

        // Remove dates in various formats (MM/DD, MM/DD/YY, MM/DD/YYYY, Date MM/DD/YY)
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\b\d{1,2}/\d{1,2}(?:/\d{2,4})?\b", " ");
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\bDate\s+\d{1,2}/\d{1,2}(?:/\d{2,4})?\b", " ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Remove confirmation numbers, IDs, transaction codes (Conf#, ID:, #followed by long digits)
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\bConf#\s*[A-Z0-9]+\b", " ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\bID:\s*[A-Z0-9]+\b", " ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\b(?:Tracer|Card|Check)\s+\d+\b", " ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"#\d{10,}", " "); // Long transaction IDs

        // Remove common bank keywords/phrases
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\b(?:PURCHASE|DEBIT CARD|DIGITAL CARD|MOBILE|DEPOSIT|WITHDRAWAL|ACH|PMNT SENT|PMNT|Visa Direct|Merchant Category Code|DES|INDN|CO ID|PPD|WEB|POS|Recurring)\b", " ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Remove phone numbers
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\b\d{3}-\d{3}-\d{4}\b", " ");
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\b\d{10}\b", " ");

        // Remove account/card masks (XXXXX, XXX-XX)
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\bX{3,}\d*\b", " ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\bXXX-XX\d+\b", " ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Remove website domains
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\b[a-zA-Z0-9-]+\.(?:com|net|org|bill)\b", " ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Remove state codes (two uppercase letters at word boundaries)
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\b[A-Z]{2}\b", " ");

        return cleaned;
    }

    private static HashSet<string> ExtractKeywords(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        // Clean and tokenize
        var cleaned = RemoveBankMetadata(description);
        var tokens = cleaned.Split(new[] { ' ', ',', '.', '-', '*', '/', '\\', '(', ')', '[', ']', '{', '}' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim().ToUpperInvariant())
            .Where(t => t.Length >= 3) // Ignore very short tokens
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return tokens;
    }

    private static double JaccardSimilarity(HashSet<string> set1, HashSet<string> set2)
    {
        if (set1.Count == 0 && set2.Count == 0)
        {
            return 1.0;
        }

        if (set1.Count == 0 || set2.Count == 0)
        {
            return 0.0;
        }

        var intersection = set1.Intersect(set2, StringComparer.OrdinalIgnoreCase).Count();
        var union = set1.Union(set2, StringComparer.OrdinalIgnoreCase).Count();

        return union > 0 ? (double)intersection / union : 0.0;
    }

    private static int LevenshteinDistance(string s, string t)
    {
        if (s == t)
        {
            return 0;
        }

        if (s.Length == 0)
        {
            return t.Length;
        }

        if (t.Length == 0)
        {
            return s.Length;
        }

        var rows = s.Length + 1;
        var cols = t.Length + 1;
        var d = new int[rows, cols];

        for (int i = 0; i < rows; i++) d[i, 0] = i;
        for (int j = 0; j < cols; j++) d[0, j] = j;

        for (int i = 1; i < rows; i++)
        {
            for (int j = 1; j < cols; j++)
            {
                var cost = s[i - 1] == t[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[rows - 1, cols - 1];
    }
}
