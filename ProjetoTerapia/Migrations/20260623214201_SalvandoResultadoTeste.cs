using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoTerapia.Migrations
{
    /// <inheritdoc />
    public partial class SalvandoResultadoTeste : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ResultadosTestePacientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PacienteId = table.Column<int>(type: "int", nullable: false),
                    PontuacaoAnsiedade = table.Column<int>(type: "int", nullable: false),
                    PontuacaoDepressao = table.Column<int>(type: "int", nullable: false),
                    PercentualAnsiedade = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PercentualDepressao = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ResultadoFinal = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nivel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Mensagem = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataResultado = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResultadosTestePacientes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResultadosTestePacientes_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResultadosTestePacientes_PacienteId",
                table: "ResultadosTestePacientes",
                column: "PacienteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResultadosTestePacientes");
        }
    }
}
