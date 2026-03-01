// <copyright file="CategoryInfo.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Chat;

/// <summary>
/// Information about a category for natural language parsing.
/// </summary>
/// <param name="Id">The category identifier.</param>
/// <param name="Name">The category name.</param>
public sealed record CategoryInfo(Guid Id, string Name);
