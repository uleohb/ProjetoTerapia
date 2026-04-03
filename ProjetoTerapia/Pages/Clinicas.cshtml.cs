using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace ProjetoTerapia.Pages
{
    public class ClinicasModel : PageModel
    {
        public List<string> Clinicas { get; set; }

        public void OnGet()
        {
            Clinicas = new List<string>
            {
                "Clínica Vida Mental",
                "Centro Psicológico Equilíbrio",
                "Instituto Bem-Estar",
                "Clínica Mente Saudável",
                "Espaço Terapêutico Viver"
            };
        }
    }
}