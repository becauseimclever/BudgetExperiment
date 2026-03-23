// <copyright file="IAccountNameLookupRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Repositories;

/// <summary>
/// Provides lightweight account name lookups.
/// </summary>
public interface IAccountNameLookupRepository
{
    /// <summary>
    /// Gets account names for the specified account IDs.
    /// </summary>
    /// <param name="ids">Account identifiers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Mapping of account IDs to account names.</returns>
    Task<IReadOnlyDictionary<Guid, string>> GetAccountNamesByIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default);
}
