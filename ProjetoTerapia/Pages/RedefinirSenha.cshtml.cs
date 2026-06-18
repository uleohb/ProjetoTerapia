using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;
using ProjetoTerapia.Services;
using System.Security.Cryptography;


namespace ProjetoTerapia.Pages
{
    public class RedefinirSenhaModel : PageModel
    {

        private readonly AppDbContext _context;


        public RedefinirSenhaModel(AppDbContext context)
        {
            _context = context;
        }



        [BindProperty]
        public string Token { get; set; } = "";


        [BindProperty]
        public string NovaSenha { get; set; } = "";


        [BindProperty]
        public string ConfirmarSenha { get; set; } = "";



        public string Mensagem { get; set; } = "";




        public IActionResult OnGet(string token)
        {

            Token = token;


            return Page();

        }



        public async Task<IActionResult> OnPost()
        {


            if (NovaSenha != ConfirmarSenha)
            {
                Mensagem = "As senhas não coincidem.";
                return Page();
            }



            var recuperacao =
            _context.RecuperacoesSenha
            .FirstOrDefault(x =>
                x.Token == Token);



            if (recuperacao == null ||
               recuperacao.Usado ||
               recuperacao.Expiracao < DateTime.Now)
            {

                Mensagem = "Token inválido ou expirado.";

                return Page();

            }



            var clinica =
            _context.Clinicas
            .First(x =>
            x.Id == recuperacao.ClinicaId);



            var salt =
            RandomNumberGenerator.GetBytes(16);



            var hash =
            Convert.ToBase64String(

            KeyDerivation.Pbkdf2(
                password: NovaSenha,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 32
            ));



            clinica.SenhaHash =
            Convert.ToBase64String(salt)
            + "." + hash;



            recuperacao.Usado = true;



            await _context.SaveChangesAsync();



            Mensagem =
            "Senha alterada com sucesso!";


            return Page();

        }



    }
}