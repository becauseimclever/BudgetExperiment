// <copyright file="SuggestionMetricsService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;
using BudgetExperiment.Domain.Repositories;

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Computes suggestion quality metrics from rule and category suggestion repositories.
/// </summary>
public sealed class SuggestionMetricsService : ISuggestionMetricsService
{
    private readonly IRuleSuggestionRepository _ruleSuggestionRepo;
    private readonly ICategorySuggestionRepository _categorySuggestionRepo;

    /// <summary>
    /// Initializes a new instance of the <see cref="SuggestionMetricsService"/> class.
    /// </summary>
    /// <param name="ruleSuggestionRepo">The rule suggestion repository.</param>
    /// <param name="categorySuggestionRepo">The category suggestion repository.</param>
    public SuggestionMetricsService(
        IRuleSuggestionRepository ruleSuggestionRepo,
        ICategorySuggestionRepository categorySuggestionRepo)
    {
        _ruleSuggestionRepo = ruleSuggestionRepo;
        _categorySuggestionRepo = categorySuggestionRepo;
    }

    /// <inheritdoc />
    public async Task<SuggestionMetricsDto> GetMetricsAsync(CancellationToken ct = default)
    {
        var ruleReviewed = await _ruleSuggestionRepo.GetReviewedCountsByTypeAsync(ct);
        var rulePending = await _ruleSuggestionRepo.GetPendingCountsByTypeAsync(ct);
        var ruleConfidence = await _ruleSuggestionRepo.GetAverageConfidenceByStatusAsync(ct);
        var categoryCounts = await _categorySuggestionRepo.GetCountsByStatusAsync(ct);
        var categoryConfidence = await _categorySuggestionRepo.GetAverageConfidenceByStatusAsync(ct);

        // Build per-type metrics for rule suggestion types
        var byType = new List<SuggestionTypeMetricsDto>();

        foreach (var type in new[] { SuggestionType.NewRule, SuggestionType.PatternOptimization, SuggestionType.RuleConflict, SuggestionType.RuleConsolidation, SuggestionType.UnusedRule })
        {
            ruleReviewed.TryGetValue((type, SuggestionStatus.Accepted), out var accepted);
            ruleReviewed.TryGetValue((type, SuggestionStatus.Dismissed), out var dismissed);
            rulePending.TryGetValue(type, out var pending);

            if (accepted == 0 && dismissed == 0 && pending == 0)
            {
                continue;
            }

            var reviewed = accepted + dismissed;

            byType.Add(new SuggestionTypeMetricsDto
            {
                Type = type.ToString(),
                Accepted = accepted,
                Dismissed = dismissed,
                Pending = pending,
                AcceptanceRate = reviewed > 0 ? (decimal)accepted / reviewed : null,
            });
        }

        // Add category suggestion type
        categoryCounts.TryGetValue(SuggestionStatus.Accepted, out var catAccepted);
        categoryCounts.TryGetValue(SuggestionStatus.Dismissed, out var catDismissed);
        categoryCounts.TryGetValue(SuggestionStatus.Pending, out var catPending);

        if (catAccepted > 0 || catDismissed > 0 || catPending > 0)
        {
            var catReviewed = catAccepted + catDismissed;
            byType.Add(new SuggestionTypeMetricsDto
            {
                Type = "Category",
                Accepted = catAccepted,
                Dismissed = catDismissed,
                Pending = catPending,
                AcceptanceRate = catReviewed > 0 ? (decimal)catAccepted / catReviewed : null,
            });
        }

        // Compute overall totals
        var totalAccepted = byType.Sum(t => t.Accepted);
        var totalDismissed = byType.Sum(t => t.Dismissed);
        var totalPending = byType.Sum(t => t.Pending);
        var totalReviewed = totalAccepted + totalDismissed;

        // Compute weighted average confidence across both rule and category suggestions
        var avgAcceptedConfidence = ComputeWeightedAverage(
            ruleConfidence.AcceptedAvgConfidence,
            byType.Where(t => t.Type != "Category").Sum(t => t.Accepted),
            categoryConfidence.AcceptedAvgConfidence,
            catAccepted);

        var avgDismissedConfidence = ComputeWeightedAverage(
            ruleConfidence.DismissedAvgConfidence,
            byType.Where(t => t.Type != "Category").Sum(t => t.Dismissed),
            categoryConfidence.DismissedAvgConfidence,
            catDismissed);

        return new SuggestionMetricsDto
        {
            TotalGenerated = totalAccepted + totalDismissed + totalPending,
            Accepted = totalAccepted,
            Dismissed = totalDismissed,
            Pending = totalPending,
            AcceptanceRate = totalReviewed > 0 ? (decimal)totalAccepted / totalReviewed : null,
            AverageAcceptedConfidence = avgAcceptedConfidence,
            AverageDismissedConfidence = avgDismissedConfidence,
            ByType = byType,
        };
    }

    private static decimal? ComputeWeightedAverage(
        decimal? ruleAvg,
        int ruleCount,
        decimal? categoryAvg,
        int categoryCount)
    {
        if (ruleCount == 0 && categoryCount == 0)
        {
            return null;
        }

        if (ruleCount > 0 && categoryCount == 0)
        {
            return ruleAvg;
        }

        if (ruleCount == 0 && categoryCount > 0)
        {
            return categoryAvg;
        }

        // Both have values — weighted average
        var totalCount = ruleCount + categoryCount;
        return (((ruleAvg ?? 0) * ruleCount) + ((categoryAvg ?? 0) * categoryCount)) / totalCount;
    }
}
