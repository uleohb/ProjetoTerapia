using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;

namespace ProjetoTerapia.Pages
{
    public class LoginAdminModel : PageModel
    {
        private readonly AppDbContext _context;

        public LoginAdminModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Email { get; set; } = "";

        [BindProperty]
        public string Senha { get; set; } = "";

        public bool Erro { get; set; }

        public bool ExisteAdmin { get; set; }

        public void OnGet()
        {
            ExisteAdmin = _context.AdminUsuarios.Any();
        }

        public IActionResult OnPost()
        {
            ExisteAdmin = _context.AdminUsuarios.Any();

            if (!ExisteAdmin)
            {
                Erro = true;
                return Page();
            }

            var emailNormalizado = Email.Trim().ToLower();

            var admin = _context.AdminUsuarios
                .FirstOrDefault(a => a.Email.ToLower() == emailNormalizado && a.Ativo);

            if (admin == null)
            {
                Erro = true;
                return Page();
            }

            var hasher = new PasswordHasher<AdminUsuario>();

            var resultado = hasher.VerifyHashedPassword(
                admin,
                admin.SenhaHash,
                Senha
            );

            if (resultado == PasswordVerificationResult.Failed)
            {
                Erro = true;
                return Page();
            }

            HttpContext.Session.SetString("AdminLogado", "true");
            HttpContext.Session.SetString("AdminId", admin.Id.ToString());
            HttpContext.Session.SetString("AdminNome", admin.Nome);
            HttpContext.Session.SetString("AdminEmail", admin.Email);
            HttpContext.Session.SetString("AdminPerfil", admin.Perfil);

            _context.AdminLogs.Add(new AdminLog
            {
                AdminUsuarioId = admin.Id,
                NomeAdmin = admin.Nome,
                PerfilAdmin = admin.Perfil,
                Acao = "Login",
                Descricao = $"Administrador {admin.Nome} acessou o painel.",
                DataAcao = DateTime.Now
            });

            _context.SaveChanges();

            return RedirectToPage("/AdminLH");
        }
    }
}