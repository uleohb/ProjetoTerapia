using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;
using System;
using System.Globalization;
using System.Linq;

namespace ProjetoTerapia.Pages
{
    public class PagamentoClinicaModel : PageModel
    {
        private readonly AppDbContext _context;

        public PagamentoClinicaModel(AppDbContext context)
        {
            _context = context;
        }

        public Clinica? Clinica { get; set; }

        public int? DiasRestantes { get; set; }

        public bool PlanoVencido { get; set; }

        public bool PlanoPertoDeVencer { get; set; }

        public string ValorPlanoFormatado { get; set; } = "R$ 360,00";

        public IActionResult OnGet()
        {
            var id = HttpContext.Session.GetString("ClinicaLogada");

            if (id == null)
            {
                return RedirectToPage("/LoginClinica");
            }

            Clinica = _context.Clinicas.FirstOrDefault(c => c.Id == int.Parse(id));

            if (Clinica == null)
            {
                return RedirectToPage("/LoginClinica");
            }

            if (Clinica.ValorPlano.HasValue)
            {
                ValorPlanoFormatado = Clinica.ValorPlano.Value.ToString("C", new CultureInfo("pt-BR"));
            }

            if (Clinica.DataVencimento.HasValue)
            {
                DiasRestantes = (int)Math.Ceiling(
                    (Clinica.DataVencimento.Value.Date - DateTime.Today).TotalDays
                );

                PlanoVencido = DiasRestantes < 0;
                PlanoPertoDeVencer = DiasRestantes <= 30 && DiasRestantes >= 0;
            }

            return Page();
        }
    }
}