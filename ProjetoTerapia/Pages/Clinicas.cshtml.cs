using Microsoft.AspNetCore.Mvc;
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

        public List<Clinica> Clinicas { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string Busca { get; set; } = "";

        public void OnGet()
        {
            var query = _context.Clinicas
                .Where(c => c.Aprovado && c.Pago);

            if (!string.IsNullOrEmpty(Busca))
            {
                query = query.Where(c =>
                    c.Nome.Contains(Busca) ||
                    c.Descricao.Contains(Busca) ||
                    c.Endereco.Contains(Busca));
            }

            Clinicas = query.ToList();
        }
    }
}