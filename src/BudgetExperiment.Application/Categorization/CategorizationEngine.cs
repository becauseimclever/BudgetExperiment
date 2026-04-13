// <copyright file="CategorizationEngine.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

using Microsoft.Extensions.Caching.Memory;

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Implementation of <see cref="ICategorizationEngine"/> for applying auto-categorization rules to transactions.
/// Uses batch transaction loading, in-memory rule caching, and string-first evaluation for performance.
/// </summary>
public class CategorizationEngine : ICategorizationEngine
{
    /// <summary>
    /// Cache key for active rules ordered by priority.
    /// </summary>
    internal const string ActiveRulesCacheKey = "CategorizationEngine_ActiveRules";

    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    private readonly ICategorizationRuleRepository _ruleRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategorizationEngine"/> class.
    /// </summary>
    /// <param name="ruleRepository">The categorization rule repository.</param>
    /// <param name="transactionRepository">The transaction repository.</param>
    /// <param name="unitOfWork">The unit of work for persisting changes.</param>
    /// <param name="cache">The memory cache for caching active rules.</param>
    public CategorizationEngine(
        ICategorizationRuleRepository ruleRepository,
        ITransactionRepository transactionRepository,
        IUnitOfWork unitOfWork,
        IMemoryCache cache)
    {
        _ruleRepository = ruleRepository;
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<Guid?> FindMatchingCategoryAsync(
        string description,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var rules = await this.GetCachedActiveRulesAsync(cancellationToken);
        return FindFirstMatch(rules, description);
    }

    /// <inheritdoc />
    public async Task<CategorizationResult> ApplyRulesAsync(
        IEnumerable<Guid>? transactionIds,
        bool overwriteExisting = false,
        CancellationToken cancellationToken = default)
    {
        var rules = await this.GetCachedActiveRulesAsync(cancellationToken);

        var totalProcessed = 0;
        var categorized = 0;
        var skipped = 0;
        var errors = 0;
        var errorMessages = new List<string>();

        IReadOnlyList<Transaction> transactions;
        var skippedDueToExisting = 0;

        if (transactionIds == null)
        {
            transactions = await _transactionRepository.GetUncategorizedAsync(
                cancellationToken: cancellationToken);
        }
        else
        {
            var idList = transactionIds.ToList();
            var allFetched = await _transactionRepository.GetByIdsAsync(idList, cancellationToken);

            var transactionList = new List<Transaction>();
            foreach (var transaction in allFetched)
            {
                if (overwriteExisting || transaction.CategoryId == null)
                {
                    transactionList.Add(transaction);
                }
                else
                {
                    skippedDueToExisting++;
                }
            }

            transactions = transactionList;
        }

        // Partition rules: string rules first, then regex rules
        var (stringRules, regexRules) = PartitionRules(rules);

        foreach (var transaction in transactions)
        {
            totalProcessed++;

            try
            {
                var matchedCategoryId = FindFirstMatch(stringRules, transaction.Description)
                    ?? FindFirstMatch(regexRules, transaction.Description);

                if (matchedCategoryId.HasValue)
                {
                    transaction.UpdateCategory(matchedCategoryId.Value);
                    categorized++;
                }
                else
                {
                    skipped++;
                }
            }
            catch (Exception ex)
            {
                errors++;
                errorMessages.Add($"Error processing transaction {transaction.Id}: {ex.Message}");
            }
        }

        if (categorized > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new CategorizationResult
        {
            TotalProcessed = totalProcessed + skippedDueToExisting,
            Categorized = categorized,
            Skipped = skipped + skippedDueToExisting,
            Errors = errors,
            ErrorMessages = errorMessages,
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> TestPatternAsync(
        RuleMatchType matchType,
        string pattern,
        bool caseSensitive,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        var testRule = CategorizationRule.Create(
            name: "Test Rule",
            pattern: pattern,
            matchType: matchType,
            categoryId: Guid.NewGuid(),
            caseSensitive: caseSensitive);

        var searchPrefix = GetDescriptionSearchPrefix(matchType, pattern);
        var allDescriptions = await _transactionRepository.GetAllDescriptionsAsync(
            searchPrefix,
            maxResults: 100,
            cancellationToken);

        var matchingDescriptions = new List<string>();

        foreach (var description in allDescriptions)
        {
            if (matchingDescriptions.Count >= limit)
            {
                break;
            }

            try
            {
                if (testRule.Matches(description))
                {
                    matchingDescriptions.Add(description);
                }
            }
            catch
            {
                // Skip descriptions that cause regex errors
            }
        }

        return matchingDescriptions;
    }

    /// <inheritdoc />
    public async Task<Dictionary<Guid, InlineCategorySuggestionDto>> GetBatchSuggestionsAsync(
        IReadOnlyList<Guid> transactionIds,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<Guid, InlineCategorySuggestionDto>();

        if (transactionIds.Count == 0)
        {
            return result;
        }

        var rules = await this.GetCachedActiveRulesAsync(cancellationToken);
        if (rules.Count == 0)
        {
            return result;
        }

        var transactions = await _transactionRepository.GetByIdsAsync(transactionIds, cancellationToken);

        foreach (var transaction in transactions)
        {
            if (transaction.CategoryId is not null)
            {
                continue;
            }

            var matchedCategoryId = FindFirstMatch(rules, transaction.Description);
            if (matchedCategoryId.HasValue)
            {
                var matchedRule = rules.First(r => r.CategoryId == matchedCategoryId.Value && r.Matches(transaction.Description));
                result[transaction.Id] = new InlineCategorySuggestionDto
                {
                    TransactionId = transaction.Id,
                    CategoryId = matchedCategoryId.Value,
                    CategoryName = matchedRule.Category?.Name ?? string.Empty,
                };
            }
        }

        return result;
    }

    /// <summary>
    /// Invalidates the cached active rules, forcing a fresh load on next access.
    /// Called by <see cref="CategorizationRuleService"/> after rule CRUD operations.
    /// </summary>
    public void InvalidateRuleCache()
    {
        _cache.Remove(ActiveRulesCacheKey);
    }

    private static Guid? FindFirstMatch(IReadOnlyList<CategorizationRule> rules, string description)
    {
        foreach (var rule in rules)
        {
            if (rule.Matches(description))
            {
                return rule.CategoryId;
            }
        }

        return null;
    }

    private static string GetDescriptionSearchPrefix(RuleMatchType matchType, string pattern)
    {
        return matchType switch
        {
            RuleMatchType.StartsWith or RuleMatchType.Exact => pattern.Trim(),
            _ => string.Empty,
        };
    }

    private static (IReadOnlyList<CategorizationRule> StringRules, IReadOnlyList<CategorizationRule> RegexRules) PartitionRules(
        IReadOnlyList<CategorizationRule> rules)
    {
        var stringRules = new List<CategorizationRule>();
        var regexRules = new List<CategorizationRule>();

        foreach (var rule in rules)
        {
            if (rule.MatchType == RuleMatchType.Regex)
            {
                regexRules.Add(rule);
            }
            else
            {
                stringRules.Add(rule);
            }
        }

        return (stringRules, regexRules);
    }

    private async Task<IReadOnlyList<CategorizationRule>> GetCachedActiveRulesAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(ActiveRulesCacheKey, out IReadOnlyList<CategorizationRule>? cached) && cached is not null)
        {
            return cached;
        }

        var rules = await _ruleRepository.GetActiveByPriorityAsync(cancellationToken);
        _cache.Set(ActiveRulesCacheKey, rules, CacheDuration);
        return rules;
    }
}
