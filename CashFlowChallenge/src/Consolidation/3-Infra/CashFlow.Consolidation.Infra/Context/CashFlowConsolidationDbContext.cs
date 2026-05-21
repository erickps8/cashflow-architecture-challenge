using CashFlow.Consolidation.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Consolidation.Infra.Context;

public class CashFlowConsolidationDbContext : DbContext
{
    public CashFlowConsolidationDbContext(
        DbContextOptions<CashFlowConsolidationDbContext> options)
    : base(options)
    {
    }

    public DbSet<DailyConsolidation> DailyConsolidations { get; set; }

}