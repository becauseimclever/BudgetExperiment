// <copyright file="VersionInfoDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Version and build information for the API.
/// </summary>
/// <param name="Version">The semantic version of the application.</param>
/// <param name="BuildDateUtc">The UTC date and time when the application was built.</param>
/// <param name="CommitHash">The Git commit hash (if available).</param>
/// <param name="Environment">The hosting environment name (e.g., Production, Development).</param>
public record VersionInfoDto(
    string Version,
    DateTime BuildDateUtc,
    string? CommitHash,
    string Environment);
