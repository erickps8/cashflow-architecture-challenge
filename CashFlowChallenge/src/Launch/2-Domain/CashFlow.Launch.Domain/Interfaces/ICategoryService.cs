using CashFlow.Launch.Domain.Entities;
using CashFlow.Launch.Domain.Enums;

namespace CashFlow.Launch.Domain.Interfaces.Services;

public interface ICategoryService
{
    Task<Category> CreateAsync(string name, EntryType type);
    Task<List<Category>> GetAllAsync();
    Task<Category?> GetByIdAsync(Guid id);
}
