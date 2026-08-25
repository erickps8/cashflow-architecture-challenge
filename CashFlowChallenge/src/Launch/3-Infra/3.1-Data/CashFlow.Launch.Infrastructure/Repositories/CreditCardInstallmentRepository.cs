using CashFlow.Launch.Domain.Entities;
using CashFlow.Launch.Domain.Interfaces;
using CashFlow.Launch.Infrastructure.Context;
using CashFlow.Launch.Infrastructure.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Launch.Infrastructure.Repositories;

public class CreditCardInstallmentRepository : BaseRepository<CreditCardInstallment>, ICreditCardInstallmentRepository
{
    private readonly CashFlowDbContext _context;
    public CreditCardInstallmentRepository(CashFlowDbContext context) : base(context) => _context = context;

    public Task<List<CreditCardInstallment>> GetByCardAndReferenceAsync(Guid creditCardId, int year, int month)
    {
        return _context.CreditCardInstallments
            .AsNoTracking()
            .Include(x => x.CreditCardPurchase)
            .Where(x => x.CreditCardPurchase!.CreditCardId == creditCardId && x.ReferenceDate.Year == year && x.ReferenceDate.Month == month)
            .OrderBy(x => x.DueDate)
            .ToListAsync();
    }
}
