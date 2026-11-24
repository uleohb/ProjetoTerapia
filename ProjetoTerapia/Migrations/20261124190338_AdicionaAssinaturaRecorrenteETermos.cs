using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoTerapia.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaAssinaturaRecorrenteETermos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AceitesTermosProfissionais",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClinicaId = table.Column<int>(type: "int", nullable: true),
                    VersaoTermos = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    HashTermos = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    AceitouTermos = table.Column<bool>(type: "bit", nullable: false),
                    VersaoPoliticaPrivacidade = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    HashPoliticaPrivacidade = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    AceitouPoliticaPrivacidade = table.Column<bool>(type: "bit", nullable: false),
                    AceitouCobrancaRecorrente = table.Column<bool>(type: "bit", nullable: false),
                    ValorInformado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiasTesteGratisInformados = table.Column<int>(type: "int", nullable: false),
                    PeriodicidadeInformada = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DataAceiteUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAceite = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OrigemAceite = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NomeProfissionalSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EmailSnapshot = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    DocumentoSnapshot = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CpfSnapshot = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AceitesTermosProfissionais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AceitesTermosProfissionais_Clinicas_ClinicaId",
                        column: x => x.ClinicaId,
                        principalTable: "Clinicas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AssinaturasProfissionais",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClinicaId = table.Column<int>(type: "int", nullable: true),
                    ExternalReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NomePlano = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    MercadoPagoPreapprovalId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    MercadoPagoPlanId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    LinkAutorizacao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Ativa = table.Column<bool>(type: "bit", nullable: false),
                    RenovacaoAutomatica = table.Column<bool>(type: "bit", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Moeda = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Frequencia = table.Column<int>(type: "int", nullable: false),
                    TipoFrequencia = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DiasTesteGratis = table.Column<int>(type: "int", nullable: false),
                    InicioTesteGratisUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FimTesteGratisUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProximaCobrancaUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UltimoPagamentoId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    StatusUltimoPagamento = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DataUltimoPagamentoUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DataCriacaoUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataAutorizacaoUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DataAtualizacaoUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DataCancelamentoUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Observacao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssinaturasProfissionais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssinaturasProfissionais_Clinicas_ClinicaId",
                        column: x => x.ClinicaId,
                        principalTable: "Clinicas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AceitesTermosProfissionais_ClinicaId",
                table: "AceitesTermosProfissionais",
                column: "ClinicaId");

            migrationBuilder.CreateIndex(
                name: "IX_AceitesTermosProfissionais_ClinicaId_VersaoTermos",
                table: "AceitesTermosProfissionais",
                columns: new[] { "ClinicaId", "VersaoTermos" });

            migrationBuilder.CreateIndex(
                name: "IX_AssinaturasProfissionais_ClinicaId",
                table: "AssinaturasProfissionais",
                column: "ClinicaId");

            migrationBuilder.CreateIndex(
                name: "IX_AssinaturasProfissionais_ExternalReference",
                table: "AssinaturasProfissionais",
                column: "ExternalReference");

            migrationBuilder.CreateIndex(
                name: "IX_AssinaturasProfissionais_MercadoPagoPreapprovalId",
                table: "AssinaturasProfissionais",
                column: "MercadoPagoPreapprovalId",
                unique: true,
                filter: "[MercadoPagoPreapprovalId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AceitesTermosProfissionais");

            migrationBuilder.DropTable(
                name: "AssinaturasProfissionais");
        }
    }
}
