using CashFlow.Launch.Domain.Entities;
using CashFlow.Launch.Domain.Enums;

namespace CashFlow.Launch.Domain.Interfaces.Services;

public interface IEntryService
{
    Task<Entry?> CreateAsync(decimal amount, EntryType type, string description, DateTime occurredAt);
    Task<List<Entry>> GetAllAsync();
}