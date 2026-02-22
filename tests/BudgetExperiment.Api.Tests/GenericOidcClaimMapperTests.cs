// <copyright file="GenericOidcClaimMapperTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Security.Claims;

using BudgetExperiment.Api.Authentication;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Unit tests for <see cref="GenericOidcClaimMapper"/> claim mapping logic.
/// Generic OIDC providers (Keycloak, Auth0, Okta, etc.) may use non-standard
/// claim names. This mapper applies configurable claim mappings and ensures
/// the application's required <c>preferred_username</c> claim is present.
/// </summary>
public sealed class GenericOidcClaimMapperTests
{
    /// <summary>
    /// Applies configured claim mappings to the principal.
    /// </summary>
    [Fact]
    public void MapClaims_AppliesConfiguredMappings()
    {
        // Arrange
        var identity = new ClaimsIdentity(
        [
            new Claim("sub", "user-123"),
            new Claim("nickname", "testuser"),
        ], "Bearer");
        var principal = new ClaimsPrincipal(identity);
        var mappings = new Dictionary<string, string>
        {
            ["nickname"] = "preferred_username",
        };

        // Act
        GenericOidcClaimMapper.MapClaims(principal, mappings);

        // Assert
        var preferredUsername = identity.FindFirst("preferred_username");
        Assert.NotNull(preferredUsername);
        Assert.Equal("testuser", preferredUsername.Value);
    }

    /// <summary>
    /// Falls back to email → preferred_username when no explicit mapping matches.
    /// </summary>
    [Fact]
    public void MapClaims_FallsBackToEmail_WhenNoMappingForPreferredUsername()
    {
        // Arrange
        var identity = new ClaimsIdentity(
        [
            new Claim("sub", "user-123"),
            new Claim("email", "user@example.com"),
        ], "Bearer");
        var principal = new ClaimsPrincipal(identity);
        var mappings = new Dictionary<string, string>();

        // Act
        GenericOidcClaimMapper.MapClaims(principal, mappings);

        // Assert
        var preferredUsername = identity.FindFirst("preferred_username");
        Assert.NotNull(preferredUsername);
        Assert.Equal("user@example.com", preferredUsername.Value);
    }

    /// <summary>
    /// Does not overwrite existing preferred_username claim.
    /// </summary>
    [Fact]
    public void MapClaims_DoesNotOverwrite_ExistingPreferredUsername()
    {
        // Arrange
        var identity = new ClaimsIdentity(
        [
            new Claim("sub", "user-123"),
            new Claim("email", "user@example.com"),
            new Claim("preferred_username", "existing-user"),
        ], "Bearer");
        var principal = new ClaimsPrincipal(identity);
        var mappings = new Dictionary<string, string>();

        // Act
        GenericOidcClaimMapper.MapClaims(principal, mappings);

        // Assert
        var claims = identity.FindAll("preferred_username").ToList();
        Assert.Single(claims);
        Assert.Equal("existing-user", claims[0].Value);
    }

    /// <summary>
    /// Applies multiple claim mappings simultaneously.
    /// </summary>
    [Fact]
    public void MapClaims_AppliesMultipleMappings()
    {
        // Arrange
        var identity = new ClaimsIdentity(
        [
            new Claim("sub", "user-123"),
            new Claim("login", "testuser"),
            new Claim("full_name", "Test User"),
        ], "Bearer");
        var principal = new ClaimsPrincipal(identity);
        var mappings = new Dictionary<string, string>
        {
            ["login"] = "preferred_username",
            ["full_name"] = "name",
        };

        // Act
        GenericOidcClaimMapper.MapClaims(principal, mappings);

        // Assert
        Assert.Equal("testuser", identity.FindFirst("preferred_username")?.Value);
        Assert.Equal("Test User", identity.FindFirst("name")?.Value);
    }

    /// <summary>
    /// Does not add target claim if source claim is missing.
    /// </summary>
    [Fact]
    public void MapClaims_SkipsMapping_WhenSourceClaimMissing()
    {
        // Arrange
        var identity = new ClaimsIdentity(
        [
            new Claim("sub", "user-123"),
        ], "Bearer");
        var principal = new ClaimsPrincipal(identity);
        var mappings = new Dictionary<string, string>
        {
            ["nickname"] = "preferred_username",
        };

        // Act
        GenericOidcClaimMapper.MapClaims(principal, mappings);

        // Assert
        Assert.Null(identity.FindFirst("preferred_username"));
    }

    /// <summary>
    /// Does not add target claim if it already exists (from mapping or original token).
    /// </summary>
    [Fact]
    public void MapClaims_SkipsMapping_WhenTargetClaimAlreadyExists()
    {
        // Arrange
        var identity = new ClaimsIdentity(
        [
            new Claim("sub", "user-123"),
            new Claim("nickname", "from-nickname"),
            new Claim("preferred_username", "already-exists"),
        ], "Bearer");
        var principal = new ClaimsPrincipal(identity);
        var mappings = new Dictionary<string, string>
        {
            ["nickname"] = "preferred_username",
        };

        // Act
        GenericOidcClaimMapper.MapClaims(principal, mappings);

        // Assert
        var claims = identity.FindAll("preferred_username").ToList();
        Assert.Single(claims);
        Assert.Equal("already-exists", claims[0].Value);
    }

    /// <summary>
    /// Handles null principal gracefully.
    /// </summary>
    [Fact]
    public void MapClaims_HandlesNullPrincipal()
    {
        // Act & Assert — should not throw
        GenericOidcClaimMapper.MapClaims(null, new Dictionary<string, string>());
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
        GenericOidcClaimMapper.MapClaims(principal, new Dictionary<string, string>());
    }

    /// <summary>
    /// Handles null mappings dictionary by only applying the email fallback.
    /// </summary>
    [Fact]
    public void MapClaims_HandlesNullMappings()
    {
        // Arrange
        var identity = new ClaimsIdentity(
        [
            new Claim("sub", "user-123"),
            new Claim("email", "user@example.com"),
        ], "Bearer");
        var principal = new ClaimsPrincipal(identity);

        // Act — should not throw
        GenericOidcClaimMapper.MapClaims(principal, null);

        // Assert — email fallback still works
        Assert.Equal("user@example.com", identity.FindFirst("preferred_username")?.Value);
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
            new Claim("sub", "user-123"),
            new Claim("email", "user@example.com"),
            new Claim("custom_claim", "custom_value"),
        ], "Bearer");
        var principal = new ClaimsPrincipal(identity);
        var mappings = new Dictionary<string, string>
        {
            ["email"] = "preferred_username",
        };

        // Act
        GenericOidcClaimMapper.MapClaims(principal, mappings);

        // Assert
        Assert.Equal("user-123", identity.FindFirst("sub")?.Value);
        Assert.Equal("user@example.com", identity.FindFirst("email")?.Value);
        Assert.Equal("custom_value", identity.FindFirst("custom_claim")?.Value);
        Assert.Equal("user@example.com", identity.FindFirst("preferred_username")?.Value);
    }

    /// <summary>
    /// Empty mappings dictionary still applies the email fallback.
    /// </summary>
    [Fact]
    public void MapClaims_WithEmptyMappings_AppliesEmailFallback()
    {
        // Arrange
        var identity = new ClaimsIdentity(
        [
            new Claim("sub", "user-123"),
            new Claim("email", "user@auth0.com"),
        ], "Bearer");
        var principal = new ClaimsPrincipal(identity);

        // Act
        GenericOidcClaimMapper.MapClaims(principal, new Dictionary<string, string>());

        // Assert
        Assert.Equal("user@auth0.com", identity.FindFirst("preferred_username")?.Value);
    }
}
