// <copyright file="EncryptedDecimalConverter.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;

using BudgetExperiment.Domain.Services;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BudgetExperiment.Infrastructure.Persistence.Converters;

/// <summary>
/// EF Core value converter for transparently encrypting/decrypting decimal properties.
/// </summary>
internal sealed class EncryptedDecimalConverter : ValueConverter<decimal, string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptedDecimalConverter"/> class.
    /// </summary>
    /// <param name="encryptionService">The encryption service.</param>
    public EncryptedDecimalConverter(IEncryptionService encryptionService)
        : base(
            value => encryptionService.EncryptAsync(value.ToString("G29", CultureInfo.InvariantCulture)).GetAwaiter().GetResult(),
            value => decimal.Parse(
                encryptionService.DecryptAsync(value).GetAwaiter().GetResult(),
                NumberStyles.Number,
                CultureInfo.InvariantCulture))
    {
    }
}
