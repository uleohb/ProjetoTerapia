using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;
using ProjetoTerapia.Services;
using System.Security.Cryptography;

namespace ProjetoTerapia.Pages;


public class RecuperarSenhaModel : PageModel
{

    private readonly AppDbContext _context;
    private readonly EmailService _email;


    public RecuperarSenhaModel(
        AppDbContext context,
        EmailService email)
    {
        _context = context;
        _email = email;
    }



    [BindProperty]
    public string? Email { get; set; }


    public string? Mensagem { get; set; }




    public async Task<IActionResult> OnPost()
    {
        var clinica =
            _context.Clinicas
            .FirstOrDefault(x => x.Email == Email);


        if (clinica == null)
        {
            Mensagem =
            "Se esse email existir, você receberá um link.";

            return Page();
        }


        var token =
            Convert.ToHexString(
                RandomNumberGenerator.GetBytes(64)
            );


        var recuperacao =
            new RecuperacaoSenha
            {
                ClinicaId = clinica.Id,
                Token = token,
                Expiracao = DateTime.Now.AddMinutes(30),
                Usado = false
            };


        _context.RecuperacoesSenha.Add(recuperacao);

        await _context.SaveChangesAsync();



        var link =
        $"https://localhost:7081/RedefinirSenha?token={token}";


        await _email.EnviarEmail(

            clinica.Email,

            "Recuperação de senha - AlinhaMente",

            $@"
             Olá.

             Recebemos uma solicitação para redefinir sua senha.

             Clique no link abaixo:

             {link}


             Esse link expira em 30 minutos.

             Caso não tenha solicitado, ignore esse email.

             Equipe AlinhaMente
             "
        );



        Mensagem =
        "Se esse email existir, você receberá um link para redefinir sua senha.";


        return Page();
    }


}