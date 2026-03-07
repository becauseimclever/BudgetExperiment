// <copyright file="GoogleClaimMapper.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

using BudgetExperiment.Contracts.Constants;

namespace BudgetExperiment.Api.Authentication;

/// <summary>
/// Maps Google OAuth claims to the application's expected claim types.
/// Google ID tokens include standard OIDC claims (<c>sub</c>, <c>name</c>, <c>email</c>, <c>picture</c>)
/// but do not include <c>preferred_username</c> which the application requires for user display.
/// </summary>
[ExcludeFromCodeCoverage]
public static class GoogleClaimMapper
{
    /// <summary>
    /// Maps Google-specific claims to the claims expected by the application.
    /// Adds <c>preferred_username</c> from the <c>email</c> claim if not already present.
    /// </summary>
    /// <param name="principal">The claims principal to update. May be null.</param>
    public static void MapClaims(ClaimsPrincipal? principal)
    {
        if (principal?.Identity is not ClaimsIdentity identity)
        {
            return;
        }

        // Google doesn't provide preferred_username; derive from email
        if (!identity.HasClaim(c => c.Type == ClaimConstants.PreferredUsername))
        {
            var email = identity.FindFirst(ClaimConstants.Email)?.Value;
            if (!string.IsNullOrEmpty(email))
            {
                identity.AddClaim(new Claim(ClaimConstants.PreferredUsername, email));
            }
        }
    }
}
