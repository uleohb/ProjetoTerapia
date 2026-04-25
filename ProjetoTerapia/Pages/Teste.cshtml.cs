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

        public int ScoreFinal { get; set; }

        public int PorcentagemAnsiedade { get; set; }

        public int PorcentagemDepressao { get; set; }

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

            PorcentagemAnsiedade = (ansiedade * 100) / maxAnsiedade;
            PorcentagemDepressao = (depressao * 100) / maxDepressao;

            ScoreFinal = (PorcentagemAnsiedade + PorcentagemDepressao) / 2;

            // NÍVEL ALTO
            if (ScoreFinal >= 70)
            {
                Nivel = "Alto";

                Mensagem = "Seu perfil emocional apresenta sinais importantes que merecem atenção imediata.";

                Recomendacao = "Recomendamos iniciar um acompanhamento profissional o quanto antes para melhorar seu bem-estar emocional e evitar que os sintomas se intensifiquem.";
            }

            // NÍVEL MODERADO
            else if (ScoreFinal >= 40)
            {
                Nivel = "Moderado";

                Mensagem = "Seu resultado mostra sinais de alerta que indicam a necessidade de maior atenção emocional.";

                Recomendacao = "Buscar orientação especializada agora pode ajudar a evitar que esses sinais evoluam e melhorar sua qualidade de vida.";
            }

            // NÍVEL BAIXO
            else
            {
                Nivel = "Baixo";

                Mensagem = "Seu resultado indica um bom equilíbrio emocional no momento.";

                Recomendacao = "Continue mantendo hábitos saudáveis, autocuidado e acompanhamento preventivo para preservar seu bem-estar.";
            }

            MostrarResultado = true;
        }
    }
}