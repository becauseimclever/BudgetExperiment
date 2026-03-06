// <copyright file="NoAuthAuthenticationStateProvider.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Security.Claims;

using BudgetExperiment.Contracts.Constants;

using Microsoft.AspNetCore.Components.Authorization;

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Authentication state provider for auth-off (demo) mode.
/// Always returns an authenticated state with the well-known family user claims.
/// Values are sourced from <see cref="FamilyUserDefaults"/> in Contracts.
/// </summary>
public sealed class NoAuthAuthenticationStateProvider : AuthenticationStateProvider
{
    /// <summary>
    /// Well-known GUID for the family user in auth-off mode.
    /// Sourced from <see cref="FamilyUserDefaults.UserId"/>.
    /// </summary>
    public const string FamilyUserId = FamilyUserDefaults.UserId;

    /// <summary>
    /// Display name for the family user.
    /// </summary>
    public const string FamilyUserName = FamilyUserDefaults.UserName;

    /// <summary>
    /// Email for the family user.
    /// </summary>
    public const string FamilyUserEmail = FamilyUserDefaults.UserEmail;

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
            new Claim(ClaimConstants.Subject, FamilyUserId),
        };

        var identity = new ClaimsIdentity(claims, "NoAuth");
        var principal = new ClaimsPrincipal(identity);
        return new AuthenticationState(principal);
    }
}
