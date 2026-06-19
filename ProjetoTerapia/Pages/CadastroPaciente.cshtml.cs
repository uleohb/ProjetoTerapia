using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;
using System.Security.Claims;
using System.Security.Cryptography;


namespace ProjetoTerapia.Pages
{
    public class CadastroPacienteModel : PageModel
    {
        private readonly AppDbContext _context;

        public CadastroPacienteModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Nome { get; set; } = "";

        [BindProperty]
        public string Email { get; set; } = "";

        [BindProperty]
        public string Senha { get; set; } = "";

        [BindProperty]
        public string ConfirmarSenha { get; set; } = "";

        public string Mensagem { get; set; } = "";

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            var emailNormalizado = Email.Trim().ToLower();

            if (Senha != ConfirmarSenha)
            {
                Mensagem = "As senhas não coincidem.";
                return Page();
            }

            if (_context.Pacientes.Any(x => x.Email.ToLower() == emailNormalizado))
            {
                Mensagem = "Já existe uma conta de paciente com este email.";
                return Page();
            }

            if (_context.Clinicas.Any(x => x.Email.ToLower() == emailNormalizado))
            {
                Mensagem = "Este email já está vinculado a uma conta profissional. Use outro email para criar uma conta de paciente.";
                return Page();
            }

            var paciente = new ProjetoTerapia.Models.Paciente
            {
                Nome = Nome.Trim(),
                Email = emailNormalizado,
                SenhaHash = GerarHashSenha(Senha),
                DataCadastro = DateTime.Now
            };

            _context.Pacientes.Add(paciente);
            _context.SaveChanges();

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
                    "/CadastroPaciente",
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
                return RedirectToPage("/CadastroPaciente");
            }

            var email = result.Principal?
                .FindFirst(ClaimTypes.Email)?.Value;

            var nome = result.Principal?
                .FindFirst(ClaimTypes.Name)?.Value;

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
                Mensagem = "Este email já está vinculado a uma conta profissional. Use outro email para criar uma conta de paciente.";
                return Page();
            }

            var paciente = _context.Pacientes
                .FirstOrDefault(x => x.Email.ToLower() == emailNormalizado);

            if (paciente == null)
            {
                paciente = new ProjetoTerapia.Models.Paciente
               {
                    Nome = nome ?? "",
                    Email = emailNormalizado,
                    SenhaHash = "",
                    DataCadastro = DateTime.Now
               };

                _context.Pacientes.Add(paciente);
                await _context.SaveChangesAsync();
            }

            HttpContext.Session.SetString(
                "PacienteLogado",
                paciente.Id.ToString()
            );

            return RedirectToPage("/HomePaciente");
        }

        private string GerarHashSenha(string senha)
        {
            var salt = RandomNumberGenerator.GetBytes(16);

            var hash = Convert.ToBase64String(
                KeyDerivation.Pbkdf2(
                    password: senha,
                    salt: salt,
                    prf: KeyDerivationPrf.HMACSHA256,
                    iterationCount: 100000,
                    numBytesRequested: 32
                )
            );

            return Convert.ToBase64String(salt) + "." + hash;
        }
    }
}