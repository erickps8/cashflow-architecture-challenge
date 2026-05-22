using CashFlow.Consolidation.Domain.Entities;

namespace CashFlow.Consolidation.Domain.Interfaces;

public interface IDailyConsolidationService
{
    Task<IEnumerable<DailyConsolidation>> GetAllAsync();
    Task<DailyConsolidation> ReprocessAsync();
    Task ProcessEntryAsync(
        decimal amount,
        int type,
        DateTime occurredAt);
}