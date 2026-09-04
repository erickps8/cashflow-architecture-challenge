using CashFlow.Launch.Domain.Entities.Base;

namespace CashFlow.Launch.Domain.Interfaces.Base;

public interface IBaseRepository<TEntity> where TEntity : Entity
{
    Task AddAsync(TEntity entity);
    Task<TEntity?> GetByIdAsync(Guid id);
    Task<List<TEntity>> GetAllAsync();
    void Update(TEntity entity);
    void Remove(TEntity entity);
    Task SaveChangesAsync();
}