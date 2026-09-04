using CashFlow.Launch.Domain.Entities;
using CashFlow.Launch.Domain.Entities.Base;
using CashFlow.Launch.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Launch.Infrastructure.Context;

public class CashFlowDbContext : DbContext
{
    private readonly ITenantContext? _tenant;
    public Guid CurrentGroupId => _tenant?.GroupId ?? Guid.Empty;
    public bool IsSystemContext => _tenant?.IsSystem == true;

    public CashFlowDbContext(DbContextOptions<CashFlowDbContext> options, ITenantContext? tenant = null) : base(options) => _tenant = tenant;

    public DbSet<Entry> Entries => Set<Entry>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<RecurringEntry> RecurringEntries => Set<RecurringEntry>();
    public DbSet<CreditCard> CreditCards => Set<CreditCard>();
    public DbSet<CreditCardPurchase> CreditCardPurchases => Set<CreditCardPurchase>();
    public DbSet<CreditCardInstallment> CreditCardInstallments => Set<CreditCardInstallment>();
    public DbSet<MonthlyBudget> MonthlyBudgets => Set<MonthlyBudget>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<Entry>().HasQueryFilter(x => IsSystemContext || x.GroupId == CurrentGroupId);
        b.Entity<Account>().HasQueryFilter(x => IsSystemContext || x.GroupId == CurrentGroupId);
        b.Entity<Category>().HasQueryFilter(x => IsSystemContext || x.GroupId == CurrentGroupId);
        b.Entity<RecurringEntry>().HasQueryFilter(x => IsSystemContext || x.GroupId == CurrentGroupId);
        b.Entity<CreditCard>().HasQueryFilter(x => IsSystemContext || x.GroupId == CurrentGroupId);
        b.Entity<CreditCardPurchase>().HasQueryFilter(x => IsSystemContext || x.GroupId == CurrentGroupId);
        b.Entity<CreditCardInstallment>().HasQueryFilter(x => IsSystemContext || x.GroupId == CurrentGroupId);
        b.Entity<MonthlyBudget>().HasQueryFilter(x => IsSystemContext || x.GroupId == CurrentGroupId);
        b.Entity<OutboxMessage>().HasQueryFilter(x => IsSystemContext || x.GroupId == CurrentGroupId);
    }

    public override int SaveChanges() { ApplyTenant(); return base.SaveChanges(); }
    public override Task<int> SaveChangesAsync(CancellationToken ct = default) { ApplyTenant(); return base.SaveChangesAsync(ct); }

    private void ApplyTenant()
    {
        var added = ChangeTracker.Entries<Entity>().Where(x => x.State == EntityState.Added).ToList();
        if (added.Count == 0) return;
        if (_tenant?.HasGroup == true)
            foreach (var entry in added) entry.Entity.GroupId = CurrentGroupId;
        else if (!IsSystemContext)
            throw new UnauthorizedAccessException("Grupo não identificado no token.");
    }
}
