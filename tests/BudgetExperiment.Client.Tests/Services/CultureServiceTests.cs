// <copyright file="CultureServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;

using BudgetExperiment.Client.Services;

using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Tests.Services;

/// <summary>
/// Unit tests for the <see cref="CultureService"/> class.
/// </summary>
public sealed class CultureServiceTests
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CultureServiceTests"/> class.
    /// </summary>
    public CultureServiceTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
    }

    /// <summary>
    /// Verifies that CurrentCulture defaults to en-US before initialization.
    /// </summary>
    [Fact]
    public void CurrentCulture_BeforeInit_DefaultsToEnUS()
    {
        var sut = new CultureService(new StubJSRuntime());

        Assert.Equal("en-US", sut.CurrentCulture.Name);
    }

    /// <summary>
    /// Verifies that CurrentTimeZone defaults to UTC before initialization.
    /// </summary>
    [Fact]
    public void CurrentTimeZone_BeforeInit_DefaultsToUtc()
    {
        var sut = new CultureService(new StubJSRuntime());

        Assert.Equal("UTC", sut.CurrentTimeZone);
    }

    /// <summary>
    /// Verifies that InitializeAsync sets culture from JS interop result.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_SetsCultureFromBrowser()
    {
        var jsRuntime = new CultureStubJSRuntime("fr-FR", "Europe/Paris");
        var sut = new CultureService(jsRuntime);

        await sut.InitializeAsync();

        Assert.Equal("fr-FR", sut.CurrentCulture.Name);
        Assert.Equal("Europe/Paris", sut.CurrentTimeZone);
    }

    /// <summary>
    /// Verifies that InitializeAsync falls back to en-US when JS interop fails.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_JsFailure_FallsBackToEnUS()
    {
        var jsRuntime = new FailingJSRuntime();
        var sut = new CultureService(jsRuntime);

        await sut.InitializeAsync();

        Assert.Equal("en-US", sut.CurrentCulture.Name);
        Assert.Equal("UTC", sut.CurrentTimeZone);
    }

    /// <summary>
    /// Verifies that InitializeAsync is idempotent.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_CalledTwice_DoesNotReinitialize()
    {
        var jsRuntime = new CultureStubJSRuntime("de-DE", "Europe/Berlin");
        var sut = new CultureService(jsRuntime);

        await sut.InitializeAsync();
        Assert.Equal("de-DE", sut.CurrentCulture.Name);

        // Change what JS would return (simulates a different answer on second call)
        jsRuntime.Language = "ja-JP";
        jsRuntime.TimeZone = "Asia/Tokyo";

        await sut.InitializeAsync();

        // Should still be de-DE because init is idempotent
        Assert.Equal("de-DE", sut.CurrentCulture.Name);
    }

    /// <summary>
    /// Verifies that FormatCurrency uses the current culture.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task FormatCurrency_UsesCurrentCulture()
    {
        var jsRuntime = new CultureStubJSRuntime("en-US", "America/New_York");
        var sut = new CultureService(jsRuntime);
        await sut.InitializeAsync();

        var result = 42.50m.ToString("C", sut.CurrentCulture);

        Assert.Equal("$42.50", result);
    }

    /// <summary>
    /// Verifies that Dispose can be called without error.
    /// </summary>
    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var sut = new CultureService(new StubJSRuntime());
        var ex = Record.Exception(() => sut.Dispose());
        Assert.Null(ex);
    }

    /// <summary>
    /// Verifies that DisposeAsync can be called without error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DisposeAsync_DoesNotThrow()
    {
        var sut = new CultureService(new StubJSRuntime());
        var ex = await Record.ExceptionAsync(async () => await sut.DisposeAsync());
        Assert.Null(ex);
    }

    /// <summary>
    /// Verifies handling of an invalid language code from browser.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_InvalidLanguage_FallsBackToEnUS()
    {
        var jsRuntime = new CultureStubJSRuntime("not-a-locale", "America/New_York");
        var sut = new CultureService(jsRuntime);

        await sut.InitializeAsync();

        Assert.Equal("en-US", sut.CurrentCulture.Name);
        Assert.Equal("America/New_York", sut.CurrentTimeZone);
    }

    /// <summary>
    /// Stub JavaScript runtime that returns defaults without actual JS interop.
    /// </summary>
    private sealed class StubJSRuntime : IJSRuntime
    {
        /// <inheritdoc/>
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return new ValueTask<TValue>(default(TValue)!);
        }

        /// <inheritdoc/>
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            return new ValueTask<TValue>(default(TValue)!);
        }
    }

    /// <summary>
    /// Stub JS runtime that returns configurable culture detection results.
    /// </summary>
    private sealed class CultureStubJSRuntime : IJSRuntime
    {
        private IJSObjectReference? _module;

        public CultureStubJSRuntime(string language, string timeZone)
        {
            Language = language;
            TimeZone = timeZone;
        }

        public string Language
        {
            get; set;
        }

        public string TimeZone
        {
            get; set;
        }

        /// <inheritdoc/>
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            if (identifier == "import")
            {
                _module = new CultureModuleStub(this);
                return new ValueTask<TValue>((TValue)(object)_module);
            }

            return new ValueTask<TValue>(default(TValue)!);
        }

        /// <inheritdoc/>
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            return InvokeAsync<TValue>(identifier, args);
        }

        private sealed class CultureModuleStub : IJSObjectReference
        {
            private readonly CultureStubJSRuntime _parent;

            public CultureModuleStub(CultureStubJSRuntime parent)
            {
                _parent = parent;
            }

            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            {
                if (identifier == "detectCulture")
                {
                    var result = new CultureDetectionResult
                    {
                        Language = _parent.Language,
                        TimeZone = _parent.TimeZone,
                    };
                    return new ValueTask<TValue>((TValue)(object)result);
                }

                return new ValueTask<TValue>(default(TValue)!);
            }

            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            {
                return InvokeAsync<TValue>(identifier, args);
            }

            public ValueTask DisposeAsync()
            {
                return ValueTask.CompletedTask;
            }
        }
    }

    /// <summary>
    /// Stub JS runtime that throws JSException to simulate interop failure.
    /// </summary>
    private sealed class FailingJSRuntime : IJSRuntime
    {
        /// <inheritdoc/>
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            throw new JSException("JS interop not available");
        }

        /// <inheritdoc/>
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            throw new JSException("JS interop not available");
        }
    }
}
