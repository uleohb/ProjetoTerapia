using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjetoTerapia.Pages
{
    public class TesteModel : PageModel
    {
        private readonly AppDbContext _context;
        public bool PacienteLogado { get; set; }
        public string NomePaciente { get; set; } = "";
        public TesteModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Pergunta> Perguntas { get; set; } = new List<Pergunta>();

        [BindProperty]
        public List<bool> Respostas { get; set; } = new List<bool>();

        public bool MostrarResultado { get; set; }

        public string Nivel { get; set; } = "";

        public double ScoreFinal { get; set; }

        public double PorcentagemAnsiedade { get; set; }

        public double PorcentagemDepressao { get; set; }

        public string Mensagem { get; set; } = "";

        public string Recomendacao { get; set; } = "";

        public bool ResultadoSalvo { get; set; }

        public void OnGet()
        {
            CarregarPacienteLogado();

            Teste teste = new Teste();
            Perguntas = teste.Perguntas;

            MostrarResultado = false;
            ResultadoSalvo = false;
        }

        public IActionResult OnPost()
        {
            CarregarPacienteLogado();

            Teste teste = new Teste();
            Perguntas = teste.Perguntas;

            int ansiedade = 0;
            int depressao = 0;

            int maxAnsiedade = 0;
            int maxDepressao = 0;

            if (Respostas == null || Respostas.Count != Perguntas.Count)
            {
                MostrarResultado = false;
                return Page();
            }

            for (int i = 0; i < Perguntas.Count; i++)
            {
                int pontos = Respostas[i] ? 10 : 0;

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

            PorcentagemAnsiedade =
                Math.Round((ansiedade * 100.0) / maxAnsiedade, 1);

            PorcentagemDepressao =
                Math.Round((depressao * 100.0) / maxDepressao, 1);

            ScoreFinal = Math.Round((PorcentagemAnsiedade + PorcentagemDepressao) / 2.0, 1);

            if (ScoreFinal >= 80)
            {
                Nivel = "Muito Alto";

                Mensagem = "Seu resultado indica sinais emocionais intensos que merecem atenção prioritária.";

                Recomendacao = "Recomendamos buscar apoio profissional o quanto antes para avaliar sua situação e iniciar um acompanhamento adequado.";
            }
            else if (ScoreFinal >= 60)
            {
                Nivel = "Alto";

                Mensagem = "Seu resultado sugere sinais importantes de sofrimento emocional.";

                Recomendacao = "Recomendamos iniciar um acompanhamento profissional o quanto antes para melhorar seu bem-estar emocional e evitar que os sintomas se intensifiquem.";
            }
            else if (ScoreFinal >= 40)
            {
                Nivel = "Moderado";

                Mensagem = "Alguns sinais emocionais merecem atenção.";

                Recomendacao = "Buscar orientação especializada agora pode ajudar a evitar que esses sinais evoluam e melhorar sua qualidade de vida.";
            }
            else if (ScoreFinal >= 25)
            {
                Nivel = "Levemente Moderado";

                Mensagem = "Seu resultado demonstra sinais leves de atenção emocional.";

                Recomendacao = "Pequenas mudanças na rotina e o suporte adequado podem ajudar a preservar seu equilíbrio emocional.";
            }
            else
            {
                Nivel = "Leve";

                Mensagem = "Seu resultado indica poucos sinais de sofrimento emocional no momento.";

                Recomendacao = "Continue mantendo hábitos saudáveis, autocuidado e acompanhamento preventivo para preservar seu bem-estar.";
            }

            MostrarResultado = true;

            SalvarResultadoSePacienteLogado(ansiedade, depressao);

            return Page();
        }

        private void SalvarResultadoSePacienteLogado(int ansiedade, int depressao)
        {
            var pacienteIdSessao = HttpContext.Session.GetString("PacienteLogado");

            if (string.IsNullOrEmpty(pacienteIdSessao))
            {
                ResultadoSalvo = false;
                return;
            }

            if (!int.TryParse(pacienteIdSessao, out int pacienteId))
            {
                ResultadoSalvo = false;
                return;
            }

            var pacienteExiste = _context.Pacientes
                .Any(p => p.Id == pacienteId);

            if (!pacienteExiste)
            {
                ResultadoSalvo = false;
                return;
            }

            var resultadoFinal = DefinirResultadoFinal();

            var resultado = new ResultadoTestePaciente
            {
                PacienteId = pacienteId,

                PontuacaoAnsiedade = ansiedade,
                PontuacaoDepressao = depressao,

                PercentualAnsiedade = Convert.ToDecimal(PorcentagemAnsiedade),
                PercentualDepressao = Convert.ToDecimal(PorcentagemDepressao),

                ResultadoFinal = resultadoFinal,
                Nivel = Nivel,
                Mensagem = Mensagem,

                DataResultado = DateTime.Now
            };

            _context.ResultadosTestePacientes.Add(resultado);
            _context.SaveChanges();

            ResultadoSalvo = true;
        }

        private string DefinirResultadoFinal()
        {
            if (ScoreFinal < 25)
            {
                return "Bem-estar preservado";
            }

            var diferenca = Math.Abs(PorcentagemAnsiedade - PorcentagemDepressao);

            if (PorcentagemAnsiedade >= 40 && PorcentagemDepressao >= 40 && diferenca <= 20)
            {
                return "Sinais mistos";
            }

            if (PorcentagemAnsiedade > PorcentagemDepressao)
            {
                return "Sinais predominantes de ansiedade";
            }

            if (PorcentagemDepressao > PorcentagemAnsiedade)
            {
                return "Sinais predominantes de depressão";
            }

            return "Atenção emocional";
        }

        private void CarregarPacienteLogado()
        {
            var pacienteIdSessao = HttpContext.Session.GetString("PacienteLogado");

            if (string.IsNullOrEmpty(pacienteIdSessao))
            {
                PacienteLogado = false;
                NomePaciente = "";
                return;
            }

            if (!int.TryParse(pacienteIdSessao, out int pacienteId))
            {
                PacienteLogado = false;
                NomePaciente = "";
                return;
            }

            var paciente = _context.Pacientes
                .FirstOrDefault(x => x.Id == pacienteId);

            if (paciente == null)
            {
                PacienteLogado = false;
                NomePaciente = "";
                return;
            }

            PacienteLogado = true;
            NomePaciente = paciente.Nome;
        }

    }
}