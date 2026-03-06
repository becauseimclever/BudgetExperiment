// <copyright file="OidcConfigDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Constants;

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// OIDC provider configuration.
/// </summary>
public sealed class OidcConfigDto
{
    /// <summary>
    /// Gets the OIDC authority URL (issuer).
    /// </summary>
    public required string Authority { get; init; }

    /// <summary>
    /// Gets the OAuth2 client ID (public identifier, NOT a secret for PKCE flows).
    /// </summary>
    public required string ClientId { get; init; }

    /// <summary>
    /// Gets the OAuth2 response type (typically "code" for PKCE).
    /// </summary>
    public string ResponseType { get; init; } = "code";

    /// <summary>
    /// Gets the scopes to request during authentication.
    /// </summary>
    public IReadOnlyList<string> Scopes { get; init; } = [OidcScopeDefaults.OpenId, OidcScopeDefaults.Profile, OidcScopeDefaults.Email];

    /// <summary>
    /// Gets the redirect URI after logout.
    /// </summary>
    public string PostLogoutRedirectUri { get; init; } = "/";

    /// <summary>
    /// Gets the redirect URI after login callback.
    /// </summary>
    public string RedirectUri { get; init; } = "authentication/login-callback";
}
