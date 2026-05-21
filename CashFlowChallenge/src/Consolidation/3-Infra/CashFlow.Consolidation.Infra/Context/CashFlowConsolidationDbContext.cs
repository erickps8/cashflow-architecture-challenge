using CashFlow.Consolidation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

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