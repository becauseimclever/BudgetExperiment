// <copyright file="ToastService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Manages toast notifications with auto-dismiss functionality.
/// </summary>
public sealed class ToastService : IToastService, IDisposable
{
    private const int DefaultDurationMs = 4000;
    private readonly List<ToastItem> _toasts = [];
    private readonly Dictionary<Guid, Timer> _timers = [];

    /// <inheritdoc/>
    public event Action? OnChange;

    /// <inheritdoc/>
    public IReadOnlyList<ToastItem> Toasts => _toasts.AsReadOnly();

    /// <inheritdoc/>
    public void ShowSuccess(string message, string? title = null)
    {
        Show(ToastLevel.Success, message, title);
    }

    /// <inheritdoc/>
    public void ShowError(string message, string? title = null)
    {
        Show(ToastLevel.Error, message, title);
    }

    /// <inheritdoc/>
    public void ShowInfo(string message, string? title = null)
    {
        Show(ToastLevel.Info, message, title);
    }

    /// <inheritdoc/>
    public void ShowWarning(string message, string? title = null)
    {
        Show(ToastLevel.Warning, message, title);
    }

    /// <inheritdoc/>
    public void Remove(Guid id)
    {
        var toast = _toasts.FirstOrDefault(t => t.Id == id);
        if (toast is not null)
        {
            _toasts.Remove(toast);

            if (_timers.TryGetValue(id, out var timer))
            {
                timer.Dispose();
                _timers.Remove(id);
            }

            OnChange?.Invoke();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (var timer in _timers.Values)
        {
            timer.Dispose();
        }

        _timers.Clear();
    }

    private void Show(ToastLevel level, string message, string? title)
    {
        var toast = new ToastItem(
            Guid.NewGuid(),
            level,
            message,
            title,
            DateTime.UtcNow);

        _toasts.Add(toast);

        var timer = new Timer(
            _ => Remove(toast.Id),
            null,
            DefaultDurationMs,
            Timeout.Infinite);

        _timers[toast.Id] = timer;

        OnChange?.Invoke();
    }
}
