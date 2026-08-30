using CashFlow.Launch.Domain.Entities;
using CashFlow.Launch.Domain.Interfaces;
using CashFlow.Launch.Infrastructure.Context;
using CashFlow.Launch.Infrastructure.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Launch.Infrastructure.Repositories;

public class RecurringEntryRepository : BaseRepository<RecurringEntry>, IRecurringEntryRepository
{
    public RecurringEntryRepository(CashFlowDbContext context) : base(context)
    {
    }

    public Task<List<RecurringEntry>> GetDueAsync(DateTime until)
    {
        return _context.RecurringEntries
            .Where(x => x.IsActive && x.NextOccurrenceAt <= until)
            .OrderBy(x => x.NextOccurrenceAt)
            .ToListAsync();
    }
}
