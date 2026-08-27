using CashFlow.Launch.Domain.Entities;
using CashFlow.Launch.Domain.Enums;
using CashFlow.Launch.Domain.Events;
using CashFlow.Launch.Domain.Interfaces;
using CashFlow.Launch.Domain.Notifications;
using CashFlow.Launch.Domain.Services;
using FluentAssertions;
using Moq;
using System.Text.Json;

namespace CashFlow.Tests;

public class EntryServiceTests
{
    private readonly Mock<IEntryRepository> _entryRepositoryMock = new();
    private readonly Mock<IOutboxMessageRepository> _outboxRepositoryMock = new();
    private readonly Mock<INotificator> _notificatorMock = new();
    private readonly Mock<IAccountRepository> _accountRepositoryMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock = new();

    private EntryService CreateService() => new(
        _entryRepositoryMock.Object,
        _outboxRepositoryMock.Object,
        _notificatorMock.Object,
        _accountRepositoryMock.Object,
        _categoryRepositoryMock.Object);

    [Fact]
    public async Task CreateAsync_Should_Return_Null_When_Amount_Is_Zero()
    {
        var service = CreateService();

        var result = await service.CreateAsync(0, EntryType.Credit, "Teste", DateTime.UtcNow);

        result.Should().BeNull();

        _entryRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Entry>()), Times.Never);
        _outboxRepositoryMock.Verify(x => x.AddAsync(It.IsAny<OutboxMessage>()), Times.Never);
        _entryRepositoryMock.Verify(x => x.SaveChangesAsync(), Times.Never);

        _notificatorMock.Verify(x =>
            x.Handle(It.Is<Notification>(n =>
                n.Message == "Amount must be greater than zero.")),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_Should_Create_Entry_When_Data_Is_Valid()
    {
        var service = CreateService();

        var result = await service.CreateAsync(100, EntryType.Credit, "Entrada teste", DateTime.Now);

        result.Should().NotBeNull();
        result!.Amount.Should().Be(100);
        result.Type.Should().Be(EntryType.Credit);
        result.Description.Should().Be("Entrada teste");
        result.OccurredAt.Kind.Should().Be(DateTimeKind.Utc);

        _entryRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Entry>()), Times.Once);
        _outboxRepositoryMock.Verify(x => x.AddAsync(It.IsAny<OutboxMessage>()), Times.Once);
        _entryRepositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_Should_Create_Outbox_Message_When_Entry_Is_Valid()
    {
        OutboxMessage? capturedOutbox = null;

        _outboxRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<OutboxMessage>()))
            .Callback<OutboxMessage>(message => capturedOutbox = message)
            .Returns(Task.CompletedTask);

        var service = CreateService();

        await service.CreateAsync(50, EntryType.Debit, "Saída teste", DateTime.UtcNow);

        capturedOutbox.Should().NotBeNull();
        capturedOutbox!.Type.Should().Be("EntryCreatedEvent");
        var payload = JsonSerializer.Deserialize<EntryCreatedEvent>(capturedOutbox.Payload);

        payload.Should().NotBeNull();
        payload!.Amount.Should().Be(50);
        payload.Type.Should().Be((int)EntryType.Debit);
        payload.Description.Should().Be("Saída teste");
    }

    [Fact]
    public async Task GetAllAsync_Should_Return_Entries_When_Repository_Succeeds()
    {
        var entries = new List<Entry>
        {
            new() { Amount = 100, Type = EntryType.Credit, Description = "Teste" }
        };

        _entryRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(entries);

        var service = CreateService();

        var result = await service.GetAllAsync();

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAllAsync_Should_Return_Empty_List_When_Repository_Throws()
    {
        _entryRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ThrowsAsync(new Exception("database error"));

        var service = CreateService();

        var result = await service.GetAllAsync();

        result.Should().BeEmpty();

        _notificatorMock.Verify(x =>
            x.Handle(It.Is<Notification>(n =>
                n.Message.Contains("Erro ao consultar lançamentos"))),
            Times.Once);
    }
}
