// <copyright file="GenericOidcProviderOptions.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Api;

/// <summary>
/// Generic OIDC provider configuration options for Keycloak, Auth0, Okta, etc.
/// Nested under <see cref="AuthenticationOptions.Oidc"/>.
/// </summary>
public sealed class GenericOidcProviderOptions
{
    /// <summary>
    /// Gets or sets the OIDC authority URL.
    /// </summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the client ID.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the client secret (for confidential clients).
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the requested scopes.
    /// </summary>
    public string[] Scopes { get; set; } = ["openid", "profile", "email"];

    /// <summary>
    /// Gets or sets the audience (API identifier).
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether HTTPS metadata is required.
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>
    /// Gets or sets custom claim mappings for non-standard providers.
    /// Key: Standard claim name (e.g., "sub"), Value: Provider's claim name.
    /// </summary>
    public Dictionary<string, string> ClaimMappings { get; set; } = new();
}
