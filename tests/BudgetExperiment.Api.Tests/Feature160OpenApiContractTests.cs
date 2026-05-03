// <copyright file="Feature160OpenApiContractTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Linq;
using System.Net;
using System.Text.Json;

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// OpenAPI contract tests for Feature 160 AI backend surface.
/// </summary>
[Collection("ApiDb")]
public sealed class Feature160OpenApiContractTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="Feature160OpenApiContractTests"/> class.
    /// </summary>
    /// <param name="factory">The shared API factory.</param>
    public Feature160OpenApiContractTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateApiClient();
    }

    /// <summary>
    /// OpenAPI must expose backend type fields and backend enum values in AI DTO schemas.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OpenApi_Includes_AiBackendType_Fields_And_EnumValues()
    {
        using var document = await GetOpenApiDocumentAsync();
        var paths = document.RootElement.GetProperty("paths");
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");

        var statusBackendTypeProperty = GetPropertySchemaFromOperationResponse(
            paths,
            "/api/v1/ai/status",
            "get",
            "200",
            "backendType",
            schemas);

        var settingsBackendTypeProperty = GetPropertySchemaFromOperationResponse(
            paths,
            "/api/v1/ai/settings",
            "get",
            "200",
            "backendType",
            schemas);

        Assert.Equal(JsonValueKind.Object, statusBackendTypeProperty.ValueKind);
        Assert.Equal(JsonValueKind.Object, settingsBackendTypeProperty.ValueKind);

        var statusEnumValues = GetEnumValuesForProperty(statusBackendTypeProperty, schemas);
        var settingsEnumValues = GetEnumValuesForProperty(settingsBackendTypeProperty, schemas);

        Assert.Contains("Ollama", statusEnumValues);
        Assert.Contains("LlamaCpp", statusEnumValues);
        Assert.Contains("Ollama", settingsEnumValues);
        Assert.Contains("LlamaCpp", settingsEnumValues);
    }

    /// <summary>
    /// OpenAPI must advertise all Feature 160 AI endpoints.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OpenApi_Includes_Ai_Endpoint_Paths_And_Methods()
    {
        using var document = await GetOpenApiDocumentAsync();
        var paths = document.RootElement.GetProperty("paths");

        AssertPathContainsMethod(paths, "/api/v1/ai/status", "get");
        AssertPathContainsMethod(paths, "/api/v1/ai/settings", "get");
        AssertPathContainsMethod(paths, "/api/v1/ai/settings", "put");
        AssertPathContainsMethod(paths, "/api/v1/ai/models", "get");
        AssertPathContainsMethod(paths, "/api/v1/ai/analyze", "post");
    }

    private static void AssertPathContainsMethod(JsonElement paths, string expectedV1Path, string method)
    {
        var pathItem = FindPathItemByExpectedV1Path(paths, expectedV1Path);
        Assert.True(pathItem.TryGetProperty(method, out _), $"Method '{method.ToUpperInvariant()}' was not found for path '{expectedV1Path}'.");
    }

    private static JsonElement FindPathItemByExpectedV1Path(JsonElement paths, string expectedV1Path)
    {
        var normalizedExpectedPath = expectedV1Path.ToLowerInvariant();

        foreach (var path in paths.EnumerateObject())
        {
            var normalized = path.Name
                .Replace("v{version:apiVersion}", "v1", StringComparison.Ordinal)
                .Replace("v{version}", "v1", StringComparison.Ordinal)
                .ToLowerInvariant();

            if (string.Equals(normalized, normalizedExpectedPath, StringComparison.Ordinal))
            {
                return path.Value;
            }
        }

        throw new InvalidOperationException($"Path '{expectedV1Path}' was not found in OpenAPI paths.");
    }

    private static JsonElement GetPropertySchemaFromOperationResponse(
        JsonElement paths,
        string expectedV1Path,
        string method,
        string expectedStatusCode,
        string propertyName,
        JsonElement schemas)
    {
        var pathItem = FindPathItemByExpectedV1Path(paths, expectedV1Path);
        if (!pathItem.TryGetProperty(method, out var operation))
        {
            throw new InvalidOperationException($"Method '{method.ToUpperInvariant()}' was not found for path '{expectedV1Path}'.");
        }

        if (!operation.TryGetProperty("responses", out var responses) ||
            !responses.TryGetProperty(expectedStatusCode, out var response))
        {
            throw new InvalidOperationException($"Response '{expectedStatusCode}' was not found for '{method.ToUpperInvariant()} {expectedV1Path}'.");
        }

        if (!response.TryGetProperty("content", out var content) ||
            !content.TryGetProperty("application/json", out var mediaType) ||
            !mediaType.TryGetProperty("schema", out var responseSchema))
        {
            throw new InvalidOperationException($"JSON response schema was not found for '{method.ToUpperInvariant()} {expectedV1Path}' response {expectedStatusCode}.");
        }

        if (TryFindPropertySchema(responseSchema, propertyName, schemas, out var propertySchema))
        {
            return propertySchema;
        }

        throw new InvalidOperationException(
            $"Property '{propertyName}' was not found in response schema for '{method.ToUpperInvariant()} {expectedV1Path}' response {expectedStatusCode}.");
    }

    private static bool TryFindPropertySchema(
        JsonElement schema,
        string propertyName,
        JsonElement schemas,
        out JsonElement propertySchema)
    {
        if (schema.TryGetProperty("properties", out var properties) &&
            properties.TryGetProperty(propertyName, out propertySchema))
        {
            return true;
        }

        if (schema.TryGetProperty("$ref", out var directRef))
        {
            var referencedSchema = GetSchemaFromReference(schemas, directRef.GetString());
            return TryFindPropertySchema(referencedSchema, propertyName, schemas, out propertySchema);
        }

        if (TryFindInComposition(schema, "allOf", propertyName, schemas, out propertySchema))
        {
            return true;
        }

        if (TryFindInComposition(schema, "oneOf", propertyName, schemas, out propertySchema))
        {
            return true;
        }

        if (TryFindInComposition(schema, "anyOf", propertyName, schemas, out propertySchema))
        {
            return true;
        }

        propertySchema = default;
        return false;
    }

    private static bool TryFindInComposition(
        JsonElement schema,
        string compositionKeyword,
        string propertyName,
        JsonElement schemas,
        out JsonElement propertySchema)
    {
        if (schema.TryGetProperty(compositionKeyword, out var compositionNode) &&
            compositionNode.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in compositionNode.EnumerateArray())
            {
                if (TryFindPropertySchema(item, propertyName, schemas, out propertySchema))
                {
                    return true;
                }
            }
        }

        propertySchema = default;
        return false;
    }

    private static HashSet<string> GetEnumValuesForProperty(JsonElement propertySchema, JsonElement schemas)
    {
        if (propertySchema.TryGetProperty("enum", out var enumNode))
        {
            return ToStringSet(enumNode);
        }

        if (propertySchema.TryGetProperty("$ref", out var directRef))
        {
            return GetEnumValuesFromReference(schemas, directRef.GetString());
        }

        if (propertySchema.TryGetProperty("allOf", out var allOfNode) && allOfNode.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in allOfNode.EnumerateArray().Where(static candidate =>
                         candidate.ValueKind == JsonValueKind.Object))
            {
                if (item.TryGetProperty("$ref", out var allOfRef))
                {
                    return GetEnumValuesFromReference(schemas, allOfRef.GetString());
                }

                if (item.TryGetProperty("enum", out var allOfEnumNode))
                {
                    return ToStringSet(allOfEnumNode);
                }
            }
        }

        throw new InvalidOperationException("Could not resolve enum values for backendType in OpenAPI schema.");
    }

    private static HashSet<string> GetEnumValuesFromReference(JsonElement schemas, string? reference)
    {
        var referencedSchema = GetSchemaFromReference(schemas, reference);

        if (!referencedSchema.TryGetProperty("enum", out var enumNode))
        {
            var schemaKey = GetSchemaKeyFromReference(reference);
            throw new InvalidOperationException($"Referenced schema '{schemaKey}' does not contain enum values.");
        }

        return ToStringSet(enumNode);
    }

    private static JsonElement GetSchemaFromReference(JsonElement schemas, string? reference)
    {
        var schemaKey = GetSchemaKeyFromReference(reference);
        if (!schemas.TryGetProperty(schemaKey, out var referencedSchema))
        {
            throw new InvalidOperationException($"Referenced schema '{schemaKey}' was not found in OpenAPI components.");
        }

        return referencedSchema;
    }

    private static string GetSchemaKeyFromReference(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            throw new InvalidOperationException("OpenAPI schema reference is null or empty.");
        }

        return reference.Split('/', StringSplitOptions.RemoveEmptyEntries).Last();
    }

    private static HashSet<string> ToStringSet(JsonElement enumNode)
    {
        return enumNode.EnumerateArray()
            .Select(enumItem => enumItem.GetString())
            .Where(static stringValue => !string.IsNullOrWhiteSpace(stringValue))
            .Select(static stringValue => stringValue!)
            .ToHashSet(StringComparer.Ordinal);
    }

    private async Task<JsonDocument> GetOpenApiDocumentAsync()
    {
        var response = await _client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json);
    }
}
