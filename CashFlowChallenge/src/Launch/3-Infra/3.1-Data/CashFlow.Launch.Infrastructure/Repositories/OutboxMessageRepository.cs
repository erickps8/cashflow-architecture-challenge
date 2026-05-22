using CashFlow.Launch.Domain.Entities;
using CashFlow.Launch.Domain.Interfaces;
using CashFlow.Launch.Infrastructure.Context;
using CashFlow.Launch.Infrastructure.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Launch.Infrastructure.Repositories;

public class OutboxMessageRepository
    : BaseRepository<OutboxMessage>, IOutboxMessageRepository
{
    public OutboxMessageRepository(CashFlowDbContext context)
        : base(context)
    {
    }

    public async Task<IEnumerable<OutboxMessage>> GetPendingAsync()
    {
        return await _context.OutboxMessages
            .Where(x => x.ProcessedAt == null)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
    }
}