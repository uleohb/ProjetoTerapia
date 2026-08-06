using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;
using System;
using System.Globalization;
using System.Linq;

namespace ProjetoTerapia.Pages
{
    public class PainelClinicaModel : PageModel
    {
        private readonly AppDbContext _context;

        public Clinica? Clinica { get; set; }

        public DivulgacaoRegional? DivulgacaoRegionalAtiva { get; set; }

        public int Visualizacoes { get; set; }

        public int CliquesWhatsapp { get; set; }

        public double TaxaConversao { get; set; }

        public string StatusPlano { get; set; } = "";

        public string NomePlanoPrincipalFormatado { get; set; } = "Nenhum plano ativo";

        public string ValorPlanoFormatado { get; set; } = "Não definido";

        public string DataVencimentoFormatada { get; set; } = "Não definido";

        public int? DiasRestantes { get; set; }

        public bool PlanoVencido { get; set; }

        public bool PerfilPublicoAtivo { get; set; }

        public string NomePlanoRegionalFormatado { get; set; } = "Nenhuma divulgação ativa";

        public string ValorPlanoRegionalFormatado { get; set; } = "R$ 0,00";

        public string CidadesPlanoRegional { get; set; } = "";

        public string DataFimPlanoRegionalFormatada { get; set; } = "Não definido";

        public int? DiasRestantesPlanoRegional { get; set; }

        public AlteracaoClinica? AlteracaoPendente { get; set; }

        public AlteracaoClinica? AlteracaoRecusada { get; set; }

        public PainelClinicaModel(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            var id = HttpContext.Session.GetString("ClinicaLogada");

            if (id == null)
                return RedirectToPage("/LoginClinica");

            Clinica = _context.Clinicas.FirstOrDefault(c => c.Id == int.Parse(id));

            if (Clinica == null)
                return RedirectToPage("/LoginClinica");

            AtualizarDivulgacoesRegionaisVencidas(Clinica.Id);

            CarregarAvisosAlteracao(Clinica.Id);

            CarregarDivulgacaoRegionalAtiva(Clinica.Id);

            CarregarResumo();

            return Page();
        }

        private void CarregarResumo()
        {
            if (Clinica == null)
                return;

            Visualizacoes = Clinica.Visualizacoes;
            CliquesWhatsapp = Clinica.CliquesWhatsapp;

            TaxaConversao = Visualizacoes > 0
                ? Math.Round((CliquesWhatsapp * 100.0) / Visualizacoes, 1)
                : 0;

            PerfilPublicoAtivo = Clinica.Aprovado && Clinica.Pago && Clinica.PerfilCompleto;

            if (Clinica.Pago)
            {
                NomePlanoPrincipalFormatado = string.IsNullOrWhiteSpace(Clinica.NomePlano)
                    ? "Plano Profissional"
                    : Clinica.NomePlano;

                var valorPlano = Clinica.ValorPlano ?? 450;

                ValorPlanoFormatado = valorPlano.ToString("C", new CultureInfo("pt-BR"));
            }

            if (Clinica.DataVencimento.HasValue)
            {
                DataVencimentoFormatada = Clinica.DataVencimento.Value.ToString("dd/MM/yyyy");

                DiasRestantes = (int)Math.Ceiling(
                    (Clinica.DataVencimento.Value.Date - DateTime.Today).TotalDays
                );

                PlanoVencido = DiasRestantes < 0;
            }

            if (!Clinica.Aprovado)
            {
                StatusPlano = "Cadastro em análise";
            }
            else if (Clinica.Aprovado && !Clinica.Pago)
            {
                StatusPlano = "Aguardando ativação";
            }
            else if (Clinica.Pago && PlanoVencido)
            {
                StatusPlano = "Plano vencido";
            }
            else if (Clinica.Pago)
            {
                StatusPlano = "Plano ativo";
            }
        }

        private void CarregarDivulgacaoRegionalAtiva(int clinicaId)
        {
            DivulgacaoRegionalAtiva = _context.DivulgacoesRegionais
                .Where(d =>
                    d.ClinicaId == clinicaId &&
                    d.Pago &&
                    d.Aprovado &&
                    d.Ativo &&
                    (!d.DataFim.HasValue || d.DataFim.Value.Date >= DateTime.Today))
                .OrderByDescending(d => d.DataInicio ?? d.DataPagamento ?? d.DataSolicitacao)
                .FirstOrDefault();

            if (DivulgacaoRegionalAtiva == null)
                return;

            NomePlanoRegionalFormatado = DivulgacaoRegionalAtiva.NomePlano;
            ValorPlanoRegionalFormatado = DivulgacaoRegionalAtiva.Valor.ToString("C", new CultureInfo("pt-BR"));
            CidadesPlanoRegional = DivulgacaoRegionalAtiva.CidadesSelecionadas;

            if (DivulgacaoRegionalAtiva.DataFim.HasValue)
            {
                DataFimPlanoRegionalFormatada = DivulgacaoRegionalAtiva.DataFim.Value.ToString("dd/MM/yyyy");

                DiasRestantesPlanoRegional = (int)Math.Ceiling(
                    (DivulgacaoRegionalAtiva.DataFim.Value.Date - DateTime.Today).TotalDays
                );
            }
        }

        private void AtualizarDivulgacoesRegionaisVencidas(int clinicaId)
        {
            var divulgacoesVencidas = _context.DivulgacoesRegionais
                .Where(d =>
                    d.ClinicaId == clinicaId &&
                    d.Ativo &&
                    d.DataFim.HasValue &&
                    d.DataFim.Value.Date < DateTime.Today)
                .ToList();

            if (!divulgacoesVencidas.Any())
                return;

            foreach (var divulgacao in divulgacoesVencidas)
            {
                divulgacao.Ativo = false;
                divulgacao.Status = "Expirado";
            }

            _context.SaveChanges();
        }

        private void CarregarAvisosAlteracao(int clinicaId)
        {
            var ultimaAlteracao = _context.AlteracoesClinicas
                .Where(a => a.ClinicaId == clinicaId)
                .OrderByDescending(a => a.DataSolicitacao)
                .FirstOrDefault();

            if (ultimaAlteracao == null)
                return;

            if (ultimaAlteracao.Status == "Pendente")
            {
                AlteracaoPendente = ultimaAlteracao;
            }

            if (ultimaAlteracao.Status == "Recusada")
            {
                AlteracaoRecusada = ultimaAlteracao;
            }
        }

        public IActionResult OnPostExcluirAlteracaoRecusada(int alteracaoId)
        {
            var id = HttpContext.Session.GetString("ClinicaLogada");

            if (string.IsNullOrEmpty(id))
            {
                return RedirectToPage("/LoginClinica");
            }

            var clinicaId = int.Parse(id);

            var alteracao = _context.AlteracoesClinicas
                .FirstOrDefault(a =>
                    a.Id == alteracaoId &&
                    a.ClinicaId == clinicaId &&
                    a.Status == "Recusada"
                );

            if (alteracao != null)
            {
                alteracao.Status = "Recusa visualizada";
                alteracao.DataAnalise ??= DateTime.Now;

                _context.SaveChanges();
            }

            return RedirectToPage("/PainelClinica");
        }
    }
}