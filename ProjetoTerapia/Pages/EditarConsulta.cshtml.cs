using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;

namespace ProjetoTerapia.Pages
{
    public class EditarConsultaModel : PageModel
    {
        private readonly AppDbContext _context;

        public EditarConsultaModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Consulta Consulta { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            var clinicaIdString =
                HttpContext.Session.GetString("ClinicaLogada");

            if (string.IsNullOrEmpty(clinicaIdString))
            {
                return RedirectToPage("/LoginClinica");
            }

            var consulta = _context.Consultas
                .FirstOrDefault(x => x.Id == id);

            if (consulta == null)
            {
                return RedirectToPage("/AgendaClinica");
            }

            Consulta = consulta;

            return Page();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var consultaBanco = _context.Consultas
                .FirstOrDefault(x => x.Id == Consulta.Id);

            if (consultaBanco == null)
            {
                return RedirectToPage("/AgendaClinica");
            }

            consultaBanco.NomePaciente = Consulta.NomePaciente;
            consultaBanco.TipoAtendimento = Consulta.TipoAtendimento;
            consultaBanco.DataConsulta = Consulta.DataConsulta;
            consultaBanco.Status = Consulta.Status;

            _context.SaveChanges();

            return RedirectToPage("/AgendaClinica");
        }
    }
}