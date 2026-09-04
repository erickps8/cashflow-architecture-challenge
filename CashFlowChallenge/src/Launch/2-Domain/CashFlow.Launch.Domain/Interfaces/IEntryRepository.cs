using CashFlow.Launch.Domain.Entities;
using CashFlow.Launch.Domain.Interfaces.Base;

namespace CashFlow.Launch.Domain.Interfaces;

public interface IEntryRepository : IBaseRepository<Entry>
{
    Task<List<Entry>> GetByPeriodAsync(DateTime start, DateTime end);
}
