using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;

namespace ProjetoTerapia.Pages
{
    public class AgendarConsultaModel : PageModel
    {
        private readonly AppDbContext _context;

        public AgendarConsultaModel(AppDbContext context)
        {
            _context = context;
        }

        public Clinica Clinica { get; set; } = new();

        public Paciente Paciente { get; set; } = new();

        public ResultadoTestePaciente? UltimoResultado { get; set; }

        [BindProperty]
        public int ClinicaId { get; set; }

        [BindProperty]
        public DateTime DataConsulta { get; set; }

        [BindProperty]
        public string TipoAtendimento { get; set; } = "";

        [BindProperty]
        public string Observacoes { get; set; } = "";

        [BindProperty]
        public bool CompartilharResultado { get; set; } = true;

        public IActionResult OnGet(int id)
        {
            var pacienteIdString = HttpContext.Session.GetString("PacienteLogado");

            if (string.IsNullOrEmpty(pacienteIdString))
            {
                TempData["Erro"] = "Entre como paciente para agendar uma consulta.";
                return RedirectToPage("/LoginPaciente");
            }

            var pacienteId = int.Parse(pacienteIdString);

            Paciente = _context.Pacientes.FirstOrDefault(p => p.Id == pacienteId)!;

            if (Paciente == null)
            {
                return RedirectToPage("/LoginPaciente");
            }

            Clinica = _context.Clinicas.FirstOrDefault(c => c.Id == id && c.Aprovado && c.Pago)!;

            if (Clinica == null)
            {
                return NotFound();
            }

            UltimoResultado = _context.ResultadosTestePacientes
                .Where(r => r.PacienteId == pacienteId)
                .OrderByDescending(r => r.DataResultado)
                .FirstOrDefault();

            ClinicaId = Clinica.Id;

            return Page();
        }

        public IActionResult OnPost()
        {
            var pacienteIdString = HttpContext.Session.GetString("PacienteLogado");

            if (string.IsNullOrEmpty(pacienteIdString))
            {
                TempData["Erro"] = "Entre como paciente para agendar uma consulta.";
                return RedirectToPage("/LoginPaciente");
            }

            var pacienteId = int.Parse(pacienteIdString);

            Paciente = _context.Pacientes.FirstOrDefault(p => p.Id == pacienteId)!;

            if (Paciente == null)
            {
                return RedirectToPage("/LoginPaciente");
            }

            Clinica = _context.Clinicas.FirstOrDefault(c => c.Id == ClinicaId && c.Aprovado && c.Pago)!;

            if (Clinica == null)
            {
                return NotFound();
            }

            if (DataConsulta <= DateTime.Now)
            {
                TempData["Erro"] = "Escolha uma data e horário futuros.";
                CarregarResultado(pacienteId);
                return Page();
            }

            if (string.IsNullOrWhiteSpace(TipoAtendimento))
            {
                TempData["Erro"] = "Selecione a modalidade do atendimento.";
                CarregarResultado(pacienteId);
                return Page();
            }

            if (TipoAtendimento == "Online" && !Clinica.AtendimentoOnline)
            {
                TempData["Erro"] = "Este profissional não atende online.";
                CarregarResultado(pacienteId);
                return Page();
            }

            if (TipoAtendimento == "Presencial" && !Clinica.AtendimentoPresencial)
            {
                TempData["Erro"] = "Este profissional não atende presencialmente.";
                CarregarResultado(pacienteId);
                return Page();
            }

            var ultimoResultado = _context.ResultadosTestePacientes
                .Where(r => r.PacienteId == pacienteId)
                .OrderByDescending(r => r.DataResultado)
                .FirstOrDefault();

            var consulta = new Consulta
            {
                ClinicaId = Clinica.Id,
                PacienteId = Paciente.Id,
                ResultadoTestePacienteId = CompartilharResultado ? ultimoResultado?.Id : null,
                NomePaciente = Paciente.Nome,
                EmailPaciente = Paciente.Email,
                TelefonePaciente = "",
                DataConsulta = DataConsulta,
                TipoAtendimento = TipoAtendimento,
                Observacoes = Observacoes,
                Status = "Pendente",
                DataCriacao = DateTime.Now
            };

            _context.Consultas.Add(consulta);
            _context.SaveChanges();

            TempData["Sucesso"] = "Consulta solicitada com sucesso. Aguarde a confirmação do profissional.";

            return RedirectToPage("/AgendarConsulta", new { id = Clinica.Id });
        }

        private void CarregarResultado(int pacienteId)
        {
            UltimoResultado = _context.ResultadosTestePacientes
                .Where(r => r.PacienteId == pacienteId)
                .OrderByDescending(r => r.DataResultado)
                .FirstOrDefault();
        }
    }
}