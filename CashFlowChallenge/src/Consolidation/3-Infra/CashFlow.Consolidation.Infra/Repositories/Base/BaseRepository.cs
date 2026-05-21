using CashFlow.Consolidation.Domain.Entities.Base;
using CashFlow.Consolidation.Domain.Interfaces.Base;
using CashFlow.Consolidation.Infra.Context;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Consolidation.Infra.Repositories.Base;

public class BaseRepository<TEntity> : IBaseRepository<TEntity>
    where TEntity : Entity
{
    protected readonly CashFlowConsolidationDbContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    public BaseRepository(CashFlowConsolidationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

    public async Task AddAsync(TEntity entity)
    {
        await _dbSet.AddAsync(entity);
    }
    public async Task<TEntity?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<List<TEntity>> GetAllAsync()
    {
        return await _dbSet.AsNoTracking().ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}