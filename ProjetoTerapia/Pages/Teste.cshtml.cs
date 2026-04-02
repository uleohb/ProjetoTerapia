using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;
using System.Collections.Generic;

namespace ProjetoTerapia.Pages
{
    public class TesteModel : PageModel
    {
        public List<Pergunta> Perguntas { get; set; }

        [BindProperty]
        public List<bool> Respostas { get; set; }

        public string Resultado { get; set; }

        public void OnGet()
        {
            Teste teste = new Teste();
            Perguntas = teste.Perguntas;
        }

        public int PorcentagemAnsiedade { get; set; }
        public int PorcentagemDepressao { get; set; }
        public string Nivel { get; set; }
        public string Mensagem { get; set; }

        public void OnPost()
        {
            Teste teste = new Teste();
            Perguntas = teste.Perguntas;

            int ansiedade = 0;
            int depressao = 0;

            int maxAnsiedade = 0;
            int maxDepressao = 0;

            for (int i = 0; i < Perguntas.Count; i++)
            {
                bool respondeuSim = Respostas[i];

                int pontos = 0;

                if ((respondeuSim && Perguntas[i].SimSaudavel) ||
                    (!respondeuSim && !Perguntas[i].SimSaudavel))
                    pontos = 10;
                else
                    pontos = 5;

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

            int maior = Math.Max(PorcentagemAnsiedade, PorcentagemDepressao);

            if (maior < 40)
            {
                Nivel = "Baixo";
                Mensagem = "Você está bem. Continue se cuidando.";
            }
            else if (maior < 70)
            {
                Nivel = "Moderado";
                Mensagem = "Fique atento ao seu estado emocional.";
            }
            else
            {
                Nivel = "Alto";
                Mensagem = "Recomendamos procurar ajuda profissional.";
            }
        }
    }
}