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

            // tratar instagram
            if (!string.IsNullOrEmpty(NovaClinica.Instagram))
            {
                NovaClinica.Instagram = NovaClinica.Instagram
                    .Replace("@", "")
                    .Trim();

                if (!NovaClinica.Instagram.StartsWith("http"))
                {
                    NovaClinica.Instagram = "https://instagram.com/" + NovaClinica.Instagram;
                }
            }

            // 🔹 TRATAR SITE
            if (!string.IsNullOrEmpty(NovaClinica.Site))
            {
                if (!NovaClinica.Site.StartsWith("http"))
                {
                    NovaClinica.Site = "https://" + NovaClinica.Site;
                }
            }

            if (!NovaClinica.AtendimentoOnline && !NovaClinica.AtendimentoPresencial)
            {
                ModelState.AddModelError("", "Selecione pelo menos um tipo de atendimento.");
                return Page();
            }

            // 🔹 VALIDAR CPF (básico)
            if (string.IsNullOrEmpty(NovaClinica.CPF) || NovaClinica.CPF.Length < 11)
            {
                ModelState.AddModelError("", "CPF inválido.");
                return Page();
            }

            var especialidades = Request.Form["Especialidades"];

            NovaClinica.Especialidades = string.Join(",", especialidades.ToArray());

            _context.Clinicas.Add(NovaClinica);
            _context.SaveChanges();



            return RedirectToPage("/PagamentoClinica");
        }


    }
}

