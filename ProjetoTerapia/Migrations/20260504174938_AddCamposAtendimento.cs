using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoTerapia.Migrations
{
    /// <inheritdoc />
    public partial class AddCamposAtendimento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AtendimentoOnline",
                table: "Clinicas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AtendimentoPresencial",
                table: "Clinicas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CEP",
                table: "Clinicas",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Cidade",
                table: "Clinicas",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Documento",
                table: "Clinicas",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AtendimentoOnline",
                table: "Clinicas");

            migrationBuilder.DropColumn(
                name: "AtendimentoPresencial",
                table: "Clinicas");

            migrationBuilder.DropColumn(
                name: "CEP",
                table: "Clinicas");

            migrationBuilder.DropColumn(
                name: "Cidade",
                table: "Clinicas");

            migrationBuilder.DropColumn(
                name: "Documento",
                table: "Clinicas");
        }
    }
}
