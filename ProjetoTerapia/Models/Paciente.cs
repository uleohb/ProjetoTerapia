using System.ComponentModel.DataAnnotations;

namespace ProjetoTerapia.Models
{
    public class Paciente
    {
        [Key]
        public int Id { get; set; }

        public int ClinicaId { get; set; }

        [Required]
        public string Nome { get; set; } = "";

        public string Telefone { get; set; } = "";

        public string Email { get; set; } = "";

        public string Observacoes { get; set; } = "";
    }
}