using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoTerapia.Migrations
{
    /// <inheritdoc />
    public partial class SeguncaAdmin2FA : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BloqueadoAte",
                table: "AdminUsuarios",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Codigo2FAExpiraEm",
                table: "AdminUsuarios",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Codigo2FAHash",
                table: "AdminUsuarios",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TentativasLoginFalhas",
                table: "AdminUsuarios",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimoCodigo2FAEnviadoEm",
                table: "AdminUsuarios",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimoLogin",
                table: "AdminUsuarios",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BloqueadoAte",
                table: "AdminUsuarios");

            migrationBuilder.DropColumn(
                name: "Codigo2FAExpiraEm",
                table: "AdminUsuarios");

            migrationBuilder.DropColumn(
                name: "Codigo2FAHash",
                table: "AdminUsuarios");

            migrationBuilder.DropColumn(
                name: "TentativasLoginFalhas",
                table: "AdminUsuarios");

            migrationBuilder.DropColumn(
                name: "UltimoCodigo2FAEnviadoEm",
                table: "AdminUsuarios");

            migrationBuilder.DropColumn(
                name: "UltimoLogin",
                table: "AdminUsuarios");
        }
    }
}
