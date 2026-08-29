using CashFlow.Launch.Domain.Entities;
using CashFlow.Launch.Domain.Enums;

namespace CashFlow.Launch.Domain.Interfaces.Services;

public interface IEntryService
{
    Task<Entry?> CreateAsync(decimal amount, EntryType type, string description, DateTime occurredAt, Guid? accountId = null, Guid? categoryId = null, bool isRecurring = false);
    Task<Entry?> UpdateAsync(Guid id, decimal amount, EntryType type, string description, DateTime occurredAt, Guid? accountId = null, Guid? categoryId = null, bool isRecurring = false);
    Task<bool> DeleteAsync(Guid id);
    Task<List<Entry>> GetAllAsync();
    Task<List<Entry>> GetByMonthAsync(int year, int month);
}