using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;

namespace ProjetoTerapia.Pages
{
    public class NovaConsultaModel : PageModel
    {
        private readonly AppDbContext _context;

        public NovaConsultaModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Consulta Consulta { get; set; } = new();

        public IActionResult OnGet()
        {
            return Page();
        }

        public IActionResult OnPost()
        {
            var clinicaIdString =
                HttpContext.Session.GetString("ClinicaLogada");

            if (string.IsNullOrEmpty(clinicaIdString))
            {
                return RedirectToPage("/LoginClinica");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            Consulta.ClinicaId = int.Parse(clinicaIdString);

            _context.Consultas.Add(Consulta);

            _context.SaveChanges();

            return RedirectToPage("/AgendaClinica");
        }
    }
}