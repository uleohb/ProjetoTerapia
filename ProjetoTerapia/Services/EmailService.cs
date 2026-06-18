using System.Net;
using System.Net.Mail;

namespace ProjetoTerapia.Services
{
    public class EmailService
    {

        private readonly IConfiguration _config;


        public EmailService(IConfiguration config)
        {
            _config = config;
        }



        public async Task EnviarEmail(
            string destino,
            string assunto,
            string mensagem)
        {


            var email = new MailMessage();


            email.From = new MailAddress(
                _config["Email:Usuario"]!
            );


            email.To.Add(destino);


            email.Subject = assunto;


            email.Body = mensagem;


            using var smtp = new SmtpClient();


            smtp.Host =
            _config["Email:Servidor"] ?? "";


            smtp.Port =
            int.Parse(_config["Email:Porta"] ?? "587");


            smtp.EnableSsl = true;


           smtp.Credentials =
           new NetworkCredential(

            _config["Email:Usuario"] ?? "",
            _config["Email:Senha"] ?? ""

           );


            await smtp.SendMailAsync(email);

        }


    }
}