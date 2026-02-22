// <copyright file="NoAuthHandler.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Security.Claims;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace BudgetExperiment.Api.Authentication;

/// <summary>
/// Authentication handler that always succeeds and creates a family user context.
/// Used when <c>Authentication:Mode</c> is set to <c>"None"</c>.
/// </summary>
/// <remarks>
/// This handler provides a deterministic user identity so that data scoping
/// by user ID continues to work even when authentication is disabled.
/// All requests are treated as authenticated using the <see cref="FamilyUserContext"/> constants.
/// </remarks>
public sealed class NoAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>
    /// The authentication scheme name used to register this handler.
    /// </summary>
    public const string SchemeName = "NoAuth";

    /// <summary>
    /// Initializes a new instance of the <see cref="NoAuthHandler"/> class.
    /// </summary>
    /// <param name="options">The authentication scheme options monitor.</param>
    /// <param name="logger">The logger factory.</param>
    /// <param name="encoder">The URL encoder.</param>
    public NoAuthHandler(
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
            new Claim(ClaimTypes.NameIdentifier, FamilyUserContext.FamilyUserId.ToString()),
            new Claim(ClaimTypes.Name, FamilyUserContext.FamilyUserName),
            new Claim(ClaimTypes.Email, FamilyUserContext.FamilyUserEmail),
            new Claim("sub", FamilyUserContext.FamilyUserId.ToString()),
            new Claim("name", FamilyUserContext.FamilyUserName),
            new Claim("preferred_username", FamilyUserContext.FamilyUserName),
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
