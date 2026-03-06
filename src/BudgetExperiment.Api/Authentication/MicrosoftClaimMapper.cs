// <copyright file="MicrosoftClaimMapper.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Security.Claims;

using BudgetExperiment.Contracts.Constants;

namespace BudgetExperiment.Api.Authentication;

/// <summary>
/// Maps Microsoft Entra ID claims to the application's expected claim types.
/// Microsoft v2 tokens typically include <c>sub</c>, <c>name</c>, <c>email</c>, <c>oid</c>,
/// and <c>tid</c>. The <c>preferred_username</c> claim may or may not be present depending
/// on token version and scopes. This mapper ensures it is always available.
/// </summary>
public static class MicrosoftClaimMapper
{
    /// <summary>
    /// Maps Microsoft-specific claims to the claims expected by the application.
    /// Adds <c>preferred_username</c> from the <c>email</c> claim if not already present.
    /// </summary>
    /// <param name="principal">The claims principal to update. May be null.</param>
    public static void MapClaims(ClaimsPrincipal? principal)
    {
        if (principal?.Identity is not ClaimsIdentity identity)
        {
            return;
        }

        // Microsoft v2 tokens may or may not include preferred_username;
        // derive from email if missing
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
