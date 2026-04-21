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

        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetString("AdminLogado") != "true")
            {
                return RedirectToPage("/LoginAdmin");
            }

            Clinicas = _context.Clinicas.ToList();
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
                        // não paga sem aprovar
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