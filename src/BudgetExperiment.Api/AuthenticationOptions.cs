// <copyright file="AuthenticationOptions.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Diagnostics.CodeAnalysis;

namespace BudgetExperiment.Api;

/// <summary>
/// Root authentication configuration options.
/// Supports multiple auth modes (None, OIDC) and multiple OIDC providers
/// (Authentik, Google, Microsoft, generic OIDC).
/// </summary>
/// <remarks>
/// Binds from the "Authentication" configuration section.
/// Backward compatible with existing <c>Authentication:Authentik:*</c> configuration keys.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class AuthenticationOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "Authentication";

    /// <summary>
    /// Gets or sets the authentication mode.
    /// <c>"None"</c> disables authentication (family/demo mode).
    /// <c>"OIDC"</c> enables OIDC-based authentication (default).
    /// </summary>
    public string Mode { get; set; } = AuthModeConstants.Oidc;

    /// <summary>
    /// Gets or sets the OIDC provider to use when <see cref="Mode"/> is <c>"OIDC"</c>.
    /// Options: "Authentik" (default), "Google", "Microsoft", "OIDC" (generic).
    /// </summary>
    public string Provider { get; set; } = AuthProviderConstants.Authentik;

    /// <summary>
    /// Gets or sets the Authentik-specific configuration.
    /// </summary>
    public AuthentikProviderOptions Authentik { get; set; } = new();

    /// <summary>
    /// Gets or sets the Google OAuth configuration.
    /// </summary>
    public GoogleProviderOptions Google { get; set; } = new();

    /// <summary>
    /// Gets or sets the Microsoft Entra ID configuration.
    /// </summary>
    public MicrosoftProviderOptions Microsoft { get; set; } = new();

    /// <summary>
    /// Gets or sets the generic OIDC provider configuration.
    /// </summary>
    public GenericOidcProviderOptions Oidc { get; set; } = new();

    /// <summary>
    /// Resolves the effective authentication mode from configuration,
    /// taking into account backward compatibility with the legacy <c>Authentication:Authentik:Enabled</c> flag.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The resolved authentication mode (<see cref="AuthModeConstants.Oidc"/> or <see cref="AuthModeConstants.None"/>).</returns>
    /// <remarks>
    /// Priority order:
    /// <list type="number">
    /// <item>Explicit <c>Authentication:Mode</c> value (if set).</item>
    /// <item>Legacy <c>Authentication:Authentik:Enabled</c> flag (<c>false</c> → <c>"None"</c>).</item>
    /// <item>Default: <c>"OIDC"</c>.</item>
    /// </list>
    /// When the legacy <c>Enabled</c> flag is used, a deprecation warning should be logged.
    /// </remarks>
    public static string ResolveEffectiveMode(IConfiguration configuration)
    {
        var authSection = configuration.GetSection(SectionName);

        // 1. Explicit Mode takes precedence
        var explicitMode = authSection.GetValue<string?>("Mode");
        if (!string.IsNullOrWhiteSpace(explicitMode))
        {
            return explicitMode;
        }

        // 2. Legacy Authentik:Enabled fallback
        var authentikSection = authSection.GetSection("Authentik");
        var enabledValue = authentikSection.GetValue<string?>("Enabled");
        if (enabledValue is not null)
        {
            if (bool.TryParse(enabledValue, out var enabled) && !enabled)
            {
                return AuthModeConstants.None;
            }
        }

        // 3. Default
        return AuthModeConstants.Oidc;
    }

    /// <summary>
    /// Validates that the OIDC configuration for the specified provider has a valid Authority.
    /// Throws <see cref="InvalidOperationException"/> if Authority is missing when Mode is OIDC.
    /// </summary>
    /// <param name="authority">The resolved authority URL.</param>
    /// <exception cref="InvalidOperationException">Thrown when the authority is not configured.</exception>
    public static void ValidateOidcAuthority(string authority)
    {
        if (string.IsNullOrWhiteSpace(authority))
        {
            throw new InvalidOperationException(
                "OIDC Authority is not configured. " +
                "Authentication is required when Mode=OIDC. " +
                "Configure the Authority for your chosen provider, or " +
                "set 'Authentication:Mode' to 'None' to disable authentication.");
        }
    }

    /// <summary>
    /// Resolves authority, audience, and HTTPS requirement from the provider-specific options.
    /// </summary>
    /// <param name="authOptions">The bound authentication options.</param>
    /// <returns>A tuple of (Authority, Audience, RequireHttpsMetadata).</returns>
    /// <exception cref="InvalidOperationException">Thrown when the provider is not recognized.</exception>
    public static (string Authority, string Audience, bool RequireHttpsMetadata) ResolveProviderSettings(
        AuthenticationOptions authOptions)
    {
        return authOptions.Provider switch
        {
            var p when string.Equals(p, AuthProviderConstants.Google, StringComparison.OrdinalIgnoreCase) =>
                (GoogleProviderOptions.Authority, authOptions.Google.ClientId, true),

            var p when string.Equals(p, AuthProviderConstants.Microsoft, StringComparison.OrdinalIgnoreCase) =>
                (authOptions.Microsoft.ResolveAuthority(), authOptions.Microsoft.ClientId, true),

            var p when string.Equals(p, AuthProviderConstants.Oidc, StringComparison.OrdinalIgnoreCase) =>
                (authOptions.Oidc.Authority, authOptions.Oidc.Audience, authOptions.Oidc.RequireHttpsMetadata),

            // Default: Authentik (backward compat — Provider defaults to "Authentik")
            _ => (authOptions.Authentik.Authority, authOptions.Authentik.Audience, authOptions.Authentik.RequireHttpsMetadata),
        };
    }
}
