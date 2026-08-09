using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;

namespace ProjetoTerapia.Pages
{
    public class LoginVendedorModel : PageModel
    {
        private readonly AppDbContext _context;

        public LoginVendedorModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Email { get; set; } = "";

        [BindProperty]
        public string Senha { get; set; } = "";

        public string MensagemErro { get; set; } = "";

        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetString("VendedorLogado") == "true")
            {
                return RedirectToPage("/PainelVendedor");
            }

            return Page();
        }

        public IActionResult OnPost()
        {
            var emailNormalizado = Email.Trim().ToLower();

            var vendedor = _context.Vendedores
                .FirstOrDefault(v => v.Email.ToLower() == emailNormalizado && v.Ativo);

            if (vendedor == null)
            {
                MensagemErro = "Email ou senha inválidos.";
                return Page();
            }

            var hasher = new PasswordHasher<Vendedor>();

            var resultado = hasher.VerifyHashedPassword(
                vendedor,
                vendedor.SenhaHash,
                Senha
            );

            if (resultado == PasswordVerificationResult.Failed)
            {
                MensagemErro = "Email ou senha inválidos.";
                return Page();
            }

            HttpContext.Session.Clear();

            HttpContext.Session.SetString("VendedorLogado", "true");
            HttpContext.Session.SetString("VendedorId", vendedor.Id.ToString());
            HttpContext.Session.SetString("VendedorNome", vendedor.Nome);

            return RedirectToPage("/PainelVendedor");
        }
    }
}