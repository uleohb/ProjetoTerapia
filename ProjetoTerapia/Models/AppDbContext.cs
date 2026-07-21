using Microsoft.EntityFrameworkCore;

namespace ProjetoTerapia.Models
{
    public class AppDbContext : DbContext
    {
        public DbSet<Clinica> Clinicas { get; set; }
        public DbSet<RegistroHumor> RegistrosHumor { get; set; }
        public DbSet<ResultadoTestePaciente> ResultadosTestePacientes { get; set; }
        public DbSet<RecuperacaoSenhaPaciente> RecuperacoesSenhaPacientes { get; set; }
        public DbSet<Paciente> Pacientes { get; set; }
        public DbSet<RecuperacaoSenha> RecuperacoesSenha { get; set; }
        public DbSet<Agendamento> Agendamentos { get; set; }
        public DbSet<Consulta> Consultas { get; set; }
        public DbSet<DivulgacaoRegional> DivulgacoesRegionais { get; set; }
        public DbSet<AdminUsuario> AdminUsuarios { get; set; }
        public DbSet<AdminLog> AdminLogs { get; set; }
        public DbSet<AlteracaoClinica> AlteracoesClinicas { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }
    }
}