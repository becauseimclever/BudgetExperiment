// <copyright file="ReconciliationService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Reconciliation;

/// <summary>
/// Application service for recurring transaction reconciliation workflow.
/// Orchestrates match discovery, queries, and delegates match actions
/// and status reporting to focused sub-services.
/// </summary>
public sealed class ReconciliationService : IReconciliationService
{
    private readonly IReconciliationMatchRepository _matchRepository;
    private readonly IRecurringTransactionRepository _recurringRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IRecurringInstanceProjector _instanceProjector;
    private readonly ITransactionMatcher _transactionMatcher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IReconciliationStatusBuilder _statusBuilder;
    private readonly IReconciliationMatchActionHandler _matchActionHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReconciliationService"/> class.
    /// </summary>
    /// <param name="matchRepository">The reconciliation match repository.</param>
    /// <param name="recurringRepository">The recurring transaction repository.</param>
    /// <param name="transactionRepository">The transaction repository.</param>
    /// <param name="instanceProjector">The recurring instance projector.</param>
    /// <param name="transactionMatcher">The transaction matcher domain service.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <param name="statusBuilder">The reconciliation status builder.</param>
    /// <param name="matchActionHandler">The reconciliation match action handler.</param>
    public ReconciliationService(
        IReconciliationMatchRepository matchRepository,
        IRecurringTransactionRepository recurringRepository,
        ITransactionRepository transactionRepository,
        IRecurringInstanceProjector instanceProjector,
        ITransactionMatcher transactionMatcher,
        IUnitOfWork unitOfWork,
        IReconciliationStatusBuilder statusBuilder,
        IReconciliationMatchActionHandler matchActionHandler)
    {
        this._matchRepository = matchRepository;
        this._recurringRepository = recurringRepository;
        this._transactionRepository = transactionRepository;
        this._instanceProjector = instanceProjector;
        this._transactionMatcher = transactionMatcher;
        this._unitOfWork = unitOfWork;
        this._statusBuilder = statusBuilder;
        this._matchActionHandler = matchActionHandler;
    }

    /// <inheritdoc />
    public async Task<FindMatchesResult> FindMatchesAsync(
        FindMatchesRequest request,
        CancellationToken cancellationToken = default)
    {
        var tolerances = request.Tolerances != null
            ? ReconciliationMapper.ToDomain(request.Tolerances)
            : MatchingTolerancesValue.Default;

        var matchesByTransaction = new Dictionary<Guid, IReadOnlyList<ReconciliationMatchDto>>();
        var totalMatches = 0;
        var highConfidenceCount = 0;

        // Get active recurring transactions and project instances for the date range
        var recurringTransactions = await this._recurringRepository.GetActiveAsync(cancellationToken);
        var instancesByDate = await this._instanceProjector.GetInstancesByDateRangeAsync(
            recurringTransactions,
            request.StartDate,
            request.EndDate,
            cancellationToken);

        // Flatten to list of candidates
        var allCandidates = instancesByDate.Values.SelectMany(list => list).ToList();

        // Process each transaction
        foreach (var transactionId in request.TransactionIds)
        {
            var transaction = await this._transactionRepository.GetByIdAsync(transactionId, cancellationToken);
            if (transaction is null)
            {
                continue;
            }

            // Find matches using the transaction matcher
            var matchResults = this._transactionMatcher.FindMatches(transaction, allCandidates, tolerances);

            var matches = new List<ReconciliationMatchDto>();

            foreach (var matchResult in matchResults)
            {
                // Check if match already exists
                var existingMatch = await this._matchRepository.ExistsAsync(
                    transactionId,
                    matchResult.RecurringTransactionId,
                    matchResult.InstanceDate,
                    cancellationToken);

                if (existingMatch)
                {
                    continue;
                }

                // Get recurring transaction for scope info
                var recurring = await this._recurringRepository.GetByIdAsync(
                    matchResult.RecurringTransactionId,
                    cancellationToken);
                if (recurring is null)
                {
                    continue;
                }

                // Create new match
                var match = ReconciliationMatch.Create(
                    transactionId,
                    matchResult.RecurringTransactionId,
                    matchResult.InstanceDate,
                    matchResult.ConfidenceScore,
                    matchResult.AmountVariance,
                    matchResult.DateOffsetDays,
                    recurring.Scope,
                    recurring.OwnerUserId);

                // Auto-match if confidence is high enough
                if (matchResult.ConfidenceScore >= tolerances.AutoMatchThreshold)
                {
                    match.AutoMatch();
                    highConfidenceCount++;
                }

                await this._matchRepository.AddAsync(match, cancellationToken);

                matches.Add(ReconciliationMapper.ToDto(
                    match,
                    transaction,
                    recurring.Description,
                    recurring.Amount));
                totalMatches++;
            }

            if (matches.Count > 0)
            {
                matchesByTransaction[transactionId] = matches;
            }
        }

        await this._unitOfWork.SaveChangesAsync(cancellationToken);

        return new FindMatchesResult
        {
            MatchesByTransaction = matchesByTransaction,
            TotalMatchesFound = totalMatches,
            HighConfidenceCount = highConfidenceCount,
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReconciliationMatchDto>> GetPendingMatchesAsync(
        CancellationToken cancellationToken = default)
    {
        var matches = await this._matchRepository.GetPendingMatchesAsync(cancellationToken);
        return await this.EnrichMatchesWithDetailsAsync(matches, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReconciliationMatchDto>> GetMatchesForRecurringTransactionAsync(
        Guid recurringTransactionId,
        CancellationToken cancellationToken = default)
    {
        // Get matches for a wide date range (past year to next year)
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1));
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1));

        var matches = await this._matchRepository.GetByRecurringTransactionAsync(
            recurringTransactionId,
            startDate,
            endDate,
            cancellationToken);
        return await this.EnrichMatchesWithDetailsAsync(matches, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ReconciliationMatchDto?> AcceptMatchAsync(
        Guid matchId,
        CancellationToken cancellationToken = default)
    {
        return this._matchActionHandler.AcceptMatchAsync(matchId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ReconciliationMatchDto?> RejectMatchAsync(
        Guid matchId,
        CancellationToken cancellationToken = default)
    {
        return this._matchActionHandler.RejectMatchAsync(matchId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ReconciliationMatchDto>> BulkAcceptMatchesAsync(
        BulkMatchActionRequest request,
        CancellationToken cancellationToken = default)
    {
        return this._matchActionHandler.BulkAcceptMatchesAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ReconciliationStatusDto> GetReconciliationStatusAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        return this._statusBuilder.GetReconciliationStatusAsync(year, month, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ReconciliationMatchDto?> CreateManualMatchAsync(
        ManualMatchRequest request,
        CancellationToken cancellationToken = default)
    {
        return this._matchActionHandler.CreateManualMatchAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ReconciliationMatchDto?> UnlinkMatchAsync(
        Guid matchId,
        CancellationToken cancellationToken = default)
    {
        return this._matchActionHandler.UnlinkMatchAsync(matchId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LinkableInstanceDto>> GetLinkableInstancesAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        var transaction = await this._transactionRepository.GetByIdAsync(transactionId, cancellationToken);
        if (transaction is null)
        {
            return [];
        }

        // Get instances within ±30 days of the transaction date
        var startDate = transaction.Date.AddDays(-30);
        var endDate = transaction.Date.AddDays(30);

        var recurringTransactions = await this._recurringRepository.GetActiveAsync(cancellationToken);
        var instancesByDate = await this._instanceProjector.GetInstancesByDateRangeAsync(
            recurringTransactions,
            startDate,
            endDate,
            cancellationToken);

        // Flatten to list of instances
        var allInstances = instancesByDate.Values.SelectMany(list => list).ToList();

        // Get existing matches to determine which instances are already matched
        var result = new List<LinkableInstanceDto>();

        foreach (var instance in allInstances)
        {
            if (instance.IsSkipped)
            {
                continue;
            }

            // Check if this instance is already matched
            var isAlreadyMatched = await this._matchRepository.IsInstanceMatchedAsync(
                instance.RecurringTransactionId,
                instance.InstanceDate,
                cancellationToken);

            // Calculate a suggested confidence score for display
            decimal? suggestedConfidence = null;
            if (!isAlreadyMatched)
            {
                var matchResults = this._transactionMatcher.FindMatches(
                    transaction,
                    [instance],
                    MatchingTolerancesValue.Default);
                var matchResult = matchResults.FirstOrDefault();
                if (matchResult != null)
                {
                    suggestedConfidence = matchResult.ConfidenceScore;
                }
            }

            result.Add(new LinkableInstanceDto
            {
                RecurringTransactionId = instance.RecurringTransactionId,
                Description = instance.Description,
                ExpectedAmount = CommonMapper.ToDto(instance.Amount),
                InstanceDate = instance.InstanceDate,
                IsAlreadyMatched = isAlreadyMatched,
                SuggestedConfidence = suggestedConfidence,
            });
        }

        // Sort by date, then by suggested confidence (highest first)
        return result
            .OrderBy(i => i.InstanceDate)
            .ThenByDescending(i => i.SuggestedConfidence ?? 0)
            .ToList();
    }

    private async Task<IReadOnlyList<ReconciliationMatchDto>> EnrichMatchesWithDetailsAsync(
        IReadOnlyList<ReconciliationMatch> matches,
        CancellationToken cancellationToken)
    {
        var result = new List<ReconciliationMatchDto>();

        foreach (var match in matches)
        {
            var transaction = await this._transactionRepository.GetByIdAsync(
                match.ImportedTransactionId,
                cancellationToken);
            var recurring = await this._recurringRepository.GetByIdAsync(
                match.RecurringTransactionId,
                cancellationToken);

            result.Add(ReconciliationMapper.ToDto(
                match,
                transaction,
                recurring?.Description,
                recurring?.Amount));
        }

        return result;
    }
}
