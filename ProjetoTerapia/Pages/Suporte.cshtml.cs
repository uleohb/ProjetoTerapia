using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;

namespace ProjetoTerapia.Pages
{
    public class SuporteModel : PageModel
    {
        private readonly IConfiguration _config;

        public SuporteModel(IConfiguration config)
        {
            _config = config;
        }

        public string LinkWhatsappSuporte { get; set; } = "";

        public void OnGet()
        {
            var numeroWhatsapp = _config["Suporte:WhatsappNumero"];

            if (string.IsNullOrWhiteSpace(numeroWhatsapp))
            {
                TempData["Erro"] = "Número de WhatsApp do suporte não configurado.";
                return;
            }

            numeroWhatsapp = ApenasNumeros(numeroWhatsapp);

            if (numeroWhatsapp.Length < 12)
            {
                TempData["Erro"] = "Número de WhatsApp do suporte inválido.";
                return;
            }

            var mensagem =
                @"Olá, suporte AlinhaVida! Vim pelo suporte da plataforma e preciso de ajuda.";

            var mensagemCodificada = WebUtility.UrlEncode(mensagem);

            LinkWhatsappSuporte = $"https://wa.me/{numeroWhatsapp}?text={mensagemCodificada}";
        }

        private string ApenasNumeros(string valor)
        {
            return new string(valor.Where(char.IsDigit).ToArray());
        }
    }
}