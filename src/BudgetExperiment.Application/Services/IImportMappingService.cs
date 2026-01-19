// <copyright file="IImportMappingService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Services;

/// <summary>
/// Service for managing import mappings.
/// </summary>
public interface IImportMappingService
{
    /// <summary>
    /// Gets all mappings for the current user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of import mappings.</returns>
    Task<IReadOnlyList<ImportMappingDto>> GetUserMappingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific mapping by ID.
    /// </summary>
    /// <param name="id">The mapping ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The mapping if found.</returns>
    Task<ImportMappingDto?> GetMappingAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new import mapping.
    /// </summary>
    /// <param name="request">The creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created mapping.</returns>
    Task<ImportMappingDto> CreateMappingAsync(CreateImportMappingRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing mapping.
    /// </summary>
    /// <param name="id">The mapping ID.</param>
    /// <param name="request">The update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated mapping if found.</returns>
    Task<ImportMappingDto?> UpdateMappingAsync(Guid id, UpdateImportMappingRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a mapping.
    /// </summary>
    /// <param name="id">The mapping ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteMappingAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Suggests a mapping based on CSV headers by matching against existing mappings.
    /// </summary>
    /// <param name="headers">The CSV column headers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A suggested mapping if a match is found.</returns>
    Task<ImportMappingDto?> SuggestMappingAsync(IReadOnlyList<string> headers, CancellationToken cancellationToken = default);
}
