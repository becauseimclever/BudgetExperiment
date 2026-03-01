// <copyright file="ClientConfigDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Configuration settings exposed to the Blazor WebAssembly client.
/// This DTO contains ONLY non-secret, client-appropriate settings.
/// </summary>
public sealed class ClientConfigDto
{
    /// <summary>
    /// Gets or sets the authentication configuration.
    /// </summary>
    public required AuthenticationConfigDto Authentication { get; init; }
}
