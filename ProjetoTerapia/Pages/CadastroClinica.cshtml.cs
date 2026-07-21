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

            var especialidadesTexto = ObterEspecialidadesFormatadas();

            var fotoNova = SalvarFotoRecortada();

            /*
                REGRA IMPORTANTE:
                Se a clínica já foi aprovada, não salva direto no perfil público.
                Cria uma solicitação de alteração para o admin aprovar.
            */
            if (editandoPerfil && clinica.Aprovado)
            {
                CriarOuAtualizarAlteracaoPendente(
                    clinica,
                    especialidadesTexto,
                    fotoNova
                );

                clinica.ClinicaAlteracaoPendente = true;

                _context.SaveChanges();

                TempData["Sucesso"] =
                    "Alterações salvas com sucesso. Aguarde a aprovação da nossa equipe.";

                return RedirectToPage("/CadastroClinica");
            }

            /*
                Se for cadastro novo ou perfil ainda não aprovado,
                pode salvar direto porque ainda não aparece publicamente.
            */
            AtualizarClinicaDireto(
                clinica,
                especialidadesTexto,
                fotoNova
            );

            _context.SaveChanges();

            if (!editandoPerfil)
            {
                HttpContext.Session.Remove("PacienteLogado");

                HttpContext.Session.SetString(
                    "ClinicaLogada",
                    clinica.Id.ToString()
                );
            }

            TempData["Sucesso"] =
                "Cadastro enviado com sucesso. Aguarde a análise da nossa equipe.";

            return RedirectToPage("/CadastroClinica");
        }

        private void AtualizarClinicaDireto(
            Clinica clinica,
            string especialidadesTexto,
            string? fotoNova)
        {
            clinica.Especialidades = especialidadesTexto;

            clinica.Nome = NovaClinica.Nome;
            clinica.Email = NovaClinica.Email;
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

            if (!string.IsNullOrWhiteSpace(fotoNova))
            {
                clinica.FotoPerfil = fotoNova;
            }

            clinica.PerfilCompleto = true;
        }

        private void CriarOuAtualizarAlteracaoPendente(
            Clinica clinica,
            string especialidadesTexto,
            string? fotoNova)
        {
            var alteracao = _context.AlteracoesClinicas
                .FirstOrDefault(a =>
                    a.ClinicaId == clinica.Id &&
                    a.Status == "Pendente"
                );

            if (alteracao == null)
            {
                alteracao = new AlteracaoClinica
                {
                    ClinicaId = clinica.Id,
                    Status = "Pendente",
                    DataSolicitacao = DateTime.Now
                };

                _context.AlteracoesClinicas.Add(alteracao);
            }

            alteracao.Nome = NovaClinica.Nome;
            alteracao.Email = NovaClinica.Email;
            alteracao.Telefone = NovaClinica.Telefone;
            alteracao.CEP = NovaClinica.CEP;
            alteracao.Cidade = NovaClinica.Cidade;
            alteracao.Endereco = NovaClinica.Endereco;
            alteracao.Descricao = NovaClinica.Descricao;
            alteracao.Especialidades = especialidadesTexto;
            alteracao.Documento = NovaClinica.Documento;
            alteracao.CPF = NovaClinica.CPF;
            alteracao.Valor = NovaClinica.Valor;
            alteracao.AtendimentoOnline = NovaClinica.AtendimentoOnline;
            alteracao.AtendimentoPresencial = NovaClinica.AtendimentoPresencial;
            alteracao.Instagram = NovaClinica.Instagram ?? "";
            alteracao.Site = NovaClinica.Site ?? "";
            alteracao.FotoPerfil = !string.IsNullOrWhiteSpace(fotoNova)
                ? fotoNova
                : clinica.FotoPerfil;

            alteracao.Status = "Pendente";
            alteracao.MotivoRecusa = null;
            alteracao.DataAnalise = null;
            alteracao.NomeAdminAnalise = null;
        }

        private string ObterEspecialidadesFormatadas()
        {
            var especialidades = Request.Form["Especialidades"];

            var lista = especialidades
                .Select(e => e?.Trim() ?? "")
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(20)
                .ToList();

            return string.Join(",", lista);
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

        private string? SalvarFotoRecortada()
        {
            if (string.IsNullOrWhiteSpace(FotoFinal))
            {
                return null;
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

            return "/uploads/" + nomeArquivo;
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