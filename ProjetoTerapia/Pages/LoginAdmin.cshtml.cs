using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
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


        // =====================================================
        // GET - ABRE A PÁGINA DE LOGIN
        // =====================================================

        public IActionResult OnGet()
        {
            // Evita indexação da área administrativa
            Response.Headers["X-Robots-Tag"] =
                "noindex, nofollow, noarchive";

            // Evita cache da página administrativa
            Response.Headers["Cache-Control"] =
                "no-store, no-cache, must-revalidate";

            Response.Headers["Pragma"] = "no-cache";


            // Se já estiver autenticado como admin,
            // não precisa mostrar a tela de login novamente
            if (HttpContext.Session.GetString("AdminLogado") == "true")
            {
                return RedirectToPage("/AdminLH");
            }


            /*
             * IMPORTANTE:
             *
             * Não consultamos o banco aqui.
             *
             * Antes havia:
             *
             * _context.AdminUsuarios.Any();
             *
             * Isso fazia a página depender do Azure SQL
             * somente para abrir.
             *
             * Se o SQL Serverless estivesse pausado,
             * o login administrativo demorava para carregar.
             */

            return Page();
        }


        // =====================================================
        // POST - REALIZA LOGIN
        // =====================================================

        public async Task<IActionResult> OnPostAsync()
        {
            Response.Headers["X-Robots-Tag"] =
                "noindex, nofollow, noarchive";

            Response.Headers["Cache-Control"] =
                "no-store, no-cache, must-revalidate";


            // -------------------------------------------------
            // VALIDAÇÕES BÁSICAS
            // -------------------------------------------------

            if (string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Senha))
            {
                await AplicarPequenoAtrasoDeSeguranca();

                Erro = true;

                return Page();
            }


            var emailNormalizado =
                NormalizarEmail(Email);


            // Limites defensivos contra entradas absurdamente grandes
            if (emailNormalizado.Length > 254 ||
                Senha.Length > 500)
            {
                await AplicarPequenoAtrasoDeSeguranca();

                Erro = true;

                return Page();
            }


            // -------------------------------------------------
            // PROCURA ADMINISTRADOR
            // -------------------------------------------------

            var admin = await _context.AdminUsuarios
                .FirstOrDefaultAsync(a =>
                    a.Email != null &&
                    a.Email.ToLower().Trim() == emailNormalizado &&
                    a.Ativo);


            /*
             * Não informamos se o email existe ou não.
             *
             * Para o usuário, qualquer falha resulta apenas em:
             *
             * "Não foi possível realizar o acesso."
             *
             * Isso reduz enumeração de contas administrativas.
             */
            if (admin == null)
            {
                await AplicarPequenoAtrasoDeSeguranca();

                Erro = true;

                return Page();
            }


            // -------------------------------------------------
            // VERIFICA HASH DA SENHA
            // -------------------------------------------------

            if (string.IsNullOrWhiteSpace(admin.SenhaHash))
            {
                await AplicarPequenoAtrasoDeSeguranca();

                Erro = true;

                return Page();
            }


            var hasher =
                new PasswordHasher<AdminUsuario>();


            PasswordVerificationResult resultado;

            try
            {
                resultado =
                    hasher.VerifyHashedPassword(
                        admin,
                        admin.SenhaHash,
                        Senha
                    );
            }
            catch
            {
                /*
                 * Se houver algum hash antigo/corrompido,
                 * não mostramos detalhes para o usuário.
                 */

                await AplicarPequenoAtrasoDeSeguranca();

                Erro = true;

                return Page();
            }


            if (resultado == PasswordVerificationResult.Failed)
            {
                await AplicarPequenoAtrasoDeSeguranca();

                Erro = true;

                return Page();
            }


            // -------------------------------------------------
            // ATUALIZA HASH ANTIGO, SE NECESSÁRIO
            // -------------------------------------------------

            if (resultado ==
                PasswordVerificationResult.SuccessRehashNeeded)
            {
                admin.SenhaHash =
                    hasher.HashPassword(
                        admin,
                        Senha
                    );

                await _context.SaveChangesAsync();
            }


            // -------------------------------------------------
            // LIMPA EVENTUAIS SESSÕES CONFLITANTES
            // -------------------------------------------------

            LimparSessaoAnterior();


            // -------------------------------------------------
            // CRIA SESSÃO ADMINISTRATIVA
            // -------------------------------------------------

            HttpContext.Session.SetString(
                "AdminLogado",
                "true"
            );

            HttpContext.Session.SetString(
                "AdminId",
                admin.Id.ToString()
            );

            HttpContext.Session.SetString(
                "AdminNome",
                admin.Nome ?? ""
            );

            HttpContext.Session.SetString(
                "AdminEmail",
                admin.Email ?? ""
            );

            HttpContext.Session.SetString(
                "AdminPerfil",
                admin.Perfil ?? ""
            );


            // -------------------------------------------------
            // LOG DE AUDITORIA
            // -------------------------------------------------

            _context.AdminLogs.Add(
                new AdminLog
                {
                    AdminUsuarioId = admin.Id,

                    NomeAdmin =
                        admin.Nome ?? "Administrador",

                    PerfilAdmin =
                        admin.Perfil ?? "",

                    Acao = "Login",

                    Descricao =
                        $"Administrador {admin.Nome} acessou o painel.",

                    DataAcao =
                        DateTime.Now
                }
            );


            await _context.SaveChangesAsync();


            // -------------------------------------------------
            // REDIRECIONA PARA O PAINEL
            // -------------------------------------------------

            return RedirectToPage("/AdminLH");
        }


        // =====================================================
        // NORMALIZA EMAIL
        // =====================================================

        private static string NormalizarEmail(
            string? email)
        {
            return (email ?? "")
                .Trim()
                .ToLowerInvariant();
        }


        // =====================================================
        // LIMPA SESSÕES ANTERIORES
        // =====================================================

        private void LimparSessaoAnterior()
        {
            /*
             * Remove sessões de outros tipos de usuário.
             *
             * O administrador entra em uma sessão administrativa
             * limpa.
             */

            HttpContext.Session.Remove(
                "PacienteLogado"
            );

            HttpContext.Session.Remove(
                "ClinicaLogada"
            );

            HttpContext.Session.Remove(
                "VendedorLogado"
            );


            // Remove dados de uma sessão admin anterior

            HttpContext.Session.Remove(
                "AdminLogado"
            );

            HttpContext.Session.Remove(
                "AdminId"
            );

            HttpContext.Session.Remove(
                "AdminNome"
            );

            HttpContext.Session.Remove(
                "AdminEmail"
            );

            HttpContext.Session.Remove(
                "AdminPerfil"
            );
        }


        // =====================================================
        // PEQUENO ATRASO DE SEGURANÇA
        // =====================================================

        private static async Task
            AplicarPequenoAtrasoDeSeguranca()
        {
            /*
             * Dificulta tentativas automatizadas muito rápidas
             * e reduz diferenças perceptíveis entre:
             *
             * - email inexistente
             * - senha incorreta
             *
             * Isso NÃO substitui o rate limiting,
             * que adicionaremos no Program.cs.
             */

            await Task.Delay(350);
        }
    }
}