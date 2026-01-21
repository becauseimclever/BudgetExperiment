// <copyright file="ReconciliationService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Reconciliation;

/// <summary>
/// Application service for recurring transaction reconciliation workflow.
/// </summary>
public sealed class ReconciliationService : IReconciliationService
{
    private readonly IReconciliationMatchRepository _matchRepository;
    private readonly IRecurringTransactionRepository _recurringRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IRecurringInstanceProjector _instanceProjector;
    private readonly ITransactionMatcher _transactionMatcher;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReconciliationService"/> class.
    /// </summary>
    /// <param name="matchRepository">The reconciliation match repository.</param>
    /// <param name="recurringRepository">The recurring transaction repository.</param>
    /// <param name="transactionRepository">The transaction repository.</param>
    /// <param name="instanceProjector">The recurring instance projector.</param>
    /// <param name="transactionMatcher">The transaction matcher domain service.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    public ReconciliationService(
        IReconciliationMatchRepository matchRepository,
        IRecurringTransactionRepository recurringRepository,
        ITransactionRepository transactionRepository,
        IRecurringInstanceProjector instanceProjector,
        ITransactionMatcher transactionMatcher,
        IUnitOfWork unitOfWork)
    {
        this._matchRepository = matchRepository;
        this._recurringRepository = recurringRepository;
        this._transactionRepository = transactionRepository;
        this._instanceProjector = instanceProjector;
        this._transactionMatcher = transactionMatcher;
        this._unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<FindMatchesResult> FindMatchesAsync(
        FindMatchesRequest request,
        CancellationToken cancellationToken = default)
    {
        var tolerances = request.Tolerances != null
            ? ReconciliationMapper.ToDomain(request.Tolerances)
            : MatchingTolerances.Default;

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
    public async Task<ReconciliationMatchDto?> AcceptMatchAsync(
        Guid matchId,
        CancellationToken cancellationToken = default)
    {
        var match = await this._matchRepository.GetByIdAsync(matchId, cancellationToken);
        if (match is null)
        {
            return null;
        }

        match.Accept();

        // Link the transaction to the recurring instance
        var transaction = await this._transactionRepository.GetByIdAsync(
            match.ImportedTransactionId,
            cancellationToken);
        transaction?.LinkToRecurringInstance(match.RecurringTransactionId, match.RecurringInstanceDate);

        await this._unitOfWork.SaveChangesAsync(cancellationToken);

        var recurring = await this._recurringRepository.GetByIdAsync(
            match.RecurringTransactionId,
            cancellationToken);

        return ReconciliationMapper.ToDto(
            match,
            transaction,
            recurring?.Description,
            recurring?.Amount);
    }

    /// <inheritdoc />
    public async Task<ReconciliationMatchDto?> RejectMatchAsync(
        Guid matchId,
        CancellationToken cancellationToken = default)
    {
        var match = await this._matchRepository.GetByIdAsync(matchId, cancellationToken);
        if (match is null)
        {
            return null;
        }

        match.Reject();
        await this._unitOfWork.SaveChangesAsync(cancellationToken);

        return ReconciliationMapper.ToDto(match);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReconciliationMatchDto>> BulkAcceptMatchesAsync(
        BulkMatchActionRequest request,
        CancellationToken cancellationToken = default)
    {
        var accepted = new List<ReconciliationMatchDto>();

        foreach (var matchId in request.MatchIds)
        {
            var result = await this.AcceptMatchAsync(matchId, cancellationToken);
            if (result != null)
            {
                accepted.Add(result);
            }
        }

        return accepted;
    }

    /// <inheritdoc />
    public async Task<ReconciliationStatusDto> GetReconciliationStatusAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var recurringTransactions = await this._recurringRepository.GetActiveAsync(cancellationToken);
        var instancesByDate = await this._instanceProjector.GetInstancesByDateRangeAsync(
            recurringTransactions,
            startDate,
            endDate,
            cancellationToken);

        var instances = new List<RecurringInstanceStatusDto>();
        var matchedCount = 0;
        var pendingCount = 0;
        var missingCount = 0;

        // Get all matches for this period
        var periodMatches = await this._matchRepository.GetByPeriodAsync(year, month, cancellationToken);

        var matchLookup = periodMatches
            .GroupBy(m => (m.RecurringTransactionId, m.RecurringInstanceDate))
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var (_, instancesForDate) in instancesByDate)
        {
            foreach (var instance in instancesForDate)
            {
                if (instance.IsSkipped)
                {
                    continue;
                }

                var key = (instance.RecurringTransactionId, instance.InstanceDate);
                var matchesForInstance = matchLookup.GetValueOrDefault(key, []);

                var acceptedMatch = matchesForInstance.FirstOrDefault(
                    m => m.Status is ReconciliationMatchStatus.Accepted or ReconciliationMatchStatus.AutoMatched);
                var pendingMatch = matchesForInstance.FirstOrDefault(
                    m => m.Status == ReconciliationMatchStatus.Suggested);

                string status;
                Guid? matchedTransactionId = null;
                MoneyDto? actualAmount = null;
                decimal? amountVariance = null;
                Guid? matchId = null;

                if (acceptedMatch != null)
                {
                    status = "Matched";
                    matchedTransactionId = acceptedMatch.ImportedTransactionId;
                    amountVariance = acceptedMatch.AmountVariance;
                    matchId = acceptedMatch.Id;
                    matchedCount++;

                    // Get actual amount from matched transaction
                    var transaction = await this._transactionRepository.GetByIdAsync(
                        acceptedMatch.ImportedTransactionId,
                        cancellationToken);
                    if (transaction != null)
                    {
                        actualAmount = CommonMapper.ToDto(transaction.Amount);
                    }
                }
                else if (pendingMatch != null)
                {
                    status = "Pending";
                    matchId = pendingMatch.Id;
                    pendingCount++;
                }
                else
                {
                    status = "Missing";
                    missingCount++;
                }

                instances.Add(new RecurringInstanceStatusDto
                {
                    RecurringTransactionId = instance.RecurringTransactionId,
                    Description = instance.Description,
                    InstanceDate = instance.InstanceDate,
                    ExpectedAmount = CommonMapper.ToDto(instance.Amount),
                    Status = status,
                    MatchedTransactionId = matchedTransactionId,
                    ActualAmount = actualAmount,
                    AmountVariance = amountVariance,
                    MatchId = matchId,
                });
            }
        }

        return new ReconciliationStatusDto
        {
            Year = year,
            Month = month,
            TotalExpectedInstances = instances.Count,
            MatchedCount = matchedCount,
            PendingCount = pendingCount,
            MissingCount = missingCount,
            Instances = instances,
        };
    }

    /// <inheritdoc />
    public async Task<ReconciliationMatchDto?> CreateManualMatchAsync(
        ManualMatchRequest request,
        CancellationToken cancellationToken = default)
    {
        var transaction = await this._transactionRepository.GetByIdAsync(
            request.TransactionId,
            cancellationToken);
        if (transaction is null)
        {
            return null;
        }

        var recurring = await this._recurringRepository.GetByIdAsync(
            request.RecurringTransactionId,
            cancellationToken);
        if (recurring is null)
        {
            return null;
        }

        // Check if match already exists
        var existingMatch = await this._matchRepository.ExistsAsync(
            request.TransactionId,
            request.RecurringTransactionId,
            request.InstanceDate,
            cancellationToken);

        if (existingMatch)
        {
            // Return existing match if found
            var existing = (await this._matchRepository.GetByTransactionIdAsync(
                request.TransactionId,
                cancellationToken)).FirstOrDefault(m =>
                    m.RecurringTransactionId == request.RecurringTransactionId &&
                    m.RecurringInstanceDate == request.InstanceDate);

            if (existing != null)
            {
                return ReconciliationMapper.ToDto(existing, transaction, recurring.Description, recurring.Amount);
            }
        }

        // Calculate variance
        var amountVariance = recurring.Amount.Amount - transaction.Amount.Amount;
        var dateOffsetDays = transaction.Date.DayNumber - request.InstanceDate.DayNumber;

        // Create the match with high confidence for manual matches
        var match = ReconciliationMatch.Create(
            request.TransactionId,
            request.RecurringTransactionId,
            request.InstanceDate,
            1.0m, // Manual matches get full confidence
            amountVariance,
            dateOffsetDays,
            recurring.Scope,
            recurring.OwnerUserId);

        // Auto-accept manual matches
        match.Accept();

        // Link the transaction
        transaction.LinkToRecurringInstance(request.RecurringTransactionId, request.InstanceDate);

        await this._matchRepository.AddAsync(match, cancellationToken);
        await this._unitOfWork.SaveChangesAsync(cancellationToken);

        return ReconciliationMapper.ToDto(match, transaction, recurring.Description, recurring.Amount);
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
