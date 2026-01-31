// <copyright file="AuthentikOptions.cs" company="Fortinbra">
// Copyright (c) 2025 Fortinbra (becauseimclever.com). All rights reserved.
// </copyright>

namespace BudgetExperiment.Api;

/// <summary>
/// Configuration options for Authentik OIDC authentication.
/// </summary>
public sealed class AuthentikOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "Authentication:Authentik";

    /// <summary>
    /// Gets or sets the authority URL (Authentik provider URL).
    /// Example: https://auth.example.com/application/o/budget-experiment/
    /// </summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the audience (typically the client ID or API identifier).
    /// Used for API JWT token validation.
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the client ID for the OIDC client (Blazor WASM).
    /// If not specified, defaults to the Audience value.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether HTTPS metadata is required.
    /// Should be true in production, can be false for local development.
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;
}
