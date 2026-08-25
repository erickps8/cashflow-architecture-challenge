using System;
using CashFlow.Launch.Infrastructure.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CashFlow.Launch.Infrastructure.Migrations
{
    [DbContext(typeof(CashFlowDbContext))]
    [Migration("20260825203000_AddMonthlyBudgets")]
    public partial class AddMonthlyBudgets : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MonthlyBudgets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlannedAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyBudgets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyBudgets_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyBudgets_CategoryId",
                table: "MonthlyBudgets",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyBudgets_Year_Month_CategoryId",
                table: "MonthlyBudgets",
                columns: new[] { "Year", "Month", "CategoryId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "MonthlyBudgets");
        }
    }
}
