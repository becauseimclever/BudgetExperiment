// <copyright file="AutoAuthenticatingTestHandler.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Security.Claims;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Test authentication handler that auto-authenticates all requests.
/// Used for backward-compatible integration tests that don't need to test auth specifically.
/// </summary>
public sealed class AutoAuthenticatingTestHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AutoAuthenticatingTestHandler"/> class.
    /// </summary>
    /// <param name="options">The authentication scheme options.</param>
    /// <param name="logger">The logger factory.</param>
    /// <param name="encoder">The URL encoder.</param>
    public AutoAuthenticatingTestHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Auto-authenticate all requests with a test user
        var claims = new[]
        {
            new Claim("sub", CustomWebApplicationFactory.TestUserId.ToString()),
            new Claim("preferred_username", CustomWebApplicationFactory.TestUsername),
            new Claim(ClaimTypes.NameIdentifier, CustomWebApplicationFactory.TestUserId.ToString()),
            new Claim(ClaimTypes.Name, CustomWebApplicationFactory.TestUsername),
        };

        var identity = new ClaimsIdentity(claims, "TestAuto");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestAuto");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
