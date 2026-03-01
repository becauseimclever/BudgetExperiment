// <copyright file="CurrencyOption.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Models;

/// <summary>
/// Represents a currency option for display in the UI.
/// </summary>
/// <param name="Code">The ISO 4217 currency code (e.g., "USD").</param>
/// <param name="Name">The display name (e.g., "US Dollar").</param>
/// <param name="Symbol">The currency symbol (e.g., "$").</param>
public record CurrencyOption(string Code, string Name, string Symbol);
