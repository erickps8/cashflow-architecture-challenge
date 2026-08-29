using CashFlow.Launch.Domain.Entities.Base;
using CashFlow.Launch.Domain.Interfaces.Base;
using CashFlow.Launch.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Launch.Infrastructure.Repositories.Base;

public class BaseRepository<TEntity> : IBaseRepository<TEntity>
    where TEntity : Entity
{
    protected readonly CashFlowDbContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    public BaseRepository(CashFlowDbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

    public async Task AddAsync(TEntity entity) => await _dbSet.AddAsync(entity);
    public async Task<TEntity?> GetByIdAsync(Guid id) => await _dbSet.FirstOrDefaultAsync(x => x.Id == id);
    public async Task<List<TEntity>> GetAllAsync() => await _dbSet.AsNoTracking().ToListAsync();
    public void Update(TEntity entity) => _dbSet.Update(entity);
    public void Remove(TEntity entity) => _dbSet.Remove(entity);
    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}