using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ProjetoTerapia.Pages
{
    public class CadastroClinicaModel : PageModel
    {
        [BindProperty]
        public string Nome { get; set; } = "";

        [BindProperty]
        public string Descricao { get; set; } = "";

        [BindProperty]
        public string Endereco { get; set; } = "";

        [BindProperty]
        public decimal Valor { get; set; }

        public bool Sucesso { get; set; }

        public void OnGet()
        {
        }

        public void OnPost()
        {
           
            Sucesso = true;
        }
    }
}
