using CashFlow.Launch.Domain.Entities;
using CashFlow.Launch.Domain.Enums;
using CashFlow.Launch.Domain.Interfaces;
using CashFlow.Launch.Domain.Interfaces.Services;

namespace CashFlow.Launch.Domain.Services;

public sealed class AccountService : IAccountService
{
    private readonly IAccountRepository _accountRepository;

    public AccountService(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<Account> CreateAsync(string name, AccountType type, decimal initialBalance)
    {
        var account = new Account
        {
            Name = name.Trim(),
            Type = type,
            InitialBalance = initialBalance
        };

        await _accountRepository.AddAsync(account);
        await _accountRepository.SaveChangesAsync();
        return account;
    }

    public async Task<Account?> UpdateAsync(
        Guid id,
        string name,
        AccountType type,
        decimal initialBalance)
    {
        var account = await _accountRepository.GetByIdAsync(id);
        if (account is null)
            return null;

        account.Name = name.Trim();
        account.Type = type;
        account.InitialBalance = initialBalance;

        _accountRepository.Update(account);
        await _accountRepository.SaveChangesAsync();
        return account;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var account = await _accountRepository.GetByIdAsync(id);
        if (account is null)
            return false;

        account.IsActive = false;
        _accountRepository.Update(account);
        await _accountRepository.SaveChangesAsync();
        return true;
    }

    public Task<List<Account>> GetAllAsync() => _accountRepository.GetAllAsync();

    public Task<Account?> GetByIdAsync(Guid id) => _accountRepository.GetByIdAsync(id);
}
