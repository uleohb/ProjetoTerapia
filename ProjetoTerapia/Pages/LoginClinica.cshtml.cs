using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;
using System.Linq;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
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
            var clinica = _context.Clinicas
                .FirstOrDefault(c =>
                    c.Email == Email &&
                    c.Senha == Senha);

            if (clinica == null)
            {
                Erro = "Email ou senha inválidos";
                return Page();
            }

            HttpContext.Session.SetString(
                "ClinicaLogada",
                clinica.Id.ToString()
            );

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
                return RedirectToPage("/LoginClinica");

            var email = result.Principal?
                .FindFirst(ClaimTypes.Email)?.Value;

            if (email == null)
                return RedirectToPage("/LoginClinica");

            var clinica = _context.Clinicas
               .FirstOrDefault(c =>
                c.Email != null &&
                c.Email.ToLower() == email.ToLower());

            // SE NÃO EXISTIR CRIA
            if (clinica == null)
            {
                clinica = new Clinica
                {
                    Nome = result.Principal?
                        .FindFirst(ClaimTypes.Name)?.Value ?? "",

                    Email = email,
                    Senha = "",
                    Pago = false
                };

                _context.Clinicas.Add(clinica);

                _context.SaveChanges();
            }

            // SALVA SESSÃO
            HttpContext.Session.SetString(
                "ClinicaLogada",
                clinica.Id.ToString()
            );

            return RedirectToPage("/PainelClinica");
        }

        // LOGOUT
        public IActionResult OnGetLogout()
        {
            HttpContext.Session.Clear();

            return RedirectToPage("/LoginClinica");
        }
    }
}