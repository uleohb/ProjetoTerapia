using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjetoTerapia.Pages
{
    public class PagamentoClinicaModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public PagamentoClinicaModel(
            AppDbContext context,
            IHttpClientFactory httpClientFactory,
            IConfiguration config)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        public Clinica? Clinica { get; set; }

        public int? DiasRestantes { get; set; }

        public bool PlanoVencido { get; set; }

        public bool PlanoPertoDeVencer { get; set; }

        public string ValorPlanoFormatado { get; set; } = "R$ 450,00";

        public IActionResult OnGet()
        {
            if (!CarregarClinica())
                return RedirectToPage("/LoginClinica");

            return Page();
        }

        public async Task<IActionResult> OnPostPagarAsync()
        {
            if (!CarregarClinica() || Clinica == null)
                return RedirectToPage("/LoginClinica");

            if (!Clinica.Aprovado)
            {
                TempData["Erro"] =
                    "Seu cadastro precisa ser aprovado antes do pagamento.";

                return RedirectToPage("/PagamentoClinica");
            }

            // Evita comprar novamente enquanto o plano ainda está longe do vencimento.
            if (Clinica.Pago &&
                !PlanoVencido &&
                !PlanoPertoDeVencer)
            {
                TempData["Erro"] = "Seu plano ainda está ativo.";
                return RedirectToPage("/PagamentoClinica");
            }

            try
            {
                var linkPagamento =
                    await CriarCheckoutMercadoPago(Clinica);

                return Redirect(linkPagamento);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                TempData["Erro"] =
                    "Não foi possível gerar o pagamento agora. Tente novamente.";

                return RedirectToPage("/PagamentoClinica");
            }
        }

        private bool CarregarClinica()
        {
            var id = HttpContext.Session.GetString("ClinicaLogada");

            if (string.IsNullOrWhiteSpace(id) ||
                !int.TryParse(id, out int clinicaId))
            {
                return false;
            }

            Clinica = _context.Clinicas
                .FirstOrDefault(c => c.Id == clinicaId);

            if (Clinica == null)
                return false;

            if (Clinica.ValorPlano.HasValue)
            {
                ValorPlanoFormatado =
                    Clinica.ValorPlano.Value
                    .ToString("C", new CultureInfo("pt-BR"));
            }

            if (Clinica.DataVencimento.HasValue)
            {
                DiasRestantes = (int)Math.Ceiling(
                    (Clinica.DataVencimento.Value.Date - DateTime.Today)
                    .TotalDays
                );

                PlanoVencido = DiasRestantes < 0;

                PlanoPertoDeVencer =
                    DiasRestantes <= 30 &&
                    DiasRestantes >= 0;
            }

            return true;
        }

        private async Task<string> CriarCheckoutMercadoPago(
            Clinica clinica)
        {
            var accessToken =
                _config["MercadoPago:AccessToken"];

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new Exception(
                    "Access Token do Mercado Pago não configurado."
                );
            }

            var baseUrl = _config["App:BaseUrl"];

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                baseUrl =
                    $"{Request.Scheme}://{Request.Host}";
            }

            var notificationUrl =
                _config["MercadoPago:NotificationUrl"];

            var valor = clinica.ValorPlano ?? 450m;

            var preference =
                new Dictionary<string, object>
                {
                    ["items"] = new[]
                    {
                        new
                        {
                            id = $"plano-clinica-{clinica.Id}",
                            title = "AlinhaVida - Plano Profissional Anual",
                            description = "Plano profissional anual AlinhaVida",
                            quantity = 1,
                            currency_id = "BRL",
                            unit_price = valor
                        }
                    },

                    ["statement_descriptor"] = "ALINHAVIDA",

                    ["external_reference"] =
                        $"PLANO-CLINICA-{clinica.Id}",

                    ["back_urls"] = new
                    {
                        success =
                            $"{baseUrl}/PagamentoClinica?pagamento=sucesso",

                        pending =
                            $"{baseUrl}/PagamentoClinica?pagamento=pendente",

                        failure =
                            $"{baseUrl}/PagamentoClinica?pagamento=falha"
                    },

                    ["auto_return"] = "approved"
                };

            if (!string.IsNullOrWhiteSpace(notificationUrl))
            {
                preference["notification_url"] =
                    notificationUrl;
            }

            var json = JsonSerializer.Serialize(preference);

            var client =
                _httpClientFactory.CreateClient();

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    "https://api.mercadopago.com/checkout/preferences"
                );

            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    accessToken
                );

            request.Content =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json"
                );

            using var response =
                await client.SendAsync(request);

            var responseJson =
                await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    "Erro Mercado Pago: " + responseJson
                );
            }

            var retorno =
                JsonSerializer.Deserialize<MercadoPagoPreferenceResponse>(
                    responseJson
                );

            var link =
                retorno?.InitPoint ??
                retorno?.SandboxInitPoint;

            if (string.IsNullOrWhiteSpace(link))
            {
                throw new Exception(
                    "Mercado Pago não retornou o link de pagamento."
                );
            }

            return link;
        }

        private class MercadoPagoPreferenceResponse
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("init_point")]
            public string? InitPoint { get; set; }

            [JsonPropertyName("sandbox_init_point")]
            public string? SandboxInitPoint { get; set; }
        }
    }
}