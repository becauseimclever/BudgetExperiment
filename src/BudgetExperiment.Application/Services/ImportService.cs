// <copyright file="ImportService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Services;

/// <summary>
/// Service for importing transactions from CSV files.
/// </summary>
public sealed class ImportService : IImportService
{
    private static readonly string[] _commonDateFormats =
    [
        "MM/dd/yyyy",
        "M/d/yyyy",
        "MM-dd-yyyy",
        "M-d-yyyy",
        "yyyy-MM-dd",
        "yyyy/MM/dd",
        "dd/MM/yyyy",
        "d/M/yyyy",
        "dd-MM-yyyy",
        "d-M-yyyy",
        "MMM dd, yyyy",
        "MMMM dd, yyyy",
        "dd MMM yyyy",
        "MM/dd/yy",
        "M/d/yy",
    ];

    private readonly ITransactionRepository _transactionRepository;
    private readonly ICategorizationRuleRepository _ruleRepository;
    private readonly IBudgetCategoryRepository _categoryRepository;
    private readonly IImportBatchRepository _batchRepository;
    private readonly IUserContext _userContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImportService"/> class.
    /// </summary>
    /// <param name="transactionRepository">Transaction repository.</param>
    /// <param name="ruleRepository">Categorization rule repository.</param>
    /// <param name="categoryRepository">Budget category repository.</param>
    /// <param name="batchRepository">Import batch repository.</param>
    /// <param name="userContext">User context.</param>
    public ImportService(
        ITransactionRepository transactionRepository,
        ICategorizationRuleRepository ruleRepository,
        IBudgetCategoryRepository categoryRepository,
        IImportBatchRepository batchRepository,
        IUserContext userContext)
    {
        this._transactionRepository = transactionRepository;
        this._ruleRepository = ruleRepository;
        this._categoryRepository = categoryRepository;
        this._batchRepository = batchRepository;
        this._userContext = userContext;
    }

    /// <inheritdoc />
    public async Task<ImportPreviewResult> PreviewAsync(ImportPreviewRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Rows.Count == 0)
        {
            return new ImportPreviewResult();
        }

        // Load active categorization rules
        var rules = await this._ruleRepository.GetActiveByPriorityAsync(cancellationToken);

        // Load categories for name matching (when CSV has category column)
        var categories = await this._categoryRepository.GetAllAsync(cancellationToken);
        var categoryByName = categories.ToDictionary(c => c.Name, c => c, StringComparer.OrdinalIgnoreCase);

        // Load existing transactions for duplicate detection
        IReadOnlyList<Transaction> existingTransactions = [];
        if (request.DuplicateSettings.Enabled)
        {
            var dates = ExtractDatesFromRows(request.Rows, request.Mappings, request.DateFormat);
            if (dates.Count > 0)
            {
                var minDate = dates.Min().AddDays(-request.DuplicateSettings.LookbackDays);
                var maxDate = dates.Max().AddDays(request.DuplicateSettings.LookbackDays);
                existingTransactions = await this._transactionRepository.GetForDuplicateDetectionAsync(
                    request.AccountId, minDate, maxDate, cancellationToken);
            }
        }

        // Process each row
        var previewRows = new List<ImportPreviewRow>();
        for (int i = 0; i < request.Rows.Count; i++)
        {
            var row = request.Rows[i];
            var previewRow = ProcessRow(
                i + 1, // 1-based row index for display
                row,
                request.Mappings,
                request.DateFormat,
                request.AmountMode,
                rules,
                categoryByName,
                existingTransactions,
                request.DuplicateSettings);
            previewRows.Add(previewRow);
        }

        // Calculate summary
        var validRows = previewRows.Where(r => r.Status == ImportRowStatus.Valid).ToList();
        var warningRows = previewRows.Where(r => r.Status == ImportRowStatus.Warning).ToList();
        var errorRows = previewRows.Where(r => r.Status == ImportRowStatus.Error).ToList();
        var duplicateRows = previewRows.Where(r => r.Status == ImportRowStatus.Duplicate).ToList();
        var autoCategorized = previewRows.Count(r => r.CategorySource == CategorySource.AutoRule);

        return new ImportPreviewResult
        {
            Rows = previewRows,
            ValidCount = validRows.Count,
            WarningCount = warningRows.Count,
            ErrorCount = errorRows.Count,
            DuplicateCount = duplicateRows.Count,
            TotalAmount = validRows.Where(r => r.Amount.HasValue).Sum(r => r.Amount!.Value),
            AutoCategorizedCount = autoCategorized,
        };
    }

    /// <inheritdoc />
    public Task<ImportResult> ExecuteAsync(ImportExecuteRequest request, CancellationToken cancellationToken = default)
    {
        // Phase 4 implementation
        throw new NotImplementedException("Import execution will be implemented in Phase 4.");
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ImportBatchDto>> GetImportHistoryAsync(CancellationToken cancellationToken = default)
    {
        // Phase 4 implementation
        throw new NotImplementedException("Import history will be implemented in Phase 4.");
    }

    /// <inheritdoc />
    public Task<int> DeleteImportBatchAsync(Guid batchId, CancellationToken cancellationToken = default)
    {
        // Phase 4 implementation
        throw new NotImplementedException("Batch deletion will be implemented in Phase 4.");
    }

    private static ImportPreviewRow ProcessRow(
        int rowIndex,
        IReadOnlyList<string> row,
        IReadOnlyList<ColumnMappingDto> mappings,
        string dateFormat,
        AmountParseMode amountMode,
        IReadOnlyList<CategorizationRule> rules,
        Dictionary<string, BudgetCategory> categoryByName,
        IReadOnlyList<Transaction> existingTransactions,
        DuplicateDetectionSettingsDto duplicateSettings)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // Extract values from row using mappings
        string? dateStr = null;
        string? description = null;
        string? amountStr = null;
        string? debitStr = null;
        string? creditStr = null;
        string? categoryName = null;
        string? reference = null;

        foreach (var mapping in mappings)
        {
            if (mapping.ColumnIndex < 0 || mapping.ColumnIndex >= row.Count)
            {
                continue;
            }

            var value = row[mapping.ColumnIndex]?.Trim();
            if (string.IsNullOrEmpty(value))
            {
                continue;
            }

            switch (mapping.TargetField)
            {
                case ImportField.Date:
                    dateStr = value;
                    break;
                case ImportField.Description:
                    description = string.IsNullOrEmpty(description) ? value : $"{description} {value}";
                    break;
                case ImportField.Amount:
                    amountStr = value;
                    break;
                case ImportField.DebitAmount:
                    debitStr = value;
                    break;
                case ImportField.CreditAmount:
                    creditStr = value;
                    break;
                case ImportField.Category:
                    categoryName = value;
                    break;
                case ImportField.Reference:
                    reference = value;
                    break;
            }
        }

        // Parse date
        DateOnly? date = null;
        if (!string.IsNullOrEmpty(dateStr))
        {
            date = ParseDate(dateStr, dateFormat);
            if (!date.HasValue)
            {
                errors.Add($"Could not parse date: '{dateStr}'");
            }
        }
        else
        {
            errors.Add("Date is required");
        }

        // Parse amount
        decimal? amount = null;
        if (!string.IsNullOrEmpty(amountStr))
        {
            amount = ParseAmount(amountStr, amountMode);
            if (!amount.HasValue)
            {
                errors.Add($"Could not parse amount: '{amountStr}'");
            }
        }
        else if (!string.IsNullOrEmpty(debitStr) || !string.IsNullOrEmpty(creditStr))
        {
            // Use debit/credit columns
            var debit = ParseAmountValue(debitStr);
            var credit = ParseAmountValue(creditStr);

            if (debit.HasValue && debit.Value != 0)
            {
                amount = -Math.Abs(debit.Value); // Debits are expenses (negative)
            }
            else if (credit.HasValue && credit.Value != 0)
            {
                amount = Math.Abs(credit.Value); // Credits are income (positive)
            }
            else if (!string.IsNullOrEmpty(debitStr) && !debit.HasValue)
            {
                errors.Add($"Could not parse debit amount: '{debitStr}'");
            }
            else if (!string.IsNullOrEmpty(creditStr) && !credit.HasValue)
            {
                errors.Add($"Could not parse credit amount: '{creditStr}'");
            }
        }
        else
        {
            errors.Add("Amount is required");
        }

        // Validate description
        if (string.IsNullOrWhiteSpace(description))
        {
            errors.Add("Description is required");
            description = string.Empty;
        }

        // Determine category
        Guid? categoryId = null;
        CategorySource categorySource = CategorySource.None;
        string? matchedRuleName = null;
        Guid? matchedRuleId = null;

        // Priority 1: CSV column category
        if (!string.IsNullOrWhiteSpace(categoryName))
        {
            if (categoryByName.TryGetValue(categoryName, out var category))
            {
                categoryId = category.Id;
                categorySource = CategorySource.CsvColumn;
            }
            else
            {
                warnings.Add($"Category '{categoryName}' not found");
            }
        }

        // Priority 2: Auto-categorization rules (only if no CSV category matched)
        if (!categoryId.HasValue && !string.IsNullOrWhiteSpace(description))
        {
            foreach (var rule in rules)
            {
                if (rule.Matches(description))
                {
                    categoryId = rule.CategoryId;
                    categorySource = CategorySource.AutoRule;
                    matchedRuleName = rule.Name;
                    matchedRuleId = rule.Id;
                    break;
                }
            }
        }

        // Check for duplicates
        Guid? duplicateOfId = null;
        if (duplicateSettings.Enabled && date.HasValue && amount.HasValue && errors.Count == 0)
        {
            duplicateOfId = FindDuplicate(
                date.Value,
                amount.Value,
                description,
                existingTransactions,
                duplicateSettings);
        }

        // Determine final status
        ImportRowStatus status;
        string? statusMessage = null;

        if (errors.Count > 0)
        {
            status = ImportRowStatus.Error;
            statusMessage = string.Join("; ", errors);
        }
        else if (duplicateOfId.HasValue)
        {
            status = ImportRowStatus.Duplicate;
            statusMessage = "Possible duplicate of existing transaction";
        }
        else if (warnings.Count > 0)
        {
            status = ImportRowStatus.Warning;
            statusMessage = string.Join("; ", warnings);
        }
        else
        {
            status = ImportRowStatus.Valid;
        }

        return new ImportPreviewRow
        {
            RowIndex = rowIndex,
            Date = date,
            Description = description,
            Amount = amount,
            Category = categoryId.HasValue && categoryByName.Values.FirstOrDefault(c => c.Id == categoryId) is { } cat
                ? cat.Name
                : categoryName,
            CategoryId = categoryId,
            CategorySource = categorySource,
            MatchedRuleName = matchedRuleName,
            MatchedRuleId = matchedRuleId,
            Reference = reference,
            Status = status,
            StatusMessage = statusMessage,
            DuplicateOfTransactionId = duplicateOfId,
            IsSelected = status != ImportRowStatus.Error && status != ImportRowStatus.Duplicate,
        };
    }

    private static DateOnly? ParseDate(string dateStr, string preferredFormat)
    {
        // Try the preferred format first
        if (DateOnly.TryParseExact(dateStr, preferredFormat, null, System.Globalization.DateTimeStyles.None, out var date))
        {
            return date;
        }

        // Try common formats
        foreach (var format in _commonDateFormats)
        {
            if (DateOnly.TryParseExact(dateStr, format, null, System.Globalization.DateTimeStyles.None, out date))
            {
                return date;
            }
        }

        // Try generic parse as last resort
        if (DateOnly.TryParse(dateStr, out date))
        {
            return date;
        }

        return null;
    }

    private static decimal? ParseAmount(string amountStr, AmountParseMode mode)
    {
        var value = ParseAmountValue(amountStr);
        if (!value.HasValue)
        {
            return null;
        }

        return mode switch
        {
            AmountParseMode.NegativeIsExpense => value.Value,
            AmountParseMode.PositiveIsExpense => -value.Value,
            AmountParseMode.AbsoluteExpense => -Math.Abs(value.Value),
            AmountParseMode.AbsoluteIncome => Math.Abs(value.Value),
            _ => value.Value,
        };
    }

    private static decimal? ParseAmountValue(string? amountStr)
    {
        if (string.IsNullOrWhiteSpace(amountStr))
        {
            return null;
        }

        // Clean up the amount string
        var cleaned = amountStr.Trim();

        // Handle parentheses as negative (accounting format)
        bool isNegative = cleaned.StartsWith('(') && cleaned.EndsWith(')');
        if (isNegative)
        {
            cleaned = cleaned[1..^1];
        }

        // Handle leading minus
        if (cleaned.StartsWith('-'))
        {
            isNegative = !isNegative; // Toggle if already negative from parentheses
            cleaned = cleaned[1..];
        }

        // Remove currency symbols and thousands separators
        cleaned = cleaned.Replace("$", string.Empty)
                        .Replace("£", string.Empty)
                        .Replace("€", string.Empty)
                        .Replace(",", string.Empty)
                        .Trim();

        if (decimal.TryParse(cleaned, out var amount))
        {
            return isNegative ? -amount : amount;
        }

        return null;
    }

    private static Guid? FindDuplicate(
        DateOnly date,
        decimal amount,
        string description,
        IReadOnlyList<Transaction> existingTransactions,
        DuplicateDetectionSettingsDto settings)
    {
        foreach (var existing in existingTransactions)
        {
            // Check date within range
            var daysDiff = Math.Abs(existing.Date.DayNumber - date.DayNumber);
            if (daysDiff > settings.LookbackDays)
            {
                continue;
            }

            // Check amount (must match exactly)
            if (existing.Amount.Amount != amount)
            {
                continue;
            }

            // Check description based on mode
            bool descriptionMatches = settings.DescriptionMatch switch
            {
                DescriptionMatchMode.Exact =>
                    string.Equals(existing.Description, description, StringComparison.OrdinalIgnoreCase),
                DescriptionMatchMode.Contains =>
                    existing.Description.Contains(description, StringComparison.OrdinalIgnoreCase) ||
                    description.Contains(existing.Description, StringComparison.OrdinalIgnoreCase),
                DescriptionMatchMode.StartsWith =>
                    existing.Description.StartsWith(description, StringComparison.OrdinalIgnoreCase) ||
                    description.StartsWith(existing.Description, StringComparison.OrdinalIgnoreCase),
                DescriptionMatchMode.Fuzzy =>
                    CalculateSimilarity(existing.Description, description) >= 0.8,
                _ => string.Equals(existing.Description, description, StringComparison.OrdinalIgnoreCase),
            };

            if (descriptionMatches)
            {
                return existing.Id;
            }
        }

        return null;
    }

    private static double CalculateSimilarity(string a, string b)
    {
        // Simple Levenshtein distance-based similarity
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
        {
            return 0;
        }

        a = a.ToLowerInvariant();
        b = b.ToLowerInvariant();

        int maxLen = Math.Max(a.Length, b.Length);
        int distance = LevenshteinDistance(a, b);

        return 1.0 - ((double)distance / maxLen);
    }

    private static int LevenshteinDistance(string a, string b)
    {
        int[,] dp = new int[a.Length + 1, b.Length + 1];

        for (int i = 0; i <= a.Length; i++)
        {
            dp[i, 0] = i;
        }

        for (int j = 0; j <= b.Length; j++)
        {
            dp[0, j] = j;
        }

        for (int i = 1; i <= a.Length; i++)
        {
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                dp[i, j] = Math.Min(
                    Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                    dp[i - 1, j - 1] + cost);
            }
        }

        return dp[a.Length, b.Length];
    }

    private static List<DateOnly> ExtractDatesFromRows(
        IReadOnlyList<IReadOnlyList<string>> rows,
        IReadOnlyList<ColumnMappingDto> mappings,
        string dateFormat)
    {
        var dateMapping = mappings.FirstOrDefault(m => m.TargetField == ImportField.Date);
        if (dateMapping is null)
        {
            return [];
        }

        var dates = new List<DateOnly>();
        foreach (var row in rows)
        {
            if (dateMapping.ColumnIndex >= 0 && dateMapping.ColumnIndex < row.Count)
            {
                var dateStr = row[dateMapping.ColumnIndex];
                var date = ParseDate(dateStr, dateFormat);
                if (date.HasValue)
                {
                    dates.Add(date.Value);
                }
            }
        }

        return dates;
    }
}
