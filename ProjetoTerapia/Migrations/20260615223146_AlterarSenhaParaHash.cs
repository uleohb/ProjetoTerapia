using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoTerapia.Migrations
{
    public partial class AlterarSenhaParaHash : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A coluna SenhaHash já foi criada por uma migration anterior.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nenhuma alteração necessária.
        }
    }
}