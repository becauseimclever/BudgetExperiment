// <copyright file="UserContext.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Security.Claims;

using BudgetExperiment.Contracts.Constants;
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
    public string UserId => this.GetClaimValue(ClaimTypes.NameIdentifier) ?? this.GetClaimValue(ClaimConstants.Subject) ?? string.Empty;

    /// <inheritdoc/>
    public Guid? UserIdAsGuid => ParseUserIdAsGuid(this.UserId);

    /// <inheritdoc/>
    public string Username => this.GetClaimValue(ClaimConstants.PreferredUsername) ?? this.GetClaimValue(ClaimTypes.Name) ?? string.Empty;

    /// <inheritdoc/>
    public string? Email => this.GetClaimValue(ClaimTypes.Email) ?? this.GetClaimValue(ClaimConstants.Email);

    /// <inheritdoc/>
    public string? DisplayName => this.GetClaimValue(ClaimConstants.Name) ?? this.GetClaimValue(ClaimTypes.GivenName);

    /// <inheritdoc/>
    public string? AvatarUrl => this.GetClaimValue(ClaimConstants.Picture);

    /// <inheritdoc/>
    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    /// <inheritdoc/>
    public BudgetScope? CurrentScope => currentScope;

    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    /// <inheritdoc/>
    public void SetScope(BudgetScope? scope)
    {
        currentScope = scope;
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
