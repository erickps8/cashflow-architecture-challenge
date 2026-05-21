using CashFlow.Launch.Domain.Entities;
using CashFlow.Launch.Domain.Enums;
using CashFlow.Launch.Domain.Interfaces;
using CashFlow.Launch.Domain.Interfaces.Services;
using CashFlow.Launch.Domain.Notifications;

namespace CashFlow.Launch.Domain.Services;

public class EntryService : IEntryService
{
    private readonly IEntryRepository _repository;
    private readonly INotificator _notificator;

    public EntryService(IEntryRepository repository, INotificator notificator)
    {
        _repository = repository;
        _notificator = notificator;
    }

    public async Task<Entry?> CreateAsync(decimal amount, EntryType type, string description, DateTime occurredAt)
    {
        if (amount <= 0)
        {
            _notificator.Handle(new Notification("O valor deve ser maior que zero."));
            return null;
        }

        var entry = new Entry
        {
            Amount = amount,
            Type = type,
            Description = description,
            OccurredAt = occurredAt
        };

        await _repository.AddAsync(entry);
        await _repository.SaveChangesAsync();

        return entry;
    }

    public async Task<List<Entry>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }
}