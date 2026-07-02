using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;
using System.Globalization;
using System.Text;

namespace ProjetoTerapia.Pages
{
    public class ClinicasModel : PageModel
    {
        private readonly AppDbContext _context;

        public ClinicasModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Clinica> Clinicas { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string Busca { get; set; } = "";

        [BindProperty(SupportsGet = true)]
        public string Perfil { get; set; } = "";

        public bool PacienteLogado { get; set; }

        public void OnGet()
        {
            PacienteLogado = !string.IsNullOrEmpty(
                HttpContext.Session.GetString("PacienteLogado")
            );

            var clinicasAtivas = _context.Clinicas
                .Where(c => c.Aprovado && c.Pago)
                .ToList();

            var divulgacoesAtivas = _context.DivulgacoesRegionais
                .Where(d =>
                    d.Ativo &&
                    d.Aprovado &&
                    d.Pago &&
                    (!d.DataFim.HasValue || d.DataFim.Value.Date >= DateTime.Today)
                )
                .ToList();

            if (!string.IsNullOrWhiteSpace(Busca))
            {
                var buscaNormalizada = Busca.Trim();

                clinicasAtivas = clinicasAtivas
                    .Where(c =>
                        TextoContem(c.Nome, buscaNormalizada) ||
                        TextoContem(c.Descricao, buscaNormalizada) ||
                        TextoContem(c.Endereco, buscaNormalizada) ||
                        TextoContem(c.Cidade, buscaNormalizada) ||
                        TextoContem(c.Especialidades, buscaNormalizada) ||
                        TemCidadeExtraAtiva(c.Id, buscaNormalizada, divulgacoesAtivas)
                    )
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(Perfil))
            {
                var perfilNormalizado = Perfil.Trim();

                clinicasAtivas = clinicasAtivas
                    .Where(c => TextoContem(c.Especialidades, perfilNormalizado))
                    .ToList();
            }

            Clinicas = clinicasAtivas
                .OrderByDescending(c => TemCidadeExtraAtiva(c.Id, Busca, divulgacoesAtivas))
                .ThenByDescending(c => c.AtendimentoOnline)
                .ThenBy(c => c.Nome)
                .ToList();
        }

        private bool TemCidadeExtraAtiva(
            int clinicaId,
            string cidadeBuscada,
            List<DivulgacaoRegional> divulgacoesAtivas)
        {
            if (string.IsNullOrWhiteSpace(cidadeBuscada))
                return false;

            var divulgacoesDaClinica = divulgacoesAtivas
                .Where(d => d.ClinicaId == clinicaId)
                .ToList();

            foreach (var divulgacao in divulgacoesDaClinica)
            {
                var cidades = divulgacao.CidadesSelecionadas
                    .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => c.Trim())
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .ToList();

                foreach (var cidade in cidades)
                {
                    if (TextoContem(cidade, cidadeBuscada))
                        return true;
                }
            }

            return false;
        }

        private bool TextoContem(string? texto, string busca)
        {
            if (string.IsNullOrWhiteSpace(texto) || string.IsNullOrWhiteSpace(busca))
                return false;

            var textoNormalizado = NormalizarTexto(texto);
            var buscaNormalizada = NormalizarTexto(busca);

            return textoNormalizado.Contains(buscaNormalizada);
        }

        private string NormalizarTexto(string texto)
        {
            var textoMinusculo = texto.ToLower().Trim();

            var textoNormalizado = textoMinusculo.Normalize(NormalizationForm.FormD);

            var caracteres = textoNormalizado
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .ToArray();

            return new string(caracteres).Normalize(NormalizationForm.FormC);
        }
    }
}