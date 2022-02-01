using Microsoft.EntityFrameworkCore.Migrations;

namespace BackArt.Migrations.ComplaintSeriesDb
{
    public partial class isRoot : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "isRoot",
                table: "CodeLinkSnapshot",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isRoot",
                table: "CodeLinkSnapshot");
        }
    }
}
