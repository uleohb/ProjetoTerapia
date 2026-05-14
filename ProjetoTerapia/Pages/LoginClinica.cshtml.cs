using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;
using System.Linq;

namespace ProjetoTerapia.Pages
{
    public class LoginClinicaModel : PageModel
    {

        private readonly AppDbContext _context;

        public LoginClinicaModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Email { get; set; } = "";

        [BindProperty]
        public string Senha { get; set; } = "";

        public string? Erro { get; set; } 

        public IActionResult OnPost()
        {
            var clinica = _context.Clinicas.FirstOrDefault(c => c.Email == Email && c.Senha == Senha);

            if (clinica == null)
            {
                Erro = "Email ou senha inválidos.";

                return Page();
            }

            HttpContext.Session.SetString("ClinicaLogada", clinica.Id.ToString());

            return RedirectToPage("/PainelClinica");
        }
        
        
        
        public IActionResult OnGetLoginGoogle()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Page("/PainelClinica")
            };

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

    }

}

