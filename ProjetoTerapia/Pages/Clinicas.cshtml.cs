using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace ProjetoTerapia.Pages
{

    public class Clinica
    {
        public int Id { get; set; }

        public string Nome { get; set; }
        public string Descricao { get; set; }
        public decimal Valor { get; set; }
        public string Endereco { get; set; }
    }
    public class ClinicasModel : PageModel
    {
        public List<Clinica> Clinicas { get; set; } = new List<Clinica>();

        public void OnGet()
        {
            Clinicas = new List<Clinica>
    {
        new Clinica { Nome = "Clínica Vida Mental", Descricao = "Atendimento psicológico", Valor = 120, Endereco = "São Paulo" },
        new Clinica { Nome = "Centro Equilíbrio", Descricao = "Terapia especializada", Valor = 150, Endereco = "Osasco" }
    };
        }
    }


}