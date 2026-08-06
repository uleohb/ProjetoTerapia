using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoTerapia.Migrations
{
    /// <inheritdoc />
    public partial class VincularVendedorNaClinica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoVendedorIndicacao",
                table: "Clinicas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VendedorId",
                table: "Clinicas",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clinicas_VendedorId",
                table: "Clinicas",
                column: "VendedorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Clinicas_Vendedores_VendedorId",
                table: "Clinicas",
                column: "VendedorId",
                principalTable: "Vendedores",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clinicas_Vendedores_VendedorId",
                table: "Clinicas");

            migrationBuilder.DropIndex(
                name: "IX_Clinicas_VendedorId",
                table: "Clinicas");

            migrationBuilder.DropColumn(
                name: "CodigoVendedorIndicacao",
                table: "Clinicas");

            migrationBuilder.DropColumn(
                name: "VendedorId",
                table: "Clinicas");
        }
    }
}
