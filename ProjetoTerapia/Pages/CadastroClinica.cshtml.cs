using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;
using Microsoft.AspNetCore.Http;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace ProjetoTerapia.Pages
{
    public class CadastroClinicaModel : PageModel
    {

        private readonly AppDbContext _context;

        private readonly IWebHostEnvironment _environment;

        public CadastroClinicaModel(
            AppDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [BindProperty]
        public Clinica NovaClinica { get; set; } = new Clinica();

        public List<string> EspecialidadesSelecionadas { get; set; } = new();


        // Propriedade para upload de foto (opcional)
        [BindProperty]
        public IFormFile? FotoUpload { get; set; }

        public IActionResult OnGet()
        {
            var id = HttpContext.Session.GetString("ClinicaLogada");

            if (id != null)
            {
                var clinica = _context.Clinicas
                    .FirstOrDefault(c => c.Id == int.Parse(id));

                if (clinica != null)
                {
                    NovaClinica = clinica;

                    if (!string.IsNullOrEmpty(clinica.Especialidades))
                    {
                        EspecialidadesSelecionadas =
                            clinica.Especialidades.Split(',').ToList();
                    }

                    Console.WriteLine(NovaClinica.Nome);
                    Console.WriteLine(NovaClinica.CEP);
                    Console.WriteLine(NovaClinica.Documento);
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
            clinica.Instagram = NovaClinica.Instagram ?? "";
            clinica.Site = NovaClinica.Site ?? "";
            clinica.Documento = NovaClinica.Documento;
            clinica.CPF = NovaClinica.CPF;
            clinica.Valor = NovaClinica.Valor;

            clinica.AtendimentoOnline =
                NovaClinica.AtendimentoOnline;

            clinica.AtendimentoPresencial =
                NovaClinica.AtendimentoPresencial;

            clinica.PerfilCompleto = true;

            // =========================================
            // FOTO DE PERFIL
            // =========================================

            if (FotoUpload != null)
            {
                // cria pasta uploads se não existir
                var pastaUploads = Path.Combine(
                    _environment.WebRootPath,
                    "uploads"
                );

                if (!Directory.Exists(pastaUploads))
                {
                    Directory.CreateDirectory(pastaUploads);
                }

                // nome único da imagem
                var nomeArquivo =
                    Guid.NewGuid().ToString()
                    + Path.GetExtension(FotoUpload.FileName);

                var caminhoArquivo = Path.Combine(
                    pastaUploads,
                    nomeArquivo
                );

                // salva arquivo
                using (var stream = new FileStream(
                    caminhoArquivo,
                    FileMode.Create))
                {
                    FotoUpload.CopyTo(stream);
                }

                // salva no banco
                clinica.FotoPerfil = "/uploads/" + nomeArquivo;
            }

            Console.WriteLine($"Documento: {clinica.Documento}");

            Console.WriteLine($"ID DA CLINICA: {clinica.Id}");

            _context.SaveChanges();

            Console.WriteLine($"SALVOU ID: {clinica.Id}");

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

            TempData["Sucesso"] = "Perfil enviado para análise com sucesso!";

            return RedirectToPage("/CadastroClinica");
        }
    }
}