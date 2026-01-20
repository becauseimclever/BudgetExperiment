// <copyright file="ChatContextService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Represents context information from the current page for the chat assistant.
/// </summary>
public class ChatPageContext
{
    /// <summary>
    /// Gets or sets the current account ID, if viewing an account-related page.
    /// </summary>
    public Guid? CurrentAccountId { get; set; }

    /// <summary>
    /// Gets or sets the current account name, if viewing an account-related page.
    /// </summary>
    public string? CurrentAccountName { get; set; }

    /// <summary>
    /// Gets or sets the current category ID, if viewing a category-related page.
    /// </summary>
    public Guid? CurrentCategoryId { get; set; }

    /// <summary>
    /// Gets or sets the current category name, if viewing a category-related page.
    /// </summary>
    public string? CurrentCategoryName { get; set; }

    /// <summary>
    /// Gets or sets the page type (e.g., "transactions", "transfers", "recurring").
    /// </summary>
    public string? PageType { get; set; }

    /// <summary>
    /// Gets a summary of the context for the AI prompt.
    /// </summary>
    /// <returns>A human-readable context summary.</returns>
    public string GetContextSummary()
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(CurrentAccountName))
        {
            parts.Add($"Currently viewing account: {CurrentAccountName}");
        }

        if (!string.IsNullOrEmpty(CurrentCategoryName))
        {
            parts.Add($"Currently viewing category: {CurrentCategoryName}");
        }

        if (!string.IsNullOrEmpty(PageType))
        {
            parts.Add($"On the {PageType} page");
        }

        return parts.Count > 0 ? string.Join(". ", parts) : string.Empty;
    }
}

/// <summary>
/// Service for tracking page context to provide to the chat assistant.
/// </summary>
public interface IChatContextService
{
    /// <summary>
    /// Gets the current page context.
    /// </summary>
    ChatPageContext CurrentContext { get; }

    /// <summary>
    /// Occurs when the context changes.
    /// </summary>
    event EventHandler? ContextChanged;

    /// <summary>
    /// Sets the current account context.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="accountName">The account name.</param>
    void SetAccountContext(Guid? accountId, string? accountName);

    /// <summary>
    /// Sets the current category context.
    /// </summary>
    /// <param name="categoryId">The category ID.</param>
    /// <param name="categoryName">The category name.</param>
    void SetCategoryContext(Guid? categoryId, string? categoryName);

    /// <summary>
    /// Sets the current page type.
    /// </summary>
    /// <param name="pageType">The page type identifier.</param>
    void SetPageType(string? pageType);

    /// <summary>
    /// Clears all context.
    /// </summary>
    void ClearContext();
}

/// <summary>
/// Implementation of <see cref="IChatContextService"/>.
/// </summary>
public class ChatContextService : IChatContextService
{
    /// <inheritdoc />
    public ChatPageContext CurrentContext { get; } = new();

    /// <inheritdoc />
    public event EventHandler? ContextChanged;

    /// <inheritdoc />
    public void SetAccountContext(Guid? accountId, string? accountName)
    {
        CurrentContext.CurrentAccountId = accountId;
        CurrentContext.CurrentAccountName = accountName;
        OnContextChanged();
    }

    /// <inheritdoc />
    public void SetCategoryContext(Guid? categoryId, string? categoryName)
    {
        CurrentContext.CurrentCategoryId = categoryId;
        CurrentContext.CurrentCategoryName = categoryName;
        OnContextChanged();
    }

    /// <inheritdoc />
    public void SetPageType(string? pageType)
    {
        CurrentContext.PageType = pageType;
        OnContextChanged();
    }

    /// <inheritdoc />
    public void ClearContext()
    {
        CurrentContext.CurrentAccountId = null;
        CurrentContext.CurrentAccountName = null;
        CurrentContext.CurrentCategoryId = null;
        CurrentContext.CurrentCategoryName = null;
        CurrentContext.PageType = null;
        OnContextChanged();
    }

    private void OnContextChanged()
    {
        ContextChanged?.Invoke(this, EventArgs.Empty);
    }
}
