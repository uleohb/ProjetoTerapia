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

        [BindProperty]
        public IFormFile? FotoUpload { get; set; }

        [BindProperty]
        public string? FotoFinal { get; set; }

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
                        EspecialidadesSelecionadas = clinica.Especialidades
                            .Split(',')
                            .Select(e => e.Trim())
                            .Where(e => !string.IsNullOrWhiteSpace(e))
                            .ToList();
                    }
                }
            }

            return Page();
        }

        public IActionResult OnPost()
        {
            var id = HttpContext.Session.GetString("ClinicaLogada");

            Clinica? clinica;
            bool editandoPerfil = id != null;

            if (editandoPerfil)
            {
                clinica = _context.Clinicas
                    .FirstOrDefault(c => c.Id == int.Parse(id!));

                if (clinica == null)
                {
                    return RedirectToPage("/LoginClinica");
                }
            }
            else
            {
                clinica = new Clinica();

                clinica.Aprovado = false;
                clinica.Pago = false;
                clinica.PerfilCompleto = false;
                clinica.ClinicaAlteracaoPendente = false;

                _context.Clinicas.Add(clinica);
            }

            if (!NovaClinica.AtendimentoOnline &&
                !NovaClinica.AtendimentoPresencial)
            {
                ModelState.AddModelError(
                    "",
                    "Selecione pelo menos um tipo de atendimento."
                );

                RecarregarEspecialidadesSelecionadas();
                return Page();
            }

            NormalizarTelefone();
            NormalizarInstagram();
            NormalizarSite();

            var especialidades = Request.Form["Especialidades"];

            clinica.Especialidades =
                string.Join(",", especialidades.ToArray());

            clinica.Nome = NovaClinica.Nome;
            clinica.Descricao = NovaClinica.Descricao;
            clinica.CEP = NovaClinica.CEP;
            clinica.Cidade = NovaClinica.Cidade;
            clinica.Endereco = NovaClinica.Endereco;
            clinica.Telefone = NovaClinica.Telefone;
            clinica.Instagram = NovaClinica.Instagram ?? "";
            clinica.Site = NovaClinica.Site ?? "";
            clinica.Documento = NovaClinica.Documento;
            clinica.CPF = NovaClinica.CPF;
            clinica.Valor = NovaClinica.Valor;

            clinica.AtendimentoOnline = NovaClinica.AtendimentoOnline;
            clinica.AtendimentoPresencial = NovaClinica.AtendimentoPresencial;

            clinica.PerfilCompleto = true;

            SalvarFotoRecortada(clinica);

            if (clinica.Aprovado)
            {
                clinica.ClinicaAlteracaoPendente = true;

                TempData["Sucesso"] =
                    "Alterações salvas e enviadas para análise da equipe.";
            }
            else
            {
                TempData["Sucesso"] =
                    "Perfil enviado para análise com sucesso!";
            }

            _context.SaveChanges();

            if (!editandoPerfil)
            {
                HttpContext.Session.Remove("PacienteLogado");

                HttpContext.Session.SetString(
                    "ClinicaLogada",
                    clinica.Id.ToString()
                );
            }

            return RedirectToPage("/CadastroClinica");
        }

        private void NormalizarTelefone()
        {
            if (!string.IsNullOrEmpty(NovaClinica.Telefone))
            {
                NovaClinica.Telefone = NovaClinica.Telefone
                    .Replace("(", "")
                    .Replace(")", "")
                    .Replace("-", "")
                    .Replace(" ", "")
                    .Replace("+", "")
                    .Trim();

                if (!NovaClinica.Telefone.StartsWith("55"))
                {
                    NovaClinica.Telefone = "55" + NovaClinica.Telefone;
                }
            }
        }

        private void NormalizarInstagram()
        {
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
        }

        private void NormalizarSite()
        {
            if (!string.IsNullOrEmpty(NovaClinica.Site))
            {
                NovaClinica.Site = NovaClinica.Site.Trim();

                if (!NovaClinica.Site.StartsWith("http"))
                {
                    NovaClinica.Site = "https://" + NovaClinica.Site;
                }
            }
        }

        private void SalvarFotoRecortada(Clinica clinica)
        {
            if (string.IsNullOrWhiteSpace(FotoFinal))
            {
                return;
            }

            var base64 = FotoFinal;

            if (base64.Contains(","))
            {
                base64 = base64.Split(',')[1];
            }

            var bytes = Convert.FromBase64String(base64);

            var pastaUploads = Path.Combine(
                _environment.WebRootPath,
                "uploads"
            );

            if (!Directory.Exists(pastaUploads))
            {
                Directory.CreateDirectory(pastaUploads);
            }

            var nomeArquivo = Guid.NewGuid().ToString() + ".jpg";

            var caminhoArquivo = Path.Combine(
                pastaUploads,
                nomeArquivo
            );

            System.IO.File.WriteAllBytes(caminhoArquivo, bytes);

            clinica.FotoPerfil = "/uploads/" + nomeArquivo;
        }

        private void RecarregarEspecialidadesSelecionadas()
        {
            var especialidades = Request.Form["Especialidades"];

            EspecialidadesSelecionadas = especialidades
                .Select(e => e ?? "")
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .ToList();
        }
    }
}