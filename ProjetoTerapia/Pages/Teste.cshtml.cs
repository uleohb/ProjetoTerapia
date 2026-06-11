using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;
using System.Collections.Generic;

namespace ProjetoTerapia.Pages
{
    public class TesteModel : PageModel
    {
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

        public void OnGet()
        {
            Teste teste = new Teste();
            Perguntas = teste.Perguntas;

            MostrarResultado = false;
        }

        public void OnPost()
        {
            Teste teste = new Teste();
            Perguntas = teste.Perguntas;

            int ansiedade = 0;
            int depressao = 0;

            int maxAnsiedade = 0;
            int maxDepressao = 0;

            if (Respostas == null || Respostas.Count != Perguntas.Count)
                return;

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

            // MUITO ALTO
            if (ScoreFinal >= 80)
            {
                Nivel = "Muito Alto";

                Mensagem = "Seu resultado indica sinais emocionais intensos que merecem atenção prioritária.";

                Recomendacao = "Recomendamos buscar apoio profissional o quanto antes para avaliar sua situação e iniciar um acompanhamento adequado.";
            }

            // ALTO
            else if (ScoreFinal >= 60)
            {
                Nivel = "Alto";

                Mensagem = "Seu resultado sugere sinais importantes de sofrimento emocional.";

                Recomendacao = "Recomendamos iniciar um acompanhamento profissional o quanto antes para melhorar seu bem-estar emocional e evitar que os sintomas se intensifiquem.";
            }

            // MODERADO
            else if (ScoreFinal >= 40)
            {
                Nivel = "Moderado";

                Mensagem = "Alguns sinais emocionais merecem atenção.";

                Recomendacao = "Buscar orientação especializada agora pode ajudar a evitar que esses sinais evoluam e melhorar sua qualidade de vida.";
            }

            // LEVEMENTE MODERADO
            else if (ScoreFinal >= 25)
            {
                Nivel = "Levemente Moderado";

                Mensagem = "Seu resultado demonstra sinais leves de atenção emocional.";

                Recomendacao = "Pequenas mudanças na rotina e o suporte adequado podem ajudar a preservar seu equilíbrio emocional.";
            }

            // LEVE
            else
            {
                Nivel = "Leve";

                Mensagem = "Seu resultado indica poucos sinais de sofrimento emocional no momento.";

                Recomendacao = "Continue mantendo hábitos saudáveis, autocuidado e acompanhamento preventivo para preservar seu bem-estar.";
            }

            MostrarResultado = true;
        }
    }
}