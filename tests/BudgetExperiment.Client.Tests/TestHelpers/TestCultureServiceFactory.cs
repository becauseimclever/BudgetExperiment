// <copyright file="TestCultureServiceFactory.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Services;

using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Tests.TestHelpers;

/// <summary>
/// Factory for creating <see cref="CultureService"/> instances pre-initialized with a
/// specific culture, suitable for bUnit tests that assert on culture-sensitive formatting.
/// </summary>
internal static class TestCultureServiceFactory
{
    /// <summary>
    /// Creates and initializes a <see cref="CultureService"/> that reports the given
    /// <paramref name="languageTag"/> as the browser culture (e.g., "de-DE", "en-US").
    /// </summary>
    /// <param name="languageTag">BCP-47 language tag to simulate (e.g., "de-DE").</param>
    /// <returns>
    /// A task that resolves to a fully initialized <see cref="CultureService"/>.
    /// </returns>
    public static async Task<CultureService> CreateAsync(string languageTag)
    {
        var service = new CultureService(new FixedLanguageJSRuntime(languageTag));
        await service.InitializeAsync();
        return service;
    }

    /// <summary>
    /// Minimal <see cref="IJSRuntime"/> that returns a fixed <see cref="CultureDetectionResult"/>
    /// for the <c>detectCulture</c> JS module call made by <see cref="CultureService"/>.
    /// </summary>
    private sealed class FixedLanguageJSRuntime : IJSRuntime
    {
        private readonly string _languageTag;

        public FixedLanguageJSRuntime(string languageTag)
        {
            _languageTag = languageTag;
        }

        /// <inheritdoc/>
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            if (identifier == "import")
            {
                IJSObjectReference module = new CultureModule(_languageTag);
                return new ValueTask<TValue>((TValue)(object)module);
            }

            return new ValueTask<TValue>(default(TValue)!);
        }

        /// <inheritdoc/>
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => InvokeAsync<TValue>(identifier, args);

        private sealed class CultureModule : IJSObjectReference
        {
            private readonly string _languageTag;

            public CultureModule(string languageTag)
            {
                _languageTag = languageTag;
            }

            /// <inheritdoc/>
            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            {
                if (identifier == "detectCulture")
                {
                    var result = new CultureDetectionResult { Language = _languageTag, TimeZone = "UTC" };
                    return new ValueTask<TValue>((TValue)(object)result);
                }

                return new ValueTask<TValue>(default(TValue)!);
            }

            /// <inheritdoc/>
            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
                => InvokeAsync<TValue>(identifier, args);

            /// <inheritdoc/>
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
