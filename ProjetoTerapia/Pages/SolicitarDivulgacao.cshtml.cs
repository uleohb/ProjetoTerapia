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
    public class SolicitarDivulgacaoModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public SolicitarDivulgacaoModel(
            AppDbContext context,
            IHttpClientFactory httpClientFactory,
            IConfiguration config)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        public Clinica? Clinica { get; set; }

        public List<DivulgacaoRegional> MinhasSolicitacoes { get; set; } = new();

        // Lista completa que aparece na tela.
        // As cidades não liberadas ainda vão aparecer como "Em breve" no front.
        public List<string> CidadesDisponiveis { get; set; } = new()
        {
            "São Paulo",
            "Osasco",
            "Barueri",
            "Carapicuíba",
            "Cotia",
            "Taboão da Serra",
            "Embu das Artes",
            "Itapevi",
            "Jandira",
            "Santana de Parnaíba",
            "Guarulhos",
            "Santo André",
            "São Bernardo do Campo",
            "São Caetano do Sul",
            "Diadema",
            "Mauá",
            "Ribeirão Pires",
            "Mogi das Cruzes",
            "Suzano",
            "Poá",
            "Itaquaquecetuba",
            "Santos",
            "São Vicente",
            "Praia Grande",
            "Guarujá",
            "Cubatão",
            "Itanhaém",
            "Mongaguá",
            "Peruíbe"
        };

        // Cidades liberadas neste momento.
        // Só essas podem ser escolhidas e salvas.
        public List<string> CidadesLiberadasAgora { get; set; } = new()
        {
            "Osasco",
            "Barueri",
            "São Paulo"
        };

        [BindProperty]
        public int QuantidadeCidades { get; set; }

        [BindProperty]
        public List<string> CidadesEscolhidas { get; set; } = new();

        public IActionResult OnGet()
        {
            var id = HttpContext.Session.GetString("ClinicaLogada");

            if (id == null)
                return RedirectToPage("/LoginClinica");

            Clinica = _context.Clinicas.FirstOrDefault(c => c.Id == int.Parse(id));

            if (Clinica == null)
                return RedirectToPage("/LoginClinica");

            if (!Clinica.Pago)
                return RedirectToPage("/PagamentoClinica");

            if (string.IsNullOrWhiteSpace(Clinica.Cidade))
            {
                TempData["Erro"] = "Antes de solicitar divulgação regional, preencha sua cidade principal no perfil profissional.";
                return RedirectToPage("/CadastroClinica");
            }

            CarregarSolicitacoes(Clinica.Id);

            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            var id = HttpContext.Session.GetString("ClinicaLogada");

            if (id == null)
                return RedirectToPage("/LoginClinica");

            Clinica = _context.Clinicas.FirstOrDefault(c => c.Id == int.Parse(id));

            if (Clinica == null)
                return RedirectToPage("/LoginClinica");

            if (!Clinica.Pago)
                return RedirectToPage("/PagamentoClinica");

            if (string.IsNullOrWhiteSpace(Clinica.Cidade))
            {
                TempData["Erro"] = "Antes de solicitar divulgação regional, preencha sua cidade principal no perfil profissional.";
                return RedirectToPage("/CadastroClinica");
            }

            var plano = ObterPlano(QuantidadeCidades);

            if (plano == null)
            {
                TempData["Erro"] = "Selecione um plano válido.";
                CarregarSolicitacoes(Clinica.Id);
                return Page();
            }

            // Evita selecionar plano maior do que a quantidade real de cidades liberadas agora.
            var limiteAtualDeCidadesAdicionais = ObterCidadesLiberadasAdicionais().Count;

            if (QuantidadeCidades > limiteAtualDeCidadesAdicionais)
            {
                TempData["Erro"] =
                    $"No momento, a divulgação regional está disponível apenas para Osasco, Barueri e São Paulo. " +
                    $"Para o seu perfil, você pode escolher até {limiteAtualDeCidadesAdicionais} cidade(s) adicional(is).";

                CarregarSolicitacoes(Clinica.Id);
                return Page();
            }

            var cidades = LimparCidadesSelecionadas();

            if (!cidades.Any())
            {
                TempData["Erro"] = "Selecione pelo menos uma cidade adicional.";
                CarregarSolicitacoes(Clinica.Id);
                return Page();
            }

            // Bloqueio real no backend.
            // Mesmo que alguém altere o HTML pelo navegador, não consegue salvar cidade em breve.
            var cidadesBloqueadas = cidades
                .Where(c => !CidadeLiberada(c))
                .ToList();

            if (cidadesBloqueadas.Any())
            {
                TempData["Erro"] = "No momento, a divulgação regional está disponível apenas para Osasco, Barueri e São Paulo.";
                CarregarSolicitacoes(Clinica.Id);
                return Page();
            }

            if (cidades.Any(c => TextosIguais(c, Clinica.Cidade)))
            {
                TempData["Erro"] = "Sua cidade principal já está inclusa no plano. Escolha apenas cidades adicionais.";
                CarregarSolicitacoes(Clinica.Id);
                return Page();
            }

            if (cidades.Count > QuantidadeCidades)
            {
                TempData["Erro"] = $"Este plano permite até {QuantidadeCidades} cidade(s) adicional(is).";
                CarregarSolicitacoes(Clinica.Id);
                return Page();
            }

            var divulgacao = new DivulgacaoRegional
            {
                ClinicaId = Clinica.Id,
                NomePlano = plano.Value.Nome,
                QuantidadeCidades = QuantidadeCidades,
                Valor = plano.Value.Valor,
                CidadesSelecionadas = string.Join(", ", cidades),
                Pago = false,
                Aprovado = false,
                Ativo = false,
                DataSolicitacao = DateTime.Now,
                Status = "Aguardando pagamento",
                MercadoPagoStatus = "pending"
            };

            _context.DivulgacoesRegionais.Add(divulgacao);
            _context.SaveChanges();

            try
            {
                await GerarPreferenciaMercadoPago(divulgacao);

                _context.SaveChanges();

                if (!string.IsNullOrWhiteSpace(divulgacao.LinkPagamento))
                {
                    return Redirect(divulgacao.LinkPagamento);
                }

                TempData["Sucesso"] = "Solicitação criada. O link de pagamento ficará disponível em instantes.";
            }
            catch
            {
                TempData["Erro"] = "Solicitação criada, mas não foi possível gerar o pagamento agora. Tente novamente em instantes ou fale com o suporte.";
            }

            return RedirectToPage("/SolicitarDivulgacao");
        }

        // Usado no front para saber se a cidade é a cidade principal da clínica.
        public bool EhCidadePrincipal(string cidade)
        {
            if (Clinica == null)
                return false;

            return TextosIguais(cidade, Clinica.Cidade);
        }

        // Usado no front para manter marcada uma cidade após erro de validação.
        public bool CidadeSelecionada(string cidade)
        {
            return CidadesEscolhidas.Any(c => TextosIguais(c, cidade));
        }

        // Usado no front para liberar ou bloquear o card da cidade.
        public bool CidadeLiberada(string cidade)
        {
            return CidadesLiberadasAgora.Any(c => TextosIguais(c, cidade));
        }

        // Usado no front para mostrar o selo "Em breve".
        public bool CidadeEmBreve(string cidade)
        {
            return !CidadeLiberada(cidade);
        }

        // Retorna só as cidades liberadas que não são a cidade principal da clínica.
        private List<string> ObterCidadesLiberadasAdicionais()
        {
            if (Clinica == null)
                return CidadesLiberadasAgora;

            return CidadesLiberadasAgora
                .Where(c => !TextosIguais(c, Clinica.Cidade))
                .ToList();
        }

        private async Task GerarPreferenciaMercadoPago(DivulgacaoRegional divulgacao)
        {
            var accessToken = _config["MercadoPago:AccessToken"];

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new Exception("Access token do Mercado Pago não configurado.");
            }

            var baseUrl = _config["App:BaseUrl"];

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                baseUrl = $"{Request.Scheme}://{Request.Host}";
            }

            var notificationUrl = _config["MercadoPago:NotificationUrl"];

            var preference = new Dictionary<string, object>
            {
                ["items"] = new[]
                {
                    new
                    {
                        id = $"divulgacao-{divulgacao.Id}",
                        title = $"AlinhaMente - {divulgacao.NomePlano}",
                        description = $"Divulgação regional: {divulgacao.CidadesSelecionadas}",
                        quantity = 1,
                        currency_id = "BRL",
                        unit_price = divulgacao.Valor
                    }
                },
                ["statement_descriptor"] = "ALINHAMENTE",
                ["external_reference"] = $"DIVULGACAO-{divulgacao.Id}",
                ["back_urls"] = new
                {
                    success = $"{baseUrl}/SolicitarDivulgacao?pagamento=sucesso",
                    pending = $"{baseUrl}/SolicitarDivulgacao?pagamento=pendente",
                    failure = $"{baseUrl}/SolicitarDivulgacao?pagamento=falha"
                },
                ["auto_return"] = "approved"
            };

            if (!string.IsNullOrWhiteSpace(notificationUrl))
            {
                preference["notification_url"] = notificationUrl;
            }

            var json = JsonSerializer.Serialize(preference);

            var client = _httpClientFactory.CreateClient();

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.mercadopago.com/checkout/preferences"
            );

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            request.Content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json"
            );

            using var response = await client.SendAsync(request);

            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Erro ao criar preferência Mercado Pago: " + responseJson);
            }

            var preferenceResponse = JsonSerializer.Deserialize<MercadoPagoPreferenceResponse>(
                responseJson
            );

            if (preferenceResponse == null ||
                string.IsNullOrWhiteSpace(preferenceResponse.Id))
            {
                throw new Exception("Resposta inválida do Mercado Pago.");
            }

            divulgacao.MercadoPagoPreferenceId = preferenceResponse.Id;

            divulgacao.LinkPagamento =
                preferenceResponse.InitPoint ??
                preferenceResponse.SandboxInitPoint;

            divulgacao.Status = "Aguardando pagamento";
            divulgacao.MercadoPagoStatus = "pending";
        }

        private List<string> LimparCidadesSelecionadas()
        {
            var cidadesLimpas = new List<string>();
            var usadas = new HashSet<string>();

            foreach (var cidade in CidadesEscolhidas)
            {
                if (string.IsNullOrWhiteSpace(cidade))
                    continue;

                var cidadeTratada = cidade.Trim();

                // Aqui a gente busca o nome oficial da cidade dentro da lista.
                // Exemplo: se vier "sao paulo", salva como "São Paulo".
                var cidadeOficial = CidadesDisponiveis
                    .FirstOrDefault(c => TextosIguais(c, cidadeTratada));

                if (string.IsNullOrWhiteSpace(cidadeOficial))
                    continue;

                var chave = NormalizarTexto(cidadeOficial);

                if (usadas.Add(chave))
                {
                    cidadesLimpas.Add(cidadeOficial);
                }
            }

            return cidadesLimpas;
        }

        private void CarregarSolicitacoes(int clinicaId)
        {
            var solicitacoes = _context.DivulgacoesRegionais
                .Where(d => d.ClinicaId == clinicaId)
                .OrderByDescending(d => d.DataSolicitacao)
                .ToList();

            foreach (var item in solicitacoes)
            {
                var expirou = !item.Pago &&
                              !item.Aprovado &&
                              item.Status != "Cancelado" &&
                              item.Status != "Expirado" &&
                              item.DataSolicitacao.AddHours(72) < DateTime.Now;

                if (expirou)
                {
                    item.Status = "Expirado";
                    item.Ativo = false;
                }
            }

            _context.SaveChanges();

            MinhasSolicitacoes = solicitacoes;
        }

        private (string Nome, decimal Valor)? ObterPlano(int quantidade)
        {
            return quantidade switch
            {
                1 => ("+1 cidade adicional", 30),
                2 => ("+2 cidades adicionais", 35),
                3 => ("+3 cidades adicionais", 35),
                5 => ("+5 cidades adicionais", 40),
                8 => ("+8 cidades adicionais", 45),
                10 => ("+10 cidades adicionais", 50),
                15 => ("+15 cidades adicionais", 55),
                20 => ("+20 cidades adicionais", 60),
                25 => ("+25 cidades adicionais", 65),
                27 => ("+27 cidades adicionais", 70),
                _ => null
            };
        }

        private bool TextosIguais(string? textoA, string? textoB)
        {
            return NormalizarTexto(textoA ?? "") == NormalizarTexto(textoB ?? "");
        }

        private string NormalizarTexto(string texto)
        {
            var textoMinusculo = texto.ToLower().Trim();

            var textoNormalizado = textoMinusculo.Normalize(NormalizationForm.FormD);

            var caracteres = textoNormalizado
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .ToArray();

            return new string(caracteres).Normalize(NormalizationForm.FormC);
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