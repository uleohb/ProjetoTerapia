using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;
using ProjetoTerapia.Pages;


namespace ProjetoTerapia.Pages 
{

    public class DashboardClinicaModel : PageModel
    {
        private readonly AppDbContext _context;

        public DashboardClinicaModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Clinica Clinica { get; set; } = new Clinica();

        public bool PerfilCompleto { get; set; } = false;

        public IActionResult OnGet()
        {
            var id = HttpContext.Session.GetInt32("ClinicaId");

            if (id == null)
                return RedirectToPage("/LoginClinica");

            var clinica = _context.Clinicas.FirstOrDefault(c => c.Id == id);

            if (clinica == null)
                return RedirectToPage("/LoginClinica");

            Clinica = clinica;

            return Page();
        }

        public IActionResult OnPost()
        {

            if (string.IsNullOrEmpty(Clinica.Nome) ||
        string.IsNullOrEmpty(Clinica.Descricao))
            {
                ModelState.AddModelError("", "Preencha os campos obrigatórios.");
                return Page();
            }


            var id = HttpContext.Session.GetInt32("ClinicaId");

            if (id == null)
                return RedirectToPage("/LoginClinica");

            var clinicaDb = _context.Clinicas.FirstOrDefault(c => c.Id == id);

            if (clinicaDb == null)
                return RedirectToPage("/LoginClinica");
            var telefone = Clinica.Telefone
                .Replace("(", "")
                .Replace(")", "")
                .Replace("-", "")
                .Replace(" ", "")
                .Replace("+", "");

            if (!telefone.StartsWith("55"))
            {
                telefone = "55" + telefone;
            }

            clinicaDb.Telefone = telefone;

            if (!string.IsNullOrEmpty(Clinica.Instagram))
            {
                var insta = Clinica.Instagram.Replace("@", "").Trim();

                if (!insta.StartsWith("http"))
                    insta = "https://instagram.com/" + insta;

                clinicaDb.Instagram = insta;
            }

            var especialidades = Request.Form["Especialidades"];
            clinicaDb.Especialidades = string.Join(",", especialidades.ToArray());



            // atualiza dados
            clinicaDb.Nome = Clinica.Nome;
            clinicaDb.Descricao = Clinica.Descricao;
            clinicaDb.Endereco = Clinica.Endereco;
            clinicaDb.Telefone = Clinica.Telefone;
            clinicaDb.CPF = Clinica.CPF;
            clinicaDb.Instagram = Clinica.Instagram;
            clinicaDb.Site = Clinica.Site;
            clinicaDb.Especialidades = Clinica.Especialidades;
            clinicaDb.Valor = Clinica.Valor;



            _context.SaveChanges();

            TempData["Sucesso"] = "Perfil atualizado com sucesso!";
            return RedirectToPage();

        }
    }

}
