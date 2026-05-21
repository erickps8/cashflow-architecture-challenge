using CashFlow.Consolidation.Domain.Entities;
using CashFlow.Consolidation.Domain.Interfaces;
using CashFlow.Consolidation.Domain.Notifications;

namespace CashFlow.Consolidation.Domain.Services;

public class DailyConsolidationService : IDailyConsolidationService
{
    private readonly IDailyConsolidationRepository _repository;
    private readonly INotificator _notificator;

    public DailyConsolidationService(
        IDailyConsolidationRepository repository,
        INotificator notificator)
    {
        _repository = repository;
        _notificator = notificator;
    }

    public async Task<IEnumerable<DailyConsolidation>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }
    public async Task<DailyConsolidation> ReprocessAsync()
    {
        var consolidation = await _repository.GetByDateAsync(DateTime.Today);

        if (consolidation is null)
        {
            consolidation = new DailyConsolidation
            {
                Date = DateTime.Today
            };

            await _repository.AddAsync(consolidation);
        }

        consolidation.TotalCredits = 1000;
        consolidation.TotalDebits = 250;
        consolidation.Balance = 750;

        await _repository.SaveChangesAsync();

        return consolidation;
    }
}