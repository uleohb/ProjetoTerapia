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

        public string Resultado { get; set; } = "";

        public bool MostrarResultado { get; set; }

        public string Nivel { get; set; } = ""; 

        public void OnGet()
        {
            Teste teste = new Teste();
            Perguntas = teste.Perguntas;

            MostrarResultado = false;
        }

        public int PorcentagemAnsiedade { get; set; }
        public int PorcentagemDepressao { get; set; }
        
        public string Mensagem { get; set; } = "";

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

            bool ansiedadeAlta = PorcentagemAnsiedade >= 70;
            bool depressaoAlta = PorcentagemDepressao >= 70;

            bool ansiedadeModerada = PorcentagemAnsiedade >= 40;
            bool depressaoModerada = PorcentagemDepressao >= 40;

            // 🔴 NÍVEL ALTO
            if (ansiedadeAlta || depressaoAlta)
            {
                Nivel = "Alto";
                Mensagem = "Você apresenta sinais importantes que merecem atenção. Buscar ajuda profissional pode ser um passo importante.";
            }

            // 🟡 NÍVEL MODERADO
            else if (ansiedadeModerada || depressaoModerada)
            {
                Nivel = "Moderado";
                Mensagem = "Você apresenta alguns sinais de alerta. Vale a pena cuidar mais da sua saúde emocional.";
            }

            // 🟢 NÍVEL BAIXO
            else
            {
                Nivel = "Baixo";
                Mensagem = "Você aparenta estar bem. Continue se cuidando e mantendo hábitos saudáveis.";
            }

            MostrarResultado = true;
        }
    }
}