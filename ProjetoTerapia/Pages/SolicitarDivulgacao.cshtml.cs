using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

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

        public List<string> CidadesDisponiveis { get; set; } = new()
        {
            "São Paulo",
            "Osasco",
            "Barueri",
            "Carapicuíba",
            "Cotia",
            "Taboão da Serra",
            "Embu das Artes",
            "Itapevi",
            "Jandira",
            "Santana de Parnaíba",
            "Guarulhos",
            "Santo André",
            "São Bernardo do Campo",
            "São Caetano do Sul",
            "Diadema",
            "Mauá",
            "Ribeirão Pires",
            "Mogi das Cruzes",
            "Suzano",
            "Poá",
            "Itaquaquecetuba",
            "Santos",
            "São Vicente",
            "Praia Grande",
            "Guarujá",
            "Cubatão",
            "Itanhaém",
            "Mongaguá",
            "Peruíbe"
        };

        [BindProperty]
        public int QuantidadeCidades { get; set; }

        [BindProperty]
        public List<string> CidadesEscolhidas { get; set; } = new();

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

            if (string.IsNullOrWhiteSpace(Clinica.Cidade))
            {
                TempData["Erro"] = "Antes de solicitar divulgação regional, preencha sua cidade principal no perfil profissional.";
                return RedirectToPage("/CadastroClinica");
            }

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

            if (string.IsNullOrWhiteSpace(Clinica.Cidade))
            {
                TempData["Erro"] = "Antes de solicitar divulgação regional, preencha sua cidade principal no perfil profissional.";
                return RedirectToPage("/CadastroClinica");
            }

            var plano = ObterPlano(QuantidadeCidades);

            if (plano == null)
            {
                TempData["Erro"] = "Selecione um plano válido.";
                CarregarSolicitacoes(Clinica.Id);
                return Page();
            }

            var cidades = LimparCidadesSelecionadas();

            if (!cidades.Any())
            {
                TempData["Erro"] = "Selecione pelo menos uma cidade adicional.";
                CarregarSolicitacoes(Clinica.Id);
                return Page();
            }

            if (cidades.Any(c => TextosIguais(c, Clinica.Cidade)))
            {
                TempData["Erro"] = "Sua cidade principal já está inclusa no plano. Escolha apenas cidades adicionais.";
                CarregarSolicitacoes(Clinica.Id);
                return Page();
            }

            if (cidades.Count > QuantidadeCidades)
            {
                TempData["Erro"] = $"Este plano permite até {QuantidadeCidades} cidade(s) adicional(is).";
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

        public bool EhCidadePrincipal(string cidade)
        {
            if (Clinica == null)
                return false;

            return TextosIguais(cidade, Clinica.Cidade);
        }

        public bool CidadeSelecionada(string cidade)
        {
            return CidadesEscolhidas.Any(c => TextosIguais(c, cidade));
        }

        private List<string> LimparCidadesSelecionadas()
        {
            var cidadesLimpas = new List<string>();
            var usadas = new HashSet<string>();

            foreach (var cidade in CidadesEscolhidas)
            {
                if (string.IsNullOrWhiteSpace(cidade))
                    continue;

                var cidadeTratada = cidade.Trim();

                var cidadeExisteNaLista = CidadesDisponiveis
                    .Any(c => TextosIguais(c, cidadeTratada));

                if (!cidadeExisteNaLista)
                    continue;

                var chave = NormalizarTexto(cidadeTratada);

                if (usadas.Add(chave))
                {
                    cidadesLimpas.Add(cidadeTratada);
                }
            }

            return cidadesLimpas;
        }

        private void CarregarSolicitacoes(int clinicaId)
        {
            var solicitacoes = _context.DivulgacoesRegionais
                .Where(d => d.ClinicaId == clinicaId)
                .OrderByDescending(d => d.DataSolicitacao)
                .ToList();

            foreach (var item in solicitacoes)
            {
                var expirou = !item.Pago &&
                              !item.Aprovado &&
                              item.Status != "Cancelado" &&
                              item.DataSolicitacao.AddHours(72) < DateTime.Now;

                if (expirou)
                {
                    item.Status = "Expirado";
                    item.Ativo = false;
                }
            }

            _context.SaveChanges();

            MinhasSolicitacoes = solicitacoes;
        }

        private (string Nome, decimal Valor)? ObterPlano(int quantidade)
        {
            return quantidade switch
            {
                1 => ("+1 cidade adicional", 30),
                3 => ("+3 cidades adicionais", 35),
                5 => ("+5 cidades adicionais", 40),
                8 => ("+8 cidades adicionais", 45),
                10 => ("+10 cidades adicionais", 50),
                15 => ("+15 cidades adicionais", 55),
                20 => ("+20 cidades adicionais", 60),
                25 => ("+25 cidades adicionais", 65),
                27 => ("+27 cidades adicionais", 70),
                _ => null
            };
        }

        private bool TextosIguais(string? textoA, string? textoB)
        {
            return NormalizarTexto(textoA ?? "") == NormalizarTexto(textoB ?? "");
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