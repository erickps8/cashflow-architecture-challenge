using CashFlow.Launch.Domain.Entities;
using CashFlow.Launch.Domain.Enums;
using CashFlow.Launch.Domain.Interfaces;
using CashFlow.Launch.Infrastructure.Context;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Tests;

public class TenantIsolationTests
{
    private sealed class Tenant(Guid groupId, bool system = false) : ITenantContext
    {
        public Guid GroupId { get; } = groupId;
        public bool HasGroup => GroupId != Guid.Empty;
        public bool IsSystem { get; } = system;
    }

    private static DbContextOptions<CashFlowDbContext> Options(string database) =>
        new DbContextOptionsBuilder<CashFlowDbContext>().UseInMemoryDatabase(database).Options;

    [Fact]
    public async Task Different_Groups_Must_Not_See_Each_Others_Accounts()
    {
        var database = Guid.NewGuid().ToString();
        var groupA = Guid.NewGuid();
        var groupB = Guid.NewGuid();

        await using (var a = new CashFlowDbContext(Options(database), new Tenant(groupA)))
        {
            a.Accounts.Add(new Account { Name = "Conta Grupo A", Type = AccountType.Checking, InitialBalance = 100 });
            await a.SaveChangesAsync();
        }

        await using (var b = new CashFlowDbContext(Options(database), new Tenant(groupB)))
        {
            b.Accounts.Add(new Account { Name = "Conta Grupo B", Type = AccountType.Checking, InitialBalance = 200 });
            await b.SaveChangesAsync();
        }

        await using var readA = new CashFlowDbContext(Options(database), new Tenant(groupA));
        await using var readB = new CashFlowDbContext(Options(database), new Tenant(groupB));

        (await readA.Accounts.Select(x => x.Name).ToListAsync()).Should().Equal("Conta Grupo A");
        (await readB.Accounts.Select(x => x.Name).ToListAsync()).Should().Equal("Conta Grupo B");
    }

    [Fact]
    public async Task Users_With_Same_Group_Must_See_The_Same_Data()
    {
        var database = Guid.NewGuid().ToString();
        var sharedGroup = Guid.NewGuid();

        await using (var firstUser = new CashFlowDbContext(Options(database), new Tenant(sharedGroup)))
        {
            firstUser.Accounts.Add(new Account { Name = "Conta Compartilhada", Type = AccountType.Checking, InitialBalance = 350 });
            await firstUser.SaveChangesAsync();
        }

        await using var secondUser = new CashFlowDbContext(Options(database), new Tenant(sharedGroup));
        var account = await secondUser.Accounts.SingleAsync();

        account.Name.Should().Be("Conta Compartilhada");
        account.InitialBalance.Should().Be(350);
        account.GroupId.Should().Be(sharedGroup);
    }

    [Fact]
    public async Task New_Entities_Must_Automatically_Receive_Current_Group()
    {
        var database = Guid.NewGuid().ToString();
        var group = Guid.NewGuid();
        await using var db = new CashFlowDbContext(Options(database), new Tenant(group));

        var account = new Account { Name = "Nova conta", Type = AccountType.Checking };
        db.Accounts.Add(account);
        await db.SaveChangesAsync();

        account.GroupId.Should().Be(group);
    }

    [Fact]
    public async Task Creating_Financial_Data_Without_Group_Must_Be_Rejected()
    {
        var database = Guid.NewGuid().ToString();
        await using var db = new CashFlowDbContext(Options(database), new Tenant(Guid.Empty));
        db.Accounts.Add(new Account { Name = "Sem grupo", Type = AccountType.Checking });

        var act = () => db.SaveChangesAsync();

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Grupo não identificado*");
    }

    [Fact]
    public async Task System_Context_Can_Read_All_Groups_For_Background_Processing()
    {
        var database = Guid.NewGuid().ToString();
        var groupA = Guid.NewGuid();
        var groupB = Guid.NewGuid();
        await using (var a = new CashFlowDbContext(Options(database), new Tenant(groupA)))
        { a.Accounts.Add(new Account { Name = "A", Type = AccountType.Checking }); await a.SaveChangesAsync(); }
        await using (var b = new CashFlowDbContext(Options(database), new Tenant(groupB)))
        { b.Accounts.Add(new Account { Name = "B", Type = AccountType.Checking }); await b.SaveChangesAsync(); }

        await using var system = new CashFlowDbContext(Options(database), new Tenant(Guid.Empty, true));
        (await system.Accounts.CountAsync()).Should().Be(2);
    }
}
