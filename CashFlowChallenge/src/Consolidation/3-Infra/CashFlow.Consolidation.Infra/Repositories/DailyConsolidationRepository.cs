using CashFlow.Consolidation.Domain.Entities;
using CashFlow.Consolidation.Domain.Interfaces;
using CashFlow.Consolidation.Infra.Context;
using CashFlow.Consolidation.Infra.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Consolidation.Infra.Repositories;

public class DailyConsolidationRepository
    : BaseRepository<DailyConsolidation>, IDailyConsolidationRepository
{
    public DailyConsolidationRepository(
        CashFlowConsolidationDbContext context)
        : base(context)
    {
    }

    public async Task<DailyConsolidation?> GetByDateAsync(DateTime date)
    {
        return await _context.DailyConsolidations
            .FirstOrDefaultAsync(x => x.Date.Date == date.Date);
    }
}