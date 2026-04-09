// <copyright file="RecurringQueryService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Recurring;

/// <summary>
/// Coordinates recurring projection with realized transaction exclusion to prevent double-counting.
/// Fetches already-realized dates from the repository and passes them as exclusions to the projector.
/// </summary>
public sealed class RecurringQueryService : IRecurringQueryService
{
    private readonly ITransactionQueryRepository _transactionRepository;
    private readonly IRecurringInstanceProjector _projector;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringQueryService"/> class.
    /// </summary>
    /// <param name="transactionRepository">The transaction repository.</param>
    /// <param name="projector">The recurring instance projector.</param>
    public RecurringQueryService(
        ITransactionQueryRepository transactionRepository,
        IRecurringInstanceProjector projector)
    {
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _projector = projector ?? throw new ArgumentNullException(nameof(projector));
    }

    /// <inheritdoc/>
    public async Task<Dictionary<DateOnly, List<RecurringInstanceInfoValue>>> GetProjectedInstancesAsync(
        IReadOnlyList<RecurringTransaction> recurringTransactions,
        DateOnly fromDate,
        DateOnly toDate,
        Guid? accountId = null,
        CancellationToken cancellationToken = default)
    {
        var recurringIds = new HashSet<Guid>(recurringTransactions.Select(r => r.Id));

        var realizedTransactions = await _transactionRepository.GetByDateRangeAsync(
            fromDate, toDate, accountId, cancellationToken);

        var realizedDates = new HashSet<DateOnly>(
            realizedTransactions
                .Where(t => t.RecurringTransactionId.HasValue && recurringIds.Contains(t.RecurringTransactionId!.Value))
                .Select(t => t.Date));

        return await _projector.GetInstancesByDateRangeAsync(
            recurringTransactions,
            fromDate,
            toDate,
            excludeDates: realizedDates,
            cancellationToken);
    }
}
