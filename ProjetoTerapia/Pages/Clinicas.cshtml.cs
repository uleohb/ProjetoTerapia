using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;
using System.Collections.Generic;

namespace ProjetoTerapia.Pages
{
    public class ClinicasModel : PageModel
    {
        public List<Clinica> Clinicas { get; set; } = new List<Clinica>();

        public void OnGet()
        {
            Clinicas = BancoFake.Clinicas;
        }
    }
}