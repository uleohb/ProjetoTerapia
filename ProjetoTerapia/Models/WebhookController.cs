using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjetoTerapia.Models;
using ProjetoTerapia.Services;
using System.Globalization;
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
        private readonly EmailService _emailService;
        private readonly ILogger<WebhookController> _logger;

        public WebhookController(
            AppDbContext context,
            IHttpClientFactory httpClientFactory,
            IConfiguration config,
            EmailService emailService,
            ILogger<WebhookController> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _config = config;
            _emailService = emailService;
            _logger = logger;
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

            var pagamento =
                await ConsultarPagamentoMercadoPago(paymentId);

            if (pagamento == null)
            {
                return Ok();
            }

            if (string.IsNullOrWhiteSpace(
                pagamento.ExternalReference))
            {
                return Ok();
            }

            if (pagamento.ExternalReference
                .StartsWith("DIVULGACAO-"))
            {
                await ProcessarPagamentoDivulgacao(
                    pagamento
                );
            }
            else if (pagamento.ExternalReference
                .StartsWith("PLANO-CLINICA-"))
            {
                await ProcessarPagamentoPlanoClinica(
                    pagamento
                );
            }

            return Ok();
        }

        // =====================================================
        // PAGAMENTO DO PLANO PRINCIPAL
        // =====================================================

        private async Task ProcessarPagamentoPlanoClinica(
            MercadoPagoPaymentResponse pagamento)
        {
            var idTexto =
                pagamento.ExternalReference!
                    .Replace(
                        "PLANO-CLINICA-",
                        ""
                    );

            if (!int.TryParse(
                idTexto,
                out int clinicaId))
            {
                return;
            }

            var clinica =
                await _context.Clinicas
                    .FirstOrDefaultAsync(
                        c => c.Id == clinicaId
                    );

            if (clinica == null)
            {
                return;
            }

            var paymentId =
                pagamento.Id.ToString();

            // Evita processar duas vezes
            // o mesmo pagamento aprovado.
            if (clinica.MercadoPagoPlanoPaymentId
                    == paymentId &&
                clinica.MercadoPagoPlanoStatus
                    == "approved")
            {
                return;
            }

            clinica.MercadoPagoPlanoStatus =
                pagamento.Status ?? "";

            var pagamentoAprovadoAgora = false;

            if (pagamento.Status == "approved")
            {
                pagamentoAprovadoAgora = true;

                clinica.MercadoPagoPlanoPaymentId =
                    paymentId;

                clinica.Pago = true;

                clinica.NomePlano =
                    "Plano Profissional Anual";

                clinica.ValorPlano =
                    pagamento.TransactionAmount > 0
                        ? pagamento.TransactionAmount
                        : 450m;

                clinica.DataPagamento =
                    DateTime.Now;

                // Se estiver renovando antes do
                // vencimento, acrescenta um ano.
                if (clinica.DataVencimento.HasValue &&
                    clinica.DataVencimento.Value.Date
                        >= DateTime.Today)
                {
                    clinica.DataVencimento =
                        clinica.DataVencimento
                            .Value
                            .AddYears(1);
                }
                else
                {
                    clinica.DataVencimento =
                        DateTime.Now.AddYears(1);
                }
            }

            /*
             * Primeiro salva o pagamento.
             *
             * Assim, mesmo que o servidor de e-mail
             * apresente algum problema, o pagamento
             * continua confirmado normalmente.
             */
            await _context.SaveChangesAsync();

            // =================================================
            // NOTIFICAÇÃO POR E-MAIL
            // =================================================

            if (pagamentoAprovadoAgora)
            {
                await EnviarNotificacaoPagamentoPlano(
                    clinica,
                    pagamento
                );
            }
        }

        // =====================================================
        // E-MAIL PARA ADMINISTRAÇÃO DO ALINHAVIDA
        // =====================================================

        private async Task EnviarNotificacaoPagamentoPlano(
            Clinica clinica,
            MercadoPagoPaymentResponse pagamento)
        {
            try
            {
                /*
                 * Preferência:
                 *
                 * Email:NotificacaoPagamentos
                 *
                 * Caso não exista, utiliza
                 * automaticamente Email:Usuario.
                 */

                var emailDestino =
                    _config[
                        "Email:NotificacaoPagamentos"
                    ];

                if (string.IsNullOrWhiteSpace(
                    emailDestino))
                {
                    emailDestino =
                        _config["Email:Usuario"];
                }

                if (string.IsNullOrWhiteSpace(
                    emailDestino))
                {
                    _logger.LogWarning(
                        "Pagamento da clínica {ClinicaId} " +
                        "foi confirmado, mas não existe " +
                        "e-mail administrativo configurado.",
                        clinica.Id
                    );

                    return;
                }

                var cultura =
                    new CultureInfo("pt-BR");

                var valor =
                    (clinica.ValorPlano ?? 450m)
                    .ToString(
                        "C",
                        cultura
                    );

                var dataPagamento =
                    clinica.DataPagamento.HasValue
                        ? clinica.DataPagamento.Value
                            .ToString(
                                "dd/MM/yyyy HH:mm"
                            )
                        : DateTime.Now
                            .ToString(
                                "dd/MM/yyyy HH:mm"
                            );

                var vencimento =
                    clinica.DataVencimento.HasValue
                        ? clinica.DataVencimento.Value
                            .ToString(
                                "dd/MM/yyyy"
                            )
                        : "Não informado";

                var assunto =
                    $"Pagamento confirmado - {clinica.Nome}";

                var mensagem =
$@"NOVO PAGAMENTO CONFIRMADO - ALINHAVIDA

Um pagamento de plano profissional foi confirmado pelo Mercado Pago.

Profissional:
{clinica.Nome}

E-mail:
{clinica.Email}

Plano:
Plano Profissional Anual

Valor:
{valor}

Data do pagamento:
{dataPagamento}

Vencimento do plano:
{vencimento}

Status:
Aprovado

ID Mercado Pago:
{pagamento.Id}

ID da clínica no AlinhaVIDA:
{clinica.Id}

O plano foi ativado automaticamente pelo sistema.

AlinhaVIDA
www.alinhavida.com.br";

                await _emailService.EnviarEmail(
                    emailDestino,
                    assunto,
                    mensagem
                );

                _logger.LogInformation(
                    "E-mail de pagamento enviado para " +
                    "a administração. Clínica {ClinicaId}, " +
                    "pagamento {PaymentId}.",
                    clinica.Id,
                    pagamento.Id
                );
            }
            catch (Exception ex)
            {
                /*
                 * IMPORTANTE:
                 *
                 * erro no e-mail NÃO desfaz
                 * pagamento nem plano.
                 */

                _logger.LogError(
                    ex,
                    "Pagamento confirmado para a clínica " +
                    "{ClinicaId}, mas houve erro ao enviar " +
                    "a notificação por e-mail.",
                    clinica.Id
                );
            }
        }

        // =====================================================
        // PAGAMENTO DA DIVULGAÇÃO REGIONAL
        // =====================================================

        private async Task ProcessarPagamentoDivulgacao(
            MercadoPagoPaymentResponse pagamento)
        {
            var idTexto =
                pagamento.ExternalReference!
                    .Replace(
                        "DIVULGACAO-",
                        ""
                    );

            if (!int.TryParse(
                idTexto,
                out int divulgacaoId))
            {
                return;
            }

            var divulgacao =
                await _context
                    .DivulgacoesRegionais
                    .FirstOrDefaultAsync(
                        d => d.Id == divulgacaoId
                    );

            if (divulgacao == null)
            {
                return;
            }

            divulgacao.MercadoPagoPaymentId =
                pagamento.Id.ToString();

            divulgacao.MercadoPagoStatus =
                pagamento.Status ?? "";

            if (pagamento.Status == "approved")
            {
                var divulgacoesAntigas =
                    await _context
                        .DivulgacoesRegionais
                        .Where(
                            d =>
                                d.ClinicaId
                                    == divulgacao.ClinicaId &&
                                d.Id
                                    != divulgacao.Id &&
                                d.Ativo
                        )
                        .ToListAsync();

                foreach (var antiga
                    in divulgacoesAntigas)
                {
                    antiga.Ativo = false;

                    antiga.Status =
                        "Substituído";
                }

                divulgacao.Pago = true;

                divulgacao.Aprovado = true;

                divulgacao.Ativo = true;

                divulgacao.DataPagamento =
                    DateTime.Now;

                divulgacao.DataAprovacao =
                    DateTime.Now;

                divulgacao.DataInicio =
                    DateTime.Now;

                divulgacao.DataFim =
                    DateTime.Now.AddMonths(1);

                divulgacao.Status =
                    "Ativo";
            }
            else if (
                pagamento.Status == "rejected" ||
                pagamento.Status == "cancelled")
            {
                divulgacao.Pago = false;

                divulgacao.Aprovado = false;

                divulgacao.Ativo = false;

                divulgacao.Status =
                    "Pagamento recusado";
            }
            else if (
                pagamento.Status == "refunded" ||
                pagamento.Status == "charged_back")
            {
                divulgacao.Pago = false;

                divulgacao.Aprovado = false;

                divulgacao.Ativo = false;

                divulgacao.Status =
                    "Pagamento estornado";
            }
            else
            {
                divulgacao.Pago = false;

                divulgacao.Aprovado = false;

                divulgacao.Ativo = false;

                divulgacao.Status =
                    "Aguardando pagamento";
            }

            await _context.SaveChangesAsync();
        }

        // =====================================================
        // OBTER ID DO PAGAMENTO
        // =====================================================

        private async Task<string?> ObterPaymentId()
        {
            var idQuery =
                Request.Query["data.id"]
                    .FirstOrDefault()
                ??
                Request.Query["id"]
                    .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(
                idQuery))
            {
                return idQuery;
            }

            using var reader =
                new StreamReader(
                    Request.Body
                );

            var body =
                await reader.ReadToEndAsync();

            _logger.LogInformation(
                "Webhook Mercado Pago recebido."
            );

            if (string.IsNullOrWhiteSpace(
                body))
            {
                return null;
            }

            try
            {
                using var json =
                    JsonDocument.Parse(
                        body
                    );

                var root =
                    json.RootElement;

                if (root.TryGetProperty(
                        "data",
                        out var data) &&
                    data.TryGetProperty(
                        "id",
                        out var dataId))
                {
                    return dataId.ToString();
                }

                if (root.TryGetProperty(
                        "id",
                        out var id))
                {
                    return id.ToString();
                }

                if (root.TryGetProperty(
                        "resource",
                        out var resource))
                {
                    var resourceText =
                        resource.GetString();

                    if (!string.IsNullOrWhiteSpace(
                        resourceText))
                    {
                        return resourceText
                            .Split("/")
                            .LastOrDefault();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Não foi possível interpretar " +
                    "o webhook do Mercado Pago."
                );

                return null;
            }

            return null;
        }

        // =====================================================
        // CONSULTAR PAGAMENTO NO MERCADO PAGO
        // =====================================================

        private async Task<MercadoPagoPaymentResponse?>
            ConsultarPagamentoMercadoPago(
                string paymentId)
        {
            var accessToken =
                _config[
                    "MercadoPago:AccessToken"
                ];

            if (string.IsNullOrWhiteSpace(
                accessToken))
            {
                return null;
            }

            var client =
                _httpClientFactory
                    .CreateClient();

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    $"https://api.mercadopago.com/v1/payments/{paymentId}"
                );

            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    accessToken
                );

            using var response =
                await client.SendAsync(
                    request
                );

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Não foi possível consultar o " +
                    "pagamento {PaymentId} no Mercado Pago. " +
                    "Status HTTP: {StatusCode}.",
                    paymentId,
                    response.StatusCode
                );

                return null;
            }

            var json =
                await response.Content
                    .ReadAsStringAsync();

            return JsonSerializer
                .Deserialize<
                    MercadoPagoPaymentResponse
                >(json);
        }

        // =====================================================
        // RETORNO DO MERCADO PAGO
        // =====================================================

        private class MercadoPagoPaymentResponse
        {
            [JsonPropertyName("id")]
            public long Id { get; set; }

            [JsonPropertyName("status")]
            public string? Status { get; set; }

            [JsonPropertyName("external_reference")]
            public string? ExternalReference { get; set; }

            [JsonPropertyName("transaction_amount")]
            public decimal TransactionAmount { get; set; }
        }
    }
}