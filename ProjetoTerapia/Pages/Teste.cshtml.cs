using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProjetoTerapia.Models;
using System.Globalization;

namespace ProjetoTerapia.Pages
{
    public class TesteModel : PageModel
    {
        private readonly AppDbContext _context;


        // =====================================================
        // CHAVES DO RESULTADO TEMPORÁRIO
        // =====================================================

        private const string SessionNivel =
            "TesteResultadoNivel";

        private const string SessionScoreFinal =
            "TesteResultadoScoreFinal";

        private const string SessionAnsiedade =
            "TesteResultadoAnsiedade";

        private const string SessionDepressao =
            "TesteResultadoDepressao";

        private const string SessionMensagem =
            "TesteResultadoMensagem";

        private const string SessionRecomendacao =
            "TesteResultadoRecomendacao";

        private const string SessionResultadoSalvo =
            "TesteResultadoSalvo";


        // =====================================================
        // CONSTRUTOR
        // =====================================================

        public TesteModel(
            AppDbContext context)
        {
            _context = context;
        }


        // =====================================================
        // DADOS DA PÁGINA
        // =====================================================

        public bool PacienteLogado { get; set; }


        public string NomePaciente { get; set; } = "";


        public List<Pergunta> Perguntas { get; set; }
            = new();


        [BindProperty]
        public List<bool> Respostas { get; set; }
            = new();


        public bool MostrarResultado { get; set; }


        public string Nivel { get; set; } = "";


        public double ScoreFinal { get; set; }


        public double PorcentagemAnsiedade { get; set; }


        public double PorcentagemDepressao { get; set; }


        public string Mensagem { get; set; } = "";


        public string Recomendacao { get; set; } = "";


        public bool ResultadoSalvo { get; set; }


        // =====================================================
        // GET
        // =====================================================

        public async Task<IActionResult> OnGetAsync(
            bool resultado = false)
        {
            await CarregarPacienteLogadoAsync();


            var teste =
                new Teste();


            Perguntas =
                teste.Perguntas;


            /*
             * Se estamos entrando normalmente em /Teste,
             * significa que o usuário quer iniciar/refazer
             * o teste.
             */
            if (!resultado)
            {
                MostrarResultado = false;

                ResultadoSalvo = false;

                LimparResultadoDaSessao();

                return Page();
            }


            /*
             * Se chegamos em:
             *
             * /Teste?resultado=true
             *
             * carregamos o resultado calculado no POST.
             */
            var resultadoEncontrado =
                CarregarResultadoDaSessao();


            if (!resultadoEncontrado)
            {
                /*
                 * Caso alguém tente acessar diretamente
                 * ?resultado=true sem ter realizado o teste.
                 */

                return RedirectToPage(
                    "/Teste"
                );
            }


            MostrarResultado = true;


            return Page();
        }


        // =====================================================
        // POST
        // =====================================================

        public async Task<IActionResult> OnPostAsync()
        {
            var teste =
                new Teste();


            Perguntas =
                teste.Perguntas;


            // =================================================
            // VALIDAR RESPOSTAS
            // =================================================

            if (Respostas == null ||
                Respostas.Count != Perguntas.Count)
            {
                await CarregarPacienteLogadoAsync();


                MostrarResultado = false;


                ModelState.AddModelError(
                    "",
                    "Responda todas as perguntas antes de finalizar o teste."
                );


                return Page();
            }


            // =================================================
            // CALCULAR PONTUAÇÕES
            // =================================================

            int ansiedade = 0;

            int depressao = 0;

            int maxAnsiedade = 0;

            int maxDepressao = 0;


            for (int i = 0;
                 i < Perguntas.Count;
                 i++)
            {
                int pontos =
                    Respostas[i]
                        ? 10
                        : 0;


                if (Perguntas[i].Tipo == "A")
                {
                    ansiedade += pontos;

                    maxAnsiedade += 10;
                }
                else
                {
                    depressao += pontos;

                    maxDepressao += 10;
                }
            }


            // =================================================
            // PORCENTAGENS
            // =================================================

            PorcentagemAnsiedade =
                maxAnsiedade > 0
                    ? Math.Round(
                        ansiedade * 100.0
                        / maxAnsiedade,
                        1
                    )
                    : 0;


            PorcentagemDepressao =
                maxDepressao > 0
                    ? Math.Round(
                        depressao * 100.0
                        / maxDepressao,
                        1
                    )
                    : 0;


            ScoreFinal =
                Math.Round(
                    (
                        PorcentagemAnsiedade
                        +
                        PorcentagemDepressao
                    )
                    / 2.0,
                    1
                );


            // =================================================
            // CLASSIFICAÇÃO
            // =================================================

            DefinirClassificacao();


            // =================================================
            // SALVAR NO HISTÓRICO DO PACIENTE
            // =================================================

            ResultadoSalvo =
                await SalvarResultadoSePacienteLogadoAsync(
                    ansiedade,
                    depressao
                );


            // =================================================
            // GUARDAR RESULTADO TEMPORARIAMENTE
            // =================================================

            SalvarResultadoNaSessao();


            /*
             * =================================================
             * POST → REDIRECT → GET
             * =================================================
             *
             * ESSA É A PARTE QUE CORRIGE O ERR_CACHE_MISS.
             *
             * O navegador não fica mais parado em uma resposta
             * de POST.
             */

            return RedirectToPage(
                "/Teste",
                new
                {
                    resultado = true
                }
            );
        }


        // =====================================================
        // CLASSIFICAÇÃO
        // =====================================================

        private void DefinirClassificacao()
        {
            if (ScoreFinal >= 80)
            {
                Nivel =
                    "Muito Alto";


                Mensagem =
                    "Seu resultado indica sinais emocionais intensos que merecem atenção prioritária.";


                Recomendacao =
                    "Recomendamos buscar apoio profissional o quanto antes para avaliar sua situação e iniciar um acompanhamento adequado.";


                return;
            }


            if (ScoreFinal >= 60)
            {
                Nivel =
                    "Alto";


                Mensagem =
                    "Seu resultado sugere sinais importantes de sofrimento emocional.";


                Recomendacao =
                    "Recomendamos iniciar um acompanhamento profissional o quanto antes para melhorar seu bem-estar emocional e evitar que os sintomas se intensifiquem.";


                return;
            }


            if (ScoreFinal >= 40)
            {
                Nivel =
                    "Moderado";


                Mensagem =
                    "Alguns sinais emocionais merecem atenção.";


                Recomendacao =
                    "Buscar orientação especializada agora pode ajudar a evitar que esses sinais evoluam e melhorar sua qualidade de vida.";


                return;
            }


            if (ScoreFinal >= 25)
            {
                Nivel =
                    "Levemente Moderado";


                Mensagem =
                    "Seu resultado demonstra sinais leves de atenção emocional.";


                Recomendacao =
                    "Pequenas mudanças na rotina e o suporte adequado podem ajudar a preservar seu equilíbrio emocional.";


                return;
            }


            Nivel =
                "Leve";


            Mensagem =
                "Seu resultado indica poucos sinais de sofrimento emocional no momento.";


            Recomendacao =
                "Continue mantendo hábitos saudáveis, autocuidado e acompanhamento preventivo para preservar seu bem-estar.";
        }


        // =====================================================
        // SALVAR RESULTADO NO BANCO
        // =====================================================

        private async Task<bool>
            SalvarResultadoSePacienteLogadoAsync(
                int ansiedade,
                int depressao)
        {
            var pacienteIdSessao =
                HttpContext.Session.GetString(
                    "PacienteLogado"
                );


            if (string.IsNullOrWhiteSpace(
                pacienteIdSessao))
            {
                return false;
            }


            if (!int.TryParse(
                pacienteIdSessao,
                out int pacienteId))
            {
                return false;
            }


            var pacienteExiste =
                await _context.Pacientes
                    .AsNoTracking()
                    .AnyAsync(
                        p =>
                            p.Id == pacienteId
                    );


            if (!pacienteExiste)
            {
                return false;
            }


            var resultadoFinal =
                DefinirResultadoFinal();


            var resultado =
                new ResultadoTestePaciente
                {
                    PacienteId =
                        pacienteId,

                    PontuacaoAnsiedade =
                        ansiedade,

                    PontuacaoDepressao =
                        depressao,

                    PercentualAnsiedade =
                        Convert.ToDecimal(
                            PorcentagemAnsiedade
                        ),

                    PercentualDepressao =
                        Convert.ToDecimal(
                            PorcentagemDepressao
                        ),

                    ResultadoFinal =
                        resultadoFinal,

                    Nivel =
                        Nivel,

                    Mensagem =
                        Mensagem,

                    DataResultado =
                        DateTime.Now
                };


            _context
                .ResultadosTestePacientes
                .Add(
                    resultado
                );


            await _context.SaveChangesAsync();


            return true;
        }


        // =====================================================
        // DEFINIR RESULTADO FINAL
        // =====================================================

        private string DefinirResultadoFinal()
        {
            if (ScoreFinal < 25)
            {
                return
                    "Bem-estar preservado";
            }


            var diferenca =
                Math.Abs(
                    PorcentagemAnsiedade
                    -
                    PorcentagemDepressao
                );


            if (PorcentagemAnsiedade >= 40 &&
                PorcentagemDepressao >= 40 &&
                diferenca <= 20)
            {
                return
                    "Sinais mistos";
            }


            if (PorcentagemAnsiedade >
                PorcentagemDepressao)
            {
                return
                    "Sinais predominantes de ansiedade";
            }


            if (PorcentagemDepressao >
                PorcentagemAnsiedade)
            {
                return
                    "Sinais predominantes de depressão";
            }


            return
                "Atenção emocional";
        }


        // =====================================================
        // SALVAR RESULTADO NA SESSÃO
        // =====================================================

        private void SalvarResultadoNaSessao()
        {
            HttpContext.Session.SetString(
                SessionNivel,
                Nivel
            );


            HttpContext.Session.SetString(
                SessionScoreFinal,
                ScoreFinal.ToString(
                    CultureInfo.InvariantCulture
                )
            );


            HttpContext.Session.SetString(
                SessionAnsiedade,
                PorcentagemAnsiedade.ToString(
                    CultureInfo.InvariantCulture
                )
            );


            HttpContext.Session.SetString(
                SessionDepressao,
                PorcentagemDepressao.ToString(
                    CultureInfo.InvariantCulture
                )
            );


            HttpContext.Session.SetString(
                SessionMensagem,
                Mensagem
            );


            HttpContext.Session.SetString(
                SessionRecomendacao,
                Recomendacao
            );


            HttpContext.Session.SetString(
                SessionResultadoSalvo,
                ResultadoSalvo
                    ? "true"
                    : "false"
            );
        }


        // =====================================================
        // CARREGAR RESULTADO DA SESSÃO
        // =====================================================

        private bool CarregarResultadoDaSessao()
        {
            var nivel =
                HttpContext.Session.GetString(
                    SessionNivel
                );


            var score =
                HttpContext.Session.GetString(
                    SessionScoreFinal
                );


            var ansiedade =
                HttpContext.Session.GetString(
                    SessionAnsiedade
                );


            var depressao =
                HttpContext.Session.GetString(
                    SessionDepressao
                );


            var mensagem =
                HttpContext.Session.GetString(
                    SessionMensagem
                );


            var recomendacao =
                HttpContext.Session.GetString(
                    SessionRecomendacao
                );


            if (string.IsNullOrWhiteSpace(nivel) ||
                string.IsNullOrWhiteSpace(score) ||
                string.IsNullOrWhiteSpace(ansiedade) ||
                string.IsNullOrWhiteSpace(depressao))
            {
                return false;
            }


            if (!double.TryParse(
                score,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var scoreFinal))
            {
                return false;
            }


            if (!double.TryParse(
                ansiedade,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var percentualAnsiedade))
            {
                return false;
            }


            if (!double.TryParse(
                depressao,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var percentualDepressao))
            {
                return false;
            }


            Nivel =
                nivel;


            ScoreFinal =
                scoreFinal;


            PorcentagemAnsiedade =
                percentualAnsiedade;


            PorcentagemDepressao =
                percentualDepressao;


            Mensagem =
                mensagem ?? "";


            Recomendacao =
                recomendacao ?? "";


            ResultadoSalvo =
                string.Equals(
                    HttpContext.Session.GetString(
                        SessionResultadoSalvo
                    ),
                    "true",
                    StringComparison.OrdinalIgnoreCase
                );


            return true;
        }


        // =====================================================
        // LIMPAR RESULTADO DA SESSÃO
        // =====================================================

        private void LimparResultadoDaSessao()
        {
            HttpContext.Session.Remove(
                SessionNivel
            );


            HttpContext.Session.Remove(
                SessionScoreFinal
            );


            HttpContext.Session.Remove(
                SessionAnsiedade
            );


            HttpContext.Session.Remove(
                SessionDepressao
            );


            HttpContext.Session.Remove(
                SessionMensagem
            );


            HttpContext.Session.Remove(
                SessionRecomendacao
            );


            HttpContext.Session.Remove(
                SessionResultadoSalvo
            );
        }


        // =====================================================
        // PACIENTE LOGADO
        // =====================================================

        private async Task CarregarPacienteLogadoAsync()
        {
            var pacienteIdSessao =
                HttpContext.Session.GetString(
                    "PacienteLogado"
                );


            if (string.IsNullOrWhiteSpace(
                pacienteIdSessao))
            {
                PacienteLogado = false;

                NomePaciente = "";

                return;
            }


            if (!int.TryParse(
                pacienteIdSessao,
                out int pacienteId))
            {
                PacienteLogado = false;

                NomePaciente = "";

                return;
            }


            var paciente =
                await _context.Pacientes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id == pacienteId
                    );


            if (paciente == null)
            {
                PacienteLogado = false;

                NomePaciente = "";

                HttpContext.Session.Remove(
                    "PacienteLogado"
                );

                return;
            }


            PacienteLogado = true;


            NomePaciente =
                paciente.Nome ?? "";
        }
    }
}