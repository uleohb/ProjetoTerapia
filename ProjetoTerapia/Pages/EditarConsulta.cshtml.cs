using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;
using Microsoft.EntityFrameworkCore;

namespace ProjetoTerapia.Pages
{
    public class EditarConsultaModel : PageModel
    {
        private readonly AppDbContext _context;

        public EditarConsultaModel(AppDbContext context)
        {
            _context = context;
        }

        public Consulta Consulta { get; set; } = new();

        [BindProperty]
        public int Id { get; set; }

        [BindProperty]
        public DateTime? DataConsulta { get; set; }

        [BindProperty]
        public string TipoAtendimento { get; set; } = "";

        [BindProperty]
        public string Status { get; set; } = "";

        [BindProperty]
        public string? AnotacoesProfissional { get; set; }

        public IActionResult OnGet(int id)
        {
            var clinicaId = ObterClinicaLogadaId();

            if (clinicaId == null)
            {
                return RedirectToPage("/LoginClinica");
            }

            Consulta = _context.Consultas
                .Include(c => c.ResultadoTestePaciente)
                .FirstOrDefault(c => c.Id == id && c.ClinicaId == clinicaId.Value)!;

            if (Consulta == null)
            {
                return NotFound();
            }

            Id = Consulta.Id;
            DataConsulta = Consulta.DataConsulta;
            TipoAtendimento = Consulta.TipoAtendimento;
            Status = Consulta.Status;
            AnotacoesProfissional = Consulta.AnotacoesProfissional;

            return Page();
        }

        public IActionResult OnPost()
        {
            var clinicaId = ObterClinicaLogadaId();

            if (clinicaId == null)
            {
                return RedirectToPage("/LoginClinica");
            }

            var consulta = _context.Consultas
                .FirstOrDefault(c => c.Id == Id && c.ClinicaId == clinicaId.Value);

            if (consulta == null)
            {
                return NotFound();
            }

            consulta.DataConsulta = DataConsulta;
            consulta.TipoAtendimento = TipoAtendimento;
            consulta.Status = Status;
            consulta.AnotacoesProfissional = AnotacoesProfissional;

            _context.SaveChanges();

            TempData["Sucesso"] = "Consulta atualizada com sucesso.";

            return RedirectToPage("/AgendaClinica");
        }

        private int? ObterClinicaLogadaId()
        {
            var clinicaIdString = HttpContext.Session.GetString("ClinicaLogada");

            if (string.IsNullOrEmpty(clinicaIdString))
            {
                return null;
            }

            if (!int.TryParse(clinicaIdString, out int clinicaId))
            {
                return null;
            }

            return clinicaId;
        }
    }
}