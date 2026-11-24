using System.ComponentModel.DataAnnotations;

namespace ProjetoTerapia.Models
{
    public class AssinaturaProfissional
    {
        public int Id { get; set; }


        // =====================================================
        // PROFISSIONAL
        // =====================================================

        /*
         * Nullable propositalmente.
         *
         * Caso futuramente uma conta seja excluída,
         * podemos manter o histórico financeiro da assinatura.
         */
        public int? ClinicaId { get; set; }

        public Clinica? Clinica { get; set; }


        // =====================================================
        // IDENTIFICAÇÃO INTERNA
        // =====================================================

        [StringLength(100)]
        public string ExternalReference { get; set; } = "";


        [StringLength(120)]
        public string NomePlano { get; set; }
            = "Plano Profissional Anual";


        // =====================================================
        // MERCADO PAGO
        // =====================================================

        /*
         * ID da assinatura individual criada no Mercado Pago.
         *
         * API:
         * /preapproval
         */
        [StringLength(150)]
        public string? MercadoPagoPreapprovalId { get; set; }


        /*
         * ID do plano recorrente cadastrado no Mercado Pago.
         *
         * API:
         * /preapproval_plan
         */
        [StringLength(150)]
        public string? MercadoPagoPlanId { get; set; }


        /*
         * Link enviado pelo Mercado Pago para autorização
         * da assinatura pelo profissional.
         */
        [StringLength(1000)]
        public string? LinkAutorizacao { get; set; }


        // =====================================================
        // STATUS
        // =====================================================

        /*
         * Exemplos esperados:
         *
         * pending
         * authorized
         * paused
         * cancelled
         */
        [StringLength(50)]
        public string Status { get; set; } = "pending";


        public bool Ativa { get; set; } = false;


        /*
         * Indica que a renovação automática permanece
         * autorizada.
         */
        public bool RenovacaoAutomatica { get; set; } = true;


        // =====================================================
        // VALOR
        // =====================================================

        public decimal Valor { get; set; } = 450.00m;


        [StringLength(10)]
        public string Moeda { get; set; } = "BRL";


        // =====================================================
        // RECORRÊNCIA
        // =====================================================

        /*
         * Para assinatura anual:
         *
         * Frequencia = 12
         * TipoFrequencia = months
         */
        public int Frequencia { get; set; } = 12;


        [StringLength(20)]
        public string TipoFrequencia { get; set; } = "months";


        // =====================================================
        // TESTE GRÁTIS
        // =====================================================

        public int DiasTesteGratis { get; set; } = 7;


        public DateTime? InicioTesteGratisUtc { get; set; }


        public DateTime? FimTesteGratisUtc { get; set; }


        // =====================================================
        // COBRANÇAS
        // =====================================================

        public DateTime? ProximaCobrancaUtc { get; set; }


        [StringLength(150)]
        public string? UltimoPagamentoId { get; set; }


        [StringLength(50)]
        public string? StatusUltimoPagamento { get; set; }


        public DateTime? DataUltimoPagamentoUtc { get; set; }


        // =====================================================
        // DATAS
        // =====================================================

        public DateTime DataCriacaoUtc { get; set; }
            = DateTime.UtcNow;


        public DateTime? DataAutorizacaoUtc { get; set; }


        public DateTime? DataAtualizacaoUtc { get; set; }


        public DateTime? DataCancelamentoUtc { get; set; }


        // =====================================================
        // OBSERVAÇÕES
        // =====================================================

        [StringLength(1000)]
        public string? Observacao { get; set; }
    }
}