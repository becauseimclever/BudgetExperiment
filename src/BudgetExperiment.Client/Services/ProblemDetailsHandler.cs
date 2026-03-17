// <copyright file="ProblemDetailsHandler.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Text.Json;

namespace BudgetExperiment.Client.Services;

/// <summary>
/// HTTP message handler that intercepts non-success responses, parses ProblemDetails JSON,
/// and stores the traceId in <see cref="IApiErrorContext"/>.
/// </summary>
public sealed class ProblemDetailsHandler : DelegatingHandler
{
    private readonly ApiErrorContext _errorContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProblemDetailsHandler"/> class.
    /// </summary>
    /// <param name="errorContext">The API error context to store traceId in.</param>
    public ProblemDetailsHandler(ApiErrorContext errorContext)
    {
        this._errorContext = errorContext;
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode && response.Content != null)
        {
            await this.TryExtractTraceIdAsync(response, cancellationToken);
        }

        return response;
    }

    private async Task TryExtractTraceIdAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (contentType is not ("application/problem+json" or "application/json"))
            {
                return;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            // Replace the content so downstream consumers can still read it
            response.Content = new StringContent(
                content,
                System.Text.Encoding.UTF8,
                contentType);

            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("traceId", out var traceIdElement) &&
                traceIdElement.ValueKind == JsonValueKind.String)
            {
                var traceId = traceIdElement.GetString();
                if (!string.IsNullOrEmpty(traceId))
                {
                    this._errorContext.SetTraceId(traceId);
                }
            }
        }
        catch (JsonException)
        {
            // Not valid JSON — ignore
        }
    }
}
