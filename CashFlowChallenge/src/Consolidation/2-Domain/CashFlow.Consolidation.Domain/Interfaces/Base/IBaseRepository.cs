using CashFlow.Consolidation.Domain.Entities.Base;

namespace CashFlow.Consolidation.Domain.Interfaces.Base;

public interface IBaseRepository<TEntity> where TEntity : Entity
{
    Task AddAsync(TEntity entity);

    Task<TEntity?> GetByIdAsync(Guid id);
    Task<List<TEntity>> GetAllAsync();

    Task SaveChangesAsync();
}