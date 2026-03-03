// <copyright file="SuggestionAcceptanceHandler.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Handles the lifecycle of rule suggestions: acceptance, dismissal, and feedback.
/// </summary>
public sealed class SuggestionAcceptanceHandler : ISuggestionAcceptanceHandler
{
    private readonly IRuleSuggestionRepository _suggestionRepository;
    private readonly ICategorizationRuleRepository _ruleRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="SuggestionAcceptanceHandler"/> class.
    /// </summary>
    /// <param name="suggestionRepository">The rule suggestion repository.</param>
    /// <param name="ruleRepository">The categorization rule repository.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    public SuggestionAcceptanceHandler(
        IRuleSuggestionRepository suggestionRepository,
        ICategorizationRuleRepository ruleRepository,
        IUnitOfWork unitOfWork)
    {
        _suggestionRepository = suggestionRepository;
        _ruleRepository = ruleRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc/>
    public async Task<CategorizationRule> AcceptSuggestionAsync(
        Guid suggestionId,
        CancellationToken ct = default)
    {
        var suggestion = await _suggestionRepository.GetByIdAsync(suggestionId, ct)
            ?? throw new DomainException($"Suggestion {suggestionId} not found");

        var rule = suggestion.Type switch
        {
            SuggestionType.NewRule => await AcceptNewRuleSuggestionAsync(suggestion, ct),
            SuggestionType.PatternOptimization => await AcceptPatternOptimizationSuggestionAsync(suggestion, ct),
            SuggestionType.UnusedRule => await AcceptUnusedRuleSuggestionAsync(suggestion, ct),
            SuggestionType.RuleConsolidation => throw new DomainException(
                "Rule consolidation requires manual review. Accept individual changes or create a new rule manually."),
            SuggestionType.RuleConflict => throw new DomainException(
                "Conflict suggestions require manual resolution. Review the conflicting rules and adjust manually."),
            _ => throw new DomainException($"Unsupported suggestion type: {suggestion.Type}"),
        };

        suggestion.Accept();
        await _unitOfWork.SaveChangesAsync(ct);

        return rule;
    }

    /// <inheritdoc/>
    public async Task DismissSuggestionAsync(
        Guid suggestionId,
        string? reason = null,
        CancellationToken ct = default)
    {
        var suggestion = await _suggestionRepository.GetByIdAsync(suggestionId, ct)
            ?? throw new DomainException($"Suggestion {suggestionId} not found");

        suggestion.Dismiss(reason);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task ProvideFeedbackAsync(
        Guid suggestionId,
        bool isPositive,
        CancellationToken ct = default)
    {
        var suggestion = await _suggestionRepository.GetByIdAsync(suggestionId, ct)
            ?? throw new DomainException($"Suggestion {suggestionId} not found");

        suggestion.ProvideFeedback(isPositive);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<CategorizationRule> AcceptNewRuleSuggestionAsync(
        RuleSuggestion suggestion,
        CancellationToken ct)
    {
        if (suggestion.SuggestedPattern is null ||
            suggestion.SuggestedMatchType is null ||
            suggestion.SuggestedCategoryId is null)
        {
            throw new DomainException("Suggestion is missing required fields for rule creation");
        }

        var priority = await _ruleRepository.GetNextPriorityAsync(ct);

        var rule = CategorizationRule.Create(
            name: suggestion.Title,
            matchType: suggestion.SuggestedMatchType.Value,
            pattern: suggestion.SuggestedPattern,
            categoryId: suggestion.SuggestedCategoryId.Value,
            priority: priority);

        await _ruleRepository.AddAsync(rule, ct);

        return rule;
    }

    private async Task<CategorizationRule> AcceptPatternOptimizationSuggestionAsync(
        RuleSuggestion suggestion,
        CancellationToken ct)
    {
        if (suggestion.TargetRuleId is null ||
            suggestion.OptimizedPattern is null)
        {
            throw new DomainException("Suggestion is missing required fields for pattern optimization");
        }

        var rule = await _ruleRepository.GetByIdAsync(suggestion.TargetRuleId.Value, ct)
            ?? throw new DomainException($"Target rule {suggestion.TargetRuleId} not found");

        rule.Update(
            name: rule.Name,
            matchType: rule.MatchType,
            pattern: suggestion.OptimizedPattern,
            categoryId: rule.CategoryId,
            caseSensitive: rule.CaseSensitive);

        return rule;
    }

    private async Task<CategorizationRule> AcceptUnusedRuleSuggestionAsync(
        RuleSuggestion suggestion,
        CancellationToken ct)
    {
        if (suggestion.TargetRuleId is null)
        {
            throw new DomainException("Suggestion is missing required target rule ID");
        }

        var rule = await _ruleRepository.GetByIdAsync(suggestion.TargetRuleId.Value, ct)
            ?? throw new DomainException($"Target rule {suggestion.TargetRuleId} not found");

        rule.Deactivate();

        return rule;
    }
}
