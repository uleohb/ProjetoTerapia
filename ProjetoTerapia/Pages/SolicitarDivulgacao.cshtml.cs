using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjetoTerapia.Pages
{
    public class SolicitarDivulgacaoModel : PageModel
    {
        private readonly AppDbContext _context;

        public SolicitarDivulgacaoModel(AppDbContext context)
        {
            _context = context;
        }

        public Clinica? Clinica { get; set; }

        public List<DivulgacaoRegional> MinhasSolicitacoes { get; set; } = new();

        [BindProperty]
        public int QuantidadeCidades { get; set; }

        [BindProperty]
        public string CidadesSelecionadas { get; set; } = "";

        public IActionResult OnGet()
        {
            var id = HttpContext.Session.GetString("ClinicaLogada");

            if (id == null)
                return RedirectToPage("/LoginClinica");

            Clinica = _context.Clinicas.FirstOrDefault(c => c.Id == int.Parse(id));

            if (Clinica == null)
                return RedirectToPage("/LoginClinica");

            if (!Clinica.Pago)
                return RedirectToPage("/PagamentoClinica");

            CarregarSolicitacoes(Clinica.Id);

            return Page();
        }

        public IActionResult OnPost()
        {
            var id = HttpContext.Session.GetString("ClinicaLogada");

            if (id == null)
                return RedirectToPage("/LoginClinica");

            Clinica = _context.Clinicas.FirstOrDefault(c => c.Id == int.Parse(id));

            if (Clinica == null)
                return RedirectToPage("/LoginClinica");

            if (!Clinica.Pago)
                return RedirectToPage("/PagamentoClinica");

            var plano = ObterPlano(QuantidadeCidades);

            if (plano == null)
            {
                TempData["Erro"] = "Selecione um plano válido.";
                CarregarSolicitacoes(Clinica.Id);
                return Page();
            }

            var cidades = CidadesSelecionadas
                .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim())
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .ToList();

            if (!cidades.Any())
            {
                TempData["Erro"] = "Informe pelo menos uma cidade.";
                CarregarSolicitacoes(Clinica.Id);
                return Page();
            }

            if (cidades.Count > QuantidadeCidades)
            {
                TempData["Erro"] = $"Este plano permite até {QuantidadeCidades} cidades.";
                CarregarSolicitacoes(Clinica.Id);
                return Page();
            }

            var divulgacao = new DivulgacaoRegional
            {
                ClinicaId = Clinica.Id,
                NomePlano = plano.Value.Nome,
                QuantidadeCidades = QuantidadeCidades,
                Valor = plano.Value.Valor,
                CidadesSelecionadas = string.Join(", ", cidades),
                Pago = false,
                Aprovado = false,
                Ativo = false,
                DataSolicitacao = DateTime.Now,
                Status = "Aguardando pagamento"
            };

            _context.DivulgacoesRegionais.Add(divulgacao);
            _context.SaveChanges();

            TempData["Sucesso"] = "Solicitação enviada com sucesso. Aguarde a confirmação da administração.";

            return RedirectToPage("/SolicitarDivulgacao");
        }

        private void CarregarSolicitacoes(int clinicaId)
        {
            MinhasSolicitacoes = _context.DivulgacoesRegionais
                .Where(d => d.ClinicaId == clinicaId)
                .OrderByDescending(d => d.DataSolicitacao)
                .ToList();
        }

        private (string Nome, decimal Valor)? ObterPlano(int quantidade)
        {
            return quantidade switch
            {
                5 => ("+5 cidades", 30),
                10 => ("+10 cidades", 35),
                15 => ("+15 cidades", 45),
                20 => ("+20 cidades", 50),
                25 => ("+25 cidades", 55),
                _ => null
            };
        }
    }
}