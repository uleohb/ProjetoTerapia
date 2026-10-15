using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProjetoTerapia.Models;
using System.Globalization;
using System.Text;

namespace ProjetoTerapia.Pages
{
    public class ClinicasModel : PageModel
    {
        private readonly AppDbContext _context;

        public ClinicasModel(AppDbContext context)
        {
            _context = context;
        }


        // =====================================================
        // DADOS DA TELA
        // =====================================================

        public List<Clinica> Clinicas { get; set; } = new();


        [BindProperty(SupportsGet = true)]
        public string Busca { get; set; } = "";


        [BindProperty(SupportsGet = true)]
        public string Perfil { get; set; } = "";


        [BindProperty(SupportsGet = true)]
        public string Cidade { get; set; } = "";


        [BindProperty(SupportsGet = true)]
        public string Atendimento { get; set; } = "";


        [BindProperty(SupportsGet = true)]
        public string Ordenar { get; set; } = "";


        public bool PacienteLogado { get; set; }


        // =====================================================
        // GET
        // =====================================================

        public async Task OnGetAsync()
        {
            PacienteLogado =
                !string.IsNullOrWhiteSpace(
                    HttpContext.Session.GetString(
                        "PacienteLogado"
                    )
                );


            /*
             * Usamos o RequestAborted para cancelar as consultas
             * caso o usuário feche a página antes do carregamento.
             */
            var cancellationToken =
                HttpContext.RequestAborted;


            /*
             * =================================================
             * CONSULTA BASE
             * =================================================
             *
             * AsNoTracking:
             *
             * Essa página é somente leitura.
             * Não precisamos que o Entity Framework monitore
             * alterações nessas clínicas.
             *
             * Isso reduz memória e processamento.
             */

            IQueryable<Clinica> query =
                _context.Clinicas
                    .AsNoTracking()
                    .Where(
                        c =>
                            c.Aprovado &&
                            c.Pago
                    );


            // =================================================
            // FILTRO DE ATENDIMENTO
            // =================================================

            if (!string.IsNullOrWhiteSpace(
                Atendimento))
            {
                var atendimento =
                    Atendimento.Trim();


                if (atendimento.Equals(
                    "online",
                    StringComparison.OrdinalIgnoreCase))
                {
                    query =
                        query.Where(
                            c => c.AtendimentoOnline
                        );
                }
                else if (atendimento.Equals(
                    "presencial",
                    StringComparison.OrdinalIgnoreCase))
                {
                    query =
                        query.Where(
                            c => c.AtendimentoPresencial
                        );
                }
            }


            // =================================================
            // FILTRO DE ESPECIALIDADE
            // =================================================

            if (!string.IsNullOrWhiteSpace(
                Perfil))
            {
                var perfil =
                    Perfil.Trim();


                /*
                 * Esse filtro já é executado pelo SQL,
                 * antes dos dados chegarem ao servidor web.
                 */

                query =
                    query.Where(
                        c =>
                            c.Especialidades != null &&
                            EF.Functions.Like(
                                c.Especialidades,
                                $"%{perfil}%"
                            )
                    );
            }


            // =================================================
            // BUSCA DAS CLÍNICAS
            // =================================================

            var clinicasAtivas =
                await query
                    .ToListAsync(
                        cancellationToken
                    );


            /*
             * =================================================
             * CIDADES EXTRAS
             * =================================================
             *
             * Antes sua página consultava
             * DivulgacoesRegionais SEMPRE.
             *
             * Isso gerava uma segunda consulta ao Azure SQL
             * mesmo quando a pessoa simplesmente abria
             * a lista de profissionais.
             *
             * Agora só buscamos essas informações quando
             * Busca ou Cidade realmente precisam delas.
             */

            var cidadesExtrasPorClinica =
                new Dictionary<int, List<string>>();


            var precisaConsultarDivulgacoes =
                !string.IsNullOrWhiteSpace(Busca) ||
                !string.IsNullOrWhiteSpace(Cidade);


            if (precisaConsultarDivulgacoes &&
                clinicasAtivas.Count > 0)
            {
                var hoje =
                    DateTime.Today;


                var idsClinicas =
                    clinicasAtivas
                        .Select(c => c.Id)
                        .ToList();


                /*
                 * Trazemos somente os campos necessários.
                 *
                 * Não precisamos carregar toda a entidade
                 * DivulgacaoRegional.
                 */

                var divulgacoesAtivas =
                    await _context
                        .DivulgacoesRegionais
                        .AsNoTracking()
                        .Where(
                            d =>
                                idsClinicas.Contains(
                                    d.ClinicaId
                                )
                                &&
                                d.Ativo
                                &&
                                d.Aprovado
                                &&
                                d.Pago
                                &&
                                d.Status == "Ativo"
                                &&
                                (
                                    !d.DataInicio.HasValue ||
                                    d.DataInicio.Value.Date <= hoje
                                )
                                &&
                                (
                                    !d.DataFim.HasValue ||
                                    d.DataFim.Value.Date >= hoje
                                )
                        )
                        .Select(
                            d => new
                            {
                                d.ClinicaId,
                                d.CidadesSelecionadas
                            }
                        )
                        .ToListAsync(
                            cancellationToken
                        );


                /*
                 * Transformamos as divulgações em um mapa:
                 *
                 * ClinicaId -> cidades extras
                 *
                 * Assim não precisamos ficar fazendo
                 * Where + Split repetidamente para cada
                 * profissional.
                 */

                foreach (var divulgacao
                    in divulgacoesAtivas)
                {
                    var cidades =
                        (divulgacao.CidadesSelecionadas ?? "")
                            .Split(
                                new[]
                                {
                                    ',',
                                    ';',
                                    '\n',
                                    '\r'
                                },
                                StringSplitOptions
                                    .RemoveEmptyEntries
                            )
                            .Select(
                                cidade =>
                                    cidade.Trim()
                            )
                            .Where(
                                cidade =>
                                    !string.IsNullOrWhiteSpace(
                                        cidade
                                    )
                            )
                            .ToList();


                    if (cidades.Count == 0)
                    {
                        continue;
                    }


                    if (!cidadesExtrasPorClinica
                        .TryGetValue(
                            divulgacao.ClinicaId,
                            out var listaExistente))
                    {
                        listaExistente =
                            new List<string>();

                        cidadesExtrasPorClinica[
                            divulgacao.ClinicaId
                        ] = listaExistente;
                    }


                    listaExistente.AddRange(
                        cidades
                    );
                }
            }


            // =================================================
            // BUSCA POR TEXTO
            // =================================================

            if (!string.IsNullOrWhiteSpace(
                Busca))
            {
                var buscaNormalizada =
                    NormalizarTexto(
                        Busca.Trim()
                    );


                clinicasAtivas =
                    clinicasAtivas
                        .Where(
                            c =>
                                TextoContemNormalizado(
                                    c.Nome,
                                    buscaNormalizada
                                )
                                ||
                                TextoContemNormalizado(
                                    c.Descricao,
                                    buscaNormalizada
                                )
                                ||
                                TextoContemNormalizado(
                                    c.Endereco,
                                    buscaNormalizada
                                )
                                ||
                                TextoContemNormalizado(
                                    c.Cidade,
                                    buscaNormalizada
                                )
                                ||
                                TextoContemNormalizado(
                                    c.Especialidades,
                                    buscaNormalizada
                                )
                                ||
                                TemCidadeExtraAtiva(
                                    c.Id,
                                    buscaNormalizada,
                                    cidadesExtrasPorClinica
                                )
                        )
                        .ToList();
            }


            // =================================================
            // FILTRO POR CIDADE
            // =================================================

            if (!string.IsNullOrWhiteSpace(
                Cidade))
            {
                var cidadeNormalizada =
                    NormalizarTexto(
                        Cidade.Trim()
                    );


                clinicasAtivas =
                    clinicasAtivas
                        .Where(
                            c =>
                                TextoContemNormalizado(
                                    c.Cidade,
                                    cidadeNormalizada
                                )
                                ||
                                TextoContemNormalizado(
                                    c.Endereco,
                                    cidadeNormalizada
                                )
                                ||
                                TemCidadeExtraAtiva(
                                    c.Id,
                                    cidadeNormalizada,
                                    cidadesExtrasPorClinica
                                )
                        )
                        .ToList();
            }


            // =================================================
            // TERMO USADO PARA PRIORIZAR RESULTADOS
            // =================================================

            var cidadeParaPriorizar =
                !string.IsNullOrWhiteSpace(Cidade)
                    ? NormalizarTexto(
                        Cidade.Trim()
                    )
                    : !string.IsNullOrWhiteSpace(Busca)
                        ? NormalizarTexto(
                            Busca.Trim()
                        )
                        : "";


            // =================================================
            // ORDENAÇÃO
            // =================================================

            Clinicas =
                Ordenar?.Trim().ToLowerInvariant()
                switch
                {
                    "menor-preco" =>
                        clinicasAtivas
                            .OrderBy(
                                c => c.Valor
                            )
                            .ThenBy(
                                c => c.Nome
                            )
                            .ToList(),


                    "maior-preco" =>
                        clinicasAtivas
                            .OrderByDescending(
                                c => c.Valor
                            )
                            .ThenBy(
                                c => c.Nome
                            )
                            .ToList(),


                    "nome" =>
                        clinicasAtivas
                            .OrderBy(
                                c => c.Nome
                            )
                            .ToList(),


                    _ =>
                        OrdenacaoPadrao(
                            clinicasAtivas,
                            cidadeParaPriorizar,
                            cidadesExtrasPorClinica
                        )
                };
        }


        // =====================================================
        // ORDENAÇÃO PADRÃO
        // =====================================================

        private List<Clinica> OrdenacaoPadrao(
            List<Clinica> clinicas,
            string cidadeParaPriorizar,
            Dictionary<int, List<string>> cidadesExtrasPorClinica)
        {
            /*
             * Quando não existe busca/cidade,
             * não faz sentido testar divulgação regional.
             *
             * Fazemos uma ordenação simples.
             */

            if (string.IsNullOrWhiteSpace(
                cidadeParaPriorizar))
            {
                return clinicas
                    .OrderByDescending(
                        c => c.AtendimentoOnline
                    )
                    .ThenBy(
                        c => c.Nome
                    )
                    .ToList();
            }


            /*
             * Quando existe cidade/busca,
             * priorizamos profissionais que possuem
             * divulgação regional para aquela localidade.
             */

            return clinicas
                .OrderByDescending(
                    c =>
                        TemCidadeExtraAtiva(
                            c.Id,
                            cidadeParaPriorizar,
                            cidadesExtrasPorClinica
                        )
                )
                .ThenByDescending(
                    c =>
                        TextoContemNormalizado(
                            c.Cidade,
                            cidadeParaPriorizar
                        )
                )
                .ThenByDescending(
                    c => c.AtendimentoOnline
                )
                .ThenBy(
                    c => c.Nome
                )
                .ToList();
        }


        // =====================================================
        // VERIFICAR CIDADE EXTRA
        // =====================================================

        private bool TemCidadeExtraAtiva(
            int clinicaId,
            string buscaNormalizada,
            Dictionary<int, List<string>> cidadesExtrasPorClinica)
        {
            if (string.IsNullOrWhiteSpace(
                buscaNormalizada))
            {
                return false;
            }


            if (!cidadesExtrasPorClinica
                .TryGetValue(
                    clinicaId,
                    out var cidades))
            {
                return false;
            }


            foreach (var cidade in cidades)
            {
                if (TextoContemNormalizado(
                    cidade,
                    buscaNormalizada))
                {
                    return true;
                }
            }


            return false;
        }


        // =====================================================
        // TEXTO CONTÉM
        // =====================================================

        private bool TextoContemNormalizado(
            string? texto,
            string buscaNormalizada)
        {
            if (string.IsNullOrWhiteSpace(
                    texto) ||
                string.IsNullOrWhiteSpace(
                    buscaNormalizada))
            {
                return false;
            }


            var textoNormalizado =
                NormalizarTexto(
                    texto
                );


            return textoNormalizado.Contains(
                buscaNormalizada,
                StringComparison.Ordinal
            );
        }


        // =====================================================
        // NORMALIZAR TEXTO
        // =====================================================

        private string NormalizarTexto(
            string texto)
        {
            if (string.IsNullOrWhiteSpace(
                texto))
            {
                return "";
            }


            var textoMinusculo =
                texto
                    .Trim()
                    .ToLowerInvariant();


            var textoDecomposto =
                textoMinusculo.Normalize(
                    NormalizationForm.FormD
                );


            var caracteres =
                textoDecomposto
                    .Where(
                        c =>
                            CharUnicodeInfo
                                .GetUnicodeCategory(c)
                            !=
                            UnicodeCategory
                                .NonSpacingMark
                    )
                    .ToArray();


            return new string(
                caracteres
            )
            .Normalize(
                NormalizationForm.FormC
            );
        }
    }
}