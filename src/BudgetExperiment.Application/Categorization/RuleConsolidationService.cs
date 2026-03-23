// <copyright file="RuleConsolidationService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Orchestrates rule consolidation analysis and persists the results as
/// <see cref="RuleSuggestion"/> domain entities.
/// </summary>
public sealed class RuleConsolidationService : IRuleConsolidationService
{
    private readonly ICategorizationRuleRepository _ruleRepository;
    private readonly IRuleSuggestionRepository _suggestionRepository;
    private readonly RuleConsolidationAnalyzer _analyzer;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="RuleConsolidationService"/> class.
    /// </summary>
    /// <param name="ruleRepository">Repository for categorization rules.</param>
    /// <param name="suggestionRepository">Repository for rule suggestions.</param>
    /// <param name="analyzer">Analyzer that detects consolidation opportunities.</param>
    /// <param name="unitOfWork">Unit of work for saving changes.</param>
    public RuleConsolidationService(
        ICategorizationRuleRepository ruleRepository,
        IRuleSuggestionRepository suggestionRepository,
        RuleConsolidationAnalyzer analyzer,
        IUnitOfWork unitOfWork)
    {
        _ruleRepository = ruleRepository;
        _suggestionRepository = suggestionRepository;
        _analyzer = analyzer;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RuleSuggestion>> AnalyzeAndStoreAsync(CancellationToken cancellationToken = default)
    {
        var rules = await _ruleRepository.GetActiveByPriorityAsync(cancellationToken);
        var consolidations = await _analyzer.AnalyzeAsync(rules);

        if (consolidations.Count == 0)
        {
            return Array.Empty<RuleSuggestion>();
        }

        var suggestions = consolidations.Select(BuildSuggestion).ToList();

        await _suggestionRepository.AddRangeAsync(suggestions, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return suggestions.AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task<CategorizationRule> AcceptConsolidationAsync(Guid suggestionId, CancellationToken cancellationToken = default)
    {
        var suggestion = await _suggestionRepository.GetByIdAsync(suggestionId, cancellationToken)
            ?? throw new DomainException($"Suggestion {suggestionId} not found", DomainExceptionType.NotFound);

        if (suggestion.Type != SuggestionType.RuleConsolidation)
        {
            throw new DomainException($"Suggestion {suggestionId} is not a consolidation suggestion.");
        }

        var sourceRules = await _ruleRepository.GetByIdsAsync(suggestion.ConflictingRuleIds, cancellationToken);
        var mergedPattern = suggestion.OptimizedPattern!;
        var priority = await _ruleRepository.GetNextPriorityAsync(cancellationToken);
        var name = BuildMergedRuleName(mergedPattern);

        var mergedRule = CategorizationRule.Create(
            name: name,
            matchType: RuleMatchType.Regex,
            pattern: mergedPattern,
            categoryId: sourceRules[0].CategoryId,
            priority: priority);

        foreach (var rule in sourceRules)
        {
            rule.Deactivate();
        }

        await _ruleRepository.AddAsync(mergedRule, cancellationToken);
        suggestion.RecordMergedRuleId(mergedRule.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return mergedRule;
    }

    /// <inheritdoc/>
    public async Task UndoConsolidationAsync(Guid suggestionId, CancellationToken cancellationToken = default)
    {
        var suggestion = await _suggestionRepository.GetByIdAsync(suggestionId, cancellationToken)
            ?? throw new DomainException($"Suggestion {suggestionId} not found", DomainExceptionType.NotFound);

        if (suggestion.Status != SuggestionStatus.Accepted)
        {
            throw new DomainException(
                $"Suggestion {suggestionId} has not been accepted and cannot be undone.",
                DomainExceptionType.InvalidState);
        }

        var sourceRules = await _ruleRepository.GetByIdsAsync(suggestion.ConflictingRuleIds, cancellationToken);
        foreach (var rule in sourceRules)
        {
            rule.Activate();
        }

        var mergedRule = await _ruleRepository.GetByIdAsync(suggestion.MergedRuleId!.Value, cancellationToken);
        mergedRule?.Deactivate();
        suggestion.Reopen();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DismissConsolidationAsync(Guid suggestionId, CancellationToken cancellationToken = default)
    {
        var suggestion = await _suggestionRepository.GetByIdAsync(suggestionId, cancellationToken)
            ?? throw new DomainException($"Suggestion {suggestionId} not found", DomainExceptionType.NotFound);

        suggestion.Dismiss();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static string BuildMergedRuleName(string pattern)
    {
        const string prefix = "Consolidated: ";
        var full = prefix + pattern;
        return full.Length <= CategorizationRule.MaxNameLength
            ? full
            : full[..CategorizationRule.MaxNameLength];
    }

    private static RuleSuggestion BuildSuggestion(ConsolidationSuggestion consolidation)
    {
        var count = consolidation.SourceRuleIds.Count;
        return RuleSuggestion.CreateConsolidationSuggestion(
            title: $"Consolidate {count} rules into one",
            description: $"Merge {count} overlapping rules into a single consolidated rule using pattern '{consolidation.MergedPattern}'.",
            reasoning: "Rules share the same pattern and target category.",
            confidence: (decimal)consolidation.Confidence,
            ruleIds: consolidation.SourceRuleIds,
            consolidatedPattern: consolidation.MergedPattern);
    }
}
