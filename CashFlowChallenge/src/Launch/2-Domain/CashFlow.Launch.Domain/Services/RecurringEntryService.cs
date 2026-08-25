using CashFlow.Launch.Domain.Entities;
using CashFlow.Launch.Domain.Enums;
using CashFlow.Launch.Domain.Interfaces;
using CashFlow.Launch.Domain.Interfaces.Services;
using CashFlow.Launch.Domain.Notifications;

namespace CashFlow.Launch.Domain.Services;

public class RecurringEntryService : IRecurringEntryService
{
    private readonly IRecurringEntryRepository _repository;
    private readonly IEntryService _entryService;
    private readonly IAccountRepository _accountRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly INotificator _notificator;

    public RecurringEntryService(IRecurringEntryRepository repository, IEntryService entryService, IAccountRepository accountRepository, ICategoryRepository categoryRepository, INotificator notificator)
    {
        _repository = repository;
        _entryService = entryService;
        _accountRepository = accountRepository;
        _categoryRepository = categoryRepository;
        _notificator = notificator;
    }

    public async Task<RecurringEntry?> CreateAsync(decimal amount, EntryType type, string description, Guid? accountId, Guid? categoryId, RecurrenceFrequency frequency, DateTime startAt, DateTime? endAt)
    {
        if (amount <= 0) { Notify("Amount must be greater than zero."); return null; }
        if (accountId.HasValue && await _accountRepository.GetByIdAsync(accountId.Value) is null) { Notify("Account not found."); return null; }
        if (categoryId.HasValue)
        {
            var category = await _categoryRepository.GetByIdAsync(categoryId.Value);
            if (category is null) { Notify("Category not found."); return null; }
            if (category.Type != type) { Notify("Category type must match entry type."); return null; }
        }

        startAt = DateTime.SpecifyKind(startAt, DateTimeKind.Utc);
        if (endAt.HasValue) endAt = DateTime.SpecifyKind(endAt.Value, DateTimeKind.Utc);
        if (endAt.HasValue && endAt.Value < startAt) { Notify("End date must be greater than or equal to start date."); return null; }

        var recurring = new RecurringEntry
        {
            Amount = amount,
            Type = type,
            Description = description,
            AccountId = accountId,
            CategoryId = categoryId,
            Frequency = frequency,
            StartAt = startAt,
            EndAt = endAt,
            NextOccurrenceAt = startAt
        };

        await _repository.AddAsync(recurring);
        await _repository.SaveChangesAsync();
        return recurring;
    }

    public Task<List<RecurringEntry>> GetAllAsync() => _repository.GetAllAsync();

    public async Task<int> GenerateDueEntriesAsync(DateTime until)
    {
        until = DateTime.SpecifyKind(until, DateTimeKind.Utc);
        var recurringEntries = await _repository.GetDueAsync(until);
        var generated = 0;

        foreach (var recurring in recurringEntries)
        {
            while (recurring.IsActive && recurring.NextOccurrenceAt <= until)
            {
                if (recurring.EndAt.HasValue && recurring.NextOccurrenceAt > recurring.EndAt.Value)
                {
                    recurring.IsActive = false;
                    break;
                }

                var entry = await _entryService.CreateAsync(recurring.Amount, recurring.Type, recurring.Description, recurring.NextOccurrenceAt, recurring.AccountId, recurring.CategoryId, true);
                if (entry is null) break;

                generated++;
                recurring.NextOccurrenceAt = GetNext(recurring.NextOccurrenceAt, recurring.Frequency);

                if (recurring.EndAt.HasValue && recurring.NextOccurrenceAt > recurring.EndAt.Value)
                    recurring.IsActive = false;
            }
        }

        await _repository.SaveChangesAsync();
        return generated;
    }

    private static DateTime GetNext(DateTime current, RecurrenceFrequency frequency) => frequency switch
    {
        RecurrenceFrequency.Weekly => current.AddDays(7),
        RecurrenceFrequency.Yearly => current.AddYears(1),
        _ => current.AddMonths(1)
    };

    private void Notify(string message) => _notificator.Handle(new Notification(message));
}
