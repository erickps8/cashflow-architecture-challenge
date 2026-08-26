using CashFlow.Launch.Infrastructure.Context;using Microsoft.EntityFrameworkCore.Infrastructure;using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace CashFlow.Launch.Infrastructure.Migrations;
[DbContext(typeof(CashFlowDbContext))][Migration("20260826223000_AddGroupIsolation")]public partial class AddGroupIsolation:Migration{
static readonly string[] Tables=["Accounts","Categories","Entries","RecurringEntries","CreditCards","CreditCardPurchases","CreditCardInstallments","MonthlyBudgets","OutboxMessages"];
protected override void Up(MigrationBuilder m){foreach(var t in Tables){m.AddColumn<Guid>(name:"GroupId",table:t,type:"uuid",nullable:false,defaultValue:new Guid("11111111-1111-1111-1111-111111111111"));m.CreateIndex(name:$"IX_{t}_GroupId",table:t,column:"GroupId");}}
protected override void Down(MigrationBuilder m){foreach(var t in Tables){m.DropIndex(name:$"IX_{t}_GroupId",table:t);m.DropColumn(name:"GroupId",table:t);}}}