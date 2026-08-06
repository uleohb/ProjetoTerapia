using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

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

        [BindProperty]
        public string CodigoVendedor { get; set; } = "";

        public string NomeVendedorAtual { get; set; } = "";

        public IActionResult OnGet(string? vendedor)
        {
            CapturarCodigoVendedor(vendedor);
            CarregarVendedorAtual();

            return Page();
        }

        public IActionResult OnPost()
        {
            CapturarCodigoVendedor(CodigoVendedor);

            if (Senha != ConfirmarSenha)
            {
                ModelState.AddModelError("", "As senhas não coincidem.");
                CarregarVendedorAtual();
                return Page();
            }

            var emailNormalizado = Email.Trim().ToLower();

            if (_context.Clinicas.Any(c => c.Email.ToLower() == emailNormalizado))
            {
                ModelState.AddModelError("", "Email já cadastrado.");
                CarregarVendedorAtual();
                return Page();
            }

            var salt = RandomNumberGenerator.GetBytes(128 / 8);

            var hash = Convert.ToBase64String(
                KeyDerivation.Pbkdf2(
                    password: Senha,
                    salt: salt,
                    prf: KeyDerivationPrf.HMACSHA256,
                    iterationCount: 100000,
                    numBytesRequested: 256 / 8
                )
            );

            var vendedorIndicacao = BuscarVendedorDaSessao();

            var clinica = new Clinica
            {
                Email = emailNormalizado,
                SenhaHash = Convert.ToBase64String(salt) + "." + hash,
                Aprovado = false,
                Pago = false,
                PerfilCompleto = false,
                ClinicaAlteracaoPendente = false,

                VendedorId = vendedorIndicacao?.Id,
                CodigoVendedorIndicacao = vendedorIndicacao?.CodigoIndicacao
            };

            _context.Clinicas.Add(clinica);
            _context.SaveChanges();

            HttpContext.Session.SetString(
                "ClinicaLogada",
                clinica.Id.ToString()
            );

            return RedirectToPage("/PainelClinica");
        }

        public IActionResult OnGetLoginGoogle()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = "/"
            };

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        private void CapturarCodigoVendedor(string? codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                return;
            }

            var codigoNormalizado = codigo.Trim().ToUpper();

            var vendedor = _context.Vendedores
                .FirstOrDefault(v =>
                    v.CodigoIndicacao == codigoNormalizado &&
                    v.Ativo
                );

            if (vendedor == null)
            {
                return;
            }

            HttpContext.Session.SetString(
                "CodigoVendedorIndicacao",
                vendedor.CodigoIndicacao
            );

            CodigoVendedor = vendedor.CodigoIndicacao;
            NomeVendedorAtual = vendedor.Nome;
        }

        private Vendedor? BuscarVendedorDaSessao()
        {
            var codigo = HttpContext.Session.GetString("CodigoVendedorIndicacao");

            if (string.IsNullOrWhiteSpace(codigo))
            {
                return null;
            }

            return _context.Vendedores
                .FirstOrDefault(v =>
                    v.CodigoIndicacao == codigo &&
                    v.Ativo
                );
        }

        private void CarregarVendedorAtual()
        {
            var vendedor = BuscarVendedorDaSessao();

            if (vendedor == null)
            {
                CodigoVendedor = "";
                NomeVendedorAtual = "";
                return;
            }

            CodigoVendedor = vendedor.CodigoIndicacao;
            NomeVendedorAtual = vendedor.Nome;
        }
    }
}