// <copyright file="EncryptedStringConverter.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain.Services;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BudgetExperiment.Infrastructure.Persistence.Converters;

/// <summary>
/// EF Core value converter for transparently encrypting/decrypting non-nullable string properties.
/// </summary>
internal sealed class EncryptedStringConverter : ValueConverter<string, string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptedStringConverter"/> class.
    /// </summary>
    /// <param name="encryptionService">The encryption service.</param>
    public EncryptedStringConverter(IEncryptionService encryptionService)
        : base(
            plaintext => encryptionService.EncryptAsync(plaintext).GetAwaiter().GetResult(),
            ciphertext => encryptionService.DecryptAsync(ciphertext).GetAwaiter().GetResult())
    {
    }
}
