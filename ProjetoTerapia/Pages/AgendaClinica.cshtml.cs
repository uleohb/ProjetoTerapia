using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjetoTerapia.Models;

namespace ProjetoTerapia.Pages
{
    public class AgendaClinicaModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AgendaClinicaModel> _logger;

        public AgendaClinicaModel(
            AppDbContext context,
            ILogger<AgendaClinicaModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<Consulta> Consultas { get; set; } = new();

        public List<Consulta> SolicitacoesPendentes { get; set; } = new();

        public int ConsultasHoje { get; set; }

        public int Confirmadas { get; set; }

        public int Pendentes { get; set; }

        public int Canceladas { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Busca { get; set; }

        public Clinica Clinica { get; set; } = new();

        public string? ErroCarregamento { get; set; }


        // =====================================================
        // ABRIR AGENDA
        // =====================================================

        public async Task<IActionResult> OnGetAsync()
        {
            var clinicaId = ObterClinicaLogadaId();

            if (!clinicaId.HasValue)
            {
                LimparSessaoClinica();

                return RedirectToPage("/LoginClinica");
            }

            try
            {
                var clinica = await _context.Clinicas
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        c => c.Id == clinicaId.Value
                    );

                if (clinica == null)
                {
                    LimparSessaoClinica();

                    return RedirectToPage("/LoginClinica");
                }

                Clinica = clinica;

                await CarregarAgendaAsync(
                    clinicaId.Value
                );

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao carregar agenda da clínica {ClinicaId}.",
                    clinicaId.Value
                );

                ErroCarregamento =
                    "Não foi possível carregar sua agenda agora. Tente novamente em alguns instantes.";

                return Page();
            }
        }


        // =====================================================
        // CONFIRMAR CONSULTA
        // =====================================================

        public async Task<IActionResult> OnPostConfirmarAsync(int id)
        {
            var clinicaId = ObterClinicaLogadaId();

            if (!clinicaId.HasValue)
            {
                LimparSessaoClinica();

                return RedirectToPage("/LoginClinica");
            }

            try
            {
                var consulta = await _context.Consultas
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id == id &&
                            x.ClinicaId == clinicaId.Value
                    );

                if (consulta == null)
                {
                    return RedirectToPage("/AgendaClinica");
                }

                consulta.Status = "Confirmado";

                await _context.SaveChangesAsync();

                return RedirectToPage("/AgendaClinica");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao confirmar consulta {ConsultaId}.",
                    id
                );

                return RedirectToPage("/AgendaClinica");
            }
        }


        // =====================================================
        // CANCELAR CONSULTA
        // =====================================================

        public async Task<IActionResult> OnPostCancelarAsync(int id)
        {
            var clinicaId = ObterClinicaLogadaId();

            if (!clinicaId.HasValue)
            {
                LimparSessaoClinica();

                return RedirectToPage("/LoginClinica");
            }

            try
            {
                var consulta = await _context.Consultas
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id == id &&
                            x.ClinicaId == clinicaId.Value
                    );

                if (consulta == null)
                {
                    return RedirectToPage("/AgendaClinica");
                }

                consulta.Status = "Cancelado";

                await _context.SaveChangesAsync();

                return RedirectToPage("/AgendaClinica");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao cancelar consulta {ConsultaId}.",
                    id
                );

                return RedirectToPage("/AgendaClinica");
            }
        }


        // =====================================================
        // EXCLUIR CONSULTA
        // =====================================================

        public async Task<IActionResult> OnPostExcluirAsync(int id)
        {
            var clinicaId = ObterClinicaLogadaId();

            if (!clinicaId.HasValue)
            {
                LimparSessaoClinica();

                return RedirectToPage("/LoginClinica");
            }

            try
            {
                var consulta = await _context.Consultas
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id == id &&
                            x.ClinicaId == clinicaId.Value
                    );

                if (consulta == null)
                {
                    return RedirectToPage("/AgendaClinica");
                }

                _context.Consultas.Remove(consulta);

                await _context.SaveChangesAsync();

                return RedirectToPage("/AgendaClinica");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao excluir consulta {ConsultaId}.",
                    id
                );

                return RedirectToPage("/AgendaClinica");
            }
        }


        // =====================================================
        // CARREGAR AGENDA
        // =====================================================

        private async Task CarregarAgendaAsync(int clinicaId)
        {
            /*
             * Por enquanto carregamos apenas Consulta.
             *
             * Removi:
             *
             * Include(x => x.Paciente)
             * Include(x => x.ResultadoTestePaciente)
             *
             * porque a versão atual da tela não precisa desses
             * relacionamentos para abrir a agenda.
             *
             * Isso deixa a consulta mais simples e reduz a chance
             * de um relacionamento antigo quebrar a página.
             */

            IQueryable<Consulta> query =
                _context.Consultas
                    .AsNoTracking()
                    .Where(
                        x => x.ClinicaId == clinicaId
                    );


            // =================================================
            // BUSCA
            // =================================================

            if (!string.IsNullOrWhiteSpace(Busca))
            {
                var busca = Busca.Trim();

                query = query.Where(
                    x =>
                        (
                            x.NomePaciente != null &&
                            EF.Functions.Like(
                                x.NomePaciente,
                                $"%{busca}%"
                            )
                        )
                        ||
                        (
                            x.EmailPaciente != null &&
                            EF.Functions.Like(
                                x.EmailPaciente,
                                $"%{busca}%"
                            )
                        )
                );
            }


            var todas = await query
                .OrderBy(x => x.DataConsulta)
                .ToListAsync();


            // =================================================
            // PENDENTES
            // =================================================

            SolicitacoesPendentes = todas
                .Where(
                    x => string.Equals(
                        x.Status,
                        "Pendente",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                .OrderBy(x => x.DataConsulta)
                .ToList();


            // =================================================
            // CONFIRMADAS
            // =================================================

            Consultas = todas
                .Where(
                    x => string.Equals(
                        x.Status,
                        "Confirmado",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                .OrderBy(x => x.DataConsulta)
                .ToList();


            // =================================================
            // CONTADORES
            // =================================================

            var hoje = DateTime.Today;


            ConsultasHoje = Consultas.Count(
                x =>
                    x.DataConsulta.HasValue &&
                    x.DataConsulta.Value.Date == hoje
            );


            Confirmadas = todas.Count(
                x => string.Equals(
                    x.Status,
                    "Confirmado",
                    StringComparison.OrdinalIgnoreCase
                )
            );


            Pendentes = todas.Count(
                x => string.Equals(
                    x.Status,
                    "Pendente",
                    StringComparison.OrdinalIgnoreCase
                )
            );


            Canceladas = todas.Count(
                x => string.Equals(
                    x.Status,
                    "Cancelado",
                    StringComparison.OrdinalIgnoreCase
                )
            );
        }


        // =====================================================
        // OBTER CLÍNICA LOGADA
        // =====================================================

        private int? ObterClinicaLogadaId()
        {
            var clinicaIdString =
                HttpContext.Session.GetString(
                    "ClinicaLogada"
                );

            if (string.IsNullOrWhiteSpace(
                clinicaIdString))
            {
                return null;
            }

            if (!int.TryParse(
                clinicaIdString,
                out var clinicaId))
            {
                return null;
            }

            if (clinicaId <= 0)
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
    }
}