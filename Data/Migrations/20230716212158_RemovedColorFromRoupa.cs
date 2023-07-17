using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoraWebsite.Data.Migrations
{
    public partial class RemovedColorFromRoupa : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cor",
                table: "Roupa");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Cor",
                table: "Roupa",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
