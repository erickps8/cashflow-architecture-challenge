using CashFlow.Launch.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Launch.Infrastructure.Context;

public class CashFlowDbContext : DbContext
{
    public CashFlowDbContext(DbContextOptions<CashFlowDbContext> options) : base(options) { }

    public DbSet<Entry> Entries { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<RecurringEntry> RecurringEntries { get; set; }
}
