using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

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

        // NOVOS DADOS DO DASHBOARD

        public int TotalClinicas { get; set; }

        public int Pendentes { get; set; }

        public int Aprovadas { get; set; }

        public int Ativas { get; set; }

        public decimal ReceitaPrevista { get; set; }

        public int TaxaConversao { get; set; }

        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetString("AdminLogado") != "true")
            {
                return RedirectToPage("/LoginAdmin");
            }

            Clinicas = _context.Clinicas.ToList();

            // MÉTRICAS

            TotalClinicas = Clinicas.Count;

            Pendentes = Clinicas.Count(c => !c.Aprovado);

            Aprovadas = Clinicas.Count(c => c.Aprovado && !c.Pago);

            Ativas = Clinicas.Count(c => c.Pago);

            // Exemplo: plano anual R$ 360
            ReceitaPrevista = Ativas * 360;

            if (Aprovadas + Ativas > 0)
            {
                TaxaConversao = (Ativas * 100) / (Aprovadas + Ativas);
            }
            else
            {
                TaxaConversao = 0;
            }

            return Page();
        }

        public IActionResult OnPost(int id, string acao)
        {
            var clinica = _context.Clinicas.FirstOrDefault(c => c.Id == id);

            if (clinica != null)
            {
                if (acao == "aprovar")
                {
                    clinica.Aprovado = true;
                }

                if (acao == "pagar")
                {
                    if (!clinica.Aprovado)
                    {
                        return RedirectToPage();
                    }

                    clinica.Pago = true;
                }

                _context.SaveChanges();
            }

            return RedirectToPage();
        }

        public IActionResult OnPostLogout()
        {
            HttpContext.Session.Clear();
            return RedirectToPage("/LoginAdmin");
        }
    }
}