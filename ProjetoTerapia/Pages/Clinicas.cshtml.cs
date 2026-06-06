using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;
using System.Collections.Generic;
using System.Linq;

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

        public void OnGet()
        {
            var cidadeUsuario = "Itanhaém/SP";

            if (!_context.Clinicas.Any(c => c.Nome.Contains("Hiromi")))
            {
                _context.Clinicas.Add(new Clinica
                {
                    Nome = "Clínica Hiromi",
                    Descricao = "Atendimento médico humanizado com foco em saúde emocional e qualidade de vida.",
                    Valor = 250,
                    Endereco = "Av. Demerval Pereira Leite, 224 - Boca da Barra - Itanhaém/SP",
                    Cidade = "Osasco",
                    Telefone = "5513955485528",
                    Documento = "CRM 72377 SP",
                    Especialidades = "Ansiedade,Depressão,Terapia",
                    AtendimentoOnline = true,
                    AtendimentoPresencial = true,
                    Aprovado = true,
                    Pago = true,
                    PerfilCompleto = true
                });

                _context.SaveChanges();
            }

            var query = _context.Clinicas
                .Where(c => c.Aprovado && c.Pago)
                .Where(c =>
                    (c.AtendimentoPresencial && c.Cidade == cidadeUsuario)
                    || c.AtendimentoOnline
                );

            //filtro do teste: busca por nome, descrição ou endereço
            if (!string.IsNullOrEmpty(Busca))
            {
                query = query.Where(c =>
                    c.Nome.Contains(Busca) ||
                    c.Descricao.Contains(Busca) ||
                    c.Endereco.Contains(Busca));
            }

            //filtro do teste: perfil do profissional
            if (!string.IsNullOrEmpty(Perfil))
            {
                query = query.Where(c => c.Especialidades != null && c.Especialidades.Contains(Perfil));
            }

            Clinicas = query
             .OrderByDescending(c => c.Nome.Contains("Hiromi"))
             .ToList();

        }
    }
}