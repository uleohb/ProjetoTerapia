using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using ProjetoTerapia.Models;
using System.Security.Claims;
using System.Security.Cryptography;

namespace ProjetoTerapia.Pages
{
    [EnableRateLimiting("admin-login")]
    public class LoginClinicaModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ILogger<LoginClinicaModel> _logger;


        // =====================================================
        // CHAVES TEMPORÁRIAS DO LOGIN GOOGLE
        // =====================================================

        private const string GoogleEmailPendente =
            "ClinicaGoogleEmailPendente";

        private const string GoogleNomePendente =
            "ClinicaGoogleNomePendente";


        // =====================================================
        // CAMPOS DA TELA
        // =====================================================

        [BindProperty]
        public string Email { get; set; } = "";


        [BindProperty]
        public string Senha { get; set; } = "";


        public string Erro { get; set; } = "";


        /*
         * Quando true, significa que o usuário já escolheu
         * a conta Google e voltou para o AlinhaVIDA.
         *
         * Nesse momento exibimos o carregamento.
         */
        public bool ProcessandoGoogle { get; private set; }


        // =====================================================
        // CONSTRUTOR
        // =====================================================

        public LoginClinicaModel(
            AppDbContext context,
            ILogger<LoginClinicaModel> logger)
        {
            _context = context;
            _logger = logger;
        }


        // =====================================================
        // ABRIR LOGIN
        // =====================================================

        public void OnGet()
        {
            ProcessandoGoogle = false;

            if (TempData["ErroLoginClinica"] is string erro)
            {
                Erro = erro;
            }
        }


        // =====================================================
        // LOGIN NORMAL
        // =====================================================

        public async Task<IActionResult> OnPostAsync()
        {
            ProcessandoGoogle = false;

            var emailNormalizado =
                NormalizarEmail(Email);


            if (string.IsNullOrWhiteSpace(
                emailNormalizado))
            {
                Erro =
                    "Informe seu email.";

                return Page();
            }


            if (string.IsNullOrWhiteSpace(
                Senha))
            {
                Erro =
                    "Informe sua senha.";

                return Page();
            }


            try
            {
                var clinica =
                    await BuscarClinicaPorEmailAsync(
                        emailNormalizado,
                        HttpContext.RequestAborted
                    );


                if (clinica == null)
                {
                    Erro =
                        "Email ou senha inválidos.";

                    return Page();
                }


                /*
                 * Conta criada exclusivamente pelo Google.
                 */
                if (string.IsNullOrWhiteSpace(
                    clinica.SenhaHash))
                {
                    Erro =
                        "Essa conta utiliza acesso pelo Google. Use o botão \"Continuar com Google\".";

                    return Page();
                }


                if (!ValidarSenha(
                    clinica,
                    Senha))
                {
                    Erro =
                        "Email ou senha inválidos.";

                    return Page();
                }


                LogarClinica(
                    clinica
                );


                return RedirectToPage(
                    "/PainelClinica"
                );
            }
            catch (OperationCanceledException)
            {
                Erro =
                    "A solicitação demorou mais que o esperado. Tente novamente.";

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro durante o login normal da clínica."
                );


                Erro =
                    "Não foi possível entrar agora. Tente novamente em alguns instantes.";

                return Page();
            }
        }


        // =====================================================
        // INICIAR LOGIN GOOGLE
        // =====================================================

        public async Task<IActionResult> OnGetLoginGoogleAsync()
        {
            /*
             * Limpamos qualquer autenticação externa anterior.
             *
             * IMPORTANTE:
             * não acessamos o banco aqui.
             *
             * Assim o botão Google abre o Google imediatamente,
             * sem mostrar o carregamento antes da escolha
             * da conta.
             */

            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults
                    .AuthenticationScheme
            );


            LimparLoginGooglePendente();


            var properties =
                new AuthenticationProperties
                {
                    RedirectUri =
                        Url.Page(
                            "/LoginClinica",
                            pageHandler:
                                "GoogleResponse"
                        )
                };


            return Challenge(
                properties,
                GoogleDefaults.AuthenticationScheme
            );
        }


        // =====================================================
        // GOOGLE RETORNOU AO ALINHAVIDA
        // =====================================================

        public async Task<IActionResult> OnGetGoogleResponseAsync()
        {
            /*
             * Neste ponto o usuário JÁ selecionou
             * uma conta no Google.
             */

            var result =
                await HttpContext.AuthenticateAsync(
                    CookieAuthenticationDefaults
                        .AuthenticationScheme
                );


            if (!result.Succeeded ||
                result.Principal == null)
            {
                TempData["ErroLoginClinica"] =
                    "Não foi possível concluir o acesso pelo Google.";

                return RedirectToPage(
                    "/LoginClinica"
                );
            }


            var email =
                result.Principal
                    .FindFirst(
                        ClaimTypes.Email
                    )
                    ?.Value;


            var nome =
                result.Principal
                    .FindFirst(
                        ClaimTypes.Name
                    )
                    ?.Value
                ?? "";


            var emailNormalizado =
                NormalizarEmail(
                    email
                );


            if (string.IsNullOrWhiteSpace(
                emailNormalizado))
            {
                await HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults
                        .AuthenticationScheme
                );


                TempData["ErroLoginClinica"] =
                    "Sua conta Google não forneceu um email válido.";

                return RedirectToPage(
                    "/LoginClinica"
                );
            }


            /*
             * Guardamos somente os dados necessários
             * temporariamente na sessão.
             *
             * A consulta ao banco acontecerá somente
             * depois que a tela de carregamento aparecer.
             */

            HttpContext.Session.SetString(
                GoogleEmailPendente,
                emailNormalizado
            );


            HttpContext.Session.SetString(
                GoogleNomePendente,
                nome
            );


            /*
             * Não precisamos manter o cookie de autenticação
             * externa depois que capturamos os dados.
             */

            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults
                    .AuthenticationScheme
            );


            /*
             * Vai para uma etapa intermediária.
             *
             * Essa etapa mostra:
             *
             * "Entrando na sua conta..."
             *
             * ANTES de começar a consulta pesada ao banco.
             */

            return RedirectToPage(
                "/LoginClinica",
                "ProcessandoGoogle"
            );
        }


        // =====================================================
        // MOSTRAR CARREGAMENTO DEPOIS DO GOOGLE
        // =====================================================

        public IActionResult OnGetProcessandoGoogle()
        {
            var email =
                HttpContext.Session.GetString(
                    GoogleEmailPendente
                );


            if (string.IsNullOrWhiteSpace(
                email))
            {
                TempData["ErroLoginClinica"] =
                    "A autenticação pelo Google expirou. Tente novamente.";

                return RedirectToPage(
                    "/LoginClinica"
                );
            }


            ProcessandoGoogle = true;


            return Page();
        }


        // =====================================================
        // FINALIZAR LOGIN GOOGLE
        // =====================================================

        public async Task<IActionResult> OnPostFinalizarGoogleAsync()
        {
            var email =
                HttpContext.Session.GetString(
                    GoogleEmailPendente
                );


            var nome =
                HttpContext.Session.GetString(
                    GoogleNomePendente
                )
                ?? "";


            if (string.IsNullOrWhiteSpace(
                email))
            {
                LimparLoginGooglePendente();


                TempData["ErroLoginClinica"] =
                    "A autenticação pelo Google expirou. Tente novamente.";

                return RedirectToPage(
                    "/LoginClinica"
                );
            }


            var emailNormalizado =
                NormalizarEmail(
                    email
                );


            try
            {
                /*
                 * Agora sim consultamos o Azure SQL.
                 *
                 * Enquanto isso acontece o usuário já está
                 * vendo a tela "Entrando na sua conta...".
                 */

                var clinica =
                    await BuscarClinicaPorEmailAsync(
                        emailNormalizado,
                        HttpContext.RequestAborted
                    );


                // =================================================
                // PRIMEIRO ACESSO GOOGLE
                // =================================================

                if (clinica == null)
                {
                    var nomeClinica =
                        string.IsNullOrWhiteSpace(nome)
                            ? emailNormalizado.Split('@')[0]
                            : nome.Trim();


                    clinica =
                        new Clinica
                        {
                            Nome =
                                nomeClinica,

                            Email =
                                emailNormalizado,

                            SenhaHash =
                                "",

                            Pago =
                                false,

                            Aprovado =
                                false,

                            PerfilCompleto =
                                false,

                            ClinicaAlteracaoPendente =
                                false
                        };


                    _context.Clinicas.Add(
                        clinica
                    );


                    await _context.SaveChangesAsync(
                        HttpContext.RequestAborted
                    );
                }


                /*
                 * Limpa sessões antigas e estabelece
                 * a sessão profissional.
                 */

                LogarClinica(
                    clinica
                );


                return RedirectToPage(
                    "/PainelClinica"
                );
            }
            catch (OperationCanceledException)
            {
                LimparLoginGooglePendente();


                TempData["ErroLoginClinica"] =
                    "O acesso demorou mais que o esperado. Tente novamente.";

                return RedirectToPage(
                    "/LoginClinica"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao finalizar login Google da clínica."
                );


                LimparLoginGooglePendente();


                TempData["ErroLoginClinica"] =
                    "Não foi possível concluir seu acesso pelo Google. Tente novamente.";

                return RedirectToPage(
                    "/LoginClinica"
                );
            }
        }


        // =====================================================
        // LOGOUT
        // =====================================================

        public async Task<IActionResult> OnGetLogoutAsync()
        {
            HttpContext.Session.Clear();


            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults
                    .AuthenticationScheme
            );


            return RedirectToPage(
                "/LoginClinica"
            );
        }


        // =====================================================
        // BUSCAR CLÍNICA PELO EMAIL
        // =====================================================

        private async Task<Clinica?> BuscarClinicaPorEmailAsync(
            string email,
            CancellationToken cancellationToken)
        {
            var emailNormalizado =
                NormalizarEmail(
                    email
                );


            return await _context.Clinicas

                /*
                 * Estamos somente lendo esse registro.
                 */
                .AsNoTracking()

                .Where(
                    c =>
                        c.Email != null
                        &&
                        c.Email
                            .Trim()
                            .ToLower()
                        ==
                        emailNormalizado
                )

                /*
                 * Caso existam registros antigos duplicados,
                 * priorizamos o perfil mais completo.
                 */
                .OrderByDescending(
                    c => c.PerfilCompleto
                )

                .ThenByDescending(
                    c => c.Aprovado
                )

                .ThenByDescending(
                    c => c.Pago
                )

                .ThenByDescending(
                    c => c.Id
                )

                .FirstOrDefaultAsync(
                    cancellationToken
                );
        }


        // =====================================================
        // VALIDAR SENHA
        // =====================================================

        private bool ValidarSenha(
            Clinica clinica,
            string senha)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(
                    clinica.SenhaHash))
                {
                    return false;
                }


                var partes =
                    clinica.SenhaHash
                        .Split('.');


                if (partes.Length != 2)
                {
                    return false;
                }


                var salt =
                    Convert.FromBase64String(
                        partes[0]
                    );


                var hashEsperado =
                    Convert.FromBase64String(
                        partes[1]
                    );


                var hashCalculado =
                    KeyDerivation.Pbkdf2(
                        password:
                            senha,

                        salt:
                            salt,

                        prf:
                            KeyDerivationPrf
                                .HMACSHA256,

                        iterationCount:
                            100000,

                        numBytesRequested:
                            hashEsperado.Length
                    );


                /*
                 * Comparação em tempo constante.
                 *
                 * Melhor que comparar strings diretamente.
                 */
                return CryptographicOperations
                    .FixedTimeEquals(
                        hashCalculado,
                        hashEsperado
                    );
            }
            catch
            {
                /*
                 * Se existir algum hash antigo/corrompido,
                 * não deixamos a aplicação gerar erro 500.
                 */

                return false;
            }
        }


        // =====================================================
        // ESTABELECER SESSÃO DA CLÍNICA
        // =====================================================

        private void LogarClinica(
            Clinica clinica)
        {
            /*
             * Evita manter sessões de outros perfis
             * misturadas no mesmo navegador.
             */

            HttpContext.Session.Clear();


            HttpContext.Session.SetString(
                "ClinicaLogada",
                clinica.Id.ToString()
            );
        }


        // =====================================================
        // LIMPAR LOGIN GOOGLE PENDENTE
        // =====================================================

        private void LimparLoginGooglePendente()
        {
            HttpContext.Session.Remove(
                GoogleEmailPendente
            );


            HttpContext.Session.Remove(
                GoogleNomePendente
            );
        }


        // =====================================================
        // NORMALIZAR EMAIL
        // =====================================================

        private static string NormalizarEmail(
            string? email)
        {
            return (email ?? "")
                .Trim()
                .ToLowerInvariant();
        }
    }
}