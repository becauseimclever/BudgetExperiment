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
    private readonly ILinkableInstanceFinder _linkableInstanceFinder;

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
    /// <param name="linkableInstanceFinder">The linkable instance finder.</param>
    public ReconciliationService(
        IReconciliationMatchRepository matchRepository,
        IRecurringTransactionRepository recurringRepository,
        ITransactionRepository transactionRepository,
        IRecurringInstanceProjector instanceProjector,
        ITransactionMatcher transactionMatcher,
        IUnitOfWork unitOfWork,
        IReconciliationStatusBuilder statusBuilder,
        IReconciliationMatchActionHandler matchActionHandler,
        ILinkableInstanceFinder linkableInstanceFinder)
    {
        _matchRepository = matchRepository;
        _recurringRepository = recurringRepository;
        _transactionRepository = transactionRepository;
        _instanceProjector = instanceProjector;
        _transactionMatcher = transactionMatcher;
        _unitOfWork = unitOfWork;
        _statusBuilder = statusBuilder;
        _matchActionHandler = matchActionHandler;
        _linkableInstanceFinder = linkableInstanceFinder;
    }

    /// <inheritdoc />
    public async Task<FindMatchesResult> FindMatchesAsync(
        FindMatchesRequest request,
        CancellationToken cancellationToken = default)
    {
        var tolerances = request.Tolerances != null
            ? ReconciliationMapper.ToDomain(request.Tolerances)
            : MatchingTolerancesValue.Default;

        var allCandidates = await this.GetMatchCandidatesAsync(
            request.StartDate, request.EndDate, cancellationToken);

        var matchesByTransaction = new Dictionary<Guid, IReadOnlyList<ReconciliationMatchDto>>();
        var totalMatches = 0;
        var highConfidenceCount = 0;

        foreach (var transactionId in request.TransactionIds)
        {
            var (matches, autoMatchedInBatch) = await this.FindMatchesForTransactionAsync(
                transactionId, allCandidates, tolerances, cancellationToken);

            if (matches.Count > 0)
            {
                matchesByTransaction[transactionId] = matches;
                totalMatches += matches.Count;
                highConfidenceCount += autoMatchedInBatch;
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
        var matches = await _matchRepository.GetPendingMatchesAsync(cancellationToken);
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

        var matches = await _matchRepository.GetByRecurringTransactionAsync(
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
        return _matchActionHandler.AcceptMatchAsync(matchId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ReconciliationMatchDto?> RejectMatchAsync(
        Guid matchId,
        CancellationToken cancellationToken = default)
    {
        return _matchActionHandler.RejectMatchAsync(matchId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ReconciliationMatchDto>> BulkAcceptMatchesAsync(
        BulkMatchActionRequest request,
        CancellationToken cancellationToken = default)
    {
        return _matchActionHandler.BulkAcceptMatchesAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ReconciliationStatusDto> GetReconciliationStatusAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        return _statusBuilder.GetReconciliationStatusAsync(year, month, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ReconciliationMatchDto?> CreateManualMatchAsync(
        ManualMatchRequest request,
        CancellationToken cancellationToken = default)
    {
        return _matchActionHandler.CreateManualMatchAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ReconciliationMatchDto?> UnlinkMatchAsync(
        Guid matchId,
        CancellationToken cancellationToken = default)
    {
        return _matchActionHandler.UnlinkMatchAsync(matchId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<LinkableInstanceDto>> GetLinkableInstancesAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        return _linkableInstanceFinder.GetLinkableInstancesAsync(transactionId, cancellationToken);
    }

    private async Task<List<RecurringInstanceInfoValue>> GetMatchCandidatesAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken)
    {
        var recurringTransactions = await _recurringRepository.GetActiveAsync(cancellationToken);
        var instancesByDate = await _instanceProjector.GetInstancesByDateRangeAsync(
            recurringTransactions, startDate, endDate, excludeDates: null, cancellationToken);

        return instancesByDate.Values.SelectMany(list => list).ToList();
    }

    private async Task<(List<ReconciliationMatchDto> Matches, int AutoMatched)> FindMatchesForTransactionAsync(
        Guid transactionId,
        List<RecurringInstanceInfoValue> candidates,
        MatchingTolerancesValue tolerances,
        CancellationToken cancellationToken)
    {
        var transaction = await _transactionRepository.GetByIdAsync(transactionId, cancellationToken);
        if (transaction is null)
        {
            return ([], 0);
        }

        var matchResults = _transactionMatcher.FindMatches(transaction, candidates, tolerances);
        var matches = new List<ReconciliationMatchDto>();
        var autoMatched = 0;

        foreach (var matchResult in matchResults)
        {
            var (dto, wasAutoMatched) = await this.CreateMatchIfNewAsync(
                transaction, transactionId, matchResult, tolerances, cancellationToken);
            if (dto is not null)
            {
                matches.Add(dto);
                if (wasAutoMatched)
                {
                    autoMatched++;
                }
            }
        }

        return (matches, autoMatched);
    }

    private async Task<(ReconciliationMatchDto? Dto, bool WasAutoMatched)> CreateMatchIfNewAsync(
        Transaction transaction,
        Guid transactionId,
        TransactionMatchResultValue matchResult,
        MatchingTolerancesValue tolerances,
        CancellationToken cancellationToken)
    {
        var existingMatch = await _matchRepository.ExistsAsync(
            transactionId, matchResult.RecurringTransactionId, matchResult.InstanceDate, cancellationToken);
        if (existingMatch)
        {
            return (null, false);
        }

        var recurring = await _recurringRepository.GetByIdAsync(
            matchResult.RecurringTransactionId, cancellationToken);
        if (recurring is null)
        {
            return (null, false);
        }

        var match = ReconciliationMatch.Create(
            transactionId,
            matchResult.RecurringTransactionId,
            matchResult.InstanceDate,
            matchResult.ConfidenceScore,
            matchResult.AmountVariance,
            matchResult.DateOffsetDays,
            recurring.Scope,
            recurring.OwnerUserId);

        var wasAutoMatched = matchResult.ConfidenceScore >= tolerances.AutoMatchThreshold;
        if (wasAutoMatched)
        {
            match.AutoMatch();
        }

        await _matchRepository.AddAsync(match, cancellationToken);

        var dto = ReconciliationMapper.ToDto(match, transaction, recurring.Description, recurring.Amount);
        return (dto, wasAutoMatched);
    }

    private async Task<IReadOnlyList<ReconciliationMatchDto>> EnrichMatchesWithDetailsAsync(
        IReadOnlyList<ReconciliationMatch> matches,
        CancellationToken cancellationToken)
    {
        var result = new List<ReconciliationMatchDto>();

        foreach (var match in matches)
        {
            var transaction = await _transactionRepository.GetByIdAsync(
                match.ImportedTransactionId,
                cancellationToken);
            var recurring = await _recurringRepository.GetByIdAsync(
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
