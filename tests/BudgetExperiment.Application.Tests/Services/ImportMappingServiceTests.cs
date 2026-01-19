// <copyright file="ImportMappingServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Services;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

using Moq;

namespace BudgetExperiment.Application.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ImportMappingService"/>.
/// </summary>
public class ImportMappingServiceTests
{
    private readonly Mock<IImportMappingRepository> _repositoryMock;
    private readonly Mock<IUserContext> _userContextMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly ImportMappingService _service;
    private readonly Guid _testUserId = Guid.NewGuid();

    public ImportMappingServiceTests()
    {
        this._repositoryMock = new Mock<IImportMappingRepository>();
        this._userContextMock = new Mock<IUserContext>();
        this._unitOfWorkMock = new Mock<IUnitOfWork>();

        this._userContextMock.Setup(u => u.UserIdAsGuid).Returns(this._testUserId);

        this._service = new ImportMappingService(
            this._repositoryMock.Object,
            this._userContextMock.Object,
            this._unitOfWorkMock.Object);
    }

    [Fact]
    public async Task GetUserMappingsAsync_ReturnsMappingsForCurrentUser()
    {
        // Arrange
        var mapping = CreateTestMapping(this._testUserId, "My Mapping");
        this._repositoryMock
            .Setup(r => r.GetByUserAsync(this._testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ImportMapping> { mapping });

        // Act
        var result = await this._service.GetUserMappingsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("My Mapping", result[0].Name);
    }

    [Fact]
    public async Task GetMappingAsync_ReturnsMapping_WhenOwnedByUser()
    {
        // Arrange
        var mapping = CreateTestMapping(this._testUserId, "My Mapping");
        this._repositoryMock
            .Setup(r => r.GetByIdAsync(mapping.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mapping);

        // Act
        var result = await this._service.GetMappingAsync(mapping.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("My Mapping", result.Name);
    }

    [Fact]
    public async Task GetMappingAsync_ReturnsNull_WhenNotOwnedByUser()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        var mapping = CreateTestMapping(otherUserId, "Other User's Mapping");
        this._repositoryMock
            .Setup(r => r.GetByIdAsync(mapping.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mapping);

        // Act
        var result = await this._service.GetMappingAsync(mapping.Id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetMappingAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        this._repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImportMapping?)null);

        // Act
        var result = await this._service.GetMappingAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateMappingAsync_CreatesMappingSuccessfully()
    {
        // Arrange
        var request = new CreateImportMappingRequest
        {
            Name = "New Mapping",
            ColumnMappings =
            [
                new ColumnMappingDto { ColumnIndex = 0, ColumnHeader = "Date", TargetField = ImportField.Date },
            ],
        };

        this._repositoryMock
            .Setup(r => r.GetByNameAsync(this._testUserId, "New Mapping", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImportMapping?)null);

        // Act
        var result = await this._service.CreateMappingAsync(request);

        // Assert
        Assert.Equal("New Mapping", result.Name);
        Assert.Single(result.ColumnMappings);
        this._repositoryMock.Verify(r => r.AddAsync(It.IsAny<ImportMapping>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateMappingAsync_ThrowsWhenDuplicateName()
    {
        // Arrange
        var existingMapping = CreateTestMapping(this._testUserId, "Existing");
        var request = new CreateImportMappingRequest
        {
            Name = "Existing",
            ColumnMappings =
            [
                new ColumnMappingDto { ColumnIndex = 0, TargetField = ImportField.Date },
            ],
        };

        this._repositoryMock
            .Setup(r => r.GetByNameAsync(this._testUserId, "Existing", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingMapping);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() => this._service.CreateMappingAsync(request));
        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task DeleteMappingAsync_ReturnsTrue_WhenOwnedByUser()
    {
        // Arrange
        var mapping = CreateTestMapping(this._testUserId, "To Delete");
        this._repositoryMock
            .Setup(r => r.GetByIdAsync(mapping.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mapping);

        // Act
        var result = await this._service.DeleteMappingAsync(mapping.Id);

        // Assert
        Assert.True(result);
        this._repositoryMock.Verify(r => r.RemoveAsync(mapping, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteMappingAsync_ReturnsFalse_WhenNotOwnedByUser()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        var mapping = CreateTestMapping(otherUserId, "Other User's");
        this._repositoryMock
            .Setup(r => r.GetByIdAsync(mapping.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mapping);

        // Act
        var result = await this._service.DeleteMappingAsync(mapping.Id);

        // Assert
        Assert.False(result);
        this._repositoryMock.Verify(r => r.RemoveAsync(It.IsAny<ImportMapping>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SuggestMappingAsync_ReturnsSuggestion_WhenHeadersMatch()
    {
        // Arrange
        var mapping = CreateTestMapping(this._testUserId, "Bank Export");
        this._repositoryMock
            .Setup(r => r.GetByUserAsync(this._testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ImportMapping> { mapping });

        var headers = new List<string> { "Date", "Description", "Amount" };

        // Act
        var result = await this._service.SuggestMappingAsync(headers);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Bank Export", result.Name);
    }

    [Fact]
    public async Task SuggestMappingAsync_ReturnsNull_WhenNoMatch()
    {
        // Arrange
        var mapping = CreateTestMapping(this._testUserId, "Bank Export");
        this._repositoryMock
            .Setup(r => r.GetByUserAsync(this._testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ImportMapping> { mapping });

        var headers = new List<string> { "TransDate", "Memo", "Value" };

        // Act
        var result = await this._service.SuggestMappingAsync(headers);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SuggestMappingAsync_ReturnsNull_WhenEmptyHeaders()
    {
        // Act
        var result = await this._service.SuggestMappingAsync([]);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserMappingsAsync_ThrowsWhenNoUserId()
    {
        // Arrange
        this._userContextMock.Setup(u => u.UserIdAsGuid).Returns((Guid?)null);

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => this._service.GetUserMappingsAsync());
    }

    private static ImportMapping CreateTestMapping(Guid userId, string name)
    {
        var mappings = new List<ColumnMapping>
        {
            new() { ColumnIndex = 0, ColumnHeader = "Date", TargetField = ImportField.Date },
            new() { ColumnIndex = 1, ColumnHeader = "Description", TargetField = ImportField.Description },
            new() { ColumnIndex = 2, ColumnHeader = "Amount", TargetField = ImportField.Amount },
        };
        return ImportMapping.Create(userId, name, mappings);
    }
}
