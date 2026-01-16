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
    public string Username => this.GetClaimValue("preferred_username") ?? this.GetClaimValue(ClaimTypes.Name) ?? string.Empty;

    /// <inheritdoc/>
    public string? Email => this.GetClaimValue(ClaimTypes.Email) ?? this.GetClaimValue("email");

    /// <inheritdoc/>
    public string? DisplayName => this.GetClaimValue("name") ?? this.GetClaimValue(ClaimTypes.GivenName);

    /// <inheritdoc/>
    public string? AvatarUrl => this.GetClaimValue("picture");

    /// <inheritdoc/>
    public bool IsAuthenticated => this.httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    private ClaimsPrincipal? User => this.httpContextAccessor.HttpContext?.User;

    private string? GetClaimValue(string claimType)
    {
        return this.User?.FindFirst(claimType)?.Value;
    }
}
