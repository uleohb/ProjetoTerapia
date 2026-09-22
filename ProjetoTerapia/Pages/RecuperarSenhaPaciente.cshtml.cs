using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;
using ProjetoTerapia.Services;
using System.Security.Cryptography;

namespace ProjetoTerapia.Pages
{
    public class RecuperarSenhaPacienteModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly EmailService _email;

        public RecuperarSenhaPacienteModel(AppDbContext context, EmailService email)
        {
            _context = context;
            _email = email;
        }

        [BindProperty]
        public string Email { get; set; } = "";

        public string Mensagem { get; set; } = "";

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPost()
        {
            var emailNormalizado = Email.Trim().ToLower();

            var existeComoProfissional = _context.Clinicas
                .Any(x => x.Email.ToLower() == emailNormalizado);

            if (existeComoProfissional)
            {
                Mensagem = "Este email está vinculado a uma conta profissional. Use a recuperação de senha da área profissional.";
                return Page();
            }

            var paciente = _context.Pacientes
                .FirstOrDefault(x => x.Email.ToLower() == emailNormalizado);

            if (paciente == null)
            {
                Mensagem = "Se esse email existir como paciente, você receberá um link para redefinir sua senha.";
                return Page();
            }

            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(64));

            var recuperacao = new RecuperacaoSenhaPaciente
            {
                PacienteId = paciente.Id,
                Token = token,
                Expiracao = DateTime.Now.AddMinutes(30),
                Usado = false
            };

            _context.RecuperacoesSenhaPacientes.Add(recuperacao);
            await _context.SaveChangesAsync();

            var link = $"{Request.Scheme}://{Request.Host}/RedefinirSenhaPaciente?token={token}";

            await _email.EnviarEmail(
                paciente.Email,
                "Recuperação de senha - AlinhaVida",
                $@"
                   Olá, {paciente.Nome}.

                   Recebemos uma solicitação para redefinir sua senha no AlinhaVida.

                   Acesse o link abaixo para criar uma nova senha:

                   {link}

                   Esse link expira em 30 minutos.

                   Caso você não tenha solicitado essa alteração, apenas ignore este email.

                   Equipe AlinhaVida
                "
            );

            Mensagem = "Se esse email existir como paciente, você receberá um link para redefinir sua senha.";
            return Page();
        }
    }
}