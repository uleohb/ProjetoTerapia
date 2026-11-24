using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProjetoTerapia.Models;
using System.IO;

namespace ProjetoTerapia.Pages
{
    public class ConteudosProfissionaisModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;


        public ConteudosProfissionaisModel(
            AppDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }


        public Clinica? Clinica { get; set; }


        public List<ConteudoProfissional> Conteudos { get; set; }
            = new();


        public const int LimiteConteudos = 6;


        // =====================================================
        // QUERYSTRING
        // =====================================================

        [BindProperty(SupportsGet = true)]
        public string? Novo { get; set; }


        // =====================================================
        // FORMULÁRIO
        // =====================================================

        [BindProperty]
        public string? TipoNovo { get; set; }


        [BindProperty]
        public string? Titulo { get; set; }


        [BindProperty]
        public string? Legenda { get; set; }


        [BindProperty]
        public IFormFile? Arquivo { get; set; }


        // =====================================================
        // GET
        // =====================================================

        public async Task<IActionResult> OnGetAsync()
        {
            var clinicaId =
                ObterClinicaLogadaId();


            if (!clinicaId.HasValue)
            {
                return RedirectToPage(
                    "/LoginClinica"
                );
            }


            var carregou =
                await CarregarDadosAsync(
                    clinicaId.Value
                );


            if (!carregou)
            {
                HttpContext.Session.Remove(
                    "ClinicaLogada"
                );


                return RedirectToPage(
                    "/LoginClinica"
                );
            }


            return Page();
        }


        // =====================================================
        // CRIAR CONTEÚDO
        // =====================================================

        public async Task<IActionResult> OnPostCriarAsync()
        {
            var clinicaId =
                ObterClinicaLogadaId();


            if (!clinicaId.HasValue)
            {
                return RedirectToPage(
                    "/LoginClinica"
                );
            }


            var clinicaExiste =
                await _context.Clinicas
                    .AsNoTracking()
                    .AnyAsync(c =>
                        c.Id == clinicaId.Value
                    );


            if (!clinicaExiste)
            {
                HttpContext.Session.Remove(
                    "ClinicaLogada"
                );


                return RedirectToPage(
                    "/LoginClinica"
                );
            }


            // =================================================
            // LIMITE DE CONTEÚDOS
            // =================================================

            var quantidade =
                await _context
                    .ConteudosProfissionais
                    .CountAsync(c =>
                        c.ClinicaId == clinicaId.Value
                    );


            if (quantidade >= LimiteConteudos)
            {
                TempData["ErroConteudo"] =
                    $"Você pode publicar no máximo {LimiteConteudos} conteúdos.";


                return Redirect(
                    "/profissional/conteudos"
                );
            }


            // =================================================
            // VALIDAR TIPO
            // =================================================

            var tipo =
                TipoNovo?.Trim();


            if (tipo != "Imagem" &&
                tipo != "Video")
            {
                TempData["ErroConteudo"] =
                    "Tipo de conteúdo inválido.";


                return Redirect(
                    "/profissional/conteudos"
                );
            }


            // =================================================
            // VALIDAR ARQUIVO
            // =================================================

            if (Arquivo == null ||
                Arquivo.Length == 0)
            {
                TempData["ErroConteudo"] =
                    "Selecione um arquivo para publicar.";


                return Redirect(
                    $"/profissional/conteudos?novo={tipo}"
                );
            }


            // =================================================
            // VALIDAR TÍTULO
            // =================================================

            if (!string.IsNullOrWhiteSpace(Titulo) &&
                Titulo.Length > 120)
            {
                TempData["ErroConteudo"] =
                    "O título pode ter no máximo 120 caracteres.";


                return Redirect(
                    $"/profissional/conteudos?novo={tipo}"
                );
            }


            // =================================================
            // VALIDAR LEGENDA
            // =================================================

            if (!string.IsNullOrWhiteSpace(Legenda) &&
                Legenda.Length > 1000)
            {
                TempData["ErroConteudo"] =
                    "A legenda pode ter no máximo 1000 caracteres.";


                return Redirect(
                    $"/profissional/conteudos?novo={tipo}"
                );
            }


            var extensao =
                Path.GetExtension(
                    Arquivo.FileName
                )
                .ToLowerInvariant();


            string[] extensoesPermitidas;

            long tamanhoMaximo;


            // =================================================
            // IMAGEM
            // =================================================

            if (tipo == "Imagem")
            {
                extensoesPermitidas =
                    new[]
                    {
                        ".jpg",
                        ".jpeg",
                        ".png",
                        ".webp"
                    };


                tamanhoMaximo =
                    10 * 1024 * 1024;
            }

            // =================================================
            // VÍDEO
            // =================================================

            else
            {
                extensoesPermitidas =
                    new[]
                    {
                        ".mp4",
                        ".webm"
                    };


                tamanhoMaximo =
                    100 * 1024 * 1024;
            }


            // =================================================
            // VALIDAR EXTENSÃO
            // =================================================

            if (!extensoesPermitidas.Contains(
                    extensao))
            {
                TempData["ErroConteudo"] =
                    tipo == "Imagem"
                        ? "Use uma imagem JPG, JPEG, PNG ou WEBP."
                        : "Use um vídeo MP4 ou WEBM.";


                return Redirect(
                    $"/profissional/conteudos?novo={tipo}"
                );
            }


            // =================================================
            // VALIDAR TAMANHO
            // =================================================

            if (Arquivo.Length > tamanhoMaximo)
            {
                TempData["ErroConteudo"] =
                    tipo == "Imagem"
                        ? "A imagem pode ter no máximo 10 MB."
                        : "O vídeo pode ter no máximo 100 MB.";


                return Redirect(
                    $"/profissional/conteudos?novo={tipo}"
                );
            }


            // =================================================
            // WWWROOT
            // =================================================

            var webRoot =
                _environment.WebRootPath;


            if (string.IsNullOrWhiteSpace(
                    webRoot))
            {
                webRoot =
                    Path.Combine(
                        _environment.ContentRootPath,
                        "wwwroot"
                    );
            }


            // =================================================
            // PASTA DO PROFISSIONAL
            // =================================================

            var pasta =
                Path.Combine(
                    webRoot,
                    "uploads",
                    "conteudos",
                    clinicaId.Value.ToString()
                );


            Directory.CreateDirectory(
                pasta
            );


            // =================================================
            // NOME ÚNICO
            // =================================================

            var nomeArquivo =
                $"{Guid.NewGuid():N}{extensao}";


            var caminhoFisico =
                Path.Combine(
                    pasta,
                    nomeArquivo
                );


            // =================================================
            // SALVAR ARQUIVO
            // =================================================

            await using (
                var stream =
                    new FileStream(
                        caminhoFisico,
                        FileMode.Create
                    )
            )
            {
                await Arquivo.CopyToAsync(
                    stream
                );
            }


            // =================================================
            // URL PÚBLICA
            // =================================================

            var urlArquivo =
                $"/uploads/conteudos/{clinicaId.Value}/{nomeArquivo}";


            // =================================================
            // ORDEM
            // =================================================

            var maiorOrdem =
                await _context
                    .ConteudosProfissionais
                    .Where(c =>
                        c.ClinicaId ==
                        clinicaId.Value
                    )
                    .Select(c =>
                        (int?)c.Ordem
                    )
                    .MaxAsync()
                ?? 0;


            // =================================================
            // CRIAR REGISTRO
            // =================================================

            var conteudo =
                new ConteudoProfissional
                {
                    ClinicaId =
                        clinicaId.Value,

                    Tipo =
                        tipo,

                    Titulo =
                        string.IsNullOrWhiteSpace(
                            Titulo
                        )
                            ? null
                            : Titulo.Trim(),

                    Legenda =
                        string.IsNullOrWhiteSpace(
                            Legenda
                        )
                            ? null
                            : Legenda.Trim(),

                    UrlArquivo =
                        urlArquivo,

                    UrlThumbnail =
                        tipo == "Imagem"
                            ? urlArquivo
                            : null,

                    Ordem =
                        maiorOrdem + 1,

                    Ativo =
                        true,

                    DataCadastro =
                        DateTime.Now
                };


            _context
                .ConteudosProfissionais
                .Add(
                    conteudo
                );


            await _context
                .SaveChangesAsync();


            TempData["SucessoConteudo"] =
                tipo == "Imagem"
                    ? "Post publicado com sucesso."
                    : "Vídeo publicado com sucesso.";


            return Redirect(
                "/profissional/conteudos"
            );
        }


        // =====================================================
        // EXCLUIR
        // =====================================================

        public async Task<IActionResult> OnPostExcluirAsync(
            int id)
        {
            var clinicaId =
                ObterClinicaLogadaId();


            if (!clinicaId.HasValue)
            {
                return RedirectToPage(
                    "/LoginClinica"
                );
            }


            var conteudo =
                await _context
                    .ConteudosProfissionais
                    .FirstOrDefaultAsync(c =>
                        c.Id == id &&
                        c.ClinicaId ==
                        clinicaId.Value
                    );


            if (conteudo == null)
            {
                TempData["ErroConteudo"] =
                    "Conteúdo não encontrado.";


                return Redirect(
                    "/profissional/conteudos"
                );
            }


            var urlArquivo =
                conteudo.UrlArquivo;


            _context
                .ConteudosProfissionais
                .Remove(
                    conteudo
                );


            await _context
                .SaveChangesAsync();


            ExcluirArquivoFisico(
                urlArquivo
            );


            TempData["SucessoConteudo"] =
                "Conteúdo excluído com sucesso.";


            return Redirect(
                "/profissional/conteudos"
            );
        }


        // =====================================================
        // PUBLICAR / OCULTAR
        // =====================================================

        public async Task<IActionResult> OnPostAlternarStatusAsync(
            int id)
        {
            var clinicaId =
                ObterClinicaLogadaId();


            if (!clinicaId.HasValue)
            {
                return RedirectToPage(
                    "/LoginClinica"
                );
            }


            var conteudo =
                await _context
                    .ConteudosProfissionais
                    .FirstOrDefaultAsync(c =>
                        c.Id == id &&
                        c.ClinicaId ==
                        clinicaId.Value
                    );


            if (conteudo == null)
            {
                TempData["ErroConteudo"] =
                    "Conteúdo não encontrado.";


                return Redirect(
                    "/profissional/conteudos"
                );
            }


            conteudo.Ativo =
                !conteudo.Ativo;


            conteudo.DataAtualizacao =
                DateTime.Now;


            await _context
                .SaveChangesAsync();


            TempData["SucessoConteudo"] =
                conteudo.Ativo
                    ? "Conteúdo publicado."
                    : "Conteúdo ocultado.";


            return Redirect(
                "/profissional/conteudos"
            );
        }


        // =====================================================
        // CARREGAR DADOS
        // =====================================================

        private async Task<bool> CarregarDadosAsync(
            int clinicaId)
        {
            Clinica =
                await _context
                    .Clinicas
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c =>
                        c.Id == clinicaId
                    );


            if (Clinica == null)
            {
                return false;
            }


            Conteudos =
                await _context
                    .ConteudosProfissionais
                    .AsNoTracking()
                    .Where(c =>
                        c.ClinicaId == clinicaId
                    )
                    .OrderBy(c =>
                        c.Ordem
                    )
                    .ThenByDescending(c =>
                        c.DataCadastro
                    )
                    .ToListAsync();


            return true;
        }


        // =====================================================
        // CLÍNICA LOGADA
        // =====================================================

        private int? ObterClinicaLogadaId()
        {
            var idSessao =
                HttpContext.Session
                    .GetString(
                        "ClinicaLogada"
                    );


            if (string.IsNullOrWhiteSpace(
                    idSessao) ||
                !int.TryParse(
                    idSessao,
                    out var clinicaId
                ))
            {
                return null;
            }


            return clinicaId;
        }


        // =====================================================
        // EXCLUIR ARQUIVO FÍSICO
        // =====================================================

        private void ExcluirArquivoFisico(
            string? urlArquivo)
        {
            if (string.IsNullOrWhiteSpace(
                    urlArquivo))
            {
                return;
            }


            try
            {
                var webRoot =
                    _environment.WebRootPath;


                if (string.IsNullOrWhiteSpace(
                        webRoot))
                {
                    return;
                }


                var caminhoRelativo =
                    urlArquivo
                        .TrimStart('/')
                        .Replace(
                            '/',
                            Path.DirectorySeparatorChar
                        );


                var caminhoCompleto =
                    Path.Combine(
                        webRoot,
                        caminhoRelativo
                    );


                if (System.IO.File.Exists(
                        caminhoCompleto))
                {
                    System.IO.File.Delete(
                        caminhoCompleto
                    );
                }
            }
            catch
            {
                // Se o arquivo já não existir,
                // não impede a exclusão do banco.
            }
        }
    }
}