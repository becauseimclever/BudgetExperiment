// <copyright file="FeedbackRequest.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Request DTO for providing feedback on a suggestion.
/// </summary>
public sealed class FeedbackRequest
{
    /// <summary>
    /// Gets or sets a value indicating whether the feedback is positive.
    /// </summary>
    public bool IsPositive
    {
        get; set;
    }
}
