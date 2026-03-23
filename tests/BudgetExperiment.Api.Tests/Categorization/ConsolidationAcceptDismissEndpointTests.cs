// <copyright file="ConsolidationAcceptDismissEndpointTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using BudgetExperiment.Contracts.Dtos;

using Microsoft.Extensions.DependencyInjection;

using Moq;
using Shouldly;

namespace BudgetExperiment.Api.Tests.Categorization;

/// <summary>
/// Integration tests for the consolidation accept and dismiss endpoints.
/// Feature 116 Slice 5: Accept and Dismiss workflow.
///
/// RED PHASE: The endpoints do not yet exist.
/// Expected failures: 404 Not Found for all three tests.
/// Lucius must implement:
///   POST /api/v1/categorizationrules/consolidation/{id}/accept
///   POST /api/v1/categorizationrules/consolidation/{id}/dismiss
/// in <see cref="Controllers.CategorizationRulesController"/> to make these green.
/// </summary>
[Collection("ApiDb")]
public sealed class ConsolidationAcceptDismissEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly Guid GroceryCategoryId = Guid.NewGuid();

    private readonly CustomWebApplicationFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsolidationAcceptDismissEndpointTests"/> class.
    /// </summary>
    /// <param name="factory">The shared test factory.</param>
    public ConsolidationAcceptDismissEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // -------------------------------------------------------------------------
    // 1. Accept valid consolidation → 200 OK with merged rule DTO
    // -------------------------------------------------------------------------

    /// <summary>
    /// POST accept with a valid suggestion ID when the consolidation service successfully
    /// creates the merged rule must return 200 OK with the merged rule DTO.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task PostAccept_ValidId_Returns200()
    {
        // Arrange
        var suggestionId = Guid.NewGuid();

        var mergedRule = CategorizationRule.Create(
            "Merged WALMART",
            RuleMatchType.Regex,
            "WALMART|WALMART GROCERY",
            GroceryCategoryId,
            priority: 10);

        var mockConsolidationService = new Mock<IRuleConsolidationService>();
        mockConsolidationService
            .Setup(s => s.AcceptConsolidationAsync(suggestionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mergedRule);

        var mockRuleService = new Mock<ICategorizationRuleService>();
        mockRuleService
            .Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CategorizationRuleDto
            {
                Id = mergedRule.Id,
                Name = mergedRule.Name,
                Pattern = mergedRule.Pattern,
                MatchType = mergedRule.MatchType.ToString(),
                CategoryId = mergedRule.CategoryId,
                Priority = mergedRule.Priority,
                IsActive = mergedRule.IsActive,
            });

        var client = _factory
            .WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                {
                    services.AddScoped(_ => mockConsolidationService.Object);
                    services.AddScoped(_ => mockRuleService.Object);
                }))
            .CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("TestAuto", "authenticated");

        // Act
        var response = await client.PostAsync(
            $"/api/v1/categorizationrules/consolidation/{suggestionId}/accept",
            content: null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var dto = await response.Content.ReadFromJsonAsync<CategorizationRuleDto>();
        dto.ShouldNotBeNull();
    }

    // -------------------------------------------------------------------------
    // 2. Dismiss valid consolidation → 204 No Content
    // -------------------------------------------------------------------------

    /// <summary>
    /// POST dismiss with a valid suggestion ID must call the consolidation service and
    /// return 204 No Content.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task PostDismiss_ValidId_Returns204()
    {
        // Arrange
        var suggestionId = Guid.NewGuid();

        var mockConsolidationService = new Mock<IRuleConsolidationService>();
        mockConsolidationService
            .Setup(s => s.DismissConsolidationAsync(suggestionId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var client = _factory
            .WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                    services.AddScoped(_ => mockConsolidationService.Object)))
            .CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("TestAuto", "authenticated");

        // Act
        var response = await client.PostAsync(
            $"/api/v1/categorizationrules/consolidation/{suggestionId}/dismiss",
            content: null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        mockConsolidationService.Verify(
            s => s.DismissConsolidationAsync(suggestionId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // -------------------------------------------------------------------------
    // 3. Accept non-existent suggestion → 404 Not Found
    // -------------------------------------------------------------------------

    /// <summary>
    /// POST accept when the consolidation service throws a
    /// <see cref="DomainException"/> with <see cref="DomainExceptionType.NotFound"/>
    /// must return 404 Not Found.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task PostAccept_NotFound_Returns404()
    {
        // Arrange
        var missingId = Guid.NewGuid();

        var mockConsolidationService = new Mock<IRuleConsolidationService>();
        mockConsolidationService
            .Setup(s => s.AcceptConsolidationAsync(missingId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainException($"Suggestion {missingId} not found", DomainExceptionType.NotFound));

        var client = _factory
            .WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                    services.AddScoped(_ => mockConsolidationService.Object)))
            .CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("TestAuto", "authenticated");

        // Act
        var response = await client.PostAsync(
            $"/api/v1/categorizationrules/consolidation/{missingId}/accept",
            content: null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
