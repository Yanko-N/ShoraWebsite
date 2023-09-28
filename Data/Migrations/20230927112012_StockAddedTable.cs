using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoraWebsite.Data.Migrations
{
    public partial class StockAddedTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Stock",
                table: "Roupa");

            migrationBuilder.CreateTable(
                name: "StockMaterial",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoupaId = table.Column<int>(type: "int", nullable: false),
                    Tamanho = table.Column<int>(type: "int", nullable: false),
                    Quantidade = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMaterial", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMaterial_Roupa_RoupaId",
                        column: x => x.RoupaId,
                        principalTable: "Roupa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockMaterial_RoupaId",
                table: "StockMaterial",
                column: "RoupaId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockMaterial");

            migrationBuilder.AddColumn<int>(
                name: "Stock",
                table: "Roupa",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
