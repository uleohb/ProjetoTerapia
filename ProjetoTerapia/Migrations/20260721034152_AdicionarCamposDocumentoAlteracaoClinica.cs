using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoTerapia.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarCamposDocumentoAlteracaoClinica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CEP",
                table: "AlteracoesClinicas",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CPF",
                table: "AlteracoesClinicas",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Documento",
                table: "AlteracoesClinicas",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CEP",
                table: "AlteracoesClinicas");

            migrationBuilder.DropColumn(
                name: "CPF",
                table: "AlteracoesClinicas");

            migrationBuilder.DropColumn(
                name: "Documento",
                table: "AlteracoesClinicas");
        }
    }
}
