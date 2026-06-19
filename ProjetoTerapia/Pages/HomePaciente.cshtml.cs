using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;

namespace ProjetoTerapia.Pages
{
    public class HomePacienteModel : PageModel
    {
        private readonly AppDbContext _context;

        public HomePacienteModel(AppDbContext context)
        {
            _context = context;
        }

        public ProjetoTerapia.Models.Paciente? Paciente { get; set; }

        public List<RegistroHumor> Registros { get; set; } = new();

        [BindProperty]
        public int Nota { get; set; }

        [BindProperty]
        public string Observacao { get; set; } = "";

        public IActionResult OnGet()
        {
            var pacienteIdSessao = HttpContext.Session.GetString("PacienteLogado");

            if (string.IsNullOrEmpty(pacienteIdSessao))
            {
                return RedirectToPage("/LoginPaciente");
            }

            var pacienteId = int.Parse(pacienteIdSessao);

            Paciente = _context.Pacientes
                .FirstOrDefault(x => x.Id == pacienteId);

            if (Paciente == null)
            {
                HttpContext.Session.Remove("PacienteLogado");
                return RedirectToPage("/LoginPaciente");
            }

            Registros = _context.RegistrosHumor
                .Where(x => x.PacienteId == pacienteId)
                .OrderByDescending(x => x.Data)
                .Take(5)
                .ToList();

            return Page();
        }

        public IActionResult OnPost()
        {
            var pacienteIdSessao = HttpContext.Session.GetString("PacienteLogado");

            if (string.IsNullOrEmpty(pacienteIdSessao))
            {
                return RedirectToPage("/LoginPaciente");
            }

            var pacienteId = int.Parse(pacienteIdSessao);

            if (Nota < 1 || Nota > 5)
            {
                return RedirectToPage();
            }

            var registro = new RegistroHumor
            {
                PacienteId = pacienteId,
                Nota = Nota,
                Observacao = Observacao ?? "",
                Data = DateTime.Now
            };

            _context.RegistrosHumor.Add(registro);
            _context.SaveChanges();

            return RedirectToPage();
        }

        public IActionResult OnGetSair()
        {
            HttpContext.Session.Remove("PacienteLogado");
            return RedirectToPage("/Teste");
        }
    }
}