// <copyright file="MicrosoftClaimMapperTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Security.Claims;

using BudgetExperiment.Api.Authentication;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Unit tests for <see cref="MicrosoftClaimMapper"/> claim mapping logic.
/// Microsoft Entra ID tokens may use <c>oid</c> for user object ID and
/// may not include <c>preferred_username</c> depending on the token version and scopes.
/// </summary>
public sealed class MicrosoftClaimMapperTests
{
    /// <summary>
    /// Maps <c>preferred_username</c> from <c>email</c> when it is missing.
    /// </summary>
    [Fact]
    public void MapClaims_AddsPreferredUsername_FromEmail()
    {
        // Arrange
        var identity = new ClaimsIdentity(
        [
            new Claim("sub", "ms-uid-123"),
            new Claim("email", "user@contoso.com"),
            new Claim("name", "Test User"),
        ],
        "Bearer");
        var principal = new ClaimsPrincipal(identity);

        // Act
        MicrosoftClaimMapper.MapClaims(principal);

        // Assert
        var preferredUsername = identity.FindFirst("preferred_username");
        Assert.NotNull(preferredUsername);
        Assert.Equal("user@contoso.com", preferredUsername.Value);
    }

    /// <summary>
    /// Does not overwrite existing <c>preferred_username</c> claim.
    /// Microsoft v2 tokens often include <c>preferred_username</c> natively.
    /// </summary>
    [Fact]
    public void MapClaims_DoesNotOverwrite_ExistingPreferredUsername()
    {
        // Arrange
        var identity = new ClaimsIdentity(
        [
            new Claim("sub", "ms-uid-123"),
            new Claim("email", "user@contoso.com"),
            new Claim("preferred_username", "existing@contoso.com"),
        ],
        "Bearer");
        var principal = new ClaimsPrincipal(identity);

        // Act
        MicrosoftClaimMapper.MapClaims(principal);

        // Assert
        var claims = identity.FindAll("preferred_username").ToList();
        Assert.Single(claims);
        Assert.Equal("existing@contoso.com", claims[0].Value);
    }

    /// <summary>
    /// Does nothing when email claim is missing.
    /// </summary>
    [Fact]
    public void MapClaims_DoesNothing_WhenEmailMissing()
    {
        // Arrange
        var identity = new ClaimsIdentity(
        [
            new Claim("sub", "ms-uid-123"),
            new Claim("name", "Test User"),
        ],
        "Bearer");
        var principal = new ClaimsPrincipal(identity);

        // Act
        MicrosoftClaimMapper.MapClaims(principal);

        // Assert
        Assert.Null(identity.FindFirst("preferred_username"));
    }

    /// <summary>
    /// Handles null principal gracefully.
    /// </summary>
    [Fact]
    public void MapClaims_HandlesNullPrincipal()
    {
        // Act & Assert — should not throw
        MicrosoftClaimMapper.MapClaims(null);
    }

    /// <summary>
    /// Handles principal with no identity gracefully.
    /// </summary>
    [Fact]
    public void MapClaims_HandlesNoIdentity()
    {
        // Arrange
        var principal = new ClaimsPrincipal();

        // Act & Assert — should not throw
        MicrosoftClaimMapper.MapClaims(principal);
    }

    /// <summary>
    /// Preserves all existing claims when mapping.
    /// </summary>
    [Fact]
    public void MapClaims_PreservesExistingClaims()
    {
        // Arrange
        var identity = new ClaimsIdentity(
        [
            new Claim("sub", "ms-uid-123"),
            new Claim("oid", "ms-object-id-456"),
            new Claim("email", "user@contoso.com"),
            new Claim("name", "Test User"),
            new Claim("tid", "tenant-id-789"),
        ],
        "Bearer");
        var principal = new ClaimsPrincipal(identity);

        // Act
        MicrosoftClaimMapper.MapClaims(principal);

        // Assert
        Assert.Equal("ms-uid-123", identity.FindFirst("sub")?.Value);
        Assert.Equal("ms-object-id-456", identity.FindFirst("oid")?.Value);
        Assert.Equal("user@contoso.com", identity.FindFirst("email")?.Value);
        Assert.Equal("Test User", identity.FindFirst("name")?.Value);
        Assert.Equal("tenant-id-789", identity.FindFirst("tid")?.Value);
    }

    /// <summary>
    /// Does not add <c>preferred_username</c> when email is empty.
    /// </summary>
    [Fact]
    public void MapClaims_DoesNotAddPreferredUsername_WhenEmailEmpty()
    {
        // Arrange
        var identity = new ClaimsIdentity(
        [
            new Claim("sub", "ms-uid-123"),
            new Claim("email", string.Empty),
        ],
        "Bearer");
        var principal = new ClaimsPrincipal(identity);

        // Act
        MicrosoftClaimMapper.MapClaims(principal);

        // Assert
        Assert.Null(identity.FindFirst("preferred_username"));
    }
}
