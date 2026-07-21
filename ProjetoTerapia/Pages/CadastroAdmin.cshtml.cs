using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;

namespace ProjetoTerapia.Pages
{
    public class CadastroAdminModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public CadastroAdminModel(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [BindProperty]
        public string Nome { get; set; } = "";

        [BindProperty]
        public string Email { get; set; } = "";

        [BindProperty]
        public string Senha { get; set; } = "";

        [BindProperty]
        public string ConfirmarSenha { get; set; } = "";

        [BindProperty]
        public string Perfil { get; set; } = "Operacional";

        [BindProperty(SupportsGet = true)]
        public string Chave { get; set; } = "";

        public bool PrimeiroAdmin { get; set; }

        public IActionResult OnGet()
        {
            PrimeiroAdmin = !_context.AdminUsuarios.Any();

            if (PrimeiroAdmin && !ChaveCadastroValida())
            {
                return RedirectToPage("/LoginAdmin");
            }

            if (!PrimeiroAdmin && !AdminPodeCadastrarAdmin())
            {
                return RedirectToPage("/AdminLH");
            }

            return Page();
        }

        public IActionResult OnPost()
        {
            PrimeiroAdmin = !_context.AdminUsuarios.Any();

            if (PrimeiroAdmin && !ChaveCadastroValida())
            {
                return RedirectToPage("/LoginAdmin");
            }

            if (!PrimeiroAdmin && !AdminPodeCadastrarAdmin())
            {
                return RedirectToPage("/AdminLH");
            }

            if (string.IsNullOrWhiteSpace(Nome) ||
                string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Senha) ||
                string.IsNullOrWhiteSpace(ConfirmarSenha))
            {
                TempData["Erro"] = "Preencha todos os campos obrigatórios.";
                return Page();
            }

            if (Senha.Length < 6)
            {
                TempData["Erro"] = "A senha precisa ter pelo menos 6 caracteres.";
                return Page();
            }

            if (Senha != ConfirmarSenha)
            {
                TempData["Erro"] = "As senhas não conferem.";
                return Page();
            }

            var emailNormalizado = Email.Trim().ToLower();

            var emailJaExiste = _context.AdminUsuarios
                .Any(a => a.Email.ToLower() == emailNormalizado);

            if (emailJaExiste)
            {
                TempData["Erro"] = "Já existe um administrador com este email.";
                return Page();
            }

            if (PrimeiroAdmin)
            {
                Perfil = "Master";
            }

            if (Perfil != "Master" && Perfil != "Gestor" && Perfil != "Operacional")
            {
                Perfil = "Operacional";
            }

            var admin = new AdminUsuario
            {
                Nome = Nome.Trim(),
                Email = emailNormalizado,
                Perfil = Perfil,
                Ativo = true,
                DataCriacao = DateTime.Now
            };

            var hasher = new PasswordHasher<AdminUsuario>();
            admin.SenhaHash = hasher.HashPassword(admin, Senha);

            _context.AdminUsuarios.Add(admin);
            _context.SaveChanges();

            _context.AdminLogs.Add(new AdminLog
            {
                AdminUsuarioId = admin.Id,
                NomeAdmin = PrimeiroAdmin
                    ? admin.Nome
                    : HttpContext.Session.GetString("AdminNome") ?? admin.Nome,

                PerfilAdmin = PrimeiroAdmin
                    ? admin.Perfil
                    : HttpContext.Session.GetString("AdminPerfil") ?? admin.Perfil,

                Acao = PrimeiroAdmin ? "Criação do primeiro admin" : "Cadastro de admin",

                Descricao = PrimeiroAdmin
                    ? $"Primeiro administrador Master criado: {admin.Nome}."
                    : $"Administrador criado: {admin.Nome} ({admin.Perfil}).",

                DataAcao = DateTime.Now
            });

            _context.SaveChanges();

            if (PrimeiroAdmin)
            {
                HttpContext.Session.SetString("AdminLogado", "true");
                HttpContext.Session.SetString("AdminId", admin.Id.ToString());
                HttpContext.Session.SetString("AdminNome", admin.Nome);
                HttpContext.Session.SetString("AdminEmail", admin.Email);
                HttpContext.Session.SetString("AdminPerfil", admin.Perfil);

                TempData["MensagemSucesso"] = "Administrador cadastrado com sucesso.";
                return RedirectToPage("/AdminLH");
            }

            TempData["MensagemSucesso"] = "Administrador cadastrado com sucesso.";
            return RedirectToPage("/AdminLH");
        }

        private bool AdminPodeCadastrarAdmin()
        {
            return HttpContext.Session.GetString("AdminLogado") == "true" &&
                   HttpContext.Session.GetString("AdminPerfil") == "Master";
        }

        private bool ChaveCadastroValida()
        {
            var chaveCorreta = _config["AdminSettings:ChaveCadastroAdmin"];

            if (string.IsNullOrWhiteSpace(chaveCorreta))
            {
                return false;
            }

            return Chave == chaveCorreta;
        }
    }
}