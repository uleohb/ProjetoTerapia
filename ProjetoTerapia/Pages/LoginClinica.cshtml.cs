using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

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

            var clinica =
                _context.Clinicas
                .FirstOrDefault(c => c.Email == Email);


            if (clinica == null)
            {
                Erro = "Email ou senha inválidos";
                return Page();
            }


            if (string.IsNullOrEmpty(clinica.SenhaHash))
            {
                Erro = "Essa conta foi criada pelo Google. Entre usando Google.";
                return Page();
            }


            var partes = clinica.SenhaHash.Split(".");


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



            var clinica =
                _context.Clinicas
                .FirstOrDefault(c =>
                    c.Email.ToLower() == email.ToLower());



            // se não existe cria uma conta Google
            if (clinica == null)
            {

                clinica = new Clinica
                {
                    Nome = result.Principal?
                        .FindFirst(ClaimTypes.Name)?.Value ?? "",

                    Email = email,

                    SenhaHash = "",

                    Pago = false,

                    Aprovado = false
                };


                _context.Clinicas.Add(clinica);

                await _context.SaveChangesAsync();

            }



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