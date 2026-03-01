// <copyright file="ImportValidationResult.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Import;

/// <summary>
/// Result of import request validation.
/// </summary>
/// <param name="Errors">The list of validation error messages.</param>
/// <param name="IsBadRequest">
/// When <c>true</c>, the errors are collection-level (400 Bad Request).
/// When <c>false</c> and errors exist, they are field-level (422 Unprocessable Entity).
/// </param>
public sealed record ImportValidationResult(IReadOnlyList<string> Errors, bool IsBadRequest)
{
    /// <summary>
    /// Gets a value indicating whether the request is valid.
    /// </summary>
    public bool IsValid => Errors.Count == 0;
}
