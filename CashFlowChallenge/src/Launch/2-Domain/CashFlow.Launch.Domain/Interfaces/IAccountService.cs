using CashFlow.Launch.Domain.Entities;
using CashFlow.Launch.Domain.Enums;

namespace CashFlow.Launch.Domain.Interfaces.Services;

public interface IAccountService
{
    Task<Account> CreateAsync(string name, AccountType type, decimal initialBalance);
    Task<Account?> UpdateAsync(Guid id, string name, AccountType type, decimal initialBalance);
    Task<bool> DeleteAsync(Guid id);
    Task<List<Account>> GetAllAsync();
    Task<Account?> GetByIdAsync(Guid id);
}
