// <copyright file="TransferDirectionTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the TransferDirection enum.
/// </summary>
public class TransferDirectionTests
{
    [Fact]
    public void TransferDirection_Has_Source_Value()
    {
        // Arrange & Act
        var direction = TransferDirection.Source;

        // Assert
        Assert.Equal(0, (int)direction);
    }

    [Fact]
    public void TransferDirection_Has_Destination_Value()
    {
        // Arrange & Act
        var direction = TransferDirection.Destination;

        // Assert
        Assert.Equal(1, (int)direction);
    }

    [Fact]
    public void TransferDirection_Has_Exactly_Two_Values()
    {
        // Arrange & Act
        var values = Enum.GetValues<TransferDirection>();

        // Assert
        Assert.Equal(2, values.Length);
    }
}
