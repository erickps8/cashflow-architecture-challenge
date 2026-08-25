using System;
using CashFlow.Launch.Infrastructure.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CashFlow.Launch.Infrastructure.Migrations
{
    [DbContext(typeof(CashFlowDbContext))]
    [Migration("20260825193000_AddCreditCards")]
    public partial class AddCreditCards : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CreditCards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Limit = table.Column<decimal>(type: "numeric", nullable: false),
                    ClosingDay = table.Column<int>(type: "integer", nullable: false),
                    DueDay = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_CreditCards", x => x.Id));

            migrationBuilder.CreateTable(
                name: "CreditCardPurchases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditCardId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    InstallmentsCount = table.Column<int>(type: "integer", nullable: false),
                    PurchaseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditCardPurchases", x => x.Id);
                    table.ForeignKey("FK_CreditCardPurchases_CreditCards_CreditCardId", x => x.CreditCardId, "CreditCards", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_CreditCardPurchases_Categories_CategoryId", x => x.CategoryId, "Categories", "Id", onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CreditCardInstallments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditCardPurchaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    ReferenceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsPaid = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditCardInstallments", x => x.Id);
                    table.ForeignKey("FK_CreditCardInstallments_CreditCardPurchases_CreditCardPurchaseId", x => x.CreditCardPurchaseId, "CreditCardPurchases", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex("IX_CreditCardPurchases_CreditCardId", "CreditCardPurchases", "CreditCardId");
            migrationBuilder.CreateIndex("IX_CreditCardPurchases_CategoryId", "CreditCardPurchases", "CategoryId");
            migrationBuilder.CreateIndex("IX_CreditCardInstallments_CreditCardPurchaseId", "CreditCardInstallments", "CreditCardPurchaseId");
            migrationBuilder.CreateIndex("IX_CreditCardInstallments_ReferenceDate", "CreditCardInstallments", "ReferenceDate");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CreditCardInstallments");
            migrationBuilder.DropTable(name: "CreditCardPurchases");
            migrationBuilder.DropTable(name: "CreditCards");
        }
    }
}
