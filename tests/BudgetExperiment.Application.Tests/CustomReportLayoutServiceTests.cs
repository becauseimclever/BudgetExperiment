// <copyright file="CustomReportLayoutServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;
using BudgetExperiment.Domain.Reports;

using Moq;

using Shouldly;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Unit tests for <see cref="CustomReportLayoutService"/>.
/// </summary>
public class CustomReportLayoutServiceTests
{
    private readonly Mock<ICustomReportLayoutRepository> _repository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<IUserContext> _userContext;

    public CustomReportLayoutServiceTests()
    {
        _repository = new Mock<ICustomReportLayoutRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _userContext = new Mock<IUserContext>();

        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    [Fact]
    public async Task CreateAsync_UserNotAuthenticated_ThrowsDomainException()
    {
        _userContext.Setup(u => u.UserIdAsGuid).Returns((Guid?)null);

        var service = CreateService();
        var dto = new CustomReportLayoutCreateDto { Name = "Test", LayoutJson = "{}" };

        var ex = await Should.ThrowAsync<DomainException>(
            () => service.CreateAsync(dto));

        ex.Message.ShouldContain("not authenticated");
    }

    [Fact]
    public async Task CreateAsync_PersonalScope_CreatesPersonalLayout()
    {
        var userId = Guid.NewGuid();
        _userContext.Setup(u => u.UserIdAsGuid).Returns(userId);

        var service = CreateService();
        var dto = new CustomReportLayoutCreateDto
        {
            Name = "My Report",
            LayoutJson = "{ \"charts\": [] }",
            Scope = "Personal",
        };

        var result = await service.CreateAsync(dto);

        result.Name.ShouldBe("My Report");
        result.Scope.ShouldBe("Personal");
        _repository.Verify(r => r.AddAsync(It.IsAny<CustomReportLayout>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_SharedScope_CreatesSharedLayout()
    {
        var userId = Guid.NewGuid();
        _userContext.Setup(u => u.UserIdAsGuid).Returns(userId);

        var service = CreateService();
        var dto = new CustomReportLayoutCreateDto
        {
            Name = "Team Report",
            LayoutJson = "{ \"charts\": [] }",
            Scope = "Shared",
        };

        var result = await service.CreateAsync(dto);

        result.Scope.ShouldBe("Shared");
    }

    [Fact]
    public async Task CreateAsync_InvalidScope_ThrowsDomainException()
    {
        var userId = Guid.NewGuid();
        _userContext.Setup(u => u.UserIdAsGuid).Returns(userId);

        var service = CreateService();
        var dto = new CustomReportLayoutCreateDto
        {
            Name = "Test",
            LayoutJson = "{}",
            Scope = "InvalidScope",
        };

        await Should.ThrowAsync<DomainException>(
            () => service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_NullScope_DefaultsToUserCurrentScope()
    {
        var userId = Guid.NewGuid();
        _userContext.Setup(u => u.UserIdAsGuid).Returns(userId);
        _userContext.Setup(u => u.CurrentScope).Returns(BudgetScope.Personal);

        var service = CreateService();
        var dto = new CustomReportLayoutCreateDto
        {
            Name = "Default Scope Report",
            LayoutJson = "{}",
            Scope = null,
        };

        var result = await service.CreateAsync(dto);

        result.Scope.ShouldBe("Personal");
    }

    [Fact]
    public async Task CreateAsync_NullScopeAndNoCurrentScope_DefaultsToShared()
    {
        var userId = Guid.NewGuid();
        _userContext.Setup(u => u.UserIdAsGuid).Returns(userId);
        _userContext.Setup(u => u.CurrentScope).Returns((BudgetScope?)null);

        var service = CreateService();
        var dto = new CustomReportLayoutCreateDto
        {
            Name = "Default Report",
            LayoutJson = "{}",
            Scope = null,
        };

        var result = await service.CreateAsync(dto);

        result.Scope.ShouldBe("Shared");
    }

    [Fact]
    public async Task UpdateAsync_LayoutNotFound_ReturnsNull()
    {
        _repository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomReportLayout?)null);

        var service = CreateService();

        var result = await service.UpdateAsync(Guid.NewGuid(), new CustomReportLayoutUpdateDto { Name = "New" });

        result.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateAsync_OnlyNameProvided_UpdatesNameOnly()
    {
        var layout = CustomReportLayout.CreateShared("Original", "{ \"v\": 1 }", Guid.NewGuid());

        _repository
            .Setup(r => r.GetByIdAsync(layout.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(layout);

        var service = CreateService();

        var result = await service.UpdateAsync(layout.Id, new CustomReportLayoutUpdateDto { Name = "Updated Name" });

        result.ShouldNotBeNull();
        result!.Name.ShouldBe("Updated Name");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithExpectedVersion_SetsConcurrencyToken()
    {
        var layout = CustomReportLayout.CreateShared("Report", "{}", Guid.NewGuid());

        _repository
            .Setup(r => r.GetByIdAsync(layout.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(layout);

        var service = CreateService();

        await service.UpdateAsync(layout.Id, new CustomReportLayoutUpdateDto { Name = "Updated" }, expectedVersion: "abc123");

        _unitOfWork.Verify(u => u.SetExpectedConcurrencyToken(layout, "abc123"), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_LayoutNotFound_ReturnsFalse()
    {
        _repository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomReportLayout?)null);

        var service = CreateService();

        var result = await service.DeleteAsync(Guid.NewGuid());

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteAsync_LayoutExists_RemovesAndReturnsTrue()
    {
        var layout = CustomReportLayout.CreateShared("Report", "{}", Guid.NewGuid());

        _repository
            .Setup(r => r.GetByIdAsync(layout.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(layout);

        var service = CreateService();

        var result = await service.DeleteAsync(layout.Id);

        result.ShouldBeTrue();
        _repository.Verify(r => r.RemoveAsync(layout, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private CustomReportLayoutService CreateService()
    {
        return new CustomReportLayoutService(
            _repository.Object,
            _unitOfWork.Object,
            _userContext.Object);
    }
}
