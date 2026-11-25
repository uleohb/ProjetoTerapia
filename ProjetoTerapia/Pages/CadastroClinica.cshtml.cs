using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
        public string? CodigoVendedor { get; set; } = "";

        // =====================================================
        // GET
        // =====================================================

        public IActionResult OnGet(string? vendedor)
        {
            CapturarCodigoVendedor(vendedor);

            var id =
                HttpContext.Session.GetString(
                    "ClinicaLogada"
                );

            if (!string.IsNullOrWhiteSpace(id) &&
                int.TryParse(id, out int clinicaId))
            {
                var clinica =
                    _context.Clinicas
                        .FirstOrDefault(
                            c => c.Id == clinicaId
                        );

                if (clinica != null)
                {
                    NovaClinica = clinica;

                    if (!string.IsNullOrWhiteSpace(
                        clinica.Especialidades))
                    {
                        EspecialidadesSelecionadas =
                            clinica.Especialidades
                                .Split(',')
                                .Select(
                                    e => e.Trim()
                                )
                                .Where(
                                    e =>
                                        !string.IsNullOrWhiteSpace(e)
                                )
                                .ToList();
                    }
                }
            }

            return Page();
        }

        // =====================================================
        // POST
        // =====================================================

        public IActionResult OnPost()
        {
            ModelState.Remove(
                nameof(CodigoVendedor)
            );

            CapturarCodigoVendedor(
                CodigoVendedor
            );

            var id =
                HttpContext.Session.GetString(
                    "ClinicaLogada"
                );

            Clinica clinica;

            var editandoPerfil =
                !string.IsNullOrWhiteSpace(id);

            // =================================================
            // EDIÇÃO
            // =================================================

            if (editandoPerfil)
            {
                if (!int.TryParse(
                    id,
                    out int clinicaId))
                {
                    return RedirectToPage(
                        "/LoginClinica"
                    );
                }

                var clinicaExistente =
                    _context.Clinicas
                        .FirstOrDefault(
                            c => c.Id == clinicaId
                        );

                if (clinicaExistente == null)
                {
                    return RedirectToPage(
                        "/LoginClinica"
                    );
                }

                clinica =
                    clinicaExistente;

                /*
                 * Não existe mais aprovação manual.
                 *
                 * Mantemos Aprovado = true somente
                 * por compatibilidade com partes antigas
                 * do sistema.
                 */
                clinica.Aprovado =
                    true;

                clinica.ClinicaAlteracaoPendente =
                    false;
            }

            // =================================================
            // NOVO CADASTRO
            // =================================================

            else
            {
                clinica =
                    new Clinica
                    {
                        /*
                         * Cadastro liberado automaticamente.
                         * Não existe mais aprovação manual.
                         */
                        Aprovado = true,

                        Pago = false,

                        PerfilCompleto = false,

                        ClinicaAlteracaoPendente =
                            false
                    };

                AplicarVendedorNaClinica(
                    clinica
                );

                _context.Clinicas.Add(
                    clinica
                );
            }

            // =================================================
            // TIPO DE ATENDIMENTO
            // =================================================

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

            // =================================================
            // NORMALIZAÇÃO
            // =================================================

            NormalizarCPF();

            NormalizarCEP();

            NormalizarTelefone();

            NormalizarInstagram();

            NormalizarSite();

            // =================================================
            // VALIDAÇÕES
            // =================================================

            if (!ValidarDadosDaClinica(
                clinica))
            {
                RecarregarEspecialidadesSelecionadas();

                return Page();
            }

            // =================================================
            // ESPECIALIDADES
            // =================================================

            var especialidadesTexto =
                ObterEspecialidadesFormatadas();

            // =================================================
            // FOTO
            // =================================================

            var fotoNova =
                SalvarFotoRecortada();

            // =================================================
            // SALVAR DIRETAMENTE
            // =================================================
            //
            // Não existe mais fluxo de alteração pendente
            // ou aprovação pelo administrador.
            //

            AtualizarClinicaDireto(
                clinica,
                especialidadesTexto,
                fotoNova
            );

            /*
             * Garante compatibilidade com qualquer
             * código antigo que ainda consulte Aprovado.
             */
            clinica.Aprovado =
                true;

            clinica.ClinicaAlteracaoPendente =
                false;

            _context.SaveChanges();

            // =================================================
            // NOVO CADASTRO
            // =================================================

            if (!editandoPerfil)
            {
                HttpContext.Session.Remove(
                    "PacienteLogado"
                );

                HttpContext.Session.SetString(
                    "ClinicaLogada",
                    clinica.Id.ToString()
                );

                TempData["Sucesso"] =
                    "Cadastro concluído. Agora ative seu plano para iniciar os 7 dias grátis.";

                return RedirectToPage(
                    "/PagamentoClinica"
                );
            }

            // =================================================
            // EDIÇÃO
            // =================================================

            TempData["Sucesso"] =
                "Alterações salvas com sucesso.";

            return RedirectToPage(
                "/CadastroClinica"
            );
        }

        // =====================================================
        // ATUALIZAR CLÍNICA
        // =====================================================

        private void AtualizarClinicaDireto(
            Clinica clinica,
            string especialidadesTexto,
            string? fotoNova)
        {
            clinica.Especialidades =
                especialidadesTexto;

            clinica.Nome =
                NovaClinica.Nome;

            if (!string.IsNullOrWhiteSpace(
                NovaClinica.Email))
            {
                clinica.Email =
                    NovaClinica.Email;
            }

            clinica.Descricao =
                NovaClinica.Descricao;

            clinica.CEP =
                NovaClinica.CEP;

            clinica.Cidade =
                NovaClinica.Cidade;

            clinica.Endereco =
                NovaClinica.Endereco;

            clinica.Telefone =
                NovaClinica.Telefone;

            clinica.Instagram =
                NovaClinica.Instagram ?? "";

            clinica.Site =
                NovaClinica.Site ?? "";

            clinica.Documento =
                NovaClinica.Documento;

            clinica.CPF =
                NovaClinica.CPF;

            clinica.Valor =
                NovaClinica.Valor;

            clinica.AtendimentoOnline =
                NovaClinica.AtendimentoOnline;

            clinica.AtendimentoPresencial =
                NovaClinica.AtendimentoPresencial;

            if (!string.IsNullOrWhiteSpace(
                fotoNova))
            {
                clinica.FotoPerfil =
                    fotoNova;
            }

            clinica.PerfilCompleto =
                true;
        }

        // =====================================================
        // ESPECIALIDADES
        // =====================================================

        private string ObterEspecialidadesFormatadas()
        {
            var especialidades =
                Request.Form["Especialidades"];

            var lista =
                especialidades
                    .Select(
                        e => e?.Trim() ?? ""
                    )
                    .Where(
                        e =>
                            !string.IsNullOrWhiteSpace(e)
                    )
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase
                    )
                    .Take(20)
                    .ToList();

            return string.Join(
                ",",
                lista
            );
        }

        // =====================================================
        // TELEFONE
        // =====================================================

        private void NormalizarTelefone()
        {
            var telefone =
                ApenasNumeros(
                    NovaClinica.Telefone
                );

            if (string.IsNullOrWhiteSpace(
                telefone))
            {
                NovaClinica.Telefone =
                    "";

                return;
            }

            if ((telefone.Length == 10 ||
                 telefone.Length == 11) &&
                !telefone.StartsWith("55"))
            {
                telefone =
                    "55" + telefone;
            }

            NovaClinica.Telefone =
                telefone;
        }

        // =====================================================
        // INSTAGRAM
        // =====================================================

        private void NormalizarInstagram()
        {
            if (string.IsNullOrWhiteSpace(
                NovaClinica.Instagram))
            {
                return;
            }

            NovaClinica.Instagram =
                NovaClinica.Instagram
                    .Replace("@", "")
                    .Trim();

            if (!NovaClinica.Instagram.StartsWith(
                "http",
                StringComparison.OrdinalIgnoreCase))
            {
                NovaClinica.Instagram =
                    "https://instagram.com/" +
                    NovaClinica.Instagram;
            }
        }

        // =====================================================
        // SITE
        // =====================================================

        private void NormalizarSite()
        {
            if (string.IsNullOrWhiteSpace(
                NovaClinica.Site))
            {
                return;
            }

            NovaClinica.Site =
                NovaClinica.Site.Trim();

            if (!NovaClinica.Site.StartsWith(
                "http",
                StringComparison.OrdinalIgnoreCase))
            {
                NovaClinica.Site =
                    "https://" +
                    NovaClinica.Site;
            }
        }

        // =====================================================
        // FOTO
        // =====================================================

        private string? SalvarFotoRecortada()
        {
            if (string.IsNullOrWhiteSpace(
                FotoFinal))
            {
                return null;
            }

            try
            {
                var base64 =
                    FotoFinal;

                if (base64.Contains(","))
                {
                    base64 =
                        base64.Split(',')[1];
                }

                var bytes =
                    Convert.FromBase64String(
                        base64
                    );

                var pastaUploads =
                    Path.Combine(
                        _environment.WebRootPath,
                        "uploads"
                    );

                if (!Directory.Exists(
                    pastaUploads))
                {
                    Directory.CreateDirectory(
                        pastaUploads
                    );
                }

                var nomeArquivo =
                    Guid.NewGuid()
                        .ToString() +
                    ".jpg";

                var caminhoArquivo =
                    Path.Combine(
                        pastaUploads,
                        nomeArquivo
                    );

                System.IO.File.WriteAllBytes(
                    caminhoArquivo,
                    bytes
                );

                return "/uploads/" +
                       nomeArquivo;
            }
            catch
            {
                ModelState.AddModelError(
                    "",
                    "Não foi possível salvar a foto enviada."
                );

                return null;
            }
        }

        // =====================================================
        // RECARREGAR ESPECIALIDADES
        // =====================================================

        private void RecarregarEspecialidadesSelecionadas()
        {
            var especialidades =
                Request.Form["Especialidades"];

            EspecialidadesSelecionadas =
                especialidades
                    .Select(
                        e => e ?? ""
                    )
                    .Where(
                        e =>
                            !string.IsNullOrWhiteSpace(e)
                    )
                    .ToList();
        }

        // =====================================================
        // VENDEDOR
        // =====================================================

        private void CapturarCodigoVendedor(
            string? codigo)
        {
            if (string.IsNullOrWhiteSpace(
                codigo))
            {
                return;
            }

            var codigoNormalizado =
                codigo
                    .Trim()
                    .ToUpperInvariant();

            var vendedor =
                _context.Vendedores
                    .FirstOrDefault(
                        v =>
                            v.CodigoIndicacao ==
                                codigoNormalizado
                            &&
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

            CodigoVendedor =
                vendedor.CodigoIndicacao;
        }

        private Vendedor? BuscarVendedorDaSessao()
        {
            var codigo =
                HttpContext.Session.GetString(
                    "CodigoVendedorIndicacao"
                );

            if (string.IsNullOrWhiteSpace(
                codigo))
            {
                return null;
            }

            return _context.Vendedores
                .FirstOrDefault(
                    v =>
                        v.CodigoIndicacao ==
                            codigo
                        &&
                        v.Ativo
                );
        }

        private void AplicarVendedorNaClinica(
            Clinica clinica)
        {
            if (clinica.VendedorId.HasValue)
            {
                return;
            }

            var vendedor =
                BuscarVendedorDaSessao();

            if (vendedor == null)
            {
                return;
            }

            clinica.VendedorId =
                vendedor.Id;

            clinica.CodigoVendedorIndicacao =
                vendedor.CodigoIndicacao;
        }

        // =====================================================
        // CPF / CEP
        // =====================================================

        private void NormalizarCPF()
        {
            NovaClinica.CPF =
                ApenasNumeros(
                    NovaClinica.CPF
                );
        }

        private void NormalizarCEP()
        {
            NovaClinica.CEP =
                ApenasNumeros(
                    NovaClinica.CEP
                );
        }

        private string ApenasNumeros(
            string? valor)
        {
            if (string.IsNullOrWhiteSpace(
                valor))
            {
                return "";
            }

            return new string(
                valor
                    .Where(char.IsDigit)
                    .ToArray()
            );
        }

        // =====================================================
        // VALIDAÇÃO DOS DADOS
        // =====================================================

        private bool ValidarDadosDaClinica(
            Clinica clinicaAtual)
        {
            var valido =
                true;

            var clinicasExistentes =
                _context.Clinicas
                    .Where(
                        c =>
                            c.Id !=
                            clinicaAtual.Id
                    )
                    .ToList();

            // =================================================
            // CPF
            // =================================================

            if (string.IsNullOrWhiteSpace(
                NovaClinica.CPF))
            {
                ModelState.AddModelError(
                    "NovaClinica.CPF",
                    "Informe o CPF."
                );

                valido =
                    false;
            }
            else if (!CpfValido(
                NovaClinica.CPF))
            {
                ModelState.AddModelError(
                    "NovaClinica.CPF",
                    "Informe um CPF válido."
                );

                valido =
                    false;
            }
            else if (clinicasExistentes.Any(
                c =>
                    ApenasNumeros(c.CPF) ==
                    NovaClinica.CPF))
            {
                ModelState.AddModelError(
                    "NovaClinica.CPF",
                    "Este CPF já está em uso."
                );

                valido =
                    false;
            }

            // =================================================
            // TELEFONE
            // =================================================

            if (string.IsNullOrWhiteSpace(
                NovaClinica.Telefone))
            {
                ModelState.AddModelError(
                    "NovaClinica.Telefone",
                    "Informe o telefone."
                );

                valido =
                    false;
            }
            else if (!TelefoneValido(
                NovaClinica.Telefone))
            {
                ModelState.AddModelError(
                    "NovaClinica.Telefone",
                    "Informe um telefone válido com DDD."
                );

                valido =
                    false;
            }
            else if (clinicasExistentes.Any(
                c =>
                    ApenasNumeros(c.Telefone) ==
                    NovaClinica.Telefone))
            {
                ModelState.AddModelError(
                    "NovaClinica.Telefone",
                    "Este telefone já está em uso."
                );

                valido =
                    false;
            }

            // =================================================
            // CEP
            // =================================================

            if (!string.IsNullOrWhiteSpace(
                    NovaClinica.CEP)
                &&
                NovaClinica.CEP.Length != 8)
            {
                ModelState.AddModelError(
                    "NovaClinica.CEP",
                    "Informe um CEP válido com 8 números."
                );

                valido =
                    false;
            }

            // =================================================
            // CIDADE
            // =================================================

            if (string.IsNullOrWhiteSpace(
                NovaClinica.Cidade))
            {
                ModelState.AddModelError(
                    "NovaClinica.Cidade",
                    "Informe a cidade principal de atendimento."
                );

                valido =
                    false;
            }

            // =================================================
            // REGISTRO PROFISSIONAL
            // =================================================

            if (!string.IsNullOrWhiteSpace(
                NovaClinica.Documento))
            {
                NovaClinica.Documento =
                    NovaClinica.Documento
                        .Trim()
                        .ToUpperInvariant();

                var documentoJaExiste =
                    clinicasExistentes.Any(
                        c =>
                            !string.IsNullOrWhiteSpace(
                                c.Documento
                            )
                            &&
                            c.Documento
                                .Trim()
                                .ToUpper() ==
                            NovaClinica.Documento
                    );

                if (documentoJaExiste)
                {
                    ModelState.AddModelError(
                        "NovaClinica.Documento",
                        "Este registro profissional já está em uso."
                    );

                    valido =
                        false;
                }
            }

            // =================================================
            // INSTAGRAM
            // =================================================

            var instagramAtual =
                NormalizarInstagramParaComparacao(
                    NovaClinica.Instagram
                );

            if (!string.IsNullOrWhiteSpace(
                instagramAtual))
            {
                var instagramJaExiste =
                    clinicasExistentes.Any(
                        c =>
                            NormalizarInstagramParaComparacao(
                                c.Instagram
                            )
                            ==
                            instagramAtual
                    );

                if (instagramJaExiste)
                {
                    ModelState.AddModelError(
                        "NovaClinica.Instagram",
                        "Este Instagram já está em uso."
                    );

                    valido =
                        false;
                }
            }

            return valido;
        }

        // =====================================================
        // NORMALIZAR INSTAGRAM PARA COMPARAÇÃO
        // =====================================================

        private string NormalizarInstagramParaComparacao(
            string? instagram)
        {
            if (string.IsNullOrWhiteSpace(
                instagram))
            {
                return "";
            }

            var valor =
                instagram
                    .Trim()
                    .ToLowerInvariant();

            valor =
                valor.Replace(
                    "https://",
                    ""
                );

            valor =
                valor.Replace(
                    "http://",
                    ""
                );

            valor =
                valor.Replace(
                    "www.",
                    ""
                );

            if (valor.StartsWith(
                "instagram.com/"))
            {
                valor =
                    valor.Replace(
                        "instagram.com/",
                        ""
                    );
            }

            if (valor.StartsWith("@"))
            {
                valor =
                    valor.Substring(1);
            }

            valor =
                valor.Split("?")[0];

            valor =
                valor
                    .Trim()
                    .Trim('/');

            return valor;
        }

        // =====================================================
        // TELEFONE VÁLIDO
        // =====================================================

        private bool TelefoneValido(
            string telefone)
        {
            if (string.IsNullOrWhiteSpace(
                telefone))
            {
                return false;
            }

            if (!telefone.StartsWith("55"))
            {
                return false;
            }

            return telefone.Length == 12 ||
                   telefone.Length == 13;
        }

        // =====================================================
        // CPF VÁLIDO
        // =====================================================

        private bool CpfValido(
            string cpf)
        {
            cpf =
                ApenasNumeros(
                    cpf
                );

            if (cpf.Length != 11)
            {
                return false;
            }

            if (cpf.All(
                c => c == cpf[0]))
            {
                return false;
            }

            var soma =
                0;

            for (var i = 0;
                 i < 9;
                 i++)
            {
                soma +=
                    (cpf[i] - '0') *
                    (10 - i);
            }

            var resto =
                soma % 11;

            var digito1 =
                resto < 2
                    ? 0
                    : 11 - resto;

            if ((cpf[9] - '0') !=
                digito1)
            {
                return false;
            }

            soma =
                0;

            for (var i = 0;
                 i < 10;
                 i++)
            {
                soma +=
                    (cpf[i] - '0') *
                    (11 - i);
            }

            resto =
                soma % 11;

            var digito2 =
                resto < 2
                    ? 0
                    : 11 - resto;

            return (cpf[10] - '0') ==
                   digito2;
        }
    }
}