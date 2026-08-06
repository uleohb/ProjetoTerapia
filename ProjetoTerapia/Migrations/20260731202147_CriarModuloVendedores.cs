using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoTerapia.Migrations
{
    /// <inheritdoc />
    public partial class CriarModuloVendedores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Vendedores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Telefone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CodigoIndicacao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SenhaHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    PercentualComissao = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ChavePix = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataCadastro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vendedores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VendasVendedores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VendedorId = table.Column<int>(type: "int", nullable: false),
                    ClinicaId = table.Column<int>(type: "int", nullable: true),
                    CodigoIndicacao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NomeClinica = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmailClinica = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValorVenda = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PercentualComissao = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorComissao = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VendaConfirmada = table.Column<bool>(type: "bit", nullable: false),
                    ComissaoPaga = table.Column<bool>(type: "bit", nullable: false),
                    DataCadastro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataConfirmacaoVenda = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DataPagamentoComissao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ComprovanteNotaFiscal = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ObservacaoAdmin = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendasVendedores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendasVendedores_Clinicas_ClinicaId",
                        column: x => x.ClinicaId,
                        principalTable: "Clinicas",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VendasVendedores_Vendedores_VendedorId",
                        column: x => x.VendedorId,
                        principalTable: "Vendedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VendasVendedores_ClinicaId",
                table: "VendasVendedores",
                column: "ClinicaId");

            migrationBuilder.CreateIndex(
                name: "IX_VendasVendedores_VendedorId",
                table: "VendasVendedores",
                column: "VendedorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VendasVendedores");

            migrationBuilder.DropTable(
                name: "Vendedores");
        }
    }
}
