// <copyright file="IEncryptionService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Services;

/// <summary>
/// Service for encrypting and decrypting sensitive data.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts the given plaintext.
    /// </summary>
    /// <param name="plaintext">The plaintext to encrypt.</param>
    /// <returns>The encrypted ciphertext.</returns>
    Task<string> EncryptAsync(string plaintext);

    /// <summary>
    /// Decrypts the given ciphertext.
    /// </summary>
    /// <param name="ciphertext">The ciphertext to decrypt.</param>
    /// <returns>The decrypted plaintext.</returns>
    /// <exception cref="DomainException">Thrown when decryption fails or ciphertext is tampered.</exception>
    Task<string> DecryptAsync(string ciphertext);
}
