// <copyright file="FakeHttpMessageHandler.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// A fake <see cref="HttpMessageHandler"/> that returns canned responses.
/// </summary>
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    /// <summary>Gets or sets the response content to return.</summary>
    public string ResponseContent { get; set; } = string.Empty;

    /// <summary>Gets or sets the HTTP status code to return.</summary>
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

    /// <summary>Gets the URI of the last request sent.</summary>
    public Uri? LastRequestUri
    {
        get; private set;
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequestUri = request.RequestUri;

        var response = new HttpResponseMessage(StatusCode)
        {
            Content = new StringContent(ResponseContent, System.Text.Encoding.UTF8, "application/json"),
        };

        return Task.FromResult(response);
    }
}
