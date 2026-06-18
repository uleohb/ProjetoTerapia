using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoTerapia.Migrations
{
    /// <inheritdoc />
    public partial class AdicionandoHumor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Observacoes",
                table: "Pacientes");

            migrationBuilder.RenameColumn(
                name: "Telefone",
                table: "Pacientes",
                newName: "SenhaHash");

            migrationBuilder.AlterColumn<int>(
                name: "ClinicaId",
                table: "Pacientes",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<DateTime>(
                name: "DataCadastro",
                table: "Pacientes",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "RegistrosHumor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PacienteId = table.Column<int>(type: "int", nullable: false),
                    Nota = table.Column<int>(type: "int", nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Data = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosHumor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrosHumor_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pacientes_ClinicaId",
                table: "Pacientes",
                column: "ClinicaId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosHumor_PacienteId",
                table: "RegistrosHumor",
                column: "PacienteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Pacientes_Clinicas_ClinicaId",
                table: "Pacientes",
                column: "ClinicaId",
                principalTable: "Clinicas",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pacientes_Clinicas_ClinicaId",
                table: "Pacientes");

            migrationBuilder.DropTable(
                name: "RegistrosHumor");

            migrationBuilder.DropIndex(
                name: "IX_Pacientes_ClinicaId",
                table: "Pacientes");

            migrationBuilder.DropColumn(
                name: "DataCadastro",
                table: "Pacientes");

            migrationBuilder.RenameColumn(
                name: "SenhaHash",
                table: "Pacientes",
                newName: "Telefone");

            migrationBuilder.AlterColumn<int>(
                name: "ClinicaId",
                table: "Pacientes",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Observacoes",
                table: "Pacientes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
