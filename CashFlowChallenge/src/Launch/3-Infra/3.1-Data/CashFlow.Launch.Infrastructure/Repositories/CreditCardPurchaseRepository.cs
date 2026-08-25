using CashFlow.Launch.Domain.Entities;
using CashFlow.Launch.Domain.Interfaces;
using CashFlow.Launch.Infrastructure.Context;
using CashFlow.Launch.Infrastructure.Repositories.Base;

namespace CashFlow.Launch.Infrastructure.Repositories;

public class CreditCardPurchaseRepository : BaseRepository<CreditCardPurchase>, ICreditCardPurchaseRepository
{
    public CreditCardPurchaseRepository(CashFlowDbContext context) : base(context) { }
}
