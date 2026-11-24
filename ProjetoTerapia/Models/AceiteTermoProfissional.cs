using System.ComponentModel.DataAnnotations;

namespace ProjetoTerapia.Models
{
    public class AceiteTermoProfissional
    {
        public int Id { get; set; }


        // =====================================================
        // PROFISSIONAL
        // =====================================================

        /*
         * Nullable para podermos preservar o registro
         * histórico mesmo caso a conta seja excluída.
         */
        public int? ClinicaId { get; set; }

        public Clinica? Clinica { get; set; }


        // =====================================================
        // TERMOS DE USO
        // =====================================================

        /*
         * Exemplo:
         *
         * 1.0
         * 1.1
         * 2.0
         */
        [Required]
        [StringLength(30)]
        public string VersaoTermos { get; set; } = "1.0";


        /*
         * SHA-256 do conteúdo exato dos termos.
         *
         * Isso permite provar posteriormente qual texto
         * estava vigente no momento do aceite.
         */
        [Required]
        [StringLength(128)]
        public string HashTermos { get; set; } = "";


        public bool AceitouTermos { get; set; } = false;


        // =====================================================
        // POLÍTICA DE PRIVACIDADE
        // =====================================================

        [Required]
        [StringLength(30)]
        public string VersaoPoliticaPrivacidade { get; set; } = "1.0";


        [StringLength(128)]
        public string? HashPoliticaPrivacidade { get; set; }


        public bool AceitouPoliticaPrivacidade { get; set; } = false;


        // =====================================================
        // COBRANÇA RECORRENTE
        // =====================================================

        /*
         * Registro separado do aceite financeiro.
         *
         * O profissional declara estar ciente de:
         *
         * - 7 dias gratuitos;
         * - cobrança de R$ 450,00 após o trial;
         * - renovação automática anual;
         * - continuidade até cancelamento.
         */
        public bool AceitouCobrancaRecorrente { get; set; } = false;


        public decimal ValorInformado { get; set; } = 450.00m;


        public int DiasTesteGratisInformados { get; set; } = 7;


        [StringLength(30)]
        public string PeriodicidadeInformada { get; set; } = "Anual";


        // =====================================================
        // DATA DO ACEITE
        // =====================================================

        public DateTime DataAceiteUtc { get; set; }
            = DateTime.UtcNow;


        // =====================================================
        // RASTREABILIDADE
        // =====================================================

        /*
         * IP usado no momento do aceite.
         */
        [StringLength(100)]
        public string? IpAceite { get; set; }


        /*
         * Navegador/dispositivo usado no aceite.
         */
        [StringLength(1000)]
        public string? UserAgent { get; set; }


        /*
         * Exemplo:
         *
         * PagamentoClinica
         * LandingPage
         * CadastroProfissional
         */
        [StringLength(100)]
        public string OrigemAceite { get; set; }
            = "PagamentoClinica";


        // =====================================================
        // SNAPSHOT DO PROFISSIONAL
        // =====================================================

        /*
         * Esses dados são guardados junto ao aceite para
         * identificar quem concordou com os termos mesmo
         * que alguns dados da conta sejam alterados depois.
         */

        [StringLength(200)]
        public string? NomeProfissionalSnapshot { get; set; }


        [StringLength(320)]
        public string? EmailSnapshot { get; set; }


        [StringLength(50)]
        public string? DocumentoSnapshot { get; set; }


        [StringLength(30)]
        public string? CpfSnapshot { get; set; }
    }
}