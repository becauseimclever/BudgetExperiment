// <copyright file="GoogleProviderOptions.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Diagnostics.CodeAnalysis;

namespace BudgetExperiment.Api;

/// <summary>
/// Google OAuth provider configuration options.
/// Nested under <see cref="AuthenticationOptions.Google"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GoogleProviderOptions
{
    /// <summary>
    /// The well-known Google OIDC authority URL.
    /// Google always uses this authority for OpenID Connect discovery.
    /// </summary>
    public const string Authority = "https://accounts.google.com";

    /// <summary>
    /// Gets or sets the Google OAuth client ID.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Google OAuth client secret.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;
}
