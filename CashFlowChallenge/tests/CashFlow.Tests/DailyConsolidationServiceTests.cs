using CashFlow.Consolidation.Domain.Entities;
using CashFlow.Consolidation.Domain.Interfaces;
using CashFlow.Consolidation.Domain.Notifications;
using CashFlow.Consolidation.Domain.Services;
using FluentAssertions;
using Moq;

namespace CashFlow.Tests;

public class DailyConsolidationServiceTests
{
    private readonly Mock<IDailyConsolidationRepository> _repositoryMock = new();
    private readonly Mock<INotificator> _notificatorMock = new();

    [Fact]
    public async Task ProcessEntryAsync_Should_Create_Consolidation_When_Not_Exists()
    {
        DailyConsolidation? createdConsolidation = null;

        _repositoryMock
            .Setup(x => x.GetByDateAsync(It.IsAny<DateTime>()))
            .ReturnsAsync((DailyConsolidation?)null);

        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<DailyConsolidation>()))
            .Callback<DailyConsolidation>(c => createdConsolidation = c)
            .Returns(Task.CompletedTask);

        var service = new DailyConsolidationService(
            _repositoryMock.Object,
            _notificatorMock.Object);

        await service.ProcessEntryAsync(100, 1, DateTime.UtcNow);

        createdConsolidation.Should().NotBeNull();
        createdConsolidation!.TotalCredits.Should().Be(100);
        createdConsolidation.TotalDebits.Should().Be(0);
        createdConsolidation.Balance.Should().Be(100);

        _repositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ProcessEntryAsync_Should_Add_Credit_When_Type_Is_One()
    {
        var consolidation = new DailyConsolidation
        {
            Date = DateTime.UtcNow.Date,
            TotalCredits = 50,
            TotalDebits = 20,
            Balance = 30
        };

        _repositoryMock
            .Setup(x => x.GetByDateAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(consolidation);

        var service = new DailyConsolidationService(
            _repositoryMock.Object,
            _notificatorMock.Object);

        await service.ProcessEntryAsync(100, 1, DateTime.UtcNow);

        consolidation.TotalCredits.Should().Be(150);
        consolidation.TotalDebits.Should().Be(20);
        consolidation.Balance.Should().Be(130);

        _repositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ProcessEntryAsync_Should_Add_Debit_When_Type_Is_Not_One()
    {
        var consolidation = new DailyConsolidation
        {
            Date = DateTime.UtcNow.Date,
            TotalCredits = 200,
            TotalDebits = 50,
            Balance = 150
        };

        _repositoryMock
            .Setup(x => x.GetByDateAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(consolidation);

        var service = new DailyConsolidationService(
            _repositoryMock.Object,
            _notificatorMock.Object);

        await service.ProcessEntryAsync(80, 2, DateTime.UtcNow);

        consolidation.TotalCredits.Should().Be(200);
        consolidation.TotalDebits.Should().Be(130);
        consolidation.Balance.Should().Be(70);

        _repositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ReprocessAsync_Should_Recalculate_Balance_When_Consolidation_Exists()
    {
        var consolidation = new DailyConsolidation
        {
            Date = DateTime.UtcNow.Date,
            TotalCredits = 500,
            TotalDebits = 150,
            Balance = 0
        };

        _repositoryMock
            .Setup(x => x.GetByDateAsync(DateTime.UtcNow.Date))
            .ReturnsAsync(consolidation);

        var service = new DailyConsolidationService(
            _repositoryMock.Object,
            _notificatorMock.Object);

        var result = await service.ReprocessAsync();

        result.Should().NotBeNull();
        result.Balance.Should().Be(350);

        _repositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ReprocessAsync_Should_Notify_When_Consolidation_Does_Not_Exist()
    {
        _repositoryMock
            .Setup(x => x.GetByDateAsync(DateTime.UtcNow.Date))
            .ReturnsAsync((DailyConsolidation?)null);

        var service = new DailyConsolidationService(
            _repositoryMock.Object,
            _notificatorMock.Object);

        var result = await service.ReprocessAsync();

        result.Should().BeNull();

        _repositoryMock.Verify(x => x.SaveChangesAsync(), Times.Never);

        _notificatorMock.Verify(x =>
            x.Handle(It.Is<Notification>(n =>
                n.Message == "Não existe consolidação para reprocessar.")),
            Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_Should_Return_Empty_When_Repository_Throws()
    {
        _repositoryMock
            .Setup(x => x.GetAllAsync())
            .ThrowsAsync(new Exception("database error"));

        var service = new DailyConsolidationService(
            _repositoryMock.Object,
            _notificatorMock.Object);

        var result = await service.GetAllAsync();

        result.Should().BeEmpty();

        _notificatorMock.Verify(x =>
            x.Handle(It.Is<Notification>(n =>
                n.Message.Contains("Erro ao consultar consolidações"))),
            Times.Once);
    }
}