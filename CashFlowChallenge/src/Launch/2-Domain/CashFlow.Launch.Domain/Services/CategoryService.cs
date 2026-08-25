using CashFlow.Launch.Domain.Entities;
using CashFlow.Launch.Domain.Enums;
using CashFlow.Launch.Domain.Interfaces;
using CashFlow.Launch.Domain.Interfaces.Services;

namespace CashFlow.Launch.Domain.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _repository;

    public CategoryService(ICategoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Category> CreateAsync(string name, EntryType type)
    {
        var category = new Category
        {
            Name = name.Trim(),
            Type = type
        };

        await _repository.AddAsync(category);
        await _repository.SaveChangesAsync();
        return category;
    }

    public Task<List<Category>> GetAllAsync() => _repository.GetAllAsync();

    public Task<Category?> GetByIdAsync(Guid id) => _repository.GetByIdAsync(id);
}
