using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoTerapia.Migrations
{
    /// <inheritdoc />
    public partial class CriandoAdministradoresELogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminUsuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SenhaHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Perfil = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminUsuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdminLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdminUsuarioId = table.Column<int>(type: "int", nullable: false),
                    NomeAdmin = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PerfilAdmin = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Acao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataAcao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminLogs_AdminUsuarios_AdminUsuarioId",
                        column: x => x.AdminUsuarioId,
                        principalTable: "AdminUsuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminLogs_AdminUsuarioId",
                table: "AdminLogs",
                column: "AdminUsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminLogs");

            migrationBuilder.DropTable(
                name: "AdminUsuarios");
        }
    }
}
