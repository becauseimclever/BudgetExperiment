// <copyright file="FileUploadZoneTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Import;
using BudgetExperiment.Client.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Tests.Components.Import;

/// <summary>
/// Unit tests for the <see cref="FileUploadZone"/> component.
/// </summary>
public sealed class FileUploadZoneTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileUploadZoneTests"/> class.
    /// </summary>
    public FileUploadZoneTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ThemeService>();
        Services.AddSingleton<CultureService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies that the upload zone renders with default content.
    /// </summary>
    [Fact]
    public void FileUploadZone_RendersDefaultContent()
    {
        // Arrange & Act
        var cut = Render<FileUploadZone>();

        // Assert
        Assert.Contains("Drag and drop", cut.Markup);
        Assert.Contains("click to browse", cut.Markup);
    }

    /// <summary>
    /// Verifies that the upload zone shows accepted formats.
    /// </summary>
    [Fact]
    public void FileUploadZone_ShowsAcceptedFormats()
    {
        // Arrange & Act
        var cut = Render<FileUploadZone>(parameters => parameters
            .Add(p => p.AcceptedFormats, ".csv"));

        // Assert
        Assert.Contains(".csv", cut.Markup);
    }

    /// <summary>
    /// Verifies that the upload zone shows max file size.
    /// </summary>
    [Fact]
    public void FileUploadZone_ShowsMaxFileSize()
    {
        // Arrange & Act
        var cut = Render<FileUploadZone>(parameters => parameters
            .Add(p => p.MaxFileSizeMb, 25));

        // Assert
        Assert.Contains("25 MB", cut.Markup);
    }

    /// <summary>
    /// Verifies that the upload zone shows uploading state.
    /// </summary>
    [Fact]
    public void FileUploadZone_ShowsUploadingState()
    {
        // Arrange & Act
        var cut = Render<FileUploadZone>(parameters => parameters
            .Add(p => p.IsUploading, true));

        // Assert
        Assert.Contains("Uploading and parsing file...", cut.Markup);
        var zone = cut.Find(".file-upload-zone");
        Assert.Contains("uploading", zone.ClassList);
    }

    /// <summary>
    /// Verifies that the upload zone does not show uploading class when not uploading.
    /// </summary>
    [Fact]
    public void FileUploadZone_NoUploadingClass_WhenNotUploading()
    {
        // Arrange & Act
        var cut = Render<FileUploadZone>(parameters => parameters
            .Add(p => p.IsUploading, false));

        // Assert
        var zone = cut.Find(".file-upload-zone");
        Assert.DoesNotContain("uploading", zone.ClassList);
    }

    /// <summary>
    /// Verifies that drag enter adds the dragging class.
    /// </summary>
    [Fact]
    public void FileUploadZone_DragEnter_AddsDraggingClass()
    {
        // Arrange
        var cut = Render<FileUploadZone>();

        // Act
        cut.Find(".file-upload-zone").TriggerEvent("ondragenter", new Microsoft.AspNetCore.Components.Web.DragEventArgs());

        // Assert
        var zone = cut.Find(".file-upload-zone");
        Assert.Contains("dragging", zone.ClassList);
    }

    /// <summary>
    /// Verifies that drag leave removes the dragging class.
    /// </summary>
    [Fact]
    public void FileUploadZone_DragLeave_RemovesDraggingClass()
    {
        // Arrange
        var cut = Render<FileUploadZone>();
        cut.Find(".file-upload-zone").TriggerEvent("ondragenter", new Microsoft.AspNetCore.Components.Web.DragEventArgs());

        // Act
        cut.Find(".file-upload-zone").TriggerEvent("ondragleave", new Microsoft.AspNetCore.Components.Web.DragEventArgs());

        // Assert
        var zone = cut.Find(".file-upload-zone");
        Assert.DoesNotContain("dragging", zone.ClassList);
    }
}
