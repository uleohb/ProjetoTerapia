using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;
using System.Security.Claims;

namespace ProjetoTerapia.Pages
{
    public class LoginPacienteModel : PageModel
    {
        private readonly AppDbContext _context;

        public LoginPacienteModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Email { get; set; } = "";

        [BindProperty]
        public string Senha { get; set; } = "";

        public string Mensagem { get; set; } = "";

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            var emailNormalizado = Email.Trim().ToLower();

            var existeComoProfissional = _context.Clinicas
                .Any(x => x.Email.ToLower() == emailNormalizado);

            if (existeComoProfissional)
            {
                Mensagem = "Este email está vinculado a uma conta profissional. Acesse pela área do profissional.";
                return Page();
            }

            var paciente = _context.Pacientes
                .FirstOrDefault(x => x.Email.ToLower() == emailNormalizado);

            if (paciente == null)
            {
                Mensagem = "Não encontramos uma conta de paciente com este email.";
                return Page();
            }

            if (string.IsNullOrEmpty(paciente.SenhaHash))
            {
                Mensagem = "Esta conta foi criada pelo Google. Use o botão Entrar com Google.";
                return Page();
            }

            var partes = paciente.SenhaHash.Split(".");

            if (partes.Length != 2)
            {
                Mensagem = "Não foi possível validar sua senha. Tente redefinir sua senha.";
                return Page();
            }

            var salt = Convert.FromBase64String(partes[0]);

            var hash = Convert.ToBase64String(
                KeyDerivation.Pbkdf2(
                    password: Senha,
                    salt: salt,
                    prf: KeyDerivationPrf.HMACSHA256,
                    iterationCount: 100000,
                    numBytesRequested: 32
                )
            );

            if (hash != partes[1])
            {
                Mensagem = "Email ou senha inválidos.";
                return Page();
            }

            HttpContext.Session.SetString(
                "PacienteLogado",
                paciente.Id.ToString()
            );

            return RedirectToPage("/HomePaciente");
        }

        public IActionResult OnGetLoginGoogle()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Page(
                    "/LoginPaciente",
                    pageHandler: "GoogleResponse")
            };

            return Challenge(
                properties,
                GoogleDefaults.AuthenticationScheme
            );
        }

        public async Task<IActionResult> OnGetGoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            if (!result.Succeeded)
            {
                return RedirectToPage("/LoginPaciente");
            }

            var email = result.Principal?
                .FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                Mensagem = "Não foi possível obter o email da conta Google.";
                return Page();
            }

            var emailNormalizado = email.Trim().ToLower();

            var existeComoProfissional = _context.Clinicas
                .Any(x => x.Email.ToLower() == emailNormalizado);

            if (existeComoProfissional)
            {
                Mensagem = "Este email está vinculado a uma conta profissional. Acesse pela área do profissional.";
                return Page();
            }

            var paciente = _context.Pacientes
                .FirstOrDefault(x => x.Email.ToLower() == emailNormalizado);

            if (paciente == null)
            {
                Mensagem = "Não encontramos uma conta de paciente com este Google. Crie sua conta primeiro.";
                return Page();
            }

            HttpContext.Session.SetString(
                "PacienteLogado",
                paciente.Id.ToString()
            );

            return RedirectToPage("/HomePaciente");
        }

        public IActionResult OnGetSair()
        {
            HttpContext.Session.Remove("PacienteLogado");
            return RedirectToPage("/Teste");
        }
    }
}