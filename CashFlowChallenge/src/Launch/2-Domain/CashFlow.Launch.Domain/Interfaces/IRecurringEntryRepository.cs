using CashFlow.Launch.Domain.Entities;
using CashFlow.Launch.Domain.Interfaces.Base;

namespace CashFlow.Launch.Domain.Interfaces;

public interface IRecurringEntryRepository : IBaseRepository<RecurringEntry>
{
    Task<List<RecurringEntry>> GetDueAsync(DateTime until);
}
