using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjetoTerapia.Models
{
    public class Consulta
    {
        [Key]
        public int Id { get; set; }

        public int ClinicaId { get; set; }

        [ForeignKey("ClinicaId")]
        public Clinica? Clinica { get; set; }

        [Required(ErrorMessage = "Informe o nome do paciente.")]
        public string NomePaciente { get; set; } = "";

        [Required(ErrorMessage = "Selecione o tipo de atendimento.")]
        public string TipoAtendimento { get; set; } = "";

        [Required(ErrorMessage = "Informe a data da consulta.")]
        public DateTime? DataConsulta { get; set; }

        public string Status { get; set; } = "Pendente";
    }
}