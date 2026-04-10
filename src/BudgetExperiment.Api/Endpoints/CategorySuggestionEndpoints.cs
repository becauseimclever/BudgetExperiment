// <copyright file="CategorySuggestionEndpoints.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Api.Endpoints;

/// <summary>
/// Minimal API endpoint group for category suggestions.
/// </summary>
public static class CategorySuggestionEndpoints
{
    /// <summary>
    /// Maps all category suggestion endpoints onto the given route builder.
    /// </summary>
    /// <param name="app">The endpoint route builder to register endpoints on.</param>
    /// <returns>The same <paramref name="app"/> instance, for chaining.</returns>
    public static IEndpointRouteBuilder MapCategorySuggestionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1/CategorySuggestions")
            .RequireAuthorization()
            .WithTags("CategorySuggestions");

        group.MapPost(
            "analyze",
            async (ICategorySuggestionService suggestionService, CancellationToken cancellationToken) =>
            {
                var suggestions = await suggestionService.AnalyzeTransactionsAsync(cancellationToken);
                var dtos = suggestions.Select(MapToDto).ToList();
                return Results.Ok(dtos);
            })
            .Produces<IReadOnlyList<CategorySuggestionDto>>(StatusCodes.Status200OK);

        group.MapGet(
            string.Empty,
            async (ICategorySuggestionService suggestionService, CancellationToken cancellationToken) =>
            {
                var suggestions = await suggestionService.GetPendingSuggestionsAsync(cancellationToken);
                var dtos = suggestions.Select(MapToDto).ToList();
                return Results.Ok(dtos);
            })
            .Produces<IReadOnlyList<CategorySuggestionDto>>(StatusCodes.Status200OK);

        group.MapGet(
            "dismissed",
            async (
                ICategorySuggestionService suggestionService,
                int skip,
                int take,
                CancellationToken cancellationToken) =>
            {
                var suggestions = await suggestionService.GetDismissedSuggestionsAsync(skip, take, cancellationToken);
                var dtos = suggestions.Select(MapToDto).ToList();
                return Results.Ok(dtos);
            })
            .Produces<IReadOnlyList<CategorySuggestionDto>>(StatusCodes.Status200OK);

        group.MapGet(
            "{id:guid}",
            async (Guid id, ICategorySuggestionService suggestionService, CancellationToken cancellationToken) =>
            {
                var suggestion = await suggestionService.GetSuggestionAsync(id, cancellationToken);
                if (suggestion is null)
                {
                    return Results.NotFound();
                }

                return Results.Ok(MapToDto(suggestion));
            })
            .Produces<CategorySuggestionDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost(
            "{id:guid}/accept",
            async (
                Guid id,
                AcceptCategorySuggestionRequest? request,
                ICategorySuggestionService suggestionService,
                CancellationToken cancellationToken) =>
            {
                var result = await suggestionService.AcceptSuggestionAsync(
                    id,
                    request?.CustomName,
                    request?.CustomIcon,
                    request?.CustomColor,
                    cancellationToken);

                var dto = new AcceptCategorySuggestionResultDto
                {
                    SuggestionId = result.SuggestionId,
                    Success = result.Success,
                    CategoryId = result.CreatedCategoryId,
                    CategoryName = result.CategoryName,
                    ErrorMessage = result.ErrorMessage,
                };

                if (!result.Success && result.ErrorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return Results.NotFound(dto);
                }

                if (!result.Success)
                {
                    return Results.BadRequest(dto);
                }

                return Results.Ok(dto);
            })
            .Produces<AcceptCategorySuggestionResultDto>(StatusCodes.Status200OK)
            .Produces<AcceptCategorySuggestionResultDto>(StatusCodes.Status400BadRequest)
            .Produces<AcceptCategorySuggestionResultDto>(StatusCodes.Status404NotFound);

        group.MapPost(
            "{id:guid}/dismiss",
            async (Guid id, ICategorySuggestionService suggestionService, CancellationToken cancellationToken) =>
            {
                var success = await suggestionService.DismissSuggestionAsync(id, cancellationToken);
                if (!success)
                {
                    return Results.NotFound();
                }

                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost(
            "{id:guid}/restore",
            async (Guid id, ICategorySuggestionService suggestionService, CancellationToken cancellationToken) =>
            {
                var success = await suggestionService.RestoreSuggestionAsync(id, cancellationToken);
                if (!success)
                {
                    return Results.NotFound();
                }

                var suggestion = await suggestionService.GetSuggestionAsync(id, cancellationToken);
                return Results.Ok(MapToDto(suggestion!));
            })
            .Produces<CategorySuggestionDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete(
            "dismissed-patterns",
            async (ICategorySuggestionService suggestionService, CancellationToken cancellationToken) =>
            {
                var clearedCount = await suggestionService.ClearDismissedPatternsAsync(cancellationToken);
                return Results.Ok(new { clearedCount });
            })
            .Produces(StatusCodes.Status200OK);

        group.MapPost(
            "bulk-accept",
            async (
                BulkAcceptCategorySuggestionsRequest request,
                ICategorySuggestionService suggestionService,
                CancellationToken cancellationToken) =>
            {
                if (request.SuggestionIds == null || request.SuggestionIds.Count == 0)
                {
                    return Results.BadRequest(new { message = "At least one suggestion ID is required." });
                }

                var results = await suggestionService.AcceptSuggestionsAsync(request.SuggestionIds, cancellationToken);

                var dtos = results.Select(r => new AcceptCategorySuggestionResultDto
                {
                    SuggestionId = r.SuggestionId,
                    Success = r.Success,
                    CategoryId = r.CreatedCategoryId,
                    CategoryName = r.CategoryName,
                    ErrorMessage = r.ErrorMessage,
                }).ToList();

                return Results.Ok(dtos);
            })
            .Produces<IReadOnlyList<AcceptCategorySuggestionResultDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapGet(
            "{id:guid}/preview-rules",
            async (Guid id, ICategorySuggestionService suggestionService, CancellationToken cancellationToken) =>
            {
                var suggestion = await suggestionService.GetSuggestionAsync(id, cancellationToken);
                if (suggestion is null)
                {
                    return Results.NotFound();
                }

                var rules = await suggestionService.GetSuggestedRulesAsync(id, cancellationToken);
                var dtos = rules.Select(r => new SuggestedCategoryRuleDto
                {
                    Pattern = r.Pattern,
                    MatchType = r.MatchType.ToString(),
                    MatchingTransactionCount = r.MatchingTransactionCount,
                    SampleDescriptions = r.SampleDescriptions,
                }).ToList();

                return Results.Ok(dtos);
            })
            .Produces<IReadOnlyList<SuggestedCategoryRuleDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost(
            "{id:guid}/create-rules",
            async (
                Guid id,
                CreateRulesFromSuggestionRequest request,
                ICategorySuggestionService suggestionService,
                ICategorizationRuleService ruleService,
                CancellationToken cancellationToken) =>
            {
                var suggestion = await suggestionService.GetSuggestionAsync(id, cancellationToken);
                if (suggestion is null)
                {
                    return Results.NotFound(new CreateRulesFromSuggestionResult
                    {
                        Success = false,
                        ErrorMessage = "Suggestion not found.",
                    });
                }

                var patterns = request.Patterns?.Count > 0
                    ? request.Patterns
                    : suggestion.MerchantPatterns;

                var allConflicts = new List<CategorizationRuleDto>();
                foreach (var pattern in patterns)
                {
                    var conflicts = await ruleService.CheckConflictsAsync(pattern, "Contains", null, cancellationToken);
                    allConflicts.AddRange(conflicts);
                }

                allConflicts = allConflicts.GroupBy(c => c.Id).Select(g => g.First()).ToList();

                var createdRules = await ruleService.CreateBulkFromPatternsAsync(
                    request.CategoryId,
                    patterns,
                    cancellationToken);

                return Results.Ok(new CreateRulesFromSuggestionResult
                {
                    Success = true,
                    CreatedRules = createdRules,
                    ConflictingRules = allConflicts.Count > 0 ? allConflicts : null,
                });
            })
            .Produces<CreateRulesFromSuggestionResult>(StatusCodes.Status200OK)
            .Produces<CreateRulesFromSuggestionResult>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest);

        return app;
    }

    private static CategorySuggestionDto MapToDto(CategorySuggestion suggestion)
    {
        return new CategorySuggestionDto
        {
            Id = suggestion.Id,
            SuggestedName = suggestion.SuggestedName,
            SuggestedIcon = suggestion.SuggestedIcon,
            SuggestedColor = suggestion.SuggestedColor,
            SuggestedType = suggestion.SuggestedType.ToString(),
            Confidence = suggestion.Confidence,
            MerchantPatterns = suggestion.MerchantPatterns,
            MatchingTransactionCount = suggestion.MatchingTransactionCount,
            Status = suggestion.Status.ToString(),
            Source = suggestion.Source.ToString(),
            Reasoning = suggestion.Reasoning,
            CreatedAtUtc = suggestion.CreatedAtUtc,
        };
    }
}
