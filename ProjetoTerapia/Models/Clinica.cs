using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        /*
         * Mantemos Pago por compatibilidade com o sistema atual.
         *
         * Depois que a assinatura recorrente estiver totalmente
         * implementada, esse campo continuará servindo como um
         * indicador rápido de acesso ao plano.
         *
         * A fonte principal do status financeiro passará a ser
         * AssinaturaProfissional.
         */
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

        public string Documento { get; set; } = "";

        public string CEP { get; set; } = "";

        public bool AtendimentoPresencial { get; set; } = false;

        public bool AtendimentoOnline { get; set; } = false;

        public string Cidade { get; set; } = "";

        public string? Instagram { get; set; }

        public string? Site { get; set; }

        public string CPF { get; set; } = "";


        // =====================================================
        // APROVAÇÃO
        // =====================================================

        public DateTime? DataAprovacao { get; set; }


        // =====================================================
        // PLANO - CAMPOS LEGADOS / COMPATIBILIDADE
        // =====================================================

        /*
         * Esses campos continuam existindo porque várias
         * páginas atuais do AlinhaVIDA ainda dependem deles.
         *
         * A nova assinatura recorrente ficará registrada
         * também na tabela AssinaturasProfissionais.
         */

        public string? NomePlano { get; set; }

        public decimal? ValorPlano { get; set; }

        public DateTime? DataPagamento { get; set; }

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
        // MERCADO PAGO - CAMPOS LEGADOS
        // =====================================================

        /*
         * Mantidos temporariamente para não quebrar
         * o fluxo de pagamento atual.
         *
         * Assinaturas recorrentes passarão a utilizar
         * AssinaturaProfissional.
         */

        public string? MercadoPagoPlanoPaymentId { get; set; }

        public string? MercadoPagoPlanoStatus { get; set; }


        // =====================================================
        // IDENTIFICAÇÃO PROFISSIONAL
        // =====================================================

        [StringLength(50)]
        public string? Profissao { get; set; }


        [StringLength(20)]
        public string? ConselhoProfissional { get; set; }


        [StringLength(50)]
        public string? NumeroRegistroProfissional { get; set; }


        [StringLength(2)]
        public string? UfRegistroProfissional { get; set; }


        [StringLength(50)]
        public string? RQE { get; set; }


        // =====================================================
        // VALIDAÇÃO PROFISSIONAL
        // =====================================================

        public bool RegistroProfissionalValidado { get; set; } = false;


        [StringLength(30)]
        public string StatusValidacaoProfissional { get; set; } = "Pendente";


        public DateTime? DataValidacaoProfissional { get; set; }


        [StringLength(200)]
        public string? NomeRetornadoConselho { get; set; }


        [StringLength(50)]
        public string? OrigemValidacaoProfissional { get; set; }


        // =====================================================
        // ASSINATURAS RECORRENTES
        // =====================================================

        public ICollection<AssinaturaProfissional>
            AssinaturasProfissionais
        { get; set; }
            = new List<AssinaturaProfissional>();


        // =====================================================
        // ACEITES DOS TERMOS
        // =====================================================

        public ICollection<AceiteTermoProfissional>
            AceitesTermosProfissionais
        { get; set; }
            = new List<AceiteTermoProfissional>();
    }
}