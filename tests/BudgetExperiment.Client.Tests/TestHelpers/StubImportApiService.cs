// <copyright file="StubImportApiService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Models;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Client.Tests.TestHelpers;

/// <summary>
/// Shared stub implementation of <see cref="IImportApiService"/> for page-level bUnit tests.
/// </summary>
internal sealed class StubImportApiService : IImportApiService
{
    /// <summary>
    /// Gets the list of mappings returned by <see cref="GetMappingsAsync"/>.
    /// </summary>
    public List<ImportMappingDto> Mappings { get; } = new();

    /// <summary>
    /// Gets the list of import batches returned by <see cref="GetHistoryAsync"/>.
    /// </summary>
    public List<ImportBatchDto> Batches { get; } = new();

    /// <summary>
    /// Gets or sets the suggested mapping returned by <see cref="SuggestMappingAsync"/>.
    /// </summary>
    public ImportMappingDto? SuggestedMapping { get; set; }

    /// <summary>
    /// Gets or sets the preview result returned by <see cref="PreviewAsync"/>.
    /// </summary>
    public ImportPreviewResult? PreviewResult { get; set; }

    /// <summary>
    /// Gets or sets the import result returned by <see cref="ExecuteAsync"/>.
    /// </summary>
    public ImportResult? ExecuteResult { get; set; }

    /// <summary>
    /// Gets or sets the created mapping returned by <see cref="CreateMappingAsync"/>.
    /// </summary>
    public ImportMappingDto? CreateMappingResult { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="GetMappingsAsync"/> should throw.
    /// </summary>
    public bool ShouldThrowOnGetMappings { get; set; }

    /// <summary>
    /// Gets or sets the result returned by <see cref="UpdateMappingAsync"/>.
    /// </summary>
    public ApiResult<ImportMappingDto>? UpdateMappingResult { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="DeleteMappingAsync"/> returns true.
    /// </summary>
    public bool DeleteMappingResult { get; set; } = true;

    /// <summary>
    /// Gets or sets the count returned by <see cref="DeleteBatchAsync"/>.
    /// </summary>
    public int? DeleteBatchResult { get; set; } = 5;

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="GetHistoryAsync"/> should throw.
    /// </summary>
    public bool ShouldThrowOnGetHistory { get; set; }

    /// <inheritdoc/>
    public Task<IReadOnlyList<ImportMappingDto>> GetMappingsAsync()
    {
        if (this.ShouldThrowOnGetMappings)
        {
            throw new HttpRequestException("Failed to load mappings");
        }

        return Task.FromResult<IReadOnlyList<ImportMappingDto>>(this.Mappings);
    }

    /// <inheritdoc/>
    public Task<ImportMappingDto?> GetMappingAsync(Guid id) =>
        Task.FromResult(this.Mappings.Find(m => m.Id == id));

    /// <inheritdoc/>
    public Task<ImportMappingDto?> CreateMappingAsync(CreateImportMappingRequest request) =>
        Task.FromResult(this.CreateMappingResult);

    /// <inheritdoc/>
    public Task<ApiResult<ImportMappingDto>> UpdateMappingAsync(Guid id, UpdateImportMappingRequest request, string? version = null) =>
        Task.FromResult(this.UpdateMappingResult ?? ApiResult<ImportMappingDto>.Failure());

    /// <inheritdoc/>
    public Task<bool> DeleteMappingAsync(Guid id) => Task.FromResult(this.DeleteMappingResult);

    /// <inheritdoc/>
    public Task<ImportMappingDto?> SuggestMappingAsync(IReadOnlyList<string> headers) =>
        Task.FromResult(this.SuggestedMapping);

    /// <inheritdoc/>
    public Task<ImportPreviewResult?> PreviewAsync(ImportPreviewRequest request) =>
        Task.FromResult(this.PreviewResult);

    /// <inheritdoc/>
    public Task<ImportResult?> ExecuteAsync(ImportExecuteRequest request) =>
        Task.FromResult(this.ExecuteResult);

    /// <inheritdoc/>
    public Task<IReadOnlyList<ImportBatchDto>> GetHistoryAsync()
    {
        if (this.ShouldThrowOnGetHistory)
        {
            throw new HttpRequestException("Failed to load history");
        }

        return Task.FromResult<IReadOnlyList<ImportBatchDto>>(this.Batches);
    }

    /// <inheritdoc/>
    public Task<ImportBatchDto?> GetBatchAsync(Guid id) =>
        Task.FromResult(this.Batches.Find(b => b.Id == id));

    /// <inheritdoc/>
    public Task<int?> DeleteBatchAsync(Guid id) => Task.FromResult(this.DeleteBatchResult);
}
