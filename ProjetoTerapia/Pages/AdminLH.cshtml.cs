using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProjetoTerapia.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;


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

        public decimal ReceitaPlanosPrincipais { get; set; }

        public decimal ReceitaDivulgacaoRegional { get; set; }

        public string ReceitaPlanosPrincipaisFormatada { get; set; } = "R$ 0,00";

        public string ReceitaDivulgacaoRegionalFormatada { get; set; } = "R$ 0,00";

        public int DivulgacoesAtivas { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Busca { get; set; } = "";

        [BindProperty(SupportsGet = true)]
        public string FiltroStatus { get; set; } = "";
        public List<DivulgacaoRegional> DivulgacoesRegionais { get; set; } = new();
        public int DivulgacoesPendentes { get; set; }
        public List<AdminLog> AdminLogs { get; set; } = new();
        public List<Vendedor> Vendedores { get; set; } = new();
        public List<VendaVendedor> VendasVendedores { get; set; } = new();

        [BindProperty]
        public string NovoVendedorNome { get; set; } = "";

        [BindProperty]
        public string NovoVendedorEmail { get; set; } = "";

        [BindProperty]
        public string NovoVendedorTelefone { get; set; } = "";

        [BindProperty]
        public string NovoVendedorSenha { get; set; } = "";

        [BindProperty]
        public decimal NovoVendedorComissao { get; set; } = 20;

        [BindProperty]
        public string NovoVendedorPix { get; set; } = "";

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

            if (acao == "cadastrarVendedor")
            {
                if (string.IsNullOrWhiteSpace(NovoVendedorNome) ||
                    string.IsNullOrWhiteSpace(NovoVendedorEmail) ||
                    string.IsNullOrWhiteSpace(NovoVendedorSenha))
                {
                    TempData["MensagemErro"] = "Preencha nome, email e senha do vendedor.";
                    return RedirectToPage(new { aba = "vendedores" });
                }

                var emailNormalizado = NovoVendedorEmail.Trim().ToLower();

                var vendedorExiste = _context.Vendedores
                    .Any(v => v.Email.ToLower() == emailNormalizado);

                if (vendedorExiste)
                {
                    TempData["MensagemErro"] = "Já existe um vendedor cadastrado com este email.";
                    return RedirectToPage(new { aba = "vendedores" });
                }

                var vendedor = new Vendedor
                {
                    Nome = NovoVendedorNome.Trim(),
                    Email = emailNormalizado,
                    Telefone = NovoVendedorTelefone?.Trim() ?? "",
                    CodigoIndicacao = GerarCodigoIndicacao(NovoVendedorNome),
                    PercentualComissao = NovoVendedorComissao <= 0 ? 20 : NovoVendedorComissao,
                    ChavePix = NovoVendedorPix?.Trim(),
                    Ativo = true,
                    DataCadastro = DateTime.Now
                };

                var passwordHasher = new PasswordHasher<Vendedor>();
                vendedor.SenhaHash = passwordHasher.HashPassword(vendedor, NovoVendedorSenha);

                _context.Vendedores.Add(vendedor);

                RegistrarLog(
                    "Cadastro de vendedor",
                    $"{HttpContext.Session.GetString("AdminNome")} cadastrou o vendedor {vendedor.Nome} com o código {vendedor.CodigoIndicacao}."
                );

                _context.SaveChanges();

                TempData["MensagemSucesso"] = "Vendedor cadastrado com sucesso.";

                return RedirectToPage(new { aba = "vendedores" });
            }

            if (acao == "ativarVendedor" || acao == "desativarVendedor")
            {
                var vendedor = _context.Vendedores.FirstOrDefault(v => v.Id == id);

                if (vendedor == null)
                {
                    TempData["MensagemErro"] = "Vendedor não encontrado.";
                    return RedirectToPage(new { aba = "vendedores" });
                }

                if (acao == "ativarVendedor")
                {
                    vendedor.Ativo = true;

                    RegistrarLog(
                        "Ativação de vendedor",
                        $"{HttpContext.Session.GetString("AdminNome")} ativou o vendedor {vendedor.Nome}."
                    );

                    TempData["MensagemSucesso"] = "Vendedor ativado com sucesso.";
                }

                if (acao == "desativarVendedor")
                {
                    vendedor.Ativo = false;

                    RegistrarLog(
                        "Desativação de vendedor",
                        $"{HttpContext.Session.GetString("AdminNome")} desativou o vendedor {vendedor.Nome}."
                    );

                    TempData["MensagemSucesso"] = "Vendedor desativado.";
                }

                _context.SaveChanges();

                return RedirectToPage(new { aba = "vendedores" });
            }

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

            if (acao == "ativarDivulgacao" ||
                acao == "cancelarDivulgacao" ||
                acao == "excluirDivulgacao")
            {
                var divulgacao = _context.DivulgacoesRegionais
                    .Include(d => d.Clinica)
                    .FirstOrDefault(d => d.Id == id);

                if (divulgacao == null)
                {
                    TempData["MensagemErro"] = "Divulgação regional não encontrada.";
                    return RedirectToPage(new { aba = "divulgacao" });
                }

                if (acao == "ativarDivulgacao")
                {
                    if (!divulgacao.Pago)
                    {
                        TempData["MensagemErro"] = "Esta divulgação ainda não possui pagamento confirmado.";
                        return RedirectToPage(new { aba = "divulgacao" });
                    }

                    var divulgacoesAntigas = _context.DivulgacoesRegionais
                        .Where(d =>
                            d.ClinicaId == divulgacao.ClinicaId &&
                            d.Id != divulgacao.Id &&
                            d.Ativo)
                        .ToList();

                    foreach (var antiga in divulgacoesAntigas)
                    {
                        antiga.Ativo = false;
                        antiga.Status = "Substituído";
                    }

                    divulgacao.Aprovado = true;
                    divulgacao.Ativo = true;
                    divulgacao.Status = "Ativo";
                    divulgacao.DataAprovacao = DateTime.Now;
                    divulgacao.DataInicio = DateTime.Now;
                    divulgacao.DataFim = DateTime.Now.AddMonths(1);

                    RegistrarLog(
                        "Ativação de divulgação regional",
                        $"{HttpContext.Session.GetString("AdminNome")} ativou a divulgação regional de {divulgacao.Clinica?.Nome}. Cidades: {divulgacao.CidadesSelecionadas}."
                    );

                    TempData["MensagemSucesso"] = "Divulgação regional ativada com sucesso.";
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

                if (acao == "excluirDivulgacao")
                {
                    divulgacao.Ativo = false;
                    divulgacao.Status = "Excluído";

                    RegistrarLog(
                        "Exclusão de registro de divulgação regional",
                        $"{HttpContext.Session.GetString("AdminNome")} ocultou o registro de divulgação regional de {divulgacao.Clinica?.Nome}."
                    );

                    TempData["MensagemSucesso"] = "Registro de divulgação removido da listagem.";
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
                clinica.ValorPlano = 450;
                clinica.DataPagamento = DateTime.Now;
                clinica.DataVencimento = DateTime.Now.AddYears(1);

                if (clinica.VendedorId.HasValue)
                {
                    var vendaJaExiste = _context.VendasVendedores
                        .Any(v => v.ClinicaId == clinica.Id && v.VendaConfirmada);

                    if (!vendaJaExiste)
                    {
                        var vendedor = _context.Vendedores
                            .FirstOrDefault(v => v.Id == clinica.VendedorId.Value && v.Ativo);

                        if (vendedor != null)
                        {
                            var valorVenda = clinica.ValorPlano ?? 450;
                            var percentualComissao = vendedor.PercentualComissao;
                            var valorComissao = valorVenda * percentualComissao / 100;

                            _context.VendasVendedores.Add(new VendaVendedor
                            {
                                VendedorId = vendedor.Id,
                                ClinicaId = clinica.Id,
                                CodigoIndicacao = vendedor.CodigoIndicacao,
                                NomeClinica = clinica.Nome,
                                EmailClinica = clinica.Email,
                                ValorVenda = valorVenda,
                                PercentualComissao = percentualComissao,
                                ValorComissao = valorComissao,
                                VendaConfirmada = true,
                                ComissaoPaga = false,
                                Status = "Venda confirmada - aguardando nota",
                                DataCadastro = DateTime.Now,
                                DataConfirmacaoVenda = DateTime.Now
                            });

                            RegistrarLog(
                                "Venda vinculada ao vendedor",
                                $"{HttpContext.Session.GetString("AdminNome")} confirmou venda de {clinica.Nome} para o vendedor {vendedor.Nome}. Comissão: {valorComissao.ToString("C", new System.Globalization.CultureInfo("pt-BR"))}."
                            );
                        }
                    }
                }

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

        public IActionResult OnPostExcluirDivulgacao(int id)
        {
            if (!AdminEstaLogado())
            {
                return RedirectToPage("/LoginAdmin");
            }

            var divulgacao = _context.DivulgacoesRegionais
                .Include(d => d.Clinica)
                .FirstOrDefault(d => d.Id == id);

            if (divulgacao == null)
            {
                TempData["MensagemErro"] = "Registro de divulgação não encontrado.";
                return RedirectToPage(new { aba = "divulgacao" });
            }

            divulgacao.Ativo = false;
            divulgacao.Status = "Excluído";

            RegistrarLog(
                "Exclusão de registro de divulgação regional",
                $"{HttpContext.Session.GetString("AdminNome")} removeu da listagem a divulgação regional de {divulgacao.Clinica?.Nome}."
            );

            _context.SaveChanges();

            TempData["MensagemSucesso"] = "Registro de divulgação removido da listagem.";

            return RedirectToPage(new { aba = "divulgacao" });
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
             .Where(d => d.Status != "Excluído")
             .OrderByDescending(d => d.Ativo)
             .ThenByDescending(d => d.Pago)
             .ThenByDescending(d => d.DataSolicitacao)
             .ToList();

            AdminLogs = _context.AdminLogs
             .OrderByDescending(l => l.DataAcao)
             .Take(100)
             .ToList();

            Vendedores = _context.Vendedores
             .OrderByDescending(v => v.Ativo)
             .ThenBy(v => v.Nome)
             .ToList();

            VendasVendedores = _context.VendasVendedores
                .Include(v => v.Vendedor)
                .Include(v => v.Clinica)
                .OrderByDescending(v => v.DataCadastro)
                .Take(100)
                .ToList();

            DivulgacoesPendentes = DivulgacoesRegionais
                .Count(d => !d.Pago && d.Status == "Aguardando pagamento");

            DivulgacoesAtivas = DivulgacoesRegionais
                .Count(d => d.Pago && d.Aprovado && d.Ativo);

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

            ReceitaPlanosPrincipais = todasClinicas
              .Where(c => c.Pago)
              .Sum(c => c.ValorPlano ?? 450);

            ReceitaDivulgacaoRegional = DivulgacoesRegionais
                .Where(d =>
                    d.Pago &&
                    d.Status != "Pagamento estornado" &&
                    d.Status != "Pagamento recusado")
                .Sum(d => d.Valor);

            ReceitaPrevista = ReceitaPlanosPrincipais + ReceitaDivulgacaoRegional;

            ReceitaPlanosPrincipaisFormatada =
                ReceitaPlanosPrincipais.ToString("C", new CultureInfo("pt-BR"));

            ReceitaDivulgacaoRegionalFormatada =
                ReceitaDivulgacaoRegional.ToString("C", new CultureInfo("pt-BR"));

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
            var houveAlteracao = false;

            var divulgacoesSemPagamento = _context.DivulgacoesRegionais
               .Where(d =>
                !d.Pago &&
                !d.Aprovado &&
                 d.Status != "Cancelado" &&
                 d.Status != "Expirado" &&
                 d.Status != "Excluído" &&
                 d.Status != "Substituído")
               .ToList();

            foreach (var divulgacao in divulgacoesSemPagamento)
            {
                var prazoExpirado = divulgacao.DataSolicitacao.AddHours(72) < DateTime.Now;

                if (prazoExpirado)
                {
                    divulgacao.Status = "Expirado";
                    divulgacao.Ativo = false;
                    houveAlteracao = true;
                }
            }

            var divulgacoesAtivasVencidas = _context.DivulgacoesRegionais
                .Where(d =>
                    d.Ativo &&
                    d.DataFim.HasValue &&
                    d.DataFim.Value.Date < DateTime.Today)
                .ToList();

            foreach (var divulgacao in divulgacoesAtivasVencidas)
            {
                divulgacao.Ativo = false;
                divulgacao.Status = "Expirado";
                houveAlteracao = true;
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

        private string GerarCodigoIndicacao(string nome)
        {
            string codigo;

            do
            {
                codigo = "VEN" + RandomNumberGenerator.GetInt32(100000, 999999);
            }
            while (_context.Vendedores.Any(v => v.CodigoIndicacao == codigo));

            return codigo;
        }

    }
}