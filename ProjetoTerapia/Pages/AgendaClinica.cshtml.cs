using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;

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

        public int ConsultasHoje { get; set; }

        public int Confirmadas { get; set; }

        public int Pendentes { get; set; }

        public int Canceladas { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Busca { get; set; }

        public IActionResult OnGet()
        {
            CarregarAgenda();
            return Page();
        }

        public IActionResult OnPostConfirmar(int id)
        {
            var consulta = _context.Consultas
                .FirstOrDefault(x => x.Id == id);

            if (consulta != null)
            {
                consulta.Status = "Confirmado";
                _context.SaveChanges();
            }

            return RedirectToPage();
        }

        public IActionResult OnPostCancelar(int id)
        {
            var consulta = _context.Consultas
                .FirstOrDefault(x => x.Id == id);

            if (consulta != null)
            {
                consulta.Status = "Cancelado";
                _context.SaveChanges();
            }

            return RedirectToPage();
        }

        public IActionResult OnPostExcluir(int id)
        {
            var consulta = _context.Consultas
                .FirstOrDefault(x => x.Id == id);

            if (consulta != null)
            {
                _context.Consultas.Remove(consulta);
                _context.SaveChanges();
            }

            return RedirectToPage();
        }

        private void CarregarAgenda()
        {
            var clinicaIdString =
                HttpContext.Session.GetString("ClinicaLogada");

            if (string.IsNullOrEmpty(clinicaIdString))
                return;

            int clinicaId = int.Parse(clinicaIdString);

            var consultas = _context.Consultas
            .Where(x => x.ClinicaId == clinicaId);

            if (!string.IsNullOrWhiteSpace(Busca))
            {
                consultas = consultas.Where(x =>
                    x.NomePaciente.Contains(Busca));
            }

            Consultas = consultas
                .OrderBy(x => x.DataConsulta)
                .ToList();

            ConsultasHoje = Consultas.Count(x =>
                x.DataConsulta.HasValue &&
                x.DataConsulta.Value.Date == DateTime.Today);

            Confirmadas = Consultas.Count(x =>
                x.Status == "Confirmado");

            Pendentes = Consultas.Count(x =>
                x.Status == "Pendente");

            Canceladas = Consultas.Count(x =>
                x.Status == "Cancelado");
        }
    }
}