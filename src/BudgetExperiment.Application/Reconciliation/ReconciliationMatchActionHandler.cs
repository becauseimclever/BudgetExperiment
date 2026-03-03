// <copyright file="ReconciliationMatchActionHandler.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Reconciliation;

/// <summary>
/// Handles reconciliation match lifecycle actions including accept, reject,
/// unlink, bulk accept, and manual link creation.
/// </summary>
public sealed class ReconciliationMatchActionHandler : IReconciliationMatchActionHandler
{
    private readonly IReconciliationMatchRepository _matchRepository;
    private readonly IRecurringTransactionRepository _recurringRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReconciliationMatchActionHandler"/> class.
    /// </summary>
    /// <param name="matchRepository">The reconciliation match repository.</param>
    /// <param name="recurringRepository">The recurring transaction repository.</param>
    /// <param name="transactionRepository">The transaction repository.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    public ReconciliationMatchActionHandler(
        IReconciliationMatchRepository matchRepository,
        IRecurringTransactionRepository recurringRepository,
        ITransactionRepository transactionRepository,
        IUnitOfWork unitOfWork)
    {
        this._matchRepository = matchRepository;
        this._recurringRepository = recurringRepository;
        this._transactionRepository = transactionRepository;
        this._unitOfWork = unitOfWork;
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
    public async Task<ReconciliationMatchDto?> UnlinkMatchAsync(
        Guid matchId,
        CancellationToken cancellationToken = default)
    {
        var match = await this._matchRepository.GetByIdAsync(matchId, cancellationToken);
        if (match is null)
        {
            return null;
        }

        // Unlink the domain match (sets status to Rejected)
        match.Unlink();

        // Also unlink the transaction
        var transaction = await this._transactionRepository.GetByIdAsync(
            match.ImportedTransactionId,
            cancellationToken);
        transaction?.UnlinkFromRecurring();

        await this._unitOfWork.SaveChangesAsync(cancellationToken);

        return ReconciliationMapper.ToDto(match);
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
            return await this.GetExistingMatchDtoAsync(
                request, transaction, recurring, cancellationToken);
        }

        // Calculate variance
        var amountVariance = recurring.Amount.Amount - transaction.Amount.Amount;
        var dateOffsetDays = transaction.Date.DayNumber - request.InstanceDate.DayNumber;

        // Create manual link (already accepted with MatchSource.Manual)
        var match = ReconciliationMatch.CreateManualLink(
            request.TransactionId,
            request.RecurringTransactionId,
            request.InstanceDate,
            amountVariance,
            dateOffsetDays,
            recurring.Scope,
            recurring.OwnerUserId);

        // Link the transaction
        transaction.LinkToRecurringInstance(request.RecurringTransactionId, request.InstanceDate);

        await this._matchRepository.AddAsync(match, cancellationToken);
        await this._unitOfWork.SaveChangesAsync(cancellationToken);

        return ReconciliationMapper.ToDto(match, transaction, recurring.Description, recurring.Amount);
    }

    private async Task<ReconciliationMatchDto?> GetExistingMatchDtoAsync(
        ManualMatchRequest request,
        Transaction transaction,
        RecurringTransaction recurring,
        CancellationToken cancellationToken)
    {
        var existing = (await this._matchRepository.GetByTransactionIdAsync(
            request.TransactionId,
            cancellationToken)).FirstOrDefault(m =>
                m.RecurringTransactionId == request.RecurringTransactionId &&
                m.RecurringInstanceDate == request.InstanceDate);

        if (existing != null)
        {
            return ReconciliationMapper.ToDto(existing, transaction, recurring.Description, recurring.Amount);
        }

        return null;
    }
}
