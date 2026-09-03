using System.Text.Json;
using CashFlow.Launch.Domain.Entities;
using CashFlow.Launch.Domain.Enums;
using CashFlow.Launch.Domain.Events;
using CashFlow.Launch.Domain.Interfaces;
using CashFlow.Launch.Domain.Interfaces.Services;
using CashFlow.Launch.Domain.Notifications;

namespace CashFlow.Launch.Domain.Services;

public sealed class EntryService : IEntryService
{
    private readonly IEntryRepository _entryRepository;
    private readonly IOutboxMessageRepository _outboxRepository;
    private readonly INotificator _notificator;
    private readonly IAccountRepository _accountRepository;
    private readonly ICategoryRepository _categoryRepository;

    public EntryService(
        IEntryRepository entryRepository,
        IOutboxMessageRepository outboxRepository,
        INotificator notificator,
        IAccountRepository accountRepository,
        ICategoryRepository categoryRepository)
    {
        _entryRepository = entryRepository;
        _outboxRepository = outboxRepository;
        _notificator = notificator;
        _accountRepository = accountRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<Entry?> CreateAsync(
        decimal amount,
        EntryType type,
        string description,
        DateTime occurredAt,
        Guid? accountId = null,
        Guid? categoryId = null,
        bool isRecurring = false)
    {
        if (!await IsValidAsync(amount, type, accountId, categoryId))
            return null;

        var entry = new Entry
        {
            Amount = amount,
            Type = type,
            Description = description,
            OccurredAt = ToUtc(occurredAt),
            AccountId = accountId,
            CategoryId = categoryId,
            IsRecurring = isRecurring
        };

        await _entryRepository.AddAsync(entry);
        await AddOutboxMessageAsync(entry);
        await _entryRepository.SaveChangesAsync();

        return entry;
    }

    public async Task<Entry?> UpdateAsync(
        Guid id,
        decimal amount,
        EntryType type,
        string description,
        DateTime occurredAt,
        Guid? accountId = null,
        Guid? categoryId = null,
        bool isRecurring = false)
    {
        var entry = await _entryRepository.GetByIdAsync(id);
        if (entry is null)
        {
            Notify("Lançamento não encontrado.");
            return null;
        }

        if (!await IsValidAsync(amount, type, accountId, categoryId))
            return null;

        entry.Amount = amount;
        entry.Type = type;
        entry.Description = description;
        entry.OccurredAt = ToUtc(occurredAt);
        entry.AccountId = accountId;
        entry.CategoryId = categoryId;
        entry.IsRecurring = isRecurring;

        _entryRepository.Update(entry);
        await _entryRepository.SaveChangesAsync();

        return entry;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entry = await _entryRepository.GetByIdAsync(id);
        if (entry is null)
        {
            Notify("Lançamento não encontrado.");
            return false;
        }

        _entryRepository.Remove(entry);
        await _entryRepository.SaveChangesAsync();
        return true;
    }

    public async Task<List<Entry>> GetAllAsync()
    {
        try
        {
            return await _entryRepository.GetAllAsync();
        }
        catch (Exception exception)
        {
            Notify($"Erro ao consultar lançamentos: {exception.Message}");
            return [];
        }
    }

    public Task<List<Entry>> GetByMonthAsync(int year, int month)
    {
        if (month is < 1 or > 12)
            return Task.FromResult(new List<Entry>());

        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        return _entryRepository.GetByPeriodAsync(start, start.AddMonths(1));
    }

    private async Task<bool> IsValidAsync(
        decimal amount,
        EntryType type,
        Guid? accountId,
        Guid? categoryId)
    {
        if (amount <= 0)
        {
            Notify("Amount must be greater than zero.");
            return false;
        }

        if (accountId.HasValue && await _accountRepository.GetByIdAsync(accountId.Value) is null)
        {
            Notify("Account not found.");
            return false;
        }

        if (!categoryId.HasValue)
            return true;

        var category = await _categoryRepository.GetByIdAsync(categoryId.Value);
        if (category is null)
        {
            Notify("Category not found.");
            return false;
        }

        if (category.Type != type)
        {
            Notify("Category type must match entry type.");
            return false;
        }

        return true;
    }

    private async Task AddOutboxMessageAsync(Entry entry)
    {
        var domainEvent = new EntryCreatedEvent
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
            Payload = JsonSerializer.Serialize(domainEvent)
        });
    }

    private static DateTime ToUtc(DateTime value) =>
        DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private void Notify(string message) =>
        _notificator.Handle(new Notification(message));
}
