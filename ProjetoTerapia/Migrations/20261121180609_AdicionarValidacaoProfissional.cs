using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoTerapia.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarValidacaoProfissional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConselhoProfissional",
                table: "Clinicas",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataValidacaoProfissional",
                table: "Clinicas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NomeRetornadoConselho",
                table: "Clinicas",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroRegistroProfissional",
                table: "Clinicas",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrigemValidacaoProfissional",
                table: "Clinicas",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Profissao",
                table: "Clinicas",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RQE",
                table: "Clinicas",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RegistroProfissionalValidado",
                table: "Clinicas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "StatusValidacaoProfissional",
                table: "Clinicas",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UfRegistroProfissional",
                table: "Clinicas",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConselhoProfissional",
                table: "Clinicas");

            migrationBuilder.DropColumn(
                name: "DataValidacaoProfissional",
                table: "Clinicas");

            migrationBuilder.DropColumn(
                name: "NomeRetornadoConselho",
                table: "Clinicas");

            migrationBuilder.DropColumn(
                name: "NumeroRegistroProfissional",
                table: "Clinicas");

            migrationBuilder.DropColumn(
                name: "OrigemValidacaoProfissional",
                table: "Clinicas");

            migrationBuilder.DropColumn(
                name: "Profissao",
                table: "Clinicas");

            migrationBuilder.DropColumn(
                name: "RQE",
                table: "Clinicas");

            migrationBuilder.DropColumn(
                name: "RegistroProfissionalValidado",
                table: "Clinicas");

            migrationBuilder.DropColumn(
                name: "StatusValidacaoProfissional",
                table: "Clinicas");

            migrationBuilder.DropColumn(
                name: "UfRegistroProfissional",
                table: "Clinicas");
        }
    }
}
