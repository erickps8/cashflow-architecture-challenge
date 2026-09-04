using System;
using CashFlow.Launch.Infrastructure.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CashFlow.Launch.Infrastructure.Migrations
{
    [DbContext(typeof(CashFlowDbContext))]
    [Migration("20260825180000_ExtendEntriesForPersonalFinance")]
    public partial class ExtendEntriesForPersonalFinance : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(name: "AccountId", table: "Entries", type: "uuid", nullable: true);
            migrationBuilder.AddColumn<Guid>(name: "CategoryId", table: "Entries", type: "uuid", nullable: true);
            migrationBuilder.AddColumn<bool>(name: "IsRecurring", table: "Entries", type: "boolean", nullable: false, defaultValue: false);
            migrationBuilder.CreateIndex(name: "IX_Entries_AccountId", table: "Entries", column: "AccountId");
            migrationBuilder.CreateIndex(name: "IX_Entries_CategoryId", table: "Entries", column: "CategoryId");
            migrationBuilder.AddForeignKey(name: "FK_Entries_Accounts_AccountId", table: "Entries", column: "AccountId", principalTable: "Accounts", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
            migrationBuilder.AddForeignKey(name: "FK_Entries_Categories_CategoryId", table: "Entries", column: "CategoryId", principalTable: "Categories", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Entries_Accounts_AccountId", table: "Entries");
            migrationBuilder.DropForeignKey(name: "FK_Entries_Categories_CategoryId", table: "Entries");
            migrationBuilder.DropIndex(name: "IX_Entries_AccountId", table: "Entries");
            migrationBuilder.DropIndex(name: "IX_Entries_CategoryId", table: "Entries");
            migrationBuilder.DropColumn(name: "AccountId", table: "Entries");
            migrationBuilder.DropColumn(name: "CategoryId", table: "Entries");
            migrationBuilder.DropColumn(name: "IsRecurring", table: "Entries");
        }
    }
}
