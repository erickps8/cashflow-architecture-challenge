using CashFlow.Launch.Domain.Entities;
using CashFlow.Launch.Domain.Interfaces;
using CashFlow.Launch.Infrastructure.Context;
using CashFlow.Launch.Infrastructure.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Launch.Infrastructure.Repositories;

public class EntryRepository : BaseRepository<Entry>, IEntryRepository
{
    public EntryRepository(CashFlowDbContext context) : base(context)
    {
    }

    public Task<List<Entry>> GetByPeriodAsync(DateTime start, DateTime end)
    {
        return _context.Entries
            .AsNoTracking()
            .Include(x => x.Account)
            .Include(x => x.Category)
            .Where(x => x.OccurredAt >= start && x.OccurredAt < end)
            .OrderBy(x => x.OccurredAt)
            .ToListAsync();
    }
}
