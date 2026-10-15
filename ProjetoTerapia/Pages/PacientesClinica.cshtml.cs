using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProjetoTerapia.Models;

namespace ProjetoTerapia.Pages
{
    public class PacientesClinicaModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PacientesClinicaModel> _logger;


        public PacientesClinicaModel(
            AppDbContext context,
            ILogger<PacientesClinicaModel> logger)
        {
            _context = context;
            _logger = logger;
        }


        // =====================================================
        // DADOS DA TELA
        // =====================================================

        public Clinica Clinica { get; set; } = new();


        public List<PacienteResumo> Pacientes { get; set; } = new();


        public int TotalPacientes { get; set; }


        public int TotalConsultasConfirmadas { get; set; }


        public int PacientesComProximaConsulta { get; set; }


        public int ConsultasHoje { get; set; }


        public string? ErroCarregamento { get; set; }


        [BindProperty(SupportsGet = true)]
        public string? Busca { get; set; }


        // =====================================================
        // GET
        // =====================================================

        public async Task<IActionResult> OnGetAsync()
        {
            var clinicaId =
                ObterClinicaLogadaId();


            if (!clinicaId.HasValue)
            {
                LimparSessaoClinica();

                return RedirectToPage(
                    "/LoginClinica"
                );
            }


            try
            {
                var cancellationToken =
                    HttpContext.RequestAborted;


                var clinica =
                    await _context.Clinicas
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            c => c.Id == clinicaId.Value,
                            cancellationToken
                        );


                if (clinica == null)
                {
                    LimparSessaoClinica();

                    return RedirectToPage(
                        "/LoginClinica"
                    );
                }


                Clinica = clinica;


                await CarregarPacientesAsync(
                    clinicaId.Value,
                    cancellationToken
                );


                return Page();
            }
            catch (OperationCanceledException)
            {
                ErroCarregamento =
                    "O carregamento foi interrompido. Atualize a página e tente novamente.";

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao carregar pacientes da clínica {ClinicaId}.",
                    clinicaId.Value
                );


                ErroCarregamento =
                    "Não foi possível carregar seus pacientes neste momento.";

                return Page();
            }
        }


        // =====================================================
        // CARREGAR PACIENTES
        // =====================================================

        private async Task CarregarPacientesAsync(
            int clinicaId,
            CancellationToken cancellationToken)
        {
            /*
             * A área Pacientes mostra somente pessoas que
             * já possuem consulta CONFIRMADA com esta clínica.
             */

            IQueryable<Consulta> query =
                _context.Consultas
                    .AsNoTracking()
                    .Where(
                        c =>
                            c.ClinicaId == clinicaId
                            &&
                            c.Status == "Confirmado"
                    );


            // =================================================
            // BUSCA
            // =================================================

            if (!string.IsNullOrWhiteSpace(
                Busca))
            {
                var busca =
                    Busca.Trim();


                query =
                    query.Where(
                        c =>
                            (
                                c.NomePaciente != null
                                &&
                                c.NomePaciente.Contains(
                                    busca
                                )
                            )
                            ||
                            (
                                c.EmailPaciente != null
                                &&
                                c.EmailPaciente.Contains(
                                    busca
                                )
                            )
                    );
            }


            /*
             * Trazemos somente as consultas confirmadas
             * desta clínica.
             */

            var consultas =
                await query
                    .OrderByDescending(
                        c => c.DataConsulta
                    )
                    .ToListAsync(
                        cancellationToken
                    );


            var agora =
                DateTime.Now;

            var hoje =
                DateTime.Today;


            TotalConsultasConfirmadas =
                consultas.Count;


            ConsultasHoje =
                consultas.Count(
                    c =>
                        c.DataConsulta.HasValue
                        &&
                        c.DataConsulta.Value.Date
                            == hoje
                );


            /*
             * Agrupamento do paciente.
             *
             * Primeiro usamos o email porque é mais confiável.
             *
             * Se não houver email, usamos o nome.
             */

            var grupos =
                consultas.GroupBy(
                    consulta =>
                    {
                        var email =
                            NormalizarEmail(
                                consulta.EmailPaciente
                            );


                        if (!string.IsNullOrWhiteSpace(
                            email))
                        {
                            return $"EMAIL:{email}";
                        }


                        var nome =
                            NormalizarTexto(
                                consulta.NomePaciente
                            );


                        return $"NOME:{nome}";
                    }
                );


            var pacientes =
                new List<PacienteResumo>();


            foreach (var grupo in grupos)
            {
                var consultasPaciente =
                    grupo
                        .OrderBy(
                            c => c.DataConsulta
                        )
                        .ToList();


                var primeiraConsulta =
                    consultasPaciente.First();


                var nome =
                    consultasPaciente
                        .Select(
                            c => c.NomePaciente
                        )
                        .FirstOrDefault(
                            nomePaciente =>
                                !string.IsNullOrWhiteSpace(
                                    nomePaciente
                                )
                        )
                    ?? "Paciente";


                var email =
                    consultasPaciente
                        .Select(
                            c => c.EmailPaciente
                        )
                        .FirstOrDefault(
                            emailPaciente =>
                                !string.IsNullOrWhiteSpace(
                                    emailPaciente
                                )
                        )
                    ?? "";


                /*
                 * Consulta futura mais próxima.
                 */

                var proximaConsulta =
                    consultasPaciente
                        .Where(
                            c =>
                                c.DataConsulta.HasValue
                                &&
                                c.DataConsulta.Value >= agora
                        )
                        .OrderBy(
                            c => c.DataConsulta
                        )
                        .FirstOrDefault();


                /*
                 * Última consulta já ocorrida.
                 */

                var ultimaConsulta =
                    consultasPaciente
                        .Where(
                            c =>
                                c.DataConsulta.HasValue
                                &&
                                c.DataConsulta.Value < agora
                        )
                        .OrderByDescending(
                            c => c.DataConsulta
                        )
                        .FirstOrDefault();


                /*
                 * Se ainda não houve consulta passada,
                 * pegamos a primeira confirmada disponível
                 * para identificar o tipo de atendimento.
                 */

                var consultaReferencia =
                    proximaConsulta
                    ?? ultimaConsulta
                    ?? primeiraConsulta;


                pacientes.Add(
                    new PacienteResumo
                    {
                        Nome =
                            nome.Trim(),

                        Email =
                            email.Trim(),

                        TotalConsultas =
                            consultasPaciente.Count,

                        UltimaConsulta =
                            ultimaConsulta?
                                .DataConsulta,

                        ProximaConsulta =
                            proximaConsulta?
                                .DataConsulta,

                        TipoAtendimento =
                            string.IsNullOrWhiteSpace(
                                consultaReferencia.TipoAtendimento
                            )
                                ? "Não informado"
                                : consultaReferencia
                                    .TipoAtendimento
                                    .Trim()
                    }
                );
            }


            /*
             * Pacientes com consulta marcada aparecem primeiro.
             *
             * Depois mostramos quem teve atendimento mais
             * recentemente.
             */

            Pacientes =
                pacientes
                    .OrderByDescending(
                        p =>
                            p.ProximaConsulta.HasValue
                    )
                    .ThenBy(
                        p =>
                            p.ProximaConsulta
                    )
                    .ThenByDescending(
                        p =>
                            p.UltimaConsulta
                    )
                    .ThenBy(
                        p =>
                            p.Nome
                    )
                    .ToList();


            TotalPacientes =
                Pacientes.Count;


            PacientesComProximaConsulta =
                Pacientes.Count(
                    p =>
                        p.ProximaConsulta.HasValue
                );
        }


        // =====================================================
        // CLÍNICA LOGADA
        // =====================================================

        private int? ObterClinicaLogadaId()
        {
            var id =
                HttpContext.Session.GetString(
                    "ClinicaLogada"
                );


            if (string.IsNullOrWhiteSpace(
                id))
            {
                return null;
            }


            if (!int.TryParse(
                id,
                out var clinicaId))
            {
                return null;
            }


            return clinicaId;
        }


        // =====================================================
        // LIMPAR SESSÃO
        // =====================================================

        private void LimparSessaoClinica()
        {
            HttpContext.Session.Remove(
                "ClinicaLogada"
            );
        }


        // =====================================================
        // NORMALIZAÇÃO
        // =====================================================

        private static string NormalizarEmail(
            string? email)
        {
            return (email ?? "")
                .Trim()
                .ToLowerInvariant();
        }


        private static string NormalizarTexto(
            string? texto)
        {
            return (texto ?? "")
                .Trim()
                .ToLowerInvariant();
        }


        // =====================================================
        // VIEW MODEL DO PACIENTE
        // =====================================================

        public sealed class PacienteResumo
        {
            public string Nome { get; set; } = "";


            public string Email { get; set; } = "";


            public int TotalConsultas { get; set; }


            public DateTime? UltimaConsulta { get; set; }


            public DateTime? ProximaConsulta { get; set; }


            public string TipoAtendimento { get; set; } = "";
        }
    }
}