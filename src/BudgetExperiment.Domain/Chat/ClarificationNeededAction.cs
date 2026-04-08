// <copyright file="ClarificationNeededAction.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Chat;

/// <summary>
/// Action indicating clarification is needed from the user.
/// </summary>
public sealed record ClarificationNeededAction : ChatAction
{
    /// <inheritdoc/>
    public override ChatActionType Type => ChatActionType.ClarificationNeeded;

    /// <summary>
    /// Gets the question to ask the user.
    /// </summary>
    public string Question { get; init; } = string.Empty;

    /// <summary>
    /// Gets the clarification request type.
    /// </summary>
    public ClarificationNeededActionType ClarificationType { get; init; } = ClarificationNeededActionType.General;

    /// <summary>
    /// Gets the name of the field that needs clarification.
    /// </summary>
    public string FieldName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the available options to choose from.
    /// </summary>
    public IReadOnlyList<ClarificationOption> Options { get; init; } = Array.Empty<ClarificationOption>();

    /// <inheritdoc/>
    public override string GetPreviewSummary() => this.Question;
}
