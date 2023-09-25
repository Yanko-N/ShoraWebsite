using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoraWebsite.Data.Migrations
{
    public partial class Reservas : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Perfils",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perfils", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Perfils_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Reserva",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoupaId = table.Column<int>(type: "int", nullable: false),
                    PerfilId = table.Column<int>(type: "int", nullable: false),
                    Quantidade = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reserva", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reserva_Perfils_PerfilId",
                        column: x => x.PerfilId,
                        principalTable: "Perfils",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reserva_Roupa_RoupaId",
                        column: x => x.RoupaId,
                        principalTable: "Roupa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Perfils_UserId",
                table: "Perfils",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reserva_PerfilId",
                table: "Reserva",
                column: "PerfilId");

            migrationBuilder.CreateIndex(
                name: "IX_Reserva_RoupaId",
                table: "Reserva",
                column: "RoupaId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reserva");

            migrationBuilder.DropTable(
                name: "Perfils");
        }
    }
}
