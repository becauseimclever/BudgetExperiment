// <copyright file="StubRecurringChargeSuggestionApiService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Client.Tests.TestHelpers;

/// <summary>
/// Shared stub implementation of <see cref="IRecurringChargeSuggestionApiService"/> for page-level bUnit tests.
/// All methods return safe defaults (empty lists, null, false). Override individual methods
/// via virtual to customize behavior for specific tests.
/// </summary>
internal class StubRecurringChargeSuggestionApiService : IRecurringChargeSuggestionApiService
{
    /// <summary>
    /// Gets the list of suggestions that will be returned by <see cref="GetSuggestionsAsync"/>.
    /// </summary>
    public List<RecurringChargeSuggestionDto> Suggestions { get; } = new();

    /// <summary>
    /// Gets or sets the count returned by <see cref="DetectAsync"/>.
    /// </summary>
    public int DetectCount { get; set; } = 0;

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="AcceptAsync"/> returns a result.
    /// </summary>
    public AcceptRecurringChargeSuggestionResultDto? AcceptResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="DismissAsync"/> returns true.
    /// </summary>
    public bool DismissResult { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether API calls should throw an exception.
    /// </summary>
    public Exception? ExceptionToThrow
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets a custom override for <see cref="GetSuggestionsAsync"/>.
    /// </summary>
    public Func<Guid?, string?, int, int, CancellationToken, Task<IReadOnlyList<RecurringChargeSuggestionDto>>>? GetSuggestionsAsyncOverride
    {
        get; set;
    }

    /// <inheritdoc />
    public virtual Task<int> DetectAsync(Guid? accountId = null, CancellationToken cancellationToken = default)
    {
        if (ExceptionToThrow != null)
        {
            return Task.FromException<int>(ExceptionToThrow);
        }

        return Task.FromResult(DetectCount);
    }

    /// <inheritdoc />
    public virtual Task<IReadOnlyList<RecurringChargeSuggestionDto>> GetSuggestionsAsync(
        Guid? accountId = null,
        string? status = null,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        if (GetSuggestionsAsyncOverride != null)
        {
            return GetSuggestionsAsyncOverride(accountId, status, skip, take, cancellationToken);
        }

        if (ExceptionToThrow != null)
        {
            return Task.FromException<IReadOnlyList<RecurringChargeSuggestionDto>>(ExceptionToThrow);
        }

        var filtered = Suggestions.AsEnumerable();

        if (!string.IsNullOrEmpty(status))
        {
            filtered = filtered.Where(s => s.Status == status);
        }

        var result = filtered.Skip(skip).Take(take).ToList();
        return Task.FromResult<IReadOnlyList<RecurringChargeSuggestionDto>>(result);
    }

    /// <inheritdoc />
    public virtual Task<RecurringChargeSuggestionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (ExceptionToThrow != null)
        {
            return Task.FromException<RecurringChargeSuggestionDto?>(ExceptionToThrow);
        }

        var suggestion = Suggestions.FirstOrDefault(s => s.Id == id);
        return Task.FromResult(suggestion);
    }

    /// <inheritdoc />
    public virtual Task<AcceptRecurringChargeSuggestionResultDto?> AcceptAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (ExceptionToThrow != null)
        {
            return Task.FromException<AcceptRecurringChargeSuggestionResultDto?>(ExceptionToThrow);
        }

        return Task.FromResult(AcceptResult);
    }

    /// <inheritdoc />
    public virtual Task<bool> DismissAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (ExceptionToThrow != null)
        {
            return Task.FromException<bool>(ExceptionToThrow);
        }

        return Task.FromResult(DismissResult);
    }
}
