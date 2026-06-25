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

        public int Visualizacoes { get; set; }

        public int CliquesWhatsapp { get; set; }

        public double TaxaConversao { get; set; }

        public string StatusPlano { get; set; } = "";

        public string ValorPlanoFormatado { get; set; } = "Não definido";

        public string DataVencimentoFormatada { get; set; } = "Não definido";

        public int? DiasRestantes { get; set; }

        public bool PlanoVencido { get; set; }

        public bool PerfilPublicoAtivo { get; set; }

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

            if (Clinica.ValorPlano.HasValue)
            {
                ValorPlanoFormatado = Clinica.ValorPlano.Value.ToString("C", new CultureInfo("pt-BR"));
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
    }
}