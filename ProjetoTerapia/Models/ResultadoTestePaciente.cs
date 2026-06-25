using System;

namespace ProjetoTerapia.Models
{
    public class ResultadoTestePaciente
    {
        public int Id { get; set; }

        public int PacienteId { get; set; }

        public Paciente? Paciente { get; set; }

        public int PontuacaoAnsiedade { get; set; }

        public int PontuacaoDepressao { get; set; }

        public decimal PercentualAnsiedade { get; set; }

        public decimal PercentualDepressao { get; set; }

        public string ResultadoFinal { get; set; } = "";

        public string Nivel { get; set; } = "";

        public string Mensagem { get; set; } = "";

        public DateTime DataResultado { get; set; } = DateTime.Now;
    }
}