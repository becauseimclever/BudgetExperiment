// <copyright file="LogSanitizerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Api.Observability;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Unit tests for <see cref="LogSanitizer"/>.
/// </summary>
public sealed class LogSanitizerTests
{
    private readonly LogSanitizer _sanitizer = new();

    private readonly EnvironmentContext _environment = new()
    {
        AppVersion = "1.0.0",
        DotnetVersion = "10.0.0",
        OsDescription = "Windows 11",
        EnvironmentName = "Development",
        MachineName = "TestMachine",
    };

    [Fact]
    public void Sanitize_IncludesNoticeAndRedactionSummary()
    {
        var entries = new List<DebugLogEntry>
        {
            CreateEntry("trace-1", new Dictionary<string, object?> { ["UserName"] = "john@example.com" }),
        };

        var result = _sanitizer.Sanitize(entries, "trace-1", _environment);

        Assert.Contains("Budget Experiment", result.Notice);
        Assert.Contains("redacted", result.Notice);
        Assert.True(result.RedactionSummary.TotalFieldsRedacted > 0);
        Assert.NotEmpty(result.RedactionSummary.CategoriesRedacted);
    }

    [Fact]
    public void Sanitize_AllowedPropertiesPassThrough()
    {
        var entries = new List<DebugLogEntry>
        {
            CreateEntry("trace-1", new Dictionary<string, object?>
            {
                ["TraceId"] = "abc123",
                ["StatusCode"] = 200,
                ["RequestMethod"] = "GET",
                ["EndpointName"] = "GetAccounts",
                ["CategoryId"] = Guid.NewGuid().ToString(),
            }),
        };

        var result = _sanitizer.Sanitize(entries, "trace-1", _environment);

        var props = result.LogEntries[0].Properties;
        Assert.NotNull(props);
        Assert.Equal("abc123", props!["TraceId"]);
        Assert.Equal(200, props["StatusCode"]);
        Assert.Equal("GET", props["RequestMethod"]);
        Assert.Equal("GetAccounts", props["EndpointName"]);
    }

    [Fact]
    public void Sanitize_PiiPropertiesAreRedacted()
    {
        var entries = new List<DebugLogEntry>
        {
            CreateEntry("trace-1", new Dictionary<string, object?>
            {
                ["UserName"] = "john@example.com",
                ["Email"] = "john@example.com",
                ["AccountName"] = "My Checking",
                ["TransactionDescription"] = "Grocery Store",
                ["Amount"] = 42.50m,
                ["Balance"] = 1000.00m,
                ["IpAddress"] = "192.168.1.100",
                ["StatusCode"] = 500, // safe property should remain
            }),
        };

        var result = _sanitizer.Sanitize(entries, "trace-1", _environment);

        var props = result.LogEntries[0].Properties;
        Assert.NotNull(props);

        // PII should be stripped
        Assert.False(props!.ContainsKey("UserName"));
        Assert.False(props.ContainsKey("Email"));
        Assert.False(props.ContainsKey("AccountName"));
        Assert.False(props.ContainsKey("TransactionDescription"));
        Assert.False(props.ContainsKey("Amount"));
        Assert.False(props.ContainsKey("Balance"));
        Assert.False(props.ContainsKey("IpAddress"));

        // Safe property intact
        Assert.Equal(500, props["StatusCode"]);
    }

    [Fact]
    public void Sanitize_UnknownPropertiesAreStripped()
    {
        var entries = new List<DebugLogEntry>
        {
            CreateEntry("trace-1", new Dictionary<string, object?>
            {
                ["SomeRandomProperty"] = "should be stripped",
                ["AnotherUnknown"] = 99,
                ["StatusCode"] = 200,
            }),
        };

        var result = _sanitizer.Sanitize(entries, "trace-1", _environment);

        var props = result.LogEntries[0].Properties;
        Assert.NotNull(props);
        Assert.False(props!.ContainsKey("SomeRandomProperty"));
        Assert.False(props.ContainsKey("AnotherUnknown"));
        Assert.Equal(200, props["StatusCode"]);
    }

    [Fact]
    public void Sanitize_RedactionSummaryCountsAccurately()
    {
        var entries = new List<DebugLogEntry>
        {
            CreateEntry("trace-1", new Dictionary<string, object?>
            {
                ["UserName"] = "john",
                ["Email"] = "john@example.com",
                ["Amount"] = 42.50m,
            }),
        };

        var result = _sanitizer.Sanitize(entries, "trace-1", _environment);

        Assert.Equal(3, result.RedactionSummary.TotalFieldsRedacted);
        Assert.Contains("UserName", result.RedactionSummary.CategoriesRedacted);
        Assert.Contains("Email", result.RedactionSummary.CategoriesRedacted);
        Assert.Contains("Amount", result.RedactionSummary.CategoriesRedacted);
    }

    [Fact]
    public void Sanitize_ExceptionMessageWithQuotedStringsIsRedacted()
    {
        var entries = new List<DebugLogEntry>
        {
            CreateEntry("trace-1", exceptionType: "System.InvalidOperationException", exceptionMessage: "Account 'My Checking' not found"),
        };

        var result = _sanitizer.Sanitize(entries, "trace-1", _environment);

        Assert.NotNull(result.Exception);
        Assert.Equal("System.InvalidOperationException", result.Exception!.Type);
        Assert.Equal("Account '[REDACTED]' not found", result.Exception.Message);
    }

    [Fact]
    public void Sanitize_ExceptionMessagePreservesGUIDs()
    {
        var guid = "d3b07384-d113-4ec6-a7dc-5e8bdef7e9c1";
        var entries = new List<DebugLogEntry>
        {
            CreateEntry(
                "trace-1",
                exceptionType: "System.InvalidOperationException",
                exceptionMessage: $"Entity with ID '{guid}' not found"),
        };

        var result = _sanitizer.Sanitize(entries, "trace-1", _environment);

        Assert.NotNull(result.Exception);
        Assert.Contains(guid, result.Exception!.Message);
    }

    [Fact]
    public void Sanitize_ExceptionMessageWithNoQuotedStrings_Unchanged()
    {
        var entries = new List<DebugLogEntry>
        {
            CreateEntry("trace-1", exceptionType: "System.NullReferenceException", exceptionMessage: "Object reference not set to an instance of an object."),
        };

        var result = _sanitizer.Sanitize(entries, "trace-1", _environment);

        Assert.NotNull(result.Exception);
        Assert.Equal("Object reference not set to an instance of an object.", result.Exception!.Message);
    }

    [Fact]
    public void Sanitize_StackTraceIsPreserved()
    {
        var stackTrace = "   at BudgetExperiment.Application.Services.TransactionService.CreateAsync()\n   at System.Runtime.CompilerServices.AsyncMethodBuilderCore.Start()";
        var entries = new List<DebugLogEntry>
        {
            CreateEntry("trace-1", exceptionType: "System.Exception", exceptionMessage: "Error", exceptionStackTrace: stackTrace),
        };

        var result = _sanitizer.Sanitize(entries, "trace-1", _environment);

        Assert.NotNull(result.Exception);
        Assert.Equal(stackTrace, result.Exception!.StackTrace);
    }

    [Fact]
    public void Sanitize_EmptyEntries_ProducesValidBundle()
    {
        var result = _sanitizer.Sanitize(new List<DebugLogEntry>(), "trace-1", _environment);

        Assert.Empty(result.LogEntries);
        Assert.Equal(0, result.RedactionSummary.TotalFieldsRedacted);
        Assert.Empty(result.RedactionSummary.CategoriesRedacted);
        Assert.Equal("trace-1", result.TraceId);
    }

    [Fact]
    public void Sanitize_NullProperties_ProducesEntryWithNullProperties()
    {
        var entries = new List<DebugLogEntry>
        {
            CreateEntry("trace-1", properties: null),
        };

        var result = _sanitizer.Sanitize(entries, "trace-1", _environment);

        Assert.Single(result.LogEntries);
        Assert.Null(result.LogEntries[0].Properties);
    }

    [Fact]
    public void Sanitize_IncludesEnvironmentContext()
    {
        var result = _sanitizer.Sanitize(new List<DebugLogEntry>(), "trace-1", _environment);

        Assert.Equal("1.0.0", result.Environment.AppVersion);
        Assert.Equal("10.0.0", result.Environment.DotnetVersion);
        Assert.Equal("Windows 11", result.Environment.OsDescription);
        Assert.Equal("Development", result.Environment.EnvironmentName);
        Assert.Equal("TestMachine", result.Environment.MachineName);
    }

    [Fact]
    public void Sanitize_ExtractsRequestContextFromProperties()
    {
        var entries = new List<DebugLogEntry>
        {
            CreateEntry("trace-1", new Dictionary<string, object?>
            {
                ["RequestMethod"] = "POST",
                ["RouteTemplate"] = "/api/v1/accounts/{id}/transactions",
                ["StatusCode"] = 500,
                ["Elapsed"] = 234.5,
            }),
        };

        var result = _sanitizer.Sanitize(entries, "trace-1", _environment);

        Assert.NotNull(result.Request);
        Assert.Equal("POST", result.Request!.Method);
        Assert.Equal("/api/v1/accounts/{id}/transactions", result.Request.RouteTemplate);
        Assert.Equal(500, result.Request.StatusCode);
        Assert.Equal(234.5, result.Request.ElapsedMs);
    }

    [Fact]
    public void Sanitize_NoRequestContext_WhenPropertiesMissing()
    {
        var entries = new List<DebugLogEntry>
        {
            CreateEntry("trace-1", new Dictionary<string, object?>
            {
                ["StatusCode"] = 200,
            }),
        };

        var result = _sanitizer.Sanitize(entries, "trace-1", _environment);

        Assert.Null(result.Request);
    }

    [Fact]
    public void Sanitize_MultipleEntries_LastExceptionIsTopLevel()
    {
        var entries = new List<DebugLogEntry>
        {
            CreateEntry("trace-1", exceptionType: "System.ArgumentException", exceptionMessage: "First error"),
            CreateEntry("trace-1"),
            CreateEntry("trace-1", exceptionType: "System.InvalidOperationException", exceptionMessage: "Last error"),
        };

        var result = _sanitizer.Sanitize(entries, "trace-1", _environment);

        Assert.NotNull(result.Exception);
        Assert.Equal("System.InvalidOperationException", result.Exception!.Type);
    }

    [Theory]
    [InlineData("Account 'My Checking' not found", "Account '[REDACTED]' not found")]
    [InlineData("No account with name 'Savings'", "No account with name '[REDACTED]'")]
    [InlineData("Simple message without quotes", "Simple message without quotes")]
    [InlineData("Entity 'd3b07384-d113-4ec6-a7dc-5e8bdef7e9c1' not found", "Entity 'd3b07384-d113-4ec6-a7dc-5e8bdef7e9c1' not found")]
    [InlineData("Multiple 'sensitive' and 'data' here", "Multiple '[REDACTED]' and '[REDACTED]' here")]
    public void SanitizeExceptionMessage_Theory(string input, string expected)
    {
        var result = LogSanitizer.SanitizeExceptionMessage(input);
        Assert.Equal(expected, result);
    }

    private static DebugLogEntry CreateEntry(
        string traceId,
        Dictionary<string, object?>? properties = null,
        string? exceptionType = null,
        string? exceptionMessage = null,
        string? exceptionStackTrace = null)
    {
        return new DebugLogEntry
        {
            TimestampUtc = DateTime.UtcNow,
            Level = exceptionType != null ? "Error" : "Information",
            MessageTemplate = "Test message",
            RenderedMessage = "Test message",
            TraceId = traceId,
            Properties = properties,
            ExceptionType = exceptionType,
            ExceptionMessage = exceptionMessage,
            ExceptionStackTrace = exceptionStackTrace,
        };
    }
}
