using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ProjetoTerapia.Pages
{
    public class DepressaoModel : PageModel
    {
        public List<string> Perguntas = new List<string>
        {
            "Você se sente triste na maior parte do tempo?",
            "Você perdeu interesse em atividades que antes gostava?",
            "Você sente cansaço frequente?",
            "Você tem dificuldade de concentração?",
            "Você se sente sem esperança?",
            "Seu apetite mudou significativamente?",
            "Você dorme demais ou de menos?",
            "Você se sente inútil ou culpado?",
            "Você evita contato social?",
            "Você sente falta de motivação?",
            "Você chora com facilidade?",
            "Você sente vazio interior?",
            "Você tem dificuldade para tomar decisões?",
            "Você perdeu energia para tarefas simples?",
            "Você sente que nada faz sentido?"
        };

        public int Porcentagem { get; set; } = -1;

        public void OnPost(int[] respostas)
        {
            int pontos = respostas.Sum();
            int total = Perguntas.Count;

            Porcentagem = (int)((double)pontos / total * 100);

            // Classificação
            if (Porcentagem <= 25)
            {
                Nivel = "Baixo";
                Mensagem = "Você apresenta poucos sinais. Continue cuidando da sua saúde mental ??";
            }
            else if (Porcentagem <= 50)
            {
                Nivel = "Leve";
                Mensagem = "Há alguns sinais de ansiedade. Observe seus sintomas e considere apoio profissional.";
            }
            else if (Porcentagem <= 75)
            {
                Nivel = "Moderado";
                Mensagem = "Os sintomas são moderados. Buscar ajuda psicológica pode ser muito benéfico.";
            }
            else
            {
                Nivel = "Alto";
                Mensagem = "Os sintomas são elevados. Recomendamos procurar um profissional o quanto antes.";
            }
        }
        public string Mensagem { get; set; } = "";
        public string Nivel { get; set; } = "";
    }
}