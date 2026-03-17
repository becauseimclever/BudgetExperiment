// <copyright file="LogSanitizer.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Text.RegularExpressions;

namespace BudgetExperiment.Api.Observability;

/// <summary>
/// Allowlist-based PII sanitizer for debug log entries. Only explicitly safe properties
/// pass through; everything else is stripped. PII key patterns are matched and redacted.
/// </summary>
public sealed partial class LogSanitizer : ILogSanitizer
{
    private const string RedactedValue = "[REDACTED]";

    private const string NoticeText =
        "This debug log was exported from Budget Experiment. " +
        "All personally identifiable information (account names, transaction details, " +
        "financial amounts, user identifiers) has been redacted. " +
        "You may review this file before attaching it to a GitHub issue.";

    /// <summary>
    /// Property names that are safe to include in the export (case-insensitive).
    /// </summary>
    private static readonly HashSet<string> AllowedProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        // Serilog / framework enrichment
        "TraceId",
        "SpanId",
        "RequestId",
        "ConnectionId",
        "MachineName",
        "EnvironmentName",
        "ApplicationVersion",
        "ThreadId",
        "ProcessId",

        // Request pipeline
        "RequestMethod",
        "RequestPath",
        "RouteTemplate",
        "StatusCode",
        "Elapsed",
        "ElapsedMilliseconds",
        "ContentType",
        "Protocol",
        "Scheme",

        // ASP.NET Core / middleware
        "EndpointName",
        "ActionName",
        "ControllerName",
        "ActionId",
        "EventId",
        "EventName",
        "SourceContext",

        // Domain-safe identifiers (GUIDs / IDs — not names)
        "AccountId",
        "TransactionId",
        "CategoryId",
        "BudgetId",
        "RuleId",
        "ImportId",
        "BatchId",

        // Counts and metrics
        "Count",
        "ItemCount",
        "PageSize",
        "Page",
        "TotalCount",
        "Duration",
        "RetryCount",
    };

    /// <summary>
    /// Patterns that indicate a property contains PII (case-insensitive substring match).
    /// </summary>
    private static readonly string[] PiiPatterns =
    [
        "UserId",
        "UserName",
        "Email",
        "Name",
        "Amount",
        "Balance",
        "Description",
        "Location",
        "Reference",
        "Token",
        "Password",
        "Secret",
        "IpAddress",
        "Authorization",
        "Address",
        "Phone",
        "City",
        "Region",
        "Country",
        "PostalCode",
        "Avatar",
        "Merchant",
        "Note",
    ];

    /// <summary>
    /// Sanitizes exception messages by redacting quoted strings while preserving GUIDs.
    /// </summary>
    /// <param name="message">The exception message to sanitize.</param>
    /// <returns>The sanitized message with quoted PII replaced by [REDACTED].</returns>
    public static string SanitizeExceptionMessage(string message)
    {
        // Replace quoted strings that are NOT GUIDs with [REDACTED]
        return QuotedStringRegex().Replace(message, match =>
        {
            var content = match.Groups[1].Value;
            if (GuidRegex().IsMatch(content))
            {
                return match.Value; // Preserve GUIDs
            }

            return $"'{RedactedValue}'";
        });
    }

    /// <inheritdoc/>
    public SanitizedDebugBundle Sanitize(
        IReadOnlyList<DebugLogEntry> entries,
        string traceId,
        EnvironmentContext environment)
    {
        var redactedCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int totalRedacted = 0;

        var sanitizedEntries = new List<SanitizedLogEntry>(entries.Count);

        RequestContext? requestContext = null;
        ExceptionContext? exceptionContext = null;

        foreach (var entry in entries)
        {
            var (sanitizedProps, entryRedactedCount, entryCategories) = SanitizeProperties(entry.Properties);
            totalRedacted += entryRedactedCount;
            foreach (var cat in entryCategories)
            {
                redactedCategories.Add(cat);
            }

            ExceptionContext? entryException = null;
            if (entry.ExceptionType != null && entry.ExceptionMessage != null)
            {
                var sanitizedMessage = SanitizeExceptionMessage(entry.ExceptionMessage);
                if (sanitizedMessage != entry.ExceptionMessage)
                {
                    totalRedacted++;
                    redactedCategories.Add("ExceptionMessage");
                }

                entryException = new ExceptionContext
                {
                    Type = entry.ExceptionType,
                    Message = sanitizedMessage,
                    StackTrace = entry.ExceptionStackTrace,
                };

                // Use the last exception entry as the top-level exception
                exceptionContext = entryException;
            }

            // Extract request context from properties if available
            if (requestContext == null && entry.Properties != null)
            {
                requestContext = TryExtractRequestContext(entry.Properties);
            }

            sanitizedEntries.Add(new SanitizedLogEntry
            {
                TimestampUtc = entry.TimestampUtc,
                Level = entry.Level,
                MessageTemplate = entry.MessageTemplate,
                Properties = sanitizedProps.Count > 0 ? sanitizedProps : null,
                Exception = entryException,
            });
        }

        return new SanitizedDebugBundle
        {
            Notice = NoticeText,
            RedactionSummary = new RedactionSummary
            {
                TotalFieldsRedacted = totalRedacted,
                CategoriesRedacted = redactedCategories.OrderBy(c => c, StringComparer.Ordinal).ToList(),
            },
            TraceId = traceId,
            ExportedAtUtc = DateTime.UtcNow,
            Environment = environment,
            Request = requestContext,
            Exception = exceptionContext,
            LogEntries = sanitizedEntries,
        };
    }

    private static (Dictionary<string, object?> Properties, int RedactedCount, List<string> Categories) SanitizeProperties(
        IReadOnlyDictionary<string, object?>? properties)
    {
        var result = new Dictionary<string, object?>();
        var redactedCount = 0;
        var categories = new List<string>();

        if (properties == null)
        {
            return (result, redactedCount, categories);
        }

        foreach (var kvp in properties)
        {
            if (AllowedProperties.Contains(kvp.Key))
            {
                result[kvp.Key] = kvp.Value;
            }
            else if (IsPiiProperty(kvp.Key))
            {
                // Known PII — count as redacted
                redactedCount++;
                categories.Add(CategorizePiiProperty(kvp.Key));
            }

            // else: unknown property — stripped entirely (allowlist approach), no count increment
        }

        return (result, redactedCount, categories);
    }

    private static bool IsPiiProperty(string propertyName)
    {
        return PiiPatterns.Any(pattern =>
            propertyName.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private static string CategorizePiiProperty(string propertyName)
    {
        foreach (var pattern in PiiPatterns)
        {
            if (propertyName.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return pattern;
            }
        }

        return "Unknown";
    }

    private static RequestContext? TryExtractRequestContext(IReadOnlyDictionary<string, object?> properties)
    {
        var hasMethod = properties.TryGetValue("RequestMethod", out var method);
        var hasRoute = properties.TryGetValue("RouteTemplate", out var route);
        var hasStatus = properties.TryGetValue("StatusCode", out var status);

        if (!hasMethod || !hasRoute || !hasStatus)
        {
            return null;
        }

        double? elapsed = null;
        if (properties.TryGetValue("Elapsed", out var elapsedVal) && elapsedVal != null)
        {
            if (elapsedVal is double d)
            {
                elapsed = d;
            }
            else if (double.TryParse(elapsedVal.ToString(), out var parsed))
            {
                elapsed = parsed;
            }
        }
        else if (properties.TryGetValue("ElapsedMilliseconds", out var elapsedMsVal) && elapsedMsVal != null)
        {
            if (elapsedMsVal is double d)
            {
                elapsed = d;
            }
            else if (double.TryParse(elapsedMsVal.ToString(), out var parsed))
            {
                elapsed = parsed;
            }
        }

        int statusCode = 0;
        if (status is int si)
        {
            statusCode = si;
        }
        else if (status != null && int.TryParse(status.ToString(), out var parsed))
        {
            statusCode = parsed;
        }

        return new RequestContext
        {
            Method = method?.ToString() ?? string.Empty,
            RouteTemplate = route?.ToString() ?? string.Empty,
            StatusCode = statusCode,
            ElapsedMs = elapsed,
        };
    }

    [GeneratedRegex(@"'([^']*)'")]
    private static partial Regex QuotedStringRegex();

    [GeneratedRegex(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$")]
    private static partial Regex GuidRegex();
}
