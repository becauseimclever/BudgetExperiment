// <copyright file="NoAuthAuthenticationStateProvider.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Security.Claims;

using Microsoft.AspNetCore.Components.Authorization;

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Authentication state provider for auth-off (demo) mode.
/// Always returns an authenticated state with the well-known family user claims.
/// This mirrors the API's NoAuthHandler and FamilyUserContext constants.
/// </summary>
public sealed class NoAuthAuthenticationStateProvider : AuthenticationStateProvider
{
    /// <summary>
    /// Well-known GUID for the family user in auth-off mode.
    /// Must match the API's FamilyUserContext.FamilyUserId.
    /// </summary>
    public const string FamilyUserId = "00000000-0000-0000-0000-000000000001";

    /// <summary>
    /// Display name for the family user.
    /// </summary>
    public const string FamilyUserName = "Family";

    /// <summary>
    /// Email for the family user.
    /// </summary>
    public const string FamilyUserEmail = "family@localhost";

    private static readonly AuthenticationState AuthenticatedState = CreateFamilyUserState();

    /// <inheritdoc/>
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(AuthenticatedState);
    }

    private static AuthenticationState CreateFamilyUserState()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, FamilyUserId),
            new Claim(ClaimTypes.Name, FamilyUserName),
            new Claim(ClaimTypes.Email, FamilyUserEmail),
            new Claim("sub", FamilyUserId),
        };

        var identity = new ClaimsIdentity(claims, "NoAuth");
        var principal = new ClaimsPrincipal(identity);
        return new AuthenticationState(principal);
    }
}
