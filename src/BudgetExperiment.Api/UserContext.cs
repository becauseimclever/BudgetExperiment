// <copyright file="UserContext.cs" company="Fortinbra">
// Copyright (c) 2025 Fortinbra (becauseimclever.com). All rights reserved.
// </copyright>

using System.Security.Claims;

using BudgetExperiment.Domain;

namespace BudgetExperiment.Api;

/// <summary>
/// Provides access to the current authenticated user's identity by extracting claims from the HTTP context.
/// </summary>
public sealed class UserContext : IUserContext
{
    private readonly IHttpContextAccessor httpContextAccessor;
    private BudgetScope? currentScope;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserContext"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    public UserContext(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <inheritdoc/>
    public string UserId => this.GetClaimValue(ClaimTypes.NameIdentifier) ?? this.GetClaimValue("sub") ?? string.Empty;

    /// <inheritdoc/>
    public Guid? UserIdAsGuid => ParseUserIdAsGuid(this.UserId);

    /// <inheritdoc/>
    public string Username => this.GetClaimValue("preferred_username") ?? this.GetClaimValue(ClaimTypes.Name) ?? string.Empty;

    /// <inheritdoc/>
    public string? Email => this.GetClaimValue(ClaimTypes.Email) ?? this.GetClaimValue("email");

    /// <inheritdoc/>
    public string? DisplayName => this.GetClaimValue("name") ?? this.GetClaimValue(ClaimTypes.GivenName);

    /// <inheritdoc/>
    public string? AvatarUrl => this.GetClaimValue("picture");

    /// <inheritdoc/>
    public bool IsAuthenticated => this.httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    /// <inheritdoc/>
    public BudgetScope? CurrentScope => this.currentScope;

    private ClaimsPrincipal? User => this.httpContextAccessor.HttpContext?.User;

    /// <inheritdoc/>
    public void SetScope(BudgetScope? scope)
    {
        this.currentScope = scope;
    }

    /// <summary>
    /// Parses a user ID string as a GUID, handling both standard GUID format
    /// and Authentik's 64-character hex format (taking first 32 chars).
    /// </summary>
    /// <param name="userId">The user ID string to parse.</param>
    /// <returns>A GUID if parsing succeeds; otherwise, null.</returns>
    private static Guid? ParseUserIdAsGuid(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        // First, try standard GUID parsing (handles "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx" format)
        if (Guid.TryParse(userId, out var guid))
        {
            return guid;
        }

        // Handle Authentik's 64-character hex format by taking first 32 hex chars
        // and formatting as a GUID. This creates a deterministic GUID from the hex string.
        if (userId.Length >= 32 && IsHexString(userId))
        {
            var hexSubstring = userId[..32];
            if (Guid.TryParseExact(hexSubstring, "N", out var derivedGuid))
            {
                return derivedGuid;
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if a string contains only hexadecimal characters.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <returns>True if all characters are hex digits; otherwise, false.</returns>
    private static bool IsHexString(string value)
    {
        foreach (var c in value)
        {
            if (!char.IsAsciiHexDigit(c))
            {
                return false;
            }
        }

        return true;
    }

    private string? GetClaimValue(string claimType)
    {
        return this.User?.FindFirst(claimType)?.Value;
    }
}
