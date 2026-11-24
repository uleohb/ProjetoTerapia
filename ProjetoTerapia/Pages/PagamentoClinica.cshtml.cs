using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using ProjetoTerapia.Models;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

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

        private readonly IHttpClientFactory
            _httpClientFactory;

        private readonly IConfiguration _config;

        private readonly IWebHostEnvironment
            _environment;


        public PagamentoClinicaModel(
            AppDbContext context,
            IHttpClientFactory httpClientFactory,
            IConfiguration config,
            IWebHostEnvironment environment)
        {
            _context = context;

            _httpClientFactory =
                httpClientFactory;

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
        public bool AceitouTermosEPrivacidade
        {
            get;
            set;
        }


        [BindProperty]
        public bool AceitouCobrancaRecorrente
        {
            get;
            set;
        }


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

        public async Task<IActionResult>
            OnGetAsync()
        {
            if (!await CarregarClinicaAsync())
            {
                return RedirectToPage(
                    "/LoginClinica"
                );
            }


            /*
             * Quando o Mercado Pago devolve o
             * profissional para o AlinhaVIDA,
             * consultamos a API para confirmar
             * o status real da assinatura.
             */
            if (string.Equals(
                Request
                    .Query["assinatura"]
                    .ToString(),
                "retorno",
                StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    await
                        SincronizarAssinaturaAtualAsync();


                    if (AssinaturaAtiva)
                    {
                        TempData["Sucesso"] =
                            "Assinatura autorizada. Seus 7 dias gratuitos começaram.";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);


                    TempData["Erro"] =
                        "O Mercado Pago retornou ao AlinhaVIDA, mas ainda não foi possível confirmar a assinatura.";
                }
            }


            await CarregarAssinaturaAtualAsync();


            return Page();
        }


        // =====================================================
        // INICIAR ASSINATURA
        // =====================================================

        public async Task<IActionResult>
            OnPostPagarAsync()
        {
            if (!await CarregarClinicaAsync() ||
                Clinica == null)
            {
                return RedirectToPage(
                    "/LoginClinica"
                );
            }


            // =================================================
            // PRECISA ESTAR APROVADO
            // =================================================

            if (!Clinica.Aprovado)
            {
                TempData["Erro"] =
                    "Seu cadastro precisa ser aprovado antes de ativar o plano.";


                return RedirectToPage(
                    "/PagamentoClinica"
                );
            }


            await CarregarAssinaturaAtualAsync();


            // =================================================
            // JÁ POSSUI ASSINATURA ATIVA
            // =================================================

            if (AssinaturaAtiva)
            {
                TempData["Erro"] =
                    "Sua assinatura recorrente já está ativa.";


                return RedirectToPage(
                    "/PagamentoClinica"
                );
            }


            // =================================================
            // JÁ EXISTE CHECKOUT PENDENTE
            // =================================================

            /*
             * Se já criamos uma assinatura no
             * Mercado Pago e o profissional
             * ainda não concluiu a autorização,
             * reutilizamos o mesmo link.
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


            // =================================================
            // PLANO ANTIGO AINDA ATIVO
            // =================================================

            /*
             * Evita assinatura duplicada caso
             * o profissional ainda possua o
             * plano antigo ativo.
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


            // =================================================
            // TERMOS OBRIGATÓRIOS
            // =================================================

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
                // =============================================
                // REGISTRAR ACEITE
                // =============================================

                await RegistrarAceiteAsync(
                    Clinica
                );


                // =============================================
                // OBTER / CRIAR PLANO MERCADO PAGO
                // =============================================

                var planId =
                    await
                        ObterOuCriarPlanoMercadoPagoAsync();


                // =============================================
                // CRIAR REGISTRO INTERNO
                // =============================================

                var assinatura =
                    new AssinaturaProfissional
                    {
                        ClinicaId =
                            Clinica.Id,


                        ExternalReference =
                            $"ALINHAVIDA-CLINICA-{Clinica.Id}-{Guid.NewGuid():N}",


                        NomePlano =
                            "Plano Profissional Anual",


                        MercadoPagoPlanId =
                            planId,


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
                            DateTime.UtcNow
                    };


                _context
                    .AssinaturasProfissionais
                    .Add(
                        assinatura
                    );


                await _context
                    .SaveChangesAsync();


                // =============================================
                // CRIAR ASSINATURA NO MERCADO PAGO
                // =============================================

                try
                {
                    var retorno =
                        await
                            CriarAssinaturaMercadoPagoAsync(
                                Clinica,
                                assinatura,
                                planId
                            );


                    assinatura
                        .MercadoPagoPreapprovalId =
                            retorno.Id;


                    assinatura
                        .LinkAutorizacao =
                            retorno.InitPoint;


                    assinatura.Status =
                        string.IsNullOrWhiteSpace(
                            retorno.Status
                        )
                        ? "pending"
                        : retorno.Status;


                    assinatura
                        .DataAtualizacaoUtc =
                            DateTime.UtcNow;


                    await _context
                        .SaveChangesAsync();


                    if (string.IsNullOrWhiteSpace(
                        assinatura
                            .LinkAutorizacao))
                    {
                        throw new Exception(
                            "O Mercado Pago não retornou o link de autorização."
                        );
                    }


                    return Redirect(
                        assinatura
                            .LinkAutorizacao
                    );
                }
                catch (Exception ex)
                {
                    assinatura.Status =
                        "error";


                    assinatura.Ativa =
                        false;


                    assinatura
                        .DataAtualizacaoUtc =
                            DateTime.UtcNow;


                    assinatura.Observacao =
                        LimitarTexto(
                            ex.Message,
                            1000
                        );


                    await _context
                        .SaveChangesAsync();


                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);


                TempData["Erro"] =
                    "Não foi possível iniciar a assinatura agora. Confira a integração do Mercado Pago e tente novamente.";


                return RedirectToPage(
                    "/PagamentoClinica"
                );
            }
        }


        // =====================================================
        // CARREGAR CLÍNICA
        // =====================================================

        private async Task<bool>
            CarregarClinicaAsync()
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


            DiasRestantes =
                null;


            PlanoVencido =
                false;


            PlanoPertoDeVencer =
                false;


            if (Clinica
                .DataVencimento
                .HasValue)
            {
                DiasRestantes =
                    (int)Math.Ceiling(
                        (
                            Clinica
                                .DataVencimento
                                .Value
                                .Date
                            -
                            DateTime
                                .Today
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

        private async Task
            RegistrarAceiteAsync(
                Clinica clinica)
        {
            var hashTermos =
                await
                    CalcularHashArquivoAsync(
                        Path.Combine(
                            "Pages",
                            "TermosProfissional.cshtml"
                        ),
                        $"AlinhaVIDA-TermosProfissionais-{VERSAO_TERMOS}"
                    );


            var hashPrivacidade =
                await
                    CalcularHashArquivoAsync(
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
        // MERCADO PAGO
        // OBTER OU CRIAR PLANO
        // =====================================================

        private async Task<string>
            ObterOuCriarPlanoMercadoPagoAsync()
        {
            var accessToken =
                ObterAccessToken();


            /*
             * Caso futuramente você cadastre
             * manualmente um PlanoProfissionalId
             * nas configurações, ele terá prioridade.
             */
            var planoConfigurado =
                _config[
                    "MercadoPago:PlanoProfissionalId"
                ];


            if (!string.IsNullOrWhiteSpace(
                planoConfigurado))
            {
                return planoConfigurado;
            }


            var client =
                _httpClientFactory
                    .CreateClient();


            // =================================================
            // PROCURAR PLANO EXISTENTE
            // =================================================

            var busca =
                Uri.EscapeDataString(
                    NOME_PLANO_MP
                );


            using (
                var request =
                    new HttpRequestMessage(
                        HttpMethod.Get,
                        "https://api.mercadopago.com/preapproval_plan/search" +
                        $"?status=active&q={busca}"
                    )
            )
            {
                request
                    .Headers
                    .Authorization =
                        new AuthenticationHeaderValue(
                            "Bearer",
                            accessToken
                        );


                using var response =
                    await client
                        .SendAsync(
                            request
                        );


                var json =
                    await response
                        .Content
                        .ReadAsStringAsync();


                if (response
                    .IsSuccessStatusCode)
                {
                    var encontrado =
                        EncontrarPlanoCompativel(
                            json
                        );


                    if (!string.IsNullOrWhiteSpace(
                        encontrado))
                    {
                        return encontrado;
                    }
                }
            }


            // =================================================
            // CRIAR NOVO PLANO
            // =================================================

            var baseUrl =
                ObterBaseUrl();


            var payload =
                new
                {
                    reason =
                        NOME_PLANO_MP,


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
                        $"{baseUrl}/PagamentoClinica?assinatura=retorno"
                };


            using var createRequest =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    "https://api.mercadopago.com/preapproval_plan"
                );


            createRequest
                .Headers
                .Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        accessToken
                    );


            createRequest.Content =
                new StringContent(
                    JsonSerializer
                        .Serialize(
                            payload
                        ),
                    Encoding.UTF8,
                    "application/json"
                );


            using var createResponse =
                await client
                    .SendAsync(
                        createRequest
                    );


            var createJson =
                await createResponse
                    .Content
                    .ReadAsStringAsync();


            if (!createResponse
                .IsSuccessStatusCode)
            {
                throw new Exception(
                    "Erro ao criar plano no Mercado Pago: " +
                    createJson
                );
            }


            using var doc =
                JsonDocument.Parse(
                    createJson
                );


            var planId =
                ObterString(
                    doc.RootElement,
                    "id"
                );


            if (string.IsNullOrWhiteSpace(
                planId))
            {
                throw new Exception(
                    "O Mercado Pago não retornou o ID do plano."
                );
            }


            return planId;
        }


        // =====================================================
        // LOCALIZAR PLANO COMPATÍVEL
        // =====================================================

        private string?
            EncontrarPlanoCompativel(
                string json)
        {
            try
            {
                using var doc =
                    JsonDocument.Parse(
                        json
                    );


                if (!doc
                    .RootElement
                    .TryGetProperty(
                        "results",
                        out var results
                    ) ||
                    results.ValueKind !=
                        JsonValueKind.Array)
                {
                    return null;
                }


                foreach (
                    var item
                    in results
                        .EnumerateArray())
                {
                    var id =
                        ObterString(
                            item,
                            "id"
                        );


                    var reason =
                        ObterString(
                            item,
                            "reason"
                        );


                    var status =
                        ObterString(
                            item,
                            "status"
                        );


                    if (string.IsNullOrWhiteSpace(
                            id) ||
                        !string.Equals(
                            reason,
                            NOME_PLANO_MP,
                            StringComparison
                                .OrdinalIgnoreCase
                        ) ||
                        !string.Equals(
                            status,
                            "active",
                            StringComparison
                                .OrdinalIgnoreCase
                        ))
                    {
                        continue;
                    }


                    if (!item
                        .TryGetProperty(
                            "auto_recurring",
                            out var recurring
                        ))
                    {
                        continue;
                    }


                    if (ObterInt(
                            recurring,
                            "frequency"
                        ) != 12 ||
                        !string.Equals(
                            ObterString(
                                recurring,
                                "frequency_type"
                            ),
                            "months",
                            StringComparison
                                .OrdinalIgnoreCase
                        ) ||
                        ObterDecimal(
                            recurring,
                            "transaction_amount"
                        ) != VALOR_PLANO ||
                        !string.Equals(
                            ObterString(
                                recurring,
                                "currency_id"
                            ),
                            "BRL",
                            StringComparison
                                .OrdinalIgnoreCase
                        ))
                    {
                        continue;
                    }


                    if (!recurring
                        .TryGetProperty(
                            "free_trial",
                            out var trial
                        ))
                    {
                        continue;
                    }


                    if (ObterInt(
                            trial,
                            "frequency"
                        ) != DIAS_TRIAL ||
                        !string.Equals(
                            ObterString(
                                trial,
                                "frequency_type"
                            ),
                            "days",
                            StringComparison
                                .OrdinalIgnoreCase
                        ))
                    {
                        continue;
                    }


                    return id;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }


            return null;
        }


        // =====================================================
        // CRIAR ASSINATURA INDIVIDUAL
        // =====================================================

        private async Task<PreapprovalResponse>
            CriarAssinaturaMercadoPagoAsync(
                Clinica clinica,
                AssinaturaProfissional assinatura,
                string planId)
        {
            var accessToken =
                ObterAccessToken();


            if (string.IsNullOrWhiteSpace(
                clinica.Email))
            {
                throw new Exception(
                    "O profissional precisa possuir um e-mail válido."
                );
            }


            var payload =
                new
                {
                    preapproval_plan_id =
                        planId,


                    reason =
                        NOME_PLANO_MP,


                    external_reference =
                        assinatura
                            .ExternalReference,


                    payer_email =
                        clinica
                            .Email
                            .Trim(),


                    back_url =
                        $"{ObterBaseUrl()}/PagamentoClinica?assinatura=retorno"
                };


            var client =
                _httpClientFactory
                    .CreateClient();


            using var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    "https://api.mercadopago.com/preapproval"
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
                await client
                    .SendAsync(
                        request
                    );


            var json =
                await response
                    .Content
                    .ReadAsStringAsync();


            if (!response
                .IsSuccessStatusCode)
            {
                throw new Exception(
                    "Erro ao criar assinatura no Mercado Pago: " +
                    json
                );
            }


            using var doc =
                JsonDocument.Parse(
                    json
                );


            var id =
                ObterString(
                    doc.RootElement,
                    "id"
                );


            var initPoint =
                ObterString(
                    doc.RootElement,
                    "init_point"
                );


            var status =
                ObterString(
                    doc.RootElement,
                    "status"
                );


            if (string.IsNullOrWhiteSpace(
                id))
            {
                throw new Exception(
                    "O Mercado Pago não retornou o ID da assinatura."
                );
            }


            if (string.IsNullOrWhiteSpace(
                initPoint))
            {
                throw new Exception(
                    "O Mercado Pago não retornou o link da assinatura."
                );
            }


            return new PreapprovalResponse
            {
                Id =
                    id,


                InitPoint =
                    initPoint,


                Status =
                    status
            };
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
                            a.MercadoPagoPreapprovalId !=
                                null
                    )
                    .OrderByDescending(
                        a =>
                            a.DataCriacaoUtc
                    )
                    .FirstOrDefaultAsync();


            if (AssinaturaAtual == null ||
                string.IsNullOrWhiteSpace(
                    AssinaturaAtual
                        .MercadoPagoPreapprovalId
                ))
            {
                return;
            }


            var client =
                _httpClientFactory
                    .CreateClient();


            using var request =
                new HttpRequestMessage(
                    HttpMethod.Get,

                    "https://api.mercadopago.com/preapproval/" +

                    Uri.EscapeDataString(
                        AssinaturaAtual
                            .MercadoPagoPreapprovalId
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
                await client
                    .SendAsync(
                        request
                    );


            var json =
                await response
                    .Content
                    .ReadAsStringAsync();


            if (!response
                .IsSuccessStatusCode)
            {
                throw new Exception(
                    "Erro ao consultar assinatura: " +
                    json
                );
            }


            using var doc =
                JsonDocument.Parse(
                    json
                );


            var root =
                doc.RootElement;


            var status =
                ObterString(
                    root,
                    "status"
                )
                ??
                AssinaturaAtual
                    .Status;


            AssinaturaAtual.Status =
                status;


            AssinaturaAtual
                .DataAtualizacaoUtc =
                    DateTime.UtcNow;


            var proxima =
                ObterDateTimeUtc(
                    root,
                    "next_payment_date"
                );


            if (proxima.HasValue)
            {
                AssinaturaAtual
                    .ProximaCobrancaUtc =
                        proxima;
            }


            // =================================================
            // AUTORIZADA
            // =================================================

            if (string.Equals(
                status,
                "authorized",
                StringComparison.OrdinalIgnoreCase))
            {
                AssinaturaAtual.Ativa =
                    true;


                AssinaturaAtual
                    .DataAutorizacaoUtc ??=
                        DateTime.UtcNow;


                AssinaturaAtual
                    .InicioTesteGratisUtc ??=
                        DateTime.UtcNow;


                /*
                 * Se o Mercado Pago informar a
                 * próxima cobrança, usamos essa
                 * data como término do trial.
                 *
                 * Caso contrário, usamos +7 dias.
                 */
                AssinaturaAtual
                    .FimTesteGratisUtc ??=
                        proxima.HasValue
                        ? proxima.Value
                        : AssinaturaAtual
                            .InicioTesteGratisUtc
                            .Value
                            .AddDays(
                                DIAS_TRIAL
                            );


                /*
                 * Durante os 7 dias gratuitos,
                 * Clinica.Pago = true significa
                 * "acesso ao plano liberado".
                 *
                 * Ainda não significa que houve
                 * cobrança financeira.
                 *
                 * DataPagamento será atualizada
                 * posteriormente pelo webhook.
                 */
                Clinica.Pago =
                    true;


                Clinica.NomePlano =
                    "Plano Profissional Anual";


                Clinica.ValorPlano =
                    VALOR_PLANO;


                Clinica
                    .MercadoPagoPlanoStatus =
                        status;


                /*
                 * Campo legado mantido
                 * temporariamente.
                 */
                Clinica
                    .MercadoPagoPlanoPaymentId =
                        AssinaturaAtual
                            .MercadoPagoPreapprovalId;
            }
            else
            {
                AssinaturaAtual.Ativa =
                    false;
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
        // HELPERS JSON
        // =====================================================

        private static string?
            ObterString(
                JsonElement element,
                string nome)
        {
            if (!element
                .TryGetProperty(
                    nome,
                    out var prop
                ))
            {
                return null;
            }


            return prop.ValueKind
                switch
            {
                JsonValueKind.String =>
                    prop.GetString(),

                JsonValueKind.Number =>
                    prop.ToString(),

                _ =>
                    null
            };
        }


        private static int?
            ObterInt(
                JsonElement element,
                string nome)
        {
            if (!element
                .TryGetProperty(
                    nome,
                    out var prop
                ))
            {
                return null;
            }


            if (prop.ValueKind ==
                    JsonValueKind.Number &&
                prop.TryGetInt32(
                    out var valor
                ))
            {
                return valor;
            }


            if (prop.ValueKind ==
                    JsonValueKind.String &&
                int.TryParse(
                    prop.GetString(),
                    out valor
                ))
            {
                return valor;
            }


            return null;
        }


        private static decimal?
            ObterDecimal(
                JsonElement element,
                string nome)
        {
            if (!element
                .TryGetProperty(
                    nome,
                    out var prop
                ))
            {
                return null;
            }


            if (prop.ValueKind ==
                    JsonValueKind.Number &&
                prop.TryGetDecimal(
                    out var valor
                ))
            {
                return valor;
            }


            if (prop.ValueKind ==
                    JsonValueKind.String &&
                decimal.TryParse(
                    prop.GetString(),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out valor
                ))
            {
                return valor;
            }


            return null;
        }


        private static DateTime?
            ObterDateTimeUtc(
                JsonElement element,
                string nome)
        {
            var texto =
                ObterString(
                    element,
                    nome
                );


            if (string.IsNullOrWhiteSpace(
                texto))
            {
                return null;
            }


            if (DateTimeOffset.TryParse(
                texto,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var data
            ))
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
        // RETORNO MERCADO PAGO
        // =====================================================

        private class PreapprovalResponse
        {
            public string Id
            {
                get;
                set;
            } = "";


            public string InitPoint
            {
                get;
                set;
            } = "";


            public string? Status
            {
                get;
                set;
            }
        }
    }
}