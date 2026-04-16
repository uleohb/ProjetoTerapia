using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ProjetoTerapia.Pages
{
    public class LoginAdminModel : PageModel
    {
        private readonly IConfiguration _config;

        public LoginAdminModel(IConfiguration config)
        {
            _config = config;
        }

        [BindProperty]
        public string? Usuario { get; set; }

        [BindProperty]
        public string? Senha { get; set; }

        public bool Erro { get; set; }

        public IActionResult OnPost()
        {
            var usuarioCorreto = _config["AdminSettings:Usuario"];
            var senhaCorreta = _config["AdminSettings:Senha"];

            if (Usuario == usuarioCorreto && Senha == senhaCorreta)
            {
                HttpContext.Session.SetString("AdminLogado", "true");
                return RedirectToPage("/AdminLH");
            }

            Erro = true;
            return Page();
        }
    }
}