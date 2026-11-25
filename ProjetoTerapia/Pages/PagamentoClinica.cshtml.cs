using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProjetoTerapia.Models;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjetoTerapia.Pages
{
    public class PagamentoClinicaModel : PageModel
    {
        private const decimal VALOR_PLANO = 450m;
        private const int DIAS_TRIAL = 7;

        private const string NOME_PLANO_MP =
            "AlinhaVIDA - Plano Profissional Anual";

        private const string VERSAO_TERMOS = "1.0";
        private const string VERSAO_PRIVACIDADE = "1.0";

        private readonly AppDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _environment;

        public PagamentoClinicaModel(
            AppDbContext context,
            IHttpClientFactory httpClientFactory,
            IConfiguration config,
            IWebHostEnvironment environment)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _config = config;
            _environment = environment;
        }

        // =====================================================
        // DADOS DA TELA
        // =====================================================

        public Clinica? Clinica { get; set; }

        public AssinaturaProfissional?
            AssinaturaAtual
        { get; set; }

        public int? DiasRestantes { get; set; }

        public bool PlanoVencido { get; set; }

        public bool PlanoPertoDeVencer { get; set; }

        public string ValorPlanoFormatado { get; set; } =
            VALOR_PLANO.ToString(
                "C",
                new CultureInfo("pt-BR")
            );

        // =====================================================
        // ACEITES
        // =====================================================

        [BindProperty]
        public bool AceitouTermosEPrivacidade { get; set; }

        [BindProperty]
        public bool AceitouCobrancaRecorrente { get; set; }

        // =====================================================
        // STATUS DA ASSINATURA
        // =====================================================

        public bool AssinaturaAtiva =>
            AssinaturaAtual != null &&
            AssinaturaAtual.Ativa &&
            string.Equals(
                AssinaturaAtual.Status,
                "authorized",
                StringComparison.OrdinalIgnoreCase
            );

        public bool AssinaturaPendente =>
            AssinaturaAtual != null &&
            string.Equals(
                AssinaturaAtual.Status,
                "pending",
                StringComparison.OrdinalIgnoreCase
            ) &&
            !string.IsNullOrWhiteSpace(
                AssinaturaAtual.LinkAutorizacao
            );

        public bool EmTesteGratis =>
            AssinaturaAtiva &&
            AssinaturaAtual?.FimTesteGratisUtc != null &&
            AssinaturaAtual
                .FimTesteGratisUtc
                .Value > DateTime.UtcNow;

        public DateTime? FimTesteGratisLocal =>
            AssinaturaAtual?
                .FimTesteGratisUtc?
                .ToLocalTime();

        public DateTime? ProximaCobrancaLocal =>
            AssinaturaAtual?
                .ProximaCobrancaUtc?
                .ToLocalTime();

        // =====================================================
        // GET
        // =====================================================

        public async Task<IActionResult> OnGetAsync()
        {
            if (!await CarregarClinicaAsync())
            {
                return RedirectToPage(
                    "/LoginClinica"
                );
            }

            if (string.Equals(
                Request.Query["assinatura"].ToString(),
                "retorno",
                StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    await SincronizarAssinaturaAtualAsync();

                    if (AssinaturaAtiva)
                    {
                        TempData["Sucesso"] =
                            "Assinatura autorizada. Seus 7 dias gratuitos começaram.";
                    }
                    else
                    {
                        TempData["Sucesso"] =
                            "Recebemos seu retorno do Mercado Pago. A assinatura está sendo confirmada.";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        "======================================"
                    );

                    Console.WriteLine(
                        "ERRO AO SINCRONIZAR ASSINATURA"
                    );

                    Console.WriteLine(
                        ex.ToString()
                    );

                    Console.WriteLine(
                        "======================================"
                    );

                    if (_environment.IsDevelopment())
                    {
                        TempData["Erro"] =
                            "ERRO MERCADO PAGO: " +
                            ex.Message;
                    }
                    else
                    {
                        TempData["Erro"] =
                            "O Mercado Pago retornou ao AlinhaVIDA, mas ainda não foi possível confirmar a assinatura.";
                    }
                }
            }

            await CarregarAssinaturaAtualAsync();

            return Page();
        }

        // =====================================================
        // INICIAR ASSINATURA
        // =====================================================

        public async Task<IActionResult> OnPostPagarAsync()
        {
            if (!await CarregarClinicaAsync() ||
                Clinica == null)
            {
                return RedirectToPage(
                    "/LoginClinica"
                );
            }

            
            await CarregarAssinaturaAtualAsync();

            if (AssinaturaAtiva)
            {
                TempData["Erro"] =
                    "Sua assinatura recorrente já está ativa.";

                return RedirectToPage(
                    "/PagamentoClinica"
                );
            }

            /*
             * Se já existe um checkout pendente,
             * reaproveitamos o link do plano.
             */
            if (AssinaturaPendente &&
                !string.IsNullOrWhiteSpace(
                    AssinaturaAtual!
                        .LinkAutorizacao
                ))
            {
                return Redirect(
                    AssinaturaAtual
                        .LinkAutorizacao
                );
            }

            /*
             * Evita assinatura duplicada para quem
             * ainda está no plano antigo e longe
             * do vencimento.
             */
            if (Clinica.Pago &&
                !PlanoVencido &&
                !PlanoPertoDeVencer)
            {
                TempData["Erro"] =
                    "Seu plano atual ainda está ativo.";

                return RedirectToPage(
                    "/PagamentoClinica"
                );
            }

            if (!AceitouTermosEPrivacidade ||
                !AceitouCobrancaRecorrente)
            {
                TempData["Erro"] =
                    "Leia e aceite os Termos de Uso, a Política de Privacidade e a cobrança recorrente anual.";

                return RedirectToPage(
                    "/PagamentoClinica"
                );
            }

            try
            {
                await RegistrarAceiteAsync(
                    Clinica
                );

                var externalReference =
                    $"ALINHAVIDA-CLINICA-{Clinica.Id}-{Guid.NewGuid():N}";

                /*
                 * NÃO criamos /preapproval aqui.
                 *
                 * O Mercado Pago exige card_token_id
                 * quando criamos uma assinatura
                 * diretamente associada a um plano.
                 *
                 * Criamos o plano e usamos o init_point.
                 * O cartão é informado diretamente
                 * dentro do Mercado Pago.
                 */
                var plano =
                    await CriarPlanoCheckoutMercadoPagoAsync(
                        externalReference
                    );

                var assinatura =
                    new AssinaturaProfissional
                    {
                        ClinicaId =
                            Clinica.Id,

                        ExternalReference =
                            externalReference,

                        NomePlano =
                            "Plano Profissional Anual",

                        MercadoPagoPlanId =
                            plano.Id,

                        MercadoPagoPreapprovalId =
                            null,

                        LinkAutorizacao =
                            plano.InitPoint,

                        Status =
                            "pending",

                        Ativa =
                            false,

                        RenovacaoAutomatica =
                            true,

                        Valor =
                            VALOR_PLANO,

                        Moeda =
                            "BRL",

                        Frequencia =
                            12,

                        TipoFrequencia =
                            "months",

                        DiasTesteGratis =
                            DIAS_TRIAL,

                        DataCriacaoUtc =
                            DateTime.UtcNow,

                        DataAtualizacaoUtc =
                            DateTime.UtcNow
                    };

                _context
                    .AssinaturasProfissionais
                    .Add(
                        assinatura
                    );

                await _context
                    .SaveChangesAsync();

                return Redirect(
                    plano.InitPoint
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "======================================"
                );

                Console.WriteLine(
                    "ERRO MERCADO PAGO"
                );

                Console.WriteLine(
                    ex.ToString()
                );

                Console.WriteLine(
                    "======================================"
                );

                if (_environment.IsDevelopment())
                {
                    TempData["Erro"] =
                        "ERRO MERCADO PAGO: " +
                        ex.Message;
                }
                else
                {
                    TempData["Erro"] =
                        "Não foi possível iniciar a assinatura agora. Tente novamente.";
                }

                return RedirectToPage(
                    "/PagamentoClinica"
                );
            }
        }

        // =====================================================
        // CARREGAR CLÍNICA
        // =====================================================

        private async Task<bool> CarregarClinicaAsync()
        {
            var id =
                HttpContext
                    .Session
                    .GetString(
                        "ClinicaLogada"
                    );

            if (string.IsNullOrWhiteSpace(id) ||
                !int.TryParse(
                    id,
                    out int clinicaId))
            {
                return false;
            }

            Clinica =
                await _context
                    .Clinicas
                    .FirstOrDefaultAsync(
                        c =>
                            c.Id ==
                            clinicaId
                    );

            if (Clinica == null)
            {
                return false;
            }

            ValorPlanoFormatado =
                (
                    Clinica.ValorPlano ??
                    VALOR_PLANO
                )
                .ToString(
                    "C",
                    new CultureInfo(
                        "pt-BR"
                    )
                );

            DiasRestantes = null;
            PlanoVencido = false;
            PlanoPertoDeVencer = false;

            if (Clinica.DataVencimento.HasValue)
            {
                DiasRestantes =
                    (int)Math.Ceiling(
                        (
                            Clinica
                                .DataVencimento
                                .Value
                                .Date
                            -
                            DateTime.Today
                        )
                        .TotalDays
                    );

                PlanoVencido =
                    DiasRestantes < 0;

                PlanoPertoDeVencer =
                    DiasRestantes >= 0 &&
                    DiasRestantes <= 30;
            }

            await CarregarAssinaturaAtualAsync();

            return true;
        }

        // =====================================================
        // CARREGAR ASSINATURA
        // =====================================================

        private async Task
            CarregarAssinaturaAtualAsync()
        {
            if (Clinica == null)
            {
                AssinaturaAtual =
                    null;

                return;
            }

            AssinaturaAtual =
                await _context
                    .AssinaturasProfissionais
                    .Where(
                        a =>
                            a.ClinicaId ==
                                Clinica.Id
                            &&
                            a.Status !=
                                "error"
                            &&
                            a.Status !=
                                "cancelled"
                            &&
                            a.Status !=
                                "canceled"
                    )
                    .OrderByDescending(
                        a =>
                            a.DataCriacaoUtc
                    )
                    .FirstOrDefaultAsync();
        }

        // =====================================================
        // REGISTRAR ACEITE DOS TERMOS
        // =====================================================

        private async Task RegistrarAceiteAsync(
            Clinica clinica)
        {
            var hashTermos =
                await CalcularHashArquivoAsync(
                    Path.Combine(
                        "Pages",
                        "TermosProfissional.cshtml"
                    ),
                    $"AlinhaVIDA-TermosProfissionais-{VERSAO_TERMOS}"
                );

            var hashPrivacidade =
                await CalcularHashArquivoAsync(
                    Path.Combine(
                        "Pages",
                        "PoliticaPrivacidade.cshtml"
                    ),
                    $"AlinhaVIDA-PoliticaPrivacidade-{VERSAO_PRIVACIDADE}"
                );

            var aceite =
                new AceiteTermoProfissional
                {
                    ClinicaId =
                        clinica.Id,

                    VersaoTermos =
                        VERSAO_TERMOS,

                    HashTermos =
                        hashTermos,

                    AceitouTermos =
                        true,

                    VersaoPoliticaPrivacidade =
                        VERSAO_PRIVACIDADE,

                    HashPoliticaPrivacidade =
                        hashPrivacidade,

                    AceitouPoliticaPrivacidade =
                        true,

                    AceitouCobrancaRecorrente =
                        true,

                    ValorInformado =
                        VALOR_PLANO,

                    DiasTesteGratisInformados =
                        DIAS_TRIAL,

                    PeriodicidadeInformada =
                        "Anual",

                    DataAceiteUtc =
                        DateTime.UtcNow,

                    IpAceite =
                        HttpContext
                            .Connection
                            .RemoteIpAddress?
                            .ToString(),

                    UserAgent =
                        LimitarTexto(
                            Request
                                .Headers[
                                    "User-Agent"
                                ]
                                .ToString(),
                            1000
                        ),

                    OrigemAceite =
                        "PagamentoClinica",

                    NomeProfissionalSnapshot =
                        LimitarTexto(
                            clinica.Nome,
                            200
                        ),

                    EmailSnapshot =
                        LimitarTexto(
                            clinica.Email,
                            320
                        ),

                    DocumentoSnapshot =
                        LimitarTexto(
                            clinica.Documento,
                            50
                        ),

                    CpfSnapshot =
                        LimitarTexto(
                            clinica.CPF,
                            30
                        )
                };

            _context
                .AceitesTermosProfissionais
                .Add(
                    aceite
                );

            await _context
                .SaveChangesAsync();
        }

        // =====================================================
        // HASH DOS TERMOS
        // =====================================================

        private async Task<string>
            CalcularHashArquivoAsync(
                string caminhoRelativo,
                string fallback)
        {
            try
            {
                var caminho =
                    Path.Combine(
                        _environment
                            .ContentRootPath,
                        caminhoRelativo
                    );

                if (System.IO.File.Exists(
                    caminho))
                {
                    var bytes =
                        await System.IO.File
                            .ReadAllBytesAsync(
                                caminho
                            );

                    return Convert
                        .ToHexString(
                            SHA256
                                .HashData(
                                    bytes
                                )
                        );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return Convert
                .ToHexString(
                    SHA256.HashData(
                        Encoding.UTF8
                            .GetBytes(
                                fallback
                            )
                    )
                );
        }

        // =====================================================
        // CRIAR PLANO + CHECKOUT
        // =====================================================

        private async Task<PlanoMercadoPagoResponse>
            CriarPlanoCheckoutMercadoPagoAsync(
                string externalReference)
        {
            var accessToken =
                ObterAccessToken();

            var baseUrl =
                ObterBaseUrl();

            var payload =
                new
                {
                    reason =
                        NOME_PLANO_MP,

                    external_reference =
                        externalReference,

                    auto_recurring =
                        new
                        {
                            frequency =
                                12,

                            frequency_type =
                                "months",

                            free_trial =
                                new
                                {
                                    frequency =
                                        DIAS_TRIAL,

                                    frequency_type =
                                        "days"
                                },

                            transaction_amount =
                                VALOR_PLANO,

                            currency_id =
                                "BRL"
                        },

                    back_url =
                            $"{baseUrl}/PagamentoClinica"
                };

            var client =
                _httpClientFactory
                    .CreateClient();

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    "https://api.mercadopago.com/preapproval_plan"
                );

            request
                .Headers
                .Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        accessToken
                    );

            request.Content =
                new StringContent(
                    JsonSerializer
                        .Serialize(
                            payload
                        ),
                    Encoding.UTF8,
                    "application/json"
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
                throw new Exception(
                    "Erro ao criar plano no Mercado Pago: " +
                    json
                );
            }

            var retorno =
                JsonSerializer
                    .Deserialize<
                        PlanoMercadoPagoResponse
                    >(json);

            if (retorno == null ||
                string.IsNullOrWhiteSpace(
                    retorno.Id))
            {
                throw new Exception(
                    "O Mercado Pago não retornou o ID do plano."
                );
            }

            if (string.IsNullOrWhiteSpace(
                retorno.InitPoint))
            {
                throw new Exception(
                    "O Mercado Pago não retornou o link de checkout do plano."
                );
            }

            return retorno;
        }

        // =====================================================
        // SINCRONIZAR ASSINATURA
        // =====================================================

        private async Task
            SincronizarAssinaturaAtualAsync()
        {
            if (Clinica == null)
            {
                return;
            }

            AssinaturaAtual =
                await _context
                    .AssinaturasProfissionais
                    .Where(
                        a =>
                            a.ClinicaId ==
                            Clinica.Id
                            &&
                            a.Status !=
                                "error"
                            &&
                            a.Status !=
                                "cancelled"
                            &&
                            a.Status !=
                                "canceled"
                    )
                    .OrderByDescending(
                        a =>
                            a.DataCriacaoUtc
                    )
                    .FirstOrDefaultAsync();

            if (AssinaturaAtual == null)
            {
                return;
            }

            /*
             * No fluxo pelo init_point do plano,
             * o preapproval só existe depois
             * que o usuário autoriza.
             */
            if (string.IsNullOrWhiteSpace(
                    AssinaturaAtual
                        .MercadoPagoPreapprovalId)
                &&
                !string.IsNullOrWhiteSpace(
                    AssinaturaAtual
                        .MercadoPagoPlanId))
            {
                var encontrada =
                    await BuscarAssinaturaPorPlanoAsync(
                        AssinaturaAtual
                            .MercadoPagoPlanId
                    );

                if (encontrada != null &&
                    !string.IsNullOrWhiteSpace(
                        encontrada.Id))
                {
                    AssinaturaAtual
                        .MercadoPagoPreapprovalId =
                            encontrada.Id;

                    if (!string.IsNullOrWhiteSpace(
                        encontrada.InitPoint))
                    {
                        AssinaturaAtual
                            .LinkAutorizacao =
                                encontrada.InitPoint;
                    }

                    await _context
                        .SaveChangesAsync();
                }
            }

            if (string.IsNullOrWhiteSpace(
                AssinaturaAtual
                    .MercadoPagoPreapprovalId))
            {
                return;
            }

            var retorno =
                await ConsultarAssinaturaMercadoPagoAsync(
                    AssinaturaAtual
                        .MercadoPagoPreapprovalId
                );

            if (retorno == null)
            {
                return;
            }

            await AplicarStatusAssinaturaAsync(
                AssinaturaAtual,
                retorno
            );
        }

        // =====================================================
        // BUSCAR ASSINATURA PELO PLAN ID
        // =====================================================

        private async Task<MercadoPagoPreapprovalResponse?>
            BuscarAssinaturaPorPlanoAsync(
                string planId)
        {
            var client =
                _httpClientFactory
                    .CreateClient();

            var url =
                "https://api.mercadopago.com/preapproval/search" +
                "?preapproval_plan_id=" +
                Uri.EscapeDataString(
                    planId
                );

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    url
                );

            request
                .Headers
                .Authorization =
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
                throw new Exception(
                    "Erro ao buscar assinatura pelo plano no Mercado Pago: " +
                    json
                );
            }

            var busca =
                JsonSerializer
                    .Deserialize<
                        MercadoPagoPreapprovalSearchResponse
                    >(json);

            if (busca?.Results == null ||
                busca.Results.Count == 0)
            {
                return null;
            }

            return busca
                .Results
                .OrderByDescending(
                    x =>
                        string.Equals(
                            x.Status,
                            "authorized",
                            StringComparison.OrdinalIgnoreCase
                        )
                )
                .ThenByDescending(
                    x =>
                        ConverterDataUtc(
                            x.DateCreated
                        )
                        ??
                        DateTime.MinValue
                )
                .FirstOrDefault();
        }

        // =====================================================
        // CONSULTAR ASSINATURA
        // =====================================================

        private async Task<MercadoPagoPreapprovalResponse?>
            ConsultarAssinaturaMercadoPagoAsync(
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

            request
                .Headers
                .Authorization =
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
                throw new Exception(
                    "Erro ao consultar assinatura no Mercado Pago: " +
                    json
                );
            }

            return JsonSerializer
                .Deserialize<
                    MercadoPagoPreapprovalResponse
                >(json);
        }

        // =====================================================
        // APLICAR STATUS
        // =====================================================

        private async Task
            AplicarStatusAssinaturaAsync(
                AssinaturaProfissional assinatura,
                MercadoPagoPreapprovalResponse retorno)
        {
            assinatura.MercadoPagoPreapprovalId =
                retorno.Id ??
                assinatura.MercadoPagoPreapprovalId;

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

            var proxima =
                ConverterDataUtc(
                    retorno.NextPaymentDate
                );

            if (proxima.HasValue)
            {
                assinatura.ProximaCobrancaUtc =
                    proxima;
            }

            var inicioMercadoPago =
                ConverterDataUtc(
                    retorno.DateCreated
                );

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

                assinatura.InicioTesteGratisUtc ??=
                    inicioMercadoPago ??
                    DateTime.UtcNow;

                if (!assinatura
                    .FimTesteGratisUtc
                    .HasValue)
                {
                    if (proxima.HasValue &&
                        proxima.Value >
                            assinatura
                                .InicioTesteGratisUtc
                                .Value
                        &&
                        proxima.Value <=
                            assinatura
                                .InicioTesteGratisUtc
                                .Value
                                .AddDays(15))
                    {
                        assinatura.FimTesteGratisUtc =
                            proxima.Value;
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

                if (Clinica != null)
                {
                    Clinica.Pago =
                        true;

                    Clinica.NomePlano =
                        "Plano Profissional Anual";

                    Clinica.ValorPlano =
                        VALOR_PLANO;

                    Clinica.MercadoPagoPlanoStatus =
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

                if (Clinica != null)
                {
                    Clinica.MercadoPagoPlanoStatus =
                        "paused";

                    Clinica.Pago =
                        TemAcessoVigente(
                            assinatura,
                            Clinica
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

                if (Clinica != null)
                {
                    Clinica.MercadoPagoPlanoStatus =
                        assinatura.Status;

                    Clinica.Pago =
                        TemAcessoVigente(
                            assinatura,
                            Clinica
                        );
                }
            }
            else
            {
                assinatura.Ativa =
                    false;

                if (Clinica != null)
                {
                    Clinica.MercadoPagoPlanoStatus =
                        assinatura.Status;
                }
            }

            await _context
                .SaveChangesAsync();
        }

        // =====================================================
        // ACCESS TOKEN
        // =====================================================

        private string ObterAccessToken()
        {
            var token =
                _config[
                    "MercadoPago:AccessToken"
                ];

            if (string.IsNullOrWhiteSpace(
                token))
            {
                throw new Exception(
                    "Access Token do Mercado Pago não configurado."
                );
            }

            return token;
        }

        // =====================================================
        // URL BASE
        // =====================================================

        private string ObterBaseUrl()
        {
            var baseUrl =
                _config[
                    "App:BaseUrl"
                ];

            if (string.IsNullOrWhiteSpace(
                baseUrl))
            {
                baseUrl =
                    $"{Request.Scheme}://{Request.Host}";
            }

            return baseUrl
                .TrimEnd('/');
        }

        // =====================================================
        // ACESSO VIGENTE
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

        // =====================================================
        // DATA
        // =====================================================

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
        // LIMITAR TEXTO
        // =====================================================

        private static string?
            LimitarTexto(
                string? texto,
                int max)
        {
            if (string.IsNullOrWhiteSpace(
                texto))
            {
                return texto;
            }

            return texto.Length <= max
                ? texto
                : texto[..max];
        }

        // =====================================================
        // DTO PLANO
        // =====================================================

        private class PlanoMercadoPagoResponse
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = "";

            [JsonPropertyName("init_point")]
            public string InitPoint { get; set; } = "";

            [JsonPropertyName("status")]
            public string? Status { get; set; }
        }

        // =====================================================
        // DTO BUSCA
        // =====================================================

        private class
            MercadoPagoPreapprovalSearchResponse
        {
            [JsonPropertyName("results")]
            public List<MercadoPagoPreapprovalResponse>
                Results
            {
                get;
                set;
            } = new();
        }

        // =====================================================
        // DTO ASSINATURA
        // =====================================================

        private class
            MercadoPagoPreapprovalResponse
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("preapproval_plan_id")]
            public string? PreapprovalPlanId { get; set; }

            [JsonPropertyName("external_reference")]
            public string? ExternalReference { get; set; }

            [JsonPropertyName("init_point")]
            public string? InitPoint { get; set; }

            [JsonPropertyName("status")]
            public string? Status { get; set; }

            [JsonPropertyName("next_payment_date")]
            public string? NextPaymentDate { get; set; }

            [JsonPropertyName("date_created")]
            public string? DateCreated { get; set; }
        }
    }
}