namespace ProjetoTerapia.Models
{
    public class RecuperacaoSenhaPaciente
    {
        public int Id { get; set; }

        public int PacienteId { get; set; }

        public Paciente? Paciente { get; set; }

        public string Token { get; set; } = "";

        public DateTime Expiracao { get; set; }

        public bool Usado { get; set; } = false;

        public DateTime DataCriacao { get; set; } = DateTime.Now;
    }
}