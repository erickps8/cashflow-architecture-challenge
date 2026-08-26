using CashFlow.Launch.Domain.Entities;
using CashFlow.Launch.Domain.Enums;
using CashFlow.Launch.Domain.Interfaces;
using CashFlow.Launch.Domain.Interfaces.Services;

namespace CashFlow.Launch.Domain.Services;

public class AccountService : IAccountService
{
    private readonly IAccountRepository _repository;
    public AccountService(IAccountRepository repository)=>_repository=repository;
    public async Task<Account> CreateAsync(string name,AccountType type,decimal initialBalance){var account=new Account{Name=name.Trim(),Type=type,InitialBalance=initialBalance};await _repository.AddAsync(account);await _repository.SaveChangesAsync();return account;}
    public async Task<Account?> UpdateAsync(Guid id,string name,AccountType type,decimal initialBalance){var x=await _repository.GetByIdAsync(id);if(x is null)return null;x.Name=name.Trim();x.Type=type;x.InitialBalance=initialBalance;_repository.Update(x);await _repository.SaveChangesAsync();return x;}
    public async Task<bool> DeleteAsync(Guid id){var x=await _repository.GetByIdAsync(id);if(x is null)return false;x.IsActive=false;_repository.Update(x);await _repository.SaveChangesAsync();return true;}
    public Task<List<Account>> GetAllAsync()=>_repository.GetAllAsync();public Task<Account?> GetByIdAsync(Guid id)=>_repository.GetByIdAsync(id);
}
