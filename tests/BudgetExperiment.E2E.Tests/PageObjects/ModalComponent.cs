// <copyright file="ModalComponent.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.E2E.Tests.PageObjects;

/// <summary>
/// Page object for modal dialog interactions.
/// </summary>
public class ModalComponent
{
    private readonly IPage _page;
    private readonly string? _title;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModalComponent"/> class.
    /// </summary>
    /// <param name="page">The Playwright page instance.</param>
    /// <param name="title">Optional modal title to identify specific modals.</param>
    public ModalComponent(IPage page, string? title = null)
    {
        _page = page;
        _title = title;
    }

    /// <summary>
    /// Gets the modal container locator.
    /// </summary>
    public ILocator Container
    {
        get
        {
            if (_title != null)
            {
                return _page.Locator(".modal").Filter(new() { HasText = _title });
            }

            return _page.Locator(".modal");
        }
    }

    /// <summary>
    /// Gets the modal overlay/backdrop.
    /// </summary>
    public ILocator Overlay => _page.Locator(".modal-overlay, .modal-backdrop");

    /// <summary>
    /// Gets the close button.
    /// </summary>
    public ILocator CloseButton => Container.Locator(".modal-close, [aria-label='Close'], button:has-text('×')");

    /// <summary>
    /// Gets the modal title.
    /// </summary>
    public ILocator Title => Container.Locator(".modal-title, h2, h3").First;

    /// <summary>
    /// Waits for the modal to become visible.
    /// </summary>
    /// <param name="timeout">Maximum time to wait in milliseconds.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task WaitForVisibleAsync(int timeout = 5000)
    {
        await Container.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = timeout,
        });
    }

    /// <summary>
    /// Waits for the modal to become hidden.
    /// </summary>
    /// <param name="timeout">Maximum time to wait in milliseconds.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task WaitForHiddenAsync(int timeout = 5000)
    {
        await Container.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Hidden,
            Timeout = timeout,
        });
    }

    /// <summary>
    /// Checks if the modal is currently visible.
    /// </summary>
    /// <returns>True if the modal is visible.</returns>
    public async Task<bool> IsVisibleAsync()
    {
        return await Container.IsVisibleAsync();
    }

    /// <summary>
    /// Closes the modal by clicking the close button.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task CloseAsync()
    {
        await CloseButton.ClickAsync();
        await WaitForHiddenAsync();
    }

    /// <summary>
    /// Gets the modal title text.
    /// </summary>
    /// <returns>The modal title text.</returns>
    public async Task<string> GetTitleAsync()
    {
        return await Title.TextContentAsync() ?? string.Empty;
    }
}
