using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace ProjetoTerapia.Pages
{
    public class PainelClinicaModel : PageModel
    {

        private readonly AppDbContext _context;

        public Clinica? Clinica { get; set; }

        public PainelClinicaModel(AppDbContext context)
        {
            _context = context;
        }
        public IActionResult OnGet()
        {
            var id = HttpContext.Session.GetString("ClinicaLogada");

            if (id == null)  
               return RedirectToPage("/LoginClinica");

            Clinica = _context.Clinicas.FirstOrDefault(c => c.Id == int.Parse(id));

            if(Clinica == null)  
                return RedirectToPage("/LoginClinica");

            return Page();
        }
    }
}
