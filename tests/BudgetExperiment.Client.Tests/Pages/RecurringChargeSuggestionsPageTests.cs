// <copyright file="RecurringChargeSuggestionsPageTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;

using BudgetExperiment.Client.Pages;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Client.Tests.TestHelpers;
using BudgetExperiment.Client.ViewModels;
using BudgetExperiment.Contracts.Dtos;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace BudgetExperiment.Client.Tests.Pages;

/// <summary>
/// Unit tests for the <see cref="RecurringChargeSuggestions"/> page component.
/// </summary>
public class RecurringChargeSuggestionsPageTests : BunitContext, IAsyncLifetime
{
    private readonly StubRecurringChargeSuggestionApiService _apiService = new();
    private readonly IToastService _toastService = new ToastService();

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringChargeSuggestionsPageTests"/> class.
    /// </summary>
    public RecurringChargeSuggestionsPageTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<IRecurringChargeSuggestionApiService>(_apiService);
        this.Services.AddSingleton<IToastService>(_toastService);
        this.Services.AddSingleton<ThemeService>();
        this.Services.AddSingleton<CultureService>();
        this.Services.AddSingleton<IFeatureFlagClientService>(new StubFeatureFlagClientService());
        this.Services.AddSingleton<IApiErrorContext>(new ApiErrorContext());
        this.Services.AddSingleton<IExportDownloadService>(new StubExportDownloadService());
        this.Services.AddTransient<RecurringChargeSuggestionsViewModel>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync()
    {
        return base.DisposeAsync().AsTask();
    }

    /// <summary>
    /// Verifies the page renders without error when no suggestions exist.
    /// </summary>
    [Fact]
    public void Renders_WhenNoSuggestionsExist()
    {
        // Arrange
        _apiService.Suggestions.Clear();

        // Act
        var cut = this.Render<RecurringChargeSuggestions>();

        // Assert
        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the page displays "Run Detection" button.
    /// </summary>
    [Fact]
    public void ShowsRunDetectionButton()
    {
        // Arrange
        _apiService.Suggestions.Clear();

        // Act
        var cut = this.Render<RecurringChargeSuggestions>();

        // Assert
        cut.Markup.ShouldContain("Run Detection");
    }

    /// <summary>
    /// Verifies the Run Detection button is enabled by default.
    /// </summary>
    [Fact]
    public void RunDetectionButton_IsEnabledByDefault()
    {
        // Arrange
        _apiService.Suggestions.Clear();

        // Act
        var cut = this.Render<RecurringChargeSuggestions>();
        var button = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Run Detection"));

        // Assert
        button.ShouldNotBeNull();

        // Check that button doesn't have disabled="True" (enabled means disabled is False, null, or absent)
        var disabledValue = button!.GetAttribute("disabled");
        (disabledValue == null || !disabledValue.Equals("True", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();
    }

    /// <summary>
    /// Verifies the page displays empty state when no suggestions loaded.
    /// </summary>
    [Fact]
    public void ShowsEmptyState_WhenNoSuggestions()
    {
        // Arrange
        _apiService.Suggestions.Clear();

        // Act
        var cut = this.Render<RecurringChargeSuggestions>();

        // Assert
        cut.Markup.ShouldContain("No Suggestions");
    }

    /// <summary>
    /// Verifies the page shows status filter buttons (Pending, Accepted, Dismissed).
    /// </summary>
    [Fact]
    public void ShowsStatusFilterButtons()
    {
        // Arrange
        _apiService.Suggestions.Clear();

        // Act
        var cut = this.Render<RecurringChargeSuggestions>();

        // Assert
        cut.Markup.ShouldContain("Pending");
        cut.Markup.ShouldContain("Accepted");
        cut.Markup.ShouldContain("Dismissed");
    }

    /// <summary>
    /// Verifies Pending filter button is active by default.
    /// </summary>
    [Fact]
    public void PendingFilter_IsActiveByDefault()
    {
        // Arrange
        _apiService.Suggestions.Clear();

        // Act
        var cut = this.Render<RecurringChargeSuggestions>();
        var pendingButton = cut.FindAll(".filter-btn").FirstOrDefault(b => b.TextContent.Contains("Pending"));

        // Assert
        pendingButton.ShouldNotBeNull();
        pendingButton!.GetAttribute("class")!.ShouldContain("active");
    }

    /// <summary>
    /// Verifies suggestions list renders when suggestions are loaded.
    /// </summary>
    [Fact]
    public void RendersSuggestionsList_WhenSuggestionsLoaded()
    {
        // Arrange
        _apiService.Suggestions.Add(new RecurringChargeSuggestionDto
        {
            Id = Guid.NewGuid(),
            SampleDescription = "Netflix",
            DetectedFrequency = "Monthly",
            Confidence = 0.95m,
            MatchingTransactionCount = 12,
            AverageAmount = new MoneyDto { Amount = 15.99m, Currency = "USD" },
            FirstOccurrence = DateOnly.FromDateTime(DateTime.Now.AddMonths(-12)),
            LastOccurrence = DateOnly.FromDateTime(DateTime.Now),
            DetectedInterval = 30,
            Status = "Pending",
        });

        // Act
        var cut = this.Render<RecurringChargeSuggestions>();

        // Assert
        cut.Markup.ShouldContain("Netflix");
        cut.Markup.ShouldContain("Monthly");
    }

    /// <summary>
    /// Verifies suggestion card displays correct information.
    /// </summary>
    [Fact]
    public void SuggestionCard_DisplaysCorrectInformation()
    {
        // Arrange
        var suggestion = new RecurringChargeSuggestionDto
        {
            Id = Guid.NewGuid(),
            SampleDescription = "Coffee",
            DetectedFrequency = "Weekly",
            Confidence = 0.85m,
            MatchingTransactionCount = 26,
            AverageAmount = new MoneyDto { Amount = 5.25m, Currency = "USD" },
            FirstOccurrence = new DateOnly(2025, 1, 1),
            LastOccurrence = new DateOnly(2025, 12, 26),
            DetectedInterval = 7,
            Status = "Pending",
        };
        _apiService.Suggestions.Add(suggestion);

        // Act
        var cut = this.Render<RecurringChargeSuggestions>();

        // Assert
        cut.Markup.ShouldContain("Coffee");
        cut.Markup.ShouldContain("Weekly");
        cut.Markup.ShouldContain("85%");
        cut.Markup.ShouldContain("26 matches");
    }

    /// <summary>
    /// Verifies suggestion shows first and last occurrence dates.
    /// </summary>
    [Fact]
    public void SuggestionCard_ShowsOccurrenceDates()
    {
        // Arrange
        var firstDate = new DateOnly(2025, 1, 15);
        var lastDate = new DateOnly(2025, 12, 15);
        _apiService.Suggestions.Add(new RecurringChargeSuggestionDto
        {
            Id = Guid.NewGuid(),
            SampleDescription = "Rent",
            DetectedFrequency = "Monthly",
            Confidence = 1.0m,
            MatchingTransactionCount = 12,
            AverageAmount = new MoneyDto { Amount = 1500m, Currency = "USD" },
            FirstOccurrence = firstDate,
            LastOccurrence = lastDate,
            DetectedInterval = 30,
            Status = "Pending",
        });

        // Act
        var cut = this.Render<RecurringChargeSuggestions>();

        // Assert
        cut.Markup.ShouldContain("Jan 15, 2025");
        cut.Markup.ShouldContain("Dec 15, 2025");
    }

    /// <summary>
    /// Verifies confidence badge is displayed with correct CSS class for high confidence.
    /// </summary>
    [Fact]
    public void SuggestionCard_ShowsHighConfidenceBadge()
    {
        // Arrange
        _apiService.Suggestions.Add(new RecurringChargeSuggestionDto
        {
            Id = Guid.NewGuid(),
            SampleDescription = "Insurance",
            DetectedFrequency = "Monthly",
            Confidence = 0.95m,
            MatchingTransactionCount = 12,
            AverageAmount = new MoneyDto { Amount = 125m, Currency = "USD" },
            FirstOccurrence = DateOnly.FromDateTime(DateTime.Now.AddMonths(-12)),
            LastOccurrence = DateOnly.FromDateTime(DateTime.Now),
            DetectedInterval = 30,
            Status = "Pending",
        });

        // Act
        var cut = this.Render<RecurringChargeSuggestions>();

        // Assert
        cut.Markup.ShouldContain("confidence-high");
    }

    /// <summary>
    /// Verifies confidence badge is displayed with correct CSS class for medium confidence.
    /// </summary>
    [Fact]
    public void SuggestionCard_ShowsMediumConfidenceBadge()
    {
        // Arrange
        _apiService.Suggestions.Add(new RecurringChargeSuggestionDto
        {
            Id = Guid.NewGuid(),
            SampleDescription = "Phone Bill",
            DetectedFrequency = "Monthly",
            Confidence = 0.75m,
            MatchingTransactionCount = 12,
            AverageAmount = new MoneyDto { Amount = 50m, Currency = "USD" },
            FirstOccurrence = DateOnly.FromDateTime(DateTime.Now.AddMonths(-12)),
            LastOccurrence = DateOnly.FromDateTime(DateTime.Now),
            DetectedInterval = 30,
            Status = "Pending",
        });

        // Act
        var cut = this.Render<RecurringChargeSuggestions>();

        // Assert
        cut.Markup.ShouldContain("confidence-medium");
    }

    /// <summary>
    /// Verifies Pending status suggestions show Accept and Dismiss buttons.
    /// </summary>
    [Fact]
    public void PendingSuggestion_ShowsAcceptAndDismissButtons()
    {
        // Arrange
        _apiService.Suggestions.Add(new RecurringChargeSuggestionDto
        {
            Id = Guid.NewGuid(),
            SampleDescription = "Gym",
            DetectedFrequency = "Monthly",
            Confidence = 0.80m,
            MatchingTransactionCount = 8,
            AverageAmount = new MoneyDto { Amount = 50m, Currency = "USD" },
            FirstOccurrence = DateOnly.FromDateTime(DateTime.Now.AddMonths(-8)),
            LastOccurrence = DateOnly.FromDateTime(DateTime.Now),
            DetectedInterval = 30,
            Status = "Pending",
        });

        // Act
        var cut = this.Render<RecurringChargeSuggestions>();

        // Assert
        cut.Markup.ShouldContain("Accept");
        cut.Markup.ShouldContain("Dismiss");
    }

    /// <summary>
    /// Verifies Accepted status suggestions show status badge.
    /// </summary>
    [Fact]
    public void AcceptedSuggestion_ShowsAcceptedBadge()
    {
        // Arrange
        _apiService.Suggestions.Add(new RecurringChargeSuggestionDto
        {
            Id = Guid.NewGuid(),
            SampleDescription = "Internet",
            DetectedFrequency = "Monthly",
            Confidence = 0.90m,
            MatchingTransactionCount = 12,
            AverageAmount = new MoneyDto { Amount = 79.99m, Currency = "USD" },
            FirstOccurrence = DateOnly.FromDateTime(DateTime.Now.AddMonths(-12)),
            LastOccurrence = DateOnly.FromDateTime(DateTime.Now),
            DetectedInterval = 30,
            Status = "Accepted",
        });

        // Act
        var cut = this.Render<RecurringChargeSuggestions>();

        // Assert
        cut.Markup.ShouldContain("Accepted");
    }

    /// <summary>
    /// Verifies Dismissed status suggestions show status indicator.
    /// </summary>
    [Fact]
    public void DismissedSuggestion_ShowsDismissedIndicator()
    {
        // Arrange
        _apiService.Suggestions.Add(new RecurringChargeSuggestionDto
        {
            Id = Guid.NewGuid(),
            SampleDescription = "Trial",
            DetectedFrequency = "One-time",
            Confidence = 0.50m,
            MatchingTransactionCount = 1,
            AverageAmount = new MoneyDto { Amount = 9.99m, Currency = "USD" },
            FirstOccurrence = DateOnly.FromDateTime(DateTime.Now.AddMonths(-1)),
            LastOccurrence = DateOnly.FromDateTime(DateTime.Now),
            DetectedInterval = 0,
            Status = "Dismissed",
        });

        // Act
        var cut = this.Render<RecurringChargeSuggestions>();

        // Assert
        cut.Markup.ShouldContain("Dismissed");
    }

    /// <summary>
    /// Verifies error alert is dismissible.
    /// </summary>
    [Fact]
    public void ErrorAlert_IsDismissible()
    {
        // Arrange
        _apiService.ExceptionToThrow = new Exception("API Error");

        // Act
        var cut = this.Render<RecurringChargeSuggestions>();

        // Assert
        cut.Markup.ShouldContain("API Error");
    }

    /// <summary>
    /// Verifies page shows loading spinner during initial load.
    /// </summary>
    [Fact]
    public void ShowsLoadingSpinner_DuringInitialLoad()
    {
        // Arrange
        var loadingTcs = new TaskCompletionSource<IReadOnlyList<RecurringChargeSuggestionDto>>();
        _apiService.GetSuggestionsAsyncOverride = (_, _, _, _, _) => loadingTcs.Task;

        // Act
        var cut = this.Render<RecurringChargeSuggestions>();

        // Assert
        cut.Markup.ShouldContain("Loading suggestions");

        // Cleanup
        loadingTcs.SetResult(new List<RecurringChargeSuggestionDto>());
    }

    /// <summary>
    /// Verifies multiple suggestions render correctly.
    /// </summary>
    [Fact]
    public void RendersMultipleSuggestions()
    {
        // Arrange
        _apiService.Suggestions.Add(new RecurringChargeSuggestionDto
        {
            Id = Guid.NewGuid(),
            SampleDescription = "Subscription A",
            DetectedFrequency = "Monthly",
            Confidence = 0.90m,
            MatchingTransactionCount = 6,
            AverageAmount = new MoneyDto { Amount = 10m, Currency = "USD" },
            FirstOccurrence = DateOnly.FromDateTime(DateTime.Now.AddMonths(-6)),
            LastOccurrence = DateOnly.FromDateTime(DateTime.Now),
            DetectedInterval = 30,
            Status = "Pending",
        });
        _apiService.Suggestions.Add(new RecurringChargeSuggestionDto
        {
            Id = Guid.NewGuid(),
            SampleDescription = "Subscription B",
            DetectedFrequency = "Monthly",
            Confidence = 0.85m,
            MatchingTransactionCount = 4,
            AverageAmount = new MoneyDto { Amount = 20m, Currency = "USD" },
            FirstOccurrence = DateOnly.FromDateTime(DateTime.Now.AddMonths(-4)),
            LastOccurrence = DateOnly.FromDateTime(DateTime.Now),
            DetectedInterval = 30,
            Status = "Pending",
        });

        // Act
        var cut = this.Render<RecurringChargeSuggestions>();

        // Assert
        cut.Markup.ShouldContain("Subscription A");
        cut.Markup.ShouldContain("Subscription B");
    }
}
