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
    private readonly IAccountRepository _accountRepository;
    private readonly ICategoryRepository _categoryRepository;

    public EntryService(IEntryRepository repository, IOutboxMessageRepository outboxRepository, INotificator notificator, IAccountRepository accountRepository, ICategoryRepository categoryRepository)
    {
        _repository = repository;
        _outboxRepository = outboxRepository;
        _notificator = notificator;
        _accountRepository = accountRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<Entry?> CreateAsync(decimal amount, EntryType type, string description, DateTime occurredAt, Guid? accountId = null, Guid? categoryId = null, bool isRecurring = false)
    {
        if (amount <= 0)
        {
            Notify("Amount must be greater than zero.");
            return null;
        }

        if (accountId.HasValue && await _accountRepository.GetByIdAsync(accountId.Value) is null)
        {
            Notify("Account not found.");
            return null;
        }

        if (categoryId.HasValue)
        {
            var category = await _categoryRepository.GetByIdAsync(categoryId.Value);
            if (category is null)
            {
                Notify("Category not found.");
                return null;
            }
            if (category.Type != type)
            {
                Notify("Category type must match entry type.");
                return null;
            }
        }

        occurredAt = DateTime.SpecifyKind(occurredAt, DateTimeKind.Utc);

        var entry = new Entry
        {
            Amount = amount,
            Type = type,
            Description = description,
            OccurredAt = occurredAt,
            AccountId = accountId,
            CategoryId = categoryId,
            IsRecurring = isRecurring
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

        await _outboxRepository.AddAsync(new OutboxMessage
        {
            Type = nameof(EntryCreatedEvent),
            Payload = JsonSerializer.Serialize(entryCreatedEvent)
        });

        await _repository.SaveChangesAsync();
        return entry;
    }

    public async Task<List<Entry>> GetAllAsync()
    {
        try { return await _repository.GetAllAsync(); }
        catch (Exception ex) { Notify($"Erro ao consultar lançamentos: {ex.Message}"); return []; }
    }

    public Task<List<Entry>> GetByMonthAsync(int year, int month)
    {
        if (month < 1 || month > 12) return Task.FromResult(new List<Entry>());
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        return _repository.GetByPeriodAsync(start, start.AddMonths(1));
    }

    private void Notify(string message) => _notificator.Handle(new Notification(message));
}
