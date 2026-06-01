using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations.Schema;


namespace ProjetoTerapia.Models
{
    public class Clinica
    {
        public int Id { get; set; }

        public string Nome { get; set; } = "";

        public string Descricao { get; set; } = "";

        public decimal Valor { get; set; }

        public string Endereco { get; set; } = "";

        public string Telefone { get; set; } = "";

        public bool Aprovado { get; set; } = false;

        public bool Pago { get; set; } = false;

        public string Email { get; set; } = ""; 

        public string Senha { get; set; } = "";

        [NotMapped]
        public string ConfirmarSenha { get; set; } = "";

        public string Especialidades { get; set; } = "";

        public string Documento { get; set; } = ""; // CRP, CRM, etc

        public string CEP { get; set; } = "";

        public bool AtendimentoPresencial { get; set; } = false;

        public bool AtendimentoOnline { get; set; } = false;

        public string Cidade { get; set; } = "";

        public string Instagram { get; set; } = "";

        public string Site { get; set; } = "";

        public string CPF { get; set; } = ""; // para pessoa física

        // data em que a clínica foi aprovada
        public DateTime? DataAprovacao { get; set; }

        // data em que o pagamento foi confirmado
        public DateTime? DataPagamento { get; set; }

        // vencimento da assinatura anual
        public DateTime? DataVencimento { get; set; }

        public bool PerfilCompleto { get; set; } = false;

        public int Visualizacoes { get; set; }

        public int CliquesWhatsapp { get; set; }

        public string FotoPerfil { get; set; } = "";
    }
}