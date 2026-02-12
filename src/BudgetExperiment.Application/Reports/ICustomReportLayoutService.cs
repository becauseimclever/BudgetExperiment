// <copyright file="ICustomReportLayoutService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Reports;

/// <summary>
/// Service interface for custom report layouts.
/// </summary>
public interface ICustomReportLayoutService
{
    /// <summary>
    /// Gets all layouts accessible in the current scope.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of layouts.</returns>
    Task<IReadOnlyList<CustomReportLayoutDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a layout by id.
    /// </summary>
    /// <param name="id">Layout identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Layout or null.</returns>
    Task<CustomReportLayoutDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new layout.
    /// </summary>
    /// <param name="dto">Layout creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created layout.</returns>
    Task<CustomReportLayoutDto> CreateAsync(CustomReportLayoutCreateDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing layout.
    /// </summary>
    /// <param name="id">Layout identifier.</param>
    /// <param name="dto">Update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated layout or null if not found.</returns>
    Task<CustomReportLayoutDto?> UpdateAsync(Guid id, CustomReportLayoutUpdateDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a layout by id.
    /// </summary>
    /// <param name="id">Layout identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
