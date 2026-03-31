using ProjetoTerapia.Models;
using System;
using System.Collections.Generic;

namespace ProjetoTerapia
{
    public class Teste
    {
        public List<Pergunta> Perguntas = new List<Pergunta>()
        {
            //ANSIEDADE 
            new Pergunta { Texto = "Seu emocional está sobrecarregado?", Tipo = "A", SimSaudavel = false },
            new Pergunta { Texto = "Sua mente nunca desliga?", Tipo = "A", SimSaudavel = false },
            new Pergunta { Texto = "Você sente que perdeu o controle dos seus pensamentos?", Tipo = "A", SimSaudavel = false },
            new Pergunta { Texto = "Seu humor oscila ao longo do dia?", Tipo = "A", SimSaudavel = false },
            new Pergunta { Texto = "Você sente ansiedade sem motivo aparente?", Tipo = "A", SimSaudavel = false },
            new Pergunta { Texto = "Você tem dificuldade para dormir por causa de pensamentos?", Tipo = "A", SimSaudavel = false },
            new Pergunta { Texto = "Você sente seu coração acelerado sem motivo?", Tipo = "A", SimSaudavel = false },
            new Pergunta { Texto = "Você se sente inquieto ou não consegue relaxar?", Tipo = "A", SimSaudavel = false },
            new Pergunta { Texto = "Você se preocupa excessivamente com situações do dia a dia?", Tipo = "A", SimSaudavel = false },
            new Pergunta { Texto = "Você sente tensão no corpo com frequência?", Tipo = "A", SimSaudavel = false },

            //DEPRESSÃO
            new Pergunta { Texto = "Sua mente está constantemente cansada?", Tipo = "D", SimSaudavel = false },
            new Pergunta { Texto = "Você se sente cansado mesmo sem esforço físico?", Tipo = "D", SimSaudavel = false },
            new Pergunta { Texto = "Sua memória ou concentração já não são as mesmas?", Tipo = "D", SimSaudavel = false },
            new Pergunta { Texto = "Você sente que está no automático na maior parte do tempo?", Tipo = "D", SimSaudavel = false },
            new Pergunta { Texto = "Você sente dificuldade de sentir prazer nas coisas?", Tipo = "D", SimSaudavel = false },
            new Pergunta { Texto = "Você sente que algo dentro de você não está bem, mas não sabe explicar?", Tipo = "D", SimSaudavel = false },
            new Pergunta { Texto = "Você perdeu o interesse em coisas que antes gostava?", Tipo = "D", SimSaudavel = false },
            new Pergunta { Texto = "Você se sente sem motivação para realizar tarefas simples?", Tipo = "D", SimSaudavel = false },
            new Pergunta { Texto = "Você se sente triste na maior parte do tempo?", Tipo = "D", SimSaudavel = false },
            new Pergunta { Texto = "Você prefere se isolar ao invés de interagir com outras pessoas?", Tipo = "D", SimSaudavel = false }
        };
    }
}