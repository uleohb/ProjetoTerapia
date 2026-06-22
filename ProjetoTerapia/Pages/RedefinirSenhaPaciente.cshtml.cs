using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;
using System.Security.Cryptography;

namespace ProjetoTerapia.Pages
{
    public class RedefinirSenhaPacienteModel : PageModel
    {
        private readonly AppDbContext _context;

        public RedefinirSenhaPacienteModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string Token { get; set; } = "";

        [BindProperty]
        public string NovaSenha { get; set; } = "";

        [BindProperty]
        public string ConfirmarSenha { get; set; } = "";

        public string Mensagem { get; set; } = "";

        public bool TokenValido { get; set; } = true;

        public bool SenhaAlterada { get; set; } = false;

        public IActionResult OnGet()
        {
            if (string.IsNullOrEmpty(Token))
            {
                TokenValido = false;
                Mensagem = "Link inválido.";
                return Page();
            }

            var recuperacao = _context.RecuperacoesSenhaPacientes
                .FirstOrDefault(x =>
                    x.Token == Token &&
                    x.Usado == false &&
                    x.Expiracao > DateTime.Now);

            if (recuperacao == null)
            {
                TokenValido = false;
                Mensagem = "Este link é inválido ou expirou.";
                return Page();
            }

            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            if (NovaSenha != ConfirmarSenha)
            {
                TokenValido = true;
                Mensagem = "As senhas não coincidem.";
                return Page();
            }

            if (NovaSenha.Length < 6)
            {
                TokenValido = true;
                Mensagem = "A senha precisa ter pelo menos 6 caracteres.";
                return Page();
            }

            var recuperacao = _context.RecuperacoesSenhaPacientes
                .FirstOrDefault(x =>
                    x.Token == Token &&
                    x.Usado == false &&
                    x.Expiracao > DateTime.Now);

            if (recuperacao == null)
            {
                TokenValido = false;
                Mensagem = "Este link é inválido ou expirou.";
                return Page();
            }

            var paciente = _context.Pacientes
                .FirstOrDefault(x => x.Id == recuperacao.PacienteId);

            if (paciente == null)
            {
                TokenValido = false;
                Mensagem = "Paciente não encontrado.";
                return Page();
            }

            paciente.SenhaHash = GerarHashSenha(NovaSenha);
            recuperacao.Usado = true;

            await _context.SaveChangesAsync();

            SenhaAlterada = true;
            TokenValido = false;
            Mensagem = "Senha alterada com sucesso. Agora você já pode entrar com email e senha.";

            return Page();
        }

        private string GerarHashSenha(string senha)
        {
            var salt = RandomNumberGenerator.GetBytes(16);

            var hash = Convert.ToBase64String(
                KeyDerivation.Pbkdf2(
                    password: senha,
                    salt: salt,
                    prf: KeyDerivationPrf.HMACSHA256,
                    iterationCount: 100000,
                    numBytesRequested: 32
                )
            );

            return Convert.ToBase64String(salt) + "." + hash;
        }
    }
}