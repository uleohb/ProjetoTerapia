using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;
using System.Collections.Generic;
using System.Linq;

namespace ProjetoTerapia.Pages
{
    public class ClinicasModel : PageModel
    {
        private readonly AppDbContext _context;

        public ClinicasModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Clinica> Clinicas { get; set; } = new List<Clinica>();

        public void OnGet()
        {
             Clinicas = _context.Clinicas
            .Where(c => c.Aprovado && c.Pago)
            .ToList();
        }
    }
}