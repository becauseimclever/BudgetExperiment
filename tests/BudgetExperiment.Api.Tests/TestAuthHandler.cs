// <copyright file="TestAuthHandler.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Security.Claims;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Test authentication handler that validates based on the Authorization header.
/// </summary>
public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestAuthHandler"/> class.
    /// </summary>
    /// <param name="options">The authentication scheme options.</param>
    /// <param name="logger">The logger factory.</param>
    /// <param name="encoder">The URL encoder.</param>
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!this.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var headerValue = authHeader.ToString();
        if (!headerValue.StartsWith("Test ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var parameter = headerValue["Test ".Length..];

        Guid userId;
        string username;

        if (parameter.Contains(':'))
        {
            // Format: "userId:username"
            var parts = parameter.Split(':');
            userId = Guid.Parse(parts[0]);
            username = parts[1];
        }
        else if (parameter == "authenticated")
        {
            // Default test user
            userId = AuthEnabledWebApplicationFactory.TestUserId;
            username = AuthEnabledWebApplicationFactory.TestUsername;
        }
        else
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid test token"));
        }

        var claims = new[]
        {
            new Claim("sub", userId.ToString()),
            new Claim("preferred_username", username),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username),
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
