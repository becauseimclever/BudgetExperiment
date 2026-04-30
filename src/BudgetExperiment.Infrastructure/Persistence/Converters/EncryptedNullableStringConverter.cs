// <copyright file="EncryptedNullableStringConverter.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain.Services;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BudgetExperiment.Infrastructure.Persistence.Converters;

/// <summary>
/// EF Core value converter for transparently encrypting/decrypting nullable string properties.
/// </summary>
internal sealed class EncryptedNullableStringConverter : ValueConverter<string?, string?>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptedNullableStringConverter"/> class.
    /// </summary>
    /// <param name="encryptionService">The encryption service.</param>
    public EncryptedNullableStringConverter(IEncryptionService encryptionService)
        : base(
            plaintext => plaintext != null ? encryptionService.EncryptAsync(plaintext).GetAwaiter().GetResult() : null,
            ciphertext => ciphertext != null ? encryptionService.DecryptAsync(ciphertext).GetAwaiter().GetResult() : null)
    {
    }
}
