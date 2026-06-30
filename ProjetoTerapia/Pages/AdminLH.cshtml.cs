using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ProjetoTerapia.Pages
{
    public class AdminLHModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AdminLHModel(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public List<Clinica> Clinicas { get; set; } = new List<Clinica>();

        public int TotalClinicas { get; set; }

        public int Pendentes { get; set; }

        public int Aprovadas { get; set; }

        public int Ativas { get; set; }

        public int PagamentosPendentes { get; set; }

        public int AlteracoesPendentes { get; set; }

        public List<Clinica> ClinicasComAlteracao { get; set; } = new();

        public int TotalVisualizacoes { get; set; }

        public int TotalCliquesWhatsapp { get; set; }

        public int TaxaConversao { get; set; }

        public decimal ReceitaPrevista { get; set; }

        public string ReceitaPrevistaFormatada { get; set; } = "R$ 0,00";

        [BindProperty(SupportsGet = true)]
        public string Busca { get; set; } = "";

        [BindProperty(SupportsGet = true)]
        public string FiltroStatus { get; set; } = "";

        public IActionResult OnGet()
        {
            if (!AdminEstaLogado())
            {
                return RedirectToPage("/LoginAdmin");
            }

            CarregarDados();

            return Page();
        }

        public IActionResult OnPost(int id, string acao)
        {
            if (!AdminEstaLogado())
            {
                return RedirectToPage("/LoginAdmin");
            }

            var clinica = _context.Clinicas.FirstOrDefault(c => c.Id == id);

            if (clinica == null)
            {
                TempData["MensagemErro"] = "Clínica não encontrada.";
                return RedirectToPage(new { aba = "clinicas" });
            }

            if (acao == "aprovar")
            {
                clinica.Aprovado = true;
                clinica.Pago = false;
                clinica.DataAprovacao = DateTime.Now;

                TempData["MensagemSucesso"] =
                    $"Clínica {clinica.Nome} aprovada com sucesso.";

                _context.SaveChanges();

                return RedirectToPage(new { aba = "clinicas" });
            }

            if (acao == "aprovarAlteracao")
            {
                clinica.ClinicaAlteracaoPendente = false;

                TempData["MensagemSucesso"] =
                    $"Alterações da clínica {clinica.Nome} aprovadas com sucesso.";

                _context.SaveChanges();

                return RedirectToPage(new { aba = "clinicas" });
            }

            if (acao == "pagar")
            {
                if (!clinica.Aprovado)
                {
                    TempData["MensagemErro"] =
                        "A clínica precisa ser aprovada antes da confirmação do pagamento.";

                    return RedirectToPage(new { aba = "clinicas" });
                }

                clinica.Pago = true;
                clinica.NomePlano = "Plano Profissional Anual";
                clinica.ValorPlano = 360;
                clinica.DataPagamento = DateTime.Now;
                clinica.DataVencimento = DateTime.Now.AddYears(1);

                TempData["MensagemSucesso"] =
                    $"Pagamento da clínica {clinica.Nome} confirmado com sucesso.";

                _context.SaveChanges();

                return RedirectToPage(new { aba = "pagamentos" });
            }

            if (acao == "suspender")
            {
                clinica.Pago = false;

                TempData["MensagemSucesso"] =
                    $"Plano da clínica {clinica.Nome} foi suspenso.";

                _context.SaveChanges();

                return RedirectToPage(new { aba = "clinicas" });
            }

            if (acao == "excluir")
            {
                try
                {
                    _context.Clinicas.Remove(clinica);
                    _context.SaveChanges();

                    TempData["MensagemSucesso"] =
                        $"Clínica {clinica.Nome} removida com sucesso.";
                }
                catch
                {
                    TempData["MensagemErro"] =
                        "Não foi possível excluir esta clínica porque ela pode possuir vínculos no sistema.";
                }

                return RedirectToPage(new { aba = "clinicas" });
            }

            TempData["MensagemErro"] = "Ação inválida.";

            return RedirectToPage(new { aba = "clinicas" });
        }

        public IActionResult OnPostLogout()
        {
            HttpContext.Session.Clear();
            return RedirectToPage("/LoginAdmin");
        }

        private bool AdminEstaLogado()
        {
            return HttpContext.Session.GetString("AdminLogado") == "true";
        }

        private void CarregarDados()
        {
            var todasClinicas = _context.Clinicas.ToList();

            ClinicasComAlteracao = todasClinicas
            .Where(c => c.ClinicaAlteracaoPendente)
            .OrderBy(c => c.Nome)
            .ToList();

            TotalClinicas = todasClinicas.Count;
            Pendentes = todasClinicas.Count(c => !c.Aprovado);
            Aprovadas = todasClinicas.Count(c => c.Aprovado && !c.Pago);
            Ativas = todasClinicas.Count(c => c.Pago);
            PagamentosPendentes = todasClinicas.Count(c => c.Aprovado && !c.Pago);
            AlteracoesPendentes = todasClinicas.Count(c => c.ClinicaAlteracaoPendente);

            TotalVisualizacoes = todasClinicas.Sum(c => c.Visualizacoes);
            TotalCliquesWhatsapp = todasClinicas.Sum(c => c.CliquesWhatsapp);

            ReceitaPrevista = todasClinicas
                .Where(c => c.Pago)
                .Sum(c => c.ValorPlano ?? 360);

            ReceitaPrevistaFormatada =
                ReceitaPrevista.ToString("C", new CultureInfo("pt-BR"));

            var totalAprovadasOuAtivas = Aprovadas + Ativas;

            TaxaConversao = totalAprovadasOuAtivas > 0
                ? (Ativas * 100) / totalAprovadasOuAtivas
                : 0;

            var query = todasClinicas.AsQueryable();

            if (!string.IsNullOrWhiteSpace(Busca))
            {
                var buscaNormalizada = Busca.Trim();

                query = query.Where(c =>
                    c.Nome.Contains(buscaNormalizada, StringComparison.OrdinalIgnoreCase) ||
                    c.Email.Contains(buscaNormalizada, StringComparison.OrdinalIgnoreCase) ||
                    c.Cidade.Contains(buscaNormalizada, StringComparison.OrdinalIgnoreCase)
                );
            }

            if (!string.IsNullOrWhiteSpace(FiltroStatus))
            {
                if (FiltroStatus == "pendente")
                {
                    query = query.Where(c => !c.Aprovado);
                }

                if (FiltroStatus == "aprovado")
                {
                    query = query.Where(c => c.Aprovado && !c.Pago);
                }

                if (FiltroStatus == "ativo")
                {
                    query = query.Where(c => c.Pago);
                }

                if (FiltroStatus == "alteracao")
                {
                    query = query.Where(c => c.ClinicaAlteracaoPendente);
                }
            }

            Clinicas = query
                .OrderByDescending(c => c.ClinicaAlteracaoPendente)
                .ThenBy(c => c.Aprovado)
                .ThenBy(c => c.Pago)
                .ThenBy(c => c.Nome)
                .ToList();
        }
    }
}