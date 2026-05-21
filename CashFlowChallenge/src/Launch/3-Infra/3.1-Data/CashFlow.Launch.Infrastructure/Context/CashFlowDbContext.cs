using CashFlow.Launch.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Launch.Infrastructure.Context;

public class CashFlowDbContext : DbContext
{
    public CashFlowDbContext(DbContextOptions<CashFlowDbContext> options)
        : base(options)
    {
    }

    public DbSet<Entry> Entries { get; set; }
}