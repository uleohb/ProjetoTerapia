using System;

namespace ProjetoTerapia.Models
{
    public class DivulgacaoRegional
    {
        public int Id { get; set; }

        public int ClinicaId { get; set; }

        public Clinica? Clinica { get; set; }

        public string NomePlano { get; set; } = "";

        public int QuantidadeCidades { get; set; }

        public decimal Valor { get; set; }

        public string CidadesSelecionadas { get; set; } = "";

        public bool Pago { get; set; } = false;

        public bool Aprovado { get; set; } = false;

        public bool Ativo { get; set; } = false;

        public DateTime DataSolicitacao { get; set; } = DateTime.Now;

        public DateTime? DataPagamento { get; set; }

        public DateTime? DataAprovacao { get; set; }

        public DateTime? DataInicio { get; set; }

        public DateTime? DataFim { get; set; }

        public string Status { get; set; } = "Pendente";
    }
}