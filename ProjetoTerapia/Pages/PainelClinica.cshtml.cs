using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;
using System.Globalization;

namespace ProjetoTerapia.Pages
{
    public class PainelClinicaModel : PageModel
    {
        private readonly AppDbContext _context;

        public PainelClinicaModel(AppDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // DADOS PRINCIPAIS
        // =====================================================

        public Clinica? Clinica { get; set; }

        public DivulgacaoRegional? DivulgacaoRegionalAtiva { get; set; }

        public int Visualizacoes { get; set; }

        public int CliquesWhatsapp { get; set; }

        public double TaxaConversao { get; set; }

        public string StatusPlano { get; set; } = "";

        public string NomePlanoPrincipalFormatado { get; set; } =
            "Nenhum plano ativo";

        public string ValorPlanoFormatado { get; set; } =
            "Não definido";

        public string DataVencimentoFormatada { get; set; } =
            "Não definido";

        public int? DiasRestantes { get; set; }

        public bool PlanoVencido { get; set; }

        public bool PerfilPublicoAtivo { get; set; }

        // =====================================================
        // DIVULGAÇÃO REGIONAL
        // =====================================================

        public string NomePlanoRegionalFormatado { get; set; } =
            "Nenhuma divulgação ativa";

        public string ValorPlanoRegionalFormatado { get; set; } =
            "R$ 0,00";

        public string CidadesPlanoRegional { get; set; } = "";

        public string DataFimPlanoRegionalFormatada { get; set; } =
            "Não definido";

        public int? DiasRestantesPlanoRegional { get; set; }

        // =====================================================
        // GET
        // =====================================================

        public IActionResult OnGet()
        {
            var id =
                HttpContext.Session.GetString(
                    "ClinicaLogada"
                );

            if (string.IsNullOrWhiteSpace(id) ||
                !int.TryParse(id, out int clinicaId))
            {
                return RedirectToPage(
                    "/LoginClinica"
                );
            }

            Clinica =
                _context.Clinicas
                    .FirstOrDefault(
                        c => c.Id == clinicaId
                    );

            if (Clinica == null)
            {
                return RedirectToPage(
                    "/LoginClinica"
                );
            }

            AtualizarDivulgacoesRegionaisVencidas(
                Clinica.Id
            );

            CarregarDivulgacaoRegionalAtiva(
                Clinica.Id
            );

            CarregarResumo();

            return Page();
        }

        // =====================================================
        // RESUMO DO PAINEL
        // =====================================================

        private void CarregarResumo()
        {
            if (Clinica == null)
            {
                return;
            }

            Visualizacoes =
                Clinica.Visualizacoes;

            CliquesWhatsapp =
                Clinica.CliquesWhatsapp;

            TaxaConversao =
                Visualizacoes > 0
                    ? Math.Round(
                        (CliquesWhatsapp * 100.0) /
                        Visualizacoes,
                        1
                    )
                    : 0;

            DiasRestantes = null;
            PlanoVencido = false;

            // =================================================
            // VENCIMENTO
            // =================================================

            if (Clinica.DataVencimento.HasValue)
            {
                DataVencimentoFormatada =
                    Clinica.DataVencimento
                        .Value
                        .ToString(
                            "dd/MM/yyyy"
                        );

                DiasRestantes =
                    (int)Math.Ceiling(
                        (
                            Clinica.DataVencimento
                                .Value
                                .Date
                            -
                            DateTime.Today
                        )
                        .TotalDays
                    );

                PlanoVencido =
                    DiasRestantes < 0;
            }

            // =================================================
            // PERFIL PÚBLICO
            // =================================================
            //
            // NÃO depende mais de aprovação do administrador.
            //
            // Para aparecer publicamente:
            //
            // - plano ativo
            // - plano não vencido
            // - perfil completo
            // - ExibirPublicamente = true
            //

            PerfilPublicoAtivo =
                Clinica.Pago &&
                !PlanoVencido &&
                Clinica.PerfilCompleto &&
                Clinica.ExibirPublicamente;

            // =================================================
            // PLANO
            // =================================================

            if (Clinica.Pago)
            {
                NomePlanoPrincipalFormatado =
                    string.IsNullOrWhiteSpace(
                        Clinica.NomePlano
                    )
                        ? "Plano Profissional"
                        : Clinica.NomePlano;

                var valorPlano =
                    Clinica.ValorPlano ??
                    450m;

                ValorPlanoFormatado =
                    valorPlano.ToString(
                        "C",
                        new CultureInfo(
                            "pt-BR"
                        )
                    );
            }
            else
            {
                NomePlanoPrincipalFormatado =
                    "Nenhum plano ativo";

                ValorPlanoFormatado =
                    "R$ 450,00";
            }

            // =================================================
            // STATUS
            // =================================================

            if (!Clinica.Pago)
            {
                StatusPlano =
                    "Aguardando ativação";
            }
            else if (PlanoVencido)
            {
                StatusPlano =
                    "Plano vencido";
            }
            else
            {
                StatusPlano =
                    "Plano ativo";
            }
        }

        // =====================================================
        // CARREGAR DIVULGAÇÃO REGIONAL
        // =====================================================

        private void CarregarDivulgacaoRegionalAtiva(
            int clinicaId)
        {
            DivulgacaoRegionalAtiva =
                _context.DivulgacoesRegionais
                    .Where(
                        d =>
                            d.ClinicaId == clinicaId &&
                            d.Pago &&

                            /*
                             * Este Aprovado pertence SOMENTE
                             * à Divulgação Regional.
                             *
                             * Não tem relação com aprovação
                             * do profissional.
                             */
                            d.Aprovado &&

                            d.Ativo &&
                            (
                                !d.DataFim.HasValue ||
                                d.DataFim.Value.Date >=
                                DateTime.Today
                            )
                    )
                    .OrderByDescending(
                        d =>
                            d.DataInicio ??
                            d.DataPagamento ??
                            d.DataSolicitacao
                    )
                    .FirstOrDefault();

            if (DivulgacaoRegionalAtiva == null)
            {
                return;
            }

            NomePlanoRegionalFormatado =
                DivulgacaoRegionalAtiva.NomePlano;

            ValorPlanoRegionalFormatado =
                DivulgacaoRegionalAtiva
                    .Valor
                    .ToString(
                        "C",
                        new CultureInfo(
                            "pt-BR"
                        )
                    );

            CidadesPlanoRegional =
                DivulgacaoRegionalAtiva
                    .CidadesSelecionadas;

            if (DivulgacaoRegionalAtiva
                .DataFim
                .HasValue)
            {
                DataFimPlanoRegionalFormatada =
                    DivulgacaoRegionalAtiva
                        .DataFim
                        .Value
                        .ToString(
                            "dd/MM/yyyy"
                        );

                DiasRestantesPlanoRegional =
                    (int)Math.Ceiling(
                        (
                            DivulgacaoRegionalAtiva
                                .DataFim
                                .Value
                                .Date
                            -
                            DateTime.Today
                        )
                        .TotalDays
                    );
            }
        }

        // =====================================================
        // DESATIVAR DIVULGAÇÕES VENCIDAS
        // =====================================================

        private void AtualizarDivulgacoesRegionaisVencidas(
            int clinicaId)
        {
            var divulgacoesVencidas =
                _context.DivulgacoesRegionais
                    .Where(
                        d =>
                            d.ClinicaId == clinicaId &&
                            d.Ativo &&
                            d.DataFim.HasValue &&
                            d.DataFim.Value.Date <
                            DateTime.Today
                    )
                    .ToList();

            if (!divulgacoesVencidas.Any())
            {
                return;
            }

            foreach (var divulgacao
                in divulgacoesVencidas)
            {
                divulgacao.Ativo =
                    false;

                divulgacao.Status =
                    "Expirado";
            }

            _context.SaveChanges();
        }
    }
}