// <copyright file="ConsolidationUndoEndpointTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Headers;

using Microsoft.Extensions.DependencyInjection;

using Moq;
using Shouldly;

namespace BudgetExperiment.Api.Tests.Categorization;

/// <summary>
/// Integration tests for the consolidation undo endpoint.
/// Feature 116 Slice 6: Undo Consolidation workflow.
///
/// RED PHASE: The endpoint does not yet exist.
/// Expected failures: 404 Not Found for all three tests (route missing).
/// Lucius must implement:
///   POST /api/v1/categorizationrules/consolidation/{id}/undo
/// in <see cref="Controllers.CategorizationRulesController"/> to make these green.
/// </summary>
[Collection("ApiDb")]
public sealed class ConsolidationUndoEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsolidationUndoEndpointTests"/> class.
    /// </summary>
    /// <param name="factory">The shared test factory.</param>
    public ConsolidationUndoEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // -------------------------------------------------------------------------
    // 1. Undo valid accepted consolidation → 204 No Content
    // -------------------------------------------------------------------------

    /// <summary>
    /// POST undo with a valid accepted suggestion ID when the consolidation service
    /// successfully reverses the consolidation must return 204 No Content.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task PostUndo_ValidId_Returns204()
    {
        // Arrange
        var suggestionId = Guid.NewGuid();

        var mockConsolidationService = new Mock<IRuleConsolidationService>();
        mockConsolidationService
            .Setup(s => s.UndoConsolidationAsync(suggestionId, It.IsAny<CancellationToken>()))
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
            $"/api/v1/categorizationrules/consolidation/{suggestionId}/undo",
            content: null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        mockConsolidationService.Verify(
            s => s.UndoConsolidationAsync(suggestionId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // -------------------------------------------------------------------------
    // 2. Undo non-existent suggestion → 404 Not Found
    // -------------------------------------------------------------------------

    /// <summary>
    /// POST undo when the consolidation service throws a
    /// <see cref="DomainException"/> with <see cref="DomainExceptionType.NotFound"/>
    /// must return 404 Not Found.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task PostUndo_NotFound_Returns404()
    {
        // Arrange
        var missingId = Guid.NewGuid();

        var mockConsolidationService = new Mock<IRuleConsolidationService>();
        mockConsolidationService
            .Setup(s => s.UndoConsolidationAsync(missingId, It.IsAny<CancellationToken>()))
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
            $"/api/v1/categorizationrules/consolidation/{missingId}/undo",
            content: null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // 3. Undo suggestion not in accepted state → 422 Unprocessable Entity
    // -------------------------------------------------------------------------

    /// <summary>
    /// POST undo when the consolidation service throws a
    /// <see cref="DomainException"/> with <see cref="DomainExceptionType.InvalidState"/>
    /// because the suggestion has not been accepted must return 422 Unprocessable Entity.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task PostUndo_NotAccepted_Returns422()
    {
        // Arrange
        var pendingSuggestionId = Guid.NewGuid();

        var mockConsolidationService = new Mock<IRuleConsolidationService>();
        mockConsolidationService
            .Setup(s => s.UndoConsolidationAsync(pendingSuggestionId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainException(
                $"Suggestion {pendingSuggestionId} has not been accepted and cannot be undone.",
                DomainExceptionType.InvalidState));

        var client = _factory
            .WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                    services.AddScoped(_ => mockConsolidationService.Object)))
            .CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("TestAuto", "authenticated");

        // Act
        var response = await client.PostAsync(
            $"/api/v1/categorizationrules/consolidation/{pendingSuggestionId}/undo",
            content: null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }
}
