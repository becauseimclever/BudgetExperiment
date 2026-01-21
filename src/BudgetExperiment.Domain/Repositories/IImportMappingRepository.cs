// <copyright file="IImportMappingRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Repositories;

/// <summary>
/// Repository interface for import mappings.
/// </summary>
public interface IImportMappingRepository : IReadRepository<ImportMapping>, IWriteRepository<ImportMapping>
{
    /// <summary>
    /// Gets all import mappings for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Import mappings for the user.</returns>
    Task<IReadOnlyList<ImportMapping>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an import mapping by name for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="name">The mapping name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The import mapping if found.</returns>
    Task<ImportMapping?> GetByNameAsync(Guid userId, string name, CancellationToken cancellationToken = default);
}
