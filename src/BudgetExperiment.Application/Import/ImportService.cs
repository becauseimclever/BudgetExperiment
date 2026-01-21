// <copyright file="ImportService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Import;

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
    private readonly IImportMappingRepository _mappingRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IRecurringTransactionRepository _recurringRepository;
    private readonly IRecurringInstanceProjector _instanceProjector;
    private readonly ITransactionMatcher _transactionMatcher;
    private readonly IReconciliationService _reconciliationService;
    private readonly IUserContext _userContext;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImportService"/> class.
    /// </summary>
    /// <param name="transactionRepository">Transaction repository.</param>
    /// <param name="ruleRepository">Categorization rule repository.</param>
    /// <param name="categoryRepository">Budget category repository.</param>
    /// <param name="batchRepository">Import batch repository.</param>
    /// <param name="mappingRepository">Import mapping repository.</param>
    /// <param name="accountRepository">Account repository.</param>
    /// <param name="recurringRepository">Recurring transaction repository.</param>
    /// <param name="instanceProjector">Recurring instance projector.</param>
    /// <param name="transactionMatcher">Transaction matcher.</param>
    /// <param name="reconciliationService">Reconciliation service.</param>
    /// <param name="userContext">User context.</param>
    /// <param name="unitOfWork">Unit of work.</param>
    public ImportService(
        ITransactionRepository transactionRepository,
        ICategorizationRuleRepository ruleRepository,
        IBudgetCategoryRepository categoryRepository,
        IImportBatchRepository batchRepository,
        IImportMappingRepository mappingRepository,
        IAccountRepository accountRepository,
        IRecurringTransactionRepository recurringRepository,
        IRecurringInstanceProjector instanceProjector,
        ITransactionMatcher transactionMatcher,
        IReconciliationService reconciliationService,
        IUserContext userContext,
        IUnitOfWork unitOfWork)
    {
        this._transactionRepository = transactionRepository;
        this._ruleRepository = ruleRepository;
        this._categoryRepository = categoryRepository;
        this._batchRepository = batchRepository;
        this._mappingRepository = mappingRepository;
        this._accountRepository = accountRepository;
        this._recurringRepository = recurringRepository;
        this._instanceProjector = instanceProjector;
        this._transactionMatcher = transactionMatcher;
        this._reconciliationService = reconciliationService;
        this._userContext = userContext;
        this._unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<ImportPreviewResult> PreviewAsync(ImportPreviewRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Rows.Count == 0)
        {
            return new ImportPreviewResult();
        }

        // Apply skip rows - skip the first N data rows (metadata rows)
        var rowsToProcess = request.Rows;
        var skippedRows = new List<IReadOnlyList<string>>();
        if (request.RowsToSkip > 0 && request.RowsToSkip < request.Rows.Count)
        {
            skippedRows = request.Rows.Take(request.RowsToSkip).ToList();
            rowsToProcess = request.Rows.Skip(request.RowsToSkip).ToList();
        }
        else if (request.RowsToSkip >= request.Rows.Count)
        {
            // All rows are skipped - nothing to process
            return new ImportPreviewResult();
        }

        // Load active categorization rules
        var rules = await this._ruleRepository.GetActiveByPriorityAsync(cancellationToken);

        // Load categories for name matching (when CSV has category column)
        // Use first match for duplicate names (prefer shared categories)
        var categories = await this._categoryRepository.GetAllAsync(cancellationToken);
        var categoryByName = categories
            .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        // Load existing transactions for duplicate detection
        IReadOnlyList<Transaction> existingTransactions = [];
        if (request.DuplicateSettings.Enabled)
        {
            var dates = ExtractDatesFromRows(rowsToProcess, request.Mappings, request.DateFormat);
            if (dates.Count > 0)
            {
                var minDate = dates.Min().AddDays(-request.DuplicateSettings.LookbackDays);
                var maxDate = dates.Max().AddDays(request.DuplicateSettings.LookbackDays);
                existingTransactions = await this._transactionRepository.GetForDuplicateDetectionAsync(
                    request.AccountId, minDate, maxDate, cancellationToken);
            }
        }

        // Process each row (using filtered rows after skip)
        var previewRows = new List<ImportPreviewRow>();
        for (int i = 0; i < rowsToProcess.Count; i++)
        {
            var row = rowsToProcess[i];
            var previewRow = ProcessRow(
                request.RowsToSkip + i + 1, // Row index accounts for skipped rows + 1-based display
                row,
                request.Mappings,
                request.DateFormat,
                request.AmountMode,
                request.IndicatorSettings,
                rules,
                categoryByName,
                existingTransactions,
                request.DuplicateSettings);
            previewRows.Add(previewRow);
        }

        // Check for recurring transaction matches if enabled
        if (request.CheckRecurringMatches)
        {
            previewRows = await this.EnrichWithRecurringMatchesAsync(previewRows, cancellationToken);
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
    public async Task<ImportResult> ExecuteAsync(ImportExecuteRequest request, CancellationToken cancellationToken = default)
    {
        var userId = this.GetRequiredUserId();

        // Validate account exists
        var account = await this._accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null)
        {
            throw new DomainException($"Account with ID '{request.AccountId}' not found.");
        }

        if (request.Transactions.Count == 0)
        {
            return new ImportResult
            {
                BatchId = Guid.Empty,
                ImportedCount = 0,
            };
        }

        // Create import batch
        var batch = ImportBatch.Create(
            userId,
            request.AccountId,
            request.FileName,
            request.Transactions.Count,
            request.MappingId);

        await this._batchRepository.AddAsync(batch, cancellationToken);

        // If a mapping was used, mark it as recently used
        if (request.MappingId.HasValue)
        {
            var mapping = await this._mappingRepository.GetByIdAsync(request.MappingId.Value, cancellationToken);
            mapping?.MarkUsed();
        }

        // Create transactions
        var createdIds = new List<Guid>();
        int autoCategorized = 0;
        int csvCategorized = 0;
        int uncategorized = 0;
        int skipped = 0;

        foreach (var txData in request.Transactions)
        {
            try
            {
                var amount = MoneyValue.Create("USD", txData.Amount);

                // Use Account.AddTransaction to properly set scope
                var transaction = account.AddTransaction(
                    amount,
                    txData.Date,
                    txData.Description,
                    txData.CategoryId);

                // Set import batch reference
                transaction.SetImportBatch(batch.Id, txData.Reference);

                await this._transactionRepository.AddAsync(transaction, cancellationToken);
                createdIds.Add(transaction.Id);

                // Track categorization statistics
                switch (txData.CategorySource)
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
            catch (DomainException)
            {
                skipped++;
            }
        }

        // Update batch with actual count and mark complete
        batch.Complete(createdIds.Count, skipped, errors: 0);

        await this._unitOfWork.SaveChangesAsync(cancellationToken);

        // Run reconciliation if requested
        var matchSuggestions = new List<ReconciliationMatchDto>();
        int reconciliationMatchCount = 0;
        int autoMatchedCount = 0;
        int pendingMatchCount = 0;

        if (request.RunReconciliation && createdIds.Count > 0)
        {
            // Get date range from imported transactions
            var importedDates = request.Transactions.Select(t => t.Date).ToList();
            var startDate = importedDates.Min().AddDays(-7); // Add tolerance for date matching
            var endDate = importedDates.Max().AddDays(7);

            var findMatchesRequest = new FindMatchesRequest
            {
                TransactionIds = createdIds,
                StartDate = startDate,
                EndDate = endDate,
            };

            var reconciliationResult = await this._reconciliationService.FindMatchesAsync(findMatchesRequest, cancellationToken);

            reconciliationMatchCount = reconciliationResult.TotalMatchesFound;
            autoMatchedCount = reconciliationResult.HighConfidenceCount;
            pendingMatchCount = reconciliationMatchCount - autoMatchedCount;

            // Collect all match suggestions for return
            foreach (var (_, matches) in reconciliationResult.MatchesByTransaction)
            {
                matchSuggestions.AddRange(matches);
            }
        }

        return new ImportResult
        {
            BatchId = batch.Id,
            ImportedCount = createdIds.Count,
            SkippedCount = skipped,
            ErrorCount = 0,
            CreatedTransactionIds = createdIds,
            AutoCategorizedCount = autoCategorized,
            CsvCategorizedCount = csvCategorized,
            UncategorizedCount = uncategorized,
            ReconciliationMatchCount = reconciliationMatchCount,
            AutoMatchedCount = autoMatchedCount,
            PendingMatchCount = pendingMatchCount,
            MatchSuggestions = matchSuggestions,
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ImportBatchDto>> GetImportHistoryAsync(CancellationToken cancellationToken = default)
    {
        var userId = this.GetRequiredUserId();
        var batches = await this._batchRepository.GetByUserAsync(userId, cancellationToken: cancellationToken);

        var result = new List<ImportBatchDto>();
        foreach (var batch in batches.OrderByDescending(b => b.ImportedAtUtc))
        {
            string? mappingName = null;
            if (batch.MappingId.HasValue)
            {
                var mapping = await this._mappingRepository.GetByIdAsync(batch.MappingId.Value, cancellationToken);
                mappingName = mapping?.Name;
            }

            var account = await this._accountRepository.GetByIdAsync(batch.AccountId, cancellationToken);

            result.Add(new ImportBatchDto
            {
                Id = batch.Id,
                AccountId = batch.AccountId,
                AccountName = account?.Name ?? "Unknown",
                FileName = batch.FileName,
                TransactionCount = batch.ImportedCount,
                Status = batch.Status,
                ImportedAtUtc = batch.ImportedAtUtc,
                MappingId = batch.MappingId,
                MappingName = mappingName,
            });
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<int> DeleteImportBatchAsync(Guid batchId, CancellationToken cancellationToken = default)
    {
        var userId = this.GetRequiredUserId();

        // Verify batch exists and belongs to user
        var batch = await this._batchRepository.GetByIdAsync(batchId, cancellationToken);
        if (batch is null)
        {
            return 0;
        }

        if (batch.UserId != userId)
        {
            throw new DomainException("Cannot delete import batch owned by another user.");
        }

        // Get and delete all transactions from this batch
        var transactions = await this._transactionRepository.GetByImportBatchAsync(batchId, cancellationToken);
        var count = transactions.Count;

        foreach (var transaction in transactions)
        {
            await this._transactionRepository.RemoveAsync(transaction, cancellationToken);
        }

        // Mark batch as deleted
        batch.MarkDeleted();

        await this._unitOfWork.SaveChangesAsync(cancellationToken);

        return count;
    }

    private Guid GetRequiredUserId()
    {
        return this._userContext.UserIdAsGuid
            ?? throw new DomainException("User ID is required for import operations.");
    }

    private async Task<List<ImportPreviewRow>> EnrichWithRecurringMatchesAsync(
        List<ImportPreviewRow> rows,
        CancellationToken cancellationToken)
    {
        // Get valid rows with dates for matching
        var validRowsWithDates = rows
            .Where(r => r.Date.HasValue && r.Amount.HasValue)
            .ToList();

        if (validRowsWithDates.Count == 0)
        {
            return rows;
        }

        // Get date range for recurring instance projection
        var dates = validRowsWithDates.Select(r => r.Date!.Value).ToList();
        var minDate = dates.Min().AddDays(-7);
        var maxDate = dates.Max().AddDays(7);

        // Load active recurring transactions and project instances
        var recurringTransactions = await this._recurringRepository.GetActiveAsync(cancellationToken);
        if (recurringTransactions.Count == 0)
        {
            return rows;
        }

        var instancesByDate = await this._instanceProjector.GetInstancesByDateRangeAsync(
            recurringTransactions,
            minDate,
            maxDate,
            cancellationToken);

        var allCandidates = instancesByDate.Values.SelectMany(list => list).ToList();
        if (allCandidates.Count == 0)
        {
            return rows;
        }

        var tolerances = MatchingTolerances.Default;

        // Enrich each valid row with potential matches
        var enrichedRows = new List<ImportPreviewRow>();
        foreach (var row in rows)
        {
            if (!row.Date.HasValue || !row.Amount.HasValue)
            {
                enrichedRows.Add(row);
                continue;
            }

            // Create a temporary transaction-like object for matching
            var bestMatch = this.FindBestMatchForPreviewRow(
                row.Description,
                row.Amount.Value,
                row.Date.Value,
                allCandidates,
                tolerances);

            if (bestMatch != null)
            {
                var matchPreview = new ImportRecurringMatchPreview
                {
                    RecurringTransactionId = bestMatch.RecurringTransactionId,
                    RecurringDescription = allCandidates
                        .First(c => c.RecurringTransactionId == bestMatch.RecurringTransactionId)
                        .Description,
                    InstanceDate = bestMatch.InstanceDate,
                    ExpectedAmount = allCandidates
                        .First(c => c.RecurringTransactionId == bestMatch.RecurringTransactionId)
                        .Amount.Amount,
                    ConfidenceScore = bestMatch.ConfidenceScore,
                    ConfidenceLevel = bestMatch.ConfidenceLevel.ToString(),
                    WouldAutoMatch = bestMatch.ConfidenceScore >= tolerances.AutoMatchThreshold,
                };

                enrichedRows.Add(row with { RecurringMatch = matchPreview });
            }
            else
            {
                enrichedRows.Add(row);
            }
        }

        return enrichedRows;
    }

    private TransactionMatchResult? FindBestMatchForPreviewRow(
        string description,
        decimal amount,
        DateOnly date,
        IReadOnlyList<RecurringInstanceInfo> candidates,
        MatchingTolerances tolerances)
    {
        TransactionMatchResult? best = null;

        foreach (var candidate in candidates)
        {
            var matchResult = this._transactionMatcher.CalculateMatch(
                CreatePreviewTransaction(description, amount, date),
                candidate,
                tolerances);

            if (matchResult != null && (best == null || matchResult.ConfidenceScore > best.ConfidenceScore))
            {
                best = matchResult;
            }
        }

        return best;
    }

    private static Transaction CreatePreviewTransaction(string description, decimal amount, DateOnly date)
    {
        // Create a temporary transaction for matching purposes
        // Using a dummy account ID since it's only for comparison
        return Transaction.Create(
            Guid.Empty,
            MoneyValue.Create("USD", amount),
            date,
            description);
    }

    private static ImportPreviewRow ProcessRow(
        int rowIndex,
        IReadOnlyList<string> row,
        IReadOnlyList<ColumnMappingDto> mappings,
        string dateFormat,
        AmountParseMode amountMode,
        DebitCreditIndicatorSettingsDto? indicatorSettings,
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
        string? indicatorValue = null;

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
                case ImportField.DebitCreditIndicator:
                    indicatorValue = value;
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
        if (amountMode == AmountParseMode.IndicatorColumn && indicatorSettings != null && indicatorSettings.ColumnIndex >= 0)
        {
            // Use indicator column to determine sign
            if (!string.IsNullOrEmpty(amountStr))
            {
                var rawAmount = ParseAmountValue(amountStr);
                if (rawAmount.HasValue)
                {
                    var signMultiplier = GetIndicatorSignMultiplier(indicatorValue, indicatorSettings);
                    if (signMultiplier.HasValue)
                    {
                        amount = Math.Abs(rawAmount.Value) * signMultiplier.Value;
                    }
                    else
                    {
                        // Unrecognized indicator - use the amount as-is but add warning
                        amount = rawAmount.Value;
                        warnings.Add($"Unrecognized indicator value: '{indicatorValue}'");
                    }
                }
                else
                {
                    errors.Add($"Could not parse amount: '{amountStr}'");
                }
            }
            else
            {
                errors.Add("Amount is required when using indicator column mode");
            }
        }
        else if (!string.IsNullOrEmpty(amountStr))
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

    private static int? GetIndicatorSignMultiplier(string? indicatorValue, DebitCreditIndicatorSettingsDto settings)
    {
        if (string.IsNullOrWhiteSpace(indicatorValue))
        {
            return null;
        }

        var trimmedValue = indicatorValue.Trim();
        var comparison = settings.CaseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        // Parse comma-separated indicator values
        var debitIndicators = settings.DebitIndicators
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var creditIndicators = settings.CreditIndicators
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (debitIndicators.Any(d => string.Equals(d, trimmedValue, comparison)))
        {
            return -1; // Debit = expense = negative
        }

        if (creditIndicators.Any(c => string.Equals(c, trimmedValue, comparison)))
        {
            return 1; // Credit = income = positive
        }

        return null; // Unrecognized indicator
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
