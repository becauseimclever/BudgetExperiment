// <copyright file="MicrosoftProviderOptions.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Api;

/// <summary>
/// Microsoft Entra ID (Azure AD) provider configuration options.
/// Nested under <see cref="AuthenticationOptions.Microsoft"/>.
/// </summary>
public sealed class MicrosoftProviderOptions
{
    /// <summary>
    /// The Microsoft Entra ID authority URL template.
    /// The <c>{0}</c> placeholder is replaced with the <see cref="TenantId"/>.
    /// </summary>
    public const string AuthorityTemplate = "https://login.microsoftonline.com/{0}/v2.0";

    /// <summary>
    /// Gets or sets the Microsoft Entra ID client (application) ID.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tenant ID. Accepts "common", "organizations", or a specific tenant GUID.
    /// Defaults to "common" for multi-tenant applications.
    /// </summary>
    public string TenantId { get; set; } = "common";

    /// <summary>
    /// Gets or sets the client secret. Optional for public clients (Blazor WASM uses PKCE).
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Resolves the Microsoft Entra ID authority URL by substituting the <see cref="TenantId"/>
    /// into the <see cref="AuthorityTemplate"/>.
    /// </summary>
    /// <returns>The fully-qualified authority URL (e.g., <c>https://login.microsoftonline.com/common/v2.0</c>).</returns>
    public string ResolveAuthority()
    {
        return string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            AuthorityTemplate,
            TenantId);
    }
}
