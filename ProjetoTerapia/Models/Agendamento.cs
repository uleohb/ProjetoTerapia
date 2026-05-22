using System;

namespace ProjetoTerapia.Models
{
    public class Agendamento
    {
        public int Id { get; set; }

        public int ClinicaId { get; set; }

        public string NomePaciente { get; set; } = "";

        public string TelefonePaciente { get; set; } = "";

        public DateTime DataConsulta { get; set; }

        public string Modalidade { get; set; } = "";

        public string Status { get; set; } = "Agendado";

        public string? Observacoes { get; set; }

        public DateTime DataCriacao { get; set; } =
            DateTime.Now;
    }
}