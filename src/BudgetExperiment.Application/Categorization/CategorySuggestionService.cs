// <copyright file="CategorySuggestionService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Service for generating and managing AI-powered category suggestions.
/// </summary>
public sealed class CategorySuggestionService : ICategorySuggestionService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IBudgetCategoryRepository _categoryRepository;
    private readonly ICategorySuggestionRepository _suggestionRepository;
    private readonly IDismissedSuggestionPatternRepository _dismissedRepository;
    private readonly IMerchantMappingService _merchantMappingService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserContext _userContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategorySuggestionService"/> class.
    /// </summary>
    /// <param name="transactionRepository">Transaction repository.</param>
    /// <param name="categoryRepository">Category repository.</param>
    /// <param name="suggestionRepository">Suggestion repository.</param>
    /// <param name="dismissedRepository">Dismissed pattern repository.</param>
    /// <param name="merchantMappingService">Merchant mapping service.</param>
    /// <param name="unitOfWork">Unit of work.</param>
    /// <param name="userContext">User context.</param>
    public CategorySuggestionService(
        ITransactionRepository transactionRepository,
        IBudgetCategoryRepository categoryRepository,
        ICategorySuggestionRepository suggestionRepository,
        IDismissedSuggestionPatternRepository dismissedRepository,
        IMerchantMappingService merchantMappingService,
        IUnitOfWork unitOfWork,
        IUserContext userContext)
    {
        _transactionRepository = transactionRepository;
        _categoryRepository = categoryRepository;
        _suggestionRepository = suggestionRepository;
        _dismissedRepository = dismissedRepository;
        _merchantMappingService = merchantMappingService;
        _unitOfWork = unitOfWork;
        _userContext = userContext;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CategorySuggestion>> AnalyzeTransactionsAsync(CancellationToken cancellationToken = default)
    {
        var ownerId = _userContext.UserId;

        // Get uncategorized transactions
        var uncategorized = await _transactionRepository.GetUncategorizedAsync(cancellationToken);
        if (uncategorized.Count == 0)
        {
            return Array.Empty<CategorySuggestion>();
        }

        // Get existing categories to avoid suggesting duplicates
        var existingCategories = await _categoryRepository.GetActiveAsync(cancellationToken);
        var existingCategoryNames = new HashSet<string>(
            existingCategories.Select(c => c.Name),
            StringComparer.OrdinalIgnoreCase);

        // Clear any pending suggestions before re-analyzing
        await _suggestionRepository.DeletePendingByOwnerAsync(ownerId, cancellationToken);

        // Find matching patterns in transaction descriptions
        var descriptions = uncategorized.Select(t => t.Description).ToList();
        var patternMatches = await _merchantMappingService.FindMatchingPatternsAsync(ownerId, descriptions, cancellationToken);

        // Group patterns by suggested category
        var categoryGroups = patternMatches
            .GroupBy(p => p.Category, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var suggestions = new List<CategorySuggestion>();

        foreach (var (categoryName, patterns) in categoryGroups)
        {
            // Skip if category already exists
            if (existingCategoryNames.Contains(categoryName))
            {
                continue;
            }

            // Skip if category was dismissed
            if (await _dismissedRepository.IsDismissedAsync(ownerId, categoryName, cancellationToken))
            {
                continue;
            }

            // Get total transaction count and patterns for this category
            var totalCount = patterns.Sum(p => p.TransactionCount);
            var allPatterns = patterns.Select(p => p.Pattern).Distinct().ToList();
            var icon = patterns.FirstOrDefault()?.Icon ?? "category";

            // Calculate confidence based on transaction count and pattern diversity
            var confidence = CalculateConfidence(totalCount, allPatterns.Count);

            var suggestion = CategorySuggestion.Create(
                categoryName,
                MerchantKnowledgeBase.GetCategoryType(categoryName),
                allPatterns,
                totalCount,
                confidence,
                ownerId,
                icon);

            suggestions.Add(suggestion);
        }

        if (suggestions.Count > 0)
        {
            await _suggestionRepository.AddRangeAsync(suggestions, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return suggestions.OrderByDescending(s => s.Confidence).ThenByDescending(s => s.MatchingTransactionCount).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CategorySuggestion>> GetPendingSuggestionsAsync(CancellationToken cancellationToken = default)
    {
        return await _suggestionRepository.GetPendingByOwnerAsync(_userContext.UserId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CategorySuggestion?> GetSuggestionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _suggestionRepository.GetByIdAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<AcceptSuggestionResult> AcceptSuggestionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return AcceptSuggestionAsync(id, null, null, null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AcceptSuggestionResult> AcceptSuggestionAsync(
        Guid id,
        string? customName,
        string? customIcon,
        string? customColor,
        CancellationToken cancellationToken = default)
    {
        var suggestion = await _suggestionRepository.GetByIdAsync(id, cancellationToken);
        if (suggestion == null)
        {
            return AcceptSuggestionResult.Failed(id, "Suggestion not found.");
        }

        if (suggestion.OwnerId != _userContext.UserId)
        {
            return AcceptSuggestionResult.Failed(id, "You do not have permission to accept this suggestion.");
        }

        if (suggestion.Status != SuggestionStatus.Pending)
        {
            return AcceptSuggestionResult.Failed(id, "Suggestion has already been processed.");
        }

        // Use custom values or fall back to suggestion values
        var categoryName = string.IsNullOrWhiteSpace(customName) ? suggestion.SuggestedName : customName.Trim();
        var categoryIcon = string.IsNullOrWhiteSpace(customIcon) ? suggestion.SuggestedIcon : customIcon.Trim();
        var categoryColor = string.IsNullOrWhiteSpace(customColor) ? suggestion.SuggestedColor : customColor.Trim();

        // Check if category name already exists
        var existingCategory = await _categoryRepository.GetByNameAsync(categoryName, cancellationToken);
        if (existingCategory != null)
        {
            return AcceptSuggestionResult.Failed(id, $"A category named '{categoryName}' already exists.");
        }

        // Create the category
        var category = BudgetCategory.Create(
            categoryName,
            suggestion.SuggestedType,
            categoryIcon,
            categoryColor);

        await _categoryRepository.AddAsync(category, cancellationToken);

        // Mark suggestion as accepted
        suggestion.Accept();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return AcceptSuggestionResult.Succeeded(id, category.Id, categoryName);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AcceptSuggestionResult>> AcceptSuggestionsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        var results = new List<AcceptSuggestionResult>();

        foreach (var id in ids)
        {
            var result = await AcceptSuggestionAsync(id, cancellationToken);
            results.Add(result);
        }

        return results;
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

        // Mark suggestion as dismissed
        suggestion.Dismiss();

        // Add dismissed pattern to prevent re-suggesting
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
    public async Task<IReadOnlyList<SuggestedRule>> GetSuggestedRulesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var suggestion = await _suggestionRepository.GetByIdAsync(id, cancellationToken);
        if (suggestion == null)
        {
            return Array.Empty<SuggestedRule>();
        }

        var uncategorized = await _transactionRepository.GetUncategorizedAsync(cancellationToken);

        var suggestedRules = new List<SuggestedRule>();

        foreach (var pattern in suggestion.MerchantPatterns)
        {
            var matchingDescriptions = uncategorized
                .Where(t => t.Description.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                .Select(t => t.Description)
                .Take(5)
                .ToList();

            var matchCount = uncategorized.Count(t => t.Description.Contains(pattern, StringComparison.OrdinalIgnoreCase));

            if (matchCount > 0)
            {
                suggestedRules.Add(new SuggestedRule
                {
                    Pattern = pattern,
                    MatchType = RuleMatchType.Contains,
                    MatchingTransactionCount = matchCount,
                    SampleDescriptions = matchingDescriptions,
                });
            }
        }

        return suggestedRules.OrderByDescending(r => r.MatchingTransactionCount).ToList();
    }

    /// <summary>
    /// Calculates confidence score based on transaction count and pattern diversity.
    /// </summary>
    private static decimal CalculateConfidence(int transactionCount, int patternCount)
    {
        // Base confidence from transaction count
        var countConfidence = transactionCount switch
        {
            >= 10 => 0.9m,
            >= 5 => 0.8m,
            >= 3 => 0.7m,
            >= 2 => 0.6m,
            _ => 0.5m,
        };

        // Boost for multiple patterns confirming the same category
        var patternBoost = patternCount switch
        {
            >= 3 => 0.1m,
            >= 2 => 0.05m,
            _ => 0m,
        };

        return Math.Min(0.99m, countConfidence + patternBoost);
    }
}
