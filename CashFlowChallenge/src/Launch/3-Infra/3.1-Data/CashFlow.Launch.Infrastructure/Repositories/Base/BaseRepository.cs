using CashFlow.Launch.Domain.Entities.Base;
using CashFlow.Launch.Domain.Interfaces.Base;
using CashFlow.Launch.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Launch.Infrastructure.Repositories.Base;

public class BaseRepository<TEntity> : IBaseRepository<TEntity>
    where TEntity : Entity
{
    protected readonly CashFlowDbContext Context;
    protected readonly DbSet<TEntity> DbSet;

    public BaseRepository(CashFlowDbContext context)
    {
        Context = context;
        DbSet = context.Set<TEntity>();
    }

    public async Task AddAsync(TEntity entity)
    {
        await DbSet.AddAsync(entity);
    }

    public async Task<TEntity?> GetByIdAsync(Guid id)
    {
        return await DbSet.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<List<TEntity>> GetAllAsync()
    {
        return await DbSet.AsNoTracking().ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await Context.SaveChangesAsync();
    }
}