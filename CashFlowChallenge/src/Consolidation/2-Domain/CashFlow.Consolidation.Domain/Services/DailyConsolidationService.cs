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
        try
        {
            return await _repository.GetAllAsync();
        }
        catch (Exception ex)
        {
            Notify($"Erro ao consultar consolidações: {ex.Message}");
            return Enumerable.Empty<DailyConsolidation>();
        }
    }

    public async Task<DailyConsolidation> ReprocessAsync()
    {
        try
        {
            var consolidation =
                await _repository.GetByDateAsync(DateTime.Today);

            if (consolidation is null)
            {
                Notify("Não existe consolidação para reprocessar.");

                return null!;
            }

            consolidation.Balance =
                consolidation.TotalCredits - consolidation.TotalDebits;

            await _repository.SaveChangesAsync();

            return consolidation;
        }
        catch (Exception ex)
        {
            Notify($"Erro ao reprocessar consolidação: {ex.Message}");

            return null!;
        }
    }

    public async Task ProcessEntryAsync(
        decimal amount,
        int type,
        DateTime occurredAt)
    {
        try
        {
            var date = occurredAt.Date;

            var consolidation = await _repository.GetByDateAsync(date);

            if (consolidation is null)
            {
                consolidation = new DailyConsolidation
                {
                    Date = date,
                    TotalCredits = 0,
                    TotalDebits = 0,
                    Balance = 0
                };

                await _repository.AddAsync(consolidation);
            }

            if (type == 1)
            {
                consolidation.TotalCredits += amount;
            }
            else
            {
                consolidation.TotalDebits += amount;
            }

            consolidation.Balance =
                consolidation.TotalCredits - consolidation.TotalDebits;

            await _repository.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Notify($"Erro ao processar lançamento: {ex.Message}");

            throw;
        }
    }
    private void Notify(string message)
    {
        _notificator.Handle(new Notification(message));
    }
}