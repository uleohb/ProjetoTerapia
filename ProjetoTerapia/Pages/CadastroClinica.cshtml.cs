using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;

namespace ProjetoTerapia.Pages
{
    public class CadastroClinicaModel : PageModel
    {
        [BindProperty]
        public Clinica NovaClinica { get; set; } = new Clinica();

        public bool Sucesso { get; set; }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            BancoFake.Clinicas.Add(NovaClinica);

            return RedirectToPage("/Clinicas");
        }
    }
}