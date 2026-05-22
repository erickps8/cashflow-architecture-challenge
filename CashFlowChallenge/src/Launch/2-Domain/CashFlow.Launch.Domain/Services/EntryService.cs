using CashFlow.Launch.Domain.Entities;
using CashFlow.Launch.Domain.Enums;
using CashFlow.Launch.Domain.Events;
using CashFlow.Launch.Domain.Interfaces;
using CashFlow.Launch.Domain.Interfaces.Services;
using CashFlow.Launch.Domain.Notifications;
using System.Text.Json;

namespace CashFlow.Launch.Domain.Services;

public class EntryService : IEntryService
{
    private readonly IEntryRepository _repository;
    private readonly INotificator _notificator;
    private readonly IOutboxMessageRepository _outboxRepository;
    public EntryService(IEntryRepository repository, IOutboxMessageRepository outboxRepository, INotificator notificator)
    {
        _repository = repository;
        _outboxRepository = outboxRepository;
        _notificator = notificator;
    }

    public async Task<Entry?> CreateAsync(decimal amount, EntryType type, string description, DateTime occurredAt)
    {
        if (amount <= 0)
        {
            _notificator.Handle(new Notification("Amount must be greater than zero."));
            return null;
        }

        occurredAt = DateTime.SpecifyKind(
        occurredAt,
        DateTimeKind.Utc);

        var entry = new Entry
        {
            Amount = amount,
            Type = type,
            Description = description,
            OccurredAt = occurredAt
        };

        await _repository.AddAsync(entry);

        var entryCreatedEvent = new EntryCreatedEvent
        {
            EntryId = entry.Id,
            Amount = entry.Amount,
            Type = (int)entry.Type,
            Description = entry.Description,
            OccurredAt = entry.OccurredAt
        };

        var outboxMessage = new OutboxMessage
        {
            Type = nameof(EntryCreatedEvent),
            Payload = JsonSerializer.Serialize(entryCreatedEvent)
        };

        await _outboxRepository.AddAsync(outboxMessage);

        await _repository.SaveChangesAsync();

        return entry;
    }

    public async Task<List<Entry>> GetAllAsync()
    {
        try
        {
            return await _repository.GetAllAsync();
        }
        catch (Exception ex)
        {
            Notify($"Erro ao consultar lançamentos: {ex.Message}");

            return [];
        }
    }
    private void Notify(string message)
    {
        _notificator.Handle(new Notification(message));
    }
}