namespace ProjetoTerapia.Models
{
    public class Paciente
    {

        public int Id { get; set; }


        public string Nome { get; set; } = "";


        public string Email { get; set; } = "";


        public string SenhaHash { get; set; } = "";


        public DateTime DataCadastro { get; set; }
            = DateTime.Now;



        // vínculo com clínica
        public int? ClinicaId { get; set; }


        public Clinica? Clinica { get; set; }



        // registros de humor
        public List<RegistroHumor> RegistrosHumor { get; set; }
            = new();

    }
}