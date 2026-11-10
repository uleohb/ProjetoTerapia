using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ProjetoTerapia.Models
{
    public class Clinica
    {
        public int Id { get; set; }

        public string Nome { get; set; } = "";

        [StringLength(1000)]
        public string Descricao { get; set; } = "";

        public decimal Valor { get; set; }

        public string Endereco { get; set; } = "";

        public string Telefone { get; set; } = "";

        // =====================================================
        // STATUS DA CONTA
        // =====================================================

        public bool Aprovado { get; set; } = false;

        public bool ClinicaAlteracaoPendente { get; set; } = false;

        public bool Pago { get; set; } = false;


        // =====================================================
        // VISIBILIDADE PÚBLICA
        // =====================================================

        /*
         * Define se o profissional deve aparecer
         * na lista pública de profissionais.
         *
         * true  = aparece na vitrine pública
         * false = continua com acesso ao sistema,
         *         mas fica invisível para pacientes
         *
         * Esse campo NÃO interfere em:
         * login, painel, agenda, pacientes,
         * pagamento ou demais recursos internos.
         */
        public bool ExibirPublicamente { get; set; } = true;


        // =====================================================
        // LOGIN / CONTA
        // =====================================================

        public string Email { get; set; } = "";

        public string SenhaHash { get; set; } = "";

        [NotMapped]
        public string Senha { get; set; } = "";

        [NotMapped]
        public string ConfirmarSenha { get; set; } = "";


        // =====================================================
        // DADOS PROFISSIONAIS
        // =====================================================

        public string Especialidades { get; set; } = "";

        public string Documento { get; set; } = ""; // CRP, CRM, etc.

        public string CEP { get; set; } = "";

        public bool AtendimentoPresencial { get; set; } = false;

        public bool AtendimentoOnline { get; set; } = false;

        public string Cidade { get; set; } = "";

        public string? Instagram { get; set; }

        public string? Site { get; set; }

        public string CPF { get; set; } = ""; // para pessoa física


        // =====================================================
        // APROVAÇÃO
        // =====================================================

        // Data em que a clínica/profissional foi aprovado.
        public DateTime? DataAprovacao { get; set; }


        // =====================================================
        // PLANO
        // =====================================================

        public string? NomePlano { get; set; }

        public decimal? ValorPlano { get; set; }

        // Data em que o pagamento foi confirmado.
        public DateTime? DataPagamento { get; set; }

        // Vencimento da assinatura anual.
        public DateTime? DataVencimento { get; set; }


        // =====================================================
        // PERFIL
        // =====================================================

        public bool PerfilCompleto { get; set; } = false;

        public int Visualizacoes { get; set; }

        public int CliquesWhatsapp { get; set; }

        public string? FotoPerfil { get; set; }


        // =====================================================
        // VENDEDOR / INDICAÇÃO
        // =====================================================

        public int? VendedorId { get; set; }

        public Vendedor? Vendedor { get; set; }

        public string? CodigoVendedorIndicacao { get; set; }


        // =====================================================
        // MERCADO PAGO
        // =====================================================

        public string? MercadoPagoPlanoPaymentId { get; set; }

        public string? MercadoPagoPlanoStatus { get; set; }
    }
}