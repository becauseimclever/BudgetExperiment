// <copyright file="IImportBatchRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain;

/// <summary>
/// Repository interface for import batches.
/// </summary>
public interface IImportBatchRepository : IReadRepository<ImportBatch>, IWriteRepository<ImportBatch>
{
    /// <summary>
    /// Gets import history for a user, ordered by most recent first.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Number of records to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Import batches for the user.</returns>
    Task<IReadOnlyList<ImportBatch>> GetByUserAsync(Guid userId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets import batches for a specific account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Import batches for the account.</returns>
    Task<IReadOnlyList<ImportBatch>> GetByAccountAsync(Guid accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets import batches that used a specific mapping.
    /// </summary>
    /// <param name="mappingId">The mapping identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Import batches using the mapping.</returns>
    Task<IReadOnlyList<ImportBatch>> GetByMappingAsync(Guid mappingId, CancellationToken cancellationToken = default);
}
