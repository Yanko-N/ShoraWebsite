using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoraWebsite.Data.Migrations
{
    public partial class smallErrorTwoVistasForEachRole : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsVista",
                table: "Messages",
                newName: "IsVistaCliente");

            migrationBuilder.AddColumn<bool>(
                name: "IsVistaAdmin",
                table: "Messages",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsVistaAdmin",
                table: "Messages");

            migrationBuilder.RenameColumn(
                name: "IsVistaCliente",
                table: "Messages",
                newName: "IsVista");
        }
    }
}
