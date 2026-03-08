// <copyright file="StubCategorySuggestionApiService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Client.Tests.TestHelpers;

/// <summary>
/// Shared stub implementation of <see cref="ICategorySuggestionApiService"/> for page-level bUnit tests.
/// </summary>
internal class StubCategorySuggestionApiService : ICategorySuggestionApiService
{
    /// <summary>
    /// Gets the list of pending suggestions returned by <see cref="GetPendingAsync"/>.
    /// </summary>
    public List<CategorySuggestionDto> PendingSuggestions { get; } = new();

    /// <summary>
    /// Gets the list of dismissed suggestions returned by <see cref="GetDismissedAsync"/>.
    /// </summary>
    public List<CategorySuggestionDto> DismissedSuggestions { get; } = new();

    /// <summary>
    /// Gets the list of suggestions returned by <see cref="AnalyzeAsync"/>.
    /// </summary>
    public List<CategorySuggestionDto> AnalyzeResult { get; } = new();

    /// <summary>
    /// Gets or sets the accept result returned by <see cref="AcceptAsync"/>.
    /// </summary>
    public AcceptCategorySuggestionResultDto? AcceptResult { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="DismissAsync"/> returns true.
    /// </summary>
    public bool DismissResult { get; set; }

    /// <summary>
    /// Gets or sets the restored suggestion returned by <see cref="RestoreAsync"/>.
    /// </summary>
    public CategorySuggestionDto? RestoreResult { get; set; }

    /// <summary>
    /// Gets or sets the count returned by <see cref="ClearDismissedPatternsAsync"/>.
    /// </summary>
    public int ClearDismissedPatternsResult { get; set; }

    /// <summary>
    /// Gets the list of rules returned by <see cref="PreviewRulesAsync"/>.
    /// </summary>
    public List<SuggestedCategoryRuleDto> PreviewRules { get; } = new();

    /// <summary>
    /// Gets the list of results returned by <see cref="BulkAcceptAsync"/>.
    /// </summary>
    public List<AcceptCategorySuggestionResultDto> BulkAcceptResults { get; } = new();

    /// <inheritdoc/>
    public Task<IReadOnlyList<CategorySuggestionDto>> AnalyzeAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<CategorySuggestionDto>>(this.AnalyzeResult);

    /// <inheritdoc/>
    public Task<IReadOnlyList<CategorySuggestionDto>> GetPendingAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<CategorySuggestionDto>>(this.PendingSuggestions);

    /// <inheritdoc/>
    public Task<CategorySuggestionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(this.PendingSuggestions.Find(s => s.Id == id) ?? this.DismissedSuggestions.Find(s => s.Id == id));

    /// <inheritdoc/>
    public Task<AcceptCategorySuggestionResultDto> AcceptAsync(Guid id, AcceptCategorySuggestionRequest? request = null, CancellationToken cancellationToken = default)
        => Task.FromResult(this.AcceptResult ?? new AcceptCategorySuggestionResultDto { SuggestionId = id, Success = false });

    /// <inheritdoc/>
    public Task<IReadOnlyList<CategorySuggestionDto>> GetDismissedAsync(int skip = 0, int take = 20, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<CategorySuggestionDto>>(this.DismissedSuggestions);

    /// <inheritdoc/>
    public Task<bool> DismissAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(this.DismissResult);

    /// <inheritdoc/>
    public Task<CategorySuggestionDto?> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(this.RestoreResult);

    /// <inheritdoc/>
    public Task<int> ClearDismissedPatternsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(this.ClearDismissedPatternsResult);

    /// <inheritdoc/>
    public Task<IReadOnlyList<AcceptCategorySuggestionResultDto>> BulkAcceptAsync(IEnumerable<Guid> suggestionIds, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<AcceptCategorySuggestionResultDto>>(this.BulkAcceptResults);

    /// <inheritdoc/>
    public Task<IReadOnlyList<SuggestedCategoryRuleDto>> PreviewRulesAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<SuggestedCategoryRuleDto>>(this.PreviewRules);

    /// <inheritdoc/>
    public Task<CreateRulesFromSuggestionResult> CreateRulesAsync(Guid id, CreateRulesFromSuggestionRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(new CreateRulesFromSuggestionResult { Success = true });
}
