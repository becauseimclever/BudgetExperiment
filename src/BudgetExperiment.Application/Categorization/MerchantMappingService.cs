// <copyright file="MerchantMappingService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Service for resolving merchant-to-category mappings.
/// Combines default knowledge base with user-learned mappings.
/// </summary>
public sealed class MerchantMappingService : IMerchantMappingService
{
    private readonly ILearnedMerchantMappingRepository _learnedMappingRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="MerchantMappingService"/> class.
    /// </summary>
    /// <param name="learnedMappingRepository">The learned merchant mapping repository.</param>
    public MerchantMappingService(ILearnedMerchantMappingRepository learnedMappingRepository)
    {
        _learnedMappingRepository = learnedMappingRepository;
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
}
