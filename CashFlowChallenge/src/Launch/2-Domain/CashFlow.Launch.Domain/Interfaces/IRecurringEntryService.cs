using CashFlow.Launch.Domain.Entities;
using CashFlow.Launch.Domain.Enums;

namespace CashFlow.Launch.Domain.Interfaces.Services;

public interface IRecurringEntryService
{
    Task<RecurringEntry?> CreateAsync(decimal amount, EntryType type, string description, Guid? accountId, Guid? categoryId, RecurrenceFrequency frequency, DateTime startAt, DateTime? endAt);
    Task<RecurringEntry?> UpdateAsync(Guid id, decimal amount, EntryType type, string description, Guid? accountId, Guid? categoryId, RecurrenceFrequency frequency, DateTime startAt, DateTime? endAt, bool isActive);
    Task<bool> DeleteAsync(Guid id);
    Task<List<RecurringEntry>> GetAllAsync();
    Task<int> GenerateDueEntriesAsync(DateTime until);
}
