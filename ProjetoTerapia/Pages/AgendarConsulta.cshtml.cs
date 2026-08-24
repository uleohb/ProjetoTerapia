using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;
using System.Net;

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

        public string LinkWhatsappProfissional { get; set; } = "";

        [BindProperty]
        public int ClinicaId { get; set; }

        [BindProperty]
        public string WhatsappPaciente { get; set; } = "";

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
                TempData["Erro"] = "Entre como paciente para solicitar contato com o profissional.";
                return RedirectToPage("/LoginPaciente");
            }

            var pacienteId = int.Parse(pacienteIdString);

            Paciente = _context.Pacientes.FirstOrDefault(p => p.Id == pacienteId)!;

            if (Paciente == null)
            {
                return RedirectToPage("/LoginPaciente");
            }

            Clinica = _context.Clinicas
                .FirstOrDefault(c => c.Id == id && c.Aprovado && c.Pago)!;

            if (Clinica == null)
            {
                return NotFound();
            }

            UltimoResultado = _context.ResultadosTestePacientes
                .Where(r => r.PacienteId == pacienteId)
                .OrderByDescending(r => r.DataResultado)
                .FirstOrDefault();

            ClinicaId = Clinica.Id;

            MontarLinkWhatsappProfissional();

            return Page();
        }

        public IActionResult OnPost()
        {
            var pacienteIdString = HttpContext.Session.GetString("PacienteLogado");

            if (string.IsNullOrEmpty(pacienteIdString))
            {
                TempData["Erro"] = "Entre como paciente para solicitar contato com o profissional.";
                return RedirectToPage("/LoginPaciente");
            }

            var pacienteId = int.Parse(pacienteIdString);

            Paciente = _context.Pacientes.FirstOrDefault(p => p.Id == pacienteId)!;

            if (Paciente == null)
            {
                return RedirectToPage("/LoginPaciente");
            }

            Clinica = _context.Clinicas
                .FirstOrDefault(c => c.Id == ClinicaId && c.Aprovado && c.Pago)!;

            if (Clinica == null)
            {
                return NotFound();
            }

            MontarLinkWhatsappProfissional();

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

            var whatsappLimpo = ApenasNumeros(WhatsappPaciente);

            if (!string.IsNullOrWhiteSpace(whatsappLimpo) &&
                whatsappLimpo.Length < 10)
            {
                TempData["Erro"] = "Informe um WhatsApp válido com DDD ou deixe o campo em branco.";
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
                TelefonePaciente = whatsappLimpo,
                DataConsulta = DateTime.Now,
                TipoAtendimento = TipoAtendimento,
                Observacoes = Observacoes,
                Status = "Pendente",
                DataCriacao = DateTime.Now
            };

            _context.Consultas.Add(consulta);
            _context.SaveChanges();

            TempData["Sucesso"] = "Solicitação enviada com sucesso. O profissional poderá entrar em contato pelo WhatsApp, se você informou o número.";

            return RedirectToPage("/AgendarConsulta", new { id = Clinica.Id });
        }

        private void CarregarResultado(int pacienteId)
        {
            UltimoResultado = _context.ResultadosTestePacientes
                .Where(r => r.PacienteId == pacienteId)
                .OrderByDescending(r => r.DataResultado)
                .FirstOrDefault();
        }

        private void MontarLinkWhatsappProfissional()
        {
            var telefone = ApenasNumeros(Clinica.Telefone);

            if (string.IsNullOrWhiteSpace(telefone))
            {
                LinkWhatsappProfissional = "";
                return;
            }

            if ((telefone.Length == 10 || telefone.Length == 11) &&
                !telefone.StartsWith("55"))
            {
                telefone = "55" + telefone;
            }

            var mensagem =
$@"Olá, encontrei seu perfil no AlinhaMente e gostaria de conversar sobre uma consulta.

Profissional: {Clinica.Nome}";

            var mensagemCodificada = WebUtility.UrlEncode(mensagem);

            LinkWhatsappProfissional = $"https://wa.me/{telefone}?text={mensagemCodificada}";
        }

        private string ApenasNumeros(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return "";
            }

            return new string(valor.Where(char.IsDigit).ToArray());
        }
    }
}