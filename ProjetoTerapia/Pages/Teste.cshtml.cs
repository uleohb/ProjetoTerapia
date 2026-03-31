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

        public void OnPost()
        {
            Teste teste = new Teste();
            Perguntas = teste.Perguntas;

            int ansiedade = 0;
            int depressao = 0;

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
                    ansiedade += pontos;
                else
                    depressao += pontos;
            }

            if (ansiedade > depressao)
                Resultado = "Você apresenta mais sinais de ansiedade.";
            else if (depressao > ansiedade)
                Resultado = "Você apresenta mais sinais de depressão.";
            else
                Resultado = "Você apresenta sinais equilibrados.";
        }
    }
}