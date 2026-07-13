using System;

namespace ProjetoTerapia.Models
{
    public class Consulta
    {
        public int Id { get; set; }

        public int ClinicaId { get; set; }
        public Clinica? Clinica { get; set; }

        public int? PacienteId { get; set; }
        public Paciente? Paciente { get; set; }

        public int? ResultadoTestePacienteId { get; set; }
        public ResultadoTestePaciente? ResultadoTestePaciente { get; set; }

        public string NomePaciente { get; set; } = "";

        public string EmailPaciente { get; set; } = "";

        public string TelefonePaciente { get; set; } = "";

        public DateTime? DataConsulta { get; set; }

        public string TipoAtendimento { get; set; } = "";

        public string Status { get; set; } = "Pendente";

        public string? Observacoes { get; set; }

        public string? AnotacoesProfissional { get; set; }

        public DateTime DataCriacao { get; set; } = DateTime.Now;
    }
}