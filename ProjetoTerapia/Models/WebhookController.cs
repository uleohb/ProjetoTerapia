using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjetoTerapia.Models;
using ProjetoTerapia.Services;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjetoTerapia.Controllers
{
    [ApiController]
    [Route("webhooks/mercadopago")]
    public class WebhookController : ControllerBase
    {
        private const decimal VALOR_PLANO = 450m;
        private const int DIAS_TRIAL = 7;

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

        // =====================================================
        // WEBHOOK
        // =====================================================

        [HttpPost]
        [HttpPost("/webhook")]
        [HttpPost("/pagamentos/mercadopago/notificacao")]
        public async Task<IActionResult> Receber()
        {
            try
            {
                var body =
                    await LerBodyAsync();

                if (!ValidarAssinaturaWebhook())
                {
                    _logger.LogWarning(
                        "Webhook Mercado Pago rejeitado por assinatura inválida."
                    );

                    return Unauthorized();
                }

                var tipo =
                    ObterTipoNotificacao(
                        body
                    );

                var recursoId =
                    ObterRecursoId(
                        body
                    );

                _logger.LogInformation(
                    "Webhook Mercado Pago recebido. Tipo: {Tipo}. Recurso: {RecursoId}.",
                    tipo,
                    recursoId
                );

                if (string.IsNullOrWhiteSpace(
                    recursoId))
                {
                    return Ok();
                }

                if (string.Equals(
                    tipo,
                    "payment",
                    StringComparison.OrdinalIgnoreCase))
                {
                    await ProcessarEventoPagamento(
                        recursoId
                    );

                    return Ok();
                }

                if (string.Equals(
                    tipo,
                    "subscription_preapproval",
                    StringComparison.OrdinalIgnoreCase))
                {
                    await ProcessarEventoAssinatura(
                        recursoId
                    );

                    return Ok();
                }

                if (string.Equals(
                    tipo,
                    "subscription_authorized_payment",
                    StringComparison.OrdinalIgnoreCase))
                {
                    await ProcessarEventoPagamentoAutorizado(
                        recursoId
                    );

                    return Ok();
                }

                if (string.Equals(
                    tipo,
                    "subscription_preapproval_plan",
                    StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation(
                        "Evento de plano de assinatura recebido. Plano: {PlanoId}.",
                        recursoId
                    );

                    return Ok();
                }

                if (string.IsNullOrWhiteSpace(
                    tipo))
                {
                    await ProcessarEventoPagamento(
                        recursoId
                    );
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao processar webhook do Mercado Pago."
                );

                return StatusCode(
                    StatusCodes.Status500InternalServerError
                );
            }
        }

        // =====================================================
        // PAYMENT
        // =====================================================

        private async Task ProcessarEventoPagamento(
            string paymentId)
        {
            var pagamento =
                await ConsultarPagamentoMercadoPago(
                    paymentId
                );

            if (pagamento == null ||
                string.IsNullOrWhiteSpace(
                    pagamento.ExternalReference))
            {
                return;
            }

            if (pagamento.ExternalReference
                .StartsWith(
                    "DIVULGACAO-",
                    StringComparison.OrdinalIgnoreCase))
            {
                await ProcessarPagamentoDivulgacao(
                    pagamento
                );

                return;
            }

            if (pagamento.ExternalReference
                .StartsWith(
                    "PLANO-CLINICA-",
                    StringComparison.OrdinalIgnoreCase))
            {
                await ProcessarPagamentoPlanoClinica(
                    pagamento
                );

                return;
            }

            if (pagamento.ExternalReference
                .StartsWith(
                    "ALINHAVIDA-CLINICA-",
                    StringComparison.OrdinalIgnoreCase))
            {
                await ProcessarPagamentoAssinaturaRecorrente(
                    pagamento
                );
            }
        }

        // =====================================================
        // SUBSCRIPTION_PREAPPROVAL
        // =====================================================

        private async Task ProcessarEventoAssinatura(
            string preapprovalId)
        {
            var retorno =
                await ConsultarAssinaturaMercadoPago(
                    preapprovalId
                );

            if (retorno == null ||
                string.IsNullOrWhiteSpace(
                    retorno.Id))
            {
                return;
            }

            var assinatura =
                await _context
                    .AssinaturasProfissionais
                    .Include(
                        a => a.Clinica
                    )
                    .FirstOrDefaultAsync(
                        a =>
                            a.MercadoPagoPreapprovalId ==
                            retorno.Id
                    );

            if (assinatura == null &&
                !string.IsNullOrWhiteSpace(
                    retorno.ExternalReference))
            {
                assinatura =
                    await _context
                        .AssinaturasProfissionais
                        .Include(
                            a => a.Clinica
                        )
                        .FirstOrDefaultAsync(
                            a =>
                                a.ExternalReference ==
                                retorno.ExternalReference
                        );
            }

            /*
             * NOVO FLUXO:
             *
             * Quando o checkout foi iniciado pelo
             * init_point de um plano, encontramos
             * a assinatura pelo preapproval_plan_id.
             */
            if (assinatura == null &&
                !string.IsNullOrWhiteSpace(
                    retorno.PreapprovalPlanId))
            {
                assinatura =
                    await _context
                        .AssinaturasProfissionais
                        .Include(
                            a => a.Clinica
                        )
                        .Where(
                            a =>
                                a.MercadoPagoPlanId ==
                                retorno.PreapprovalPlanId
                        )
                        .OrderByDescending(
                            a =>
                                a.DataCriacaoUtc
                        )
                        .FirstOrDefaultAsync();
            }

            if (assinatura == null)
            {
                _logger.LogWarning(
                    "Assinatura Mercado Pago {PreapprovalId} não encontrada no AlinhaVIDA. PlanId: {PlanId}.",
                    preapprovalId,
                    retorno.PreapprovalPlanId
                );

                return;
            }

            assinatura.MercadoPagoPreapprovalId =
                retorno.Id;

            if (!string.IsNullOrWhiteSpace(
                retorno.PreapprovalPlanId))
            {
                assinatura.MercadoPagoPlanId =
                    retorno.PreapprovalPlanId;
            }

            if (!string.IsNullOrWhiteSpace(
                retorno.InitPoint))
            {
                assinatura.LinkAutorizacao =
                    retorno.InitPoint;
            }

            assinatura.Status =
                retorno.Status ??
                assinatura.Status;

            assinatura.DataAtualizacaoUtc =
                DateTime.UtcNow;

            var proximaCobranca =
                ConverterDataUtc(
                    retorno.NextPaymentDate
                );

            if (proximaCobranca.HasValue)
            {
                assinatura.ProximaCobrancaUtc =
                    proximaCobranca;
            }

            if (string.Equals(
                assinatura.Status,
                "authorized",
                StringComparison.OrdinalIgnoreCase))
            {
                assinatura.Ativa =
                    true;

                assinatura.RenovacaoAutomatica =
                    true;

                assinatura.DataAutorizacaoUtc ??=
                    DateTime.UtcNow;

                var dataCriacaoMercadoPago =
                    ConverterDataUtc(
                        retorno.DateCreated
                    );

                assinatura.InicioTesteGratisUtc ??=
                    dataCriacaoMercadoPago ??
                    DateTime.UtcNow;

                if (!assinatura
                    .FimTesteGratisUtc
                    .HasValue)
                {
                    if (proximaCobranca.HasValue &&
                        proximaCobranca.Value >
                            assinatura
                                .InicioTesteGratisUtc
                                .Value
                        &&
                        proximaCobranca.Value <=
                            assinatura
                                .InicioTesteGratisUtc
                                .Value
                                .AddDays(15))
                    {
                        assinatura.FimTesteGratisUtc =
                            proximaCobranca.Value;
                    }
                    else
                    {
                        assinatura.FimTesteGratisUtc =
                            assinatura
                                .InicioTesteGratisUtc
                                .Value
                                .AddDays(
                                    DIAS_TRIAL
                                );
                    }
                }

                if (assinatura.Clinica != null)
                {
                    assinatura.Clinica.Pago =
                        true;

                    assinatura.Clinica.NomePlano =
                        "Plano Profissional Anual";

                    assinatura.Clinica.ValorPlano =
                        VALOR_PLANO;

                    assinatura
                        .Clinica
                        .MercadoPagoPlanoStatus =
                            "authorized";
                }
            }
            else if (string.Equals(
                assinatura.Status,
                "paused",
                StringComparison.OrdinalIgnoreCase))
            {
                assinatura.Ativa =
                    false;

                if (assinatura.Clinica != null)
                {
                    assinatura
                        .Clinica
                        .MercadoPagoPlanoStatus =
                            "paused";

                    assinatura.Clinica.Pago =
                        TemAcessoVigente(
                            assinatura,
                            assinatura.Clinica
                        );
                }
            }
            else if (
                string.Equals(
                    assinatura.Status,
                    "cancelled",
                    StringComparison.OrdinalIgnoreCase)
                ||
                string.Equals(
                    assinatura.Status,
                    "canceled",
                    StringComparison.OrdinalIgnoreCase))
            {
                assinatura.Ativa =
                    false;

                assinatura.RenovacaoAutomatica =
                    false;

                assinatura.DataCancelamentoUtc ??=
                    DateTime.UtcNow;

                if (assinatura.Clinica != null)
                {
                    assinatura
                        .Clinica
                        .MercadoPagoPlanoStatus =
                            assinatura.Status;

                    assinatura.Clinica.Pago =
                        TemAcessoVigente(
                            assinatura,
                            assinatura.Clinica
                        );
                }
            }
            else if (string.Equals(
                assinatura.Status,
                "pending",
                StringComparison.OrdinalIgnoreCase))
            {
                assinatura.Ativa =
                    false;

                if (assinatura.Clinica != null)
                {
                    assinatura
                        .Clinica
                        .MercadoPagoPlanoStatus =
                            "pending";
                }
            }

            await _context
                .SaveChangesAsync();
        }

        // =====================================================
        // SUBSCRIPTION_AUTHORIZED_PAYMENT
        // =====================================================

        private async Task
            ProcessarEventoPagamentoAutorizado(
                string authorizedPaymentId)
        {
            var fatura =
                await ConsultarPagamentoAutorizadoMercadoPago(
                    authorizedPaymentId
                );

            if (fatura == null ||
                string.IsNullOrWhiteSpace(
                    fatura.PreapprovalId))
            {
                return;
            }

            var assinatura =
                await _context
                    .AssinaturasProfissionais
                    .Include(
                        a => a.Clinica
                    )
                    .FirstOrDefaultAsync(
                        a =>
                            a.MercadoPagoPreapprovalId ==
                            fatura.PreapprovalId
                    );

            if (assinatura == null)
            {
                await ProcessarEventoAssinatura(
                    fatura.PreapprovalId
                );

                assinatura =
                    await _context
                        .AssinaturasProfissionais
                        .Include(
                            a => a.Clinica
                        )
                        .FirstOrDefaultAsync(
                            a =>
                                a.MercadoPagoPreapprovalId ==
                                fatura.PreapprovalId
                        );
            }

            if (assinatura == null)
            {
                _logger.LogWarning(
                    "Assinatura da fatura recorrente {FaturaId} não encontrada. Preapproval: {PreapprovalId}.",
                    authorizedPaymentId,
                    fatura.PreapprovalId
                );

                return;
            }

            var statusPagamento =
                fatura.Payment?.Status ??
                fatura.Summarized ??
                fatura.Status;

            assinatura.StatusUltimoPagamento =
                statusPagamento;

            assinatura.DataAtualizacaoUtc =
                DateTime.UtcNow;

            if (fatura.Payment != null &&
                fatura.Payment.Id > 0)
            {
                var paymentId =
                    fatura.Payment.Id.ToString();

                var pagamento =
                    await ConsultarPagamentoMercadoPago(
                        paymentId
                    );

                if (pagamento != null)
                {
                    await AplicarPagamentoNaAssinatura(
                        assinatura,
                        pagamento
                    );
                }
                else
                {
                    assinatura.UltimoPagamentoId =
                        paymentId;

                    await _context
                        .SaveChangesAsync();
                }
            }
            else
            {
                await _context
                    .SaveChangesAsync();
            }

            await ProcessarEventoAssinatura(
                fatura.PreapprovalId
            );
        }

        // =====================================================
        // PAGAMENTO RECORRENTE
        // =====================================================

        private async Task
            ProcessarPagamentoAssinaturaRecorrente(
                MercadoPagoPaymentResponse pagamento)
        {
            if (string.IsNullOrWhiteSpace(
                pagamento.ExternalReference))
            {
                return;
            }

            var assinatura =
                await _context
                    .AssinaturasProfissionais
                    .Include(
                        a => a.Clinica
                    )
                    .FirstOrDefaultAsync(
                        a =>
                            a.ExternalReference ==
                            pagamento.ExternalReference
                    );

            if (assinatura == null)
            {
                var clinicaId =
                    ExtrairClinicaIdExternalReference(
                        pagamento.ExternalReference
                    );

                if (clinicaId.HasValue)
                {
                    assinatura =
                        await _context
                            .AssinaturasProfissionais
                            .Include(
                                a => a.Clinica
                            )
                            .Where(
                                a =>
                                    a.ClinicaId ==
                                    clinicaId.Value
                            )
                            .OrderByDescending(
                                a =>
                                    a.DataCriacaoUtc
                            )
                            .FirstOrDefaultAsync();
                }
            }

            if (assinatura == null)
            {
                return;
            }

            await AplicarPagamentoNaAssinatura(
                assinatura,
                pagamento
            );

            if (!string.IsNullOrWhiteSpace(
                assinatura
                    .MercadoPagoPreapprovalId))
            {
                await ProcessarEventoAssinatura(
                    assinatura
                        .MercadoPagoPreapprovalId
                );
            }
        }

        // =====================================================
        // APLICAR PAGAMENTO
        // =====================================================

        private async Task AplicarPagamentoNaAssinatura(
            AssinaturaProfissional assinatura,
            MercadoPagoPaymentResponse pagamento)
        {
            var paymentId =
                pagamento.Id.ToString();

            var jaProcessado =
                assinatura.UltimoPagamentoId ==
                    paymentId
                &&
                string.Equals(
                    assinatura.StatusUltimoPagamento,
                    pagamento.Status,
                    StringComparison.OrdinalIgnoreCase
                );

            assinatura.UltimoPagamentoId =
                paymentId;

            assinatura.StatusUltimoPagamento =
                pagamento.Status;

            assinatura.DataAtualizacaoUtc =
                DateTime.UtcNow;

            if (string.Equals(
                pagamento.Status,
                "approved",
                StringComparison.OrdinalIgnoreCase))
            {
                assinatura.Ativa =
                    true;

                assinatura.Status =
                    "authorized";

                assinatura.RenovacaoAutomatica =
                    true;

                assinatura.DataUltimoPagamentoUtc =
                    ConverterDataUtc(
                        pagamento.DateApproved
                    )
                    ??
                    DateTime.UtcNow;

                if (assinatura.Clinica != null)
                {
                    var clinica =
                        assinatura.Clinica;

                    clinica.Pago =
                        true;

                    clinica.NomePlano =
                        "Plano Profissional Anual";

                    clinica.ValorPlano =
                        pagamento.TransactionAmount > 0
                        ? pagamento.TransactionAmount
                        : VALOR_PLANO;

                    clinica.MercadoPagoPlanoPaymentId =
                        paymentId;

                    clinica.MercadoPagoPlanoStatus =
                        "approved";

                    if (!jaProcessado)
                    {
                        clinica.DataPagamento =
                            DateTime.Now;

                        if (clinica
                                .DataVencimento
                                .HasValue
                            &&
                            clinica
                                .DataVencimento
                                .Value
                                .Date
                                >=
                            DateTime.Today)
                        {
                            clinica.DataVencimento =
                                clinica
                                    .DataVencimento
                                    .Value
                                    .AddYears(1);
                        }
                        else
                        {
                            clinica.DataVencimento =
                                DateTime.Now
                                    .AddYears(1);
                        }
                    }
                }

                await _context
                    .SaveChangesAsync();

                if (!jaProcessado &&
                    assinatura.Clinica != null)
                {
                    await EnviarNotificacaoPagamentoPlano(
                        assinatura.Clinica,
                        pagamento
                    );
                }

                return;
            }

            if (string.Equals(
                    pagamento.Status,
                    "rejected",
                    StringComparison.OrdinalIgnoreCase)
                ||
                string.Equals(
                    pagamento.Status,
                    "cancelled",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (assinatura.Clinica != null)
                {
                    assinatura
                        .Clinica
                        .MercadoPagoPlanoStatus =
                            pagamento.Status;
                }

                await _context
                    .SaveChangesAsync();

                return;
            }

            if (string.Equals(
                    pagamento.Status,
                    "refunded",
                    StringComparison.OrdinalIgnoreCase)
                ||
                string.Equals(
                    pagamento.Status,
                    "charged_back",
                    StringComparison.OrdinalIgnoreCase))
            {
                assinatura.Ativa =
                    false;

                if (assinatura.Clinica != null)
                {
                    assinatura.Clinica.Pago =
                        false;

                    assinatura
                        .Clinica
                        .MercadoPagoPlanoStatus =
                            pagamento.Status;
                }

                await _context
                    .SaveChangesAsync();

                return;
            }

            await _context
                .SaveChangesAsync();
        }

        // =====================================================
        // PLANO ANTIGO
        // =====================================================

        private async Task ProcessarPagamentoPlanoClinica(
            MercadoPagoPaymentResponse pagamento)
        {
            var idTexto =
                pagamento.ExternalReference!
                    .Replace(
                        "PLANO-CLINICA-",
                        "",
                        StringComparison.OrdinalIgnoreCase
                    );

            if (!int.TryParse(
                idTexto,
                out int clinicaId))
            {
                return;
            }

            var clinica =
                await _context
                    .Clinicas
                    .FirstOrDefaultAsync(
                        c =>
                            c.Id ==
                            clinicaId
                    );

            if (clinica == null)
            {
                return;
            }

            var paymentId =
                pagamento.Id.ToString();

            if (clinica.MercadoPagoPlanoPaymentId ==
                    paymentId
                &&
                clinica.MercadoPagoPlanoStatus ==
                    "approved")
            {
                return;
            }

            clinica.MercadoPagoPlanoStatus =
                pagamento.Status ?? "";

            var pagamentoAprovadoAgora =
                false;

            if (pagamento.Status == "approved")
            {
                pagamentoAprovadoAgora =
                    true;

                clinica.MercadoPagoPlanoPaymentId =
                    paymentId;

                clinica.Pago =
                    true;

                clinica.NomePlano =
                    "Plano Profissional Anual";

                clinica.ValorPlano =
                    pagamento.TransactionAmount > 0
                    ? pagamento.TransactionAmount
                    : VALOR_PLANO;

                clinica.DataPagamento =
                    DateTime.Now;

                if (clinica.DataVencimento.HasValue &&
                    clinica.DataVencimento.Value.Date >=
                    DateTime.Today)
                {
                    clinica.DataVencimento =
                        clinica
                            .DataVencimento
                            .Value
                            .AddYears(1);
                }
                else
                {
                    clinica.DataVencimento =
                        DateTime.Now
                            .AddYears(1);
                }
            }

            await _context
                .SaveChangesAsync();

            if (pagamentoAprovadoAgora)
            {
                await EnviarNotificacaoPagamentoPlano(
                    clinica,
                    pagamento
                );
            }
        }

        // =====================================================
        // E-MAIL
        // =====================================================

        private async Task EnviarNotificacaoPagamentoPlano(
            Clinica clinica,
            MercadoPagoPaymentResponse pagamento)
        {
            try
            {
                var emailDestino =
                    _config[
                        "Email:NotificacaoPagamentos"
                    ];

                if (string.IsNullOrWhiteSpace(
                    emailDestino))
                {
                    emailDestino =
                        _config[
                            "Email:Usuario"
                        ];
                }

                if (string.IsNullOrWhiteSpace(
                    emailDestino))
                {
                    return;
                }

                var cultura =
                    new CultureInfo(
                        "pt-BR"
                    );

                var valor =
                    (
                        clinica.ValorPlano ??
                        VALOR_PLANO
                    )
                    .ToString(
                        "C",
                        cultura
                    );

                var dataPagamento =
                    clinica.DataPagamento.HasValue
                    ?
                    clinica
                        .DataPagamento
                        .Value
                        .ToString(
                            "dd/MM/yyyy HH:mm"
                        )
                    :
                    DateTime.Now.ToString(
                        "dd/MM/yyyy HH:mm"
                    );

                var vencimento =
                    clinica.DataVencimento.HasValue
                    ?
                    clinica
                        .DataVencimento
                        .Value
                        .ToString(
                            "dd/MM/yyyy"
                        )
                    :
                    "Não informado";

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

Vencimento / próximo ciclo:
{vencimento}

Status:
Aprovado

ID Mercado Pago:
{pagamento.Id}

ID da clínica no AlinhaVIDA:
{clinica.Id}

O plano foi atualizado automaticamente pelo sistema.

AlinhaVIDA
www.alinhavida.com.br";

                await _emailService
                    .EnviarEmail(
                        emailDestino,
                        assunto,
                        mensagem
                    );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Pagamento confirmado para clínica {ClinicaId}, mas houve erro ao enviar o e-mail.",
                    clinica.Id
                );
            }
        }

        // =====================================================
        // DIVULGAÇÃO
        // =====================================================

        private async Task ProcessarPagamentoDivulgacao(
            MercadoPagoPaymentResponse pagamento)
        {
            var idTexto =
                pagamento.ExternalReference!
                    .Replace(
                        "DIVULGACAO-",
                        "",
                        StringComparison.OrdinalIgnoreCase
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
                        d =>
                            d.Id ==
                            divulgacaoId
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
                                d.ClinicaId ==
                                    divulgacao.ClinicaId
                                &&
                                d.Id !=
                                    divulgacao.Id
                                &&
                                d.Ativo
                        )
                        .ToListAsync();

                foreach (
                    var antiga
                    in divulgacoesAntigas)
                {
                    antiga.Ativo =
                        false;

                    antiga.Status =
                        "Substituído";
                }

                divulgacao.Pago =
                    true;

                divulgacao.Aprovado =
                    true;

                divulgacao.Ativo =
                    true;

                divulgacao.DataPagamento =
                    DateTime.Now;

                divulgacao.DataAprovacao =
                    DateTime.Now;

                divulgacao.DataInicio =
                    DateTime.Now;

                divulgacao.DataFim =
                    DateTime.Now
                        .AddMonths(1);

                divulgacao.Status =
                    "Ativo";
            }
            else if (
                pagamento.Status == "rejected"
                ||
                pagamento.Status == "cancelled")
            {
                divulgacao.Pago =
                    false;

                divulgacao.Aprovado =
                    false;

                divulgacao.Ativo =
                    false;

                divulgacao.Status =
                    "Pagamento recusado";
            }
            else if (
                pagamento.Status == "refunded"
                ||
                pagamento.Status == "charged_back")
            {
                divulgacao.Pago =
                    false;

                divulgacao.Aprovado =
                    false;

                divulgacao.Ativo =
                    false;

                divulgacao.Status =
                    "Pagamento estornado";
            }
            else
            {
                divulgacao.Pago =
                    false;

                divulgacao.Aprovado =
                    false;

                divulgacao.Ativo =
                    false;

                divulgacao.Status =
                    "Aguardando pagamento";
            }

            await _context
                .SaveChangesAsync();
        }

        // =====================================================
        // CONSULTAR PAYMENT
        // =====================================================

        private async Task<MercadoPagoPaymentResponse?>
            ConsultarPagamentoMercadoPago(
                string paymentId)
        {
            var client =
                _httpClientFactory
                    .CreateClient();

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    $"https://api.mercadopago.com/v1/payments/{Uri.EscapeDataString(paymentId)}"
                );

            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    ObterAccessToken()
                );

            using var response =
                await client.SendAsync(
                    request
                );

            var json =
                await response
                    .Content
                    .ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return JsonSerializer
                .Deserialize<
                    MercadoPagoPaymentResponse
                >(json);
        }

        // =====================================================
        // CONSULTAR ASSINATURA
        // =====================================================

        private async Task<MercadoPagoPreapprovalResponse?>
            ConsultarAssinaturaMercadoPago(
                string preapprovalId)
        {
            var client =
                _httpClientFactory
                    .CreateClient();

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    "https://api.mercadopago.com/preapproval/" +
                    Uri.EscapeDataString(
                        preapprovalId
                    )
                );

            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    ObterAccessToken()
                );

            using var response =
                await client.SendAsync(
                    request
                );

            var json =
                await response
                    .Content
                    .ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return JsonSerializer
                .Deserialize<
                    MercadoPagoPreapprovalResponse
                >(json);
        }

        // =====================================================
        // CONSULTAR FATURA
        // =====================================================

        private async Task<
            MercadoPagoAuthorizedPaymentResponse?>
            ConsultarPagamentoAutorizadoMercadoPago(
                string authorizedPaymentId)
        {
            var client =
                _httpClientFactory
                    .CreateClient();

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    "https://api.mercadopago.com/authorized_payments/" +
                    Uri.EscapeDataString(
                        authorizedPaymentId
                    )
                );

            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    ObterAccessToken()
                );

            using var response =
                await client.SendAsync(
                    request
                );

            var json =
                await response
                    .Content
                    .ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return JsonSerializer
                .Deserialize<
                    MercadoPagoAuthorizedPaymentResponse
                >(json);
        }

        // =====================================================
        // BODY
        // =====================================================

        private async Task<string> LerBodyAsync()
        {
            Request.EnableBuffering();

            if (Request.Body.CanSeek)
            {
                Request.Body.Position =
                    0;
            }

            using var reader =
                new StreamReader(
                    Request.Body,
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    leaveOpen: true
                );

            var body =
                await reader
                    .ReadToEndAsync();

            if (Request.Body.CanSeek)
            {
                Request.Body.Position =
                    0;
            }

            return body;
        }

        // =====================================================
        // TIPO
        // =====================================================

        private string? ObterTipoNotificacao(
            string body)
        {
            var queryTipo =
                Request.Query["type"]
                    .FirstOrDefault()
                ??
                Request.Query["topic"]
                    .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(
                queryTipo))
            {
                return queryTipo;
            }

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
                        "type",
                        out var type))
                {
                    return type.GetString();
                }

                if (root.TryGetProperty(
                        "topic",
                        out var topic))
                {
                    return topic.GetString();
                }

                if (root.TryGetProperty(
                        "action",
                        out var action))
                {
                    var actionText =
                        action.GetString();

                    if (!string.IsNullOrWhiteSpace(
                        actionText)
                        &&
                        actionText.StartsWith(
                            "payment.",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return "payment";
                    }
                }
            }
            catch
            {
            }

            return null;
        }

        // =====================================================
        // RECURSO ID
        // =====================================================

        private string? ObterRecursoId(
            string body)
        {
            var queryId =
                Request.Query["data.id"]
                    .FirstOrDefault()
                ??
                Request.Query["data_id"]
                    .FirstOrDefault()
                ??
                Request.Query["id"]
                    .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(
                queryId))
            {
                return queryId;
            }

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
                        out var data)
                    &&
                    data.TryGetProperty(
                        "id",
                        out var dataId))
                {
                    return dataId.ToString();
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
                            .Split(
                                '/',
                                StringSplitOptions
                                    .RemoveEmptyEntries
                            )
                            .LastOrDefault();
                    }
                }

                if (root.TryGetProperty(
                        "id",
                        out var id))
                {
                    return id.ToString();
                }
            }
            catch
            {
            }

            return null;
        }

        // =====================================================
        // ASSINATURA DO WEBHOOK
        // =====================================================

        private bool ValidarAssinaturaWebhook()
        {
            var secret =
                _config[
                    "MercadoPago:WebhookSecret"
                ];

            if (string.IsNullOrWhiteSpace(
                secret))
            {
                return true;
            }

            var xSignature =
                Request.Headers[
                    "x-signature"
                ]
                .FirstOrDefault();

            var xRequestId =
                Request.Headers[
                    "x-request-id"
                ]
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(
                xSignature))
            {
                return false;
            }

            string? timestamp =
                null;

            string? assinaturaRecebida =
                null;

            foreach (
                var parte
                in xSignature.Split(','))
            {
                var itens =
                    parte.Split(
                        '=',
                        2
                    );

                if (itens.Length != 2)
                {
                    continue;
                }

                var chave =
                    itens[0].Trim();

                var valor =
                    itens[1].Trim();

                if (string.Equals(
                    chave,
                    "ts",
                    StringComparison.OrdinalIgnoreCase))
                {
                    timestamp =
                        valor;
                }

                if (string.Equals(
                    chave,
                    "v1",
                    StringComparison.OrdinalIgnoreCase))
                {
                    assinaturaRecebida =
                        valor;
                }
            }

            if (string.IsNullOrWhiteSpace(
                    timestamp)
                ||
                string.IsNullOrWhiteSpace(
                    assinaturaRecebida))
            {
                return false;
            }

            var dataId =
                Request.Query[
                    "data.id"
                ]
                .FirstOrDefault()
                ??
                Request.Query[
                    "data_id"
                ]
                .FirstOrDefault();

            var manifest =
                new StringBuilder();

            if (!string.IsNullOrWhiteSpace(
                dataId))
            {
                manifest.Append(
                    $"id:{dataId};"
                );
            }

            if (!string.IsNullOrWhiteSpace(
                xRequestId))
            {
                manifest.Append(
                    $"request-id:{xRequestId};"
                );
            }

            manifest.Append(
                $"ts:{timestamp};"
            );

            using var hmac =
                new HMACSHA256(
                    Encoding.UTF8.GetBytes(
                        secret
                    )
                );

            var hashBytes =
                hmac.ComputeHash(
                    Encoding.UTF8.GetBytes(
                        manifest.ToString()
                    )
                );

            var assinaturaCalculada =
                Convert
                    .ToHexString(
                        hashBytes
                    )
                    .ToLowerInvariant();

            var recebida =
                assinaturaRecebida
                    .Trim()
                    .ToLowerInvariant();

            if (assinaturaCalculada.Length !=
                recebida.Length)
            {
                return false;
            }

            return CryptographicOperations
                .FixedTimeEquals(
                    Encoding.UTF8.GetBytes(
                        assinaturaCalculada
                    ),
                    Encoding.UTF8.GetBytes(
                        recebida
                    )
                );
        }

        // =====================================================
        // HELPERS
        // =====================================================

        private static bool TemAcessoVigente(
            AssinaturaProfissional assinatura,
            Clinica clinica)
        {
            if (assinatura
                    .FimTesteGratisUtc
                    .HasValue
                &&
                assinatura
                    .FimTesteGratisUtc
                    .Value
                    >
                DateTime.UtcNow)
            {
                return true;
            }

            if (clinica
                    .DataVencimento
                    .HasValue
                &&
                clinica
                    .DataVencimento
                    .Value
                    >
                DateTime.Now)
            {
                return true;
            }

            return false;
        }

        private static int?
            ExtrairClinicaIdExternalReference(
                string externalReference)
        {
            const string prefixo =
                "ALINHAVIDA-CLINICA-";

            if (!externalReference
                .StartsWith(
                    prefixo,
                    StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var restante =
                externalReference
                    .Substring(
                        prefixo.Length
                    );

            var primeiroTrecho =
                restante
                    .Split(
                        '-',
                        StringSplitOptions
                            .RemoveEmptyEntries
                    )
                    .FirstOrDefault();

            if (int.TryParse(
                primeiroTrecho,
                out int clinicaId))
            {
                return clinicaId;
            }

            return null;
        }

        private string ObterAccessToken()
        {
            var accessToken =
                _config[
                    "MercadoPago:AccessToken"
                ];

            if (string.IsNullOrWhiteSpace(
                accessToken))
            {
                throw new Exception(
                    "MercadoPago:AccessToken não configurado."
                );
            }

            return accessToken;
        }

        private static DateTime?
            ConverterDataUtc(
                string? texto)
        {
            if (string.IsNullOrWhiteSpace(
                texto))
            {
                return null;
            }

            if (DateTimeOffset.TryParse(
                texto,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var data))
            {
                return data.UtcDateTime;
            }

            return null;
        }

        // =====================================================
        // DTO PAYMENT
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
            [JsonNumberHandling(
                JsonNumberHandling.AllowReadingFromString)]
            public decimal TransactionAmount { get; set; }

            [JsonPropertyName("date_approved")]
            public string? DateApproved { get; set; }
        }

        // =====================================================
        // DTO PREAPPROVAL
        // =====================================================

        private class MercadoPagoPreapprovalResponse
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("preapproval_plan_id")]
            public string? PreapprovalPlanId { get; set; }

            [JsonPropertyName("status")]
            public string? Status { get; set; }

            [JsonPropertyName("external_reference")]
            public string? ExternalReference { get; set; }

            [JsonPropertyName("init_point")]
            public string? InitPoint { get; set; }

            [JsonPropertyName("next_payment_date")]
            public string? NextPaymentDate { get; set; }

            [JsonPropertyName("date_created")]
            public string? DateCreated { get; set; }
        }

        // =====================================================
        // DTO FATURA
        // =====================================================

        private class
            MercadoPagoAuthorizedPaymentResponse
        {
            [JsonPropertyName("id")]
            public long Id { get; set; }

            [JsonPropertyName("preapproval_id")]
            public string? PreapprovalId { get; set; }

            [JsonPropertyName("status")]
            public string? Status { get; set; }

            [JsonPropertyName("summarized")]
            public string? Summarized { get; set; }

            [JsonPropertyName("transaction_amount")]
            [JsonNumberHandling(
                JsonNumberHandling.AllowReadingFromString)]
            public decimal TransactionAmount { get; set; }

            [JsonPropertyName("payment")]
            public MercadoPagoAuthorizedPaymentDetail?
                Payment
            {
                get;
                set;
            }
        }

        private class
            MercadoPagoAuthorizedPaymentDetail
        {
            [JsonPropertyName("id")]
            public long Id { get; set; }

            [JsonPropertyName("status")]
            public string? Status { get; set; }

            [JsonPropertyName("status_detail")]
            public string? StatusDetail { get; set; }
        }
    }
}