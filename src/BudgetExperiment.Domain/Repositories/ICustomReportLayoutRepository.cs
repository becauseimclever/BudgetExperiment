// <copyright file="ICustomReportLayoutRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Repositories;

/// <summary>
/// Repository interface for custom report layouts.
/// </summary>
public interface ICustomReportLayoutRepository : IReadRepository<CustomReportLayout>, IWriteRepository<CustomReportLayout>
{
    /// <summary>
    /// Gets all layouts accessible in the current scope.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All layouts.</returns>
    Task<IReadOnlyList<CustomReportLayout>> GetAllAsync(CancellationToken cancellationToken = default);
}
