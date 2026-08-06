using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjetoTerapia.Models;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjetoTerapia.Controllers
{
    [ApiController]
    [Route("webhooks/mercadopago")]
    public class WebhookController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public WebhookController(
            AppDbContext context,
            IHttpClientFactory httpClientFactory,
            IConfiguration config)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        [HttpPost]
        [HttpPost("/webhook")]
        public async Task<IActionResult> Receber()
        {
            var paymentId = await ObterPaymentId();

            if (string.IsNullOrWhiteSpace(paymentId))
            {
                return Ok();
            }

            var pagamento = await ConsultarPagamentoMercadoPago(paymentId);

            if (pagamento == null)
            {
                return Ok();
            }

            if (string.IsNullOrWhiteSpace(pagamento.ExternalReference))
            {
                return Ok();
            }

            if (pagamento.ExternalReference.StartsWith("DIVULGACAO-"))
            {
                await ProcessarPagamentoDivulgacao(pagamento);
            }

            return Ok();
        }

        private async Task ProcessarPagamentoDivulgacao(MercadoPagoPaymentResponse pagamento)
        {
            var idTexto = pagamento.ExternalReference!
                .Replace("DIVULGACAO-", "");

            if (!int.TryParse(idTexto, out int divulgacaoId))
            {
                return;
            }

            var divulgacao = await _context.DivulgacoesRegionais
                .FirstOrDefaultAsync(d => d.Id == divulgacaoId);

            if (divulgacao == null)
            {
                return;
            }

            divulgacao.MercadoPagoPaymentId = pagamento.Id.ToString();
            divulgacao.MercadoPagoStatus = pagamento.Status ?? "";

            if (pagamento.Status == "approved")
            {
                var divulgacoesAntigas = await _context.DivulgacoesRegionais
                    .Where(d =>
                        d.ClinicaId == divulgacao.ClinicaId &&
                        d.Id != divulgacao.Id &&
                        d.Ativo)
                    .ToListAsync();

                foreach (var antiga in divulgacoesAntigas)
                {
                    antiga.Ativo = false;
                    antiga.Status = "Substituído";
                }

                divulgacao.Pago = true;
                divulgacao.Aprovado = true;
                divulgacao.Ativo = true;
                divulgacao.DataPagamento = DateTime.Now;
                divulgacao.DataAprovacao = DateTime.Now;
                divulgacao.DataInicio = DateTime.Now;
                divulgacao.DataFim = DateTime.Now.AddMonths(1);
                divulgacao.Status = "Ativo";
            }
            else if (pagamento.Status == "rejected" ||
                     pagamento.Status == "cancelled")
            {
                divulgacao.Pago = false;
                divulgacao.Aprovado = false;
                divulgacao.Ativo = false;
                divulgacao.Status = "Pagamento recusado";
            }
            else if (pagamento.Status == "refunded" ||
                     pagamento.Status == "charged_back")
            {
                divulgacao.Pago = false;
                divulgacao.Aprovado = false;
                divulgacao.Ativo = false;
                divulgacao.Status = "Pagamento estornado";
            }
            else
            {
                divulgacao.Pago = false;
                divulgacao.Aprovado = false;
                divulgacao.Ativo = false;
                divulgacao.Status = "Aguardando pagamento";
            }

            await _context.SaveChangesAsync();
        }

        private async Task<string?> ObterPaymentId()
        {
            var idQuery =
                Request.Query["data.id"].FirstOrDefault() ??
                Request.Query["id"].FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(idQuery))
            {
                return idQuery;
            }

            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            Console.WriteLine("Webhook Mercado Pago recebido:");
            Console.WriteLine(body);

            if (string.IsNullOrWhiteSpace(body))
            {
                return null;
            }

            try
            {
                using var json = JsonDocument.Parse(body);
                var root = json.RootElement;

                if (root.TryGetProperty("data", out var data) &&
                    data.TryGetProperty("id", out var dataId))
                {
                    return dataId.ToString();
                }

                if (root.TryGetProperty("id", out var id))
                {
                    return id.ToString();
                }

                if (root.TryGetProperty("resource", out var resource))
                {
                    var resourceText = resource.GetString();

                    if (!string.IsNullOrWhiteSpace(resourceText))
                    {
                        return resourceText
                            .Split("/")
                            .LastOrDefault();
                    }
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        private async Task<MercadoPagoPaymentResponse?> ConsultarPagamentoMercadoPago(string paymentId)
        {
            var accessToken = _config["MercadoPago:AccessToken"];

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return null;
            }

            var client = _httpClientFactory.CreateClient();

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://api.mercadopago.com/v1/payments/{paymentId}"
            );

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<MercadoPagoPaymentResponse>(json);
        }

        private class MercadoPagoPaymentResponse
        {
            [JsonPropertyName("id")]
            public long Id { get; set; }

            [JsonPropertyName("status")]
            public string? Status { get; set; }

            [JsonPropertyName("external_reference")]
            public string? ExternalReference { get; set; }
        }
    }
}