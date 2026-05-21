using CashFlow.Consolidation.Domain.Entities;
using CashFlow.Consolidation.Domain.Interfaces.Base;

namespace CashFlow.Consolidation.Domain.Interfaces;

public interface IDailyConsolidationRepository : IBaseRepository<DailyConsolidation>
{
    Task<DailyConsolidation?> GetByDateAsync(DateTime date);
}