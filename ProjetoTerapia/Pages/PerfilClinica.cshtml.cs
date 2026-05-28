using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;

namespace ProjetoTerapia.Pages
{
    public class PerfilClinicaModel : PageModel
    {
        private readonly AppDbContext _context;

        public Clinica Clinica { get; set; } = new();

        public List<string> ListaEspecialidades { get; set; } = new();

        public PerfilClinicaModel(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet(int? id)
        {
            // TEMPORÁRIO:
            // se vier ID pega pelo ID
            // senão pega o primeiro

            Clinica = id.HasValue
                ? _context.Clinicas.FirstOrDefault(c => c.Id == id.Value)
                : _context.Clinicas.FirstOrDefault();

            if (Clinica == null)
            {
                return RedirectToPage("/Clinicas");
            }

            // transforma especialidades em lista
            if (!string.IsNullOrEmpty(Clinica.Especialidades))
            {
                ListaEspecialidades = Clinica.Especialidades
                    .Split(',')
                    .Select(e => e.Trim())
                    .ToList();
            }

            return Page();
        }
    }
}