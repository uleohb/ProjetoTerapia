using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace ProjetoTerapia.Pages
{
    public class AdminLHModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AdminLHModel(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public List<Clinica> Clinicas { get; set; } = new List<Clinica>();

        public int TotalClinicas { get; set; }

        public int Pendentes { get; set; }

        public int Aprovadas { get; set; }

        public int Ativas { get; set; }

        public int PagamentosPendentes { get; set; }

        public int AlteracoesPendentes { get; set; }

        public List<AlteracaoClinica> AlteracoesClinicasPendentes { get; set; } = new();
        public List<Clinica> ClinicasComAlteracao { get; set; } = new();

        public int TotalVisualizacoes { get; set; }

        public int TotalCliquesWhatsapp { get; set; }

        public int TaxaConversao { get; set; }

        public decimal ReceitaPrevista { get; set; }

        public string ReceitaPrevistaFormatada { get; set; } = "R$ 0,00";

        [BindProperty(SupportsGet = true)]
        public string Busca { get; set; } = "";

        [BindProperty(SupportsGet = true)]
        public string FiltroStatus { get; set; } = "";
        public List<DivulgacaoRegional> DivulgacoesRegionais { get; set; } = new();
        public int DivulgacoesPendentes { get; set; }
        public List<AdminLog> AdminLogs { get; set; } = new();

        public IActionResult OnGet()
        {
            if (!AdminEstaLogado()) return RedirectToPage("/LoginAdmin");

            AtualizarDivulgacoesExpiradas();
            CarregarDados();

            return Page();
        }

        public IActionResult OnPost(int id, string acao)
        {
            if (!AdminEstaLogado())
            {
                return RedirectToPage("/LoginAdmin");
            }

            AtualizarDivulgacoesExpiradas();

            if (acao == "aprovarAlteracao" || acao == "recusarAlteracao")
            {
                var alteracao = _context.AlteracoesClinicas
                    .Include(a => a.Clinica)
                    .FirstOrDefault(a => a.Id == id && a.Status == "Pendente");

                if (alteracao == null)
                {
                    TempData["MensagemErro"] = "Solicitação de alteração não encontrada.";
                    return RedirectToPage(new { aba = "alteracoes" });
                }

                var clinicaAlterada = alteracao.Clinica;

                if (clinicaAlterada == null)
                {
                    TempData["MensagemErro"] = "Profissional vinculado à alteração não encontrado.";
                    return RedirectToPage(new { aba = "alteracoes" });
                }

                if (acao == "aprovarAlteracao")
                {
                    clinicaAlterada.Nome = alteracao.Nome;
                    clinicaAlterada.Email = alteracao.Email;
                    clinicaAlterada.Telefone = alteracao.Telefone;
                    clinicaAlterada.CEP = alteracao.CEP;
                    clinicaAlterada.Cidade = alteracao.Cidade;
                    clinicaAlterada.Endereco = alteracao.Endereco;
                    clinicaAlterada.Descricao = alteracao.Descricao;
                    clinicaAlterada.Especialidades = alteracao.Especialidades;
                    clinicaAlterada.Documento = alteracao.Documento;
                    clinicaAlterada.CPF = alteracao.CPF;
                    clinicaAlterada.Valor = alteracao.Valor;
                    clinicaAlterada.AtendimentoOnline = alteracao.AtendimentoOnline;
                    clinicaAlterada.AtendimentoPresencial = alteracao.AtendimentoPresencial;
                    clinicaAlterada.Instagram = alteracao.Instagram;
                    clinicaAlterada.Site = alteracao.Site;

                    if (!string.IsNullOrWhiteSpace(alteracao.FotoPerfil))
                    {
                        clinicaAlterada.FotoPerfil = alteracao.FotoPerfil;
                    }

                    clinicaAlterada.ClinicaAlteracaoPendente = false;

                    alteracao.Status = "Aprovada";
                    alteracao.DataAnalise = DateTime.Now;
                    alteracao.NomeAdminAnalise = HttpContext.Session.GetString("AdminNome");

                    RegistrarLog(
                        "Aprovação de alteração de perfil",
                        $"{HttpContext.Session.GetString("AdminNome")} aprovou as alterações do perfil de {clinicaAlterada.Nome}."
                    );

                    TempData["MensagemSucesso"] =
                        $"Alterações da clínica {clinicaAlterada.Nome} aprovadas com sucesso.";
                }

                if (acao == "recusarAlteracao")
                {
                    var motivo = Request.Form["MotivoRecusa"].ToString();

                    if (string.IsNullOrWhiteSpace(motivo))
                    {
                        motivo = "Alteração recusada pela administração.";
                    }

                    clinicaAlterada.ClinicaAlteracaoPendente = false;

                    alteracao.Status = "Recusada";
                    alteracao.MotivoRecusa = motivo;
                    alteracao.DataAnalise = DateTime.Now;
                    alteracao.NomeAdminAnalise = HttpContext.Session.GetString("AdminNome");

                    RegistrarLog(
                        "Recusa de alteração de perfil",
                        $"{HttpContext.Session.GetString("AdminNome")} recusou as alterações do perfil de {clinicaAlterada.Nome}. Motivo: {motivo}"
                    );

                    TempData["MensagemSucesso"] =
                        $"Alterações da clínica {clinicaAlterada.Nome} recusadas.";
                }

                _context.SaveChanges();

                return RedirectToPage(new { aba = "alteracoes" });
            }

            if (acao == "confirmarPagamentoDivulgacao" ||
              acao == "aprovarDivulgacao" ||
              acao == "cancelarDivulgacao")
            {
                var divulgacao = _context.DivulgacoesRegionais
                    .Include(d => d.Clinica)
                    .FirstOrDefault(d => d.Id == id);

                if (divulgacao == null)
                {
                    TempData["MensagemErro"] = "Solicitação de divulgação não encontrada.";
                    return RedirectToPage(new { aba = "divulgacao" });
                }

                if (acao == "confirmarPagamentoDivulgacao")
                {
                    if (divulgacao.Status == "Expirado")
                    {
                        TempData["MensagemErro"] = "Esta solicitação expirou. O profissional precisa criar uma nova solicitação.";
                        return RedirectToPage(new { aba = "divulgacao" });
                    }

                    divulgacao.Pago = true;
                    divulgacao.DataPagamento = DateTime.Now;
                    divulgacao.Status = "Pagamento confirmado";

                    RegistrarLog(
                     "Confirmação de pagamento de divulgação",
                     $"{HttpContext.Session.GetString("AdminNome")} confirmou o pagamento " +
                     $"da divulgação regional de {divulgacao.Clinica?.Nome} no plano {divulgacao.NomePlano}."
                    );

                    TempData["MensagemSucesso"] = "Pagamento da divulgação confirmado.";
                }

                if (acao == "aprovarDivulgacao")
                {
                    if (divulgacao.Status == "Expirado")
                    {
                        TempData["MensagemErro"] = "Esta solicitação expirou e não pode mais ser aprovada.";
                        return RedirectToPage(new { aba = "divulgacao" });
                    }

                    if (!divulgacao.Pago)
                    {
                        TempData["MensagemErro"] = "Confirme o pagamento antes de aprovar a divulgação.";
                        return RedirectToPage(new { aba = "divulgacao" });
                    }

                    divulgacao.Aprovado = true;
                    divulgacao.Ativo = true;
                    divulgacao.DataAprovacao = DateTime.Now;
                    divulgacao.DataInicio = DateTime.Now;
                    divulgacao.DataFim = DateTime.Now.AddMonths(1);
                    divulgacao.Status = "Ativo";

                    RegistrarLog(
                     "Aprovação de divulgação regional",
                     $"{HttpContext.Session.GetString("AdminNome")} aprovou a " +
                     $"divulgação regional de {divulgacao.Clinica?.Nome}. Cidades: {divulgacao.CidadesSelecionadas}."
                    );

                    TempData["MensagemSucesso"] = "Divulgação regional aprovada e ativada.";
                }

                if (acao == "cancelarDivulgacao")
                {
                    divulgacao.Ativo = false;
                    divulgacao.Status = "Cancelado";

                    RegistrarLog(
                     "Cancelamento de divulgação regional",
                     $"{HttpContext.Session.GetString("AdminNome")} cancelou a divulgação regional de {divulgacao.Clinica?.Nome}."
                    );

                    TempData["MensagemSucesso"] = "Divulgação regional cancelada.";
                }

                _context.SaveChanges();

                return RedirectToPage(new { aba = "divulgacao" });
            }

            var clinica = _context.Clinicas.FirstOrDefault(c => c.Id == id);

            if (clinica == null)
            {
                TempData["MensagemErro"] = "Clínica não encontrada.";
                return RedirectToPage(new { aba = "clinicas" });
            }

            if (acao == "aprovar")
            {
                clinica.Aprovado = true;
                clinica.Pago = false;
                clinica.DataAprovacao = DateTime.Now;

                RegistrarLog(
                 "Aprovação de profissional",
                 $"{HttpContext.Session.GetString("AdminNome")} aprovou o cadastro do profissional {clinica.Nome}."
                );

                TempData["MensagemSucesso"] =
                    $"Clínica {clinica.Nome} aprovada com sucesso.";

                _context.SaveChanges();

                return RedirectToPage(new { aba = "clinicas" });
            }

            if (acao == "pagar")
            {
                if (!clinica.Aprovado)
                {
                    TempData["MensagemErro"] =
                        "A clínica precisa ser aprovada antes da confirmação do pagamento.";

                    return RedirectToPage(new { aba = "clinicas" });
                }

                clinica.Pago = true;
                clinica.NomePlano = "Plano Profissional Anual";
                clinica.ValorPlano = 360;
                clinica.DataPagamento = DateTime.Now;
                clinica.DataVencimento = DateTime.Now.AddYears(1);

                RegistrarLog(
                 "Confirmação de pagamento",
                 $"{HttpContext.Session.GetString("AdminNome")} confirmou o pagamento do plano profissional de {clinica.Nome}."
                );

                TempData["MensagemSucesso"] =
                    $"Pagamento da clínica {clinica.Nome} confirmado com sucesso.";

                _context.SaveChanges();

                return RedirectToPage(new { aba = "pagamentos" });
            }

            if (acao == "suspender")
            {
                clinica.Pago = false;

                RegistrarLog(
                 "Suspensão de plano",
                 $"{HttpContext.Session.GetString("AdminNome")} suspendeu o plano do profissional {clinica.Nome}."
                );

                TempData["MensagemSucesso"] =
                    $"Plano da clínica {clinica.Nome} foi suspenso.";

                _context.SaveChanges();

                return RedirectToPage(new { aba = "clinicas" });
            }

            if (acao == "excluir")
            {
                try
                {
                    RegistrarLog(
                     "Exclusão de profissional",
                     $"{HttpContext.Session.GetString("AdminNome")} excluiu o profissional {clinica.Nome}."
                    );

                    _context.Clinicas.Remove(clinica);
                    _context.SaveChanges();

                    TempData["MensagemSucesso"] =
                        $"Clínica {clinica.Nome} removida com sucesso.";
                }
                catch
                {
                    TempData["MensagemErro"] =
                        "Não foi possível excluir esta clínica porque ela pode possuir vínculos no sistema.";
                }

                return RedirectToPage(new { aba = "clinicas" });
            }

            TempData["MensagemErro"] = "Ação inválida.";

            return RedirectToPage(new { aba = "clinicas" });
        }

        public IActionResult OnPostLogout()
        {
            if (AdminEstaLogado())
            {
                RegistrarLog(
                    "Logout",
                    $"{HttpContext.Session.GetString("AdminNome")} saiu do painel administrativo."
                );

                _context.SaveChanges();
            }

            HttpContext.Session.Clear();
            return RedirectToPage("/LoginAdmin");
        }

        private bool AdminEstaLogado()
        {
            return HttpContext.Session.GetString("AdminLogado") == "true";
        }

        private void CarregarDados()
        {
            var todasClinicas = _context.Clinicas.ToList();

            AlteracoesClinicasPendentes = _context.AlteracoesClinicas
              .Include(a => a.Clinica)
              .Where(a => a.Status == "Pendente")
              .OrderByDescending(a => a.DataSolicitacao)
              .ToList();

            DivulgacoesRegionais = _context.DivulgacoesRegionais
             .Include(d => d.Clinica)
             .OrderByDescending(d => d.DataSolicitacao)
             .ToList();

            AdminLogs = _context.AdminLogs
             .OrderByDescending(l => l.DataAcao)
             .Take(100)
             .ToList();

            DivulgacoesPendentes = DivulgacoesRegionais
             .Count(d =>
              d.Status != "Expirado" &&
              d.Status != "Cancelado" &&
              (!d.Aprovado || !d.Pago)
             );

            ClinicasComAlteracao = AlteracoesClinicasPendentes
             .Where(a => a.Clinica != null)
             .Select(a => a.Clinica!)
             .OrderBy(c => c.Nome)
             .ToList();

            TotalClinicas = todasClinicas.Count;
            Pendentes = todasClinicas.Count(c => !c.Aprovado);
            Aprovadas = todasClinicas.Count(c => c.Aprovado && !c.Pago);
            Ativas = todasClinicas.Count(c => c.Pago);
            PagamentosPendentes = todasClinicas.Count(c => c.Aprovado && !c.Pago);
            AlteracoesPendentes = AlteracoesClinicasPendentes.Count;

            TotalVisualizacoes = todasClinicas.Sum(c => c.Visualizacoes);
            TotalCliquesWhatsapp = todasClinicas.Sum(c => c.CliquesWhatsapp);

            ReceitaPrevista = todasClinicas
                .Where(c => c.Pago)
                .Sum(c => c.ValorPlano ?? 360);

            ReceitaPrevistaFormatada =
                ReceitaPrevista.ToString("C", new CultureInfo("pt-BR"));

            var totalAprovadasOuAtivas = Aprovadas + Ativas;

            TaxaConversao = totalAprovadasOuAtivas > 0
                ? (Ativas * 100) / totalAprovadasOuAtivas
                : 0;

            var query = todasClinicas.AsQueryable();

            if (!string.IsNullOrWhiteSpace(Busca))
            {
                var buscaNormalizada = Busca.Trim();

                query = query.Where(c =>
                    c.Nome.Contains(buscaNormalizada, StringComparison.OrdinalIgnoreCase) ||
                    c.Email.Contains(buscaNormalizada, StringComparison.OrdinalIgnoreCase) ||
                    c.Cidade.Contains(buscaNormalizada, StringComparison.OrdinalIgnoreCase)
                );
            }

            if (!string.IsNullOrWhiteSpace(FiltroStatus))
            {
                if (FiltroStatus == "pendente")
                {
                    query = query.Where(c => !c.Aprovado);
                }

                if (FiltroStatus == "aprovado")
                {
                    query = query.Where(c => c.Aprovado && !c.Pago);
                }

                if (FiltroStatus == "ativo")
                {
                    query = query.Where(c => c.Pago);
                }

                if (FiltroStatus == "alteracao")
                {
                    query = query.Where(c => c.ClinicaAlteracaoPendente);
                }
            }

            Clinicas = query
                .OrderByDescending(c => c.ClinicaAlteracaoPendente)
                .ThenBy(c => c.Aprovado)
                .ThenBy(c => c.Pago)
                .ThenBy(c => c.Nome)
                .ToList();
        }

        private void AtualizarDivulgacoesExpiradas()
        {
            var divulgacoesPendentes = _context.DivulgacoesRegionais
                .Where(d =>
                    !d.Pago &&
                    !d.Aprovado &&
                    d.Status != "Cancelado" &&
                    d.Status != "Expirado")
                .ToList();

            var houveAlteracao = false;

            foreach (var divulgacao in divulgacoesPendentes)
            {
                var prazoExpirado = divulgacao.DataSolicitacao.AddHours(72) < DateTime.Now;

                if (prazoExpirado)
                {
                    divulgacao.Status = "Expirado";
                    divulgacao.Ativo = false;
                    houveAlteracao = true;
                }
            }

            if (houveAlteracao)
            {
                _context.SaveChanges();
            }
        }

        private void RegistrarLog(string acao, string descricao)
        {
            var adminIdTexto = HttpContext.Session.GetString("AdminId");

            if (!int.TryParse(adminIdTexto, out int adminId))
            {
                return;
            }

            var nomeAdmin = HttpContext.Session.GetString("AdminNome") ?? "Administrador";
            var perfilAdmin = HttpContext.Session.GetString("AdminPerfil") ?? "Não informado";

            _context.AdminLogs.Add(new AdminLog
            {
                AdminUsuarioId = adminId,
                NomeAdmin = nomeAdmin,
                PerfilAdmin = perfilAdmin,
                Acao = acao,
                Descricao = descricao,
                DataAcao = DateTime.Now
            });
        }

    }
}