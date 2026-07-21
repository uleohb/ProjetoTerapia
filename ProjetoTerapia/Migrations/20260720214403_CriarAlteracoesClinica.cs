using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoTerapia.Migrations
{
    /// <inheritdoc />
    public partial class CriarAlteracoesClinica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlteracoesClinicas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClinicaId = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Telefone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cidade = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Endereco = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Especialidades = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AtendimentoOnline = table.Column<bool>(type: "bit", nullable: false),
                    AtendimentoPresencial = table.Column<bool>(type: "bit", nullable: false),
                    Instagram = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Site = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FotoPerfil = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MotivoRecusa = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataSolicitacao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataAnalise = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NomeAdminAnalise = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlteracoesClinicas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlteracoesClinicas_Clinicas_ClinicaId",
                        column: x => x.ClinicaId,
                        principalTable: "Clinicas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlteracoesClinicas_ClinicaId",
                table: "AlteracoesClinicas",
                column: "ClinicaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlteracoesClinicas");
        }
    }
}
