using System;

namespace ProjetoTerapia.Models
{
    public class VendaVendedor
    {
        public int Id { get; set; }

        public int VendedorId { get; set; }

        public Vendedor? Vendedor { get; set; }

        public int? ClinicaId { get; set; }

        public Clinica? Clinica { get; set; }

        public string CodigoIndicacao { get; set; } = "";

        public string NomeClinica { get; set; } = "";

        public string EmailClinica { get; set; } = "";

        public decimal ValorVenda { get; set; }

        public decimal PercentualComissao { get; set; }

        public decimal ValorComissao { get; set; }

        public string Status { get; set; } = "Pendente";

        public bool VendaConfirmada { get; set; } = false;

        public bool ComissaoPaga { get; set; } = false;

        public DateTime DataCadastro { get; set; } = DateTime.Now;

        public DateTime? DataConfirmacaoVenda { get; set; }

        public DateTime? DataPagamentoComissao { get; set; }

        public string? ComprovanteNotaFiscal { get; set; }

        public string? ObservacaoAdmin { get; set; }
    }
}