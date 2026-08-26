using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace CashFlow.Auth.Api.Migrations;

public partial class AddCashFlowGroups : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name:"CashFlowGroups",columns:table=>new{Id=table.Column<Guid>(type:"uuid",nullable:false),Name=table.Column<string>(type:"character varying(120)",maxLength:120,nullable:false),NormalizedName=table.Column<string>(type:"character varying(120)",maxLength:120,nullable:false),OwnerUserId=table.Column<string>(type:"text",nullable:false),CreatedAt=table.Column<DateTime>(type:"timestamp with time zone",nullable:false)},constraints:table=>table.PrimaryKey("PK_CashFlowGroups",x=>x.Id));
        migrationBuilder.CreateIndex(name:"IX_CashFlowGroups_NormalizedName",table:"CashFlowGroups",column:"NormalizedName",unique:true);
        migrationBuilder.CreateTable(name:"GroupMemberships",columns:table=>new{Id=table.Column<Guid>(type:"uuid",nullable:false),GroupId=table.Column<Guid>(type:"uuid",nullable:false),UserId=table.Column<string>(type:"text",nullable:false),Status=table.Column<int>(type:"integer",nullable:false),Role=table.Column<int>(type:"integer",nullable:false),CreatedAt=table.Column<DateTime>(type:"timestamp with time zone",nullable:false)},constraints:table=>{table.PrimaryKey("PK_GroupMemberships",x=>x.Id);table.ForeignKey(name:"FK_GroupMemberships_CashFlowGroups_GroupId",column:x=>x.GroupId,principalTable:"CashFlowGroups",principalColumn:"Id",onDelete:ReferentialAction.Cascade);});
        migrationBuilder.CreateIndex(name:"IX_GroupMemberships_GroupId_UserId",table:"GroupMemberships",columns:new[]{"GroupId","UserId"},unique:true);
    }
    protected override void Down(MigrationBuilder migrationBuilder){migrationBuilder.DropTable("GroupMemberships");migrationBuilder.DropTable("CashFlowGroups");}
}