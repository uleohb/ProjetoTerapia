using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;
using Microsoft.EntityFrameworkCore;

namespace ProjetoTerapia.Pages
{
    public class AgendaClinicaModel : PageModel
    {
        private readonly AppDbContext _context;

        public AgendaClinicaModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Consulta> Consultas { get; set; } = new();

        public List<Consulta> SolicitacoesPendentes { get; set; } = new();

        public int ConsultasHoje { get; set; }

        public int Confirmadas { get; set; }

        public int Pendentes { get; set; }

        public int Canceladas { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Busca { get; set; }

        public Clinica Clinica { get; set; } = new();

        public IActionResult OnGet()
        {
            var clinicaId = ObterClinicaLogadaId();

            if (clinicaId == null)
            {
                return RedirectToPage("/LoginClinica");
            }

            CarregarAgenda(clinicaId.Value);

            return Page();
        }

        public IActionResult OnPostConfirmar(int id)
        {
            var clinicaId = ObterClinicaLogadaId();

            if (clinicaId == null)
            {
                return RedirectToPage("/LoginClinica");
            }

            var consulta = _context.Consultas
                .FirstOrDefault(x => x.Id == id && x.ClinicaId == clinicaId.Value);

            if (consulta != null)
            {
                consulta.Status = "Confirmado";
                _context.SaveChanges();
            }

            return RedirectToPage();
        }

        public IActionResult OnPostCancelar(int id)
        {
            var clinicaId = ObterClinicaLogadaId();

            if (clinicaId == null)
            {
                return RedirectToPage("/LoginClinica");
            }

            var consulta = _context.Consultas
                .FirstOrDefault(x => x.Id == id && x.ClinicaId == clinicaId.Value);

            if (consulta != null)
            {
                consulta.Status = "Cancelado";
                _context.SaveChanges();
            }

            return RedirectToPage();
        }

        public IActionResult OnPostExcluir(int id)
        {
            var clinicaId = ObterClinicaLogadaId();

            if (clinicaId == null)
            {
                return RedirectToPage("/LoginClinica");
            }

            var consulta = _context.Consultas
                .FirstOrDefault(x => x.Id == id && x.ClinicaId == clinicaId.Value);

            if (consulta != null)
            {
                _context.Consultas.Remove(consulta);
                _context.SaveChanges();
            }

            return RedirectToPage();
        }

        private void CarregarAgenda(int clinicaId)
        {
            Clinica = _context.Clinicas
                .FirstOrDefault(c => c.Id == clinicaId) ?? new Clinica();

            var query = _context.Consultas
                .Include(x => x.Paciente)
                .Include(x => x.ResultadoTestePaciente)
                .Where(x => x.ClinicaId == clinicaId);

            if (!string.IsNullOrWhiteSpace(Busca))
            {
                query = query.Where(x =>
                    x.NomePaciente.Contains(Busca) ||
                    x.EmailPaciente.Contains(Busca));
            }

            var todas = query.ToList();

            SolicitacoesPendentes = todas
                .Where(x => x.Status == "Pendente")
                .OrderBy(x => x.DataConsulta)
                .ToList();

            Consultas = todas
                .Where(x => x.Status == "Confirmado")
                .OrderBy(x => x.DataConsulta)
                .ToList();

            ConsultasHoje = Consultas.Count(x =>
                x.DataConsulta.HasValue &&
                x.DataConsulta.Value.Date == DateTime.Today);

            Confirmadas = todas.Count(x => x.Status == "Confirmado");

            Pendentes = todas.Count(x => x.Status == "Pendente");

            Canceladas = todas.Count(x => x.Status == "Cancelado");
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