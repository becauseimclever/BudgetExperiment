// <copyright file="DecimalToStringConverter.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BudgetExperiment.Infrastructure.Persistence.Converters;

/// <summary>
/// EF Core value converter for persisting decimal values as invariant strings.
/// </summary>
internal sealed class DecimalToStringConverter : ValueConverter<decimal, string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DecimalToStringConverter"/> class.
    /// </summary>
    public DecimalToStringConverter()
        : base(
            value => value.ToString("G29", CultureInfo.InvariantCulture),
            value => decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture))
    {
    }
}
