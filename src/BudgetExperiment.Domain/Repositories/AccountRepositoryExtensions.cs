// <copyright file="AccountRepositoryExtensions.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Repositories;

/// <summary>
/// Extension helpers for account repository capabilities.
/// </summary>
public static class AccountRepositoryExtensions
{
    /// <summary>
    /// Gets an account by ID including transactions within a date range.
    /// Falls back to the repository default when range-based access is unavailable.
    /// </summary>
    /// <param name="repository">Account repository.</param>
    /// <param name="id">Account identifier.</param>
    /// <param name="startDate">Start date (inclusive).</param>
    /// <param name="endDate">End date (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The account with transactions or null.</returns>
    public static Task<Account?> GetByIdWithTransactionsAsync(
        this IAccountRepository repository,
        Guid id,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        if (repository is IAccountTransactionRangeRepository rangeRepository)
        {
            return rangeRepository.GetByIdWithTransactionsAsync(id, startDate, endDate, cancellationToken);
        }

        return repository.GetByIdWithTransactionsAsync(id, cancellationToken);
    }

    /// <summary>
    /// Gets account names for the specified account IDs.
    /// Falls back to loading all accounts when optimized lookup is unavailable.
    /// </summary>
    /// <param name="repository">Account repository.</param>
    /// <param name="ids">Account identifiers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Mapping of account IDs to account names.</returns>
    public static async Task<IReadOnlyDictionary<Guid, string>> GetAccountNamesByIdsAsync(
        this IAccountRepository repository,
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        if (repository is IAccountNameLookupRepository nameLookupRepository)
        {
            return await nameLookupRepository.GetAccountNamesByIdsAsync(ids, cancellationToken);
        }

        var idList = ids.Distinct().ToList();
        if (idList.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var accounts = await repository.GetAllAsync(cancellationToken);
        return accounts
            .Where(a => idList.Contains(a.Id))
            .ToDictionary(a => a.Id, a => a.Name);
    }
}
