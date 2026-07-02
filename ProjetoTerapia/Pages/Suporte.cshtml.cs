using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace ProjetoTerapia.Pages
{
    public class SuporteModel : PageModel
    {
        private readonly IConfiguration _config;

        public SuporteModel(IConfiguration config)
        {
            _config = config;
        }

        [BindProperty]
        public string Nome { get; set; } = "";

        [BindProperty]
        public string Email { get; set; } = "";

        [BindProperty]
        public string TipoUsuario { get; set; } = "";

        [BindProperty]
        public string Assunto { get; set; } = "";

        [BindProperty]
        public string Mensagem { get; set; } = "";

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            if (string.IsNullOrWhiteSpace(Nome) ||
                string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Assunto) ||
                string.IsNullOrWhiteSpace(Mensagem))
            {
                TempData["Erro"] = "Preencha todos os campos obrigatórios.";
                return Page();
            }

            try
            {
                var host = _config["Email:Servidor"];
                var port = int.Parse(_config["Email:Porta"] ?? "587");
                var usuario = _config["Email:Usuario"];
                var senha = _config["Email:Senha"];
                var destino = _config["Email:Destino"];

                if (string.IsNullOrWhiteSpace(host) ||
                    string.IsNullOrWhiteSpace(usuario) ||
                    string.IsNullOrWhiteSpace(senha) ||
                    string.IsNullOrWhiteSpace(destino))
                {
                    TempData["Erro"] = "Configuração de email incompleta.";
                    return Page();
                }

                var corpo = new StringBuilder();

                corpo.AppendLine("<h2>Novo chamado de suporte - AlinhaMente</h2>");
                corpo.AppendLine("<hr />");
                corpo.AppendLine($"<p><strong>Nome:</strong> {Nome}</p>");
                corpo.AppendLine($"<p><strong>Email:</strong> {Email}</p>");
                corpo.AppendLine($"<p><strong>Tipo de usuário:</strong> {TipoUsuario}</p>");
                corpo.AppendLine($"<p><strong>Assunto:</strong> {Assunto}</p>");
                corpo.AppendLine("<hr />");
                corpo.AppendLine("<p><strong>Mensagem:</strong></p>");
                corpo.AppendLine($"<p>{Mensagem.Replace("\n", "<br />")}</p>");

                var email = new MailMessage();

                email.From = new MailAddress(usuario, "Suporte AlinhaMente");
                email.To.Add(destino);
                email.ReplyToList.Add(new MailAddress(Email, Nome));
                email.Subject = $"Suporte AlinhaMente - {Assunto}";
                email.Body = corpo.ToString();
                email.IsBodyHtml = true;

                using var smtp = new SmtpClient(host, port);

                smtp.Credentials = new NetworkCredential(usuario, senha);
                smtp.EnableSsl = true;

                smtp.Send(email);

                TempData["Sucesso"] = "Mensagem enviada com sucesso. O suporte entrará em contato em breve.";

                return RedirectToPage("/Suporte");
            }
            catch
            {
                TempData["Erro"] = "Não foi possível enviar sua mensagem agora. Verifique a configuração do email.";
                return Page();
            }
        }
    }
}