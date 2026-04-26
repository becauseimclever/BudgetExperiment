// <copyright file="EncryptionService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Security.Cryptography;
using System.Text;

using BudgetExperiment.Domain.Common;
using BudgetExperiment.Domain.Services;

using Microsoft.Extensions.Configuration;

namespace BudgetExperiment.Infrastructure.Encryption;

/// <summary>
/// AES-256-GCM authenticated encryption service for sensitive data.
/// </summary>
internal sealed class EncryptionService : IEncryptionService
{
    private const int KeySizeBytes = 32; // AES-256
    private const int NonceSizeBytes = 12; // GCM standard nonce size
    private const int TagSizeBytes = 16; // GCM authentication tag size
    private const string EnvironmentVariableName = "ENCRYPTION_MASTER_KEY";
    private const string ConfigurationKey = "Encryption:MasterKey";

    private readonly byte[] _masterKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptionService"/> class.
    /// </summary>
    /// <param name="configuration">Configuration containing the master key.</param>
    /// <exception cref="DomainException">Thrown when the master key is not configured.</exception>
    public EncryptionService(IConfiguration configuration)
    {
        var keyBase64 = Environment.GetEnvironmentVariable(EnvironmentVariableName)
            ?? configuration[ConfigurationKey];

        if (string.IsNullOrWhiteSpace(keyBase64))
        {
            throw new DomainException(
                $"Encryption key not configured. Set {EnvironmentVariableName} environment variable or {ConfigurationKey} in user secrets.");
        }

        try
        {
            _masterKey = Convert.FromBase64String(keyBase64);
        }
        catch (FormatException)
        {
            throw new DomainException("Encryption key is not a valid Base64 string.");
        }

        if (_masterKey.Length != KeySizeBytes)
        {
            throw new DomainException($"Encryption key must be exactly {KeySizeBytes} bytes ({KeySizeBytes * 8}-bit).");
        }
    }

    /// <summary>
    /// Generates a new secure random key suitable for AES-256.
    /// </summary>
    /// <returns>Base64-encoded 256-bit key.</returns>
    public static string GenerateSecureKey()
    {
        var key = new byte[KeySizeBytes];
        RandomNumberGenerator.Fill(key);
        return Convert.ToBase64String(key);
    }

    /// <inheritdoc />
    public Task<string> EncryptAsync(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
        {
            return Task.FromResult(string.Empty);
        }

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = new byte[NonceSizeBytes];
        var tag = new byte[TagSizeBytes];
        var ciphertext = new byte[plaintextBytes.Length];

        RandomNumberGenerator.Fill(nonce);

        using var aesGcm = new AesGcm(_masterKey, TagSizeBytes);
        aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        // Format: nonce + tag + ciphertext
        var combined = new byte[NonceSizeBytes + TagSizeBytes + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, NonceSizeBytes);
        Buffer.BlockCopy(tag, 0, combined, NonceSizeBytes, TagSizeBytes);
        Buffer.BlockCopy(ciphertext, 0, combined, NonceSizeBytes + TagSizeBytes, ciphertext.Length);

        return Task.FromResult(Convert.ToBase64String(combined));
    }

    /// <inheritdoc />
    public Task<string> DecryptAsync(string ciphertext)
    {
        if (string.IsNullOrEmpty(ciphertext))
        {
            return Task.FromResult(string.Empty);
        }

        byte[] combined;
        try
        {
            combined = Convert.FromBase64String(ciphertext);
        }
        catch (FormatException)
        {
            throw new DomainException("Invalid encrypted data format.");
        }

        if (combined.Length < NonceSizeBytes + TagSizeBytes)
        {
            throw new DomainException("Encrypted data is too short.");
        }

        var nonce = new byte[NonceSizeBytes];
        var tag = new byte[TagSizeBytes];
        var ciphertextBytes = new byte[combined.Length - NonceSizeBytes - TagSizeBytes];

        Buffer.BlockCopy(combined, 0, nonce, 0, NonceSizeBytes);
        Buffer.BlockCopy(combined, NonceSizeBytes, tag, 0, TagSizeBytes);
        Buffer.BlockCopy(combined, NonceSizeBytes + TagSizeBytes, ciphertextBytes, 0, ciphertextBytes.Length);

        var plaintext = new byte[ciphertextBytes.Length];

        try
        {
            using var aesGcm = new AesGcm(_masterKey, TagSizeBytes);
            aesGcm.Decrypt(nonce, ciphertextBytes, tag, plaintext);
        }
        catch (CryptographicException ex)
        {
            throw new DomainException("Decryption failed. Data may be corrupted or tampered.", ex);
        }

        return Task.FromResult(Encoding.UTF8.GetString(plaintext));
    }
}
