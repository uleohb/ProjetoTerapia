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

            NovaClinica.Telefone = NovaClinica.Telefone //remove caracteres comuns de formatação de telefone
             .Replace("(", "")
             .Replace(")", "")
             .Replace("-", "")
             .Replace(" ", "")
             .Replace("+", "");

            if (!NovaClinica.Telefone.StartsWith("55")) //garante que o telefone comece com o código do país (55 para Brasil)
            {
                NovaClinica.Telefone = "55" + NovaClinica.Telefone;
            }

            _context.Clinicas.Add(NovaClinica);
            _context.SaveChanges();

            return RedirectToPage("/PagamentoClinica");
        }
    }
}