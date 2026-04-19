// <copyright file="Feature161Phase2ApiContractTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Infrastructure.Persistence;
using BudgetExperiment.Infrastructure.Seeding;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Regression tests for Feature 161 Phase 2 API and contract cleanup.
/// </summary>
[Collection("ApiDb")]
public sealed class Feature161Phase2ApiContractTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="Feature161Phase2ApiContractTests"/> class.
    /// </summary>
    /// <param name="factory">The shared API factory.</param>
    public Feature161Phase2ApiContractTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
    }

    /// <summary>
    /// Phase 2 contracts must not expose scope-bearing DTO properties.
    /// </summary>
    /// <param name="dtoType">The DTO type to inspect.</param>
    /// <param name="propertyName">The removed property name.</param>
    [Theory]
    [InlineData(typeof(AccountCreateDto), "Scope")]
    [InlineData(typeof(AccountDto), "Scope")]
    [InlineData(typeof(CustomReportLayoutCreateDto), "Scope")]
    [InlineData(typeof(CustomReportLayoutDto), "Scope")]
    [InlineData(typeof(UserSettingsDto), "DefaultScope")]
    [InlineData(typeof(UserSettingsUpdateDto), "DefaultScope")]
    public void Contracts_DoNotExpose_ScopeProperties(Type dtoType, string propertyName)
    {
        Assert.Null(dtoType.GetProperty(propertyName));
    }

    /// <summary>
    /// The dedicated scope DTO should be gone once the scope endpoint is removed.
    /// </summary>
    [Fact]
    public void Contracts_DoNotExpose_ScopeDto()
    {
        var scopeDtoType = typeof(AccountDto).Assembly.GetType("BudgetExperiment.Contracts.Dtos.ScopeDto");

        Assert.Null(scopeDtoType);
    }

    /// <summary>
    /// The API should no longer ship scope middleware once Phase 2 is complete.
    /// </summary>
    [Fact]
    public void Api_DoesNotShip_BudgetScopeMiddleware()
    {
        var middlewareType = typeof(Program).Assembly.GetType("BudgetExperiment.Api.Middleware.BudgetScopeMiddleware");

        Assert.Null(middlewareType);
    }

    /// <summary>
    /// The API user context should stop exposing current scope members in Phase 2.
    /// </summary>
    [Fact]
    public void Api_UserContext_DoesNotExpose_ScopeMembers()
    {
        var userContextType = typeof(Program).Assembly.GetType("BudgetExperiment.Api.UserContext");

        Assert.NotNull(userContextType);
        Assert.Null(userContextType.GetProperty("CurrentScope"));
        Assert.Null(userContextType.GetMethod("SetScope"));
    }

    /// <summary>
    /// Account endpoints must work without a scope header and omit scope in the JSON contract.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Accounts_WithoutScopeHeader_OmitScopeField()
    {
        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/accounts",
            new AccountCreateDto
            {
                Name = "Phase 2 Account",
                Type = "Checking",
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        using var createPayload = await ReadJsonAsync(createResponse);
        Assert.False(createPayload.RootElement.TryGetProperty("scope", out _));

        var accountId = createPayload.RootElement.GetProperty("id").GetGuid();
        var getResponse = await _client.GetAsync($"/api/v1/accounts/{accountId}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        using var getPayload = await ReadJsonAsync(getResponse);
        Assert.False(getPayload.RootElement.TryGetProperty("scope", out _));
    }

    /// <summary>
    /// Custom report endpoints must omit scope in the JSON contract once Phase 2 lands.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CustomReports_WithoutScopeHeader_OmitScopeField()
    {
        await EnsureCustomReportsEnabledAsync(_factory);

        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/custom-reports",
            new CustomReportLayoutCreateDto
            {
                Name = "Phase 2 Report",
                LayoutJson = "{\"type\":\"bar\"}",
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        using var createPayload = await ReadJsonAsync(createResponse);
        Assert.False(createPayload.RootElement.TryGetProperty("scope", out _));
    }

    /// <summary>
    /// The generated OpenAPI document must not advertise legacy scope routes, fields, or headers.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OpenApi_DoesNotAdvertise_ScopeArtifacts()
    {
        var response = await _client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");

        Assert.DoesNotContain("/user/scope", json, StringComparison.Ordinal);
        Assert.DoesNotContain("X-Budget-Scope", json, StringComparison.Ordinal);
        Assert.False(GetSchemaProperties(schemas, nameof(AccountCreateDto)).TryGetProperty("scope", out _));
        Assert.False(GetSchemaProperties(schemas, nameof(AccountDto)).TryGetProperty("scope", out _));
        Assert.False(GetSchemaProperties(schemas, nameof(CustomReportLayoutCreateDto)).TryGetProperty("scope", out _));
        Assert.False(GetSchemaProperties(schemas, nameof(CustomReportLayoutDto)).TryGetProperty("scope", out _));
        Assert.False(GetSchemaProperties(schemas, nameof(UserSettingsDto)).TryGetProperty("defaultScope", out _));
        Assert.False(GetSchemaProperties(schemas, nameof(UserSettingsUpdateDto)).TryGetProperty("defaultScope", out _));
    }

    private static JsonElement GetSchemaProperties(JsonElement schemas, string schemaName)
    {
        foreach (var schema in schemas.EnumerateObject())
        {
            if (schema.Name.EndsWith(schemaName, StringComparison.Ordinal))
            {
                return schema.Value.GetProperty("properties");
            }

            if (schema.Value.TryGetProperty("title", out var title) &&
                string.Equals(title.GetString(), schemaName, StringComparison.Ordinal))
            {
                return schema.Value.GetProperty("properties");
            }
        }

        throw new InvalidOperationException($"Schema '{schemaName}' was not found in the OpenAPI document.");
    }

    private static async Task EnsureCustomReportsEnabledAsync(CustomWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BudgetDbContext>();
        await FeatureFlagSeeder.SeedAsync(db);
        await db.Database.ExecuteSqlRawAsync(
            """UPDATE "FeatureFlags" SET "IsEnabled" = true WHERE "Name" = 'Reports:CustomReportBuilder'""");
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json);
    }
}
