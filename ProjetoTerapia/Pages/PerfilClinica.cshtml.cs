using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;
using Microsoft.AspNetCore.Http;

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
            if (id.HasValue)
            {
                Clinica = _context.Clinicas
                    .FirstOrDefault(c => c.Id == id.Value)!;
            }
            else
            {
                var clinicaLogada =
                    HttpContext.Session.GetString("ClinicaLogada");

                if (clinicaLogada != null)
                {
                    Clinica = _context.Clinicas
                        .FirstOrDefault(c =>
                            c.Id == int.Parse(clinicaLogada))!;
                }
            }

            if (Clinica == null)
            {
                return NotFound();
            }

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