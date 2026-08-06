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

        [BindProperty]
        public string CodigoVendedor { get; set; } = "";

        public IActionResult OnGet(string? vendedor)
        {
            CapturarCodigoVendedor(vendedor);

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

            CapturarCodigoVendedor(CodigoVendedor);

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

                AplicarVendedorNaClinica(clinica);

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

            NormalizarCPF();
            NormalizarCEP();
            NormalizarTelefone();
            NormalizarInstagram();
            NormalizarSite();

            if (!ValidarDadosDaClinica(clinica))
            {
                RecarregarEspecialidadesSelecionadas();
                return Page();
            }

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

            if (!string.IsNullOrWhiteSpace(NovaClinica.Email))
            {
                clinica.Email = NovaClinica.Email;
            }

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
            alteracao.Email = clinica.Email;
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
            var telefone = ApenasNumeros(NovaClinica.Telefone);

            if (string.IsNullOrWhiteSpace(telefone))
            {
                NovaClinica.Telefone = "";
                return;
            }

            if ((telefone.Length == 10 || telefone.Length == 11) &&
                !telefone.StartsWith("55"))
            {
                telefone = "55" + telefone;
            }

            NovaClinica.Telefone = telefone;
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

        private void CapturarCodigoVendedor(string? codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                return;
            }

            var codigoNormalizado = codigo.Trim().ToUpper();

            var vendedor = _context.Vendedores
                .FirstOrDefault(v =>
                    v.CodigoIndicacao == codigoNormalizado &&
                    v.Ativo
                );

            if (vendedor == null)
            {
                return;
            }

            HttpContext.Session.SetString(
                "CodigoVendedorIndicacao",
                vendedor.CodigoIndicacao
            );

            CodigoVendedor = vendedor.CodigoIndicacao;
        }

        private Vendedor? BuscarVendedorDaSessao()
        {
            var codigo = HttpContext.Session.GetString("CodigoVendedorIndicacao");

            if (string.IsNullOrWhiteSpace(codigo))
            {
                return null;
            }

            return _context.Vendedores
                .FirstOrDefault(v =>
                    v.CodigoIndicacao == codigo &&
                    v.Ativo
                );
        }

        private void AplicarVendedorNaClinica(Clinica clinica)
        {
            if (clinica.VendedorId.HasValue)
            {
                return;
            }

            var vendedor = BuscarVendedorDaSessao();

            if (vendedor == null)
            {
                return;
            }

            clinica.VendedorId = vendedor.Id;
            clinica.CodigoVendedorIndicacao = vendedor.CodigoIndicacao;
        }

        private void NormalizarCPF()
        {
            NovaClinica.CPF = ApenasNumeros(NovaClinica.CPF);
        }

        private void NormalizarCEP()
        {
            NovaClinica.CEP = ApenasNumeros(NovaClinica.CEP);
        }

        private string ApenasNumeros(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return "";
            }

            return new string(valor.Where(char.IsDigit).ToArray());
        }

        private bool ValidarDadosDaClinica(Clinica clinicaAtual)
        {
            var valido = true;

            var clinicasExistentes = _context.Clinicas
                .Where(c => c.Id != clinicaAtual.Id)
                .ToList();

            if (string.IsNullOrWhiteSpace(NovaClinica.CPF))
            {
                ModelState.AddModelError("NovaClinica.CPF", "Informe o CPF.");
                valido = false;
            }
            else if (!CpfValido(NovaClinica.CPF))
            {
                ModelState.AddModelError("NovaClinica.CPF", "Informe um CPF válido.");
                valido = false;
            }
            else if (clinicasExistentes.Any(c => ApenasNumeros(c.CPF) == NovaClinica.CPF))
            {
                ModelState.AddModelError("NovaClinica.CPF", "Este CPF já está em uso.");
                valido = false;
            }

            if (string.IsNullOrWhiteSpace(NovaClinica.Telefone))
            {
                ModelState.AddModelError("NovaClinica.Telefone", "Informe o telefone.");
                valido = false;
            }
            else if (!TelefoneValido(NovaClinica.Telefone))
            {
                ModelState.AddModelError("NovaClinica.Telefone", "Informe um telefone válido com DDD.");
                valido = false;
            }
            else if (clinicasExistentes.Any(c => ApenasNumeros(c.Telefone) == NovaClinica.Telefone))
            {
                ModelState.AddModelError("NovaClinica.Telefone", "Este telefone já está em uso.");
                valido = false;
            }

            if (!string.IsNullOrWhiteSpace(NovaClinica.CEP) &&
                NovaClinica.CEP.Length != 8)
            {
                ModelState.AddModelError("NovaClinica.CEP", "Informe um CEP válido com 8 números.");
                valido = false;
            }

            if (string.IsNullOrWhiteSpace(NovaClinica.Cidade))
            {
                ModelState.AddModelError("NovaClinica.Cidade", "Informe a cidade principal de atendimento.");
                valido = false;
            }

            if (!string.IsNullOrWhiteSpace(NovaClinica.Documento))
            {
                NovaClinica.Documento = NovaClinica.Documento.Trim().ToUpper();

                var documentoJaExiste = clinicasExistentes.Any(c =>
                    !string.IsNullOrWhiteSpace(c.Documento) &&
                    c.Documento.Trim().ToUpper() == NovaClinica.Documento
                );

                if (documentoJaExiste)
                {
                    ModelState.AddModelError("NovaClinica.Documento", "Este registro profissional já está em uso.");
                    valido = false;
                }
            }

            return valido;
        }

        private bool TelefoneValido(string telefone)
        {
            if (string.IsNullOrWhiteSpace(telefone))
            {
                return false;
            }

            if (!telefone.StartsWith("55"))
            {
                return false;
            }

            return telefone.Length == 12 || telefone.Length == 13;
        }

        private bool CpfValido(string cpf)
        {
            cpf = ApenasNumeros(cpf);

            if (cpf.Length != 11)
            {
                return false;
            }

            if (cpf.All(c => c == cpf[0]))
            {
                return false;
            }

            var soma = 0;

            for (var i = 0; i < 9; i++)
            {
                soma += (cpf[i] - '0') * (10 - i);
            }

            var resto = soma % 11;
            var digito1 = resto < 2 ? 0 : 11 - resto;

            if ((cpf[9] - '0') != digito1)
            {
                return false;
            }

            soma = 0;

            for (var i = 0; i < 10; i++)
            {
                soma += (cpf[i] - '0') * (11 - i);
            }

            resto = soma % 11;
            var digito2 = resto < 2 ? 0 : 11 - resto;

            return (cpf[10] - '0') == digito2;
        }
    }
}