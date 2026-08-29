using CashFlow.Auth.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Auth.Api.Data;

public class AuthDbContext : IdentityDbContext<IdentityUser>
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    public DbSet<CashFlowGroup> Groups => Set<CashFlowGroup>();
    public DbSet<GroupMembership> GroupMemberships => Set<GroupMembership>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<CashFlowGroup>(e =>
        {
            e.ToTable("CashFlowGroups");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.NormalizedName).HasMaxLength(120).IsRequired();
            e.HasIndex(x => x.NormalizedName).IsUnique();
        });
        builder.Entity<GroupMembership>(e =>
        {
            e.ToTable("GroupMemberships");
            e.HasKey(x => x.Id);
            e.Property(x => x.UserId).IsRequired();
            e.HasIndex(x => new { x.GroupId, x.UserId }).IsUnique();
            e.HasOne(x => x.Group).WithMany(x => x.Memberships).HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}