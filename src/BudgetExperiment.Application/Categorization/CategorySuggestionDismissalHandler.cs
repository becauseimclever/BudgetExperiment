// <copyright file="CategorySuggestionDismissalHandler.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Handles dismissing and restoring category suggestions, including
/// managing dismissed suggestion patterns to prevent re-analysis.
/// </summary>
public sealed class CategorySuggestionDismissalHandler : ICategorySuggestionDismissalHandler
{
    private readonly ICategorySuggestionRepository _suggestionRepository;
    private readonly IDismissedSuggestionPatternRepository _dismissedRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserContext _userContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategorySuggestionDismissalHandler"/> class.
    /// </summary>
    /// <param name="suggestionRepository">The suggestion repository.</param>
    /// <param name="dismissedRepository">The dismissed pattern repository.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <param name="userContext">The user context.</param>
    public CategorySuggestionDismissalHandler(
        ICategorySuggestionRepository suggestionRepository,
        IDismissedSuggestionPatternRepository dismissedRepository,
        IUnitOfWork unitOfWork,
        IUserContext userContext)
    {
        _suggestionRepository = suggestionRepository;
        _dismissedRepository = dismissedRepository;
        _unitOfWork = unitOfWork;
        _userContext = userContext;
    }

    /// <inheritdoc />
    public async Task<bool> DismissSuggestionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var suggestion = await _suggestionRepository.GetByIdAsync(id, cancellationToken);
        if (suggestion == null)
        {
            return false;
        }

        if (suggestion.OwnerId != _userContext.UserId)
        {
            return false;
        }

        if (suggestion.Status != SuggestionStatus.Pending)
        {
            return false;
        }

        suggestion.Dismiss();

        var isDismissed = await _dismissedRepository.IsDismissedAsync(
            _userContext.UserId,
            suggestion.SuggestedName,
            cancellationToken);

        if (!isDismissed)
        {
            var dismissedPattern = DismissedSuggestionPattern.Create(suggestion.SuggestedName, _userContext.UserId);
            await _dismissedRepository.AddAsync(dismissedPattern, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> RestoreSuggestionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var suggestion = await _suggestionRepository.GetByIdAsync(id, cancellationToken);
        if (suggestion == null)
        {
            return false;
        }

        if (suggestion.OwnerId != _userContext.UserId)
        {
            return false;
        }

        if (suggestion.Status != SuggestionStatus.Dismissed)
        {
            return false;
        }

        suggestion.Restore();

        var dismissedPattern = await _dismissedRepository.GetByPatternAsync(
            _userContext.UserId,
            suggestion.SuggestedName.ToUpperInvariant(),
            cancellationToken);

        if (dismissedPattern != null)
        {
            await _dismissedRepository.RemoveAsync(dismissedPattern, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <inheritdoc />
    public async Task<int> ClearDismissedPatternsAsync(CancellationToken cancellationToken = default)
    {
        var clearedCount = await _dismissedRepository.ClearByOwnerAsync(_userContext.UserId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return clearedCount;
    }
}
