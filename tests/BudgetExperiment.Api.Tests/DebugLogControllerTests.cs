// <copyright file="DebugLogControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Text.Json;

using BudgetExperiment.Api.Observability;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for <see cref="Controllers.DebugLogController"/>.
/// </summary>
[Collection("ApiDb")]
public sealed class DebugLogControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DebugLogControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The web application factory.</param>
    public DebugLogControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetDebugLog_WithValidTraceId_Returns200WithBundle()
    {
        // Arrange: create factory with buffer populated
        var traceId = "test-trace-123";
        using var client = CreateClientWithBuffer(traceId, out _);

        // Act
        var response = await client.GetAsync($"/api/v1/debug/logs/{traceId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var contentDisposition = response.Content.Headers.ContentDisposition;
        Assert.NotNull(contentDisposition);
        Assert.Equal("attachment", contentDisposition!.DispositionType);
        Assert.Contains("budget-debug-", contentDisposition.FileName);

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal(traceId, root.GetProperty("traceId").GetString());
        Assert.True(root.TryGetProperty("notice", out _));
        Assert.True(root.TryGetProperty("redactionSummary", out _));
        Assert.True(root.TryGetProperty("environment", out _));
        Assert.True(root.TryGetProperty("logEntries", out var entries));
        Assert.True(entries.GetArrayLength() > 0);
    }

    [Fact]
    public async Task GetDebugLog_PiiIsNotInResponse()
    {
        // Arrange: add entry with PII properties
        var traceId = "pii-trace";
        using var client = CreateClientWithBuffer(traceId, out var buffer, includePii: true);

        // Act
        var response = await client.GetAsync($"/api/v1/debug/logs/{traceId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("john@example.com", json);
        Assert.DoesNotContain("My Checking", json);
        Assert.DoesNotContain("192.168.1.100", json);
        Assert.DoesNotContain("42.50", json);
    }

    [Fact]
    public async Task GetDebugLog_UnknownTraceId_Returns404()
    {
        // Arrange
        using var client = CreateClientWithBuffer("known-trace", out _);

        // Act
        var response = await client.GetAsync("/api/v1/debug/logs/unknown-trace");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetDebugLog_BufferNotRegistered_Returns501()
    {
        // Arrange: create factory without buffer
        using var client = CreateClientWithoutBuffer();

        // Act
        var response = await client.GetAsync("/api/v1/debug/logs/any-trace");

        // Assert
        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
    }

    [Fact]
    public async Task GetDebugLog_DoesNotRequireAuthentication()
    {
        // Arrange
        var traceId = "anon-trace";
        var anonFactory = this._factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var buffer = new DebugLogBuffer();
                buffer.Add(CreateEntry(traceId, "Test entry"));
                services.AddSingleton<IDebugLogBuffer>(buffer);
            });
        });

        // Use CreateClient (no auth header) instead of CreateApiClient
        using var client = anonFactory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/v1/debug/logs/{traceId}");

        // Assert — should not be 401 or 403
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetDebugLog_ResponseIsIndentedJson()
    {
        // Arrange
        var traceId = "format-trace";
        using var client = CreateClientWithBuffer(traceId, out _);

        // Act
        var response = await client.GetAsync($"/api/v1/debug/logs/{traceId}");
        var json = await response.Content.ReadAsStringAsync();

        // Assert: indented JSON has newlines and spaces
        Assert.Contains("\n", json);
        Assert.Contains("  ", json);
    }

    private static DebugLogEntry CreateEntry(string traceId, string message)
    {
        return new DebugLogEntry
        {
            TimestampUtc = DateTime.UtcNow,
            Level = "Information",
            MessageTemplate = message,
            RenderedMessage = message,
            TraceId = traceId,
        };
    }

    private HttpClient CreateClientWithBuffer(string traceId, out DebugLogBuffer buffer, bool includePii = false)
    {
        var localBuffer = new DebugLogBuffer();
        localBuffer.Add(CreateEntry(traceId, "Test log entry"));

        if (includePii)
        {
            localBuffer.Add(new DebugLogEntry
            {
                TimestampUtc = DateTime.UtcNow,
                Level = "Warning",
                MessageTemplate = "User {UserName} accessed {AccountName}",
                RenderedMessage = "User john@example.com accessed My Checking",
                TraceId = traceId,
                Properties = new Dictionary<string, object?>
                {
                    ["UserName"] = "john@example.com",
                    ["AccountName"] = "My Checking",
                    ["IpAddress"] = "192.168.1.100",
                    ["Amount"] = 42.50m,
                    ["StatusCode"] = 200,
                },
            });
        }

        buffer = localBuffer;

        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove any existing buffer registration
                var desc = services.SingleOrDefault(d => d.ServiceType == typeof(IDebugLogBuffer));
                if (desc != null)
                {
                    services.Remove(desc);
                }

                services.AddSingleton<IDebugLogBuffer>(localBuffer);
            });
        });

        return factory.CreateClient();
    }

    private HttpClient CreateClientWithoutBuffer()
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove any existing buffer registration
                var desc = services.SingleOrDefault(d => d.ServiceType == typeof(IDebugLogBuffer));
                if (desc != null)
                {
                    services.Remove(desc);
                }
            });
        });

        return factory.CreateClient();
    }
}
