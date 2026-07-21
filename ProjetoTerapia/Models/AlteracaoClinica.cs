using System;

namespace ProjetoTerapia.Models
{
    public class AlteracaoClinica
    {
        public int Id { get; set; }

        public int ClinicaId { get; set; }

        public Clinica? Clinica { get; set; }

        public string Nome { get; set; } = "";

        public string Email { get; set; } = "";

        public string Telefone { get; set; } = "";

        public string CEP { get; set; } = "";

        public string Cidade { get; set; } = "";

        public string Endereco { get; set; } = "";

        public string Descricao { get; set; } = "";

        public string Especialidades { get; set; } = "";

        public string Documento { get; set; } = "";

        public string CPF { get; set; } = "";

        public decimal Valor { get; set; }

        public bool AtendimentoOnline { get; set; }

        public bool AtendimentoPresencial { get; set; }

        public string Instagram { get; set; } = "";

        public string Site { get; set; } = "";

        public string? FotoPerfil { get; set; }

        public string Status { get; set; } = "Pendente";

        public string? MotivoRecusa { get; set; }

        public DateTime DataSolicitacao { get; set; } = DateTime.Now;

        public DateTime? DataAnalise { get; set; }

        public string? NomeAdminAnalise { get; set; }
    }
}