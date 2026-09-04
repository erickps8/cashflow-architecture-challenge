using CashFlow.Launch.Domain.Entities;
using CashFlow.Launch.Domain.Enums;
using CashFlow.Launch.Domain.Interfaces;
using CashFlow.Launch.Domain.Interfaces.Services;
using CashFlow.Launch.Domain.Notifications;

namespace CashFlow.Launch.Domain.Services;

public sealed class RecurringEntryService : IRecurringEntryService
{
    private readonly IRecurringEntryRepository _recurringEntryRepository;
    private readonly IEntryService _entryService;
    private readonly IAccountRepository _accountRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly INotificator _notificator;

    public RecurringEntryService(
        IRecurringEntryRepository recurringEntryRepository,
        IEntryService entryService,
        IAccountRepository accountRepository,
        ICategoryRepository categoryRepository,
        INotificator notificator)
    {
        _recurringEntryRepository = recurringEntryRepository;
        _entryService = entryService;
        _accountRepository = accountRepository;
        _categoryRepository = categoryRepository;
        _notificator = notificator;
    }

    public async Task<RecurringEntry?> CreateAsync(
        decimal amount,
        EntryType type,
        string description,
        Guid? accountId,
        Guid? categoryId,
        RecurrenceFrequency frequency,
        DateTime startAt,
        DateTime? endAt)
    {
        if (!await IsValidAsync(amount, type, accountId, categoryId, startAt, endAt))
            return null;

        var normalizedStart = ToUtc(startAt);
        var recurringEntry = new RecurringEntry
        {
            Amount = amount,
            Type = type,
            Description = description,
            AccountId = accountId,
            CategoryId = categoryId,
            Frequency = frequency,
            StartAt = normalizedStart,
            EndAt = endAt.HasValue ? ToUtc(endAt.Value) : null,
            NextOccurrenceAt = normalizedStart
        };

        await _recurringEntryRepository.AddAsync(recurringEntry);
        await _recurringEntryRepository.SaveChangesAsync();
        return recurringEntry;
    }

    public async Task<RecurringEntry?> UpdateAsync(
        Guid id,
        decimal amount,
        EntryType type,
        string description,
        Guid? accountId,
        Guid? categoryId,
        RecurrenceFrequency frequency,
        DateTime startAt,
        DateTime? endAt,
        bool isActive)
    {
        var recurringEntry = await _recurringEntryRepository.GetByIdAsync(id);
        if (recurringEntry is null)
        {
            Notify("Recorrência não encontrada.");
            return null;
        }

        if (!await IsValidAsync(amount, type, accountId, categoryId, startAt, endAt))
            return null;

        var previousStart = recurringEntry.StartAt;
        recurringEntry.Amount = amount;
        recurringEntry.Type = type;
        recurringEntry.Description = description;
        recurringEntry.AccountId = accountId;
        recurringEntry.CategoryId = categoryId;
        recurringEntry.Frequency = frequency;
        recurringEntry.StartAt = ToUtc(startAt);
        recurringEntry.EndAt = endAt.HasValue ? ToUtc(endAt.Value) : null;
        recurringEntry.IsActive = isActive;

        if (recurringEntry.NextOccurrenceAt == previousStart)
            recurringEntry.NextOccurrenceAt = recurringEntry.StartAt;

        _recurringEntryRepository.Update(recurringEntry);
        await _recurringEntryRepository.SaveChangesAsync();
        return recurringEntry;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var recurringEntry = await _recurringEntryRepository.GetByIdAsync(id);
        if (recurringEntry is null)
        {
            Notify("Recorrência não encontrada.");
            return false;
        }

        _recurringEntryRepository.Remove(recurringEntry);
        await _recurringEntryRepository.SaveChangesAsync();
        return true;
    }

    public Task<List<RecurringEntry>> GetAllAsync() =>
        _recurringEntryRepository.GetAllAsync();

    public async Task<int> GenerateDueEntriesAsync(DateTime until)
    {
        var normalizedUntil = ToUtc(until);
        var recurringEntries = await _recurringEntryRepository.GetDueAsync(normalizedUntil);
        var generatedEntries = 0;

        foreach (var recurringEntry in recurringEntries)
            generatedEntries += await GenerateOccurrencesAsync(recurringEntry, normalizedUntil);

        await _recurringEntryRepository.SaveChangesAsync();
        return generatedEntries;
    }

    private async Task<int> GenerateOccurrencesAsync(RecurringEntry recurringEntry, DateTime until)
    {
        var generatedEntries = 0;

        while (recurringEntry.IsActive && recurringEntry.NextOccurrenceAt <= until)
        {
            if (HasPassedEndDate(recurringEntry))
            {
                recurringEntry.IsActive = false;
                break;
            }

            var entry = await _entryService.CreateAsync(
                recurringEntry.Amount,
                recurringEntry.Type,
                recurringEntry.Description,
                recurringEntry.NextOccurrenceAt,
                recurringEntry.AccountId,
                recurringEntry.CategoryId,
                true);

            if (entry is null)
                break;

            generatedEntries++;
            recurringEntry.NextOccurrenceAt = GetNextOccurrence(
                recurringEntry.NextOccurrenceAt,
                recurringEntry.Frequency);

            if (HasPassedEndDate(recurringEntry))
                recurringEntry.IsActive = false;
        }

        return generatedEntries;
    }

    private async Task<bool> IsValidAsync(
        decimal amount,
        EntryType type,
        Guid? accountId,
        Guid? categoryId,
        DateTime startAt,
        DateTime? endAt)
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

        if (categoryId.HasValue)
        {
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
        }

        if (endAt.HasValue && endAt.Value < startAt)
        {
            Notify("End date must be greater than or equal to start date.");
            return false;
        }

        return true;
    }

    private static bool HasPassedEndDate(RecurringEntry recurringEntry) =>
        recurringEntry.EndAt.HasValue
        && recurringEntry.NextOccurrenceAt > recurringEntry.EndAt.Value;

    private static DateTime ToUtc(DateTime value) =>
        DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private static DateTime GetNextOccurrence(DateTime occurrence, RecurrenceFrequency frequency) =>
        frequency switch
        {
            RecurrenceFrequency.Weekly => occurrence.AddDays(7),
            RecurrenceFrequency.Yearly => occurrence.AddYears(1),
            _ => occurrence.AddMonths(1)
        };

    private void Notify(string message) =>
        _notificator.Handle(new Notification(message));
}
