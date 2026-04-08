// <copyright file="CategoryInfo.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Chat;

/// <summary>
/// Information about a category for natural language parsing.
/// </summary>
/// <param name="Id">The category identifier.</param>
/// <param name="Name">The category name.</param>
/// <param name="KakeiboCategory">The Kakeibo category for the budget category.</param>
public sealed record CategoryInfo(Guid Id, string Name, KakeiboCategory? KakeiboCategory = null);
