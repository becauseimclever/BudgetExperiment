// <copyright file="ImportService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Recurring;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Import;

/// <summary>
/// Orchestrates CSV import operations, delegating parsing, duplicate detection,
/// and enrichment to focused sub-services.
/// </summary>
public sealed class ImportService : IImportService
{
    private readonly IImportRowProcessor _rowProcessor;
    private readonly IImportPreviewEnricher _previewEnricher;
    private readonly IImportBatchManager _batchManager;
    private readonly IImportTransactionCreator _transactionCreator;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICategorizationRuleRepository _ruleRepository;
    private readonly IBudgetCategoryRepository _categoryRepository;
    private readonly IImportBatchRepository _batchRepository;
    private readonly IImportMappingRepository _mappingRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IReconciliationService _reconciliationService;
    private readonly IRecurringChargeDetectionService _recurringChargeDetectionService;
    private readonly IUserContext _userContext;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImportService"/> class.
    /// </summary>
    /// <param name="rowProcessor">Row processor for CSV parsing.</param>
    /// <param name="previewEnricher">Preview enrichment service.</param>
    /// <param name="batchManager">Batch history manager.</param>
    /// <param name="transactionCreator">Transaction creator for import execution.</param>
    /// <param name="transactionRepository">Transaction repository for duplicate detection.</param>
    /// <param name="ruleRepository">Categorization rule repository.</param>
    /// <param name="categoryRepository">Budget category repository.</param>
    /// <param name="batchRepository">Import batch repository.</param>
    /// <param name="mappingRepository">Import mapping repository.</param>
    /// <param name="accountRepository">Account repository.</param>
    /// <param name="reconciliationService">Reconciliation service.</param>
    /// <param name="recurringChargeDetectionService">Recurring charge detection service.</param>
    /// <param name="userContext">User context.</param>
    /// <param name="unitOfWork">Unit of work.</param>
    public ImportService(
        IImportRowProcessor rowProcessor,
        IImportPreviewEnricher previewEnricher,
        IImportBatchManager batchManager,
        IImportTransactionCreator transactionCreator,
        ITransactionRepository transactionRepository,
        ICategorizationRuleRepository ruleRepository,
        IBudgetCategoryRepository categoryRepository,
        IImportBatchRepository batchRepository,
        IImportMappingRepository mappingRepository,
        IAccountRepository accountRepository,
        IReconciliationService reconciliationService,
        IRecurringChargeDetectionService recurringChargeDetectionService,
        IUserContext userContext,
        IUnitOfWork unitOfWork)
    {
        this._rowProcessor = rowProcessor;
        this._previewEnricher = previewEnricher;
        this._batchManager = batchManager;
        this._transactionCreator = transactionCreator;
        this._transactionRepository = transactionRepository;
        this._ruleRepository = ruleRepository;
        this._categoryRepository = categoryRepository;
        this._batchRepository = batchRepository;
        this._mappingRepository = mappingRepository;
        this._accountRepository = accountRepository;
        this._reconciliationService = reconciliationService;
        this._recurringChargeDetectionService = recurringChargeDetectionService;
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

        var rowsToProcess = request.Rows;

        var rules = await this._ruleRepository.GetActiveByPriorityAsync(cancellationToken);
        var categories = await this._categoryRepository.GetAllAsync(cancellationToken);
        var categoryByName = categories
            .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var existingTransactions = await this.LoadExistingTransactionsForDuplicateDetectionAsync(
            request, rowsToProcess, cancellationToken);

        var previewRows = this.ProcessAllRows(rowsToProcess, request, rules, categoryByName, existingTransactions);

        if (request.CheckRecurringMatches)
        {
            previewRows = await this._previewEnricher.EnrichWithRecurringMatchesAsync(previewRows, cancellationToken);
        }

        previewRows = await this._previewEnricher.EnrichWithLocationDataAsync(previewRows, cancellationToken);

        return BuildPreviewResult(previewRows);
    }

    /// <inheritdoc />
    public async Task<ImportResult> ExecuteAsync(ImportExecuteRequest request, CancellationToken cancellationToken = default)
    {
        var userId = this.GetRequiredUserId();

        var account = await this._accountRepository.GetByIdAsync(request.AccountId, cancellationToken)
            ?? throw new DomainException($"Account with ID '{request.AccountId}' not found.");

        if (request.Transactions.Count == 0)
        {
            return new ImportResult { BatchId = Guid.Empty, ImportedCount = 0 };
        }

        var batch = ImportBatch.Create(userId, request.AccountId, request.FileName, request.Transactions.Count, request.MappingId);
        await this._batchRepository.AddAsync(batch, cancellationToken);

        await this.MarkMappingAsUsedAsync(request.MappingId, cancellationToken);

        var importStats = await this._transactionCreator.CreateTransactionsAsync(account, batch.Id, request.Transactions, cancellationToken);

        batch.Complete(importStats.CreatedIds.Count, importStats.Skipped, errors: 0);
        await this._unitOfWork.SaveChangesAsync(cancellationToken);

        var reconciliation = request.RunReconciliation && importStats.CreatedIds.Count > 0
            ? await this.RunReconciliationAsync(importStats.CreatedIds, request.Transactions, cancellationToken)
            : ReconciliationStats.Empty;

        var recurringChargeSuggestions = importStats.CreatedIds.Count > 0
            ? await this._recurringChargeDetectionService.DetectAsync(request.AccountId, cancellationToken)
            : 0;

        return new ImportResult
        {
            BatchId = batch.Id,
            ImportedCount = importStats.CreatedIds.Count,
            SkippedCount = importStats.Skipped,
            ErrorCount = 0,
            CreatedTransactionIds = importStats.CreatedIds,
            AutoCategorizedCount = importStats.AutoCategorized,
            CsvCategorizedCount = importStats.CsvCategorized,
            UncategorizedCount = importStats.Uncategorized,
            ReconciliationMatchCount = reconciliation.TotalMatches,
            AutoMatchedCount = reconciliation.AutoMatched,
            PendingMatchCount = reconciliation.PendingMatches,
            MatchSuggestions = reconciliation.Suggestions,
            LocationEnrichedCount = importStats.LocationEnriched,
            RecurringChargeSuggestionsCount = recurringChargeSuggestions,
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ImportBatchDto>> GetImportHistoryAsync(CancellationToken cancellationToken = default)
    {
        return await this._batchManager.GetImportHistoryAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> DeleteImportBatchAsync(Guid batchId, CancellationToken cancellationToken = default)
    {
        return await this._batchManager.DeleteImportBatchAsync(batchId, cancellationToken);
    }

    private static ImportPreviewResult BuildPreviewResult(List<ImportPreviewRow> previewRows)
    {
        var validRows = previewRows.Where(r => r.Status == ImportRowStatus.Valid).ToList();

        return new ImportPreviewResult
        {
            Rows = previewRows,
            ValidCount = validRows.Count,
            WarningCount = previewRows.Count(r => r.Status == ImportRowStatus.Warning),
            ErrorCount = previewRows.Count(r => r.Status == ImportRowStatus.Error),
            DuplicateCount = previewRows.Count(r => r.Status == ImportRowStatus.Duplicate),
            TotalAmount = validRows.Where(r => r.Amount.HasValue).Sum(r => r.Amount!.Value),
            AutoCategorizedCount = previewRows.Count(r => r.CategorySource == CategorySource.AutoRule),
            LocationEnrichedCount = previewRows.Count(r => r.ParsedLocation != null),
        };
    }

    private Guid GetRequiredUserId()
    {
        return this._userContext.UserIdAsGuid
            ?? throw new DomainException("User ID is required for import operations.");
    }

    private async Task<IReadOnlyList<Transaction>> LoadExistingTransactionsForDuplicateDetectionAsync(
        ImportPreviewRequest request,
        IReadOnlyList<IReadOnlyList<string>> rowsToProcess,
        CancellationToken cancellationToken)
    {
        if (!request.DuplicateSettings.Enabled)
        {
            return [];
        }

        var dates = this._rowProcessor.ExtractDatesFromRows(rowsToProcess, request.Mappings, request.DateFormat);
        if (dates.Count == 0)
        {
            return [];
        }

        var minDate = dates.Min().AddDays(-request.DuplicateSettings.LookbackDays);
        var maxDate = dates.Max().AddDays(request.DuplicateSettings.LookbackDays);

        return await this._transactionRepository.GetForDuplicateDetectionAsync(
            request.AccountId, minDate, maxDate, cancellationToken);
    }

    private List<ImportPreviewRow> ProcessAllRows(
        IReadOnlyList<IReadOnlyList<string>> rowsToProcess,
        ImportPreviewRequest request,
        IReadOnlyList<CategorizationRule> rules,
        Dictionary<string, BudgetCategory> categoryByName,
        IReadOnlyList<Transaction> existingTransactions)
    {
        var previewRows = new List<ImportPreviewRow>();
        for (int i = 0; i < rowsToProcess.Count; i++)
        {
            var previewRow = this._rowProcessor.ProcessRow(
                request.RowsToSkip + i + 1,
                rowsToProcess[i],
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

        return previewRows;
    }

    private async Task MarkMappingAsUsedAsync(Guid? mappingId, CancellationToken cancellationToken)
    {
        if (!mappingId.HasValue)
        {
            return;
        }

        var mapping = await this._mappingRepository.GetByIdAsync(mappingId.Value, cancellationToken);
        mapping?.MarkUsed();
    }

    private async Task<ReconciliationStats> RunReconciliationAsync(
        List<Guid> createdIds,
        IReadOnlyList<ImportTransactionData> transactions,
        CancellationToken cancellationToken)
    {
        var importedDates = transactions.Select(t => t.Date).ToList();
        var startDate = importedDates.Min().AddDays(-7);
        var endDate = importedDates.Max().AddDays(7);

        var findMatchesRequest = new FindMatchesRequest
        {
            TransactionIds = createdIds,
            StartDate = startDate,
            EndDate = endDate,
        };

        var result = await this._reconciliationService.FindMatchesAsync(findMatchesRequest, cancellationToken);

        var suggestions = new List<ReconciliationMatchDto>();
        foreach (var (_, matches) in result.MatchesByTransaction)
        {
            suggestions.AddRange(matches);
        }

        return new ReconciliationStats(
            result.TotalMatchesFound,
            result.HighConfidenceCount,
            result.TotalMatchesFound - result.HighConfidenceCount,
            suggestions);
    }

    private sealed record ReconciliationStats(
        int TotalMatches,
        int AutoMatched,
        int PendingMatches,
        List<ReconciliationMatchDto> Suggestions)
    {
        public static ReconciliationStats Empty { get; } = new(0, 0, 0, []);
    }
}
