// <copyright file="MerchantMappingService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using BudgetExperiment.Domain.Categorization;

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Service for resolving merchant-to-category mappings.
/// Combines default knowledge base with user-learned mappings.
/// </summary>
public sealed class MerchantMappingService : IMerchantMappingService
{
    private readonly ILearnedMerchantMappingRepository _learnedMappingRepository;
    private readonly IBudgetCategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="MerchantMappingService"/> class.
    /// </summary>
    /// <param name="learnedMappingRepository">The learned merchant mapping repository.</param>
    /// <param name="categoryRepository">The category repository.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    public MerchantMappingService(
        ILearnedMerchantMappingRepository learnedMappingRepository,
        IBudgetCategoryRepository categoryRepository,
        IUnitOfWork unitOfWork)
    {
        _learnedMappingRepository = learnedMappingRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<MerchantMappingResult?> GetMappingAsync(
        string ownerId,
        string description,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        // First check learned mappings (takes precedence)
        var learnedMappings = await _learnedMappingRepository.GetByOwnerAsync(ownerId, cancellationToken);
        var normalizedDescription = description.ToUpperInvariant();

        foreach (var learned in learnedMappings.OrderByDescending(m => m.LearnCount))
        {
            if (normalizedDescription.Contains(learned.MerchantPattern, StringComparison.OrdinalIgnoreCase))
            {
                return new MerchantMappingResult
                {
                    Pattern = learned.MerchantPattern,
                    Category = "Learned", // Category name would need to be looked up
                    Icon = "star", // Default for learned
                    IsLearned = true,
                    CategoryId = learned.CategoryId,
                };
            }
        }

        // Fall back to default knowledge base
        var pattern = MerchantKnowledgeBase.GetMatchedPattern(description);
        if (pattern != null && MerchantKnowledgeBase.TryGetMapping(description, out var mapping))
        {
            return new MerchantMappingResult
            {
                Pattern = pattern,
                Category = mapping!.Value.Category,
                Icon = mapping.Value.Icon,
                IsLearned = false,
                CategoryId = null,
            };
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PatternMatch>> FindMatchingPatternsAsync(
        string ownerId,
        IEnumerable<string> descriptions,
        CancellationToken cancellationToken = default)
    {
        var descriptionList = descriptions.ToList();
        var patternMatches = new Dictionary<string, PatternMatch>(StringComparer.OrdinalIgnoreCase);

        // Load learned mappings
        var learnedMappings = await _learnedMappingRepository.GetByOwnerAsync(ownerId, cancellationToken);

        foreach (var description in descriptionList)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                continue;
            }

            var normalizedDescription = description.ToUpperInvariant();

            // Check learned mappings first
            LearnedMerchantMapping? matchedLearned = null;
            foreach (var learned in learnedMappings.OrderByDescending(m => m.LearnCount))
            {
                if (normalizedDescription.Contains(learned.MerchantPattern, StringComparison.OrdinalIgnoreCase))
                {
                    matchedLearned = learned;
                    break;
                }
            }

            if (matchedLearned != null)
            {
                if (!patternMatches.TryGetValue(matchedLearned.MerchantPattern, out var existingMatch))
                {
                    existingMatch = new PatternMatch
                    {
                        Pattern = matchedLearned.MerchantPattern,
                        Category = "Learned",
                        Icon = "star",
                        IsLearned = true,
                    };
                    patternMatches[matchedLearned.MerchantPattern] = existingMatch;
                }

                existingMatch.TransactionCount++;
                if (existingMatch.SampleDescriptions.Count < 5)
                {
                    existingMatch.SampleDescriptions.Add(description);
                }

                continue;
            }

            // Check default knowledge base
            var pattern = MerchantKnowledgeBase.GetMatchedPattern(description);
            if (pattern != null && MerchantKnowledgeBase.TryGetMapping(description, out var mapping))
            {
                if (!patternMatches.TryGetValue(pattern, out var existingMatch))
                {
                    existingMatch = new PatternMatch
                    {
                        Pattern = pattern,
                        Category = mapping!.Value.Category,
                        Icon = mapping.Value.Icon,
                        IsLearned = false,
                    };
                    patternMatches[pattern] = existingMatch;
                }

                existingMatch.TransactionCount++;
                if (existingMatch.SampleDescriptions.Count < 5)
                {
                    existingMatch.SampleDescriptions.Add(description);
                }
            }
        }

        return patternMatches.Values
            .OrderByDescending(m => m.TransactionCount)
            .ThenBy(m => m.Category)
            .ThenBy(m => m.Pattern)
            .ToList();
    }

    /// <inheritdoc />
    public async Task LearnFromCategorizationAsync(
        string ownerId,
        string description,
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return;
        }

        // Extract a merchant pattern from the description
        var pattern = ExtractMerchantPattern(description);
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return;
        }

        // Check if we already have this mapping
        var existingMappings = await _learnedMappingRepository.GetByOwnerAsync(ownerId, cancellationToken);
        var existing = existingMappings.FirstOrDefault(m =>
            m.MerchantPattern.Equals(pattern, StringComparison.OrdinalIgnoreCase) &&
            m.CategoryId == categoryId);

        if (existing != null)
        {
            // Reinforce the existing mapping
            existing.IncrementLearnCount();
        }
        else
        {
            // Create a new mapping
            var mapping = LearnedMerchantMapping.Create(pattern, categoryId, ownerId);
            await _learnedMappingRepository.AddAsync(mapping, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LearnedMerchantMappingInfo>> GetLearnedMappingsAsync(
        string ownerId,
        CancellationToken cancellationToken = default)
    {
        var mappings = await _learnedMappingRepository.GetByOwnerAsync(ownerId, cancellationToken);
        var categoryIds = mappings.Select(m => m.CategoryId).Distinct().ToList();
        var categories = await _categoryRepository.GetByIdsAsync(categoryIds, cancellationToken);
        var categoryDict = categories.ToDictionary(c => c.Id, c => c.Name);

        return mappings
            .Select(m => new LearnedMerchantMappingInfo
            {
                Id = m.Id,
                MerchantPattern = m.MerchantPattern,
                CategoryId = m.CategoryId,
                CategoryName = categoryDict.GetValueOrDefault(m.CategoryId, "Unknown"),
                LearnCount = m.LearnCount,
                CreatedAtUtc = m.CreatedAtUtc,
                UpdatedAtUtc = m.UpdatedAtUtc,
            })
            .OrderByDescending(m => m.LearnCount)
            .ThenBy(m => m.MerchantPattern)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<bool> DeleteLearnedMappingAsync(
        string ownerId,
        Guid mappingId,
        CancellationToken cancellationToken = default)
    {
        var mappings = await _learnedMappingRepository.GetByOwnerAsync(ownerId, cancellationToken);
        var mapping = mappings.FirstOrDefault(m => m.Id == mappingId);

        if (mapping == null)
        {
            return false;
        }

        await _learnedMappingRepository.RemoveAsync(mapping, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Extracts a merchant pattern from a transaction description.
    /// </summary>
    private static string ExtractMerchantPattern(string description)
    {
        // Normalize and clean the description
        var cleaned = description.ToUpperInvariant().Trim();

        // Remove common transaction prefixes
        var prefixes = new[] { "POS ", "DEBIT ", "CREDIT ", "ACH ", "CHECK ", "WIRE ", "PURCHASE " };
        foreach (var prefix in prefixes)
        {
            if (cleaned.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned[prefix.Length..].TrimStart();
            }
        }

        // Take the first significant word(s) as the pattern
        // Split on common separators
        var parts = cleaned.Split(new[] { ' ', '-', '*', '#', '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

        // Take the first meaningful part (skip numbers and very short tokens)
        var significantParts = parts
            .Where(p => p.Length >= 3 && !p.All(char.IsDigit))
            .Take(2)
            .ToList();

        if (significantParts.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(" ", significantParts);
    }
}
