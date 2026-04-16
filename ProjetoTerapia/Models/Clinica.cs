namespace ProjetoTerapia.Models
{
    public class Clinica
    {
        public int Id { get; set; }

        public string Nome { get; set; } = "";

        public string Descricao { get; set; } = "";

        public decimal Valor { get; set; }

        public string Endereco { get; set; } = "";

        public string Telefone { get; set; } = "";

        public bool Aprovado { get; set; } = false;

        public bool Pago { get; set; } = false;
    }
}
