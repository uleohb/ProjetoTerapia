using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoTerapia.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarMercadoPagoDivulgacaoCidades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LinkPagamento",
                table: "DivulgacoesRegionais",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MercadoPagoPaymentId",
                table: "DivulgacoesRegionais",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MercadoPagoPreferenceId",
                table: "DivulgacoesRegionais",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MercadoPagoStatus",
                table: "DivulgacoesRegionais",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LinkPagamento",
                table: "DivulgacoesRegionais");

            migrationBuilder.DropColumn(
                name: "MercadoPagoPaymentId",
                table: "DivulgacoesRegionais");

            migrationBuilder.DropColumn(
                name: "MercadoPagoPreferenceId",
                table: "DivulgacoesRegionais");

            migrationBuilder.DropColumn(
                name: "MercadoPagoStatus",
                table: "DivulgacoesRegionais");
        }
    }
}
