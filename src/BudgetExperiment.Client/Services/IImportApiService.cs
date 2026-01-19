// <copyright file="IImportApiService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Models;
using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Service interface for communicating with the Import API.
/// </summary>
public interface IImportApiService
{
    /// <summary>
    /// Parses an uploaded CSV file.
    /// </summary>
    /// <param name="fileContent">The file content as a stream.</param>
    /// <param name="fileName">The file name.</param>
    /// <returns>The parse result.</returns>
    Task<CsvParseResultModel?> ParseCsvAsync(Stream fileContent, string fileName);

    /// <summary>
    /// Gets all saved import mappings for the current user.
    /// </summary>
    /// <returns>List of saved mappings.</returns>
    Task<IReadOnlyList<ImportMappingDto>> GetMappingsAsync();

    /// <summary>
    /// Gets a specific import mapping by ID.
    /// </summary>
    /// <param name="id">The mapping ID.</param>
    /// <returns>The mapping or null if not found.</returns>
    Task<ImportMappingDto?> GetMappingAsync(Guid id);

    /// <summary>
    /// Creates a new import mapping.
    /// </summary>
    /// <param name="request">The creation request.</param>
    /// <returns>The created mapping.</returns>
    Task<ImportMappingDto?> CreateMappingAsync(CreateImportMappingRequest request);

    /// <summary>
    /// Updates an existing import mapping.
    /// </summary>
    /// <param name="id">The mapping ID.</param>
    /// <param name="request">The update request.</param>
    /// <returns>The updated mapping.</returns>
    Task<ImportMappingDto?> UpdateMappingAsync(Guid id, UpdateImportMappingRequest request);

    /// <summary>
    /// Deletes an import mapping.
    /// </summary>
    /// <param name="id">The mapping ID.</param>
    /// <returns>True if deleted successfully.</returns>
    Task<bool> DeleteMappingAsync(Guid id);

    /// <summary>
    /// Suggests a mapping based on CSV headers.
    /// </summary>
    /// <param name="headers">The CSV headers.</param>
    /// <returns>A suggested mapping or null if no match.</returns>
    Task<ImportMappingDto?> SuggestMappingAsync(IReadOnlyList<string> headers);

    /// <summary>
    /// Previews an import with validation and categorization.
    /// </summary>
    /// <param name="request">The preview request.</param>
    /// <returns>The preview result.</returns>
    Task<ImportPreviewResult?> PreviewAsync(ImportPreviewRequest request);

    /// <summary>
    /// Executes an import, creating transactions.
    /// </summary>
    /// <param name="request">The execution request.</param>
    /// <returns>The import result.</returns>
    Task<ImportResult?> ExecuteAsync(ImportExecuteRequest request);

    /// <summary>
    /// Gets import history for the current user.
    /// </summary>
    /// <returns>List of import batches.</returns>
    Task<IReadOnlyList<ImportBatchDto>> GetHistoryAsync();

    /// <summary>
    /// Gets a specific import batch by ID.
    /// </summary>
    /// <param name="id">The batch ID.</param>
    /// <returns>The batch or null if not found.</returns>
    Task<ImportBatchDto?> GetBatchAsync(Guid id);

    /// <summary>
    /// Deletes all transactions from an import batch (undo import).
    /// </summary>
    /// <param name="id">The batch ID.</param>
    /// <returns>The count of deleted transactions, or null if not found.</returns>
    Task<int?> DeleteBatchAsync(Guid id);
}
