using CashFlow.Launch.Domain.Entities;
using CashFlow.Launch.Domain.Interfaces.Base;

namespace CashFlow.Launch.Domain.Interfaces;

public interface ICreditCardInstallmentRepository : IBaseRepository<CreditCardInstallment>
{
    Task<List<CreditCardInstallment>> GetByCardAndReferenceAsync(Guid creditCardId, int year, int month);
    Task<List<CreditCardInstallment>> GetByReferenceAsync(int year, int month);
}
