using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProjetoTerapia.Models;

namespace ProjetoTerapia.Pages
{
    public class PainelVendedorModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public PainelVendedorModel(
            AppDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public Vendedor Vendedor { get; set; } = new();

        public List<VendaVendedor> Vendas { get; set; } = new();

        [BindProperty]
        public IFormFile? ArquivoComprovante { get; set; }

        public IActionResult OnGet()
        {
            if (!VendedorEstaLogado())
            {
                return RedirectToPage("/LoginVendedor");
            }

            CarregarDados();

            return Page();
        }

        public IActionResult OnPostEnviarComprovante(int vendaId)
        {
            if (!VendedorEstaLogado())
            {
                return RedirectToPage("/LoginVendedor");
            }

            var vendedorId = int.Parse(HttpContext.Session.GetString("VendedorId")!);

            var venda = _context.VendasVendedores
                .FirstOrDefault(v =>
                    v.Id == vendaId &&
                    v.VendedorId == vendedorId
                );

            if (venda == null)
            {
                TempData["MensagemErro"] = "Venda não encontrada.";
                return RedirectToPage("/PainelVendedor");
            }

            // Depois que o admin marcou como paga, o vendedor não pode trocar o documento
            if (venda.ComissaoPaga)
            {
                TempData["MensagemErro"] = "Esta comissão já foi paga. Não é possível reenviar o documento.";
                return RedirectToPage("/PainelVendedor");
            }

            if (ArquivoComprovante == null || ArquivoComprovante.Length == 0)
            {
                TempData["MensagemErro"] = "Selecione um arquivo para enviar.";
                return RedirectToPage("/PainelVendedor");
            }

            var extensao = Path.GetExtension(ArquivoComprovante.FileName).ToLower();

            var extensoesPermitidas = new[]
            {
        ".pdf", ".jpg", ".jpeg", ".png", ".webp"
    };

            if (!extensoesPermitidas.Contains(extensao))
            {
                TempData["MensagemErro"] = "Envie um arquivo PDF, JPG, PNG ou WEBP.";
                return RedirectToPage("/PainelVendedor");
            }

            if (ArquivoComprovante.Length > 5 * 1024 * 1024)
            {
                TempData["MensagemErro"] = "O arquivo deve ter no máximo 5 MB.";
                return RedirectToPage("/PainelVendedor");
            }

            var pasta = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "comprovantes-vendedores"
            );

            if (!Directory.Exists(pasta))
            {
                Directory.CreateDirectory(pasta);
            }

            var tinhaDocumentoAnterior = !string.IsNullOrWhiteSpace(venda.ComprovanteNotaFiscal);

            // Se já tinha documento, apaga o arquivo antigo para não acumular lixo no servidor
            if (tinhaDocumentoAnterior)
            {
                var caminhoAntigo = Path.Combine(
                    _environment.WebRootPath,
                    venda.ComprovanteNotaFiscal!
                        .TrimStart('/')
                        .Replace("/", Path.DirectorySeparatorChar.ToString())
                );

                if (System.IO.File.Exists(caminhoAntigo))
                {
                    System.IO.File.Delete(caminhoAntigo);
                }
            }

            var nomeArquivo = $"venda-{venda.Id}-{Guid.NewGuid()}{extensao}";
            var caminhoCompleto = Path.Combine(pasta, nomeArquivo);

            using (var stream = new FileStream(caminhoCompleto, FileMode.Create))
            {
                ArquivoComprovante.CopyTo(stream);
            }

            venda.ComprovanteNotaFiscal =
                "/uploads/comprovantes-vendedores/" + nomeArquivo;

            venda.Status = tinhaDocumentoAnterior
                ? "Documento reenviado - aguardando conferência"
                : "Nota enviada - aguardando conferência";

            _context.SaveChanges();

            TempData["MensagemSucesso"] = tinhaDocumentoAnterior
                ? "Documento reenviado com sucesso."
                : "Documento enviado com sucesso.";

            return RedirectToPage("/PainelVendedor");
        }

        public IActionResult OnPostSair()
        {
            HttpContext.Session.Clear();
            return RedirectToPage("/LoginVendedor");
        }

        private bool VendedorEstaLogado()
        {
            return HttpContext.Session.GetString("VendedorLogado") == "true";
        }

        private void CarregarDados()
        {
            var vendedorId = int.Parse(HttpContext.Session.GetString("VendedorId")!);

            Vendedor = _context.Vendedores
                .FirstOrDefault(v => v.Id == vendedorId)!;

            Vendas = _context.VendasVendedores
                .Include(v => v.Clinica)
                .Where(v => v.VendedorId == vendedorId)
                .OrderByDescending(v => v.DataCadastro)
                .ToList();
        }
    }
}