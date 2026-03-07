// <copyright file="VersionServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Client.Tests.Services;

/// <summary>
/// Unit tests for the <see cref="VersionService"/> class.
/// </summary>
public sealed class VersionServiceTests
{
    /// <summary>
    /// Verifies that CurrentVersion returns the default placeholder before loading.
    /// </summary>
    [Fact]
    public void CurrentVersion_BeforeLoad_ReturnsPlaceholder()
    {
        // Arrange
        var sut = CreateService();

        // Assert
        Assert.Equal("loading...", sut.CurrentVersion);
    }

    /// <summary>
    /// Verifies that IsLoaded returns false before LoadVersionAsync is called.
    /// </summary>
    [Fact]
    public void IsLoaded_BeforeLoad_ReturnsFalse()
    {
        // Arrange
        var sut = CreateService();

        // Assert
        Assert.False(sut.IsLoaded);
    }

    /// <summary>
    /// Verifies that VersionInfo returns null before loading.
    /// </summary>
    [Fact]
    public void VersionInfo_BeforeLoad_ReturnsNull()
    {
        // Arrange
        var sut = CreateService();

        // Assert
        Assert.Null(sut.VersionInfo);
    }

    /// <summary>
    /// Verifies that LoadVersionAsync populates version properties on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task LoadVersionAsync_OnSuccess_PopulatesProperties()
    {
        // Arrange
        var dto = new VersionInfoDto("1.2.3", DateTime.UtcNow, "abc123", "Development");
        var sut = CreateService(dto);

        // Act
        await sut.LoadVersionAsync();

        // Assert
        Assert.True(sut.IsLoaded);
        Assert.Equal("1.2.3", sut.CurrentVersion);
        Assert.NotNull(sut.VersionInfo);
        Assert.Equal("abc123", sut.VersionInfo!.CommitHash);
    }

    /// <summary>
    /// Verifies that LoadVersionAsync only fetches once (caching).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task LoadVersionAsync_CalledTwice_OnlyFetchesOnce()
    {
        // Arrange
        int callCount = 0;
        var handler = new MockHttpMessageHandler((_, _) =>
        {
            callCount++;
            var dto = new VersionInfoDto("1.0.0", DateTime.UtcNow, null, "Test");
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(dto),
            };
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var sut = new VersionService(client);

        // Act
        await sut.LoadVersionAsync();
        await sut.LoadVersionAsync();

        // Assert
        Assert.Equal(1, callCount);
    }

    /// <summary>
    /// Verifies that LoadVersionAsync sets fallback version on HTTP failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task LoadVersionAsync_OnFailure_SetsFallbackVersion()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var sut = new VersionService(client);

        // Act
        await sut.LoadVersionAsync();

        // Assert
        Assert.True(sut.IsLoaded);
        Assert.Equal("unknown", sut.CurrentVersion);
    }

    private static VersionService CreateService(VersionInfoDto? dto = null)
    {
        var handler = new MockHttpMessageHandler((_, _) =>
        {
            if (dto is null)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(dto),
            };
            return Task.FromResult(response);
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        return new VersionService(client);
    }

    /// <summary>
    /// Mock HTTP message handler for testing.
    /// </summary>
    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            this._handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return this._handler(request, cancellationToken);
        }
    }
}
