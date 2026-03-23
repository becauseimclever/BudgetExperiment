// <copyright file="DomainExceptionType.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Common;

/// <summary>
/// Classifies a <see cref="DomainException"/> so that infrastructure and API layers can map it
/// to an appropriate HTTP status code without relying on message-text inspection.
/// </summary>
public enum DomainExceptionType
{
    /// <summary>
    /// A domain validation rule was violated (maps to HTTP 400).
    /// </summary>
    Validation = 0,

    /// <summary>
    /// A requested resource does not exist (maps to HTTP 404).
    /// </summary>
    NotFound = 1,

    /// <summary>
    /// An operation was attempted on an entity in an invalid state (maps to HTTP 422).
    /// </summary>
    InvalidState = 2,
}
