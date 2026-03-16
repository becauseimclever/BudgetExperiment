// Copyright (c) BecauseImClever. All rights reserved.

using System.Security.Claims;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BudgetExperiment.Performance.Tests.Infrastructure;

/// <summary>
/// Test authentication handler that auto-authenticates all requests with a known test user.
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
        var claims = new[]
        {
            new Claim("sub", PerformanceWebApplicationFactory.TestUserId.ToString()),
            new Claim("preferred_username", PerformanceWebApplicationFactory.TestUsername),
            new Claim(ClaimTypes.NameIdentifier, PerformanceWebApplicationFactory.TestUserId.ToString()),
            new Claim(ClaimTypes.Name, PerformanceWebApplicationFactory.TestUsername),
        };

        var identity = new ClaimsIdentity(claims, "TestAuto");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestAuto");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
