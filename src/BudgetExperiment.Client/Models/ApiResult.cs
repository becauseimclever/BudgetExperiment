// <copyright file="ApiResult.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Models;

/// <summary>
/// Represents the result of an API update operation, distinguishing success, conflict, and failure.
/// </summary>
/// <typeparam name="T">The response data type.</typeparam>
public sealed class ApiResult<T>
{
    private ApiResult()
    {
    }

    /// <summary>
    /// Gets the response data (null on conflict or failure).
    /// </summary>
    public T? Data
    {
        get; private init;
    }

    /// <summary>
    /// Gets a value indicating whether the resource was modified by another user (409 Conflict).
    /// </summary>
    public bool IsConflict
    {
        get; private init;
    }

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess => Data is not null && !IsConflict;

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="data">The response data.</param>
    /// <returns>A success result.</returns>
    public static ApiResult<T> Success(T data) => new() { Data = data };

    /// <summary>
    /// Creates a conflict result (409).
    /// </summary>
    /// <returns>A conflict result.</returns>
    public static ApiResult<T> Conflict() => new() { IsConflict = true };

    /// <summary>
    /// Creates a generic failure result.
    /// </summary>
    /// <returns>A failure result.</returns>
    public static ApiResult<T> Failure() => new();
}
