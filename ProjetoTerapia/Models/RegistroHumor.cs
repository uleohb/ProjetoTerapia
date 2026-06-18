namespace ProjetoTerapia.Models
{
    public class RegistroHumor
    {
        public int Id { get; set; }


        public int PacienteId { get; set; }


        public Paciente Paciente { get; set; } = null!;


        public int Nota { get; set; }


        public string Observacao { get; set; } = "";


        public DateTime Data { get; set; }
            = DateTime.Now;
    }
}