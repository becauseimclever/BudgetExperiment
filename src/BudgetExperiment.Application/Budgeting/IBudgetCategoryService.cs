// <copyright file="IBudgetCategoryService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Budgeting;

/// <summary>
/// Service interface for budget category operations.
/// </summary>
public interface IBudgetCategoryService
{
    /// <summary>
    /// Gets a budget category by its identifier.
    /// </summary>
    /// <param name="id">The category identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The category DTO, or null if not found.</returns>
    Task<BudgetCategoryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all budget categories.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all category DTOs.</returns>
    Task<IReadOnlyList<BudgetCategoryDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active budget categories.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of active category DTOs.</returns>
    Task<IReadOnlyList<BudgetCategoryDto>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new budget category.
    /// </summary>
    /// <param name="dto">The category creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created category DTO.</returns>
    Task<BudgetCategoryDto> CreateAsync(BudgetCategoryCreateDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing budget category.
    /// </summary>
    /// <param name="id">The category identifier.</param>
    /// <param name="dto">The category update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated category DTO, or null if not found.</returns>
    Task<BudgetCategoryDto?> UpdateAsync(Guid id, BudgetCategoryUpdateDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a budget category.
    /// </summary>
    /// <param name="id">The category identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successfully deactivated, false if not found.</returns>
    Task<bool> DeactivateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a budget category.
    /// </summary>
    /// <param name="id">The category identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successfully activated, false if not found.</returns>
    Task<bool> ActivateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a budget category.
    /// </summary>
    /// <param name="id">The category identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successfully deleted, false if not found.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
