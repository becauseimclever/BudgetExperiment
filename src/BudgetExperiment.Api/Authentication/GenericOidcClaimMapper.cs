// <copyright file="GenericOidcClaimMapper.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Security.Claims;

namespace BudgetExperiment.Api.Authentication;

/// <summary>
/// Maps claims from generic OIDC providers (Keycloak, Auth0, Okta, etc.) to the
/// application's expected claim types using configurable claim mappings.
/// Non-standard providers may use different claim names for common attributes.
/// This mapper applies user-defined mappings and falls back to deriving
/// <c>preferred_username</c> from <c>email</c> if not already present.
/// </summary>
public static class GenericOidcClaimMapper
{
    /// <summary>
    /// Maps provider-specific claims to the claims expected by the application
    /// using the configured claim mappings dictionary.
    /// For each mapping entry, copies the value from the source claim to a new
    /// target claim if the source exists and the target does not.
    /// Additionally, adds <c>preferred_username</c> from <c>email</c> if still missing
    /// after applying explicit mappings.
    /// </summary>
    /// <param name="principal">The claims principal to update. May be null.</param>
    /// <param name="claimMappings">
    /// Dictionary of source → target claim mappings. May be null or empty.
    /// Key: source claim type in the token. Value: target claim type expected by the application.
    /// </param>
    public static void MapClaims(ClaimsPrincipal? principal, Dictionary<string, string>? claimMappings)
    {
        if (principal?.Identity is not ClaimsIdentity identity)
        {
            return;
        }

        // Apply explicit claim mappings
        if (claimMappings is { Count: > 0 })
        {
            foreach (var (sourceClaim, targetClaim) in claimMappings)
            {
                if (!identity.HasClaim(c => c.Type == targetClaim))
                {
                    var sourceValue = identity.FindFirst(sourceClaim)?.Value;
                    if (!string.IsNullOrEmpty(sourceValue))
                    {
                        identity.AddClaim(new Claim(targetClaim, sourceValue));
                    }
                }
            }
        }

        // Fallback: derive preferred_username from email if still missing
        if (!identity.HasClaim(c => c.Type == "preferred_username"))
        {
            var email = identity.FindFirst("email")?.Value;
            if (!string.IsNullOrEmpty(email))
            {
                identity.AddClaim(new Claim("preferred_username", email));
            }
        }
    }
}
