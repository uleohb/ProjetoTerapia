using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;

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

            var query = _context.Clinicas
                .Where(c => c.Aprovado && c.Pago);

            if (!string.IsNullOrWhiteSpace(Busca))
            {
                var buscaNormalizada = Busca.Trim();

                query = query.Where(c =>
                    c.Nome.Contains(buscaNormalizada) ||
                    c.Descricao.Contains(buscaNormalizada) ||
                    c.Endereco.Contains(buscaNormalizada) ||
                    c.Cidade.Contains(buscaNormalizada) ||
                    c.Especialidades.Contains(buscaNormalizada)
                );
            }

            if (!string.IsNullOrWhiteSpace(Perfil))
            {
                var perfilNormalizado = Perfil.Trim();

                query = query.Where(c =>
                    c.Especialidades != null &&
                    c.Especialidades.Contains(perfilNormalizado)
                );
            }

            Clinicas = query
                .OrderByDescending(c => c.AtendimentoOnline)
                .ThenBy(c => c.Nome)
                .ToList();
        }
    }
}