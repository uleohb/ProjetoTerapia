using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;
using Microsoft.AspNetCore.Http;
using System.Linq;

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

        public IActionResult OnGet()
        {
            var id = HttpContext.Session.GetString("ClinicaLogada");

            // Se estiver logado, carrega os dados atuais
            if (id != null)
            {
                var clinica = _context.Clinicas
                    .FirstOrDefault(c => c.Id == int.Parse(id));

                if (clinica != null)
                {
                    NovaClinica = clinica;
                }
            }

            return Page();
        }

        public IActionResult OnPost()
        {
            var id = HttpContext.Session.GetString("ClinicaLogada");

            Clinica? clinica;

            // =========================================
            // EDITAR PERFIL
            // =========================================
            if (id != null)
            {
                clinica = _context.Clinicas
                    .FirstOrDefault(c => c.Id == int.Parse(id));

                if (clinica == null)
                {
                    return RedirectToPage("/LoginClinica");
                }

            }

         

            // =========================================
            // NOVO CADASTRO
            // =========================================
            else
            {
                clinica = new Clinica();

                _context.Clinicas.Add(clinica);

                clinica.Aprovado = false;
                clinica.Pago = false;
            }

            // =========================================
            // TELEFONE
            // =========================================
            if (!string.IsNullOrEmpty(NovaClinica.Telefone))
            {
                NovaClinica.Telefone = NovaClinica.Telefone
                    .Replace("(", "")
                    .Replace(")", "")
                    .Replace("-", "")
                    .Replace(" ", "")
                    .Replace("+", "");

                if (!NovaClinica.Telefone.StartsWith("55"))
                {
                    NovaClinica.Telefone = "55" + NovaClinica.Telefone;
                }
            }

            // =========================================
            // INSTAGRAM
            // =========================================
            if (!string.IsNullOrEmpty(NovaClinica.Instagram))
            {
                NovaClinica.Instagram = NovaClinica.Instagram
                    .Replace("@", "")
                    .Trim();

                if (!NovaClinica.Instagram.StartsWith("http"))
                {
                    NovaClinica.Instagram =
                        "https://instagram.com/" + NovaClinica.Instagram;
                }
            }

            // =========================================
            // SITE
            // =========================================
            if (!string.IsNullOrEmpty(NovaClinica.Site))
            {
                if (!NovaClinica.Site.StartsWith("http"))
                {
                    NovaClinica.Site = "https://" + NovaClinica.Site;
                }
            }

            // =========================================
            // VALIDAR ATENDIMENTO
            // =========================================
            if (!NovaClinica.AtendimentoOnline &&
                !NovaClinica.AtendimentoPresencial)
            {
                ModelState.AddModelError(
                    "",
                    "Selecione pelo menos um tipo de atendimento."
                );

                return Page();
            }

            // =========================================
            // ESPECIALIDADES
            // =========================================
            var especialidades = Request.Form["Especialidades"];

            clinica.Especialidades =
                string.Join(",", especialidades.ToArray());

            // =========================================
            // DADOS
            // =========================================
            clinica.Nome = NovaClinica.Nome;
            clinica.Descricao = NovaClinica.Descricao;
            clinica.CEP = NovaClinica.CEP;
            clinica.Endereco = NovaClinica.Endereco;
            clinica.Telefone = NovaClinica.Telefone;
            clinica.Email = NovaClinica.Email;
            clinica.Instagram = NovaClinica.Instagram;
            clinica.Site = NovaClinica.Site;
            clinica.Documento = NovaClinica.Documento;
            clinica.CPF = NovaClinica.CPF;
            clinica.Valor = NovaClinica.Valor;

            clinica.AtendimentoOnline =
                NovaClinica.AtendimentoOnline;

            clinica.AtendimentoPresencial =
                NovaClinica.AtendimentoPresencial;

            clinica.PerfilCompleto = true;

            _context.SaveChanges();

            // =========================================
            // LOGIN AUTOMÁTICO NO PRIMEIRO CADASTRO
            // =========================================
            if (id == null)
            {
                HttpContext.Session.SetString(
                    "ClinicaLogada",
                    clinica.Id.ToString()
                );
            }

            return RedirectToPage("/PainelClinica");
        }
    }
}