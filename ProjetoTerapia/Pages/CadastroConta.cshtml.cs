using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using System.Linq;


namespace ProjetoTerapia.Pages
{
    public class CadastroContaModel : PageModel
    {
        private readonly AppDbContext _context;

        public CadastroContaModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Email { get; set; } = "";

        [BindProperty]
        public string Senha { get; set; } = "";

        [BindProperty]
        public string ConfirmarSenha { get; set; } = "";

        public IActionResult OnPost()
        {
            // valida senha
            if (Senha != ConfirmarSenha)
            {
                ModelState.AddModelError("", "As senhas não coincidem.");
                return Page();
            }

            // valida se já existe
            if (_context.Clinicas.Any(c => c.Email == Email))
            {
                ModelState.AddModelError("", "Email já cadastrado.");
                return Page();
            }

            var clinica = new Clinica
            {
                Email = Email,
                Senha = Senha,
                Aprovado = false,
                Pago = false
            };

            _context.Clinicas.Add(clinica);
            _context.SaveChanges();

            // cria sessão
            HttpContext.Session.SetInt32("ClinicaId", clinica.Id);

            return RedirectToPage("/DashboardClinica");
        }

        public IActionResult OnGetLoginGoogle()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = "/"
            };

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }
    }
}