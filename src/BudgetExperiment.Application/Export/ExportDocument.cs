// <copyright file="ExportDocument.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Export;

/// <summary>
/// Represents an export document result.
/// </summary>
public sealed record ExportDocument
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExportDocument"/> class.
    /// </summary>
    /// <param name="fileName">The file name including extension.</param>
    /// <param name="contentType">The content type.</param>
    /// <param name="content">The file content bytes.</param>
    public ExportDocument(string fileName, string contentType, byte[] content)
    {
        this.FileName = fileName;
        this.ContentType = contentType;
        this.Content = content;
    }

    /// <summary>
    /// Gets the file name including extension.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// Gets the content type.
    /// </summary>
    public string ContentType { get; }

    /// <summary>
    /// Gets the file content bytes.
    /// </summary>
    public byte[] Content { get; }
}
