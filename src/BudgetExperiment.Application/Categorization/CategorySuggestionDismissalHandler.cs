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
        this._suggestionRepository = suggestionRepository;
        this._dismissedRepository = dismissedRepository;
        this._unitOfWork = unitOfWork;
        this._userContext = userContext;
    }

    /// <inheritdoc />
    public async Task<bool> DismissSuggestionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var suggestion = await this._suggestionRepository.GetByIdAsync(id, cancellationToken);
        if (suggestion == null)
        {
            return false;
        }

        if (suggestion.OwnerId != this._userContext.UserId)
        {
            return false;
        }

        if (suggestion.Status != SuggestionStatus.Pending)
        {
            return false;
        }

        suggestion.Dismiss();

        var isDismissed = await this._dismissedRepository.IsDismissedAsync(
            this._userContext.UserId,
            suggestion.SuggestedName,
            cancellationToken);

        if (!isDismissed)
        {
            var dismissedPattern = DismissedSuggestionPattern.Create(suggestion.SuggestedName, this._userContext.UserId);
            await this._dismissedRepository.AddAsync(dismissedPattern, cancellationToken);
        }

        await this._unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> RestoreSuggestionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var suggestion = await this._suggestionRepository.GetByIdAsync(id, cancellationToken);
        if (suggestion == null)
        {
            return false;
        }

        if (suggestion.OwnerId != this._userContext.UserId)
        {
            return false;
        }

        if (suggestion.Status != SuggestionStatus.Dismissed)
        {
            return false;
        }

        suggestion.Restore();

        var dismissedPattern = await this._dismissedRepository.GetByPatternAsync(
            this._userContext.UserId,
            suggestion.SuggestedName.ToUpperInvariant(),
            cancellationToken);

        if (dismissedPattern != null)
        {
            await this._dismissedRepository.RemoveAsync(dismissedPattern, cancellationToken);
        }

        await this._unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <inheritdoc />
    public async Task<int> ClearDismissedPatternsAsync(CancellationToken cancellationToken = default)
    {
        var clearedCount = await this._dismissedRepository.ClearByOwnerAsync(this._userContext.UserId, cancellationToken);
        await this._unitOfWork.SaveChangesAsync(cancellationToken);
        return clearedCount;
    }
}
