using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;

namespace ProjetoTerapia.Pages
{

    public class CadastroClinicaModel : PageModel
    {
        private readonly AppDbContext _context;

        public CadastroClinicaModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Clinica NovaClinica { get; set; } = new Clinica();

        public IActionResult OnPost()
        {
            NovaClinica.Aprovado = false;

            _context.Clinicas.Add(NovaClinica);
            _context.SaveChanges();

            return RedirectToPage("/PagamentoClinica");
        }
    }
}