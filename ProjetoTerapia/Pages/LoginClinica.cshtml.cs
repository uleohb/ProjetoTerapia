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
    public class LoginClinicaModel : PageModel
    {
        private readonly AppDbContext _context;

        [BindProperty]
        public string Email { get; set; } = "";

        [BindProperty]
        public string Senha { get; set; } = "";

        public string Erro { get; set; } = "";

        public LoginClinicaModel(AppDbContext context)
        {
            _context = context;
        }

        public void OnGet()
        {
        }

        // LOGIN NORMAL
        public IActionResult OnPost()
        {
            var emailNormalizado = NormalizarEmail(Email);

            if (string.IsNullOrWhiteSpace(emailNormalizado))
            {
                Erro = "Informe seu email.";
                return Page();
            }

            var clinica = BuscarClinicaPorEmail(emailNormalizado);

            if (clinica == null)
            {
                Erro = "Email ou senha inválidos.";
                return Page();
            }

            if (string.IsNullOrWhiteSpace(clinica.SenhaHash))
            {
                Erro = "Essa conta foi criada pelo Google. Entre usando Google.";
                return Page();
            }

            var partes = clinica.SenhaHash.Split(".");

            if (partes.Length != 2)
            {
                Erro = "Não foi possível validar sua senha. Entre em contato com o suporte.";
                return Page();
            }

            var salt = Convert.FromBase64String(partes[0]);

            var hash = Convert.ToBase64String(
                KeyDerivation.Pbkdf2(
                    password: Senha,
                    salt: salt,
                    prf: KeyDerivationPrf.HMACSHA256,
                    iterationCount: 100000,
                    numBytesRequested: 256 / 8
                )
            );

            if (hash != partes[1])
            {
                Erro = "Email ou senha inválidos.";
                return Page();
            }

            LogarClinica(clinica);

            return RedirectToPage("/PainelClinica");
        }

        // LOGIN GOOGLE
        public IActionResult OnGetLoginGoogle()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Page(
                    "/LoginClinica",
                    pageHandler: "GoogleResponse")
            };

            return Challenge(
                properties,
                GoogleDefaults.AuthenticationScheme);
        }

        // RESPOSTA GOOGLE
        public async Task<IActionResult> OnGetGoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            if (!result.Succeeded)
            {
                return RedirectToPage("/LoginClinica");
            }

            var email = result.Principal?
                .FindFirst(ClaimTypes.Email)?.Value;

            var nome = result.Principal?
                .FindFirst(ClaimTypes.Name)?.Value ?? "";

            var emailNormalizado = NormalizarEmail(email);

            if (string.IsNullOrWhiteSpace(emailNormalizado))
            {
                return RedirectToPage("/LoginClinica");
            }

            var clinica = BuscarClinicaPorEmail(emailNormalizado);

            // Só cria nova clínica se realmente não existir nenhuma com esse email
            if (clinica == null)
            {
                clinica = new Clinica
                {
                    Nome = nome,
                    Email = emailNormalizado,
                    SenhaHash = "",
                    Pago = false,
                    Aprovado = false,
                    PerfilCompleto = false,
                    ClinicaAlteracaoPendente = false
                };

                _context.Clinicas.Add(clinica);
                await _context.SaveChangesAsync();
            }
            else
            {
                // Garante que a clínica encontrada não fique sem email
                if (string.IsNullOrWhiteSpace(clinica.Email))
                {
                    clinica.Email = emailNormalizado;
                    await _context.SaveChangesAsync();
                }
            }

            LogarClinica(clinica);

            return RedirectToPage("/PainelClinica");
        }

        // LOGOUT
        public IActionResult OnGetLogout()
        {
            HttpContext.Session.Remove("ClinicaLogada");

            return RedirectToPage("/LoginClinica");
        }

        private Clinica? BuscarClinicaPorEmail(string email)
        {
            var emailNormalizado = NormalizarEmail(email);

            return _context.Clinicas
                .Where(c =>
                    c.Email != null &&
                    c.Email.ToLower().Trim() == emailNormalizado)
                .OrderByDescending(c => c.PerfilCompleto)
                .ThenByDescending(c => c.Aprovado)
                .ThenByDescending(c => c.Pago)
                .ThenByDescending(c => c.Id)
                .FirstOrDefault();
        }

        private void LogarClinica(Clinica clinica)
        {
            HttpContext.Session.Remove("PacienteLogado");
            HttpContext.Session.Remove("AdminLogado");

            HttpContext.Session.SetString(
                "ClinicaLogada",
                clinica.Id.ToString()
            );
        }

        private string NormalizarEmail(string? email)
        {
            return (email ?? "").Trim().ToLower();
        }
    }
}