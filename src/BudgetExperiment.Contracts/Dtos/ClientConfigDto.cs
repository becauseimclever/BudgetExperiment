// <copyright file="ClientConfigDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Configuration settings exposed to the Blazor WebAssembly client.
/// This DTO contains ONLY non-secret, client-appropriate settings.
/// </summary>
public sealed class ClientConfigDto
{
    /// <summary>
    /// Gets or sets the authentication configuration.
    /// </summary>
    public required AuthenticationConfigDto Authentication { get; init; }
}

/// <summary>
/// Authentication configuration for the client.
/// </summary>
public sealed class AuthenticationConfigDto
{
    /// <summary>
    /// Gets or sets the authentication mode: "none" or "oidc".
    /// When "none", authentication is disabled and all users get default scope.
    /// </summary>
    public required string Mode { get; init; }

    /// <summary>
    /// Gets or sets the OIDC provider settings (populated when Mode = "oidc").
    /// </summary>
    public OidcConfigDto? Oidc { get; init; }
}

/// <summary>
/// OIDC provider configuration.
/// </summary>
public sealed class OidcConfigDto
{
    /// <summary>
    /// Gets or sets the OIDC authority URL (issuer).
    /// </summary>
    public required string Authority { get; init; }

    /// <summary>
    /// Gets or sets the OAuth2 client ID (public identifier, NOT a secret for PKCE flows).
    /// </summary>
    public required string ClientId { get; init; }

    /// <summary>
    /// Gets or sets the OAuth2 response type (typically "code" for PKCE).
    /// </summary>
    public string ResponseType { get; init; } = "code";

    /// <summary>
    /// Gets or sets the scopes to request during authentication.
    /// </summary>
    public IReadOnlyList<string> Scopes { get; init; } = ["openid", "profile", "email"];

    /// <summary>
    /// Gets or sets the redirect URI after logout.
    /// </summary>
    public string PostLogoutRedirectUri { get; init; } = "/";

    /// <summary>
    /// Gets or sets the redirect URI after login callback.
    /// </summary>
    public string RedirectUri { get; init; } = "authentication/login-callback";
}
