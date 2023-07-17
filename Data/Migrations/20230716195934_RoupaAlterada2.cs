using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoraWebsite.Data.Migrations
{
    public partial class RoupaAlterada2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Price",
                table: "Roupa");

            migrationBuilder.DropColumn(
                name: "Stock",
                table: "Roupa");

            migrationBuilder.DropColumn(
                name: "Tamanho",
                table: "Roupa");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "Price",
                table: "Roupa",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<int>(
                name: "Stock",
                table: "Roupa",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Tamanho",
                table: "Roupa",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
