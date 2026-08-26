using CashFlow.Launch.Domain.Entities;
using CashFlow.Launch.Domain.Enums;
using CashFlow.Launch.Domain.Events;
using CashFlow.Launch.Domain.Interfaces;
using CashFlow.Launch.Domain.Interfaces.Services;
using CashFlow.Launch.Domain.Notifications;
using System.Text.Json;

namespace CashFlow.Launch.Domain.Services;

public class EntryService : IEntryService
{
    private readonly IEntryRepository _repository;
    private readonly INotificator _notificator;
    private readonly IOutboxMessageRepository _outboxRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ICategoryRepository _categoryRepository;

    public EntryService(IEntryRepository repository, IOutboxMessageRepository outboxRepository, INotificator notificator, IAccountRepository accountRepository, ICategoryRepository categoryRepository)
    { _repository=repository;_outboxRepository=outboxRepository;_notificator=notificator;_accountRepository=accountRepository;_categoryRepository=categoryRepository; }

    public async Task<Entry?> CreateAsync(decimal amount, EntryType type, string description, DateTime occurredAt, Guid? accountId=null, Guid? categoryId=null, bool isRecurring=false)
    {
        if (!await ValidateAsync(amount,type,accountId,categoryId)) return null;
        var entry=new Entry{Amount=amount,Type=type,Description=description,OccurredAt=Utc(occurredAt),AccountId=accountId,CategoryId=categoryId,IsRecurring=isRecurring};
        await _repository.AddAsync(entry);
        var evt=new EntryCreatedEvent{EntryId=entry.Id,Amount=entry.Amount,Type=(int)entry.Type,Description=entry.Description,OccurredAt=entry.OccurredAt};
        await _outboxRepository.AddAsync(new OutboxMessage{Type=nameof(EntryCreatedEvent),Payload=JsonSerializer.Serialize(evt)});
        await _repository.SaveChangesAsync();return entry;
    }

    public async Task<Entry?> UpdateAsync(Guid id, decimal amount, EntryType type, string description, DateTime occurredAt, Guid? accountId=null, Guid? categoryId=null, bool isRecurring=false)
    {
        var entry=await _repository.GetByIdAsync(id);if(entry is null){Notify("Lançamento não encontrado.");return null;}
        if(!await ValidateAsync(amount,type,accountId,categoryId))return null;
        entry.Amount=amount;entry.Type=type;entry.Description=description;entry.OccurredAt=Utc(occurredAt);entry.AccountId=accountId;entry.CategoryId=categoryId;entry.IsRecurring=isRecurring;
        _repository.Update(entry);await _repository.SaveChangesAsync();return entry;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entry=await _repository.GetByIdAsync(id);if(entry is null){Notify("Lançamento não encontrado.");return false;}
        _repository.Remove(entry);await _repository.SaveChangesAsync();return true;
    }

    public async Task<List<Entry>> GetAllAsync(){try{return await _repository.GetAllAsync();}catch(Exception ex){Notify($"Erro ao consultar lançamentos: {ex.Message}");return[];}}
    public Task<List<Entry>> GetByMonthAsync(int year,int month){if(month<1||month>12)return Task.FromResult(new List<Entry>());var start=new DateTime(year,month,1,0,0,0,DateTimeKind.Utc);return _repository.GetByPeriodAsync(start,start.AddMonths(1));}

    private async Task<bool> ValidateAsync(decimal amount,EntryType type,Guid? accountId,Guid? categoryId)
    {
        if(amount<=0){Notify("Amount must be greater than zero.");return false;}
        if(accountId.HasValue&&await _accountRepository.GetByIdAsync(accountId.Value)is null){Notify("Account not found.");return false;}
        if(categoryId.HasValue){var c=await _categoryRepository.GetByIdAsync(categoryId.Value);if(c is null){Notify("Category not found.");return false;}if(c.Type!=type){Notify("Category type must match entry type.");return false;}}
        return true;
    }
    private static DateTime Utc(DateTime value)=>DateTime.SpecifyKind(value,DateTimeKind.Utc);
    private void Notify(string message)=>_notificator.Handle(new Notification(message));
}