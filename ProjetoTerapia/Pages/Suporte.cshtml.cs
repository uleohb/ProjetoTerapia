using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
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
        public string TemCadastro { get; set; } = "";

        [BindProperty]
        public string CPF { get; set; } = "";

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
            // Validação dos campos obrigatórios principais
            if (string.IsNullOrWhiteSpace(Nome) ||
                string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(TemCadastro) ||
                string.IsNullOrWhiteSpace(TipoUsuario) ||
                string.IsNullOrWhiteSpace(Assunto) ||
                string.IsNullOrWhiteSpace(Mensagem))
            {
                TempData["Erro"] = "Preencha todos os campos obrigatórios.";
                return Page();
            }

            // Se o usuário marcou que já tem cadastro, o CPF passa a ser obrigatório
            if (TemCadastro == "Sim" && string.IsNullOrWhiteSpace(CPF))
            {
                TempData["Erro"] = "Informe o CPF cadastrado para facilitar o atendimento.";
                return Page();
            }

            var numeroWhatsapp = _config["Suporte:WhatsappNumero"];

            if (string.IsNullOrWhiteSpace(numeroWhatsapp))
            {
                TempData["Erro"] = "Número de WhatsApp do suporte não configurado.";
                return Page();
            }

            numeroWhatsapp = ApenasNumeros(numeroWhatsapp);

            if (numeroWhatsapp.Length < 12)
            {
                TempData["Erro"] = "Número de WhatsApp do suporte inválido.";
                return Page();
            }

            var cpfFormatado = "Não informado";

            if (TemCadastro == "Sim" && !string.IsNullOrWhiteSpace(CPF))
            {
                cpfFormatado = CPF.Trim();
            }

            // Monta a mensagem que será enviada para o WhatsApp
            var texto = new StringBuilder();

            texto.AppendLine("Olá, suporte AlinhaMente! Preciso de atendimento.");
            texto.AppendLine();
            texto.AppendLine("Dados do chamado:");
            texto.AppendLine($"Nome: {Nome.Trim()}");
            texto.AppendLine($"Email: {Email.Trim()}");
            texto.AppendLine($"Já tenho cadastro: {TemCadastro.Trim()}");

            if (TemCadastro == "Sim")
            {
                texto.AppendLine($"CPF cadastrado: {cpfFormatado}");
            }

            texto.AppendLine($"Tipo de usuário: {TipoUsuario.Trim()}");
            texto.AppendLine($"Assunto: {Assunto.Trim()}");
            texto.AppendLine();
            texto.AppendLine("Mensagem:");
            texto.AppendLine(Mensagem.Trim());
            texto.AppendLine();
            texto.AppendLine("Enviado pelo formulário de suporte da plataforma AlinhaMente.");

            var mensagemCodificada = WebUtility.UrlEncode(texto.ToString());

            var linkWhatsapp = $"https://wa.me/{numeroWhatsapp}?text={mensagemCodificada}";

            return Redirect(linkWhatsapp);
        }

        private string ApenasNumeros(string valor)
        {
            return new string(valor.Where(char.IsDigit).ToArray());
        }
    }
}