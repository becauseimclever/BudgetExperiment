// <copyright file="ClientConfigOptions.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Api;

using BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Strongly-typed options for client configuration.
/// Maps from IConfiguration and exposes settings safe for client consumption.
/// </summary>
public sealed class ClientConfigOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "ClientConfig";

    /// <summary>
    /// Gets or sets the authentication mode: "none" or "oidc".
    /// </summary>
    public string AuthMode { get; set; } = "oidc";

    /// <summary>
    /// Gets or sets the OIDC authority URL.
    /// </summary>
    public string OidcAuthority { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the OIDC client ID.
    /// </summary>
    public string OidcClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the OAuth2 response type.
    /// </summary>
    public string OidcResponseType { get; set; } = "code";

    /// <summary>
    /// Gets or sets the OIDC scopes.
    /// </summary>
    public List<string> OidcScopes { get; set; } = ["openid", "profile", "email"];

    /// <summary>
    /// Gets or sets the post-logout redirect URI.
    /// </summary>
    public string OidcPostLogoutRedirectUri { get; set; } = "/";

    /// <summary>
    /// Gets or sets the redirect URI after login.
    /// </summary>
    public string OidcRedirectUri { get; set; } = "authentication/login-callback";

    /// <summary>
    /// Converts this options instance to a client-safe DTO.
    /// </summary>
    /// <returns>A <see cref="ClientConfigDto"/> containing the client configuration.</returns>
    public ClientConfigDto ToDto() => new()
    {
        Authentication = new AuthenticationConfigDto
        {
            Mode = this.AuthMode,
            Oidc = string.Equals(this.AuthMode, "oidc", StringComparison.OrdinalIgnoreCase)
                ? new OidcConfigDto
                {
                    Authority = this.OidcAuthority,
                    ClientId = this.OidcClientId,
                    ResponseType = this.OidcResponseType,
                    Scopes = this.OidcScopes,
                    PostLogoutRedirectUri = this.OidcPostLogoutRedirectUri,
                    RedirectUri = this.OidcRedirectUri,
                }
                : null,
        },
    };
}
