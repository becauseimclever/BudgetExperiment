// <copyright file="AccountInfo.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Chat;

/// <summary>
/// Information about an account for natural language parsing.
/// </summary>
/// <param name="Id">The account identifier.</param>
/// <param name="Name">The account name.</param>
/// <param name="Type">The account type.</param>
public sealed record AccountInfo(Guid Id, string Name, AccountType Type);
